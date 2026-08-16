# Northbound / 向北

Northbound is a bilingual narrative game about five friends in Greybridge deciding whether "going north" is still a shared promise or a path each person must redefine.

Northbound 是一款中英双语叙事游戏。五位 Greybridge 的旧友必须重新理解“向北”的约定：一起离开、选择留下、独自出发，或暂缓启程。

## Play Online / 在线游玩

**[https://denniskong666.github.io/Northbound/](https://denniskong666.github.io/Northbound/)**

The browser edition runs directly online with no installation. / 网页版无需安装，打开链接即可游玩。

## Projects / 项目目录

| Folder / 目录 | Description / 说明 |
| --- | --- |
| [`website/`](website/) | Phaser 3 browser edition with PWA support. / 基于 Phaser 3、支持 PWA 的网页版本。 |
| [`APP/`](APP/) | Full Unity 6 desktop game source project. / 完整的 Unity 6 桌面游戏源码工程。 |

Each project has its own bilingual README with setup, controls, structure, and build instructions.

每个项目目录均包含独立的中英文 README，说明运行方式、操作、目录结构与构建流程。

## Quick Start / 快速开始

### Website / 网页版

```bash
cd website
pnpm install
pnpm dev
```

### APP / Unity 桌面版

Players can download ready-to-run Windows and macOS builds from [GitHub Releases](https://github.com/denniskong666/Northbound/releases/latest). Unity is not required to play either version.

普通玩家可从 [GitHub Releases](https://github.com/denniskong666/Northbound/releases/latest) 下载可直接运行的 Windows 或 macOS 版本，两个版本游玩都不需要安装 Unity。

- **Windows:** Download `Northbound-Windows-v1.0.0.zip`, extract the complete folder, and open `Northbound.exe`.
- **macOS:** Download `Northbound-macOS-v1.0.0.zip`, unzip it, and open `Northbound.app`. If macOS blocks the first launch, right-click the app and choose **Open**.

- **Windows：**下载 `Northbound-Windows-v1.0.0.zip`，完整解压文件夹后双击 `Northbound.exe`。
- **macOS：**下载 `Northbound-macOS-v1.0.0.zip`，解压后打开 `Northbound.app`。如果 macOS 第一次阻止运行，请右键应用并选择“打开”。

Developers who want to inspect or modify the source project should install Git LFS before cloning, then open `APP/` with Unity `6000.3.22f1`.

需要查看或修改源码的开发者应在克隆前安装 Git LFS，然后使用 Unity `6000.3.22f1` 打开 `APP/`。

```bash
git lfs install
git clone https://github.com/denniskong666/Northbound.git
```

Large cinematic files are stored with Git LFS. Generated Unity folders and compiled desktop builds are intentionally excluded; the complete rebuildable source and media assets are included.

大型剧情视频由 Git LFS 管理。Unity 缓存目录和编译后的桌面应用不纳入源码目录；仓库包含可完整重建的源码与媒体资源，成品在 Releases 下载。

## License / 许可

No open-source license is currently granted. All source code, story content, artwork, audio, and video remain under the project owner's copyright unless stated otherwise.

本项目目前未授予开源许可。除非另有说明，源码、剧情、美术、音频与视频的版权均归项目所有者所有。
