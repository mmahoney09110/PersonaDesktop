using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WindowsAssistant.Services;

namespace PersonaDesk.Services
{
    public class CommandService
    {
        // commands
        private readonly string[] _commands = new[]
        {
            "open browser",
            "change volume to",
            "turn up volume",
            "turn down volume",
            "empty recycle bin",
            "delete file",
            "open folder",
            "show help"
        };

        private enum InteractionState
        {
            None,
            AwaitingFilePath,
            AwaitingDeleteConfirmation
        }

        private InteractionState _state = InteractionState.None;
        private string _pendingFilePath = null;

        private const double THRESHOLD = 0.45;

        public async Task<string> HandleCommand(string input)
        {
            input = input?.Trim();
            if (string.IsNullOrWhiteSpace(input))
                return "No command entered.";

            // Handle confirmation if we're in delete-confirm state
            if (_state == InteractionState.AwaitingDeleteConfirmation)
            {
                if (input == "yes" || input == "y")
                {
                    try
                    {
                        File.Delete(_pendingFilePath);
                        _state = InteractionState.None;
                        return $"Deleted: {_pendingFilePath}";
                    }
                    catch (Exception ex)
                    {
                        _state = InteractionState.None;
                        return $"Failed to delete file: {ex.Message}";
                    }
                }
                else
                {
                    _state = InteractionState.None;
                    return "File deletion cancelled.";
                }
            }

            // Handle file name/path if we're waiting
            if (_state == InteractionState.AwaitingFilePath)
            {
                var resolved = ResolveFilePath(input);
                if (resolved == null)
                {
                    _state = InteractionState.None;
                    return "Could not find a file with that name or path.";
                }

                _pendingFilePath = resolved;
                _state = InteractionState.AwaitingDeleteConfirmation;
                return $"Found: {_pendingFilePath}. Delete it? (yes/no)";
            }

            // Get the user embedding
            var userEmb = await App.EmbeddingService
                                   .GetEmbeddingAsync(input)
                                   .ConfigureAwait(false);

            // Generate embeddings for each canonical command in parallel
            var tasks = _commands
                .Select(cmd => App.EmbeddingService
                                   .GetEmbeddingAsync(cmd)
                                   .ContinueWith(t => (Command: cmd, Emb: t.Result)))
                .ToArray();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Compute similarities, log each, and pick the best
            string bestCmd = null;
            double bestScore = double.MinValue;


            foreach (var (Command, Emb) in results)
            {
                double score = Cosine(userEmb, Emb);
                // Log to console
                Console.WriteLine($"[Similarity] \"{Command}\" → {score:F2}");

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCmd = Command;
                }
            }

            // Check threshold
            if (bestScore < THRESHOLD)
                return $"No valid command found. Did you mean {bestCmd}?";

            if (bestCmd == "change volume to")
            {
                int? percent = ExtractVolumePercentage(input);
                if (percent.HasValue)
                    return ChangeVolumeTo(percent.Value);
                else
                    return "I didn’t catch what volume to set. Try something like 'change volume to 50'.";
            }

