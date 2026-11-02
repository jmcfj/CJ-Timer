# CJ-Timer · 动一下

蓝白运动风 Windows 桌面久坐提醒器，默认每 30 分钟提醒一次。

## 使用

下载仓库中的 [MoveReminder.exe](dist/MoveReminder.exe)，双击运行。需要 Windows 自带的 .NET Framework 4.x。

- 可设置 1–240 分钟，点击“应用并重新计时”保存设置并开始新一轮。
- 到点在桌面右下角弹窗，蓝色边框柔和闪动。
- 点击“暂停提醒，去运动”，停止闪动和计时。
- 运动结束后点击“运动结束，重新计时”，开始完整的新一轮。
- 关闭主窗口后继续在系统托盘运行，右键托盘图标可退出。
- 设置保存在当前用户的 `%LOCALAPPDATA%\MoveReminder\interval.txt`。
- 电脑休眠期间不会弹窗，唤醒后会检查到期状态。程序退出后不再提醒。

## 编译

在 Windows PowerShell 中运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

生成程序位于 `dist/MoveReminder.exe`。无需安装额外 NuGet 包。

## 字体修正

圆环内的文字启用 ClearTypeGridFit 文本抗锯齿，独立于圆环图形的 SmoothingMode；启用 DPI 感知以减少系统缩放造成的模糊。
