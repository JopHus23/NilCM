# NilCM 架构规划（仅规划，不含实现）

> 网易云音乐第三方桌面客户端 · AvaloniaUI。学习者项目，按工业实践对标。
> v0.4 · 2026-11-17 · 变更：新增 §6.1 外部联动契约（对接 DovaBox 本地点歌）；§1 树与依赖图加入规划项目 NilCM.Ipc；README 建立版本/发布约定

## 0. 前提：两个决定切分方式的外部现实

架构不是从模式清单里挑出来的，是从这个项目特有的风险里长出来的。本项目风险最大的两件事：

1. **数据源是逆向工程产物，且生态正受版权压力**（全系统最易腐的部件）
   - 标志性事实：最著名的社区 API（Binaryify/NeteaseCloudMusicApi）已于 2024-04 归档停维护；NuGet 上的 C# 移植多数停更（wwh1004 版停在 2021），少数仍在维护（HyPlayer.NeteaseProvider，2026 年仍发版）。
   - 含义：API 包会死、会失效、要换，是常态不是意外。
2. **.NET 没有统一的跨平台音频引擎，Avalonia 本身不带播放能力**（全系统最易换的部件）
   - 当前跨平台成熟解是 LibVLCSharp（LGPL，活跃维护）；纯音频播放不需要它的任何 UI 集成层。
   - 含义：播放引擎存在按平台换型、按能力升级的真实可能。

**架构主轴一句话：把"易腐的"关进可以整体替换的囚笼，把"易换的"压到一个接口背后，其余一切从简。**

### 0.1 产品定位与质量属性（v0.2 新增）

一句话定位：**低占用、简约而精致、配置级高度自定义的跨平台音乐客户端。**

定位必须写成可测约束，否则"简约精致"会退化成口号、"高度自定义"会退化成过早的插件框架。工业做法：质量属性是架构的第一驱动力，每条都要落到"可测目标 + 架构含义"。

| 质量属性 | 可测目标（初版预算，首跑后校准） | 对架构的具体约束 |
|---|---|---|
| 低占用·内存 | 空闲 ≤ 120 MB；播放+浏览列表 ≤ 250 MB（Windows x64 估算） | 无常驻后台进程；专辑封面按需解码 + 有上限的内存缓存（App 层统一图片管线）；网络响应流式反序列化，不整包读入 |
| 低占用·启动 | 冷启动 ≤ 1.5 s（SSD，到主界面可交互） | 发布启用 ReadyToRun + 裁剪；首屏只渲染首页，其余页面惰性加载 |
| 低占用·体积 | 安装包 ≤ 80 MB（含 native 引擎） | LibVLC native 库是最大头（估 50-70 MB）；超预算即触发引擎换型评估（§7.2）——引擎可插拔的第二个动机 |
| 简约精致 | 不设数字指标，用机制保障 | ① design token：色彩/间距/圆角/字号全部收敛为 App 资源字典令牌，控件零魔法值；② 动效只用 Avalonia 过渡/Composition 动画，不堆第三方动效库；③ 第三方 UI 库从严，最多引入一个控件库 |
| 高度自定义 | v1 = 配置级 | ① token 主题引擎：accent/明暗/密度全走 token；② 行为开关 + 全局快捷键（要求操作命令化）；③ **明确不做：插件系统**（脚本/热加载/第三方扩展）——范围外决策，高度自定义完全通过配置级达成 |

自定义范围的边界（已定，产品决策）：**本项目自定义 = 用户改配置改变外观与行为；"第三方代码改变功能"（插件化）明确不做**——需要 API 版本化、沙箱、稳定性承诺，成本高一个数量级，超出本项目定位。

## 1. 项目划分与依赖方向

