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

- **v0.1.0**（本地已就绪，待所有者授权推送）— 首个里程碑：架构文档 v0.4（质量属性预算 / DovaBox 本地点歌联动契约 §6.1）+ 可编译解决方案骨架（net10.0 · Avalonia 12.1.6 · 中央包管理 · 三个测试项目 · build/test 全绿）。

