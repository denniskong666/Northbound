# 向北 Northbound

一款基于 Phaser 3 的俯视角剧情探索游戏。你将扮演杰米（Jamie），与 Elias、Maya、Noah、Leo 穿梭于格雷布里奇的老街、修理厂与屋顶，在「一起北上」的约定和每个人真实的人生选择之间寻找答案。

## 在线游玩

无需下载安装，直接访问：

**[dried-hollow-decent-gospel.trycloudflare.com](https://dried-hollow-decent-gospel.trycloudflare.com/)**

## 最新版本

- 完整中英双语，可在标题界面切换语言
- 序章、四个主章节与终章，共六段连续剧情
- 对话选择会记录跨章 A / B / C 印记，并改变人物羁绊、后续台词和支线
- 四种主要结局，并根据累计选择呈现不同人物、道具与场景细节
- 浏览器本地自动存档，可从标题界面继续游戏
- 加入「数北方的灯」小游戏、可调查场景物件与任务链
- 支持 PWA，可安装到桌面并缓存核心资源

## 故事简介

北方曾象征希望与崭新的人生。杰米和四位朋友早早立下约定，要一起离开格雷布里奇。但随着启程之日临近，故土、家庭、热爱和彼此之间的承诺开始拉扯每个人。坚持原计划、选择留下、独自出发，还是暂缓前行，都将由你一路留下的选择决定。

## 操作

| 操作 | 按键 |
| --- | --- |
| 移动 | `W` `A` `S` `D` 或方向键 |
| 奔跑 | 长按 `Shift` |
| 交互 | 靠近目标后按 `E` |
| 推进对话 | `空格`、`Enter` 或鼠标点击 |
| 选择选项 | `W` / `S`、上下方向键或鼠标 |
| 返回标题 | `Esc`（自动存档保留） |

## 章节

| 章节 | 标题 | 核心冲突 |
| --- | --- | --- |
| 序章 | 北方的召唤 | 写下愿望，认识所有伙伴 |
| 第一章 | 既定计划 | 攒路费时出现第一道裂痕 |
| 第二章 | 裂痕显现 | 朋友们的人生开始分岔 |
| 第三章 | 两难抉择 | 在互相冲突的任务之间取舍 |
| 第四章 | 北上成为枷锁 | 整理回忆，面对最终选择 |
| 终章 | 你来吗？ | 旧车已经修好，你是否出发 |

## 本地运行

需要 Node.js 20 或更高版本。

```bash
npm install
npm run dev
```

生产构建与本地预览：

```bash
npm run build
npm run preview
```

## 技术栈

- Phaser 3.80
- TypeScript 5
- Vite 5
- Cloudflare Tunnel
- Service Worker + Web App Manifest

## 项目结构

```text
src/
|-- config/       # 游戏配置
|-- data/         # 双语对话与 NPC 定义
|-- scenes/       # 标题、探索场景与终章
|-- state/        # 章节、选择、存档与结局状态
|-- systems/      # 对话、任务、选择、国际化与场景绘制
`-- main.ts       # 游戏入口

public/           # PWA 图标、清单与 Service Worker
dist/             # 当前生产构建
```

---

# Northbound - Web Edition

A top-down narrative exploration game built with Phaser 3. You play as Jamie and move through Greybridge's old district, garage, rooftops, and the lives of Elias, Maya, Noah, and Leo. The promise to "go north together" changes as each friend confronts family, ambition, belonging, and the cost of leaving.

## Play Online

No installation is required:

**[dried-hollow-decent-gospel.trycloudflare.com](https://dried-hollow-decent-gospel.trycloudflare.com/)**

## Current Features

- Full Chinese and English interface, selectable from the title screen
- A prologue, four main chapters, and a finale
- Cross-chapter A / B / C choice marks that affect relationships, later dialogue, side routes, and ending details
- Four principal endings shaped by accumulated decisions
- Automatic browser saves with Continue support
- The "Count the Northern Lights" minigame, inspectable objects, and multi-step tasks
- Installable PWA with cached core resources

## Story

North once meant hope and a clean beginning. Jamie and four friends promised to leave Greybridge together, but the departure date forces each of them to confront what home, love, work, and loyalty really mean. Your decisions determine whether Jamie follows the original plan, stays, leaves alone, or waits for a different moment.

## Controls

| Action | Input |
| --- | --- |
| Move | `W` `A` `S` `D` or arrow keys |
| Run | Hold `Shift` |
| Interact | Press `E` near a target |
| Advance dialogue | `Space`, `Enter`, or mouse click |
| Choose an option | `W` / `S`, arrow keys, or mouse |
| Return to title | `Esc` (autosave is retained) |

## Run Locally

Node.js 20 or newer is required. pnpm is recommended because the repository includes a lockfile.

```bash
pnpm install
pnpm dev
```

Create and preview a production build:

```bash
pnpm build
pnpm preview
```

## Technology

- Phaser 3.80
- TypeScript 5
- Vite 5
- Cloudflare Tunnel
- Service Worker and Web App Manifest

## Structure

```text
src/
|-- config/       # Game configuration
|-- data/         # Bilingual dialogue and NPC definitions
|-- scenes/       # Title, exploration, and epilogue scenes
|-- state/        # Chapters, choices, saves, and endings
|-- systems/      # Dialogue, tasks, choices, i18n, and rendering
`-- main.ts       # Game entry point

public/           # PWA icons, manifest, and service worker
dist/             # Current production build
```
