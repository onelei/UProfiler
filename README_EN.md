<p align="right">
  <img src="https://img.shields.io/badge/version-1.1.2-blue?style=for-the-badge" alt="v1.1.2">
  <a href="CHANGELOG_EN.md"><img src="https://img.shields.io/badge/changelog-Changelog-lightgrey?style=for-the-badge" alt="Changelog"></a>
  <a href="README.md"><img src="https://img.shields.io/badge/lang-&#31616;&#20307;&#20013;&#25991;-lightgrey?style=for-the-badge" alt="Simplified Chinese"></a>
  <img src="https://img.shields.io/badge/lang-English-blue?style=for-the-badge" alt="English">
</p>

# UProfiler

Unity runtime performance profiler with a local web report server - a self-hosted workflow similar to UWA-style performance analysis.

The Unity client collects frame rate, memory, rendering, logs, and device metrics, uploads them over HTTP to a local server, and auto-generates visual HTML reports plus a project portal. The server supports **Feishu (Lark) login**, **account settings**, and configurable access control.

## Screenshots

| Portal Home | Project Overview |
|:---:|:---:|
| ![Portal home](docs/screenshots/01-portal-home.png) | ![Project overview](docs/screenshots/02-project-overview.png) |

| Performance Analysis | Report Detail |
|:---:|:---:|
| ![Performance analysis](docs/screenshots/03-performance-analysis.png) | ![Report detail](docs/screenshots/04-performance-report-detail.png) |

## Project Structure

```
UProfiler/
  docs/
    screenshots/              # UI screenshots
  UProfiler-Unity/              # Unity 2022.3 host project
    Assets/                   # Samples imported via Package Manager
    Packages/
      com.lemonframework.uprofiler/   # Core UPM package
      com.unity.nuget.mono-cecil/     # IL Hook dependency
    ProjectSettings/
  UProfiler-Server/             # ASP.NET Core 8 report server
    Program.cs                # Entry point & routes
    Models/                   # Data models
    Services/                 # Report generation, portal, auth
    wwwroot/                  # CSS / JS (includes ECharts)
    auth.json                 # Auth config (edit on deploy)
    auth.example.json         # Auth config template
    check-auth.ps1            # Pre-start auth validation
    start.bat                 # Start script
    stop.bat                  # Stop script
  UProfiler.sln                 # Solution file
```

## Requirements

- Unity **2022.3 LTS** (project version 2022.3.62f3)
- **.NET SDK 8+** (for the report server)
- (Optional) Feishu Open Platform self-built app (for login)

## Deployment & Configuration

UProfiler has two parts: **Unity client** (uploads data) and **report server** (receives data and generates reports). Both must use the **same host and port**.

### Configuration file (Unity)

All upload/report URLs are built from one file:

```
UProfiler-Unity/Packages/com.lemonframework.uprofiler/Runtime/Scripts/Core/Config.cs
```

If you embed the package in your own project, edit `Config.cs` under your project's `Packages/com.lemonframework.uprofiler/` path.

| Field | Default | Description |
|-------|---------|-------------|
| `IP` | `"localhost"` | Report server host. Used for file upload, report links, and callbacks. |
| `Port` | `8080` | Report server port. Must match the running server. |

```csharp
public static string IP = "localhost";
public static int Port = 8080;
```

Derived URLs (do **not** edit manually - they follow `IP` and `Port`):

| Property | Purpose |
|----------|---------|
| `BaseUrl` | `http://{IP}:{Port}` |
| `PostFileUrl` | Upload endpoint (`/TestHandler.ashx`) |
| `ReportRecordUpdateRequestUrl` | Trigger report generation (`/ReceiveDataHandler.ashx`) |
| `ReportUrl` | Portal base URL |

### Report server port

The server listens on **all network interfaces**. On startup it prints:

```
Local:   http://localhost:8080/
Network: http://192.168.x.x:8080/    # your LAN IP, if available
```

Configure the server port in any of these ways (priority: CLI > env > default):

| Method | Example |
|--------|---------|
| `start.bat` | `start.bat 8080` or edit `set PORT=8080` in the script |
| Command line | `dotnet run --project UProfiler-Server.csproj -c Release -- --port 8080` |
| Environment variable | `MONITOR_TOOL_PORT=8080` |

If the requested port is busy, the server tries the next port (+1 to +10) and prints a reminder to update `Config.Port` in Unity.

### Authentication & Feishu login (optional)

By default, login is **disabled** (`enabled: false` in `auth.json`), suitable for local debugging. When enabled, the top-right avatar supports Feishu OAuth and account settings.