            // Execute the matched command
            string actionResult = bestCmd switch
            {
                "open browser" => Try(() => Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.google.com",
                    UseShellExecute = true
                }), "Opening browser…"),
                "turn up volume" => ChangeVolume(0.1f),
                "turn down volume" => ChangeVolume(-0.1f),
                "empty recycle bin" => new WindowsInterop().EmptyRecycleBin(),
                "delete file" => StartFileDeleteFlow(),
                "open folder" => OpenFolderFromInput(ExtractFolderTarget(input)),
                "show help" => "Available commands: " + string.Join(", ", _commands),
                _ => $"Matched {bestCmd} (score {bestScore:F2})"
            };

            // return when command is finished
            return actionResult;
        }

        // small helper to catch errors in Process.Start
        private string Try(Action a, string successMsg)
        {
            try { a(); return successMsg; }
            catch (Exception ex) { return $"Error: {ex.Message}"; }
        }


        private static double Cosine(float[] a, float[] b)
        {
            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        private string ChangeVolume(float delta)
        {
            try
            {
                var device = new AudioSwitcher.AudioApi.CoreAudio.CoreAudioController()
                                .DefaultPlaybackDevice;
                double newVolume = Math.Clamp(device.Volume + (delta * 100), 0, 100);
                device.Volume = newVolume;
                return $"Volume set to {newVolume:0}%";
            }
            catch (Exception ex)
            {
                return $"Failed to change volume: {ex.Message}";
            }
        }

        private int? ExtractVolumePercentage(string input)
        {
            // Try to find a number in the user input
            var match = System.Text.RegularExpressions.Regex.Match(input, @"\b(\d{1,3})\b");

            if (match.Success && int.TryParse(match.Groups[1].Value, out int percent))
            {
                return Math.Clamp(percent, 0, 100);
            }

            return null;
        }

        private string ChangeVolumeTo(int percent)
        {
            try
            {
                var device = new AudioSwitcher.AudioApi.CoreAudio.CoreAudioController()
                                .DefaultPlaybackDevice;
                device.Volume = percent;
                return $"Volume set to {percent}%";
            }
            catch (Exception ex)
            {
                return $"Failed to change volume: {ex.Message}";
            }
        }

        private string StartFileDeleteFlow()
        {
            _state = InteractionState.AwaitingFilePath;
            _pendingFilePath = null;
            return "Sure! You can give me the location or name and I’ll see what I can do.";
        }

        private string ResolveFilePath(string input)
        {
            if (File.Exists(input))
                return Path.GetFullPath(input);

            var folders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos)
            };

            foreach (var folder in folders)
            {
                Console.WriteLine($"[Scanning] {folder} for {input}");
                try
                {
                    var matches = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                        .Where(f => Path.GetFileName(f).Equals(input, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (matches.Count > 0)
                        return matches[0];
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"[Access Denied] Skipping {folder}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Scanning {folder}: {ex.Message}");
                }
            }

            return null;
        }
        private string OpenFolderFromInput(string input)
        {
            var path = ResolveFolderOpenPath(input);

            if (path != null)
            {
                Process.Start("explorer.exe", path);
                return $"Opening: {path}";
            }

            return $"Sorry, I couldn’t find a folder {input}.";
        }
        private string ExtractFolderTarget(string input)
        {
            // Split the input into words and grab the last one as the folder name
            var words = input?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words == null || words.Length == 0)
                return "";

            // Handle cases like "open folder downloads" or "open folder my pictures"
            var index = Array.FindIndex(words, w => w.Equals("folder", StringComparison.OrdinalIgnoreCase));

            if (index >= 0 && index < words.Length - 1)
            {
                // Return all remaining words after "folder"
                return string.Join(' ', words.Skip(index + 1));
            }

            // Fallback: use the last word
            return words[^1];
        }

        private string ResolveFolderOpenPath(string input)
        {
            if (Directory.Exists(input))
                return Path.GetFullPath(input);

            // Known folder shortcuts
            var knownFolders = new Dictionary<string, Environment.SpecialFolder>(StringComparer.OrdinalIgnoreCase)
            {
                { "Desktop", Environment.SpecialFolder.Desktop },
                { "Documents", Environment.SpecialFolder.MyDocuments },
                { "Pictures", Environment.SpecialFolder.MyPictures },
                { "Music", Environment.SpecialFolder.MyMusic },
                { "Videos", Environment.SpecialFolder.MyVideos },
                { "AppData", Environment.SpecialFolder.ApplicationData },
                { "CommonDocuments", Environment.SpecialFolder.CommonDocuments },
                { "CommonPictures", Environment.SpecialFolder.CommonPictures },
                { "CommonMusic", Environment.SpecialFolder.CommonMusic },
                { "CommonVideos", Environment.SpecialFolder.CommonVideos },
                { "UserProfile", Environment.SpecialFolder.UserProfile }, // UserProfile is a common base
                { "Downloads", Environment.SpecialFolder.UserProfile } // Downloads is inside UserProfile
            };

            if (knownFolders.TryGetValue(input, out var specialFolder))
            {
                var basePath = Environment.GetFolderPath(specialFolder);

                // Downloads isn't a SpecialFolder directly; handle it manually
                if (input == "Downloads" || input == "downloads")
                    return Path.Combine(basePath, "Downloads");

                return basePath;
            }

            var folders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos)
            };

            foreach (var root in folders)
            {
                Console.WriteLine($"[Scanning Folders] {root} for '{input}'");

                try
                {
                    
                    var match = Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
                        .FirstOrDefault(dir => Path.GetFileName(dir)
                            .Equals(input, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                        return match;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"[Access Denied] {root}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] {root}: {ex.Message}");
                }
            }

            return null;
        }

    }
}
