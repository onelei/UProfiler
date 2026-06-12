# Changelog

All notable changes to UProfiler are documented here. Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), versioning follows [Semantic Versioning](https://semver.org/).

Chinese version: [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]

## [1.1.1] - 2026-06-12

### Changed

- Improved report page sidebar tab switching: cached panel/sidebar DOM refs, removed duplicate `activatePanel` calls
- Charts load in batches (first chart immediately, rest via `requestIdleCallback`); only charts in the active panel are resized on switch
- Instant scroll on tab change; hidden panels use `content-visibility: hidden` to reduce rendering cost
- Preload ECharts on page load; static asset cache version bumped to `111`

## [1.1.0] - 2026-06-09

### Added

- Feishu (Lark) OAuth login (`/login`, `/auth/feishu`)
- Account settings: profile form, account info, top-right avatar dropdown
- Auth & permissions: `auth.json`, `auth.example.json`, environment variable overrides
- Pre-start auth validation: `check-auth.ps1`, integrated into `start.bat`
- Local user storage (`users.json`)
- `VERSION` file and `CHANGELOG.md`
- `scripts/sync-changelog.ps1`: sync version and changelog from git commits
- README defaults to Chinese; English moved to `README_EN.md`

### Changed

- Static asset cache version aligned with project version
- Server prints current version on startup

### Documentation

- README updated with Feishu login, auth config, and API endpoints

## [1.0.2] - 2026-06-09

### Changed

- Improved overall performance trend chart display (`09cc2d4`)

## [1.0.1] - 2026-06-05

### Changed

- Code cleanup and organization (`c6a090c`)

## [1.0.0] - 2026-06-04

### Added

- Initial release: Unity runtime profiling + local web report server (`c503d79`)
- Unity UPM package `com.lemonframework.uprofiler`: FPS, memory, rendering, logs, device metrics
- ASP.NET Core 8 report server: upload, HTML report generation, project portal
- Project portal: home, project detail, performance analysis, report list & trends
- Single-session report page: charts, diagnosis, module analysis (ECharts)
- Optional Samples: File Upload, Method Inject, Android Interact
- IL Hook function profiling (Editor injection)
- Android device PSS / power consumption
- `start.bat` / `stop.bat` scripts

[1.1.1]: https://github.com/lemonframework/UProfiler/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/lemonframework/UProfiler/compare/v1.0.2...v1.1.0
[1.0.2]: https://github.com/lemonframework/UProfiler/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/lemonframework/UProfiler/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/lemonframework/UProfiler/releases/tag/v1.0.0
