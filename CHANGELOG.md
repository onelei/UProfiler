# 更新日志

本文件记录 UProfiler 的版本变更。格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

英文版见 [CHANGELOG_EN.md](CHANGELOG_EN.md)。

## [未发布]

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

[1.1.0]: https://github.com/lemonframework/UProfiler/compare/v1.0.2...v1.1.0
[1.0.2]: https://github.com/lemonframework/UProfiler/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/lemonframework/UProfiler/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/lemonframework/UProfiler/releases/tag/v1.0.0
