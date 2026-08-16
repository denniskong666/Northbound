# Northbound APP / 向北桌面版

## 中文

### 项目简介

Northbound APP 是使用 Unity 6 制作的完整桌面叙事探索游戏。玩家扮演 Jamie，在 Greybridge 与 Elias、Maya、Noah、Leo 一起生活、完成任务并面对“是否仍要一起向北”的选择。章节中的态度、关系和关键记忆会持续影响人物回应、任务反馈、终章路线与结局呈现。

### 主要内容

- 序章、四个主章节和终章的完整叙事流程
- 中英双语界面、对话、任务、引导和结局文本
- 可累计的关系、立场与章节记忆，以及兼容旧进度的存档迁移
- Opening、Maya、Noah、Leo、Rooftop、Finale 六段剧情视频
- 可跳过的小游戏、明确的拾取目标、金色任务标记与方向指引
- 四条终章路线，以及不同的场景色彩、携带物和选择回响
- macOS 桌面构建与自动化 EditMode / PlayMode 测试

### 获取工程

视频资源通过 Git LFS 保存。克隆前请先安装并启用 Git LFS，否则只能得到视频指针文件。

```bash
git lfs install
git clone https://github.com/denniskong666/Northbound.git
cd Northbound/APP
git lfs pull
```

### 开发环境

- Unity `6000.3.22f1`
- Universal Render Pipeline `17.3.0`
- Input System `1.14.0`
- macOS 构建目标（发布构建为 arm64 / x86_64 Universal）

使用 Unity Hub 添加并打开 `APP/` 目录。首次打开时 Unity 会自动恢复 `Library/` 缓存和 Package Manager 依赖。

### 操作

| 操作 | 按键 |
| --- | --- |
| 移动 | `W` `A` `S` `D` 或方向键 |
| 奔跑 | 长按 `Shift` |
| 交互 / 拾取 | `E` 或 `Enter` |
| 推进对话 | `Space`、`Enter` 或鼠标 |
| 选择选项 | 上下方向键、`W` / `S` 或鼠标 |
| 暂停 / 返回 | `Esc` |

### 测试与构建

在 Unity Test Runner 中运行 EditMode 和 PlayMode 测试。当前项目的 QA 证据位于 `docs/qa/`。

macOS 发布构建入口：

```text
Northbound.Editor.NorthboundReleaseBuilder.BuildMacOS
```

构建结果生成于 `Builds/macOS/Northbound.app`。`Builds/`、`Library/`、`Temp/`、日志和崩溃转储属于生成文件，不上传到 GitHub。

### 目录结构

```text
Assets/Northbound/
|-- Art/           # 角色、场景、道具与品牌图标
|-- Audio/         # 混音与音频配置
|-- Cinematics/    # Git LFS 管理的剧情视频
|-- Data/          # 章节、对话、任务与视频目录
|-- Editor/        # 内容生成、验证与发布构建工具
|-- Prefabs/       # UI 与游戏对象预制体
|-- Scenes/        # Bootstrap 与测试场景
|-- Scripts/       # 游戏运行时代码
`-- Tests/         # EditMode 与 PlayMode 自动化测试

Packages/          # Unity Package Manager 清单与锁文件
ProjectSettings/   # 可复现的 Unity 项目设置
Tools/             # 品牌图标等辅助生成脚本
docs/qa/            # 测试结果与发布检查记录
```

---

## English

### Overview

Northbound APP is the complete Unity 6 desktop edition of the narrative exploration game. You play as Jamie in Greybridge, completing tasks and living alongside Elias, Maya, Noah, and Leo while deciding whether the promise to "go north together" still holds. Chapter attitudes, relationships, and key memories continue into later dialogue, task feedback, the finale routes, and the ending presentation.

### Features

- A complete prologue, four main chapters, and finale
- Bilingual Chinese and English UI, dialogue, objectives, guidance, and endings
- Persistent relationship, stance, and chapter-memory consequences with save migration
- Six cinematics: Opening, Maya, Noah, Leo, Rooftop, and Finale
- Skippable minigames, explicit pickup objectives, gold markers, and directional guidance
- Four finale routes with distinct palettes, carried items, and callbacks to earlier choices
- macOS desktop builds plus automated EditMode and PlayMode coverage

### Clone the Project

Cinematic assets are stored with Git LFS. Install and enable Git LFS before cloning; otherwise the video files will remain small pointer files.

```bash
git lfs install
git clone https://github.com/denniskong666/Northbound.git
cd Northbound/APP
git lfs pull
```

### Development Environment

- Unity `6000.3.22f1`
- Universal Render Pipeline `17.3.0`
- Input System `1.14.0`
- macOS build target (the release build is Universal arm64 / x86_64)

Add and open the `APP/` directory in Unity Hub. Unity restores the generated `Library/` cache and Package Manager dependencies on the first launch.

### Controls

| Action | Input |
| --- | --- |
| Move | `W` `A` `S` `D` or arrow keys |
| Run | Hold `Shift` |
| Interact / pick up | `E` or `Enter` |
| Advance dialogue | `Space`, `Enter`, or mouse |
| Choose an option | Arrow keys, `W` / `S`, or mouse |
| Pause / back | `Esc` |

### Test and Build

Run the EditMode and PlayMode suites through Unity Test Runner. Current QA evidence is stored in `docs/qa/`.

The macOS release entry point is:

```text
Northbound.Editor.NorthboundReleaseBuilder.BuildMacOS
```

The build is generated at `Builds/macOS/Northbound.app`. `Builds/`, `Library/`, `Temp/`, logs, and crash dumps are generated artifacts and are intentionally excluded from GitHub.

### Project Structure

```text
Assets/Northbound/
|-- Art/           # Characters, environments, props, and branding
|-- Audio/         # Mixer and audio configuration
|-- Cinematics/    # Git LFS-managed story videos
|-- Data/          # Chapters, dialogue, objectives, and cinematic catalog
|-- Editor/        # Content generation, validation, and release tools
|-- Prefabs/       # UI and gameplay prefabs
|-- Scenes/        # Bootstrap and test scenes
|-- Scripts/       # Runtime game code
`-- Tests/         # EditMode and PlayMode automation

Packages/          # Unity Package Manager manifest and lockfile
ProjectSettings/   # Reproducible Unity project settings
Tools/             # Supporting asset-generation scripts
docs/qa/            # Test evidence and release checklists
```

## License / 许可

No open-source license is currently granted. All rights are reserved by the project owner.

本项目目前未授予开源许可，所有权利由项目所有者保留。