```
NilCM.sln
├── README.md                       # 入口：构建命令与声明
├── ARCHITECTURE.md                 # 本文档：架构与全部决策
├── Directory.Packages.props        # 中央包版本管理（零成本工业惯例）
├── src/
│   ├── NilCM.App                   # Avalonia 宿主 + UI + ViewModel + DI 组合根
│   ├── NilCM.Core                  # 领域模型 + 全部抽象接口 + 播放编排（纯逻辑）
│   ├── NilCM.Netease               # 网易 API 适配器（易腐囚笼）
│   ├── NilCM.Playback              # 音频引擎适配器（易换插件）
│   ├── NilCM.Infrastructure        # 设置/凭据持久化（预留：离线缓存）
│   └── NilCM.Ipc                   # 【规划中·联动落地时新增】本地播放控制 API（入站适配器 → Core）
└── tests/
    ├── NilCM.Core.Tests            # 队列/码率回退/LRC 解析等纯逻辑
    ├── NilCM.Netease.Tests         # 录制 JSON fixture 的契约测试
    └── NilCM.App.Tests             # ViewModel 测试（可后补）
```

依赖方向（箭头 = "引用/知道"）：

```
              NilCM.App   ← 唯一允许知道所有具体实现的地方
             /    |    \     \
            ▼     ▼     ▼     ▼
   NilCM.Netease  NilCM.Playback  NilCM.Infrastructure  NilCM.Ipc*
            \     |     /       /
             ▼    ▼    ▼      ▼
             NilCM.Core   ← 零依赖

   * NilCM.Ipc 为规划中项目，联动落地时新增，依赖方向同实现项目（→ Core）。
```

只有三条规则，全部可被编译器强制：

1. **Core 不引用任何项目、零第三方 NuGet 包**（BCL 除外）。
2. **实现项目只引用 Core**，方向永远向内；它们互相之间不认识。
3. **App 是唯一引用全部实现的项目**——"具体用哪个实现"这个脏知识必须收敛在组合根一处，靠 DI 注册装配。

> 学习者注：想再砍一刀，Infrastructure 初期可并入 App（就是写个 JSON 设置文件），等离线缓存立项时再拆出；Netease/Playback 不建议合并，它们是这套架构的全部意义。

## 2. 各项目职责

### NilCM.Core（无依赖）
- **放**：领域模型 Track / Playlist / User / LyricLine / SearchResult——干净模型，绝不携带网易 JSON 的脏字段（privileges、多层嵌套 data、魔法值）。
- **放**：全部抽象接口（见 §4）。
- **放**：应用编排（纯逻辑、可单测）：
  - `PlaybackService`：主用例"播一首歌"的装配线——取播放 URL → 码率回退 → 喂给 IAudioPlayer → 报告状态；
  - `PlayQueue`：队列、循环模式、随机重排；
  - `LyricParser`：LRC 文本 → LyricLine[]（纯函数）。
- **不放**：任何 HTTP、任何文件 IO、任何 UI 概念。

### NilCM.Netease（引用 Core）
- HttpClient 管线：weapi/eapi 加密、cookie 管理、重试。
- **DTO 与 Core 模型的映射；DTO 永不出本程序集**——这是本项目存在的全部意义。
- 登录流程（二维码轮询、匿名 cookie）。
- 实现 Core 的 IMusicApi 系列接口。
- 内部路线两条、随时可换：包一层现成 C# 移植包（快），或自实现加密层（学得多）。无论哪条，泄漏面都限制在本项目内。

### NilCM.Playback（引用 Core）
- 实现 IAudioPlayer；引擎默认 LibVLCSharp（纯音频无 UI 集成层负担）。
- 引擎专属的坑全部关在这里：预加载/缓冲、native 库加载、引擎异常转译成 Core 的 PlaybackError——App 永远看不到 VLC 的类型。

### NilCM.Infrastructure（引用 Core）
- ISettingsStore 实现：JSON 落 `%APPDATA%`。
- ITokenStore 实现：cookie 持久化（Windows 用 DPAPI 加密；macOS/Linux 等价方案是已知债务，先明文 + 文档标注）。
- 【预留位】离线缓存实现落点，现在不写。

