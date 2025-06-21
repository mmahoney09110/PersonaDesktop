# 🧠 PersonaDesk: A Smart, Customizable PC Assistant

**PersonaDesk** is a desktop application that lets you control your PC with natural commands, all filtered through a personality you define. It can launch programs, delete files, adjust volume, empty the recycle bin, and more. If it doesn't understand a command, it offers smart, clickable fallback suggestions.

Unlike typical utilities, PersonaDesk gives your assistant a *soul*—you choose how it speaks, how it reacts, and how it interacts with you.

---

## ✨ Features (MVP)

- ⚙️ **Command-Based Desktop Assistant**
  - Launch applications by name (`open chrome`)
  - Delete specific files with confirmation
  - Adjust system volume (`set volume 50`)
  - Empty recycle bin
  - Open folders by common name (`open downloads`)

- 🧠 **Fallback System**
  - If your input isn’t recognized, the assistant suggests possible matches or alternatives
  - Click on a suggestion to auto-run it

- 🧑‍🎨 **Custom Personality Profiles**
  - Create assistants with unique names, tones, and response templates
  - Store personalities in simple editable JSON
  - Instant personality swapping in the UI

- 🖥️ **Simple, Clean UI**
  - WPF-based command bar and response log
  - Assistant display with name and optional avatar
  - Clickable fallback options when needed

---

## 🚧 Planned (Post-MVP)

- 🎤 Voice command input (Whisper.cpp, Azure STT, or Vosk)
- 🔊 Text-to-speech replies (Azure Neural TTS or ElevenLabs)
- 📜 Custom macros (e.g., "start work mode" = open Outlook + set volume + launch Slack)
- 📁 File system browsing commands (`find files with name report`)
- 🌐 Internet search fallback if command fails (`search "how to uninstall Discord"`)

---

## 🛠️ Tech Stack

| Area | Tools |
|------|-------|
| UI | C# WPF (.NET 8) |
| Command Parser | Custom intent matcher w/ fallback engine |
| Personality Config | JSON-driven assistant templates |
| File Ops | `System.IO`, Windows Shell Interop |
| Volume Control | `CoreAudioApi` or `NAudio` |
| Hotkeys | Global keyboard listener (TBD) |

---

## 🧪 Example Usage

```
> open chrome  
[Persona Juno]: “On it. Launching Chrome now.”

> set volume to 30  
[Persona Juno]: “Lowering the vibes—volume is now 30%.”

> delete file taxes2024.pdf  
[Persona Juno]: “Are you sure you want to delete taxes2024.pdf? (Yes / No)”

> open chrmo  
[Persona Juno]: “Hmm, did you mean:
  - Open Chrome
  - Open Calculator
  - Open Downloads?”
```

---

## 📁 Personality Config Format

```json
{
  "name": "Juno",
  "tone": "Witty and helpful",
  "responses": {
    "launch": "Launching {app}.",
    "volume": "Volume set to {value}%.",
    "delete": "Deleted {filename}. Gone forever… unless it’s in the bin.",
    "fallback": "Didn't quite catch that. Maybe you meant one of these?"
  }
}
```

---

## 🚀 Getting Started (when ready)

1. Clone the repo
2. Open in Visual Studio 2022+
3. Run the project (`PersonaDesk.sln`)
4. Start entering commands or swap out the `Personality.json` in `Assets/Personas/`

---

## 🙋 Why This Project?

PersonaDesk blends **daily utility** with **custom flair**. It’s a smart desktop companion that reflects your style—helpful, sarcastic, robotic, or friendly. Perfect for productivity, personalization, and showcasing your skills in:
- Desktop UI/UX
- OS-level command handling
- AI integration (planned)
- Modular software architecture
