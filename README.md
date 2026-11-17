# NilCM

网易云音乐第三方桌面客户端 · **学习项目** · Avalonia 12 / .NET 10

- 架构与全部决策：见 [ARCHITECTURE.md](ARCHITECTURE.md)（先读它，再动代码）
- 构建与验证：

```bash
dotnet build                          # 全解决方案编译
dotnet run --project src/NilCM.App    # 运行
dotnet test                           # 全部测试
```

- 声明：仅供学习研究，不分发、不商用。
- License：GPL-3.0（见 LICENSE）

## 版本与发布约定

- 版本 tag 采用语义化：`vMAJOR.MINOR.PATCH`，始终打 **annotated tag** 并附说明；
- 每次 push 同步更新下方「更新日志」；
- **push 仅在仓库所有者明确授权后执行**（未经明确允许严禁 push）。

## 更新日志

- **v0.2.0** — 功能层五连纵切片：
  - 纵切片 #1「歌词全链」：Core 新增 `LyricLine` / `LyricParser`（LRC 纯函数解析，含 offset/多时间戳/排序）/ `ILyricsApi` / `LyricsService`；`NilCM.Netease` 实现歌词接口（DTO 不出程序集，录制 fixture 契约测试）；`NilCM.App` 组合根 DI 接线 + 最小歌词查看界面。
  - 纵切片 #2「播放全链」：Core 新增 `IAudioPlayer` / `IPlayUrlResolver` / `PlaybackService` / `PlaybackState` / `PlayUrlResult`（含试听标记）/ `IAuthApi` / `ITokenStore`；`NilCM.Netease` 实现 weapi 加密（双重 AES-CBC + RSA 无填充）与播放地址解析；`NilCM.Playback` 以 LibVLCSharp 3.10.2 实现 `IAudioPlayer`（引擎线程差异封闭在适配器内，UI 轮询规避跨线程）；`NilCM.Infrastructure` 落地 `JsonFileTokenStore`；App 播放/暂停/继续/停止/音量/进度控制。**播放已激活**：导入登录 cookie 后黑胶 VIP 账号含 VIP 曲目均出 CDN 直链。
  - 纵切片 #3「播放队列 + 本地文件」：`PlayQueue` / `QueueMode`（顺序/列表循环/单曲循环，userSkip 手动切歌语义）；队列 UI（▶ 高亮当前）/ 本地文件选择器（VM 零 Avalonia 类型）/ 播完自动连播。
  - 纵切片 #4「随机播放 + 扫码登录功能层」：`QueueMode.Random` 整表全随机（Fisher-Yates，无“最近 N 首”截断；轮完自动重洗）；扫码登录 eapi 状态机落地（unikey 必需字段待补全，诊断探针内置）。
  - 纵切片 #5「目录功能层」：搜索 / 日推 / 歌单分组（创建 + 收藏）/ 歌单详情 / 专辑 / 歌手主页 / 评论分页（`ICatalogApi` / `IUserApi` / `ICommentApi` + 8 模型 + 容错映射适配器）；`LyricLocator` 歌词定位底座（桌面歌词 UI 阶段复用）。
  - 实连验证：VIP 账号播放直链、日推 30 首、歌单 14 创建 + 29 收藏、Beyond 歌手页 50 热门 + 50 专辑、评论 51665 条分页。
- **v0.1.0** — 首个里程碑：架构文档 v0.4（质量属性预算 / DovaBox 本地点歌联动契约 §6.1）+ 可编译解决方案骨架（net10.0 · Avalonia 12.1.6 · 中央包管理 · 三个测试项目 · build/test 全绿）。