### NilCM.App（引用所有）
- Avalonia 程序生命周期、DI 组装（Microsoft.Extensions.DependencyInjection）。
- Views + ViewModels（CommunityToolkit.Mvvm）。
- 导航：主窗口内简单页面切换，不引导航框架；主题用资源字典（换肤预留点）。
- **两条纪律**：
  1. ViewModel 不出现任何 Avalonia 控件类型，只依赖 Core 的接口/服务。理由：这是"将来换 UI 框架/出移动端"最便宜的保险——一旦破戒，迁移成本从"搬目录"变"重写"。
  2. 引擎/网络回调线程切回 UI 线程，只发生在 App（Avalonia Dispatcher 属表现层细节）。

## 3. 边界为什么这样切

**切分依据是变化速率与爆炸半径，不是技术名词：**

| 项目 | 变化来源 | 频率 | 爆炸半径（若泄漏） | 对策 |
|---|---|---|---|---|
| NilCM.Netease | 网易改接口、API 包死掉 | 高、不可控 | 全项目重写 | 独立囚笼 + DTO 映射 + 契约测试 |
| NilCM.Playback | 引擎换型、OS 音频差异 | 低、但伤筋动骨 | Core/App 全改 | 单接口隔离 |
| NilCM.Core | 产品需求（业务规则） | 中、可控 | 自身 | 纯逻辑可测 |
| NilCM.App | UI 改版 | 高、只影响自己 | 自身 | 组合根收敛 |

- **为什么 IMusicApi 按能力拆而不是一个巨型接口**：接口隔离。哪块坏了只换哪块实现；巨型接口会让"歌词接口挂了"也要动整个 API 实现，且天然滑向 god-interface。
- **为什么组合根唯一**：抽象不免费（每个接口都有映射与维护成本）。只对高频不可控部件收"过路费"，设置存储、导航这类低危部件不过度抽象——这就是"最简方案"的具体含义。
- **对照工业完整版 Clean Architecture（Domain/Application/Infrastructure/Presentation + CQRS）砍掉了什么**：
  - 砍独立 Application 层：现在只有"播一首歌"一条主用例，PlaybackService 放 Core 够用；出现第二条复杂链路（歌单云同步、每日推荐）再拆（§7.4）。
  - 砍 Repository/UnitOfWork：数据源是远端 API 不是数据库，在这里是仪式不是价值。
  - 砍独立 Presentation 项目：ViewModel 留在 App，靠纪律而非项目引用隔离；把纪律升级为项目边界是 §7.5 的重构动作，不是现在的事。

## 4. 关键接口最小集

```
ICatalogApi      搜索 / 歌单 / 专辑 / 艺人 / 每日推荐
ILyricsApi       歌词（原始 LRC 文本）
IPlayUrlResolver 给定 Track + 音质等级 → 播放 URL（含 VIP/试听回退策略）
                 ★ 未来离线缓存的装饰器挂点
IAuthApi         二维码登录 / 匿名 cookie
IUserApi         用户歌单 / 收藏 / 红心

IAudioPlayer     Load / Play / Pause / Resume / Seek / Volume / Stop
                 + Position / Duration 属性
                 + StateChanged / PositionChanged 事件（进度条与未来桌面歌词共用）
                 + 失败语义：PlaybackError（可区分 无版权 / 加载失败 / 解码失败）

ISettingsStore   读写强类型 Settings
ITokenStore      读写登录凭据（cookie）
```

## 5. 技术选型（每项一句话理由）

