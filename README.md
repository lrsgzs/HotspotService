# HotspotService

![HotspotService 图标](HotspotService/icon.png)

HotspotService（移动热点守护助手）是一个面向 ClassIsland 的 Windows 移动热点守护插件。它把“系统热点应该保持开启还是关闭”抽象成一个可持续维护的守护目标，并在后台定时检查当前系统状态，必要时自动把热点拉回目标状态。

## 项目背景

在教室、一体机或值班设备场景里，词典笔、平板和其他临时联网设备往往依赖 Windows 的“移动热点”功能。实际使用中最常见的问题并不是不会开热点，而是：

- 每次开机都要手动打开一次热点；
- 热点被意外关闭后，往往不能第一时间发现；
- 想把热点开关纳入 ClassIsland 自动化与规则集时，缺少统一入口。

HotspotService 当前版本聚焦于“热点开/关守护”本身，帮助减少重复人工操作。连接设备数量统计、异常连接清理和超载自动重启等能力目前仍属于后续扩展方向。

## 基本概念

- 守护开启/关闭：守护开启后，后台服务每 10 秒检查一次系统热点状态；守护关闭时，只记录状态，不主动干预系统热点。
- 守护目标：插件期望系统热点维持的目标状态，可选为“开启”或“关闭”。

## 当前功能

- 支持 ClassIsland 启动后按配置自动开启守护
- 支持设置启动时默认守护目标
- 支持在设置页手动开启或关闭守护
- 支持在设置页手动修改当前守护目标，并立即应用
- 支持显示守护状态、守护目标、系统热点状态、最近检查时间和最近错误
- 支持通过自动化动作开启守护、关闭守护、修改守护目标
- 支持在规则集中判断守护状态、守护目标和系统热点状态
- 当系统热点状态与守护目标不一致时，自动执行同步并在后续轮询中重试

## 功能实现情况

- [x] 基础守护服务
- [x] 手动修改守护目标
- [x] 手动启停守护服务
- [x] 自动化中使用插件功能（启停守护、修改守护目标）
- [x] 规则集中使用插件功能（判断守护状态、判断守护目标、判断系统热点状态）
- [x] 设置页展示当前守护状态与最近检查结果
- [ ] 主界面展示组件
- [ ] 连接设备数量统计与清理策略
- [ ] 基于连接数量或异常状态的自动重启热点策略

## 项目结构

- `HotspotService/`：插件主体、设置页、自动化扩展与资源文件
- `HotspotService.Tests/`：核心协调逻辑与设置存储测试
- `HotspotService.BehaviorTests/`：面向行为的补充测试

## 开发与验证

```powershell
dotnet build .\HotspotService.slnx
dotnet run --project .\HotspotService.Tests\HotspotService.Tests.csproj
dotnet run --project .\HotspotService.BehaviorTests\HotspotService.BehaviorTests.csproj
```

## 许可证

本项目基于 GPL-3.0 许可证发布，详见 [LICENSE](HotspotService/LICENSE)。
