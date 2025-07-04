using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using WindowsAssistant.Services;

namespace PersonaDesk.Services
{
    public class CommandService
    {
        private SettingsModel _settings;
        private TtsService _ttsService = new TtsService();
        private List<MessageModel> _chatHistory;

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
            "open file",
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

        private const double THRESHOLD = 0.44;

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
                        return await PersonaResponse($"to delete a file and they did confirm yes so you deleted file {_pendingFilePath}.", $"Deleted: {_pendingFilePath}");
                    }
                    catch (Exception ex)
                    {
                        _state = InteractionState.None;
                        return await PersonaResponse($"to delete a file but something went wrong trying to delete {_pendingFilePath}. Error: {ex}", $"Failed to delete file: {ex.Message}");
                    }
                }
                else if (input == "no" || input == "n")
                {
                    _state = InteractionState.None;
                    return await PersonaResponse("to delete a file but they cancelled the request", "File deletion cancelled.");
                }
                else
                {
                    return await PersonaResponse($"to delete a file but they did not confirm with yes or no and responded with {input}. They need to say yes or no.", "Please respond with 'yes' or 'no'.");
                }
            }

            // Handle file name/path if we're waiting
            if (_state == InteractionState.AwaitingFilePath)
            {
                // Check if the input is a valid file path
                if (File.Exists(input))
                {
                    _pendingFilePath = Path.GetFullPath(input);
                    _state = InteractionState.AwaitingDeleteConfirmation;
                    return await PersonaResponse($"delete file at {_pendingFilePath}, ask for confirmation and tell the where the file was found (yes/no).", $"Found: {_pendingFilePath}. Delete it? (yes/no)");
                }
                else
                {
                    // Extract file name and extension if specified
                    var fileName = Path.GetFileNameWithoutExtension(input);
                    var ext = Path.GetExtension(input);

                    string[]? extensions = null;
                    if (!string.IsNullOrEmpty(ext))
                        extensions = new[] { ext };

                    // Call ResolvePath with extension (enforces if provided)
                    var resolved = ResolvePath(fileName, false, extensions);

                    if (resolved == null)
                    {
                        _state = InteractionState.None;
                        return await PersonaResponse("find file to delete but you found nothing", "Could not find a file with that name or path.");
                    }

                    _pendingFilePath = resolved;
                    _state = InteractionState.AwaitingDeleteConfirmation;
                    return await PersonaResponse($"delete file at {_pendingFilePath}, ask for confirmation and tell the where the file was found (yes/no).", $"Found: {_pendingFilePath}. Delete it? (yes/no)");
                }
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

            if (bestScore <= THRESHOLD)
            {
                // Ask OpenAI to help classify
                var aiGuess = await PersonaResponse(
                    $"User input: '{input}'.\n" +
                    "If this matches one of these system commands, respond ONLY with that exact command string (case sensitive): " +
                    string.Join(", ", _commands) +
                    ".\n" +
                    "If this does not match any system command, respond with 'NONE'. Do not explain.",
                    "Hmm, I’m not sure what command that is..."
                );

                if (!string.IsNullOrWhiteSpace(aiGuess))
                {
                    aiGuess = aiGuess.Trim();

                    // Check if AI guessed a valid command
                    if (_commands.Contains(aiGuess))
                    {
                        bestCmd = aiGuess;
                    }
                    else if (aiGuess.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                    {
                        // AI determined it's not a system command — treat as chat
                        return await PersonaResponse($" to just chat: {input}", $"Okay! Let’s talk: {input}");
                    }
                    else
                    {
                        // Unknown fallback — treat as chat too
                        return await PersonaResponse($" to just chat: {input}", $"Hmm, I’m not sure what command that is... did you mean {bestCmd}?");
                    }
                }
                else
                {
                    // No guess at all — fallback to chat
                    return await PersonaResponse($" to just chat: {input}", $"Hmm, I’m not sure what command that is... did you mean {bestCmd}?");
                }
            }

            if (bestCmd == "change volume to")
            {
                int? percent = await ExtractVolumePercentage(input);
                if (percent.HasValue)
                    return await ChangeVolumeTo(percent.Value);
                else
                    return await PersonaResponse($"change volume but did not correctly specify what number and said '{input}'", "I didn’t catch what volume to set. Try something like 'change volume to 50'.");
            }

            // Execute the matched command
            string actionResult = bestCmd switch
            {
                "open browser" => await Try(() => Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.google.com",
                    UseShellExecute = true
                }), await PersonaResponse("Open browser", "Opening browser...")),
                "turn up volume" => await ChangeVolume(0.1f),
                "turn down volume" => await ChangeVolume(-0.1f),
                "empty recycle bin" => await Try(() => new WindowsInterop().EmptyRecycleBin(), await PersonaResponse("empty recycle bin", "Recycle bin emptied.")),
                "delete file" => await StartFileDeleteFlow(),
                "open folder" => await OpenFolderFromInput(ExtractFolderTarget(input)),
                "open file" => await OpenFileFromInput(ExtractFileTarget(input)),
                "show help" => await PersonaResponse($"show all commands. These are: " + string.Join(", ", _commands), "Available commands: " + string.Join(", ", _commands)),
                _ => $"Matched {bestCmd} but did not find command"
            };

            // return when command is finished
            return actionResult;
        }

        public async Task<string> PersonaResponse(string command, string fallback)
        {
            var result = fallback;
            try
            {
                _chatHistory = ChatHistoryService.LoadHistory();
                var recentMessages = _chatHistory.TakeLast(10).ToList();

                _settings = SettingsService.LoadSettings();
                var sender = new PersonaResponse();
                string name = _settings.AssistantName ?? "Persona";
                var today = DateTime.Now.ToString("MMMM dd, yyyy");
                string system = $"Your name is {name}. You are a Persona, an assistant with personality that has the ability to manage the users PC and run various commands. Do not use formatting or emoticons. When user issues a command don't tell them how to do it, as the program you are apart of handles that. The date is {today}. Respond with this personality: {_settings.PersonalityPrompt}" ?? "You are a friendly assistant.";
                string userMessage = "";
                if (recentMessages.Count != 0)
                {
                    userMessage += $"Message History:\n";
                    foreach (var msg in recentMessages)
                    {
                        userMessage += $"{msg.Role}: {msg.Content}\n";
                    }
                }

                userMessage += $"\nThe user request is {command}";

                result = await sender.SendAsync(system, userMessage);
                Console.WriteLine("Server Response: " + result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            var audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "response.wav");
            if (audioPath != null)
            {
                var player = new System.Media.SoundPlayer(audioPath);
                player.Play();
            }

            if (_settings.SpeechEnabled) 
            { 
                await _ttsService.GenerateSpeechAsync(result);
                audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.wav");
                if (audioPath != null)
                {
                    var player = new System.Media.SoundPlayer(audioPath);
                    player.Play();
                }
            }
            return result;
        }

        // small helper to catch errors in Process.Start
        private async Task<string> Try(Action a, string successMsg)
        {
            try { a(); return successMsg; }
            catch (Exception ex) { return await PersonaResponse($"but got Error: {ex.Message}", $"Error: {ex.Message}"); }
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
        public async Task<string> ChangeVolume(float delta)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (device == null)
                    return await PersonaResponse("chanage volume but no device was found", "No playback device found.");

                float current = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                float updated = Math.Clamp(current + delta, 0f, 1f);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = updated;

                if (current > updated)
                {
                    return await PersonaResponse($"lower volume to {(int)(updated * 100)}%", $"Volume set to {(int)(updated * 100)}%");
                }
                else if (current < updated)
                {
                    return await PersonaResponse($"raise volume to {(int)(updated * 100)}%", $"Volume set to {(int)(updated * 100)}%");
                }
                return await PersonaResponse($"set volume to {(int)(updated * 100)}%", $"Volume set to {(int)(updated * 100)}%");

            }
            catch (Exception ex)
            {
                return $"Failed to change volume: {ex.Message}";
            }
        }

        private async Task<int?> ExtractVolumePercentage(string input)
        {
            // Try to find a number in the user input
            var match = System.Text.RegularExpressions.Regex.Match(input, @"\b(\d{1,3})\b");

            if (match.Success && int.TryParse(match.Groups[1].Value, out int percent))
            {
                return Math.Clamp(percent, 0, 100);
            }

            return null;
        }

        private async Task<string> ChangeVolumeTo(int percent)
        {
            try
            {
                if (percent < 0 || percent > 100)
                    return await PersonaResponse("change volume, butpercent must be between 0 and 100.", "volume percent must be between 0 and 100.");

                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (device == null)
                    return await PersonaResponse("chanage volume but no device was found", "No playback device found.");

                float scalar = percent / 100f; // Convert 0–100 to 0.0–1.0
                device.AudioEndpointVolume.MasterVolumeLevelScalar = scalar;

                return await PersonaResponse($"set volume to {percent}%", $"Volume set to {percent}%");
            }
            catch (Exception ex)
            {
                return $"Failed to change volume: {ex.Message}";
            }
        }

        private async Task<string> StartFileDeleteFlow()
        {
            _state = InteractionState.AwaitingFilePath;
            _pendingFilePath = null;
            return await PersonaResponse("delete a file, ask them for name or location but warn that just the name may take a while to search for it.", "Sure! You can give me the location or name and I’ll see what I can do. Just a name may take a while to search for.");
        }

        private async Task<string> OpenFolderFromInput(string input)
        {
            var path = ResolvePath(input, true);

            if (path != null)
            {
                Process.Start("explorer.exe", path);
                return await PersonaResponse($"to open folder: {path}.", $"Opening: {path}");
            }

            return await PersonaResponse($"to open folder but you found no folder called {input}", $"Sorry, I couldn’t find a folder {input}.");
        }

        private async Task<string> OpenFileFromInput(string input)
        {
            Console.WriteLine($"OpenFileFromInput: {input}");
            string? path = input;
            // Check if the input is a valid file path
            if (File.Exists(input))
            {
                Process.Start("explorer.exe", path);
                return await PersonaResponse($"to open file: {path}.", $"Opening: {path}");
            }
            else
            {
                // Extract file name and extension if specified
                var fileName = Path.GetFileNameWithoutExtension(input);
                var ext = Path.GetExtension(input);

                string[]? extensions = null;
                if (!string.IsNullOrEmpty(ext))
                    extensions = new[] { ext };
                Console.WriteLine($"Extracted file name: {fileName}, extension: {ext}");
                path = ResolvePath(fileName, false, extensions);
            }

            if (path != null)
            {
                Process.Start("explorer.exe", path);
                return await PersonaResponse($"to open file: {path}.", $"Opening: {path}");
            }

            return await PersonaResponse($"to open a file but you found no file called {input}", $"Sorry, I couldn’t find a the file {input}.");
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

        private string ExtractFileTarget(string input)
        {
            // Split the input into words and grab the last one as the file name
            var words = input?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words == null || words.Length == 0)
                return "";

            // Handle cases like "open file test" or "open file my test"
            var index = Array.FindIndex(words, w => w.Equals("file", StringComparison.OrdinalIgnoreCase));

            if (index >= 0 && index < words.Length - 1)
            {
                // Return all remaining words after "file"
                return string.Join(' ', words.Skip(index + 1));
            }

            // Fallback: use the last word
            return words[^1];
        }

        private static readonly string[] CommonRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
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

        public static string? ResolvePath(string targetName, bool isFolder, string[]? extraExtensions = null)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return null;

            // Direct check first
            if (isFolder && Directory.Exists(targetName))
                return Path.GetFullPath(targetName);
            if (!isFolder && File.Exists(targetName))
                return Path.GetFullPath(targetName);

            // Initialize the queue with all root folders
            var queue = new Queue<string>(CommonRoots.Distinct());

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                try
                {
                    var dirInfo = new DirectoryInfo(current);
                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        continue;  // skip symlinks

                    // Check current directory
                    if (isFolder)
                    {
                        if (dirInfo.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                            return current;
                    }
                    else
                    {
                        foreach (var file in Directory.EnumerateFiles(current, "*", SearchOption.TopDirectoryOnly))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            var ext = Path.GetExtension(file);

                            if (extraExtensions != null && extraExtensions.Length > 0)
                            {
                                if (fileName.Equals(targetName, StringComparison.OrdinalIgnoreCase) &&
                                    extraExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                                    return file;
                            }
                            else if (fileName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                            {
                                return file;
                            }
                        }
                    }

                    // Enqueue all subdirectories for the next “level”
                    foreach (var subdir in Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly))
                    {
                        // skip reparse points
                        var subInfo = new DirectoryInfo(subdir);
                        if (!subInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                            queue.Enqueue(subdir);
                    }
                }
                catch (UnauthorizedAccessException) { /* skip */ }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] {current}: {ex.Message}");
                }
            }

            return null;
        }
    }
}
