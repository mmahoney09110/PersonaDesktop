# Persona Desktop: A Smart, Customizable PC Assistant

**Persona Desktop** is a desktop application that lets you control your PC with natural commands, all filtered through a personality you define. It can launch programs, delete files, adjust volume, empty the recycle bin, and more. If it doesn't understand a command, it offers smart, clickable fallback suggestions.

Unlike typical utilities, Windows Assistant gives your assistant a *soul*, you choose how it speaks, how it reacts, and how it interacts with you.

---

## Features (MVP)

- **Command-Based Desktop Assistant**
  - Launch applications by name (`open chrome`)
  - Delete specific files with confirmation
  - Adjust system volume (`set volume 50`)
  - Empty recycle bin
  - Open folders by common name (`open downloads`)
  - more

- **Fallback System**
  - If your input isn’t recognized, the assistant suggests possible matches or alternatives

- **Custom Personality Profiles**
  - Edit assistants with unique names and tones
  - Instant personality swapping in the UI

- **Simple, Clean UI**
  - WPF-based command bar and response log
  - Assistant display with name and optional avatar
  - Clickable fallback options when needed
 
- **TTS and STT
  - Optional TTS using mircrosoft voices
  - STT command activated by saying wake word "Persona"

---

## Planned (Post-MVP)
- Custom macros (e.g., "start work mode" = open Outlook + set volume + launch Slack)
- File system browsing commands (`find files with name report`)
- Internet search fallback if command fails (`search "how to uninstall Discord"`)

---

## Tech Stack

| Area | Tools |
|------|-------|
| UI | C# WPF (.NET 9) |
| Command Parser | Custom intent matcher |
| Personality Config | JSON-driven assistant templates |
| File Ops | `System.IO`, Windows Shell Interop |
| Volume Control | `NAudio` |
| Hotkeys | Global keyboard listener (TBD) |

---

## Example Usage

```
> open chrome  
[Persona Juno]: “On it. Launching Chrome now.”

> set volume to 30  
[Persona Juno]: “Lowering the noise, volume is now 30%.”

> delete file taxes2024.pdf  
[Persona Juno]: “Are you sure you want to delete taxes2024.pdf? (Yes / No)”

> open chrmo  
[Persona Juno]: “Hmm, did you mean:
  - Open Chrome
  - Open Calculator
  - Open Downloads?”
```

---

## Getting Started

1. Clone the repo
2. Grab and install assets in Releases
3. Open in Visual Studio 2022+
4. Run the project (`WindowsAssistant.sln`)
5. Start entering commands or edit settings.

---

## Why This Project?

Persona Desktop combines **daily utility** with **custom personality**. It’s a smart desktop companion that reflects your style: helpful, sarcastic, robotic, or friendly. I want to add flair to the classic assitant and bring something special to a PC helper. For me its also a portfolio builder that showcases my skills in:
- Desktop UI/UX
- OS-level command handling
- AI integration
- API integration
- Modular software architecture

---

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).
