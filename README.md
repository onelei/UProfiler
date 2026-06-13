<p align="right">
  <img src="https://img.shields.io/badge/version-1.1.2-blue?style=for-the-badge" alt="v1.1.2">
  <a href="CHANGELOG.md"><img src="https://img.shields.io/badge/changelog-更新日志-lightgrey?style=for-the-badge" alt="Changelog"></a>
  <img src="https://img.shields.io/badge/lang-简体中文-blue?style=for-the-badge" alt="简体中文">
  <a href="README_EN.md"><img src="https://img.shields.io/badge/lang-English-lightgrey?style=for-the-badge" alt="English"></a>
</p>

# UProfiler

Unity 运行时性能采集 + 本地 Web 报告服务，对标 UWA 类性能分析工作流。

Unity 端采集帧率、内存、渲染、日志、设备信息等数据，通过 HTTP 上传到本地服务器，自动生成可视化 HTML 报告与项目门户。服务器支持**飞书登录**、**账户设置**与可配置的访问权限。

## 界面预览

| 项目门户首页 | 项目详情与测试服务 |
|:---:|:---:|
| ![门户首页](docs/screenshots/01-portal-home.png) | ![项目概览](docs/screenshots/02-project-overview.png) |

| 总体性能分析 | 性能报告详情 |
|:---:|:---:|
| ![性能分析](docs/screenshots/03-performance-analysis.png) | ![报告详情](docs/screenshots/04-performance-report-detail.png) |

## 项目结构

```
UProfiler/
  docs/
    screenshots/              # 界面截图
  UProfiler-Unity/              # Unity 2022.3 宿主工程
    Assets/                   # Package Manager 导入的 Samples
    Packages/
      com.lemonframework.uprofiler/   # 核心 UPM 包
      com.unity.nuget.mono-cecil/     # IL Hook 依赖
    ProjectSettings/
  UProfiler-Server/             # ASP.NET Core 8 报告服务器
    Program.cs                # 入口与路由
    Models/                   # 数据模型
    Services/                 # 报告生成、门户、认证
    wwwroot/                  # CSS / JS（含 ECharts）
    auth.json                 # 认证配置（部署时编辑）
    auth.example.json         # 认证配置模板
    check-auth.ps1            # 启动前认证检查脚本
    start.bat                 # 启动脚本
    stop.bat                  # 停止脚本
  UProfiler.sln                 # 解决方案
```

## 环境要求

- Unity **2022.3 LTS**（工程版本 2022.3.62f3）
- **.NET SDK 8+**（运行报告服务器）
- （可选）飞书开放平台企业自建应用（启用登录时）

## 部署与配置

UProfiler 分为 **Unity 客户端**（采集并上传）和 **报告服务器**（接收数据并生成报告）两部分，两边的 **地址和端口必须一致**。

### 配置文件（Unity 端）

所有上传、报告相关 URL 均由此文件统一生成：

```
UProfiler-Unity/Packages/com.lemonframework.uprofiler/Runtime/Scripts/Core/Config.cs
```

若将包集成到自己的工程，请修改你工程内 `Packages/com.lemonframework.uprofiler/` 下的 `Config.cs`。

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `IP` | `"localhost"` | 报告服务器地址，用于上传、报告链接、回调等 |
| `Port` | `8080` | 报告服务器端口，须与服务器实际监听端口一致 |

```csharp
public static string IP = "localhost";
public static int Port = 8080;
```

以下属性由 `IP`、`Port` 自动拼接，**无需单独修改**：

| 属性 | 用途 |
|------|------|
| `BaseUrl` | `http://{IP}:{Port}` |
| `PostFileUrl` | 文件上传接口（`/TestHandler.ashx`） |
| `ReportRecordUpdateRequestUrl` | 触发报告生成（`/ReceiveDataHandler.ashx`） |
| `ReportUrl` | 门户基础地址 |

### 报告服务器端口

服务器监听**所有网卡**。启动后控制台会输出：

```
Local:   http://localhost:8080/
Network: http://192.168.x.x:8080/    # 本机局域网 IP（如有）
```

可通过以下方式配置端口（优先级：命令行 > 环境变量 > 默认值）：

| 方式 | 示例 |
|------|------|
| `start.bat` | `start.bat 8080` 或修改脚本内 `set PORT=8080` |
| 命令行 | `dotnet run --project UProfiler-Server.csproj -c Release -- --port 8080` |
| 环境变量 | `MONITOR_TOOL_PORT=8080` |

若请求端口被占用，服务器会自动尝试 +1～+10，并提示同步修改 Unity 端 `Config.Port`。

### 认证与飞书登录（可选）

默认**不强制登录**（`auth.json` 中 `enabled: false`），适合本机调试。启用后，右上角显示账户头像，支持飞书 OAuth 登录与账户设置页。

配置文件路径：`UProfiler-Server/auth.json`（首次启动可从 `auth.example.json` 自动复制）