| 选型 | 理由 |
|---|---|
| Avalonia 12.x（当前稳定线，模板 12.1.6） | 跨平台 XAML，MVVM 生态成熟，学习主攻目标 |
| CommunityToolkit.Mvvm | Avalonia 生态事实标准，源码生成器减少样板 |
| Microsoft.Extensions.DependencyInjection | Avalonia 模板默认 DI 友好，不引第三方容器 |
| System.Text.Json | BCL 自带，零额外依赖 |
| LibVLCSharp（+ VideoLAN native 库包） | 跨平台音频唯一成熟解；纯音频不需要 UI 集成层 |
| xUnit + 录制 fixture | 契约测试不真连网，可重复、可离线 |
| Directory.Packages.props | 中央版本管理，多项目零成本统一 |
| ReadyToRun + 裁剪（发布配置） | 低占用的启动/体积预算来源（§0.1） |
| Serilog / Polly | 都不引。Console + ILogger 默认实现、手写重试足够；需求出现再加 |

## 6. 为未来预留的扩展点（现在只留缝，不写码）

| 扩展 | 预留的缝 | 届时的动作 | 成本预估 |
|---|---|---|---|
| 离线缓存 | IPlayUrlResolver（装饰器挂点） | 新写 CachePlayUrlResolver 包住真实实现，DI 换注册 | 低 |
| 桌面歌词 | LyricLine[] 时间轴 + IAudioPlayer.Position 事件（进度条本来就要用） | App 新增透明置顶无边框窗口，按 Position 滚动 | 中：架构零成本，真正的坑在三平台窗口透明/点击穿透互操作（Windows 需 WS_EX_TRANSPARENT / WS_EX_LAYERED） |
| SMTC / 系统媒体键 | PlaybackService 已聚合状态事件 | 新增 NilCM.Platform 项目接 Windows.Media / MPRIS | 低-中 |
| 主题自定义深化 | design token 资源字典（§0.1）已预留 | v1 内置 accent/明暗/密度；第三方主题包 = 运行时加载 axaml 资源字典包，需主题引擎版本化 | 低→中 |
| 音质/VIP 回退 | IPlayUrlResolver 策略位 | 已预留 | 零 |
| 第二条用例链（云同步、每日推荐） | — | Core 拆出 Application 层 | 中 |

**明确不预留**（防过度设计）：多账号、均衡器、**插件系统（明确不做）**等。预留仅限"外部强加、时间点无法预测"的变化（API、引擎、OS）；产品自有功能迭代不需要预留——那是正常开发。

### 6.1 外部联动：本地点歌 API（对接 DovaBox，v0.4 新增）

**构想**：DovaBox（独立的桌面工具合集，含 B 站弹幕姬）负责接收直播弹幕、识别"点歌"指令；NilCM 在本机暴露**通用本地播放控制 API**，DovaBox 调用它完成搜索与播放。

**三条边界判断（本设计的全部价值所在）**：
1. **进程边界 = 产品边界**：联动走本地契约，不做代码级复用——DovaBox 绝不引用 NilCM.Core，否则两个产品被迫同步演进，Core 会被弹幕需求污染。
2. **NilCM 不知道"弹幕"的存在**：API 语义是"本地播放控制"（搜索并播放 / 队列 / 传输控制 / 正在播放查询）。弹幕姬只是调用方之一，未来手机遥控、OBS 集成复用同一 API。
3. **业务规则各归其主**：点歌侧规则（"点歌 xxx"指令解析、按用户限频、黑名单）留在 DovaBox；播放侧规则（搜索匹配、码率回退、队列排序）留在 NilCM，DovaBox 不越界。

**新增项目 NilCM.Ipc（入站适配器）**：
- 依赖方向同实现项目：只引用 Core，调用 `PlaybackService` / `ICatalogApi` / `PlayQueue`——外部命令与 UI 按钮走**完全相同的用例缝**，绝不触碰 App/ViewModel。
- 技术：HttpListener（进程内、零额外运行时依赖，契合 §0.1 低占用预算）；遇瓶颈换 Kestrel，同样被关在项目内。
- 生命周期由 App 启停（设置开关 + 托盘项）。

