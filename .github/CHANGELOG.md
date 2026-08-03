# Changelog

All notable changes to **SaveOver for Amnesia: The Dark Descent** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/2.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-08-03
### Added

- Added automated pull-request validation with x64 packaged and unpackaged builds.
- Added release automation for self-contained x86, x64, and ARM64 portable downloads and Microsoft Store packages.
- Added draft GitHub release creation with architecture-specific archives.
- Added release-asset scanning with VirusTotal.
- Added a comprehensive contribution guide and Keep a Changelog-compatible changelog.
- Added expanded documentation covering installation, requirements, backups, diagnostics, support, and troubleshooting.

## [1.0.0] - 2026-08-03
### Added

- Introduced a Windows 10/11 save editor for *Amnesia: The Dark Descent*.
- Added editing for health, sanity, lamp oil, and tinderboxes, including Min and Max shortcuts.
- Added file browsing and drag-and-drop support for `.sav` files.
- Added preservation-safe saving with validation, timestamped backups, and atomic file replacement.
- Added configurable backup retention, save confirmations, themes, navigation styles, sounds, and last-save reopening.
- Added privacy-conscious application logs and links for reporting issues and supporting the project.

### Changed

- Expanded the supported tinderbox range to the full non-negative 32-bit integer range.
- Improved the Home page with clearer safety guidance, save-folder actions, and project links.

### Fixed

- Prevented saves modified by another application after opening from being overwritten.
- Corrected out-of-range player values when loading and clearly reported each adjustment.
- Improved error handling and feedback for file operations, folder access, clipboard actions, and opening the issue tracker.

[Unreleased]: https://github.com/saveover/AmnesiaDarkDescent/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/saveover/AmnesiaDarkDescent/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/saveover/AmnesiaDarkDescent/releases/tag/v1.0.0