```json
{
  "enabled": false,
  "requireAuthForView": true,
  "requireAuthForUpload": false,
  "sessionDays": 7,
  "sessionSecret": "请改为随机字符串",
  "feishu": {
    "appId": "",
    "appSecret": "",
    "redirectUri": "http://localhost:8080/auth/feishu/callback"
  },
  "adminOpenIds": []
}
```

| 字段 | 说明 |
|------|------|
| `enabled` | 是否启用认证。`false` 时所有人可访问，不显示登录入口 |
| `requireAuthForView` | 查看门户/报告是否需登录 |
| `requireAuthForUpload` | Unity 上传接口是否需登录（局域网调试建议 `false`） |
| `sessionSecret` | 会话签名密钥，生产环境务必修改 |
| `feishu.appId` / `appSecret` | 飞书开放平台企业自建应用凭证 |
| `feishu.redirectUri` | OAuth 回调地址，须与飞书应用「安全设置」中一致 |
| `adminOpenIds` | 管理员飞书 Open ID 列表，拥有 `admin` 角色 |

也可用环境变量覆盖（适合 CI / 容器部署）：

| 环境变量 | 说明 |
|----------|------|
| `UPROFILER_AUTH_ENABLED` | 是否启用认证 |
| `UPROFILER_AUTH_REQUIRE_VIEW` | 查看是否需登录 |
| `UPROFILER_AUTH_REQUIRE_UPLOAD` | 上传是否需登录 |
| `UPROFILER_FEISHU_APP_ID` | 飞书 App ID |
| `UPROFILER_FEISHU_APP_SECRET` | 飞书 App Secret |
| `UPROFILER_FEISHU_REDIRECT_URI` | OAuth 回调地址 |
| `UPROFILER_AUTH_SECRET` | 会话签名密钥 |
| `UPROFILER_ADMIN_OPEN_IDS` | 管理员 Open ID（逗号分隔） |

#### 飞书应用配置步骤

