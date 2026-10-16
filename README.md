# ZZZKeyEcho

《绝区零》输入显示工具：把键盘 / 鼠标 / 手柄输入实时画在屏幕上；并支持用 OBS「浏览器源」单独叠加（真透明背景，主播屏幕和观众画面一致）。

## 技术栈

- **.NET 8 / C# / WPF**（`net8.0-windows`），自包含单文件发布（win-x64，使用者免装 .NET）
- **输入**：Win32 低级钩子（键鼠）、XInput、HID 直读（DualSense / DualShock 4）
- **界面**：WPF（悬浮窗 / 设置窗 / 调试面板 / 托盘）；网页输出由内置 `HttpListener` 提供透明 HTML 页面
- **测试**：xUnit，共 110 个用例；配置为程序同目录的 `config.json`

## 目录

```
src/ZZZKeyEcho.Core    引擎与输入（配置、映射、记录引擎、手柄、Win32）
src/ZZZKeyEcho.App     界面（悬浮窗 / 设置 / 调试面板 / 浏览器源输出 / 托盘）
tests/                 单元测试
tools/pack.ps1         打包脚本
assets/                内置图标、网页模板、默认配置
```

## 常用指令

```powershell
dotnet run --project src/ZZZKeyEcho.App          # 运行（开发）
dotnet test                                      # 单元测试
dotnet build                                     # 构建
powershell -File tools/pack.ps1 -Version 0.1.0   # 打包便携版 → publish/*.zip + .sha256
```

## 快速开始

1. 下载 Release 里的 `ZZZKeyEcho-v0.1.1-win-x64.zip`，解压到任意目录
2. 双击 `ZZZKeyEcho.exe`，悬浮窗出现在屏幕左上角
3. 默认热键：`F8` 显示/隐藏 ｜ `F9` 锁定/解锁 ｜ `F10` 打开设置 ｜ `F11` 调试模式

> 若游戏以管理员身份运行，请同样以管理员身份运行本程序，否则系统会拦截输入监听。

> 首次运行会弹出 UAC 提示，请点「是」：程序以管理员权限运行，否则当游戏也以管理员启动时会收不到输入。

## 在 OBS 里叠加

设置 → **OBS 输出** → 勾选「开启网页输出」→ 复制地址 → OBS 添加「**浏览器**」源并粘贴该地址（背景透明，不影响游戏源；主播端仍使用原生悬浮窗）。

## 许可

MIT License —— 可自由用于个人与商业用途，保留版权声明即可，详见 [LICENSE](./LICENSE)。

## 说明

- 与米哈游 / HoYoverse 无关；不修改、不注入游戏，不联网（网页输出只监听 `127.0.0.1`）