Config file: `UProfiler-Server/auth.json` (auto-copied from `auth.example.json` on first start if missing)

```json
{
  "enabled": false,
  "requireAuthForView": true,
  "requireAuthForUpload": false,
  "sessionDays": 7,
  "sessionSecret": "change-to-a-random-string",
  "feishu": {
    "appId": "",
    "appSecret": "",
    "redirectUri": "http://localhost:8080/auth/feishu/callback"
  },
  "adminOpenIds": []
}
```

| Field | Description |
|-------|-------------|
| `enabled` | Enable auth. When `false`, all pages are public and no login UI is shown |
| `requireAuthForView` | Require login to view portal/reports |
| `requireAuthForUpload` | Require login for Unity upload APIs (keep `false` for LAN debugging) |
| `sessionSecret` | Session signing key - change before production |
| `feishu.appId` / `appSecret` | Feishu Open Platform app credentials |
| `feishu.redirectUri` | OAuth callback URL - must match Feishu app security settings |
| `adminOpenIds` | Feishu Open IDs with `admin` role |

Environment variable overrides (for CI / containers):

| Variable | Description |
|----------|-------------|
| `UPROFILER_AUTH_ENABLED` | Enable auth |
| `UPROFILER_AUTH_REQUIRE_VIEW` | Require login to view |
| `UPROFILER_AUTH_REQUIRE_UPLOAD` | Require login to upload |
| `UPROFILER_FEISHU_APP_ID` | Feishu App ID |
| `UPROFILER_FEISHU_APP_SECRET` | Feishu App Secret |
| `UPROFILER_FEISHU_REDIRECT_URI` | OAuth callback URL |
| `UPROFILER_AUTH_SECRET` | Session signing key |
| `UPROFILER_ADMIN_OPEN_IDS` | Admin Open IDs (comma-separated) |

#### Feishu app setup