1. 登录 [飞书开放平台](https://open.feishu.cn/)，创建**企业自建应用**
2. 在「凭证与基础信息」获取 **App ID**、**App Secret**
3. 在「安全设置 → 重定向 URL」添加：`http://<服务器地址>:<端口>/auth/feishu/callback`
4. 开启获取用户基本信息的权限
5. 将凭证填入 `auth.json`，设置 `"enabled": true`
6. 运行 `start.bat` — 启动前会自动检查配置是否完整

`start.bat` 启动时会执行 `check-auth.ps1`：若 `enabled: true` 但飞书凭证未填写，将阻止启动并给出提示。

用户数据保存在服务器运行目录下的 `users.json`。

### 常见部署场景

| 场景 | `Config.IP` | `Config.Port` | 说明 |
|------|-------------|---------------|------|
| 本机 Unity Editor 调试 | `localhost` | `8080` | 默认配置，无需修改 |
| Android / iOS 真机（局域网） | 运行服务器的电脑局域网 IP | `8080` | 使用控制台 `Network:` 一行中的地址，**不能用** `localhost` |
| 自定义端口 | `localhost` 或局域网 IP | 如 `9090` | 同时修改 `start.bat` 的 `PORT` 与 `Config.Port` |
| 远程服务器 + 飞书登录 | 服务器 IP 或域名 | 服务器端口 | 防火墙放行端口；`redirectUri` 改为公网地址 |

### 部署步骤

1. **编译并启动服务器**
   ```bat
   cd UProfiler-Server
   start.bat
   ```
2. **确认控制台显示的端口**（默认 `8080`）及认证状态。
3. **修改 `Config.cs`** — 将 `IP`、`Port` 改为与服务器一致。
4. **（可选）配置 `auth.json`** — 启用飞书登录与权限控制。
5. **放行防火墙**（真机/局域网场景）— 允许入站 TCP 访问服务器端口。
6. **运行 Unity** — 使用 `UProfiler.prefab` 或 `UProfilerSample.unity`，开始/停止监控。
7. **查看报告** — 浏览器访问 `http://<IP>:<Port>/`

### 真机调试说明

- `Config.IP` 填**运行 UProfiler-Server 的电脑**地址，不是手机地址。
- 手机与电脑须在同一局域网。
- Unity 使用 HTTP 明文传输；Editor 工程会通过 `AllowInsecureHttpSetting.cs` 自动开启 `InsecureHttpOption.AlwaysAllowed`。

## 快速开始

### 1. 配置 Unity 上传地址

完整说明见 [部署与配置](#部署与配置)。本机 Editor 调试使用 `Config.cs` 默认值即可：

```csharp
public static string IP = "localhost";
public static int Port = 8080;
```

### 2. 启动报告服务器

建议在 Unity 运行**之前**启动服务器：

```bat
cd UProfiler-Server
start.bat
```

默认监听 **8080** 端口。启动脚本会依次：检查端口 → 检查认证配置 → 编译 → 启动服务。

关闭窗口或按 `Ctrl+C` 停止服务，也可运行 `stop.bat` 释放端口。

### 3. Unity 中采集数据

1. 用 Unity Hub 打开 `UProfiler-Unity` 工程
2. 打开内置场景 `Packages/com.lemonframework.uprofiler/Runtime/Scenes/UProfilerSample.unity`，或在场景中使用 `UProfiler.prefab`
3. 运行后点击 **开始监控** / **停止监控**
4. 停止后数据自动上传到服务器并生成报告

Editor 菜单 **UProfiler > Download** 可打开本地下载目录。

### 4. 查看报告

浏览器访问 `http://<IP>:8080/` 进入项目门户，或直接打开单次报告：

```
http://<IP>:8080/report_{TestTime}.html
```

`TestTime` 为会话时间戳，例如 `2026_06_04_10_34_06`。

启用认证后，还可访问：

- 登录页：`http://<IP>:<Port>/login`
- 账户设置：`http://<IP>:<Port>/account/profile`

## 数据流

```
Unity (UProfilerHost)
  -> 本地 .txt / .data 文件
  -> POST /TestHandler.ashx
  -> uploads/{session}/
  -> GET /ReceiveDataHandler.ashx
  -> reports/report_{session}.html
  -> 浏览器访问门户 / 报告页
```

## Unity 包结构

| 路径 | 说明 |
|------|------|
| `Runtime/Scripts/Core` | 核心逻辑：配置、采集、上传、Hook |
| `Runtime/Scripts/Components` | 运行时组件：`UProfilerHost`、HUD、Android 代理 |
| `Runtime/Scenes` | 内置示例场景 `UProfilerSample.unity` |
| `Runtime/Prefabs` | `UProfiler.prefab` 预制体 |
| `Runtime/Plugins` | SharpZipLib、Android AAR |
| `Editor` | 菜单项、IL Hook 注入、HTTP 设置 |
| `Samples~` | 可选示例（Package Manager 导入） |

### 可选 Samples

在 Package Manager 中导入 **LemonFramework UProfiler** 的 Samples：

| Sample | 说明 |
|--------|-------------|
| File Upload | HTTP 文件上传演示 |
| Method Inject | IL Hook 与函数性能分析 |
| Android Interact | Unity-Android JNI 交互演示 |

### 采集指标

- 帧率（FPS）
- Unity 运行日志
- 设备信息
- 资源内存分布（Texture、Mesh、Material 等）
- 渲染统计（DrawCall、SetPassCall、顶点/三角形）
- 函数性能（需 Editor 执行 IL Hook 注入）
- Android PSS / 功耗（真机）

## 报告服务器结构

| 路径 | 说明 |
|------|------|
| `Program.cs` | 路由：上传、报告、门户、认证 |
| `Services/UploadIndex.cs` | 上传文件索引 |
| `Services/ReportGenerator.cs` | 解析数据并生成 HTML 报告 |
| `Services/ReportHtmlBuilder.cs` | 单次报告页面 |
| `Services/PortalHtmlBuilder.cs` | 项目门户与性能分析页 |
| `Services/AccountHtmlBuilder.cs` | 登录页与账户设置页 |
| `Services/FeishuOAuthService.cs` | 飞书 OAuth |
| `Services/UserStore.cs` | 用户资料存储 |
| `Services/ProjectCatalog.cs` | 按 PackageName 聚合项目 |
| `wwwroot/css/` | `portal.css`、`report.css`、`account.css` |
| `wwwroot/js/` | `portal-trend.js`、`report.js`、`account.js`、ECharts |

运行时目录（由服务器自动创建，位于输出目录下）：

- `uploads/` - 上传的原始数据
- `reports/` - 生成的 HTML 报告
- `logs/` - 服务器日志
- `users.json` - 用户账户数据（启用认证后）

## API 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/TestHandler.ashx` | POST | 接收 multipart 文件上传 |
| `/ReceiveDataHandler.ashx` | GET | 触发报告生成 |
| `/` | GET | 项目门户首页 |
| `/project/{package}/` | GET | 项目详情 |
| `/project/{package}/performance` | GET | 总体性能分析 |
| `/report_{session}.html` | GET | 单次测试报告 |
| `/login` | GET | 登录页（飞书） |
| `/auth/feishu` | GET | 跳转飞书授权 |
| `/auth/feishu/callback` | GET | 飞书 OAuth 回调 |
| `/auth/logout` | POST | 退出登录 |
| `/account/profile` | GET | 个人信息设置 |
| `/account/settings` | GET | 账户资料 |
| `/api/account/profile` | POST | 保存个人信息 |

## 版本与更新日志

当前版本：**1.1.2**（见根目录 `VERSION`）

- 中文更新日志：[CHANGELOG.md](CHANGELOG.md)
- English changelog: [CHANGELOG_EN.md](CHANGELOG_EN.md)

发版时可用脚本同步版本号：

```powershell
.\scripts\sync-changelog.ps1 -ListCommits    # 查看 git 提交
.\scripts\sync-changelog.ps1 -Bump patch     # 递增补丁版本（1.1.0 → 1.1.1）
```

脚本会自动更新 `VERSION`、`package.json`、服务器程序集版本、Unity `UProfilerVersion` 与 README 版本徽章；随后在 CHANGELOG 中补充对应条目即可。

## License

待定


