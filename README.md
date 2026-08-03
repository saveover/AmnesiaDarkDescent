<div align="center">
  <img src="Assets/AppIcons/AppIcon-Dark.png"
       width="112"
       alt="SaveOver Application Logo">

# SaveOver for Amnesia: The Dark Descent

**Free, open-source tools for editing game saves—built with care for the files behind your progress.**

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/saveover/AmnesiaDarkDescent/release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/saveover/AmnesiaDarkDescent)](https://github.com/saveover/AmnesiaDarkDescent/releases/latest)
[![GitHub License](https://img.shields.io/github/license/saveover/AmnesiaDarkDescent)](LICENSE.txt)
[![Windows](https://img.shields.io/badge/platform-Windows-0078D4?logo=windows)](#requirements)
[![Discord](https://img.shields.io/discord/1529883189861158923?logo=discord&logoColor=white&color=%235865F2)](https://discord.gg/nzQSeGcta8)

</div>

This free, open-source save editor for **Amnesia: The Dark Descent** on Windows allows you to 
edit your character's stats and inventory through a modern graphical interface. 
Automatic backups protect your original save files.

> [!IMPORTANT]
> SaveOver is an independent, unofficial project. It is not affiliated with,
> endorsed by, or supported by Frictional Games.

## Features

- Modify character health, sanity, oil level, and tinderboxes.
- Creates a timestamped backup outside the Steam Cloud save folder before each write.
- Stages changes in a temporary file before replacing the original.
- Automatic dark mode support! 🌚
- Run as an x86, x64, or ARM64 application.

## Quick start

1. Download the appropriate archive for your operating system from the
   [latest release](https://github.com/saveover/AmnesiaDarkDescent/releases/latest):
   - `win-x64` for most Windows computers.
   - `win-arm64` for Windows on ARM.
   - `win-x86` only for 32-bit Windows.
2. Extract all files from the ZIP archive.
3. Run `SaveOver for Amnesia The Dark Descent.exe`.
4. Select '**Load save file**' and open the relevant `.sav` file.
5. Use the navigation pages to make your desired changes.
6. Return to '**Home**' and select '**Save file**'.

Amnesia: The Dark Descent normally stores saves in:

```text
%USERPROFILE%\Documents\Amnesia\Main
```

Before modifying a save, SaveOver creates a timestamped backup in its own local application-data
folder. Keeping backups outside the game directory prevents Steam Cloud from treating them as
game saves. The Settings page lets you choose, open or reset the backup folder and select how many
backups to retain per save file. The app also confirms before overwriting a save unless you turn
that confirmation off in the dialog or Settings.

SaveOver writes small rolling diagnostic logs. If something goes wrong, open
**Settings > Diagnostics > Application logs** and attach the newest `.log` file to the bug report.
Logs include application and error details but never save contents or local file paths.

## Installation

### Portable release

The portable release is self-contained and includes both .NET and the Windows App SDK, 
eliminating the need for additional runtime installation. Download, extract, and run 
the application.

Because releases are unsigned, Windows may display a Microsoft Defender SmartScreen 
warning. This is expected.

### MSIX

MSIX packages are produced for Microsoft Store submission. These are currently unavailable.

## Requirements

- Windows 10 version 1809 (build 17763) or later, or Windows 11.
- An x86, x64, or ARM64 processor matching the downloaded release.
- A game save file.

## Documentation and support

- [Support and troubleshooting](.github/SUPPORT.md)
- [Contributing guide](.github/CONTRIBUTING.md)
- [Code of Conduct](.github/CODE_OF_CONDUCT.md)
- [Release history](https://github.com/saveover/AmnesiaDarkDescent/releases)
- [Issue tracker](https://github.com/saveover/AmnesiaDarkDescent/issues)
