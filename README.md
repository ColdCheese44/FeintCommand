# FeintCommand

FeintCommand is the Windows command center for FeintAI applications. The current release is a native launcher for FeintTrade, FeintSupplyCo, FeintSignal, and future Feint programs. Each program profile can expose multiple launch targets, so one card can start a desktop app, backend, frontend, script, dashboard URL, or project folder.

## Current capabilities

- Dashboard inspired by the FeintSignal control panel
- Starter profiles for FeintTrade, FeintSupplyCo, and FeintSignal
- Add, edit, and remove program profiles without changing code
- Multiple launch targets per program
- Launch `.exe`, `.bat`, `.cmd`, `.ps1`, shortcuts, folders, and HTTP/HTTPS URLs
- Working-directory and command-argument support
- Running-process status for processes started by the current session
- Persistent dark, light, and system themes
- JSON configuration stored outside the installation directory
- Keyboard shortcuts and accessible control names

## Requirements

- Windows 10 or Windows 11
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) for framework-dependent builds
- .NET 10 SDK to build from source

## Build and run

```powershell
dotnet build .\FeintCommand.csproj
dotnet run --project .\FeintCommand.csproj
```

Create a self-contained Windows x64 build:

```powershell
dotnet publish .\FeintCommand.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\artifacts\publish\win-x64
```

## Configure programs

Open **Programs**, choose **Edit**, and set one or more launch targets. A target contains:

| Field | Purpose |
| --- | --- |
| Button label | The action shown on the program card |
| Command | Executable, script, shortcut, directory, or web URL |
| Arguments | Optional command-line arguments |
| Working directory | Optional directory used when the process starts |

The configuration is saved to:

```text
%LOCALAPPDATA%\FeintAI\FeintCommand\launcher.json
```

The launcher writes configuration changes atomically. If the JSON is invalid, it preserves the invalid file with a timestamped backup and restores starter profiles.

## Project structure

```text
Models/      Program, target, configuration, and activity data
Services/    Configuration, process launching, and theme behavior
Styles/      Shared WPF control styles
Themes/      Dark and light color resources
```

## Direction

The launcher keeps profile storage, process execution, and UI composition separate. That boundary supports the next stage of FeintCommand: replacing selected external launch targets with in-process Feint modules while retaining the same dashboard and navigation model.

Planned milestones:

1. Automatic discovery of installed Feint applications and their manifests.
2. Health checks, logs, and lifecycle controls for multi-process Feint stacks.
3. A module contract for hosting shared Feint experiences inside FeintCommand.
4. Unified identity, settings, notifications, and cross-program data services.
5. Installer, update channel, and signed release packaging.