**契约（`/v1/` 前缀 = 协议演进的廉价保险）**：
- `POST /v1/requests` `{query, requestedBy?}` → 同步返回 `{track, queued|now}`。同步响应即满足"回弹幕：已加入队列：晴天-周杰伦"，**无需推送通道**（SSE/WebSocket 明确不做，出现需求再谈）。
- `GET /v1/now-playing`、`POST /v1/transport {action: pause|resume|skip}`——预留语义，实现随纵切片走。
- 端口默认固定值 + 设置可改；令牌与端口写入本地 endpoint 发现文件，DovaBox 读取配对。

**安全三定律（本机 API 不可省）**：
1. 只绑 `127.0.0.1`，永不 `0.0.0.0`；
2. 令牌鉴权：首启随机生成 token——同时天然防浏览器 DNS rebinding 打 localhost；
3. 外部请求只能"查询/入队"，不能绕过 `PlayQueue` 直接操作引擎。

**明确不预留**：跨机访问、公网暴露、调用方账号体系——出现即视为新需求重谈契约。

## 7. 什么时候这套架构需要重构

**7.1 预期内失效——不叫重构，叫替换**
API 包死了 / 网易小改接口 → 只动 NilCM.Netease 内部（换实现、修加密、更新映射）。
可操作的边界检验标准：**Netease.Tests 的 fixture 全部更新后，Core/App 一个文件不改、测试全绿 = 边界守住了。**

**7.2 预期内换型——结构性小重构**
需要多平台播放器适配（gapless 无缝播放、WASAPI 独占、DSP）→ Playback 拆成 NilCM.Playback.LibVlc / NilCM.Playback.Windows；IAudioPlayer 版本化扩展（能力探测，如 Supports(Capability.Gapless)）。Core/App 基本不动。这正是把引擎独立成项目时提前买好的保险。

**7.3 契约崩塌——唯一预计的大修**
网易接口迁移到客户端无法复现的方案（强设备指纹绑定、官方开放平台 only、全走社区代理）→ IMusicApi / IPlayUrlResolver 契约本身要重谈，Core 波及。这是本架构唯一诚实承认的"赌输"情形，因为合同物（API）不受我们控制。缓解：Core 模型保持数据源无关（Track 不含网易专有字段），为换数据源留死口。

**7.4 用例膨胀——经典 Clean Architecture 化**
复杂业务链路 ≥ 3 条 → 从 Core 拆 NilCM.Application（用例层），PlaybackService 迁入。机械性重构。

**7.5 换 UI 框架 / 出移动端**
ViewModel 迁出 App 成独立 NilCM.Presentation。只要 §2 两条纪律没破，就是搬目录级别的工作量。

**7.6 离线缓存规模化**
淘汰策略、schema 迁移 → Infrastructure 引入 SQLite。其他层无感。

**失效模式总结**：这套架构赌"易变物都能被接口关住"。外部契约层面赌输只有一种——API 契约本身崩塌（7.3）；内部增长层面（7.4-7.6）都是预留了减震器的渐进重构。而契约崩塌时，重构代价已被诚实暴露在 Core 的接口面上，可估算、可谈判。

## 8. 已定决策 / 待定决策

- **已定**：五项目切分；三条依赖规则；组合根唯一；ViewModel 不沾 Avalonia 类型；DTO 不出 Netease 程序集；接口按能力拆分；**自定义范围 = 配置级（token 主题/密度/快捷键/行为开关）；插件化明确不做（产品决策，范围外）**。
- **待定**（不阻塞开工）：播放引擎最终选型（先 LibVLCSharp 跑通，接口在，随时换）；API 实现路线（包包装起步 vs 自实现学习，可先包后研）；§0.1 性能预算数值待首跑校准。
- **合规提示**（工业实践含法律意识）：第三方客户端与逆向 API 处于灰色地带，本项目仅作学习用途，不分发、不商用；这也是对 API 层做"整体可替换"设计的原因之一——它可能在任何一天必须被关掉。

