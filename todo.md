# UProfiler Unity 数据上传格式 & UWA 页面对照

对照 [UWA GOT Online 示例报告](https://www.uwa4d.com/u/got/perfanalysis.html/report?dataKey=20250521115551ncha0bb4727&project=1016&engine=1) 各页签右侧内容整理。

---

## 上传规则

- 文件名：`{prefix}_{yyyy_MM_dd_HH_mm_ss}.txt` 或 `.data`
- 会话键：`yyyy_MM_dd_HH_mm_ss`
- 解析入口：`ReportDataLoader.cs`

---

## 页签 UI 对照（UWA Playwright 逐页爬取 → UProfiler 实现）

> 数据源：UWA 示例 `dataKey=20250521115551ncha0bb4727`，2025-06-10 Playwright 逐页签爬取。

### 性能简报 `#brief`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 报告名称 / 测试时间 / 测试帧数 | `brief-meta` |
| 数据汇总 + 同档次排名 | KPI 网格 + 排名按钮（占位，需云端） |
| FPS/Jank/内存/功耗/温度 KPI + 优化任务队列 | `brief-kpi-grid` |
| FPS均值详情 + 仅显示优化项 | `brief-detail-head` + JS 过滤 |
| B类指标表（名称/行业水平/数值/历史趋势/优化项） | `brief-metrics-table` |

### 运行信息 `#basicinfo`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 总览（0-N帧）/ 指定帧 / 指定场景 | `scope-toolbar` |
| FPS / CPU每帧耗时 / Jank / PSS / 功率 / 温度 指标卡+迷你图 | `uwa-metric-grid` |
| 测试信息 + 设备信息表格 | `info-grid` |

### 场景概览 · 性能概览 `#scene-overview`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 场景性能概览 CPU耗时(ms) 堆叠柱图 | `sceneCpuBarChart`（`scene-cpu-bar`） |
| 场景表：场景名/帧数/begin/end/FPS/PSS/Mono/CPU均值峰值/Tri/DC | `uwa-scene-overview-table` |

### 场景概览 · 场景管理 `#scene-management`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 每帧耗时曲线（单位 ms，场景 markArea） | `sceneFrameTimeChart` |
| 帧提示（第N帧 场景名 · ms） | `sceneFrameHint` + JS |
| 场景表 + 场景详情按钮 | `uwa-scene-table` |

### GPU 分析 `#gpu-render` / `#gpu-bandwidth` / `#gpu-summary`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 总览工具条 + GPU Clocks 压力系数 | 指标卡 + DrawCall/三角面图 |
| GPU Bound 说明 + 相关指标 | `chart-legend` + KPI 卡片 |
| GPU Total Bandwidth 曲线 | 估算图（真实需 `gpuBandwidth_`） |
| 主要/次要指标汇总 | `#gpu-summary` 卡片组 |

### 模块耗时统计 `#module-time`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 模块占比预览饼图 | `modulePieChart` |
| 模块分类 / CPU耗时均值 / 推荐值 表 | `module-table` |
| 各模块 CPU 耗时堆叠图 | `moduleTimeChart` |

### 各线程 CPU 调用堆栈 `#thread-stack`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 线程总览 + 各线程 CPU 耗时条形图 | `threadStackChart`（模块均值近似） |
| 多线程火焰图 | 需 `threadStack_`；当前 MainThread 函数表 |

### 卡顿分析 `#jank-frames` / `#jank-func`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 卡顿帧汇总（全部/严重/GC/加载/其他） | `jank-summary-grid` |
| 卡顿帧函数列表（8列 UWA 表头） | `JankHotFunctionRow` 表格 |
| 重点函数分析 + GC/加载等 Tab | `jank-func-tabs` + 函数表 |

### 内存分析四页

| 页签 | UWA 右侧内容 | UProfiler 实现 |
|-----|-------------|----------------|
| `#memory-occupy` | PSS/Reserved 曲线 + 内存堆栈表 | 双图 + 堆栈表 |
| `#memory-resource` | 资源趋势 + 占比预览 + 类型表 | 柱图/饼图 + 推荐值列 |
| `#memory-mono` | Mono Reserved/Used 曲线 | `memory-mono` 图 |
| `#memory-lua` | LUA堆/Table/Function 多图 | 占位（需 `luaMemory_`） |

### 耗电量 / 温度 `#battery` / `#temperature`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 总览工具条 + 电量/功率/温度指标卡 | `uwa-metric-grid` |
| 趋势曲线 | `power` / `temperature` 图表 |

### 自定义模块 `#custom-*`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| 预设面板列表 + 新建面板 | `custom-panel-chip` |
| 自定义指标曲线 | 占位 + FPS 预览 |

### 资源管理 `#resource-*`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| AB/Resources/Instantiate KPI | 卡片（需 `resourceManagement_`） |
| TOP 10 表（AB加载/资源加载/Instantiate） | `resource-top-grid` 空态 |
| 资源内存类型汇总 | `resMemoryDistribution_` 表格 |

### 运行日志 `#log`

| UWA 右侧内容 | UProfiler 实现 |
|-------------|----------------|
| All/Log/Warning/Error/Exception 筛选 | `log-filter-bar` + JS |
| 帧数/场景/Log内容 + 导出 | 分批加载 + 分级着色 |

---

## 页签 UI 对照（字段级 · Unity 数据来源）

### 性能简报 `#brief`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 评级 / 趋势 | KPI 卡片 | `frameRate_` + 诊断引擎 |
| 数据汇总（FPS/Jank/内存/功耗/温度） | `brief-kpi-grid` | 同上 + `powerConsume_` + `pssMemoryInfo_` |
| FPS 均值详情 + B 类指标表 | `brief-metrics-table` | `ModuleTimeBuilder` 估算 |

### 运行信息 `#basicinfo`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 测试信息 / 设备信息表格 | `info-grid` | `test_` + `device_` |

### 场景概览 · 性能概览 `#scene-overview`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| FPS / 耗时趋势图 | 双图表 | `frameRate_` |
| 场景分段表 | `uwa-scene-table` | `sceneInfo_`（无则合并为默认场景） |

### 场景概览 · 场景管理 `#scene-management`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| **每帧耗时曲线**（带场景色块） | `sceneFrameTimeChart` | `frameRate_` → ms = 1000/fps |
| 场景表（场景/起始帧/结束帧/总帧数/平均耗时/场景详情） | `uwa-scene-table` + 按钮 | **`sceneInfo_`（必需真实场景名）** |

#### sceneInfo_ 格式（Unity 需实现）

```json
{
  "segments": [
    {
      "sceneName": "0_MainMenu",
      "startFrame": 0,
      "endFrame": 600,
      "note": ""
    },
    {
      "sceneName": "00_mochuanlinju",
      "startFrame": 601,
      "endFrame": 5610,
      "note": ""
    }
  ]
}
```

Unity 采集建议：在 `SceneManager.sceneLoaded` / 自定义场景切换回调中记录 `frameIndex` 与 `scene.name`。

### GPU 分析 `#gpu-render` / `#gpu-bandwidth` / `#gpu-summary`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| DrawCall / 三角面趋势 | 图表 | `renderInfo_` |
| GPU 带宽 | 渲染压力估算图 | 降级；真实需 `gpuBandwidth_` |
| 指标汇总 KPI | 卡片 | `renderInfo_` |

#### gpuBandwidth_ 格式

```json
{
  "samples": [
    { "frameIndex": 100, "readBytes": 1234567, "writeBytes": 890123, "totalBytes": 2124680 }
  ]
}
```

### 总体性能趋势 `#trend` / `#module-time`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 帧率/内存/渲染/PSS/功耗趋势 | 多图表 | 已有上传文件 |
| 模块耗时饼图/堆叠图/模块详情 | 模块区 | 估算；真实需 `moduleTime_` |

#### moduleTime_ 格式

```json
{
  "x": [100, 200, 300],
  "series": {
    "logic": [10.2, 9.8, 11.0],
    "rendering": [3.1, 3.2, 3.0],
    "ui": [0.3, 0.2, 0.3],
    "sync": [1.2, 1.1, 1.0],
    "loading": [0.1, 0.0, 0.2],
    "physics": [0.5, 0.4, 0.5],
    "animation": [0.8, 0.7, 0.9],
    "particles": [0.05, 0.04, 0.06]
  }
}
```

### 各线程 CPU 调用堆栈 `#thread-stack`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 多线程火焰图/堆栈 | 函数表（降级） | **`threadStack_`** |

#### threadStack_ 格式

```json
[
  {
    "threadName": "Main Thread",
    "samples": [
      {
        "frameIndex": 100,
        "stack": [
          { "name": "Update", "selfMs": 2.1, "totalMs": 8.5 },
          { "name": "MyGame.Logic", "selfMs": 5.2, "totalMs": 5.2 }
        ]
      }
    ]
  }
]
```

### 卡顿分析 `#jank-frames` / `#jank-func`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| Jank/BigJank 统计 + 卡顿帧列表 | 卡片 + 表格 | `frameRate_` 自动检测 |
| 重点函数 | 表格 | `funcAnalysis_` |

### 内存分析 `#memory-occupy` / `#memory-resource` / `#memory-mono` / `#memory-lua`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| Unity 内存 / PSS 曲线 | 图表 | `uprofiler_` + `pssMemoryInfo_` |
| 资源类型内存表 + 饼图 | 表格 + 图表 | `resMemoryDistribution_` |
| Mono 堆趋势 | 图表 | `uprofiler_` |
| Lua 内存 | 占位 | **`luaMemory_`** |

#### luaMemory_ 格式

```json
{
  "samples": [
    { "frameIndex": 100, "luaMemoryKb": 20480.5 }
  ]
}
```

### 耗电量 `#battery` / 温度 `#temperature`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 功耗趋势 | 图表 | `powerConsume_`（Android） |
| CPU 温度趋势 | 图表 | `powerConsume_.CpuTemperate` |

### 自定义模块 `#custom-*`

| 页签 | 前缀 | 格式 |
|-----|------|------|
| 自定义面板 | `customDashboard_` | `{ "panels": [{ "title", "metrics": [{ "label", "unit", "values": [] }] }] }` |
| 自定义函数组 | `apiFuncs_` | `[{ "groupName", "functions": [{ "name", "avgMs", "calls" }] }]` |
| 自定义变量 | `apiInfo_` | `{ "samples": [{ "frameIndex", "vars": { "name": 1.0 } }] }` |
| 自定义代码段 | `apiCodeFrame_` | `{ "segments": [{ "name", "startFrame", "endFrame", "avgMs" }] }` |

### 资源管理 `#resource-*`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 汇总表 | 资源类型统计 | `resMemoryDistribution_` |
| AB 加载/资源加载/实例化事件 | 占位 | **`resourceManagement_`** |

#### resourceManagement_ 格式

```json
{
  "events": [
    {
      "frameIndex": 1200,
      "category": "AssetBundle",
      "action": "Load",
      "assetName": "ui/main.ab",
      "durationMs": 45.2,
      "sizeBytes": 5242880
    }
  ]
}
```

`category`: `AssetBundle` | `Resource` | `Object`  
`action`: `Load` | `Unload` | `Instantiate` | `Activate`

### 运行日志 `#log`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 分级日志列表 | `log-box` 分批加载 | `log_` |

### 截图 `#capture`

| UWA 内容 | 本地实现 | 数据来源 |
|---------|---------|---------|
| 采样点截图浮窗 | `capturePanel` | `captureFrame_*.zip` |

---

## 已实现文件前缀速查

| 前缀 | 说明 |
|-----|------|
| `test_` | 测试元信息 |
| `device_` | 设备信息 |
| `frameRate_` | 每帧 FPS |
| `uprofiler_` / `monitor_` | 内存采样 |
| `renderInfo_` | DrawCall/三角面 |
| `funcAnalysis_` | 函数耗时 |
| `powerConsume_` | 功耗/温度 |
| `pssMemoryInfo_` | Android PSS |
| `resMemoryDistribution_` | 资源内存分布 |
| `log_` | 运行日志 |
| `captureFrame_` | 截图 zip |

---

## Unity 实现优先级

1. **P0** `sceneInfo_` — 场景管理页真实场景表（UWA 核心）
2. **P0** `moduleTime_` — 模块耗时真实值
3. **P0** `threadStack_` — 线程堆栈
4. **P1** `resourceManagement_` — 资源事件流
5. **P2** `gpuBandwidth_` / `luaMemory_` / 自定义模块系列

---

## 参考链接

- [UWA 场景管理示例](https://www.uwa4d.com/u/got/perfanalysis.html/scenemanagement?dataKey=20250521115551ncha0bb4727&project=1016&engine=1)
- Unity 上传：`UProfilerHost.cs` → `UploadFile()`
- 服务端：`ReportDataLoader.cs` / `ReportSectionsBuilder.cs`

## UWA 深度爬取（Playwright 逐页签 · 含子页签/折叠/下拉）

> 数据源：UWA 示例 `dataKey=20250521115551ncha0bb4727`，2026-06-10 Playwright MCP 深度爬取。
> 共 **34** 个左侧页签；含 `defaultView` / `expandedView`（折叠展开后）/ `rightTabViews`（右侧子页签逐一点击）。

---

### 性能简报 `brief` → UProfiler `#brief`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/report?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**右侧子页签**
- GPU压力系数 36 %
- 渲染耗时均值 3.12 ms
- 逻辑代码耗时均值 3.74 ms
- 同步等待耗时均值 6.1 ms
- UI耗时均值 0.31 ms
- 物理耗时均值 0.47 ms
- 动画耗时均值 0.78 ms
- 粒子系统耗时均值 0.03 ms
- 加载耗时均值 0.08 ms

**折叠面板 (el-collapse)**
- GPU压力系数 36 %
- 渲染耗时均值 3.12 ms
- 逻辑代码耗时均值 3.74 ms
- 同步等待耗时均值 6.1 ms
- UI耗时均值 0.31 ms
- 物理耗时均值 0.47 ms
- 动画耗时均值 0.78 ms
- 粒子系统耗时均值 0.03 ms
- 加载耗时均值 0.08 ms

**表格列**
- 报告名称
- 测试时间
- 机型

**操作按钮**
- 同档次排名

#### 展开后视图（折叠/子页签点击后）
**右侧子页签**
- GPU压力系数 36 %
- 渲染耗时均值 3.12 ms
- 逻辑代码耗时均值 3.74 ms
- 同步等待耗时均值 6.1 ms
- UI耗时均值 0.31 ms
- 物理耗时均值 0.47 ms
- 动画耗时均值 0.78 ms
- 粒子系统耗时均值 0.03 ms
- 加载耗时均值 0.08 ms

**折叠面板 (el-collapse)**
- GPU压力系数 36 % [已展开]
- 渲染耗时均值 3.12 ms [已展开]
- 逻辑代码耗时均值 3.74 ms [已展开]
- 同步等待耗时均值 6.1 ms [已展开]
- UI耗时均值 0.31 ms [已展开]
- 物理耗时均值 0.47 ms [已展开]
- 动画耗时均值 0.78 ms [已展开]
- 粒子系统耗时均值 0.03 ms [已展开]
- 加载耗时均值 0.08 ms

**表格列**
- 报告名称
- 测试时间
- 机型

**操作按钮**
- 同档次排名

<details><summary>页面文本采样</summary>

```
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
性能简报
评级
趋势
报告名称：
荣耀
V20_2025/06/20_overview
测试时间：
2025-05-21
11:55:51
报告备注：
测试帧数：
32970
数据汇总
同档次排名
FPS均值
54.76
帧/秒
行业水平
优于83%
```
</details>

---

### 运行信息 `basicinfo` → UProfiler `#basicinfo`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/basicinfo?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**操作按钮**
- 下载

<details><summary>页面文本采样</summary>

```
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
运行信息
总览（0-32987帧）
指定帧
指定场景
查看场景性能列表
FPS
长按可拖拽排序
均值：
54.76
帧/秒
当前帧：
25帧/秒
(第1帧)
最大值：
57帧/秒
(第2460帧)
CPU频率
长按可拖拽排序
均值：
1340.4
MHz
最大值：
1955.75MHz
(第0帧)
无数据
CPU每帧耗时
长按可拖拽排序
>40ms帧数占比：
0.57
%
最大值：
461.95ms
(第11288帧)
无数据
CPU每帧耗时
长按可拖拽排序
卡顿率：
0.43
%
最大值：
461.95ms
(第11288帧)
无数据
CPU每帧耗时
长按可拖拽排序
Jank均值：
1.19
次/分钟
最大值：
461.95ms
(第11288帧)
无数据
CPU每帧耗时
长按可拖拽排序
Big
Jank均值：
0.89
次/分钟
最大值：
461.95ms
(第11288帧)
无数据
PSS内存
长按可拖拽排序
峰值：
1.06
GB
最大值：
1.06GB
(第17940帧)
无数据
网络下载
长按可拖拽排序
峰值：
4.58
KB
最大值：
4.58KB
(第32280帧)
无数据
网络上传
长按可拖拽排序
峰值：
2.25
KB
最大值：
2.25KB
(第32280帧)
无数据
```
</details>

---

### 场景概览 · 性能概览 `sceneOverview` → UProfiler `#scene-overview`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/sceneOverview?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**表格列**
- 场景名
- 帧数
- begin
- end
- FPS均值(帧/秒)
- PSS内存峰值(MB)
- Reserved Mono峰值(MB)
- CPU耗时均值(ms)
- CPU耗时峰值(ms)
- Triangles峰值(个)
- DrawCall峰值(个)
- 网络上传峰值(MB)
- 网络下载峰值(MB)

**操作按钮**
- 导出数据
- 0_MainMenu
- 00_mochuanlinju
- 1000_daditu
- 01_moqiaoshanzhuang
- 1000_daditu
- 08_gezilou
- 1000_daditu
- 05_yilinlou
- 1000_daditu
- 17_luojibinghang
- 0_MainMenu
- 00_mochuanlinju
- 1000_daditu
- 01_moqiaoshanzhuang

<details><summary>页面文本采样</summary>

```
场景管理
GPU分析
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
性能概览
场景性能概览
CPU耗时(ms)
26.5726.57
18.1718.17
18.218.2
18.1818.18
18.2418.24
18.1618.16
18.1818.18
18.1218.12
18.3118.31
18.0918.09
渲染
UI
加载
动画
粒子系统
物理
同步等待
逻辑代码
0_MainMenu
00_mochuanlinju
1000_daditu
01_moqiaoshanzhuang
1000_daditu
08_gezilou
1000_daditu
05_yilinlou
1000_daditu
17_luojibinghang
0
10
20
30
场景性能概览列表
导出数据
帧数
begin
end
FPS均值(帧/秒)
PSS内存峰值(MB)
Reserved
Mono峰值(MB)
CPU耗时均值(ms)
CPU耗时峰值(ms)
Triangles峰值(个)
DrawCall峰值(个)
网络上传峰值(MB)
网络下载峰值(MB)
600
0
600
39.1
720.66
32.51
26.57
400.61
2346
40
0
0
5010
600
5610
55
909.15
32.51
18.17
225.96
113515
454
0
0
5670
5610
11280
54.93
927.62
32.51
18.2
423.76
225116
411
0
0
6580
11280
17860
55.09
1085.77
32.15
18.18
461.95
171092
567
0
0
830
17860
18690
54.78
1086.63
30.76
18.24
110.99
246761
374
0
0
4540
18690
23230
55.16
1082.48
30.38
18.16
128.1
232813
341
0
0
470
23230
23700
54.8
1058.26
30.38
18.18
55.8
241005
383
0
0
3430
23700
27130
55.17
1061.11
30.38
18.12
154.66
201118
365
0
0
640
27130
27770
54.77
1062.92
30.38
18.31
112.18
205602
349
0
0
5210
27770
32980
55.26
105
…（截断）
```
</details>

---

### 场景概览 · 场景管理 `scenemanagement` → UProfiler `#scene-management`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/scenemanagement?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**区块标题**
- 每帧耗时曲线
- 未获取到数据

**表格列**
- 场景 游戏运行过程中小于100帧的场景已隐藏，在场景拆分时您可任意分配场景帧数。
- 起始帧
- 结束帧
- 总帧数
- 平均每帧耗时（ms）
- 查看

**操作按钮**
- 场景详情
- 场景详情
- 场景详情
- 场景详情
- 场景详情
- 场景详情
- 场景详情
- 场景详情
- 场景详情
- 场景详情

<details><summary>页面文本采样</summary>

```
GPU分析
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
场景管理
每帧耗时曲线
单位:
ms
0
5495
10
990
16
485
21
980
27
475
0
5495
10
990
16
485
21
980
27
475
32
970
0.00
96.84
193.68
290.53
387.37
484.21
每帧耗时曲线
场景
起始帧
结束帧
总帧数
平均每帧耗时（ms）
查看
0_MainMenu
0
600
600
26.57
场景详情
00_mochuanlinju
600
5610
5010
18.17
场景详情
1000_daditu
5610
11280
5670
18.20
场景详情
01_moqiaoshanzhuang
11280
17860
6580
18.18
场景详情
1000_daditu
17860
18690
830
18.24
场景详情
08_gezilou
18690
23230
4540
18.16
场景详情
1000_daditu
23230
23700
470
18.18
场景详情
05_yilinlou
23700
27130
3430
18.12
场景详情
1000_daditu
27130
27770
640
18.31
场景详情
17_luojibinghang
27770
32980
5210
18.09
场景详情
```
</details>

---

### GPU 分析 · 渲染分析 `gpu-render` → UProfiler `#gpu-render`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/gpuAnalysis/renderAnalysis?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**操作按钮**
- 均值曲线
- 峰值曲线
- 下载

<details><summary>页面文本采样</summary>

```
GPU
带宽分析
指标汇总
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
GPU渲染分析
总览（0-32987帧）
指定场景
查看场景性能列表
GPU
Clocks
压力系数均值：
36.19
%
当前帧：
42.53万Cycles
(第0帧)
最大值：
1679.82万Cycles
(第18600帧)
0
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
400
800
1200
1600
Clocks（万Cycles）
GPU
Bound
能耗压力阈值（万Cycles）
帧率压力阈值（万Cycles）
均值曲线
峰值曲线
相关指标：
GPU
Fragment
Shaded
长按可拖拽排序
均值：
737.22
万个
当前帧：
372.36万个
(第0帧)
最大值：
3344.15万个
(第5610帧)
GPU
Total
Shader
Cycles
长按可拖拽排序
均值：
41.13
百万Cycles
当前帧：
6.28百万Cycles
(第0帧)
最大值：
181.71百万Cycles
(第23460帧)
GPU
Shader
Instructions
长按可拖拽排序
均值：
17.3
百万个
当前帧：
1.98百万个
(第0帧)
最大值：
77.8百万个
(第23460帧)
GPU
Input
Primitive
长按可拖拽排序
均值：
8.49
万个
当前帧：
843.5个
(第0帧)
最大值：
40.71万个
(第8580帧)
FPS
长按可拖拽排序
均值：
54.76
帧/秒
当前帧：
25帧/秒
(第0帧)
最大值：
57帧/秒
(第2460帧)
GPU
Freq
长按可拖拽排序
均值：
397.41
MHz
当前帧：
277MHz
(第0帧)
最大值：
644MHz
(第5820帧)
```
</details>

---

### GPU 分析 · 带宽分析 `gpu-bandwidth` → UProfiler `#gpu-bandwidth`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/gpuAnalysis/bandWidthAnalysis?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**操作按钮**
- 均值曲线
- 峰值曲线
- 下载

<details><summary>页面文本采样</summary>

```
指标汇总
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
GPU带宽分析
总览（0-32987帧）
指定场景
查看场景性能列表
GPU
Total
Bandwidth
长按可拖拽排序
均值：
30.39
MB
当前帧：
4.64MB
(第0帧)
最大值：
110.14MB
(第5610帧)
0
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
14.31
28.61
42.92
57.22
Total（MB）
Read
Total（MB）
Write
Total（MB）
GPU
Bound
均值曲线
峰值曲线
GPU
Total
Bandwidth（in
seconds）
长按可拖拽排序
均值：
1657.31
MB/秒
当前帧：
141.58MB/秒
(第0帧)
最大值：
2811.96MB/秒
(第4730帧)
相关指标：
GPU
Read
Stall
长按可拖拽排序
均值：
0
%
当前帧：
0%
(第0帧)
最大值：
0%
(第0帧)
GPU
Input
Primitive
长按可拖拽排序
均值：
8.49
万个
当前帧：
843.5个
(第0帧)
最大值：
40.71万个
(第8580帧)
功率
长按可拖拽排序
峰值：
4550
mW
当前帧：
2336mW
(第0帧)
最大值：
4550mW
(第18720帧)
```
</details>

---

### GPU 分析 · 性能汇总 `gpu-summary` → UProfiler `#gpu-summary`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/gpuAnalysis/performance?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**操作按钮**
- 均值曲线
- 峰值曲线
- 下载

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
GPU
渲染分析
GPU
带宽分析
指标汇总
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
指标汇总
总览（0-32987帧）
指定场景
查看场景性能列表
主要指标：
GPU
Clocks
长按可拖拽排序
压力系数均值：
36.19
%
当前帧：
42.53万Cycles
(第0帧)
最大值：
1679.82万Cycles
(第18600帧)
0
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
400
800
1200
1600
Clocks（万Cycles）
GPU
Bound
能耗压力阈值（万Cycles）
帧率压力阈值（万Cycles）
均值曲线
峰值曲线
GPU
Total
Bandwidth
长按可拖拽排序
均值：
30.39
MB
当前帧：
4.64MB
(第0帧)
最大值：
110.14MB
(第5610帧)
GPU
Total
Bandwidth（in
seconds）
长按可拖拽排序
均值：
1657.31
MB/秒
当前帧：
141.58MB/秒
(第0帧)
最大值：
2811.96MB/秒
(第4730帧)
次要指标：
GPU
Freq
长按可拖拽排序
均值：
397.41
MHz
当前帧：
277MHz
(第0帧)
最大值：
644MHz
(第5820帧)
GPU
Usage
长按可拖拽排序
均值：
54.67
%
当前帧：
38%
(第0帧)
最大值：
89%
(第5820帧)
GPU
Time
长按可拖拽排序
均值：
13.3
ms
当前帧：
2.90ms
(第0帧)
最大值：
45ms
(第17860帧)
GPU
Fragment
Utilization
长按可拖拽排序
均值：
69.32
%
当前帧：
70.8%
(第0帧)
最大值：
100%
(第1500帧)
GPU
Fragment
Shaded
长按可拖拽排序
均值：
737.22
万个
当前帧：
372.36万个
(第0帧)
最大值：
3344.15万个
(第5610帧)
GPU
Input
Primitive
长按可拖拽排序
均值：
8.49
万个
当前帧：
843.5个
(第0帧)
最大值：
40.71万个
(第8580帧)
GPU
Total
Shad
…（截断）
```
</details>

---

### 总体性能趋势 · 模块耗时统计 `moduleoverview` → UProfiler `#module-time`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=moduleoverview`

#### 默认视图
**表格列**
- 模块分类
- CPU耗时均值(ms)
- 推荐值(ms)

<details><summary>页面文本采样</summary>

```
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
模块耗时统计
各模块CPU耗时
模块占比预览
单击模块，查看模块函数占比
模块分类
CPU耗时均值(ms)
推荐值(ms)
同步等待
6.1
-
逻辑代码
3.74
7
渲染
3.12
6
动画
0.78
1
物理
0.47
0.4
UI
0.31
1
加载
0.08
0.5
粒子系统
0.03
0.5
Overhead
3.69
-
各模块CPU耗时
```
</details>

---

### 各线程 CPU 调用堆栈 `threadstack` → UProfiler `#thread-stack`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=threadstack`

#### 默认视图
**右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**图表标题**
- 各线程CPU耗时

**区块标题**
- 各线程CPU耗时

#### 展开后视图（折叠/子页签点击后）
**右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**图表标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Audio Mixer Thread函数堆栈汇总（总览）

**区块标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Audio Mixer Thread函数堆栈汇总（总览）

**表格列**
- 函数名
- 总耗时(ms)
- 总耗时均值(ms)
- 总体占比
- 自身耗时(ms)
- 自身耗时均值(ms)
- 自身占比
- 调用次数
- 操作

**操作按钮**
- 导出堆栈

#### 右侧子页签详情（共 6 个）
##### 子页签：线程总览
**  右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**  图表标题**
- 各线程CPU耗时

**  区块标题**
- 各线程CPU耗时

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
各线程CPU调用堆栈
线程总览
MainThread
Audio
Stream
Thread
Render
Thread
Loading.AsyncRead
Audio
Mixer
Thread
线程CPU耗时均值
各线程CPU耗时
```
</details>

##### 子页签：MainThread
**  右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**  图表标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- MainThread函数堆栈汇总（总览）

**  区块标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- MainThread函数堆栈汇总（总览）

**  表格列**
- 函数名
- 总耗时(ms)
- 总耗时均值(ms)
- 总体占比
- 自身耗时(ms)
- 自身耗时均值(ms)
- 自身占比
- 调用次数
- 显著调用帧数
- 操作

**  操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
各线程CPU调用堆栈
线程总览
MainThread
Audio
Stream
Thread
Render
Thread
Loading.AsyncRead
Audio
Mixer
Thread
总览（0-32987帧）
指定帧
指定场景
MainThread
CPU耗时
均值：
16.95
ms
当前帧耗时：
2.18ms
(第1帧)
函数堆栈自定义面板
搜索函数
过滤总体占比<1%的堆栈节点
函数堆栈冰柱图（总览）
MainThread函数堆栈汇总（总览）
导出堆栈
函数名
总耗时(ms)
总耗时均值(ms)
总体占比
自身耗时(ms)
自身耗时均值(ms)
自身占比
调用次数
显著调用帧数
Gfx.WaitForPresentOnGfxThread
172296.14
5.23
100%
171988.18
5.22
99.82%
32985
32985
Camera.Render
99455.36
3.02
100%
5786.90
0.18
5.82%
32986
32986
UWA.Overhead
50420.94
1.53
100%
3967.34
0.12
7.87%
131945
32987
TimeUpdate.WaitForLastPresentationAndUpdateTime
28166.04
0.85
100%
837.39
0.03
2.97%
32987
32987
GUI.Repaint
26627.92
0.81
100%
2778.48
0.08
10.43%
32986
32986
Profiler.FlushCounters
19742.37
0.60
100%
220.89
0.01
1.12%
32987
32987
Director.ProcessFrame
18581.44
0.56
100%
397.73
0.01
2.14%
225076
32986
Physics.Processing
11563.12
0.35
100%
11563.12
0.35
100%
29281
27386
NavMeshManager
11318.57
0.
…（截断）
```
</details>

##### 子页签：Audio Stream Thread
**  右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**  图表标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Audio Stream Thread函数堆栈汇总（总览）

**  区块标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Audio Stream Thread函数堆栈汇总（总览）

**  表格列**
- 函数名
- 总耗时(ms)
- 总耗时均值(ms)
- 总体占比
- 自身耗时(ms)
- 自身耗时均值(ms)
- 自身占比
- 调用次数
- 操作

**  操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
各线程CPU调用堆栈
线程总览
MainThread
Audio
Stream
Thread
Render
Thread
Loading.AsyncRead
Audio
Mixer
Thread
总览（0-32987帧）
指定帧
指定场景
Audio
Stream
Thread
CPU耗时
均值：
0.01
ms
当前帧耗时：
0.04ms
(第1帧)
函数堆栈自定义面板
搜索函数
过滤总体占比<1%的堆栈节点
函数堆栈冰柱图（总览）
Audio
Stream
Thread函数堆栈汇总（总览）
导出堆栈
函数名
总耗时(ms)
总耗时均值(ms)
总体占比
自身耗时(ms)
自身耗时均值(ms)
自身占比
调用次数
Audio.Thread
441.34
0.01
100%
441.34
0.01
100%
59689
操作
Time
Call
```
</details>

##### 子页签：Render Thread
**  右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**  图表标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Render Thread函数堆栈汇总（总览）

**  区块标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Render Thread函数堆栈汇总（总览）

**  表格列**
- 函数名
- 总耗时(ms)
- 总耗时均值(ms)
- 总体占比
- 自身耗时(ms)
- 自身耗时均值(ms)
- 自身占比
- 调用次数
- 操作

**  操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
各线程CPU调用堆栈
线程总览
MainThread
Audio
Stream
Thread
Render
Thread
Loading.AsyncRead
Audio
Mixer
Thread
总览（0-32987帧）
指定帧
指定场景
Render
Thread
CPU耗时
均值：
15.27
ms
当前帧耗时：
2.79ms
(第1帧)
函数堆栈自定义面板
搜索函数
过滤总体占比<1%的堆栈节点
函数堆栈冰柱图（总览）
Render
Thread函数堆栈汇总（总览）
导出堆栈
函数名
总耗时(ms)
总耗时均值(ms)
总体占比
自身耗时(ms)
自身耗时均值(ms)
自身占比
调用次数
Gfx.PresentFrame
343062.85
10.41
100%
312510.91
9.48
91.09%
32987
Camera.Render
110759.83
3.36
100%
7381.16
0.22
6.66%
32987
GUI.Repaint
18759.04
0.57
100%
18670.52
0.57
99.53%
32987
Rendering.RenderOverlays
10176.88
0.31
100%
650.76
0.02
6.39%
32987
PlayerEndOfFrame
9805.64
0.30
100%
9805.64
0.30
100%
32986
MeshSkinning.SkinOnGPU
4680.69
0.14
100%
349.03
0.01
7.46%
81727
Profiler.FlushRenderCounters
3238.07
0.10
100%
3238.07
0.10
100%
32987
ScheduleGeometryJobs
1492.77
0.05
100%
1492.77
0.05
100%
20487
AsyncUploadManager.AsyncResourceUploadAll
918.65
0.03
100%
334.76
0.01
36.44%
66473
Gfx.UploadTexture
2
…（截断）
```
</details>

##### 子页签：Loading.AsyncRead
**  右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**  图表标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Loading.AsyncRead函数堆栈汇总（总览）

**  区块标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Loading.AsyncRead函数堆栈汇总（总览）

**  表格列**
- 函数名
- 总耗时(ms)
- 总耗时均值(ms)
- 总体占比
- 自身耗时(ms)
- 自身耗时均值(ms)
- 自身占比
- 调用次数
- 操作

**  操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
各线程CPU调用堆栈
线程总览
MainThread
Audio
Stream
Thread
Render
Thread
Loading.AsyncRead
Audio
Mixer
Thread
总览（0-32987帧）
指定帧
指定场景
Loading.AsyncRead
CPU耗时
均值：
0.02
ms
当前帧耗时：
0.21ms
(第1帧)
函数堆栈自定义面板
搜索函数
过滤总体占比<1%的堆栈节点
函数堆栈冰柱图（总览）
Loading.AsyncRead函数堆栈汇总（总览）
导出堆栈
函数名
总耗时(ms)
总耗时均值(ms)
总体占比
自身耗时(ms)
自身耗时均值(ms)
自身占比
调用次数
AsyncReadManager.ReadFile
703.27
0.02
100%
17.02
0.00
2.42%
731
操作
Time
Call
```
</details>

##### 子页签：Audio Mixer Thread
**  右侧子页签**
- 线程总览
- MainThread
- Audio Stream Thread
- Render Thread
- Loading.AsyncRead
- Audio Mixer Thread

**  图表标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Audio Mixer Thread函数堆栈汇总（总览）

**  区块标题**
- 函数堆栈自定义面板
- 函数堆栈冰柱图（总览）
- Audio Mixer Thread函数堆栈汇总（总览）

**  表格列**
- 函数名
- 总耗时(ms)
- 总耗时均值(ms)
- 总体占比
- 自身耗时(ms)
- 自身耗时均值(ms)
- 自身占比
- 调用次数
- 操作

**  操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
各线程CPU调用堆栈
线程总览
MainThread
Audio
Stream
Thread
Render
Thread
Loading.AsyncRead
Audio
Mixer
Thread
总览（0-32987帧）
指定帧
指定场景
Audio
Mixer
Thread
CPU耗时
均值：
0.07
ms
当前帧耗时：
0.09ms
(第1帧)
函数堆栈自定义面板
搜索函数
过滤总体占比<1%的堆栈节点
函数堆栈冰柱图（总览）
Audio
Mixer
Thread函数堆栈汇总（总览）
导出堆栈
函数名
总耗时(ms)
总耗时均值(ms)
总体占比
自身耗时(ms)
自身耗时均值(ms)
自身占比
调用次数
Audio.Thread
2195.48
0.07
100%
2007.41
0.06
91.43%
59507
操作
Time
Call
```
</details>

---

### 渲染模块性能 `rendering` → UProfiler `#module-rendering`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=rendering`

#### 默认视图
**右侧子页签**
- Draw Call峰值过高

**折叠面板 (el-collapse)**
- Draw Call峰值过高

**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
渲染模块性能
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
渲染模块
CPU耗时
长按可拖拽排序
均值：
3.12
ms
当前帧：
0ms
(第1帧)
最大值：
260.15ms
(第5618帧)
Camera.Render
CPU耗时
长按可拖拽排序
均值：
3.01
ms
最大值：
260.09ms
(第5618帧)
无数据
不透明渲染
CPU耗时
长按可拖拽排序
均值：
0.86
ms
最大值：
193.53ms
(第2499帧)
无数据
半透明渲染
CPU耗时
长按可拖拽排序
均值：
0.12
ms
最大值：
69.11ms
(第5618帧)
无数据
GL
DrawCall
数量
长按可拖拽排序
峰值：
567
个
最大值：
567个
(第16022帧)
无数据
GL
Triangle
数量
长按可拖拽排序
峰值：
246761
个
最大值：
246761个
(第18154帧)
无数据
GL
Batches
数量
长按可拖拽排序
峰值：
196
个
最大值：
196个
(第4202帧)
无数据
ParticleSystem.Draw
调用次数
长按可拖拽排序
峰值：
6
次
最大值：
6次
(第5618帧)
无数据
Camera.ImageEffects
CPU耗时
长按可拖拽排序
均值：
0.04
ms
最大值：
37.8ms
(第5618帧)
无数据
Culling
CPU耗时
长按可拖拽排序
均值：
1.21
ms
最大值：
46.65ms
(第6497帧)
无数据
Screen.Resolution.width
长按可拖拽排序
最大值：
1500个
(第1帧)
无数据
Screen.Resolution.height
长按可拖拽排序
最大值：
734个
(第1帧)
无数据
OnDemandRendering.renderFrameInterval
长按可拖拽排序
最大值：
1个
(第1帧)
无数据
函数堆栈自定义面板
渲染模块函数堆栈（总览）
搜索函数
本模块的渲染函数调用
其他模块的渲染函数调用
自定义指标配置
导出堆栈
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
Camera.Render
3.01
99455.36
100%
578
…（截断）
```
</details>

---

### GPU 同步模块性能 `sync` → UProfiler `#module-sync`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=sync`

#### 默认视图
**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
GPU同步模块性能
总览（0-32980帧）
指定帧
指定场景
函数堆栈自定义面板
GPU同步模块函数堆栈（总览）
搜索函数
本模块的GPU同步函数调用
其他模块的GPU同步函数调用
自定义指标配置
导出堆栈
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
Gfx.WaitForPresentOnGfxThread
5.22
172296.14
100%
171988.18
99.82%
32985
5.22
32985
1.00
TimeUpdate.WaitForLastPresentationAndUpdateTime
0.85
28166.04
100%
837.39
2.97%
32987
0.85
32987
1.00
EndGraphicsJobs
0.02
527.84
100%
312.18
59.14%
131944
0.00
32986
4.00
Graphics.PresentAndSync
0.01
361.86
100%
86.64
23.94%
32987
0.01
32987
1.00
操作
Time
Call
Time
Call
Time
Call
Time
Call
```
</details>

---

### 逻辑代码模块性能 `scripting` → UProfiler `#module-scripting`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=scripting`

#### 默认视图
**单选组**
- 正序调用分析
- 倒序调用分析

**区块标题**
- 函数堆栈冰柱图（总览）

**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
逻辑代码模块性能
总览（0-32987帧）
指定帧
指定场景
查看场景性能列表
逻辑代码模块
CPU耗时
均值：
3.74
ms
当前帧：
46.03ms
(第1帧)
最大值：
417.72ms
(第11288帧)
函数堆栈自定义面板
搜索函数
过滤总体占比<1%的堆栈节点
函数堆栈冰柱图（总览）
逻辑代码模块函数堆栈（总览）
自定义指标配置
导出堆栈
正序调用分析
倒序调用分析
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
GUI.Repaint
0.81
26627.92
100%
2778.48
10.43%
32986
0.81
32986
1.00
NavMeshManager
0.34
11318.57
100%
6808.10
60.15%
65371
0.17
32986
1.98
LevelMaster.Update
0.30
9930.89
100%
9930.89
100%
32385
0.31
32385
0.98
EventSystem.Update
0.28
9081.07
100%
8412.21
92.63%
33290
0.27
32986
1.01
InputManager_Base.Update
0.25
8316.74
100%
8316.74
100%
32986
0.25
32986
1.00
CinemachineBrain.LateUpdate
0.21
7055.53
100%
6221.00
88.17%
32385
0.22
32385
0.98
CoroutinesDelayedCalls
0.15
4813.68
100%
1420.00
29.5%
129031
0.04
32986
3.91
InputManager_Base.FixedUpdate
0.10
3250.57
100%
3250.57
100%
30073
0.11
27942
0.91
Jyx2Player.Update
0.10
3176.61
100%
944.17
29.72%
32092
0.10
32092
0.97
Jyx2_PlayerMovement.Update
0.07
2452.57
100%
2448.82
99.85%
32092
0.08
32092

…（截断）
```
</details>

---

### UI 模块性能 `ui` → UProfiler `#module-ui`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=ui`

#### 默认视图
**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
模块耗时统计
各线程CPU调用堆栈
渲染模块性能1
GPU同步模块性能
逻辑代码模块性能
UI模块性能
加载模块性能1
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
UI模块性能
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
UI模块
CPU耗时
长按可拖拽排序
均值：
0.31
ms
当前帧：
0ms
(第1帧)
最大值：
28.06ms
(第2345帧)
Canvas.SendWillRenderCanvases
CPU耗时
长按可拖拽排序
均值：
0.1
ms
最大值：
9.97ms
(第305帧)
无数据
Canvas.BuildBatch
CPU耗时
长按可拖拽排序
均值：
0.02
ms
最大值：
0.99ms
(第20041帧)
无数据
Rendering.EmitWorldScreenspaceCameraGeometry
CPU耗时
长按可拖拽排序
均值：
0.01
ms
最大值：
0.44ms
(第19440帧)
无数据
CanvasRenderer.SyncTransform
调用次数
长按可拖拽排序
峰值：
90
次
最大值：
90次
(第22745帧)
无数据
函数堆栈自定义面板
UI模块函数堆栈（总览）
搜索函数
本模块的UI函数调用
其他模块的UI函数调用
自定义指标配置
导出堆栈
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
Rendering.UpdateBatches
0.18
5984.45
100%
1256.99
21%
32986
0.18
32986
1.00
Rendering.RenderOverlays
0.10
3163.28
100%
289.38
9.15%
32986
0.10
32986
1.00
ScrollRect.LateUpdate
0.02
704.00
100%
175.42
24.92%
8566
0.08
7680
0.26
Rendering.EmitWorldScreenspaceCameraGeometry
0.01
246.63
100%
246.63
100%
65972
0.00
32986
2.00
操作
Time
Call
Time
Call
Time
Call

…（截断）
```
</details>

---

### 加载模块性能 `loading` → UProfiler `#module-loading`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=loading`

#### 默认视图
**右侧子页签**
- GC.Collect调用频率过高

**折叠面板 (el-collapse)**
- GC.Collect调用频率过高

**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈
- 下载

<details><summary>页面文本采样</summary>

```
物理系统性能2
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
加载模块性能
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
加载模块
CPU耗时
长按可拖拽排序
均值：
0.08
ms
当前帧：
0ms
(第1帧)
最大值：
388.71ms
(第293帧)
LUP
CPU耗时
长按可拖拽排序
显著帧均值：
26.07
ms
最大值：
388.71ms
(第293帧)
无数据
GarbageCollectAssetsProfile
CPU耗时
长按可拖拽排序
显著帧均值：
38.44
ms
最大值：
40.98ms
(第18700帧)
无数据
GC.Collect
CPU耗时
长按可拖拽排序
调用频率：
26.51
次/分钟
最大值：
10.95ms
(第27776帧)
无数据
Game
Object
数量
长按可拖拽排序
峰值：
2157
个
最大值：
2157个
(第17844帧)
无数据
Scene
Object
数量
长按可拖拽排序
峰值：
9013
个
最大值：
9013个
(第17861帧)
无数据
Asset
数量
长按可拖拽排序
峰值：
12415
个
最大值：
12415个
(第603帧)
无数据
Object
数量
长按可拖拽排序
峰值：
19437
个
最大值：
19437个
(第603帧)
无数据
Application.backgroundLoadingPriority
长按可拖拽排序
最大值：
1
(第1帧)
无数据
函数堆栈自定义面板
加载模块函数堆栈（总览）
搜索函数
本模块的加载函数调用
其他模块的加载函数调用
自定义指标配置
导出堆栈
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
UpdatePreloading
0.08
2772.92
100%
116.81
4.21%
32986
0.08
32986
1.00
操作
Time
Call
```
</details>

---

### 物理系统性能 `physics` → UProfiler `#module-physics`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=physics`

#### 默认视图
**右侧子页签**
- 物理模块耗时高
- Physics.Processing耗时过高

**折叠面板 (el-collapse)**
- 物理模块耗时高
- Physics.Processing耗时过高

**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
动画模块性能
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
物理系统性能
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
物理模块
CPU耗时
长按可拖拽排序
均值：
0.47
ms
当前帧：
0ms
(第1帧)
最大值：
8.9ms
(第15651帧)
Physics.Processing
CPU耗时
长按可拖拽排序
均值：
0.35
ms
最大值：
8.74ms
(第15651帧)
无数据
Physics.Simulate
CPU耗时
长按可拖拽排序
均值：
0.07
ms
最大值：
1.57ms
(第16621帧)
无数据
Physics.FetchResults
CPU耗时
长按可拖拽排序
显著帧均值：
0
ms
最大值：
1.37ms
(第17247帧)
无数据
Physics.ProcessReports
CPU耗时
长按可拖拽排序
显著帧均值：
5.26
ms
最大值：
6.28ms
(第603帧)
无数据
函数堆栈自定义面板
物理系统函数堆栈（总览）
搜索函数
本模块的物理函数调用
其他模块的物理函数调用
自定义指标配置
导出堆栈
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
Physics.Processing
0.35
11563.12
100%
11563.12
100%
29281
0.39
27386
0.89
Physics.Simulate
0.07
2240.28
100%
2240.28
100%
29281
0.08
27386
0.89
Physics.FetchResults
0.03
849.44
100%
849.44
100%
29281
0.03
27386
0.89
Physics.ProcessReports
0.02
526.35
100%
182.61
34.69%
29281
0.02
27386
0.89
Physics2D.Simulate
0.01
174.12
100%
174.12
100%
30073
0.01
27942
0.91
Physics2D.InterpolatePoses
0.00
79.52
100%
79.52
100%
32986
0.00
32986
1.00
操作
Time
Call
Time
Call
Time
Call
Time
Call
Time
Call
Time
Call
```
</details>

---

### 动画模块性能 `animation` → UProfiler `#module-animation`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=animation`

#### 默认视图
**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
粒子系统性能
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
动画模块性能
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
动画模块
CPU耗时
长按可拖拽排序
均值：
0.78
ms
当前帧：
0ms
(第1帧)
最大值：
7.19ms
(第25822帧)
Animators.Update
CPU耗时
长按可拖拽排序
均值：
0.55
ms
最大值：
6.44ms
(第13334帧)
无数据
MeshSkinning.Update
CPU耗时
长按可拖拽排序
均值：
0.07
ms
最大值：
3ms
(第25981帧)
无数据
Animator.Initialize
CPU耗时
长按可拖拽排序
显著帧均值：
3.02
ms
最大值：
3.02ms
(第23705帧)
无数据
Animators.WriteJob
CPU耗时
长按可拖拽排序
均值：
0.08
ms
最大值：
3.22ms
(第27123帧)
无数据
函数堆栈自定义面板
动画模块函数堆栈（总览）
搜索函数
本模块的动画函数调用
其他模块的动画函数调用
自定义指标配置
导出堆栈
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
Director.ProcessFrame
0.56
18581.44
100%
397.73
2.14%
225076
0.08
32986
6.82
Director.PrepareFrame
0.15
4815.57
100%
3334.13
69.24%
225076
0.02
32986
6.82
MeshSkinning.Update
0.07
2233.28
100%
638.47
28.59%
32375
0.07
32375
0.98
操作
Time
Call
Time
Call
Time
Call
```
</details>

---

### 粒子系统性能 `particle` → UProfiler `#module-particle`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/engine?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=particle`

#### 默认视图
**表格列**
- 函数名
- 耗时均值(ms)
- 总耗时(ms)
- 总体占比
- 自身耗时(ms)
- 自身占比
- 总调用次数
- 单次耗时(ms)
- 调用帧数
- 每帧调用次数
- 操作

**操作按钮**
- 导出堆栈

<details><summary>页面文本采样</summary>

```
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
粒子系统性能
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
粒子系统模块
CPU耗时
长按可拖拽排序
均值：
0.03
ms
当前帧：
0ms
(第1帧)
最大值：
6.71ms
(第27421帧)
ParticleSystem.Update
CPU耗时
长按可拖拽排序
均值：
0.02
ms
最大值：
0.63ms
(第10935帧)
无数据
函数堆栈自定义面板
粒子系统函数堆栈（总览）
搜索函数
本模块的粒子函数调用
其他模块的粒子函数调用
自定义指标配置
导出堆栈
函数名
耗时均值(ms)
总耗时(ms)
总体占比
自身耗时(ms)
自身占比
总调用次数
单次耗时(ms)
调用帧数
每帧调用次数
ParticleSystem.Update
0.02
724.33
100%
675.31
93.23%
32986
0.02
32986
1.00
ParticleSystem.EndUpdateAll
0.01
230.00
100%
131.71
57.27%
32986
0.01
32986
1.00
操作
Time
Call
Time
Call
```
</details>

---

### 卡顿分析 · 卡顿帧汇总 `jankFrame` → UProfiler `#jank-frames`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/jank?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=jankFrame`

#### 默认视图
**表格列**
- 函数名称
- 重点卡顿点数量
- 总耗时占比
- 自身耗时占比
- 总耗时
- 自身耗时
- 分布卡顿点数量
- 操作

**操作按钮**
- 展开全部
- 函数详情
- 添加至函数组
- 函数详情
- 添加至函数组
- 函数详情
- 添加至函数组
- 函数详情
- 添加至函数组
- 函数详情
- 添加至函数组
- 函数详情
- 添加至函数组

<details><summary>页面文本采样</summary>

```
全部卡顿帧
：
12
（100.00
%）
严重卡顿帧
：
9
（75.00
%）
GC.Collect类卡顿帧
：
0
（0.00
%）
Unload
Unused卡顿帧
：
0
（0.00
%）
加载类卡顿帧
：
5
（41.67
%）
动画类卡顿帧
：
0
（0.00
%）
物理类卡顿帧
：
0
（0.00
%）
其他类卡顿帧
：
10
（83.33
%）
卡顿帧函数冰柱图
展开全部
卡顿帧函数列表
查看函数组耗时
函数名称
重点卡顿点数量
总耗时占比
自身耗时占比
总耗时
自身耗时
分布卡顿点数量
操作
UpdatePreloading
10
33.63
%
0.00
%
873.941
ms
0.055
ms
12
函数详情
添加至函数组
Camera.Render
4
23.68
%
0.08
%
615.439
ms
2.100
ms
12
函数详情
添加至函数组
GUI.Repaint
1
17.05
%
0.03
%
442.990
ms
0.732
ms
12
函数详情
添加至函数组
UWA.Overhead
8
15.86
%
0.11
%
412.082
ms
2.912
ms
12
函数详情
添加至函数组
CoroutinesDelayedCalls
0
3.14
%
0.07
%
81.670
ms
1.947
ms
12
函数详情
添加至函数组
EnlightenRuntimeManager.PostUpdate
0
1.44
%
0.04
%
37.326
ms
0.913
ms
6
函数详情
添加至函数组
Rendering.UpdateBatches
0
1.20
%
0.10
%
31.059
ms
2.518
ms
12
函数详情
添加至函数组
```
</details>

---

### 卡顿分析 · 重点函数 `jankFunction` → UProfiler `#jank-func`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/jank?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=jankFunction`

#### 默认视图
**右侧子页签**
- Unload Unused调用频率过高
- GC.Collect调用频率过高

**折叠面板 (el-collapse)**
- Unload Unused调用频率过高
- GC.Collect调用频率过高

**操作按钮**
- 下载

<details><summary>页面文本采样</summary>

```
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
重点函数分析
GC.Collect卡顿点
1
Unload
Unused卡顿点
1
加载卡顿点
动画卡顿点
物理卡顿点
总览（0-32987帧）
指定帧
指定场景
查看场景性能列表
GC.Collect
CPU耗时
调用频率：
26.51
次/分钟
当前帧：
1.73ms
(第1帧)
最大值：
10.95ms
(第27776帧)
```
</details>

---

### 内存分析 · 内存占用 `memory-occupy` → UProfiler `#memory-occupy`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/memoryAnalysis?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=occupy`

#### 默认视图
**区块标题**
- 内存占比冰柱图

**表格列**
- 内存堆栈
- 内存占用
- 推荐值
- 总体占比
- 操作

**操作按钮**
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用

<details><summary>页面文本采样</summary>

```
资源内存11
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
内存占用
内存占用
10k
20k
30k
2.5k
5k
7.5k
12.5k
15k
17.5k
22.5k
25k
27.5k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
381.47
MB
762.94
MB
1.12
GB
PSS内存占用
Reserved
Total
LUA堆内存占用
PSS显存
资源对象快照帧
low
memory
PSS内存占用峰值：
1.06GB
Reserved
Total峰值：
793.2MB
LUA堆内存占用峰值：
0.85MB
Reserved
Mono峰值：
32.51MB
当前帧内存堆栈冰柱图
（帧数：1）
内存占比冰柱图
双击色块，查看具体内存分配，展开下方内存堆栈
当前帧内存堆栈
（帧数：1）
内存堆栈
内存占用
推荐值
总体占比
操作
PSS内存占用
649.96
MB
2000MB
-
Reserved
Total
448.09
MB
1150MB
68.94
%
LUA堆内存占用
630
KB
110MB
0.09
%
其它
201.25
MB
-
30.96
%
Profiler
Reserved/Used
10k
20k
30k
2.5k
5k
7.5k
12.5k
15k
17.5k
22.5k
25k
27.5k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
476.84
MB
953.67
MB
1.4
GB
Used
System
Reserved
Total
Used
Total
Reserved
Gfx
Used
Gfx
Reserved
GC
Used
GC
Reserved
Profiler
Used
Profiler
Reserved
Audio
Used
Audio
Reserved
Video
Used
Video
```
</details>

---

### 内存分析 · 资源内存 `memory-resource` → UProfiler `#memory-resource`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/memoryAnalysis?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=resource`

#### 默认视图
**右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**折叠面板 (el-collapse)**
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**表格列**
- 资源类型
- 数量
- 内存占用
- 推荐值
- 操作

**操作按钮**
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看资源组成
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用

#### 展开后视图（折叠/子页签点击后）
**右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**折叠面板 (el-collapse)**
- 粒子系统资源内存 峰值： 1.98 MB 运行时粒子系统占用的内存峰值。除了内存外，该指标还可能影响渲染模块耗时、粒子系统更新耗时等，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 18 MB
- 粒子系统资源数量 峰值： 168 个 运行时同时存在于内存中的粒子系统组件数量峰值。作为一种SceneObjects对象，Hierarchy中粒子系统组建的数量直接影响其内存，需结合实际播放量综合判断是否存在缓存过度导致的浪费。 推荐值: [已展开]
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**单选组**
- 显示相同资源
- 显示差异资源

**表格列**
- 资源名称 疑似过度缓存 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- Playing组件数峰值-1
- Playing粒子数峰值-1
- 生命周期（帧数）-1
- 操作

**操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- Playing粒子数
- 内存占用
- 资源数量
- Playing粒子数
- 内存占用
- 资源数量
- Playing粒子数
- 内存占用
- 资源数量

#### 右侧子页签详情（共 10 个）
##### 子页签：总览
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  表格列**
- 资源类型
- 数量
- 内存占用
- 推荐值
- 操作

**  操作按钮**
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看资源组成
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用
- 查看具体资源使用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
资源内存占用
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
76.29
MB
152.59
MB
228.88
MB
纹理资源
网格资源
动画片段
音频片段
材质资源
Shader资源
字体资源
RenderTexture
粒子系统
AssetBundle
TextAsset
其它
资源对象快照帧
资源对象快照
(帧数：1)
对比模式
资源类型占比预览
单击资源类型，查看具体资源使用情况
纹理资源：29.21
%纹理资源：29.21
%
Shader资源：27.49
%Shader资源：27.49
%
字体资源：16.27
%字体资源：16.27
%
其他：11.30
%其他：11.30
%
RenderTexture：6.69
%RenderTexture：6.69
%
网格资源：4.35
%网格资源：4.35
%
AssetBundle：3.38
%AssetBundle：3.38
%
TextAsset：0.63
%TextAsset：0.63
%
动画片段：0.61
%动画片段：0.61
%
材质资源：0.07
%材质资源：0.07
%
音频片段：0.00
%音频片段：0.00
%
粒子系统：0.00
%粒子系统：0.00
%
资源类型
数量
内存占用
推荐值
操作
纹理资源
213
55.06
MB
210.00
MB
查看具体资源使用
Shader资源
73
51.81
MB
50.00
MB
查看具体资源使用
字体资源
6
30.66
MB
20.00
MB
查看具体资源使用
其他
6259
21.30
MB
-
查看资源组成
RenderTexture
2
12.60
MB
60.00
MB
查看具体资源使用
网格资源
29
8.20
MB
75.00
MB
查看具体资源使用
AssetBundle
3
6.37
MB
60.00
MB
查看具体资源使用
TextAsset
3617
1.19
MB
30.00
MB
查看具体资源使用
动画片段
32
1.14
MB
60.00
MB
查看具体资源使用
…（截断）
```
</details>

##### 子页签：纹理资源 3
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- 纹理资源内存 峰值： 166.07 MB 运行时纹理占用的内存峰值。除了内存外，该指标还可能影响GPU带宽、包体大小、加载效率，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 210 MB 推荐阅读 性能优化，进无止境-内 [已展开]
- 纹理资源数量 峰值： 596 个 运行时同时存在于内存中的纹理数量峰值。数量过多意味着可能资源未及时释放或缓存策略过于激进，对内存产生压力。 推荐阅读 性能优化，进无止境-内存篇（上）Unity加载模块深度解析（网格篇）如何优化资源，你还差
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- 是否开启R/W-1
- 高度-1
- 宽度-1
- 格式-1
- Mipmap-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
RGBA32/ARGB32格式资源：
35
RGB24格式资源：
30
常驻内存资源：
83
>1MB资源：
18
疑似冗余资源：
96
开启RW的资源数：
23
纹理资源内存
峰值：
166.07
MB
0
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
76.29
MB
152.59
MB
228.88
MB
总体资源内存占用
资源对象快照帧
纹理资源数量
峰值：
596
个
0
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
250
个
500
个
750
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
未关联
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
是否开启R/W
高度
宽度
格式
Mipmap
64MB
2
32940
true
4096
4096
Alpha8
1
3.64KB
2
32940
false
32
32
RGBA32
6
5.5KB
3
32940
false
32
32
RGBA32
6
3.64KB
2
32940
false
32
32
RGBA32
6
60.11KB
2
32940
false
87
87
RGBA32
1
6.66KB
2
32940
false
56
52
ETC2_RGBA8
1
128.98KB
2
32940
false
256
256
ETC2_RGBA8
1
8.89KB
2
32940
false
64
64
Alpha8
1
11.05KB
2
32940
false
64
64
Alpha8
1
8.89KB
2
32940
false
64
64
Alpha8
1
资源名称
font
Atlas
疑似泄漏
UISprite
疑似泄漏
Backgr
…（截断）
```
</details>

##### 子页签：网格资源 3
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- 网格资源内存 峰值： 48.32 MB 运行时网格占用的内存峰值。除了内存外，该指标还可能影响GPU渲染计算开销、GPU带宽、包体大小、加载效率，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 75 MB 推荐阅读 性能优 [已展开]
- 网格资源数量 峰值： 193 个 运行时同时存在于内存中的网格数量峰值。数量过多意味着可能资源未及时释放或缓存策略过于激进，对内存产生压力。 推荐阅读 性能优化，进无止境-内存篇（上）Unity加载模块深度解析（网格篇）如何优化资源，你还差
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- 是否开启R/W-1
- 骨骼数-1
- 顶点数-1
- Triangles数-1
- Normal数-1
- Tangents数-1
- Color数-1
- Blend Weight数-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
含有Color属性资源：
12
含有Tangent属性资源：
62
疑似冗余资源：
11
开启RW的资源数：
102
网格资源内存
峰值：
48.32
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
19.07
MB
38.15
MB
57.22
MB
总体资源内存占用
资源对象快照帧
网格资源数量
峰值：
193
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
80
个
160
个
240
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
未关联
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
是否开启R/W
骨骼数
顶点数
Triangles数
Normal数
Tangents数
Color数
Blend
Weight数
733.49KB
2
32940
false
0
2701
-1
-1
-1
-1
-1
21.18KB
17
32400
true
0
4
2
4
4
0
4
629.3KB
2
32940
false
0
2360
-1
-1
-1
-1
-1
725.91KB
2
32940
false
0
2706
-1
-1
-1
-1
-1
702.87KB
2
32940
false
0
2614
-1
-1
-1
-1
-1
246.49KB
2
13260
true
0
1051
611
1051
1051
0
1051
221.22KB
2
13260
true
0
915
802
915
915
0
915
20.22KB
2
8580
true
0
106
64
106
0
0
106
3.19KB
3
2400
true
0
0
0
0
0
0
0
22.08KB
13
7620
tru
…（截断）
```
</details>

##### 子页签：动画片段
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- 动画资源内存 峰值： 2.83 MB 运行时动画占用的内存峰值。除了内存外，该指标还可能影响动画更新耗时、包体大小、加载效率等，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 60 MB 推荐阅读 关于Unity中的资源管 [已展开]
- 动画资源数量 峰值： 92 个 运行时同时存在于内存中的动画数量峰值。数量过多说明可能资源未及时释放或缓存策略过于激进，对内存产生压力。 推荐阅读 关于Unity中的资源管理，你可能遇到这些问题
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- 时长-1
- Frame Rate-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
常驻内存资源：
26
疑似冗余资源：
7
动画资源内存
峰值：
2.83
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
1.14
MB
2.29
MB
3.43
MB
总体资源内存占用
资源对象快照帧
动画资源数量
峰值：
92
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
40
个
80
个
120
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
未关联
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
时长
Frame
Rate
33.55KB
2
32940
0.7
30
31.02KB
2
32940
0.7
30
41.83KB
2
32940
2
30
110.09KB
2
32940
0.53
30
29.83KB
5
1800
0.03
30
8.71KB
2
26400
1
30
69.24KB
2
3540
1.33
30
18.53KB
1
32940
1
30
14.23KB
1
32940
1.33
30
175.09KB
1
32940
2
30
资源名称
map_Mainrolemove
疑似泄漏
DarkerAction
疑似泄漏
male_idle_breath
疑似泄漏
run
疑似泄漏
Take
001
疑似冗余
Take
001
疑似冗余
Stand
疑似冗余
StandardBeaten
常驻
Mainrolestand_loop
常驻
Armed-Stunned
常驻
操作
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量

…（截断）
```
</details>

##### 子页签：音频片段 1
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- 音频资源内存 峰值： 38.8 MB 运行时音频占用的内存峰值。除了内存外，该指标还可能影响包体大小、加载效率等游戏性能，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 30 MB 推荐阅读 关于Unity中的资源管理，你 [已展开]
- 音频资源数量 峰值： 38 个 运行时同时存在于内存中的音频数量峰值。数量过多可能意味着资源未及时释放或缓存策略过于激进，对内存产生压力。 推荐阅读 关于Unity中的资源管理，你可能遇到这些问题
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- 时长-1
- Samples-1
- LoadType-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
常驻内存资源：
0
疑似冗余资源：
0
音频资源内存
峰值：
38.8
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
14.31
MB
28.61
MB
42.92
MB
总体资源内存占用
资源对象快照帧
音频资源数量
峰值：
38
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
15
个
30
个
45
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
未关联
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
时长
Samples
LoadType
42.78KB
1
32340
1.51
16622
DecompressOnLoad
163.32KB
1
32340
1.78
78336
DecompressOnLoad
33.29KB
1
32340
1.07
11763
DecompressOnLoad
30.22KB
1
32340
0.93
10191
DecompressOnLoad
95.82KB
1
32340
0.99
43776
DecompressOnLoad
19.62KB
1
32340
0.43
4765
DecompressOnLoad
20.98KB
1
32340
0.5
5459
DecompressOnLoad
91.32KB
1
32340
0.94
41472
DecompressOnLoad
109.32KB
1
32340
1.15
50688
DecompressOnLoad
15.01KB
1
32340
0.22
2404
DecompressOnLoad
资源名称
e05
36
atk09
e23
50
atk13
atk08
51
107
atk03
操作
内存
…（截断）
```
</details>

##### 子页签：材质资源
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 材质资源数量 峰值： 413 个 项目运行过程中，材质资源的数量最大值。 推荐值: < 1000
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- 材质资源内存 峰值： 1.45 MB 运行时材质占用的内存峰值。材质的内存占用相比其他资源不高，但该指标还是可能影响渲染模块耗时等，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 2 MB [已展开]
- 材质资源数量 峰值： 413 个 项目运行过程中，材质资源的数量最大值。 推荐值: < 1000
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- Shader-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
常驻内存资源：
36
疑似冗余资源：
53
材质资源内存
峰值：
1.45
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
585.94
KB
1.14
MB
1.72
MB
总体资源内存占用
资源对象快照帧
材质资源数量
峰值：
413
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
150
个
300
个
450
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
未关联
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
Shader
2.82KB
2
32940
Hidden/BlitCopy
13.61KB
9
32940
GUI/Text
Shader
13.34KB
2
32940
TextMeshPro/Distance
Field
4.59KB
2
32940
Legacy
Shaders/Particles/Alpha
Blended
5.62KB
2
32940
SkillEffect/Character
2.73KB
2
32940
Hidden/PostProcessing/CopyStd
3.59KB
2
32940
Nature/Terrain/Standard
7.77KB
2
32940
SkillEffect/Character
5.62KB
2
32940
SkillEffect/Character
5.62KB
2
32940
SkillEffect/Character
资源名称
Hidden/BlitCopy
疑似泄漏
Font
Material
疑似泄漏
font
Atlas
Material
疑似泄漏
decal.pointer
疑似泄漏
TaoLiuYi
疑似泄漏
PostPr
…（截断）
```
</details>

##### 子页签：Shader资源 2
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- Shader资源内存 峰值： 117.47 MB 运行时Shader占用的内存峰值。除了内存外，该指标还可能影响包体大小、加载效率等，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 50 MB [已展开]
- Shader资源数量 峰值： 170 个 运行时同时存在于内存中的Shader数量峰值。建议结合项目实际渲染需要评估Shader资源数量是否符合预期。
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- 硬件支持-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
常驻内存资源：
15
疑似冗余资源：
59
Shader资源内存
峰值：
117.47
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
47.68
MB
95.37
MB
143.05
MB
总体资源内存占用
资源对象快照帧
Shader资源数量
峰值：
170
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
60
个
120
个
180
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
未关联
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
硬件支持
1.98MB
2
32940
True
49.46KB
3
32940
True
2.64KB
2
32940
False
1.32MB
3
32940
True
1.73MB
3
32940
True
1.62MB
3
32940
True
35.57KB
3
32940
True
27.75KB
2
32940
True
2.8MB
2
32940
True
23.58KB
2
32940
True
资源名称
Custom/Leaf
疑似泄漏
Legacy
Shaders/Particles/Alpha
Blended
疑似泄漏
Hidden/Nature/Terrain/Utilities
疑似泄漏
Hidden/TerrainEngine/Details/Vertexlit
疑似泄漏
Hidden/TerrainEngine/Details/WavingDoublePass
疑似泄漏
Hidden/TerrainEngine/Details/BillboardWavingDoublePass
疑似泄漏
Hidden/TerrainEngine/Billboard
…（截断）
```
</details>

##### 子页签：字体资源 2
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- 字体资源内存 峰值： 60.23 MB 运行时字体占用的内存峰值。除了内存外，该指标还可能影响包体大小、加载效率等，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 20 MB [已展开]
- 字体资源数量 峰值： 8 个 运行时同时存在于内存中的字体数量峰值。数量过多可能意味着资源未及时释放或缓存策略过于激进，对内存产生压力。
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- 动态字体-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
>5MB资源：
6
动态字体资源：
15
疑似冗余资源：
2
字体资源内存
峰值：
60.23
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
23.84
MB
47.68
MB
71.53
MB
总体资源内存占用
资源对象快照帧
字体资源数量
峰值：
8
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
3
个
6
个
9
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
未关联
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
动态字体
29.48MB
2
2340
True
29.52MB
2
32340
True
10.55MB
1
32940
True
4.07MB
1
32940
True
642.38KB
1
32940
True
622.73KB
1
32940
True
61.25KB
1
540
True
70.96KB
1
60
True
14.73MB
1
32400
True
84.43KB
1
1740
True
资源名称
font
疑似冗余
font
疑似冗余
汉仪铁山隶书简
常驻
隶书
常驻
Lato-Bold
常驻
Lato-Medium
常驻
Arial
Arial
font
Arial
操作
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
内存占用
资源数量
12
```
</details>

##### 子页签：RenderTexture
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- RenderTexture资源内存 峰值： 28.33 MB 运行时RenderTexture占用的内存峰值。除了内存外，该指标还显著影响GPU渲染计算、GPU带宽等，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 60 [已展开]
- RenderTexture资源数量 峰值： 18 个 运行时同时存在于内存中的RenderTexture数量峰值。建议结合项目实际渲染需要评估RenderTexture资源数量是否符合预期。
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似泄漏 疑似冗余 常驻 AB打包冗余 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- 生命周期（帧数）-1
- 高度-1
- 宽度-1
- Depth-1
- 格式-1
- MSAA-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用
- 资源数量
- 内存占用

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
>5MB资源：
12
AA>1资源：
0
RenderTexture资源内存
峰值：
28.33
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
11.44
MB
22.89
MB
34.33
MB
总体资源内存占用
资源对象快照帧
RenderTexture资源数量
峰值：
18
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
8
个
16
个
24
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
生命周期（帧数）
高度
宽度
Depth
格式
MSAA
1.98KB
2
11580
0
0
0
-
0
6.67MB
2
7620
256
2048
16
ARGB32
1
4.2MB
1
25320
734
1500
0
RGB111110Float
1
8.4MB
1
600
734
1500
24
ARGB32
1
3.95KB
1
11520
22
46
0
RGB111110Float
1
3.95KB
1
11520
22
46
0
RGB111110Float
1
16.35KB
1
11520
45
93
0
RGB111110Float
1
16.35KB
1
11520
45
93
0
RGB111110Float
1
66.47KB
1
11520
91
187
0
RGB111110Float
1
66.47KB
1
11520
91
187
0
RGB111110Float
1
资源名称
SmallAsset
(<1KB)
疑似冗余
Tree
Imposter
Texture
疑似冗余
_TargetPool0
TempBuffer
3
1500x734
_Bl
…（截断）
```
</details>

##### 子页签：粒子系统
**  右侧子页签**
- 总览
- 纹理资源 3
- 网格资源 3
- 动画片段
- 音频片段 1
- 材质资源
- Shader资源 2
- 字体资源 2
- RenderTexture
- 粒子系统
- AssetBundle
- TextAsset
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  折叠面板 (el-collapse)**
- 粒子系统资源内存 峰值： 1.98 MB 运行时粒子系统占用的内存峰值。除了内存外，该指标还可能影响渲染模块耗时、粒子系统更新耗时等，建议结合截图、生命周期曲线、资源列表予以综合分析。 推荐值: < 18 MB [已展开]
- 粒子系统资源数量 峰值： 168 个 运行时同时存在于内存中的粒子系统组件数量峰值。作为一种SceneObjects对象，Hierarchy中粒子系统组建的数量直接影响其内存，需结合实际播放量综合判断是否存在缓存过度导致的浪费。 推荐值:
- 资源自定义看板
- RGB24格式的纹理资源数量较多
- 网格资源疑似冗余数量过高
- 字体资源存在冗余
- RGBA32格式的纹理资源数量较多
- 带有Tangent属性的网格资源数过多
- Shader疑似冗余数量过高
- 纹理资源疑似冗余数量过高
- 字体资源内存占用过高
- Shader内存占用过高
- 音频资源内存占用过高
- 开启Read/Write的网格资源过多

**  单选组**
- 显示相同资源
- 显示差异资源

**  表格列**
- 资源名称 疑似过度缓存 筛选 重置
- 内存占用峰值-1
- 数量峰值-1
- Playing组件数峰值-1
- Playing粒子数峰值-1
- 生命周期（帧数）-1
- 操作

**  操作按钮**
- 导出数据
- 指定帧对比
- 筛选
- 重置
- 内存占用
- 资源数量
- Playing粒子数
- 内存占用
- 资源数量
- Playing粒子数
- 内存占用
- 资源数量
- Playing粒子数
- 内存占用
- 资源数量

<details><summary>页面文本采样</summary>

```
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源内存
总览
纹理资源
3
网格资源
3
动画片段
音频片段
1
材质资源
Shader资源
2
字体资源
2
RenderTexture
粒子系统
AssetBundle
TextAsset
总览（0-32987帧）
指定帧
指定场景
Playing组件数峰值为0的粒子系统资源：
105
粒子系统资源内存
峰值：
1.98
MB
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
781.25
KB
1.53
MB
2.29
MB
总体资源内存占用
资源对象快照帧
粒子系统资源数量
峰值：
168
个
2.5k
5k
7.5k
10k
12.5k
15k
17.5k
20k
22.5k
25k
27.5k
30k
32.5k
0
5000
10000
15000
20000
25000
30000
0
个
60
个
120
个
180
个
总体资源数量
资源对象快照帧
资源自定义看板
具体资源使用情况：
（查看全部资源）
导出数据
对比模式
显示相同资源
显示差异资源
指定帧对比
内存占用峰值
数量峰值
Playing组件数峰值
Playing粒子数峰值
生命周期（帧数）
47.11KB
4
0
0
32760
35.33KB
3
0
0
32760
11.78KB
1
0
0
32760
12.02KB
1
0
0
32760
106.47KB
9
0
0
32760
35.33KB
3
0
0
32760
47.41KB
4
0
0
32760
11.8KB
1
0
0
32760
71.23KB
6
0
0
32760
84.12KB
7
0
0
32760
资源名称
CFX3_Hit_Light_B_Air
(Purple)/CFX3
Stars
CFX3_Hit_Ice_A_Air
(Purple)/CFX3
IceSmoke
CFX3_Hit_Light_B_Air
(Purple)
CFX3_Hit_Ice_A_Air
(Purple)
CFX3_Hit_Ice_A_Air
(Purple)/CFX3
Spikes
CFX3_Hit_Ice_A_Air
(Purple)/CFX3
Aura
ArrowHit/Flash
ArrowHit/Circle
ArrowHit/Smoke
ArrowHit
…（截断）
```
</details>

---

### 内存分析 · Lua 内存 `memory-lua` → UProfiler `#memory-lua`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/memoryAnalysis?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=lua`

#### 默认视图

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
卡顿分析2
内存分析11
内存占用
资源内存11
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
Lua内存
总体堆内存
堆内存具体分配
Mono对象引用
总览（0-32987帧）
指定帧
指定场景
LUA堆内存峰值
长按可拖拽排序
峰值：
866
KB
当前帧内存占用：630KB
(第0帧)
最大值：866KB
(第28260帧)
0
10k
20k
30k
2.5k
5k
7.5k
12.5k
15k
17.5k
22.5k
25k
27.5k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
390.63
KB
781.25
KB
1.14
MB
LUA堆内存
Table数量峰值
长按可拖拽排序
峰值：
1210
个
当前帧数量：1032个
(第0帧)
最大值：1210个
(第29000帧)
0
2k
4k
6k
8k
10k
12k
14k
16k
18k
20k
22k
24k
26k
28k
30k
32k
0
5000
10000
15000
20000
25000
30000
0
个
500
个
1000
个
1500
个
Table数量
Function数量峰值
长按可拖拽排序
峰值：
1082
个
当前帧数量：905个
(第0帧)
最大值：1082个
(第29000帧)
0
2k
4k
6k
8k
10k
12k
14k
16k
18k
20k
22k
24k
26k
28k
30k
32k
0
5000
10000
15000
20000
25000
30000
0
个
400
个
800
个
1200
个
Function数量
Userdata数量峰值
长按可拖拽排序
峰值：
39
个
当前帧数量：39个
(第0帧)
最大值：39个
(第0帧)
0
2k
4k
6k
8k
10k
12k
14k
16k
18k
20k
22k
24k
26k
28k
30k
32k
0
5000
10000
15000
20000
25000
30000
0
个
15
个
30
个
45
个
Userdata数量
```
</details>

#### 补充：右侧三子页签（2026-06-10 二次爬取）

**子页签导航**（非 el-tabs，为页面内文字链接触发）：
- 总体堆内存
- 堆内存具体分配
- Mono对象引用

**总体堆内存** — scope 工具条 + 可拖拽指标卡：
- 总览（0-32987帧）/ 指定帧 / 指定场景
- LUA堆内存峰值（峰值 866 KB，含曲线）
- Table数量峰值（峰值 1210 个）
- Function数量峰值（峰值 1082 个）
- Userdata数量峰值（峰值 39 个）
- 各指标卡支持「长按可拖拽排序」

**堆内存具体分配** — 表格列：
- 总堆内存分配 / 累计分配堆内存均值(每10K帧) / 调用次数 / 函数名 / 操作
- 按钮：查看堆内存分配
- 区块：Lua内存分配

**Mono对象引用** — 表格列：
- 对象个数 / Destroyed对象个数 / 对象名
- 按钮：对比模式
- 区块：Mono对象类型列表 (截止至第32000帧)

---

### 内存分析 · Mono 内存 `memory-mono` → UProfiler `#memory-mono`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/memoryAnalysis?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=mono`

#### 默认视图

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
卡顿分析2
内存分析11
内存占用
资源内存11
Lua内存
Mono内存
耗电量2
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
Mono内存
总览（0-32987帧）
指定帧
指定场景
Reserved
Mono峰值
峰值：
32.51
MB
当前帧内存占用：32.51MB
(第0帧)
最大值：32.51MB
(第0帧)
0
10k
20k
30k
2.5k
5k
7.5k
12.5k
15k
17.5k
22.5k
25k
27.5k
32.5k
0
5000
10000
15000
20000
25000
30000
0
B
11.44
MB
22.89
MB
34.33
MB
Mono
Reserved
Mono
Used
```
</details>

---

### 耗电量 `battery` → UProfiler `#battery`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/battery?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**右侧子页签**
- 功率峰值过高
- 电流峰值过高

**折叠面板 (el-collapse)**
- 功率峰值过高
- 电流峰值过高

**操作按钮**
- 下载

<details><summary>页面文本采样</summary>

```
温度变化量1
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
耗电量
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
电量
长按可拖拽排序
每万帧耗电均值：
1.21
%
当前帧：
96%
(第0帧)
最大值：
96%
(第0帧)
功率
1
长按可拖拽排序
峰值：
4550
mW
最大值：
4550mW
(第18720帧)
本次测试未检测到该指标
```
</details>

---

### 温度变化量 `temperature` → UProfiler `#temperature`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/temperature?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**右侧子页签**
- 电池温度峰值过高

**折叠面板 (el-collapse)**
- 电池温度峰值过高

**操作按钮**
- 下载

<details><summary>页面文本采样</summary>

```
自定义模块
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
温度变化量
总览（0-32980帧）
指定帧
指定场景
查看场景性能列表
综合温度
1
峰值：
52
℃
当前帧：
36.5℃
(第0帧)
最大值：
52℃
(第18720帧)
```
</details>

---

### 自定义模块 · 自定义面板 `custom-dashboard` → UProfiler `#custom-dashboard`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/customizedDashboard?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**操作按钮**
- 新建面板
- 下载

<details><summary>页面文本采样</summary>

```
自定义函数组
自定义变量
自定义代码段
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
自定义面板
发热降频分析
GPU压力导致的发热耗电分析
骁龙设备的顶点压力分析
UGUI的CPU端开销整合
新建自定义面板5
新建自定义面板6
新建自定义面板7
新建自定义面板8
新建面板
总览（0-32987帧）
指定帧
指定场景
查看场景性能列表
FPS
长按可拖拽排序
均值：
54.76
帧/秒
当前帧：
25帧/秒
(第0帧)
最大值：
57帧/秒
(第2460帧)
CPU频率
长按可拖拽排序
均值：
1340.4
MHz
最大值：
1955.75MHz
(第0帧)
GPU
Freq
长按可拖拽排序
均值：
397.41
MHz
最大值：
644MHz
(第5820帧)
综合温度
1
长按可拖拽排序
峰值：
52
℃
均值：
44.26℃
最大值：
52℃
(第18720帧)
```
</details>

---

### 自定义模块 · 自定义函数组 `custom-funcs` → UProfiler `#custom-funcs`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/apifuncs?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**表格列**
- 函数组名称
- 总耗时均值
- 自身耗时均值
- 耗时占比
- 函数数量
- 操作

**操作按钮**
- 新建自定义分组

<details><summary>页面文本采样</summary>

```
自定义变量
自定义代码段
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
自定义函数组
函数组列表
新建自定义分组
总览（0-32987帧）
指定场景
函数组名称
总耗时均值
自身耗时均值
耗时占比
函数数量
Logic
0.006
ms
0.004
ms
0.030
%
3
GameLogic
1.401
ms
0.602
ms
7.650
%
9
Coroutine
-
-
-
10
操作
详情
编辑
删除
详情
编辑
删除
详情
编辑
删除
1
各函数组CPU耗时
```
</details>

---

### 自定义模块 · 自定义变量 `custom-vars` → UProfiler `#custom-vars`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/apiinfo?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**区块标题**
- 无自定义变量数据

<details><summary>页面文本采样</summary>

```
自定义代码段
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
自定义变量
无自定义变量数据
如何获取
查看文档了解更多>>
精准定位性能问题
如何使用自定义参数定位性能问题？
```
</details>

---

### 自定义模块 · 自定义代码段 `custom-code` → UProfiler `#custom-code-frame`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/apicode-frame?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**区块标题**
- 无自定义参数数据

<details><summary>页面文本采样</summary>

```
资源管理
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
自定义代码段
无自定义参数数据
如何获取
查看文档了解更多>>
精准定位性能问题
如何使用自定义参数定位性能问题？
```
</details>

---

### 资源管理 · 汇总 `resource-summary` → UProfiler `#resource-summary`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/management?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=Summary`

#### 默认视图
**表格列**
- AB路径
- 加载方式
- 加载次数
- AB名称
- 卸载方式
- 卸载次数
- 资源路径
- 耗时
- 资源名称
- 调用次数

<details><summary>页面文本采样</summary>

```
AssetBundle加载&卸载
资源加载&卸载
资源实例化&激活
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源管理汇总
AssetBundle总加载次数
0.0
AssetBundle千帧加载次数
0.0
AssetBundle总卸载次数
0.0
AssetBundle千帧卸载次数
0.0
AssetBundle
加载次数
TOP
10
AB路径
加载方式
加载次数
在测试过程中未发生/未检测到该数据
AssetBundle
卸载次数
TOP
10
AB名称
卸载方式
卸载次数
在测试过程中未发生/未检测到该数据
Resources.Load千帧调用次数
0.2
Resources.Load千帧耗时（ms）
0.0
AssetBundle.Load千帧调用次数
16.3
AssetBundle.Load千帧耗时（ms）
3.7
资源加载次数
TOP
10
资源路径
加载方式
加载次数
assets/mods/sample/buildsource/head/0.png
AssetBundle.LoadAssetAsync
63
assets/mods/sample/buildsource/head/100.png
AssetBundle.LoadAssetAsync
27
assets/mods/sample/buildsource/head/101.png
AssetBundle.LoadAssetAsync
15
assets/mods/sample/buildsource/head/180.png
AssetBundle.LoadAssetAsync
15
assets/mods/sample/buildsource/head/80.png
AssetBundle.LoadAssetAsync
11
assets/mods/sample/buildsource/head/11.png
AssetBundle.LoadAssetAsync
9
assets/mods/sample/buildsource/head/154.png
AssetBundle.LoadAssetAsync
8
assets/prefabs/storyselectionitem.prefab
AssetBundle.LoadAsset
5
assets/prefabs/jyx2itemui.prefab
AssetBundle.LoadAsset
5
assets/mods/sample/buildsource/items/0.png
AssetBundle.LoadAssetAsync
5
资源加
…（截断）
```
</details>

---

### 资源管理 · AssetBundle 加载/卸载 `resource-ab` → UProfiler `#resource-ab`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/management?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=AssetBundle`

#### 默认视图
**区块标题**
- 具体资源使用情况 (所有资源)

**表格列**
- AssetBundle路径
- 调用接口
- 调用次数
- 耗时
- 操作

**操作按钮**
- 查看所有资源
- 导出CSV

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
资源管理汇总
AssetBundle加载&卸载
资源加载&卸载
资源实例化&激活
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
AssetBundle加载&卸载
AssetBundle.LoadFromFile
AssetBundle.LoadFromMemory
AssetBundle.Unload
AssetBundle.LoadFromStream
调用频率
0
次/千帧
历史趋势
AssetBundle.LoadFromFile
调用次数：0
AssetBundle.LoadFromFileAsync
调用次数：0
资源自定义看板
具体资源使用情况
(所有资源)
查看所有资源
导出CSV
AssetBundle路径
调用接口
调用次数
耗时
操作
暂无数据
1
前往页
```
</details>

---

### 资源管理 · 资源加载/卸载 `resource-load` → UProfiler `#resource-load`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/management?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=Resource`

#### 默认视图
**区块标题**
- 具体资源使用情况 (所有资源)

**表格列**
- 资源路径
- 调用接口
- 调用次数
- 耗时
- 操作

**操作按钮**
- 查看所有资源
- 导出CSV
- 调用次数
- 调用耗时
- 调用次数
- 调用耗时
- 调用次数
- 调用耗时
- 调用次数

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
资源管理汇总
AssetBundle加载&卸载
资源加载&卸载
资源实例化&激活
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源加载&卸载
Resources.Load
AssetBundle.LoadAsset
Resources.UnloadAsset
调用频率
0.21
次/千帧
历史趋势
Resources.Load
调用次数：7
Resources.LoadAsync
调用次数：0
Resources.LoadAll
调用次数：0
资源自定义看板
具体资源使用情况
(所有资源)
查看所有资源
导出CSV
资源路径
调用接口
调用次数
耗时
操作
Jyx2Configs/SettingsHelper.lua
Resources.Load
1
0.03
调用次数
调用耗时
Jyx2Configs/BattleHelper.lua
Resources.Load
1
0.04
调用次数
调用耗时
Jyx2Configs/ShopHelper.lua
Resources.Load
1
0.03
调用次数
调用耗时
perf/profiler.lua
Resources.Load
1
0.61
调用次数
调用耗时
Jyx2Configs/CharacterHelper.lua
Resources.Load
1
0.04
调用次数
调用耗时
Jyx2Configs/SkillHelper.lua
Resources.Load
1
0.03
调用次数
调用耗时
xlua/util.lua
Resources.Load
1
0.76
调用次数
调用耗时
1
前往页
```
</details>

---

### 资源管理 · 资源实例化/激活 `resource-instantiate` → UProfiler `#resource-instantiate`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/management?dataKey=20250521115551ncha0bb4727&project=1016&engine=1&category=Object`

#### 默认视图
**区块标题**
- 具体资源使用情况 (所有资源)

**表格列**
- 资源名称
- 调用次数
- 耗时
- 操作

**操作按钮**
- 查看所有资源
- 导出CSV
- 调用次数
- 调用耗时
- 调用次数
- 调用耗时
- 调用次数
- 调用耗时
- 调用次数
- 调用耗时
- 调用次数
- 调用耗时
- 调用次数

<details><summary>页面文本采样</summary>

```
AI问答
产品
价格
支持
关于我们
社区
登录
注册
性能简报
运行信息
场景概览
GPU分析
总体性能趋势4
卡顿分析2
内存分析11
耗电量2
温度变化量1
自定义模块
资源管理
资源管理汇总
AssetBundle加载&卸载
资源加载&卸载
资源实例化&激活
运行日志
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
资源实例化&激活
Object.Instantiate
Object.Destroy
GameObject:SetActive
调用频率
4.76
次/千帧
历史趋势
Object.Instantiate
调用次数：157
资源自定义看板
具体资源使用情况
(所有资源)
查看所有资源
导出CSV
资源名称
调用次数
耗时
操作
AmbientOcclusion
1
0.07
调用次数
调用耗时
Jyx2ItemUI
7
5.15
调用次数
调用耗时
Vignette
1
0.08
调用次数
调用耗时
ChatUIPanel
1
3.82
调用次数
调用耗时
StorySelectionItem
2
0.53
调用次数
调用耗时
Bloom
1
0.08
调用次数
调用耗时
GameMainMenu
1
11.62
调用次数
调用耗时
SelectRolePanel
1
3.21
调用次数
调用耗时
GameInfoPanel
1
0.49
调用次数
调用耗时
MessageBox
1
1.25
调用次数
调用耗时
123
前往页
```
</details>

---

### 运行日志 `log` → UProfiler `#log`

- **UWA URL**: `https://www.uwa4d.com/u/got/perfanalysis.html/log?dataKey=20250521115551ncha0bb4727&project=1016&engine=1`

#### 默认视图
**下拉框**
- 请选择

**操作按钮**
- All (320)
- Log (119)
- Warning (199)
- Error (2)
- Exception (0)
- Assert (0)
- 导出Log

<details><summary>页面文本采样</summary>

```
HUAWEI
PCTAL10
新功能
UWA
SDK版本
2.5.2.1
60
FPS
3
G
Unity
示例项目
/
Android
/
总体性能分析-荣耀
V20_2025/06/20_overview
/
运行日志
All
(320)
Log
(119)
Warning
(199)
Error
(2)
Exception
(0)
Assert
(0)
导出Log
帧数
场景
Log内容
287
0_MainMenu
1
x
LUA:
游戏没有在运行，unity协程没法正常工作
287
0_MainMenu
1
x
LUA:
Jyx2
Init
287
0_MainMenu
1
x
LUA:
Jyx2
增加模块:
Battle
287
0_MainMenu
1
x
LUA:
Jyx2
增加模块:
ConfigMgr
292
0_MainMenu
1
x
载入的配置表数量:
7
292
0_MainMenu
1
x
LUA:
Jyx2Configs/BattleHelper载入失败
292
0_MainMenu
1
x
LUA:
Jyx2Configs/CharacterHelper载入失败
292
0_MainMenu
1
x
LUA:
Jyx2Configs/SettingsHelper载入失败
292
0_MainMenu
1
x
LUA:
Jyx2Configs/ShopHelper载入失败
292
0_MainMenu
1
x
LUA:
Jyx2Configs/SkillHelper载入失败
共
227
条
1
2
3
4
5
6
23
前往页
堆栈信息
无堆栈信息
```
</details>

---

## 深度爬取 → Unity 数据格式补充

### threadStack_ （各线程 CPU 堆栈）

```json
{
  "threads": [
    {
      "name": "MainThread",
      "avgCpuMs": 16.5,
      "functions": [
        {
          "name": "PlayerLoop",
          "avgMs": 12.3,
          "totalMs": 405000,
          "selfMs": 1.2,
          "totalPct": 100,
          "selfPct": 9.8,
          "callCount": 32986,
          "callsPerFrame": 1.0,
          "frameCount": 32986
        }
      ]
    }
  ]
}
```

### moduleFuncStack_ （各模块函数堆栈，rendering/sync/scripting/ui/loading/physics/animation/particle）

```json
{
  "module": "rendering",
  "scope": "overview",
  "metrics": [
    {
      "label": "渲染模块 CPU耗时",
      "avgMs": 3.12,
      "peakMs": 260.15,
      "peakFrame": 5618
    }
  ],
  "stackMode": "module",
  "order": "forward",
  "functions": [
    {
      "name": "Camera.Render",
      "avgMs": 3.01,
      "totalMs": 99455.36,
      "selfMs": 5786.9,
      "totalPct": 100,
      "selfPct": 5.82,
      "callCount": 32986,
      "callsPerFrame": 1.0
    }
  ],
  "aiDiagnosis": [
    {
      "title": "Draw Call峰值过高",
      "severity": "Medium",
      "suggestion": "DrawCall峰值 < 350 个"
    }
  ]
}
```

### luaMemory_ （Lua 内存三子页签）

```json
{
  "tabs": {
    "heapOverview": {
      "metrics": [
        "LUA堆内存",
        "Table数量",
        "Function数量",
        "Userdata数量"
      ],
      "curves": [
        {
          "label": "LUA堆内存",
          "unit": "KB",
          "values": []
        }
      ]
    },
    "heapDetail": {
      "allocations": [
        {
          "type": "table",
          "sizeBytes": 1024,
          "count": 10
        }
      ]
    },
    "monoRefs": {
      "objects": [
        {
          "type": "LuaTable",
          "refCount": 5
        }
      ]
    }
  },
  "subTabs": [
    "总体堆内存",
    "堆内存具体分配",
    "Mono对象引用"
  ]
}
```

### briefAiDiagnosis_ （性能简报 B 类指标折叠 + AI 建议）

```json
{
  "metrics": [
    {
      "name": "GPU压力系数",
      "value": 36,
      "unit": "%",
      "industryRank": "优于83%",
      "optimizeCount": 0,
      "diagnosis": [
        {
          "severity": "Medium",
          "roles": [
            "程序",
            "美术",
            "策划"
          ],
          "title": "Draw Call峰值过高",
          "value": 567,
          "suggestion": "DrawCall峰值 < 350 个"
        }
      ]
    }
  ]
}
```

### resourceManagement_ 扩展（资源管理四子页 TOP10）

```json
{
  "summary": [
    {
      "type": "Texture",
      "count": 120,
      "memoryMB": 45.2
    }
  ],
  "assetBundle": [
    {
      "frame": 1200,
      "action": "Load",
      "name": "ui/main.ab",
      "durationMs": 45.2
    }
  ],
  "resource": [
    {
      "frame": 800,
      "action": "Load",
      "path": "Prefabs/Player",
      "durationMs": 12.1
    }
  ],
  "instantiate": [
    {
      "frame": 1500,
      "action": "Instantiate",
      "name": "Player(Clone)",
      "durationMs": 3.5
    }
  ]
}
```

---

## UProfiler 未完成功能清单（2026-06-10 现状）

> 对照当前报告 `report_2026_06_09_14_54_35.html` 与 UWA 深度爬取结构。  
> ✅ = 已完成；⚠️ = 部分完成 / 简化版；❌ = 未实现或仅占位。

### 总览

| 类别 | 状态 | 说明 |
|------|------|------|
| 左侧 34 个页签 + 白色侧栏 | ✅ | `ReportSidebarBuilder.cs` / `report.css` |
| 性能简报 / 运行信息 / 场景 / GPU / 内存 / 日志等主框架 | ✅ | 有 UWA 风格布局，部分页有真实数据 |
| 各模块性能独立页（8 个） | ✅ | 独立 `#module-*` section + 11 列函数表 |
| 各线程 CPU 堆栈 | ✅ | 6 子页签 + 10 列函数表 + 导出 |
| 性能简报 B 类指标折叠 + AI 诊断 | ✅ | el-collapse 式折叠 + 诊断映射 |
| Lua 内存三子页签 | ✅ | UI 骨架完成，需 `luaMemory_` 填数据 |
| 自定义模块 4 页 | ✅ | UI + 数据解析，需 Unity 上传 |
| 资源管理 AB/加载/实例化 | ✅ | 四页 UI + `resourceManagement_` 解析 |
| 同档次排名 / 部分云端交互 | ⚠️ | 同档次排名 / AI 解读仍待云端 |

---

### 一、UI 结构未完成（需改服务端 HTML / JS / CSS）

#### 1. 各模块性能独立页 `#module-rendering` … `#module-particle`

**现状**：8 个模块（渲染/GPU同步/逻辑/UI/加载/物理/动画/粒子）通过 `#module-time/{hash}` 在「模块耗时统计」内切换详情，缺少 UWA 独立页结构。

**待补**（每个模块页统一模式，见深度爬取 `rendering` ~ `particle`）：

- [x] scope 工具条：总览 / 指定帧 / 指定场景 +「查看场景性能列表」
- [x] 可拖拽指标卡网格（各模块专属 KPI，如 DrawCall、Camera.Render CPU 等）
- [x] 函数堆栈自定义面板 + 搜索函数
- [x] 本模块 / 其他模块 函数调用切换（radio）
- [x] 函数堆栈冰柱图（总览）— 占位容器，有数据时可接 ECharts
- [x] **11 列函数表**：函数名、耗时均值、总耗时、总体占比、自身耗时、自身占比、总调用次数、单次耗时、调用帧数、每帧调用次数、操作（Time/Call）
- [x] AI 诊断折叠项（如 Draw Call 峰值过高、GC.Collect 调用频率过高等）
- [x] 逻辑代码模块额外：**正序调用分析 / 倒序调用分析** radio
- [x] 侧栏链接改为独立 section 或完整 SPA 视图（当前 `data-module-nav` 仅切 module-time 内嵌视图）

**涉及文件**：`ReportSectionsBuilder.cs`（或新建各模块 Section）、`ReportHtmlBuilder.cs`、`report.js`、`report.css`

---

#### 2. 各线程 CPU 调用堆栈 `#thread-stack`

**现状**：单页 + 模块均值近似条形图 +「暂无线程堆栈数据」提示。

**待补**（见深度爬取 `threadstack`）：

- [x] 右侧 6 个子页签：线程总览 | MainThread | Audio Stream Thread | Render Thread | Loading.AsyncRead | Audio Mixer Thread
- [x] 线程总览：各线程 CPU 耗时条形图 + 线程 CPU 耗时均值
- [x] 各线程页：函数堆栈自定义面板 + 冰柱图 + **10 列函数堆栈表**
- [x] 子页签切换 JS（类似 UWA `el-tabs` 行为）
- [x] 导出堆栈按钮

**涉及文件**：`ReportSectionsBuilder.BuildThreadStackSection`、`report.js`

---

#### 3. 性能简报 B 类指标折叠 + AI 诊断 `#brief`

**现状**：KPI 网格 + B 类指标 flat 表格 +「仅显示优化项」JS 过滤。

**待补**（见深度爬取 `brief` expandedView）：

- [x] 9 个 B 类指标改为 **el-collapse 式可折叠行**（GPU压力系数、渲染耗时、逻辑代码、同步等待、UI、物理、动画、粒子、加载）
- [x] 展开后显示：严重等级（Medium/Low）、负责角色（程序/美术/策划）、具体问题标题、UWA 建议阈值
- [x] 行业水平 / 历史趋势 / 优化项列与 UWA 对齐（趋势可为占位）
- [x] 「仅显示优化项」过滤折叠行

**涉及文件**：`ReportSectionsBuilder.BuildBriefSection`、`report.js`（brief 过滤逻辑扩展）

**数据依赖**：`briefAiDiagnosis_`（Unity 上传）或从现有 `DiagnosisItems` 映射

---

#### 4. Lua 内存三子页签 `#memory-lua`

**现状**：scope 工具条 + 占位文案 + 空图表「LUA堆内存（占位）」。

**待补**（见深度爬取 `memory-lua` 补充爬取）：

- [x] 子页签导航：**总体堆内存 | 堆内存具体分配 | Mono对象引用**（文字链接，非 el-tabs）
- [x] **总体堆内存**：LUA堆内存 / Table数量 / Function数量 / Userdata数量 可拖拽指标卡 + 曲线
- [x] **堆内存具体分配**：表格（总堆内存分配、累计分配均值、调用次数、函数名、操作）+「查看堆内存分配」
- [x] **Mono对象引用**：表格（对象个数、Destroyed对象个数、对象名）+「对比模式」
- [x] AI 诊断折叠（4 项）

**涉及文件**：`ReportSectionsBuilder.BuildMemorySections`、`report.js`

**数据依赖**：`luaMemory_`

---

#### 5. 自定义模块 4 页 `#custom-*`

**现状**：4 个 section 均为「需 Unity 上传 xxx」占位；自定义面板仅预览运行信息指标。

**待补**：

- [x] `#custom-dashboard`：自定义指标卡 + 多曲线面板（用户配置的 panels/metrics）
- [x] `#custom-funcs`：自定义函数组列表 + 函数耗时表
- [x] `#custom-vars`：变量采样时间轴 / 帧-变量值表
- [x] `#custom-code`：自定义代码段时间段 + 耗时汇总

**涉及文件**：`ReportSectionsBuilder.BuildCustomSections`、`ReportDataLoader.cs`

**数据依赖**：`customDashboard_`、`apiFuncs_`、`apiInfo_`、`apiCodeFrame_`

---

#### 6. 资源管理 4 子页 `#resource-*`

**现状**：汇总页有 KPI 占位 + TOP10 空表；AB/加载/实例化三页整页占位文案。

**待补**（见深度爬取 `resource-summary` ~ `resource-instantiate`）：

- [x] `#resource-summary`：Resources.Load / AB.Load / Instantiate 千帧调用次数 KPI + 四类 TOP10 表
- [x] `#resource-ab`：AssetBundle 加载/卸载事件流表格（帧、场景、路径、耗时等）
- [x] `#resource-load`：Resources 加载/卸载事件流
- [x] `#resource-instantiate`：Instantiate / Activate 事件流
- [x] 帧范围筛选、导出（若 UWA 有）— scope 工具条 + 导出按钮占位

**涉及文件**：`ReportSectionsBuilder.BuildResourceManagementSections`

**数据依赖**：`resourceManagement_`

---

#### 7. 内存分析 · 资源内存 `#memory-resource`

**现状**：资源类型汇总趋势图 + 总览表；**无** UWA 10+ 资源类型子页签。

**待补**（见深度爬取 `memory-resource` rightTabViews）：

- [x] 子页签：总览 | 纹理资源 | 网格资源 | 动画片段 | 音频片段 | 材质资源 | Shader资源 | 字体资源 | RenderTexture | 粒子系统 …
- [x] 各类型：内存占用曲线 + 具体资源 TOP 表 + 展开详情

**涉及文件**：`ReportSectionsBuilder`、`report.js`

**数据依赖**：`resMemoryDistribution_` 扩展为按类型明细列表

---

#### 8. 卡顿分析 `#jank-func`

**现状**：卡顿帧汇总 + 分布图可用；重点函数页 tabs 在但无数据表。

**待补**：

- [x] GC.Collect / 加载 / 动画 / 物理 卡顿点 tab 切换
- [x] 重点函数表：函数名、总耗时占比、自身耗时占比、总耗时、自身耗时、总调用次数等（见深度爬取 `jankFunction`）
- [x] 与 `#thread-stack` / 模块函数表联动跳转

**涉及文件**：`ReportSectionsBuilder.BuildJankSections`、`JankAnalyzer.cs`、`report.js`

**数据依赖**：函数分析 Hook（`funcAnalysis_` 扩展卡顿分类）

---

#### 9. 场景管理 `#scene-management`

**现状**：每帧耗时曲线 + 场景表；无 `sceneInfo_` 时显示合并视图提示。

**待补**：

- [x] 真实场景分段名 / begin-end / 场景详情按钮
- [x] 小于 100 帧场景隐藏逻辑
- [x] 场景 markArea 与帧提示联动（部分已有 JS）

**数据依赖**：`sceneInfo_`（P0）

---

#### 10. GPU 带宽 `#gpu-bandwidth`

**现状**：基于 DrawCall/三角面估算，非真实 GPU Bandwidth 曲线。

**待补**：

- [x] GPU Total Bandwidth 真实曲线（有 `gpuBandwidth_` 时）
- [x] GPU Bound 标记与主要/次要指标分组（部分在 `#gpu-summary`）

**数据依赖**：`gpuBandwidth_`

---

#### 11. 交互与云端能力

- [ ] **同档次排名**按钮（`#brief` KPI 区域）— 需 UWA 云端 API 或自建排名服务
- [x] **优化任务队列**数字跳转 — 当前仅展示计数
- [ ] **AI 解读 / 对比分析** — UWA 助手侧栏，本地可不实现或做简化
- [x] 运行信息 **指标卡拖拽排序** — UI 文案已有，JS 未实现
- [x] 指定帧 / 指定场景 scope — 全局 scope 工具条（指定帧跳转 + 指定场景链到场景管理）

**涉及文件**：`report.js`、可选后端 API

---

### 二、Unity 数据上传未完成（有 UI 但缺数据 / 有占位）

| 优先级 | 文件前缀 | 影响页签 | 说明 |
|--------|---------|---------|------|
| **P0** | `sceneInfo_` | 场景概览、场景管理、各模块 scope | 真实场景表与 markArea |
| **P0** | `moduleTime_` | 模块耗时统计 | 各模块真实 CPU 耗时（当前部分估算） |
| **P0** | `threadStack_` | 各线程 CPU 堆栈 | 多线程函数堆栈 + 冰柱图数据 |
| **P1** | `moduleFuncStack_` | 8 个模块性能页 | 11 列函数表 + 正序/倒序 |
| **P1** | `briefAiDiagnosis_` | 性能简报 | B 类折叠 + AI 建议内容 |
| **P1** | `resourceManagement_` | 资源管理 4 页 | AB/Load/Instantiate 事件流 + TOP10 |
| **P1** | `funcAnalysis_`（扩展） | 卡顿重点函数、模块堆栈 | 卡顿分类 + 热点函数 |
| **P2** | `gpuBandwidth_` | GPU 带宽分析 | 真实带宽曲线 |
| **P2** | `luaMemory_` | Lua 内存 3 子页 | 堆内存/分配/Mono引用 |
| **P2** | `customDashboard_` | 自定义面板 | 用户自定义指标 |
| **P2** | `apiFuncs_` | 自定义函数组 | 函数组耗时 |
| **P2** | `apiInfo_` | 自定义变量 | 变量采样 |
| **P2** | `apiCodeFrame_` | 自定义代码段 | 代码段耗时 |
| **P2** | `resMemoryDetail_`（待定义） | 资源内存子页签 | 各资源类型 TOP 明细 |

格式详见上文「深度爬取 → Unity 数据格式补充」及「页签 UI 对照（字段级）」各节。

---

### 三、建议实现顺序

1. **P0 数据**：`sceneInfo_` → 场景两页立刻有真实内容
2. **P0 UI**：线程堆栈多 tab + 函数表结构（可先接 `funcAnalysis_` 近似）
3. **P0 UI**：8 个模块独立页 UI 骨架（11 列空表 + 冰柱图占位）
4. **P1**：性能简报折叠 + `briefAiDiagnosis_`
5. **P1**：`resourceManagement_` + 资源管理 4 页
6. **P2**：Lua 内存 / 自定义模块 / GPU 带宽 / 资源内存子页签

---

### 四、主要代码入口

| 文件 | 职责 |
|------|------|
| `ReportSectionsBuilder.cs` | 各页签右侧 HTML |
| `ReportSidebarBuilder.cs` | 左侧导航 |
| `ReportHtmlBuilder.cs` | 报告组装、模块详情 payload |
| `ReportDataLoader.cs` | Unity 上传解析 |
| `wwwroot/js/report.js` | 页签切换、图表、brief 过滤、模块导航 |
| `wwwroot/css/report.css` | UWA 白色主题样式 |
| Unity `UProfilerHost.cs` | 数据上传 |

---

## 2026-06-12 第二轮对照（UWA 示例 project=8115，dataKey=20260603164558ydx9262overview786）

> 数据源：Playwright 逐页签爬取 33 页，摘要见 `uwa/crawl/`。

### 一、本轮已实现

| 功能 | 实现位置 |
|------|---------|
| 运行信息：CPU频率 / 网络下载 / 网络上传指标卡 + 曲线 | `ReportSectionsBuilder.BuildBasicInfoSection` + `report.js`（cpufreq/netrecv/netsent） |
| 顶栏徽标：目标帧率（60 FPS）、网络制式（WIFI） | `ReportHtmlBuilder.BuildReportToolbar` |
| 报告「下载 JSON」按钮（导出全部结构化 payload） | `report.js initReportDownload` |
| 内存页 low memory 事件横幅 + 内存/PSS 曲线红色 markLine | `BuildMemorySections` + `chartPayload.lowMemoryFrames` |
| 卡顿分类细化：GC.Collect / Unload Unused / 动画 / 物理类卡顿帧卡片 | `JankAnalyzer` + `BuildJankSections` |
| 运行日志：Assert 过滤、导出 Log、点击行查看堆栈面板 | `BuildLogSection` + `report.js initLogFilters` |
| 模块/线程函数堆栈冰柱图（HTML 渲染，点击段落跳函数表行） | `report.js renderIcicle`（`data-module-icicle` / `data-thread-icicle`） |
| 模块指标卡支持非 ms 单位与统计口径（次 / 每帧均值） | `ModuleFuncStackMetricRow.Unit/StatLabel` |
| 资源管理三子页：接口统计条（千帧调用频率 + 各 API 次数）、资源聚合表、调用明细联动、导出 CSV | `BuildResourceEventSection` + `report.js` |
| 资源管理汇总：总加载/卸载次数、每帧加载率、耗时 TOP10 | `BuildResourceManagementSections` |

### 二、本轮新增 Unity 上传需求

#### hardwareInfo_{session}.txt （P1，已建临时数据）

```json
{
  "targetFrameRate": 60,
  "networkType": "WIFI",
  "samples": [
    { "frameIndex": 1, "cpuFreqMHz": 1804.8, "netSentKB": 3.2, "netRecvKB": 8.5, "lowMemory": false }
  ]
}
```

- Unity 端采集：`SystemInfo.processorFrequency`（静态频率，安卓动态频率需读 `/sys/devices/system/cpu/cpu*/cpufreq/scaling_cur_freq`）；网络收发可用 `UnityEngine.Networking` 统计或安卓 `TrafficStats`；low memory 接 `Application.lowMemory` 回调。
- 临时数据生成脚本：`scripts/gen_mock_hardware_info.py`（已写入会话 `2026_06_09_14_54_35`）。

#### 日志堆栈（P2）

- 当前 `log_` 仅一行文本，点击行的「堆栈信息」面板只能显示行本身。
- Unity 端建议：`Application.logMessageReceived` 的 `stackTrace` 参数随行上传，格式 `{原文}\n@stack\n{stackTrace}`，前端已按 `at xxx` 模式识别。

#### 临时数据脚本汇总

| 脚本 | 生成文件 | 说明 |
|------|---------|------|
| `scripts/gen_mock_hardware_info.py` | `hardwareInfo_{session}.txt` | CPU频率（含中段降频模拟）/网络/low memory |
| `scripts/gen_mock_resource_management.py` | `resourceManagement_{session}.txt`、`moduleFuncStack_{rendering,ui,loading}_{session}.txt` | 资源事件流 + 3 个模块函数堆栈 |

### 三、不好添加（暂不实现）

| UWA 功能 | 原因 |
|---------|------|
| GPU 渲染分析真实数据（Overdraw、像素填充率、GPU 计数器） | 依赖 Mali/Adreno 驱动计数器，Unity API 无法获取，需厂商 SDK |
| GPU 带宽真实读写分解（纹理/顶点/FB 带宽） | 同上，目前仅有估算曲线 |
| 耗电量硬件级测量（mAh 拆解到 CPU/GPU/屏幕） | 需外接功耗板或厂商电池服务，软件侧只能近似 |
| AI 智能诊断（行业大盘对比、推荐值） | UWA 服务端模型 + 行业数据库，本地无对应数据源 |
| 重点函数分析的源码级归因（C# 行号热点） | 需 IL2CPP 符号化与采样式 Profiler，超出当前上传协议 |
| 温度变化量页的多传感器（电池/外壳温度） | `SystemInfo` 无此 API，安卓需逐厂商 HAL 适配 |
