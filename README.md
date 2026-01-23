# TunesLayer

**Gaming Music Overlay for Windows**

A high-performance, anti-cheat-safe music overlay that lets you control Spotify, Apple Music, YouTube Music, and any other media app while gaming.

## Features

- **Zero Login Required** - Works instantly with Windows Media Session (SMTC)
- **Anti-Cheat Safe** - External overlay with no DLL injection
- **Performance-First** - < 50MB RAM, < 0.1% CPU impact
- **Universal Media Support** - Works with Spotify, Apple Music, YouTube Music, Tidal, Deezer, and more
- **5 Built-in Themes** - Midnight, Neon, Minimal, Glassmorphism, Retro
- **Global Hotkeys** - Control music even in exclusive fullscreen games
- **Discord Integration** - Show what you're listening to
- **OBS Widget** - Now Playing widget for streamers
- **Gaming Analytics** - Track your listening habits while gaming

## Screenshots

*(Coming soon)*

## Installation

### Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime

### Quick Install
1. Download the latest installer from [Releases](https://github.com/yourusername/TunesLayer/releases)
2. Run `TunesLayer-Setup-x.x.x.exe`
3. Launch TunesLayer from the Start menu

### Build from Source
```powershell
# Clone the repository
git clone https://github.com/yourusername/TunesLayer.git
cd TunesLayer

# Build the solution
dotnet restore src/TunesLayer.sln
dotnet build src/TunesLayer.sln -c Release

# Run the application
dotnet run --project src/TunesLayer.App/TunesLayer.App.csproj
```

## Usage

### Basic Controls
| Hotkey | Action |
|--------|--------|
| `Ctrl+Alt+Space` | Play/Pause |
| `Ctrl+Alt+Right` | Next Track |
| `Ctrl+Alt+Left` | Previous Track |
| `Ctrl+Alt+Up` | Volume Up |
| `Ctrl+Alt+Down` | Volume Down |
| `Ctrl+Alt+O` | Toggle Overlay |

### System Tray
- **Double-click** the tray icon to toggle the overlay
- **Right-click** for quick controls and settings

### OBS Integration
1. Enable the OBS Widget in Settings > Integrations
2. Add a Browser Source in OBS
3. Set URL to `http://localhost:5123/widget`
4. Set dimensions to ~350x100

## Architecture

TunesLayer uses a safe, external overlay approach:

```
┌─────────────────────────────────────────────┐
│                Windows                       │
│  ┌───────────────┐  ┌───────────────────┐   │
│  │   Your Game   │  │    TunesLayer     │   │
│  │               │  │  (Separate Window) │   │
│  │               │  │                   │   │
│  │               │  │  ┌─────────────┐  │   │
│  │               │  │  │   Overlay   │  │   │
│  │               │  │  │   (Topmost) │  │   │
│  └───────────────┘  │  └─────────────┘  │   │
│                     └───────────────────┘   │
│  ┌───────────────────────────────────────┐  │
│  │     Windows Media Session (SMTC)      │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐  │  │
│  │  │ Spotify │ │  Apple  │ │ YouTube │  │  │
│  │  │         │ │  Music  │ │  Music  │  │  │
│  │  └─────────┘ └─────────┘ └─────────┘  │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

### Anti-Cheat Safety
TunesLayer is designed to be completely safe for competitive gaming:

- **No DLL Injection** - We never inject code into game processes
- **External Window** - The overlay is a separate Windows application
- **WDA_EXCLUDEFROMCAPTURE** - Invisible to screen capture and anti-cheat scanners
- **No Game Hooks** - We only read from Windows Media Session APIs
- **No Memory Manipulation** - Zero interaction with game memory

## Project Structure

```
TunesLayer/
├── src/
│   ├── TunesLayer.App/           # Main WPF application
│   │   ├── Views/                # XAML windows
│   │   ├── ViewModels/           # MVVM view models
│   │   ├── Themes/               # XAML theme files
│   │   └── Assets/               # Icons and resources
│   ├── TunesLayer.Core/          # Business logic
│   │   ├── Models/               # Data models
│   │   └── Services/             # Core services
│   ├── TunesLayer.Overlay/       # Overlay window
│   └── TunesLayer.Integrations/  # Discord, OBS
│       ├── Discord/
│       └── OBS/
├── installer/                     # Inno Setup scripts
└── README.md
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | WPF (.NET 8) |
| Rendering | DirectComposition |
| Media API | Windows.Media.Control (WinRT) |
| Hotkeys | Win32 RegisterHotKey |
| Settings | JSON + SQLite |
| Discord | DiscordRichPresence |
| OBS | HTTP Server + WebSocket |

## Legal Notice

### Not a Spotify App
TunesLayer is a **Windows System Media Controller**, not a Spotify application. It:
- Does **NOT** use the Spotify API
- Does **NOT** require Spotify authentication
- Works with **ANY** media app that publishes to Windows SMTC

This means TunesLayer is **not bound** by Spotify's Developer Terms of Service.

### Monetization
Since TunesLayer doesn't use the Spotify API, all features can be legally monetized:
- Premium themes
- Analytics features
- Discord/OBS integrations

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting PRs.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Support

- [GitHub Issues](https://github.com/yourusername/TunesLayer/issues)
- [Discord Server](https://discord.gg/tuneslayer)

---

Made with ♪ for gamers who love music
