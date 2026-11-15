# 像素拾光 · Pixel Moments

基于《像素拾光 游戏策划案 V1.0》的 Unity 数织游戏 Demo。目标编辑器：**Unity 2022.3.62f3c1（中国版）**，16:9 横屏，移动端触屏和桌面鼠标共用 UI。

## 打开试玩

1. 在 Unity Hub 中添加此仓库，使用 **2022.3.62f3c1** 打开。允许 Package Manager 解析 UGUI 与 Test Framework。
2. 打开 `Assets/Scenes/PixelMoments.unity`，点击 Play。界面和声音由运行时代码生成，不依赖外部服务或密钥。
3. 「玩法 ?」查看说明；选择涂色、叉号或擦除，再点击格子。也可选择循环模式。
4. 构建 Windows：安装对应 Unity Windows Build Support，选择菜单 `Pixel Moments > Build Windows Demo`。产物在 `Builds/Windows`。

## Demo 内容

- 五角星 5×5 棋盘，由答案自动生成行列线索，穷举验证唯一解。
- 三态标记、5 点推理值、逐次错误扣分、归零锁定、重置。
- 必须全部 25 格正确；通关后禁用输入与重置，揭图并播放柔和提示音。
- 错误短闪、点击微动、锁定变暗、通关缩放与金色边缘脉冲。
- 明快纸感配色：向日葵黄、珊瑚橙、草木绿、天蓝与暖纸底。平面圆角色块，无材质高光或立体塑料效果。
- 内置中文字体（Noto Sans SC，SIL OFL，许可证随资源附带）和程序合成提示音。无背景音乐。

## 验证

Unity：`Window > General > Test Runner > EditMode > Run All`。

可选独立逻辑测试（.NET SDK 8）：

```sh
dotnet test Tools/CoreTests/CoreTests.csproj
```

命令行构建（已安装且已激活指定 Unity 编辑器时）：

```sh
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod PixelMoments.Editor.ProjectSetup.BuildWindows -logFile Builds-unity.log
```

当前云环境不包含 Unity 编辑器与许可证，不能据此宣称 Unity 编译、画面或构建已通过；具体结果见 `Documentation/Validation.md`。

## 结构与边界

`Puzzle.cs` 是无 Unity 依赖的规则模型；`GameView.cs` 构建 UI、处理反馈与音效；`ProjectSetup.cs` 注册场景和 Windows 构建入口。测试直接使用正式规则模型。

本次实现范围为策划案必须实现的首关 Demo，不包含策划建议中的后续关卡、题库编辑器、存档或 15 秒自动录屏。无时间限制。重复提交同一状态不重复扣值；清空标记不扣值。循环模式遵守每次新错误标记扣分，推荐使用直接涂色／叉号模式。

项目代码完成后直接提交 GitHub；不将 Library、缓存、构建产物或密钥入库。
