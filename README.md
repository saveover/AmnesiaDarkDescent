# SaveOver for Amnesia: The Dark Descent

A Windows 10/11 save editor for Amnesia: The Dark Descent, built with WinUI 3 and .NET 10.

## Supported edits

- Health
- Sanity
- Lamp oil
- Tinderboxes

The editor locates the exact `cLuxPlayer_SaveData` / `mPlayer` object, preserves unknown XML content and ordering, creates a timestamped backup, and atomically replaces the original `.sav` file.

## Build

```powershell
dotnet build SaveOver.AmnesiaDarkDescent.csproj -c Debug-Unpackaged -p:Platform=x64
```

Use a disposable copy of a save when manually validating edits. Amnesia saves are normally under `%USERPROFILE%\Documents\Amnesia\Main` (including redirected Documents folders such as OneDrive).
