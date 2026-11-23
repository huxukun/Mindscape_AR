# Mindscape AR（心象空间）

面向 VITURE Luma Ultra 的 6DoF 室内 AR 应用：把个人第二大脑（[second-brain-memory](https://github.com/huxukun/second-brain-memory)）与情绪记录搬进房间空间。

## 目标形态

- **PARA 空间分区**：房间四周悬浮 Projects / Areas / Resources / Archives 四个空间文件夹，转头即可调出卡片。
- **3D 心情时间轴**：把情绪标签生成一条可拖拽的光轨，停到某天弹出对话摘要；情绪低谷时自动出现呼吸球。
- **进度可视化**：任务完成率以悬浮 3D 进度条呈现，完成即推进。
- **灵感收件箱**：固定在空间中的 Inbox 面板，语音/手势随时投递想法卡片。

## 数据来源

- `second-brain-memory` 仓库：`任务/`、`记忆/`、`伴侣机器人/`、`日志/`、`领域/` 下的 Markdown（YAML frontmatter）。
- 由导出脚本转换为 JSON，复制进 `Assets/StreamingAssets/ar-data/` 供 Unity 读取。
- `ar-data/` 含个人记忆与情绪数据，已在 `.gitignore` 中排除，需本地运行导出脚本生成。
- 聊天机器人的结构化情绪数据暂缺，先以对话记录中的文字线索 + 人工/规则打标方式生成。

## 技术栈

- Unity 6.3 LTS（URP）+ VITURE XR Plugin 0.9.0
- Android ARM64 / IL2CPP / OpenGLES3 / minSdk 33
- 包名 `com.huxukun.mindscape`，构建产物 `Build/Mindscape.apk`

## 构建

```
Unity.exe -batchmode -quit -projectPath . -executeMethod BuildAndroid.Build
```

首次在新机器上初始化工程可执行 `VitureAutoSetup.Run`，场景会生成在 `Assets/Scenes/Mindscape.unity`。
