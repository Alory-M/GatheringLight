# 像素拾光 · Pixel Moments

**Unity 2022.3.62f3c1（中国版）** 彩色数织游戏。整个游戏画幅为 **9:16 竖屏，900×1600**，包括主页、棋盘、帮助和通关页。鲜明的黄、橙、绿、蓝与暖纸底搭配，避免塑料高光。

## 试玩

1. 用指定 Unity 打开仓库，等待 UGUI / Test Framework 解析，打开 `Assets/Scenes/PixelMoments.unity`。
2. Game View 自动设为 900×1600；若需手动恢复，使用 `Pixel Moments > Set entire Game View to 9 x 16`。
3. 在主页选择关卡，选「涂色」后点击或自由滑动，**横、竖、斜线、转弯均可**。沿途格子会补齐，同笔经过同一格不重复提交。颜色由对应图案像素自动填入，无需选颜色。
4. **只要所有图案格涂对，且没有多涂空白，就能通关。叉号可选。** 多涂的空白用擦除修正。进度显示正确涂色格数／图案格总数。
5. 前两关完成后点「下一关」；第三关完成后点「返回主页」。

支持单指触屏与鼠标左键；第二根手指不会接管当前笔画。移出棋盘后只处理实际穿过棋盘的线段，不沿边缘虚画。换工具、重置、切换页面或失焦会结束笔画。

## 彩色行列线索

左侧和顶部使用相同规则：**有底色的数字块表示一段连续同色格子的数量**，左侧按从左到右排列，顶部按从上到下排列，并与对应行／列对齐。

例如黄底 `2`、橙底 `1` 表示连续两个黄色格，再出现一个橙色格。不同颜色可以直接相邻；相同颜色的两段之间至少有一个空白。整行／列为空则显示 `0`。玩家只需确定位置，系统自动使用目标颜色。

## 使用指定的三幅图

![用户选定的图像，直接从游戏像素源导出；非 Unity 截图](Documentation/Art/Levels.png)

| 关卡 | 难度 | 需要填色格数 | 彩色线索初始可确定格 |
| --- | --- | --- | --- |
| 向阳花开 | 入门 | 131 | 53 |
| 晴日小屋 | 进阶 | 124 | 42 |
| 花间来信 | 挑战 | 117 | 15 |

已恢复附图中蝴蝶没有额外镂空的版本。三幅图的全部 **675 个像素**与此前参考图采样一致，并以 RGB SHA-256 测试防止后续漂移。填色、行列线索、主页预览、最终成图都读取 `Level.cs` 同一份数据；完成图使用 Point 过滤的 15×15 纹理，保留方正像素，不用圆角格拼出另一种轮廓。

三题按实际彩色线索求解均恰好一解，可通过行列约束传播完成；初始可确定格递减。难度标签仍需真人试玩校准。

## 推理值

每关 5 点。每个新错误填色或叉号扣 1；正确标记、擦除与相同状态重复提交不扣分。同笔去重不代表免费试错，滑过多个错误格仍逐格扣分，归零立即锁定。重置清盘并恢复 5 点。

完成后不可再修改或重置。主页收集勾选仅保留本次运行，退出不保存棋盘。

## 构建与验证

- Android：Hub 安装 Android Build Support、SDK、NDK、OpenJDK；菜单 `Pixel Moments > Build Android APK`，输出 `Builds/Android/PixelMoments.apk`（ARM64 / IL2CPP）。
- Windows：安装 Windows Build Support；菜单 `Pixel Moments > Build Windows Demo`，输出 `Builds/Windows/PixelMoments.exe`，整个窗口为竖屏。
- Unity 测试：`Window > General > Test Runner > EditMode > Run All`。
- 独立测试：安装 .NET SDK 8 后运行 `dotnet test Tools/CoreTests/CoreTests.csproj`。

**39 项独立测试通过，0 失败、0 跳过。当前云环境没有 Unity 编辑器或授权，尚未验证 Unity 导入、真实触屏和 APK / EXE 构建。** 详细记录见 [Validation.md](Documentation/Validation.md)。

代码分工：`Level.cs` 像素与线索；`Puzzle.cs` 规则与流程；`LineStroke.cs` 自由笔画网格遍历；`BoardInput.cs` 指针采集；`ColorNonogramSolver.cs` 彩色题验证；`GameView.cs` 运行界面。`NonogramSolver.cs` 保留为二值轮廓诊断工具，不用于界面彩色数字。

中文字体为 Noto Sans SC（SIL OFL，来源和许可随项目提供），提示音为程序合成，无背景音乐。源码直接提交 GitHub，不提交缓存、构建产物或密钥。
