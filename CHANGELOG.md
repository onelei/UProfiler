# 更新日志

本文件记录 UProfiler 的版本变更。格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

英文版见 [CHANGELOG_EN.md](CHANGELOG_EN.md)。

## [未发布]

## [1.1.2] - 2026-06-13

### 新增

- 报告页 UWA 风格左侧页签与右侧分 panel 布局（性能概览、模块耗时、资源管理、卡顿点等）
- Unity 扩展数据上传：`moduleTime_`、`hardwareInfo_`、`threadStack_`、`moduleFuncStack_`、`briefAiDiagnosis_`、`gpuBandwidth_`、`luaMemory_`、`resourceManagement_`
- `ModuleTimeSampler`：基于 Unity ProfilerRecorder 的模块耗时采样
- `HardwareInfoSampler`：CPU 频率、网络流量等硬件信息采样
- `ResourceManagementAutoSampler`：自动检测 GameObject / AssetBundle / 资源对象生命周期并生成资源管理事件流
- `ResourceEventTracker`：Resources / AssetBundle 手动埋点 API
- `LuaMemoryProvider` / `LuaMemoryAutoProbe`：Lua 内存采集框架（xLua 等需注册）
- `CustomDataTracker`：自定义面板 / 函数组 / 变量 / 代码段数据
- 场景信息采集（`sceneInfo_`）与报告场景管理页签
- 卡顿点分析（JankAnalyzer）优化

### 变更

- 服务端报告数据加载与模块函数堆栈补全（`funcAnalysis_` / Profiler 采样降级）
- 资源管理、Lua 内存、模块性能等报告区块对齐 UWA 展示
- 图表帧选竖线辅助系列不再出现在图例与 tooltip 中
- 静态资源缓存版本更新为 `112`
- UWA 参考材料移至 `uwa/` 并加入 `.gitignore`

### 修复

- `sceneInfo_` 有数据时仍显示「请上传」的提示逻辑
- Unity 端 `ModuleTimeSampler` Unity 2020.1 编译兼容、`UProfilerHost` FTP 回调与 Android 变量名等问题

## [1.1.1] - 2026-06-12

### 变更

- 优化报告页左侧页签切换性能：缓存 panel / sidebar DOM 引用，消除重复的 `activatePanel` 调用
- 图表改为分批懒加载（首个立即渲染，其余 `requestIdleCallback` 异步加载），切换时仅 resize 当前 panel 内图表
- 页签切换改为即时滚动；隐藏 panel 使用 `content-visibility: hidden` 降低浏览器渲染开销
- 页面加载时预加载 ECharts；静态资源缓存版本更新为 `111`

## [1.1.0] - 2026-06-09

### 新增

- 飞书 OAuth 登录（`/login`、`/auth/feishu`）
- 账户设置页：个人信息表单、账户资料、右上角头像下拉菜单
- 认证与权限配置：`auth.json`、`auth.example.json`，支持环境变量覆盖
- 启动前认证检查：`check-auth.ps1`，`start.bat` 集成四步启动流程
- 用户数据本地存储（`users.json`）
- 版本号文件 `VERSION` 与更新日志 `CHANGELOG.md`
- `scripts/sync-changelog.ps1`：从 git 提交同步版本号与变更记录
- README 默认改为中文，英文文档迁至 `README_EN.md`

### 变更

- 报告服务器静态资源缓存版本与项目版本对齐
- 服务器启动时输出当前版本号

### 文档

- README 补充飞书登录、认证配置与 API 端点说明

## [1.0.2] - 2026-06-09

### 变更

- 更新总体性能趋势图的显示（`09cc2d4`）

## [1.0.1] - 2026-06-05

### 变更

- 整理代码结构（`c6a090c`）

## [1.0.0] - 2026-06-04

### 新增

- 初始发布：Unity 运行时性能采集与本地 Web 报告服务（`c503d79`）
- Unity UPM 包 `com.lemonframework.uprofiler`：帧率、内存、渲染、日志、设备信息采集
- ASP.NET Core 8 报告服务器：文件上传、HTML 报告生成、项目门户
- 项目门户：首页、项目详情、总体性能分析、报告列表与趋势图
- 单次性能报告页：图表、诊断、模块分析（ECharts）
- 可选 Samples：File Upload、Method Inject、Android Interact
- IL Hook 函数性能分析（Editor 注入）
- Android 真机 PSS / 功耗采集
- 启动脚本 `start.bat` / `stop.bat`

[1.1.2]: https://github.com/lemonframework/UProfiler/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/lemonframework/UProfiler/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/lemonframework/UProfiler/compare/v1.0.2...v1.1.0
[1.0.2]: https://github.com/lemonframework/UProfiler/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/lemonframework/UProfiler/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/lemonframework/UProfiler/releases/tag/v1.0.0
