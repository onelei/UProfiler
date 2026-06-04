<p align="right">
  <img src="https://img.shields.io/badge/lang-English-blue?style=for-the-badge" alt="English">
  <a href="README_CN.md"><img src="https://img.shields.io/badge/lang-&#31616;&#20307;&#20013;&#25991;-lightgrey?style=for-the-badge" alt="Simplified Chinese"></a>
</p>

# UProfiler

Unity runtime performance profiler with a local web report server - a self-hosted workflow similar to UWA-style performance analysis.

The Unity client collects frame rate, memory, rendering, logs, and device metrics, uploads them over HTTP to a local server, and auto-generates visual HTML reports plus a project portal.

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
    Services/                 # Report generation, portal, indexing
    wwwroot/                  # CSS / JS (includes ECharts)
    start.bat                 # Start script
    stop.bat                  # Stop script
  UProfiler.sln                 # Solution file
```

## Requirements

- Unity **2022.3 LTS** (project version 2022.3.62f3)
- **.NET SDK 8+** (for the report server)

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

Derived URLs (do **not** edit manually ? they follow `IP` and `Port`):

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
| `start.bat` | Edit `set PORT=8080` at the top of `UProfiler-Server/start.bat` |
| Command line | `dotnet run --project UProfiler-Server.csproj -c Release -- --port 8080` |
| Environment variable | `MONITOR_TOOL_PORT=8080` |

If the requested port is busy, the server tries the next port (+1 ? +10) and prints a reminder to update `Config.Port` in Unity.

### Deployment scenarios

| Scenario | `Config.IP` | `Config.Port` | Notes |
|----------|-------------|---------------|-------|
| Unity Editor on same PC | `localhost` | `8080` | Default ? no changes needed |
| Android / iOS device on LAN | Your PC's LAN IP | `8080` | Copy the `Network:` address from server console; **do not** use `localhost` |
| Custom port | `localhost` or LAN IP | e.g. `9090` | Change `PORT` in `start.bat` **and** `Config.Port` together |
| Remote server | Server IP or domain | Server port | Ensure firewall allows inbound TCP on that port |

### Deployment checklist

1. **Build & start the server**
   ```bat
   cd UProfiler-Server
   start.bat
   ```
2. **Note the port** shown in the console (default `8080`).
3. **Edit `Config.cs`** ? set `IP` and `Port` to match the server.
4. **Open firewall** (LAN/device only) ? allow inbound TCP on the server port.
5. **Run Unity** ? use `UProfiler.prefab` or `UProfilerSample.unity`, start/stop monitoring.
6. **Open reports** ? browser: `http://<IP>:<Port>/`

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

Listens on port **8080** by default. If the port is busy, the server tries +1 through +10 and prints a message to update `Config.Port` accordingly.

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
| `Program.cs` | Routes: `TestHandler.ashx`, `ReceiveDataHandler.ashx`, portal & report pages |
| `Services/UploadIndex.cs` | Upload file index |
| `Services/ReportGenerator.cs` | Parse data and generate HTML reports |
| `Services/ReportHtmlBuilder.cs` | Single-session report page |
| `Services/PortalHtmlBuilder.cs` | Project portal & performance pages |
| `Services/ProjectCatalog.cs` | Aggregate projects by PackageName |
| `wwwroot/css/` | `portal.css`, `report.css` |
| `wwwroot/js/` | `portal-trend.js`, `report.js`, ECharts |

Runtime directories (auto-created under the output folder):

- `uploads/` - raw uploaded data
- `reports/` - generated HTML reports
- `logs/` - server logs

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/TestHandler.ashx` | POST | Receive multipart file uploads |
| `/ReceiveDataHandler.ashx` | GET | Trigger report generation |
| `/` | GET | Project portal home |
| `/project/{package}/` | GET | Project detail |
| `/project/{package}/performance` | GET | Overall performance analysis |
| `/report_{session}.html` | GET | Single test report |

## License

TBD