1. Create a **self-built app** on [Feishu Open Platform](https://open.feishu.cn/)
2. Copy **App ID** and **App Secret** from Credentials
3. Add redirect URL under Security Settings: `http://<host>:<port>/auth/feishu/callback`
4. Enable permissions to read basic user info
5. Fill credentials in `auth.json` and set `"enabled": true`
6. Run `start.bat` - it validates auth config before starting

`start.bat` runs `check-auth.ps1` first. If `enabled: true` but Feishu credentials are missing, startup is blocked with hints.

User data is stored in `users.json` under the server output directory.

### Deployment scenarios

| Scenario | `Config.IP` | `Config.Port` | Notes |
|----------|-------------|---------------|-------|
| Unity Editor on same PC | `localhost` | `8080` | Default - no changes needed |
| Android / iOS device on LAN | Your PC's LAN IP | `8080` | Use `Network:` address from server console; **do not** use `localhost` |
| Custom port | `localhost` or LAN IP | e.g. `9090` | Change `PORT` in `start.bat` **and** `Config.Port` together |
| Remote server + Feishu login | Server IP or domain | Server port | Open firewall; set `redirectUri` to public URL |

### Deployment checklist

1. **Build & start the server**
   ```bat
   cd UProfiler-Server
   start.bat
   ```
2. **Note the port** and auth status from the console (default `8080`).
3. **Edit `Config.cs`** - set `IP` and `Port` to match the server.
4. **(Optional) Configure `auth.json`** - enable Feishu login and access control.
5. **Open firewall** (LAN/device only) - allow inbound TCP on the server port.
6. **Run Unity** - use `UProfiler.prefab` or `UProfilerSample.unity`, start/stop monitoring.
7. **Open reports** - browser: `http://<IP>:<Port>/`

### Android / device notes

- Set `Config.IP` to the **PC running UProfiler-Server**, not the device itself.
- Phone and PC must be on the same network.
- Unity uses plain HTTP; the Editor project auto-enables `InsecureHttpOption.AlwaysAllowed` via `AllowInsecureHttpSetting.cs`.

## Quick Start

### 1. Configure Unity upload address

See [Deployment & Configuration](#deployment--configuration) for full details. For local debugging, defaults in `Config.cs` are enough:

```csharp
public static string IP = "localhost";
public static int Port = 8080;
```

### 2. Start the report server

Start the server **before** running Unity:

```bat
cd UProfiler-Server
start.bat
```

Listens on port **8080** by default. `start.bat` checks port, validates auth config, builds, then starts.

Close the window or press `Ctrl+C` to stop. You can also run `stop.bat` to free the port.

### 3. Collect data in Unity

1. Open the `UProfiler-Unity` project in Unity Hub
2. Open the built-in scene `Packages/com.lemonframework.uprofiler/Runtime/Scenes/UProfilerSample.unity`, or place `UProfiler.prefab` in your scene
3. Play and click **Start Monitoring** / **Stop Monitoring**
4. After stopping, data is uploaded automatically and a report is generated

Use the Editor menu **UProfiler > Download** to open the local download folder.

### 4. View reports

Open `http://<IP>:8080/` in a browser for the project portal, or go directly to a session report:

```
http://<IP>:8080/report_{TestTime}.html
```

`TestTime` is the session timestamp, e.g. `2026_06_04_10_34_06`.

With auth enabled:

- Login: `http://<IP>:<Port>/login`
- Account settings: `http://<IP>:<Port>/account/profile`

## Data Flow

```
Unity (UProfilerHost)
  -> local .txt / .data files
  -> POST /TestHandler.ashx
  -> uploads/{session}/
  -> GET /ReceiveDataHandler.ashx
  -> reports/report_{session}.html
  -> browser portal / report page
```

## Unity Package Layout

| Path | Description |
|------|-------------|
| `Runtime/Scripts/Core` | Core logic: config, collection, upload, hook |
| `Runtime/Scripts/Components` | Runtime components: `UProfilerHost`, HUD, Android proxy |
| `Runtime/Scenes` | Built-in sample scene `UProfilerSample.unity` |
| `Runtime/Prefabs` | `UProfiler.prefab` |
| `Runtime/Plugins` | SharpZipLib, Android AAR |
| `Editor` | Menu items, IL hook injection, HTTP settings |
| `Samples~` | Optional samples (import via Package Manager) |

### Optional Samples

Import samples from **LemonFramework UProfiler** in Package Manager:

| Sample | Description |
|--------|-------------|
| File Upload | HTTP file upload demo |
| Method Inject | IL hook & function profiling |
| Android Interact | Unity-Android JNI interaction demo |

### Collected Metrics

- Frame rate (FPS)
- Unity runtime logs
- Device info
- Resource memory distribution (Texture, Mesh, Material, etc.)
- Rendering stats (DrawCall, SetPassCall, vertices/triangles)
- Function profiling (requires IL hook injection in Editor)
- Android PSS / power consumption (on device)

## Report Server Layout

| Path | Description |
|------|-------------|
| `Program.cs` | Routes: upload, reports, portal, auth |
| `Services/UploadIndex.cs` | Upload file index |
| `Services/ReportGenerator.cs` | Parse data and generate HTML reports |
| `Services/ReportHtmlBuilder.cs` | Single-session report page |
| `Services/PortalHtmlBuilder.cs` | Project portal & performance pages |
| `Services/AccountHtmlBuilder.cs` | Login & account settings pages |
| `Services/FeishuOAuthService.cs` | Feishu OAuth |
| `Services/UserStore.cs` | User profile storage |
| `Services/ProjectCatalog.cs` | Aggregate projects by PackageName |
| `wwwroot/css/` | `portal.css`, `report.css`, `account.css` |
| `wwwroot/js/` | `portal-trend.js`, `report.js`, `account.js`, ECharts |

Runtime directories (auto-created under the output folder):

- `uploads/` - raw uploaded data
- `reports/` - generated HTML reports
- `logs/` - server logs
- `users.json` - user accounts (when auth is enabled)

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/TestHandler.ashx` | POST | Receive multipart file uploads |
| `/ReceiveDataHandler.ashx` | GET | Trigger report generation |
| `/` | GET | Project portal home |
| `/project/{package}/` | GET | Project detail |
| `/project/{package}/performance` | GET | Overall performance analysis |
| `/report_{session}.html` | GET | Single test report |
| `/login` | GET | Login page (Feishu) |
| `/auth/feishu` | GET | Redirect to Feishu authorization |
| `/auth/feishu/callback` | GET | Feishu OAuth callback |
| `/auth/logout` | POST | Log out |
| `/account/profile` | GET | Personal info settings |
| `/account/settings` | GET | Account profile |
| `/api/account/profile` | POST | Save personal info |

## Version & Changelog

Current version: **1.1.2** (see `VERSION` at repo root)

- Changelog (Chinese): [CHANGELOG.md](CHANGELOG.md)
- Changelog (English): [CHANGELOG_EN.md](CHANGELOG_EN.md)

Sync version across the repo:

```powershell
.\scripts\sync-changelog.ps1 -ListCommits    # list git commits
.\scripts\sync-changelog.ps1 -Bump patch     # bump patch (1.1.0 -> 1.1.1)
```

The script updates `VERSION`, `package.json`, server assembly version, Unity `UProfilerVersion`, and README badges. Add the matching entry to CHANGELOG manually.

## License

TBD


