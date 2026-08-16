// 场景基类：提取玩家移动、输入、相机、对话系统、调试面板、POI/NPC/门交互等通用逻辑
// 子类只需实现 getMap / getSpawnTile / spawnContent，并可覆盖 applyChapterContent / registerChoices
// 门（addDoor）是特殊的 POI，交互后 fadeOut 切换到目标场景

import Phaser from 'phaser';
import { TILE_SIZE, PLAYER_SPEED, Direction, PLAYER_NAME } from '../config/GameConfig';
import { GameState, ENDING_LABEL, CARRY_ITEM_LABEL } from '../state/GameState';
import { chapterMeta, ChapterId } from '../state/Chapter';
import { ChoiceSystem } from '../systems/ChoiceSystem';
import { DialogueSystem, DialogueData } from '../systems/DialogueSystem';
import { TaskSystem } from '../systems/TaskSystem';
import { NpcPlacement, NpcProfile, getNpcProfile, NpcId } from '../data/NpcDefs';
import { DIALOGUES, DIALOGUES_DAILY, CH0_ELIAS_DIALOGUE, CH0_MAYA_DIALOGUE, CH0_NOAH_DIALOGUE, CH0_LEO_DIALOGUE } from '../data/Dialogues';
import { SceneArt } from '../systems/SceneArt';
import { t, L, getLang } from '../systems/I18n';

// POI 类型：决定标记点颜色（物品/任务/门/信息）
export type PoiType = 'item' | 'task' | 'door' | 'info';
const POI_TINT: Record<PoiType, number> = {
  item: 0xf5c97a,  // 琥珀（收集物）
  task: 0x6bd4f0,  // 青蓝（任务点）
  door: 0x9adf8a,  // 嫩绿（出入口）
  info: 0xd8c9a0   // 暖白（背景信息）
};

// 可交互点（POI：发光标记 + 文字交互）
export interface Poi {
  marker: Phaser.GameObjects.Image;
  labelText?: Phaser.GameObjects.Text;
  tileX: number;
  tileY: number;
  label: string;
  type?: PoiType;
  onInteract: () => void;
}

// NPC 可交互体
export interface NpcInteractable {
  sprite: Phaser.Physics.Arcade.Sprite;
  nameText: Phaser.GameObjects.Text;
  placement: NpcPlacement;   // 场景位置
  profile: NpcProfile;       // 人物档案
  tileX: number;
  tileY: number;
  label: string;
  onInteract: () => void;
}

// 统一可交互接口
export interface Interactable {
  tileX: number;
  tileY: number;
  label: string;
  onInteract: () => void;
}

// tile 编码 → 纹理 key 映射
const TILE_TEXTURE: Record<string, string> = {
  '0': 'tile_ground',
  '1': 'tile_wall',
  '2': 'tile_road',
  '3': 'tile_garage',
  '4': 'tile_roof'
};

export abstract class BaseScene extends Phaser.Scene {
  protected player!: Phaser.Physics.Arcade.Sprite;
  protected walls!: Phaser.Physics.Arcade.StaticGroup;
  protected cursors!: Phaser.Types.Input.Keyboard.CursorKeys;
  protected keyE!: Phaser.Input.Keyboard.Key;
  protected keyT!: Phaser.Input.Keyboard.Key;
  protected keyP!: Phaser.Input.Keyboard.Key;
  protected keyR!: Phaser.Input.Keyboard.Key;
  protected keyEsc!: Phaser.Input.Keyboard.Key;
  protected keyW!: Phaser.Input.Keyboard.Key;
  protected keyA!: Phaser.Input.Keyboard.Key;
  protected keyS!: Phaser.Input.Keyboard.Key;
  protected keyD!: Phaser.Input.Keyboard.Key;
  protected keyShift!: Phaser.Input.Keyboard.Key;
  protected currentDir: Direction = 'down';
  protected pois: Poi[] = [];
  protected npcInteractables: NpcInteractable[] = [];
  protected nearby: Interactable | null = null;
  protected promptText!: Phaser.GameObjects.Text;
  protected debugText!: Phaser.GameObjects.Text;
  protected taskText!: Phaser.GameObjects.Text;
  protected chapterText!: Phaser.GameObjects.Text;
  protected debugVisible = false;
  protected currentTaskId: string | null = null;
  protected choiceSystem = new ChoiceSystem();
  protected dialogueSystem!: DialogueSystem;
  protected inputLocked = false;
  protected tintRect?: Phaser.GameObjects.Rectangle;
  protected sceneArt!: SceneArt;
  // 物品放大查看界面
  private zoomOverlay?: Phaser.GameObjects.Container;
  private _zoomCloseCallback?: () => void;
  // 数北方灯火小游戏界面
  private nbLightOverlay?: Phaser.GameObjects.Container;
  private nbLightDots: Phaser.GameObjects.Image[] = [];
  private nbLightTimer?: Phaser.Time.TimerEvent;
  private nbLightTimeLeft = 0;
  private nbLightHitCount = 0;
  private nbLightTarget = 0;
  private nbLightText?: Phaser.GameObjects.Text;
  private nbLightTimerText?: Phaser.GameObjects.Text;
  private nbLightOnDone?: (success: boolean, count: number) => void;
  // 简单选项面板
  private simpleChoiceOverlay?: Phaser.GameObjects.Container;
  private simpleChoiceResult?: (index: number) => void;
  private simpleChoiceLabels: Phaser.GameObjects.Text[] = [];
  private simpleChoiceBars: Phaser.GameObjects.Rectangle[] = [];
  private simpleChoiceCursor = 0;
  private _sUpLatch = false;
  private _sDownLatch = false;
  // overlay 关闭防穿透：任何 overlay（放大/选项/小游戏）正在淡出时，屏蔽 ESC 退出游戏
  private _overlayClosing = false;
  private keySpace!: Phaser.Input.Keyboard.Key;

  // —— 子类实现 ——
  protected abstract getMap(): string[];
  protected abstract getSpawnTile(): { x: number; y: number };
  protected abstract spawnContent(): void;
  protected abstract sceneKey(): string;

  constructor(key: string) {
    super(key);
  }

  create(): void {
    this.pois = [];
    this.npcInteractables = [];
    this.nearby = null;
    this.currentDir = 'down';
    this.inputLocked = false;
    this.currentTaskId = null;  // 重置任务ID缓存，确保新场景立即刷新任务文本
    this.zoomOverlay = undefined;  // 重置放大查看状态，避免场景重启后残留引用阻断输入
    this._zoomCloseCallback = undefined;
    // 重置数北方灯火小游戏
    this.nbLightOverlay = undefined;
    this.nbLightDots = [];
    if (this.nbLightTimer) { this.nbLightTimer.remove(); this.nbLightTimer = undefined; }
    this.nbLightTimeLeft = 0;
    this.nbLightHitCount = 0;
    this.nbLightTarget = 0;
    this.nbLightOnDone = undefined;
    // 重置简单选项面板
    this.simpleChoiceOverlay = undefined;
    this.simpleChoiceResult = undefined;
    this.simpleChoiceLabels = [];
    this.simpleChoiceBars = [];
    this.simpleChoiceCursor = 0;
    this._sUpLatch = false;
    this._sDownLatch = false;
    this._overlayClosing = false;
    this.sceneArt = new SceneArt(this);

    this.registerChoices();
    this.buildMap();
    this.createPlayer();
    this.setupInput();
    this.setupCamera();
    this.createUI();
    this.spawnContent();
    this.setupDialogue();
    this.applyChapterContent(GameState.inst.chapter);
    this.applyChapterTint(GameState.inst.chapter);
    this.updateChapterLabel();
    // 场景启动后立即刷新任务提示（不等第一帧 update，避免场景切换后短暂不可见）
    this.updateTaskUI();

    this.cameras.main.fadeIn(500, 0, 0, 0);
  }

  // 子类可覆盖：注册互斥任务（默认空）
  protected registerChoices(): void {}

  // 子类可覆盖：按章节刷新场景内容（默认空）
  protected applyChapterContent(_ch: ChapterId): void {}

  // —— 地图构建 ——
  protected buildMap(): void {
    const map = this.getMap();
    const ch = GameState.inst.chapter;
    const isCh0 = ch === 'ch0';
    this.walls = this.physics.add.staticGroup();
    for (let row = 0; row < map.length; row++) {
      for (let col = 0; col < map[row].length; col++) {
        const code = map[row][col];
        const x = col * TILE_SIZE + TILE_SIZE / 2;
        const y = row * TILE_SIZE + TILE_SIZE / 2;
        if (code === '1') {
          this.walls.create(x, y, isCh0 ? 'tile_wall_ch0' : 'tile_wall');
        } else {
          let tex = TILE_TEXTURE[code] ?? 'tile_ground';
          if (isCh0) {
            if (code === '0') tex = 'tile_ground_ch0';
            else if (code === '2') tex = 'tile_road_ch0';
          }
          this.add.image(x, y, tex);
        }
      }
    }
  }

  protected get mapWidthTiles(): number { return this.getMap()[0].length; }
  protected get mapHeightTiles(): number { return this.getMap().length; }

  protected createPlayer(): void {
    const sp = this.getSpawnTile();
    const x = sp.x * TILE_SIZE + TILE_SIZE / 2;
    const y = sp.y * TILE_SIZE + TILE_SIZE / 2;
    this.player = this.physics.add.sprite(x, y, 'player', 'down_1');
    this.player.setDepth(10);
    this.player.body!.setSize(16, 14, false);
    this.player.body!.setOffset(16, 33);
    this.player.setCollideWorldBounds(true);
    this.physics.add.collider(this.player, this.walls);
  }

  protected setupInput(): void {
    this.cursors = this.input.keyboard!.createCursorKeys();
    this.keyW = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.W);
    this.keyA = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.A);
    this.keyS = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.S);
    this.keyD = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.D);
    this.keyE = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.E);
    this.keyT = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.T);
    this.keyP = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.P);
    this.keyR = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.R);
    this.keyEsc = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.ESC);
    this.keyShift = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.SHIFT);
    this.keySpace = this.input.keyboard!.addKey(Phaser.Input.Keyboard.KeyCodes.SPACE);
  }

  protected setupCamera(): void {
    const worldW = this.mapWidthTiles * TILE_SIZE;
    const worldH = this.mapHeightTiles * TILE_SIZE;
    this.physics.world.setBounds(0, 0, worldW, worldH);
    this.cameras.main.setBounds(0, 0, worldW, worldH);
    this.cameras.main.startFollow(this.player, true, 0.12, 0.12);
  }

  protected createUI(): void {
    // 屏幕暗角氛围
    this.createVignette();

    this.promptText = this.add.text(this.scale.width / 2, this.scale.height - 40, '', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '16px',
      color: '#f5c97a',
      backgroundColor: 'rgba(20,18,24,0.75)',
      padding: { x: 12, y: 6 }
    }).setOrigin(0.5).setDepth(100).setScrollFactor(0).setVisible(false);

    // 任务提示面板（左上常驻）—— 初始化不隐藏，由 updateTaskUI 用 alpha 控制显示
    this.taskText = this.add.text(12, 12, '', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '13px',
      color: '#e8e4d8',
      backgroundColor: 'rgba(15,13,18,0.7)',
      padding: { x: 10, y: 7 },
      lineSpacing: 3
    }).setDepth(150).setScrollFactor(0).setVisible(true).setAlpha(0);

    // 章节标识（右上角常驻，低调显示"第X章 · 章节名"）
    this.chapterText = this.add.text(this.scale.width - 12, 12, '', {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '14px',
      color: '#c9b890',
      backgroundColor: 'rgba(15,13,18,0.55)',
      padding: { x: 10, y: 5 }
    }).setOrigin(1, 0).setDepth(150).setScrollFactor(0);

    this.debugText = this.add.text(12, 90, '', {
      fontFamily: 'Consolas, "Microsoft YaHei", monospace',
      fontSize: '12px',
      color: '#9ad',
      backgroundColor: 'rgba(10,10,16,0.7)',
      padding: { x: 8, y: 6 },
      lineSpacing: 2
    }).setDepth(200).setScrollFactor(0).setVisible(false);
  }

  // 刷新章节标识：强制常驻（与任务提示同级别保护）
  protected updateChapterLabel(): void {
    if (!this.chapterText || !this.chapterText.scene || this.chapterText.active === false) {
      this.chapterText = this.add.text(this.scale.width - 12, 12, '', {
        fontFamily: '"PingFang SC","Microsoft YaHei",serif',
        fontSize: '14px',
        color: '#c9b890',
        backgroundColor: 'rgba(15,13,18,0.55)',
        padding: { x: 10, y: 5 }
      }).setScrollFactor(0).setOrigin(1, 0);
    }
    const full = chapterMeta(GameState.inst.chapter).title;
    const expected = full.split(' · ')[0];
    this.chapterText.setScrollFactor(0);
    this.chapterText.setDepth(150);
    this.chapterText.setVisible(true);
    this.chapterText.setActive(true);
    this.chapterText.setOrigin(1, 0);
    this.chapterText.setPosition(this.scale.width - 12, 12);
    if (this.chapterText.text !== expected) {
      this.chapterText.setText(expected);
    }
    this.chapterText.setAlpha(1);
  }

  // 屏幕暗角：径向渐变叠加，增强氛围感
  private createVignette(): void {
    const W = this.scale.width;
    const H = this.scale.height;
    const key = 'vignette';
    if (!this.textures.exists(key)) {
      const canvas = this.textures.createCanvas(key, W, H);
      if (!canvas) return;
      const ctx = canvas.getContext();
      const g = ctx.createRadialGradient(W / 2, H / 2, Math.min(W, H) * 0.35, W / 2, H / 2, Math.max(W, H) * 0.75);
      g.addColorStop(0, 'rgba(0,0,0,0)');
      g.addColorStop(1, 'rgba(0,0,0,0.45)');
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, W, H);
      canvas.refresh();
    }
    this.add.image(W / 2, H / 2, key).setDepth(250).setScrollFactor(0);
  }

  // 章节氛围色调：每章一层低透明度色彩滤镜，区分时间流逝与情绪
  // 序章地面贴图本身已明亮（tile_ground_ch0），叠加层仅做极淡暖光点缀
  protected getChapterTint(ch: ChapterId): { color: number; alpha: number } {
    switch (ch) {
      case 'ch0': return { color: 0xf5c97a, alpha: 0.05 };  // 极淡暖金点缀（地面本身已明亮）
      case 'ch1': return { color: 0xf5c97a, alpha: 0.08 };  // 暖晨光
      case 'ch2': return { color: 0x7a8296, alpha: 0.14 };  // 阴天冷灰
      case 'ch3': return { color: 0xe07a5f, alpha: 0.12 };  // 黄昏珊瑚
      case 'ch4': return { color: 0x2a3c6e, alpha: 0.22 };  // 深夜冷蓝
      default:    return { color: 0x1a1620, alpha: 0.12 };  // 终章/默认
    }
  }

  protected applyChapterTint(ch: ChapterId): void {
    const { color, alpha } = this.getChapterTint(ch);
    const W = this.scale.width;
    const H = this.scale.height;
    if (!this.tintRect) {
      this.tintRect = this.add.rectangle(W / 2, H / 2, W, H, color, alpha)
        .setDepth(240).setScrollFactor(0);
    } else {
      this.tintRect.setFillStyle(color, alpha);
    }
  }

  // 刷新任务提示：强制常驻显示（完成前不消失）
  // 关键修复：不依赖 setVisible(false) 隐藏，改用 setAlpha；每帧强制 setVisible(true)+重设 depth/scrollFactor
  protected updateTaskUI(): void {
    // 安全网：若 taskText 因任何原因被销毁/不可用，立即重建
    if (!this.taskText || !this.taskText.scene || this.taskText.active === false) {
      this.taskText = this.add.text(12, 12, '', {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '13px',
        color: '#e8e4d8',
        backgroundColor: 'rgba(15,13,18,0.7)',
        padding: { x: 10, y: 7 },
        lineSpacing: 3
      }).setScrollFactor(0);
    }

    const task = TaskSystem.inst.currentTask(GameState.inst.chapter);
    const id = task?.id ?? null;
    if (id !== this.currentTaskId) {
      this.currentTaskId = id;
    }

    // 强制恢复渲染属性（防止场景切换/转场后属性错乱）
    this.taskText.setScrollFactor(0);
    this.taskText.setDepth(150);
    this.taskText.setVisible(true);
    this.taskText.setActive(true);
    // 确保在相机视野内，无任何父容器影响
    if (this.taskText.parentContainer) this.taskText.parentContainer.remove(this.taskText);
    this.taskText.setPosition(12, 12);

    if (task) {
      const expected = `${t('task_prefix')}${task.title}\n${task.goal}`;
      if (this.taskText.text !== expected) {
        this.taskText.setText(expected);
      }
      // 有活跃任务：不透明显示
      this.taskText.setAlpha(1);
    } else {
      // 无活跃任务：仅用 alpha 透明化，绝不调用 setVisible(false)
      this.taskText.setAlpha(0);
    }
  }

  protected setupDialogue(): void {
    this.dialogueSystem = new DialogueSystem({
      scene: this,
      onLockInput: () => { this.inputLocked = true; this.promptText.setVisible(false); },
      onUnlockInput: () => { this.inputLocked = false; }
    });
  }

  // —— 生成 NPC ——
  protected spawnNpcs(placements: NpcPlacement[]): void {
    for (const placement of placements) {
      const profile = getNpcProfile(placement.id);
      const x = placement.tileX * TILE_SIZE + TILE_SIZE / 2;
      const y = placement.tileY * TILE_SIZE + TILE_SIZE / 2;
      const sprite = this.physics.add.sprite(x, y, profile.textureKey, `${placement.facing}_1`);
      sprite.setDepth(8);
      sprite.body!.setSize(16, 14, false);
      sprite.body!.setOffset(16, 33);
      sprite.setCollideWorldBounds(true);
      sprite.setImmovable(true);
      this.physics.add.collider(this.player, sprite);

      // 待机呼吸：轻微纵向缩放，让 NPC 不再"死板"
      this.tweens.add({
        targets: sprite,
        scaleY: { from: 1, to: 1.035 },
        duration: 1800 + (placement.tileX % 5) * 200, // 错开节奏避免整齐同步
        yoyo: true,
        repeat: -1,
        ease: 'Sine.easeInOut',
        delay: (placement.tileY % 4) * 150
      });

      const nameText = this.add.text(x, y - 34, profile.name, {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '12px',
        color: '#e8e4d8',
        backgroundColor: 'rgba(15,13,18,0.7)',
        padding: { x: 6, y: 2 }
      }).setOrigin(0.5).setDepth(20);

      const label = placement.label ?? `和${profile.name}说话`;
      const npc: NpcInteractable = {
        sprite, nameText, placement, profile,
        tileX: placement.tileX, tileY: placement.tileY,
        label,
        onInteract: () => {
          this.turnNpcTowardPlayer(placement);
          const ch = GameState.inst.chapter;
          const talkedFlag = `npc_${placement.id}_talked_${ch}`;
          const oldTalkedFlag = `npc_${placement.id}_talked`;

          if (!GameState.inst.hasFlag(talkedFlag)) {
            // 继承旧标记：若玩家在旧版本打过招呼，允许继续用日常对话（兼容）
            const alreadyTalked = GameState.inst.hasFlag(oldTalkedFlag);
            const markDialogue = this.getMarkIntroDialogue(placement.id);
            const storyDialogue = this.getChapterStoryDialogue(placement.id, ch);
            const startStory = () => {
              this.dialogueSystem.start(storyDialogue, () => {
                GameState.inst.applyEffects({ flag: talkedFlag });
                GameState.inst.applyEffects({ flag: oldTalkedFlag });
                this.onNpcDialogueComplete(placement.id);
              });
            };
            if (markDialogue && !alreadyTalked) {
              // 印记连锁台词 → 对话系统播放（带说话者+UI），完成后接剧情对话
              this.dialogueSystem.start(markDialogue, startStory);
            } else {
              startStory();
            }
          } else {
            const daily = this.getChapterDailyDialogue(placement.id, ch);
            this.dialogueSystem.start(daily);
          }
        }
      };
      this.npcInteractables.push(npc);
    }
  }

  protected turnNpcTowardPlayer(placement: NpcPlacement): void {
    const npc = this.npcInteractables.find(n => n.placement.id === placement.id);
    if (!npc) return;
    const dx = this.player.x - npc.sprite.x;
    const dy = this.player.y - npc.sprite.y;
    let dir: Direction;
    if (Math.abs(dy) >= Math.abs(dx)) dir = dy < 0 ? 'up' : 'down';
    else dir = dx < 0 ? 'left' : 'right';
    npc.sprite.setFrame(`${dir}_1`);
  }

  // 跨章印记连锁：根据当前章节和前章印记返回开场白
  // 所有台词来自剧本原文，确保与玩家前章选择精确联动
  // 按章节返回 NPC 的剧情对话（序章使用专属欢愉对话，其他章节用默认 DIALOGUES）
  protected getChapterStoryDialogue(npcId: NpcId, ch: ChapterId): DialogueData {
    if (ch === 'ch0') {
      const ch0Map: Record<string, DialogueData> = {
        elias: CH0_ELIAS_DIALOGUE,
        maya: CH0_MAYA_DIALOGUE,
        noah: CH0_NOAH_DIALOGUE,
        leo: CH0_LEO_DIALOGUE
      };
      if (ch0Map[npcId]) return ch0Map[npcId];
    }
    return DIALOGUES[npcId];
  }

  // NPC 剧情对话完成后的回调（子类可覆盖以触发任务进度等）
  protected onNpcDialogueComplete(_npcId: NpcId): void {
    // 默认无操作
  }

  // 跨章印记连锁：根据当前章节和前章印记返回开场对话（用对话系统播放，带说话者+UI）
  private getMarkIntroDialogue(npcId: string): DialogueData | null {
    const ch = GameState.inst.chapter;
    let speaker = '';
    let text = '';

    // 第二章：根据 ch1 印记 → 剧本原文三分支
    if (ch === 'ch2') {
      const m1 = GameState.inst.getStoryMark('ch1');
      if (!m1) return null;

      if (npcId === 'maya') {
        speaker = t('npc_maya');
        if (m1 === 'A1') text = L('Elias 前段时间和我说，你一门心思只想凑路费去北边，完全不在意老街的一切。', 'Elias told me recently that you only care about saving fare for the North and have no regard for the old street at all.');
        else if (m1 === 'C1') text = L('Leo 前段时间来找我聊天，说你和他想法一样，都舍不得这座城市。我本来还以为所有人都只想逃离。', 'Leo came to chat with me recently, saying you and he feel the same — reluctant to leave this city. I had thought everyone just wanted to escape.');
        else text = L('Elias 和 Leo 对你的评价完全不一样，说你两边都能理解，不会偏执一方。', 'Elias and Leo describe you completely differently, saying you can understand both sides and won\u2019t favor either.');
      } else if (npcId === 'noah') {
        speaker = t('npc_noah');
        if (m1 === 'A1') text = L('我本来也想靠北上躲开家里安排，但听完你们的想法，我有点犹豫该不该放弃手工爱好。', 'I also wanted to head North to dodge my family\u2019s plans, but hearing your thoughts, I\u2019m hesitating whether to give up my craft.');
        else if (m1 === 'C1') text = L('如果这座城市值得留下，或许我不用非要靠远走来逃避家人。', 'If this city is worth staying for, maybe I don\u2019t have to flee far to escape my family.');
        else text = L('那正好，我一边想逃离家庭，一边又舍不得刚找到的手工乐趣。', 'That works out — part of me wants to escape my family, yet I can\u2019t bear to leave this craft I just found.');
      } else return null;
    }
    // 第三章：根据 ch1+ch2 组合印记 → 剧本原文三分支
    else if (ch === 'ch3') {
      const m1 = GameState.inst.getStoryMark('ch1');
      const m2 = GameState.inst.getStoryMark('ch2');
      if (!m1 || !m2) return null;

      const bothA = m1 === 'A1' && m2 === 'A2';
      const bothC = m1 === 'C1' && m2 === 'C2';

      if (npcId === 'elias') {
        speaker = t('npc_elias');
        if (bothA) text = L('之前听 Leo、Maya 说，从攒路费到收集物资，你一直都以我们共同的北上约定为先。办通行材料我帮你加急。', 'Leo and Maya mentioned that from saving fare to gathering supplies, you always put our shared Northbound pact first. I\u2019ll expedite your travel documents.');
        else if (bothC) text = L('我听说你一直认同 Leo，还支持 Maya 留下来画画，看来你早就不把我们年少的约定放在心上了。', 'I hear you\u2019ve sided with Leo all along and backed Maya staying to paint — seems you stopped caring about our childhood pact long ago.');
        else text = L('我知道你两边都顾及，不会完全偏袒谁，但通行手续不能拖。', 'I know you\u2019ve been mindful of both sides and won\u2019t fully favor anyone, but the travel paperwork can\u2019t wait.');
      } else if (npcId === 'maya') {
        speaker = t('npc_maya');
        if (bothA) text = L('我知道你的重心一直在远行，我的画展你大概率没时间来看，我不勉强你。', 'I know your heart has been on the journey — you probably won\u2019t have time to see my show. I won\u2019t insist.');
        else if (bothC) text = L('我很早就想和你聊聊，难得有人能理解我不想盲目离开的想法。首展我特别希望你到场。', 'I\u2019ve wanted to talk to you for a while — it\u2019s rare to find someone who understands why I don\u2019t want to leave blindly. I really hope you\u2019ll be at the opening.');
        else text = L('如果你愿意抽空过来，我可以把开展时间延后一点。', 'If you can spare the time to come, I can push the opening back a little.');
      } else return null;
    }
    // 第四章：根据 ch1+ch2+ch3 组合印记 → 剧本原文三分支
    else if (ch === 'ch4') {
      const m1 = GameState.inst.getStoryMark('ch1');
      const m2 = GameState.inst.getStoryMark('ch2');
      const m3 = GameState.inst.getStoryMark('ch3');
      if (!m1 || !m2 || !m3) return null;

      const fullA = m1 === 'A1' && m2 === 'A2' && m3 === 'A3';
      const fullC = m1 === 'C1' && m2 === 'C2' && m3 === 'C3';

      if (npcId === 'noah') {
        speaker = t('npc_noah');
        if (fullA) text = L('Maya 和我说，办通行材料的时候你毫不犹豫选择优先北上手续，放弃了她的画展。你从头到尾都只想离开这座城市。', 'Maya told me that when handling the travel documents, you didn\u2019t hesitate to prioritize the Northbound paperwork and skipped her show. You\u2019ve wanted to leave this city from start to finish.');
        else if (fullC) text = L('Maya 告诉我，为了陪她看画展，你推迟了出城手续。我现在也不想为了逃避家人盲目北上。', 'Maya told me you delayed the departure paperwork so you could attend her show. Now I also don\u2019t want to head North blindly just to escape my family.');
        else text = L('我听 Maya、Elias 说，一路上你谁都没有刻意辜负，一直在平衡远行和留在本地两种生活。', 'Maya and Elias said that along the way you never let anyone down on purpose, always balancing the journey and staying put.');
      } else if (npcId === 'leo') {
        speaker = t('npc_leo');
        if (fullA) text = L('当初我和你聊老街回忆的时候，你完全不在意，现在看来我们本来就不是一路人。', 'When I talked to you about old street memories back then, you didn\u2019t care at all — looks like we were never on the same path.');
        else if (fullC) text = L('第一章我们在屋顶聊家乡的时候，我就知道你和我一样，舍不得这里的一切。', 'Back in Chapter One, when we talked about home on the rooftop, I knew you were like me — reluctant to leave all of this behind.');
        else text = L('不管是走是留，至少你从来没有强迫任何人遵从某一种选择。', 'Whether to leave or stay, at least you never forced anyone to follow a single choice.');
      } else return null;
    } else {
      return null;
    }

    if (!text) return null;
    return {
      id: `mark_intro_${npcId}_${ch}`,
      start: 'line',
      nodes: { line: { speaker, text } }
    };
  }

  // 用对话系统播放旁白（speaker 为空，带对话框 UI + 打字机效果）
  protected playNarration(text: string, onComplete?: () => void): void {
    this.dialogueSystem.start({
      id: `narration_${Date.now()}`,
      start: 'line',
      nodes: { line: { speaker: '', text } }
    }, onComplete);
  }

  // —— 章节特定日常对话 ——
  private getChapterDailyDialogue(npcId: NpcId, ch: ChapterId): DialogueData {
    const base = DIALOGUES_DAILY[npcId];
    const lineMap: Record<string, Partial<Record<ChapterId, string>>> = {
      elias: {
        ch0: L('路线我反复查过了，没错。北方就是我们的方向！', 'I\u2019ve checked the route over and over, it\u2019s right. The North is our direction!'),
        ch1: L('零件的事不急，先把手头的活干完。北边不会跑掉的。', 'No rush on the parts, let\u2019s finish what\u2019s at hand first. The North isn\u2019t going anywhere.'),
        ch2: L('旅行车又出了点小问题，不过修修就好。你那边进展如何？', 'The camper had a small issue again, but it\u2019ll be fine after a fix. How\u2019s your end coming along?'),
        ch3: L('通行材料的事我听说了，先办好这个，画展的事 Maya 能理解的。', 'I heard about the travel documents — get that sorted first, Maya will understand about the show.'),
        ch4: L('这是最后一次整理物资了。你准备好了吗？', 'This is the last time we\u2019re packing supplies. Are you ready?'),
        epilogue: ''
      },
      maya: {
        ch0: L('我昨晚画到很晚——北方的极光，颜色太美了！', 'I painted late into the night — the Northern lights, the colors are so beautiful!'),
        ch1: L('今天光线不错，回头我画一张这条街的速写给你看。', 'The light is nice today, I\u2019ll sketch this street for you later.'),
        ch2: L('我在考虑提交参展作品，画的是这条街四季的样子。', 'I\u2019m considering submitting a piece for the show — it\u2019s this street through the four seasons.'),
        ch3: L('画展就在这周了。你选了集体那边的话也没关系，我知道你在做选择。', 'The show is this week. It\u2019s okay if you went with the group\u2019s side — I know you\u2019re making a choice.'),
        ch4: L('画作我已经装箱了。不管去哪儿，这条街的颜色都会跟着我。', 'I\u2019ve packed up the paintings. No matter where I go, the colors of this street will follow me.'),
        epilogue: ''
      },
      noah: {
        ch0: L('一想到北方没人认识我，就觉得呼吸都顺畅了。', 'Just thinking that no one in the North knows me makes it easier to breathe.'),
        ch1: L('录音机昨天又录到一段不错的风声。走了以后，大概会想念这些声音吧。', 'The recorder caught a nice stretch of wind yesterday. After we leave, I\u2019ll probably miss these sounds.'),
        ch2: L('我决定不去考那个学校了。有些事现在不做，以后就再也没机会了。', 'I\u2019ve decided not to take that school\u2019s exam. Some things, if not done now, will never have another chance.'),
        ch3: L('录音机录了大家筹备画展的声音，回头剪在一起应该挺有味道。', 'The recorder captured everyone prepping for the show — splicing them together later should be quite something.'),
        ch4: L('最后一段录音我想留给自己。未来的路，需要自己听清楚。', 'I want to keep the last recording for myself. The road ahead, I need to hear it clearly on my own.'),
        epilogue: ''
      },
      leo: {
        ch0: L('十八年了，终于要走出去看看外面是什么样了！', 'Eighteen years, finally stepping out to see what the outside is like!'),
        ch1: L('餐厅今天有新菜。说真的，这地方的吃的，到哪儿都替代不了。', 'The diner has a new dish today. Honestly, the food here can\u2019t be replaced anywhere.'),
        ch2: L('我帮你看着仓库了，你放心去收物资。老街的治安我最熟。', 'I\u2019m watching the warehouse for you, go gather supplies in peace. I know the old street\u2019s security best.'),
        ch3: L('画展布置得差不多了，缺的那些画架我已经找人搬过去了。', 'The show setup is nearly done, I\u2019ve already had someone move the missing easels over.'),
        ch4: L('老街的街角我拍了最后一张照。以后想起来，就看这个。', 'I took a final photo of the old street corner. When I think of it later, I\u2019ll look at this.'),
        epilogue: ''
      }
    };
    const texts = lineMap[npcId];
    if (texts && texts[ch]) {
      return {
        id: `${npcId}_daily_${ch}`,
        start: 'line',
        nodes: {
          line: {
            speaker: base.nodes.line.speaker,
            text: texts[ch]!
          }
        }
      };
    }
    return base;
  }

  // —— POI / 门 ——
  protected addPoi(tx: number, ty: number, label: string, opts: { line?: string; type?: PoiType; onInteract?: () => void } = {}): Poi {
    const x = tx * TILE_SIZE + TILE_SIZE / 2;
    const y = ty * TILE_SIZE + TILE_SIZE / 2;
    const type: PoiType = opts.type ?? 'info';
    const marker = this.add.image(x, y, 'marker').setDepth(5).setTint(POI_TINT[type]);
    this.tweens.add({
      targets: marker,
      scale: { from: 0.85, to: 1.15 },
      alpha: { from: 0.7, to: 1 },
      duration: 1200,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    });
    // 浮动标签：始终显示在标记上方，便于玩家识别
    const labelText = this.add.text(x, y - TILE_SIZE * 0.65, label, {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '11px',
      color: '#f5c97a',
      stroke: '#000000',
      strokeThickness: 3,
    }).setOrigin(0.5).setDepth(6).setAlpha(0.75);

    const poi: Poi = {
      marker, labelText, tileX: tx, tileY: ty, label, type,
      onInteract: opts.onInteract ?? (() => this.showSpeech(opts.line ?? ''))
    };
    this.pois.push(poi);
    return poi;
  }

  // 门：交互后 fadeOut 切换场景（嫩绿标记，区别于普通 POI）
  protected addDoor(tx: number, ty: number, label: string, targetScene: string, data?: object): Poi {
    return this.addPoi(tx, ty, label, {
      type: 'door',
      onInteract: () => this.gotoScene(targetScene, data)
    });
  }

  protected gotoScene(target: string, data?: object): void {
    if (this.inputLocked) return;
    this.inputLocked = true;
    this.cameras.main.once('camerafadeoutcomplete', () => {
      this.scene.start(target, data);
    });
    this.cameras.main.fadeOut(300, 0, 0, 0);
  }

  protected removePoi(poi: Poi): void {
    poi.marker.destroy();
    poi.labelText?.destroy();
    const i = this.pois.indexOf(poi);
    if (i >= 0) this.pois.splice(i, 1);
    if (this.nearby === poi) {
      this.nearby = null;
      this.promptText.setVisible(false);
    }
  }

  // —— 章节与调试 ——
  protected advanceChapter(): void {
    const n = GameState.inst.advance();
    if (!n) { this.showSpeech(t('already_finale')); return; }
    this.applyChapterContent(n);
    this.applyChapterTint(n);
    this.updateChapterLabel();
  }

  protected resetGame(): void {
    GameState.inst.reset();
    this.applyChapterContent(GameState.inst.chapter);
    this.applyChapterTint(GameState.inst.chapter);
    this.updateChapterLabel();
    this.showSpeech(t('save_reset'));
  }

  // ESC 一键退出到标题界面（存档保留，可从"继续游戏"恢复）
  protected quitToTitle(): void {
    this.inputLocked = true;
    this.cameras.main.fadeOut(400, 0, 0, 0);
    this.cameras.main.once('camerafadeoutcomplete', () => {
      this.scene.start('TitleScene');
    });
  }

  // 章节丝滑转场：fadeOut → 章节标题卡 → mid（推进章节状态）→ fadeIn
  // 未来 CG 制作完成后，在黑屏停留处接入预渲染动画播放即可。
  protected playChapterTransition(mid: () => void): void {
    this.inputLocked = true;
    this.promptText.setVisible(false);
    this.cameras.main.fadeOut(600, 0, 0, 0);
    this.cameras.main.once('camerafadeoutcomplete', () => {
      // ===== CG 动画插入点 =====
      mid();
      this.updateChapterLabel();
      this.applyChapterTint(GameState.inst.chapter);

      // 章节标题卡片：黑屏中央淡入章节名，停留后随场景淡出
      const title = chapterMeta(GameState.inst.chapter).title;
      const parts = title.split(' · ');
      const card = this.add.container(this.scale.width / 2, this.scale.height / 2)
        .setDepth(500).setScrollFactor(0).setAlpha(0);

      const mainTitle = this.add.text(0, -16, parts[0] ?? '', {
        fontFamily: '"PingFang SC","Microsoft YaHei",serif',
        fontSize: '36px', color: '#e8e4d8', fontStyle: 'bold',
        stroke: '#000000', strokeThickness: 6,
      }).setOrigin(0.5);

      const subTitle = this.add.text(0, 22, parts.slice(1).join(' · '), {
        fontFamily: 'serif', fontSize: '16px', color: '#c9b890',
        letterSpacing: 6,
      }).setOrigin(0.5);

      card.add([mainTitle, subTitle]);

      this.tweens.add({
        targets: card, alpha: { from: 0, to: 1 },
        duration: 500, ease: 'Sine.easeOut',
        onComplete: () => {
          this.time.delayedCall(1000, () => {
            this.cameras.main.fadeIn(700, 0, 0, 0);
            this.cameras.main.once('camerafadeincomplete', () => {
              this.tweens.add({
                targets: card, alpha: 0, duration: 500,
                onComplete: () => card.destroy()
              });
              this.inputLocked = false;
            });
          });
        }
      });
    });
  }

  protected showChapterTitle(text: string): void {
    const title = this.add.text(this.scale.width / 2, 60, text, {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '28px',
      color: '#e8e4d8',
      fontStyle: 'bold'
    }).setOrigin(0.5).setDepth(100).setScrollFactor(0).setAlpha(0);

    this.tweens.add({
      targets: title,
      alpha: 1,
      duration: 600,
      yoyo: true,
      hold: 1600,
      ease: 'Quad.easeOut',
      onComplete: () => title.destroy()
    });
  }

  protected showSpeech(text: string): void {
    const bubble = this.add.text(this.player.x, this.player.y - 40, text, {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '15px',
      color: '#f0ece0',
      backgroundColor: 'rgba(15,13,18,0.85)',
      padding: { x: 10, y: 6 },
      wordWrap: { width: 240 }
    }).setOrigin(0.5).setDepth(50).setAlpha(0);

    this.tweens.add({
      targets: bubble,
      alpha: 1,
      duration: 200,
      yoyo: true,
      hold: 1800,
      onComplete: () => bubble.destroy()
    });
  }

  // —— 物品放大查看 ——
  // 弹出全屏暗色界面，展示物品大图 + 描述文字；按 E / 空格 / 点击 关闭
  protected showZoomView(textureKey: string, description: string, title?: string, scale: number = 3, closeCallback?: () => void): void {
    if (this.zoomOverlay) return;
    this.inputLocked = true;
    this.promptText.setVisible(false);

    const W = this.scale.width;
    const H = this.scale.height;
    const overlay = this.add.container(0, 0).setDepth(400).setScrollFactor(0);

    // 暗色背景
    const bg = this.add.rectangle(W / 2, H / 2, W, H, 0x000000, 0.85);
    bg.setInteractive();
    overlay.add(bg);

    // 物品大图（放大居中偏上）
    const img = this.add.image(W / 2, H / 2 - 50, textureKey).setScale(scale);
    overlay.add(img);

    const imgBottom = (H / 2 - 50) + (img.height * scale) / 2;

    // 标题
    let textY = imgBottom + 28;
    if (title) {
      const titleText = this.add.text(W / 2, textY, title, {
        fontFamily: '"PingFang SC","Microsoft YaHei",serif',
        fontSize: '20px', color: '#f5c97a', fontStyle: 'bold',
        stroke: '#000000', strokeThickness: 4
      }).setOrigin(0.5);
      overlay.add(titleText);
      textY += 32;
    }

    // 描述
    const desc = this.add.text(W / 2, textY, description, {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '15px', color: '#e8e4d8',
      backgroundColor: 'rgba(15,13,18,0.6)',
      padding: { x: 14, y: 8 },
      wordWrap: { width: W - 120 },
      align: 'center',
      lineSpacing: 4
    }).setOrigin(0.5, 0);
    overlay.add(desc);

    // 关闭提示
    const hint = this.add.text(W / 2, H - 28, t('hint_close'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '13px', color: '#8a8275'
    }).setOrigin(0.5);
    overlay.add(hint);

    // 淡入
    overlay.setAlpha(0);
    this.tweens.add({ targets: overlay, alpha: 1, duration: 200 });

    this.zoomOverlay = overlay;
    // 保存关闭回调（关闭时触发一次）
    this._zoomCloseCallback = closeCallback;

    // 点击关闭
    bg.on('pointerdown', () => this.closeZoomView());
  }

  protected closeZoomView(): void {
    if (!this.zoomOverlay) return;
    const overlay = this.zoomOverlay;
    const cb = this._zoomCloseCallback;
    this.zoomOverlay = undefined;
    this._zoomCloseCallback = undefined;
    this._overlayClosing = true;  // 防穿透：淡出期间屏蔽 ESC 退出游戏
    this.tweens.add({
      targets: overlay, alpha: 0, duration: 160,
      onComplete: () => {
        overlay.destroy();
        this.inputLocked = false;
        this._overlayClosing = false;
        if (cb) cb();
      }
    });
  }

  // 创建可放大查看的 POI：交互时弹出物品大图 + 描述
  protected addZoomablePoi(tx: number, ty: number, label: string, textureKey: string, scale: number, title: string, description: string): Poi {
    return this.addPoi(tx, ty, label, {
      type: 'info',
      onInteract: () => this.showZoomView(textureKey, description, title, scale)
    });
  }

  // ============================================================
  // 数北方灯火小游戏：限时内点击全部闪烁光点
  // 玩法：20 秒内点击 8 盏「北方的灯火」，点错不扣分，时间到未点完则失败
  //  ============================================================
  protected startNBLightGame(onDone?: (success: boolean, count: number) => void): void {
    if (this.nbLightOverlay) return;
    this.inputLocked = true;
    this.promptText.setVisible(false);
    this.nbLightOnDone = onDone;
    this.nbLightDots = [];
    this.nbLightHitCount = 0;
    this.nbLightTarget = 8;     // 目标：点亮 8 盏灯
    this.nbLightTimeLeft = 20;  // 时间：20 秒

    const W = this.scale.width;
    const H = this.scale.height;
    const overlay = this.add.container(0, 0).setDepth(400).setScrollFactor(0);
    this.nbLightOverlay = overlay;

    // 暗色背景（模拟夜色）
    const bg = this.add.rectangle(W / 2, H / 2, W, H, 0x03050c, 0.92);
    bg.setInteractive();
    overlay.add(bg);

    // 远山剪影（装饰）
    const mountains = this.add.graphics();
    mountains.fillStyle(0x0a0e1c, 1);
    mountains.beginPath();
    mountains.moveTo(0, H * 0.75);
    for (let x = 0; x <= W; x += 40) {
      const y = H * 0.75 - 20 - Math.sin(x * 0.02) * 30 - Math.random() * 10;
      mountains.lineTo(x, y);
    }
    mountains.lineTo(W, H);
    mountains.lineTo(0, H);
    mountains.closePath();
    mountains.fillPath();
    overlay.add(mountains);

    // 标题
    const title = this.add.text(W / 2, 36, t('lightgame_title'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '22px', color: '#f5c97a', fontStyle: 'bold',
      stroke: '#000000', strokeThickness: 5
    }).setOrigin(0.5);
    overlay.add(title);

    // 副标题（玩法说明）
    const sub = this.add.text(W / 2, 66, t('lightgame_subtitle'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '14px', color: '#a0a8c0'
    }).setOrigin(0.5);
    overlay.add(sub);

    // 操作提示（醒目）
    const howto = this.add.text(W / 2, 90, t('lightgame_howto'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '13px', color: '#8ad8ff'
    }).setOrigin(0.5);
    overlay.add(howto);

    // 进度条文字
    this.nbLightText = this.add.text(W / 2, 114, `${t('lightgame_progress')} 0 / 8`, {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '16px', color: '#f5c97a', fontStyle: 'bold'
    }).setOrigin(0.5);
    overlay.add(this.nbLightText);

    // 倒计时文字（右上角）
    this.nbLightTimerText = this.add.text(W - 50, 30, '20s', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '20px', color: '#ff8a8a', fontStyle: 'bold'
    }).setOrigin(0.5);
    overlay.add(this.nbLightTimerText);

    // 退出提示（左上角，明确告知只是放弃小游戏）
    const hint = this.add.text(50, 30, t('lightgame_esc'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '12px', color: '#6a6258'
    }).setOrigin(0.5);
    overlay.add(hint);

    // 生成 8 盏灯火（在屏幕中部 80%×60% 区域）
    const marginX = W * 0.1;
    const marginY = H * 0.22;
    const playW = W - marginX * 2;
    const playH = H * 0.5;

    for (let i = 0; i < this.nbLightTarget; i++) {
      const lx = marginX + playW * (0.08 + Math.random() * 0.84);
      const ly = marginY + playH * (0.1 + Math.random() * 0.8);
      const sizes: Array<'s' | 'm' | 'l'> = ['s', 'm', 'l'];
      const size = sizes[Math.floor(Math.random() * 3)];
      const dot = this.sceneArt.placeNBLight(lx, ly, size);
      // 初始隐藏（延迟一个个出现）
      dot.setVisible(false);
      dot.setAlpha(0);
      dot.setInteractive({ useHandCursor: true });
      // 点击：收集！
      dot.on('pointerdown', () => this._hitLightDot(dot));
      overlay.add(dot);
      this.nbLightDots.push(dot);
      // 逐个淡入，模拟灯火渐渐亮起
      this.time.delayedCall(i * 250, () => {
        if (!this.nbLightOverlay) return;
        dot.setVisible(true);
        this.tweens.add({ targets: dot, alpha: 1, duration: 220 });
      });
    }

    // 倒计时（每秒 -1）
    this.nbLightTimer = this.time.addEvent({
      delay: 1000,
      loop: true,
      callback: () => {
        this.nbLightTimeLeft--;
        if (this.nbLightTimerText) {
          this.nbLightTimerText.setText(`${Math.max(0, this.nbLightTimeLeft)}s`);
          if (this.nbLightTimeLeft <= 5) {
            this.nbLightTimerText.setColor('#ff4a4a');
          }
        }
        if (this.nbLightTimeLeft <= 0) {
          this._finishLightGame(false);
        }
      }
    });

    // 淡入
    overlay.setAlpha(0);
    this.tweens.add({ targets: overlay, alpha: 1, duration: 220 });
  }

  // 点击了一盏灯
  private _hitLightDot(dot: Phaser.GameObjects.Image): void {
    if (!this.nbLightOverlay || !dot.visible) return;
    this.nbLightHitCount++;
    if (this.nbLightText) {
      this.nbLightText.setText(`${t('lightgame_progress')} ${this.nbLightHitCount} / ${this.nbLightTarget}`);
    }
    // 点击反馈：粒子 + 变亮 + 消失
    this.burstSparkle(dot.x, dot.y, 0x8ad8ff);
    this.tweens.add({
      targets: dot,
      scale: 1.8,
      alpha: 0,
      duration: 180,
      onComplete: () => { dot.setVisible(false); dot.disableInteractive(); }
    });
    // 完成目标
    if (this.nbLightHitCount >= this.nbLightTarget) {
      this.time.delayedCall(200, () => this._finishLightGame(true));
    }
  }

  // 结束灯火小游戏
  private _finishLightGame(success: boolean): void {
    if (!this.nbLightOverlay) return;
    if (this.nbLightTimer) { this.nbLightTimer.remove(); this.nbLightTimer = undefined; }
    const cb = this.nbLightOnDone;
    const count = this.nbLightHitCount;
    const overlay = this.nbLightOverlay;
    this.nbLightOverlay = undefined;
    this.nbLightOnDone = undefined;
    this.nbLightDots = [];
    // 结果提示
    const W = this.scale.width;
    const H = this.scale.height;
    const result = this.add.text(W / 2, H / 2 - 20,
      success ? `${t('lightgame_result_win')} ${count} ${t('lightgame_seconds')}` : `${t('lightgame_result_lose')} ${count} ${t('lightgame_lights')}`,
      {
        fontFamily: '"PingFang SC","Microsoft YaHei",serif',
        fontSize: '28px', color: success ? '#f5c97a' : '#a0a8c0',
        fontStyle: 'bold', stroke: '#000000', strokeThickness: 6
      }
    ).setOrigin(0.5).setDepth(450).setScrollFactor(0);
    overlay.add(result);
    // 设置防穿透标志（淡出期间屏蔽 ESC 退出游戏）
    this._overlayClosing = true;
    // 淡出
    this.tweens.add({
      targets: overlay, alpha: 0, duration: 500, delay: 900,
      onComplete: () => {
        overlay.destroy();
        this.inputLocked = false;
        this._overlayClosing = false;
        if (cb) cb(success, count);
      }
    });
  }

  // 放弃灯火小游戏（ESC 触发）
  protected closeNBLightGame(): void {
    if (!this.nbLightOverlay) return;
    this._finishLightGame(false);
  }

  // ============================================================
  // 通用简单选项面板：屏幕底部弹出 n 个选项，键盘/鼠标操作
  //  ============================================================
  protected showSimpleChoices(title: string, options: string[], onChoose: (index: number) => void): void {
    if (this.simpleChoiceOverlay) return;
    this.inputLocked = true;
    this.promptText.setVisible(false);
    this.simpleChoiceResult = onChoose;
    this.simpleChoiceCursor = 0;
    this.simpleChoiceLabels = [];
    this.simpleChoiceBars = [];

    const W = this.scale.width;
    const H = this.scale.height;
    const overlay = this.add.container(0, 0).setDepth(390).setScrollFactor(0);
    this.simpleChoiceOverlay = overlay;

    // 半透明背景
    const bg = this.add.rectangle(W / 2, H / 2, W, H, 0x000000, 0.55).setInteractive();
    overlay.add(bg);

    // 面板背景
    const barW = W - 140;
    const panelH = 60 + options.length * 42;
    const panelY = H - panelH / 2 - 20;
    const panel = this.add.rectangle(W / 2, panelY, W - 100, panelH, 0x0e0c10, 0.92);
    panel.setStrokeStyle(1.5, 0xf5c97a, 0.7);
    overlay.add(panel);

    // 标题
    const titleText = this.add.text(W / 2, panelY - panelH / 2 + 22, title, {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '16px', color: '#f5c97a', fontStyle: 'bold'
    }).setOrigin(0.5);
    overlay.add(titleText);

    // 选项条 + 文字
    const barLeft = W / 2 - barW / 2;
    options.forEach((opt, i) => {
      const oy = panelY - panelH / 2 + 52 + i * 42;
      // 选项条背景
      const bar = this.add.rectangle(W / 2, oy, barW, 36, 0x1a1620, 0.9);
      bar.setStrokeStyle(1, 0x3a3238, 0.8);
      bar.setInteractive({ useHandCursor: true });
      bar.on('pointerover', () => this._setSimpleChoice(i));
      bar.on('pointerdown', () => this._confirmSimpleChoice());
      overlay.add(bar);
      this.simpleChoiceBars.push(bar);
      // 选项文字（左对齐在选项条内）
      const txt = this.add.text(barLeft + 16, oy, `${i + 1}. ${opt}`, {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '15px', color: '#a09a8c'
      }).setOrigin(0, 0.5);
      overlay.add(txt);
      this.simpleChoiceLabels.push(txt);
    });

    // 操作提示
    const hint = this.add.text(W / 2, panelY + panelH / 2 - 14, t('simple_choice_hint'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '12px', color: '#6a6258'
    }).setOrigin(0.5);
    overlay.add(hint);

    this._setSimpleChoice(0);

    // 淡入
    overlay.setAlpha(0);
    this.tweens.add({ targets: overlay, alpha: 1, duration: 180 });
  }

  private _setSimpleChoice(i: number): void {
    if (!this.simpleChoiceOverlay) return;
    this.simpleChoiceCursor = i;
    // 更新文字颜色
    this.simpleChoiceLabels.forEach((t, idx) => {
      if (idx === i) t.setColor('#f5c97a').setFontStyle('bold');
      else t.setColor('#a09a8c').setFontStyle('normal');
    });
    // 更新选项条高亮
    this.simpleChoiceBars.forEach((b, idx) => {
      if (idx === i) {
        b.setFillStyle(0x2a2430, 0.95);
        b.setStrokeStyle(1.2, 0xf5c97a, 0.8);
      } else {
        b.setFillStyle(0x1a1620, 0.9);
        b.setStrokeStyle(1, 0x3a3238, 0.8);
      }
    });
  }

  private _confirmSimpleChoice(): void {
    if (!this.simpleChoiceOverlay || !this.simpleChoiceResult) return;
    const idx = this.simpleChoiceCursor;
    const cb = this.simpleChoiceResult;
    const overlay = this.simpleChoiceOverlay;
    this.simpleChoiceOverlay = undefined;
    this.simpleChoiceResult = undefined;
    this.simpleChoiceLabels = [];
    this._overlayClosing = true;  // 防穿透
    this.tweens.add({
      targets: overlay, alpha: 0, duration: 140,
      onComplete: () => { overlay.destroy(); this.inputLocked = false; this._overlayClosing = false; cb(idx); }
    });
  }

  protected closeSimpleChoice(): void {
    if (!this.simpleChoiceOverlay) return;
    // 取消视为选 -1
    const cb = this.simpleChoiceResult;
    const overlay = this.simpleChoiceOverlay;
    this.simpleChoiceOverlay = undefined;
    this.simpleChoiceResult = undefined;
    this.simpleChoiceLabels = [];
    this._overlayClosing = true;  // 防穿透
    this.tweens.add({
      targets: overlay, alpha: 0, duration: 140,
      onComplete: () => { overlay.destroy(); this.inputLocked = false; this._overlayClosing = false; if (cb) cb(-1); }
    });
  }

  // 收集/达成反馈：在指定位置迸发光点
  protected burstSparkle(x: number, y: number, color = 0xf5c97a): void {
    const count = 10;
    for (let i = 0; i < count; i++) {
      const angle = (Math.PI * 2 * i) / count + Math.random() * 0.5;
      const dist = 26 + Math.random() * 20;
      const spark = this.add.image(x, y, 'spark').setDepth(60).setTint(color);
      this.tweens.add({
        targets: spark,
        x: x + Math.cos(angle) * dist,
        y: y + Math.sin(angle) * dist,
        alpha: { from: 1, to: 0 },
        scale: { from: 0.9, to: 0.1 },
        duration: 480 + Math.random() * 220,
        ease: 'Sine.easeOut',
        onComplete: () => spark.destroy()
      });
    }
  }

  // 任务完成 Toast：右上角浮入通知
  protected showToast(text: string): void {
    const W = this.scale.width;
    const toast = this.add.text(W / 2, 64, text, {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '15px',
      color: '#f5c97a',
      backgroundColor: 'rgba(15,13,18,0.92)',
      padding: { x: 16, y: 8 },
      stroke: '#000000', strokeThickness: 2
    }).setOrigin(0.5).setDepth(280).setScrollFactor(0).setAlpha(0);

    this.tweens.add({
      targets: toast,
      alpha: { from: 0, to: 1 },
      y: { from: 44, to: 64 },
      duration: 280, ease: 'Sine.easeOut',
      onComplete: () => {
        this.tweens.add({
          targets: toast,
          alpha: 0, y: 44,
          duration: 400, delay: 1700, ease: 'Sine.easeIn',
          onComplete: () => toast.destroy()
        });
      }
    });
  }

  // 任务完成统一入口：记录 flag + 弹 Toast（供子类调用）
  protected completeTaskWithToast(taskId: string, title: string): void {
    TaskSystem.inst.complete(taskId);
    this.showToast(`${t('task_complete')}${title}`);
  }

  protected updateDebug(): void {
    const s = GameState.inst;
    const tend = s.tendency;
    const b = s.bond;
    const ending = s.computeEnding();
    const fmt = (v: number) => (v >= 0 ? '+' : '') + v;
    const resolved = [...s.resolvedChoices].map(id => {
      const c = this.choiceSystem.chosenOption(id);
      return c ? `${id}=${c}` : id;
    }).join(', ') || t('dbg_none');
    const carried = s.carriedItem ? CARRY_ITEM_LABEL[s.carriedItem] : t('none_label');
    this.debugText.setText([
      `${t('dbg_protagonist')}: ${PLAYER_NAME}  ·  ${t('dbg_scene')}: ${this.sceneKey()}  ·  ${t('dbg_chapter')}: ${s.chapter}  ·  ${t('dbg_countdown')}: ${s.daysLeft}${t('dbg_days')}`,
      chapterMeta(s.chapter).title,
      `${t('dbg_commitment')} ${fmt(tend.commitment)}  ${t('dbg_rootedness')} ${fmt(tend.rootedness)}  ${t('dbg_agency')} ${fmt(tend.agency)}`,
      `${t('dbg_bond')}: ${t('dbg_bond_maya')} ${b.maya} / ${t('dbg_bond_noah')} ${b.noah} / ${t('dbg_bond_leo')} ${b.leo}  (${t('dbg_highest')}: ${s.topBond() ?? '—'})`,
      `${t('dbg_resolved')}: ${resolved}`,
      `${t('dbg_carry')}: ${carried}  ${t('dbg_flags')}: ${s.flags.size}`,
      `${t('dbg_ending')}: ${ending ? ENDING_LABEL[ending] : t('dbg_undecided')}`,
      '',
      `[Debug] ${t('dbg_next_chapter')}`
    ].join('\n'));
  }

  update(): void {
    // 防穿透：任何 overlay 正在淡出关闭时，屏蔽全部按键输入（含 ESC 退出游戏）
    if (this._overlayClosing) return;
    // 数北方灯火小游戏开启时：优先 ESC 放弃
    if (this.nbLightOverlay) {
      if (Phaser.Input.Keyboard.JustDown(this.keyEsc)) this.closeNBLightGame();
      return;
    }
    // 简单选项面板开启时：↑↓ / 回车 / ESC
    if (this.simpleChoiceOverlay) {
      if (this.cursors.up?.isDown || this.keyW.isDown) {
        if (!this._sUpLatch) {
          const n = this.simpleChoiceLabels.length;
          this._setSimpleChoice((this.simpleChoiceCursor - 1 + n) % n);
          this._sUpLatch = true;
        }
      } else this._sUpLatch = false;
      if (this.cursors.down?.isDown || this.keyS.isDown) {
        if (!this._sDownLatch) {
          this._setSimpleChoice((this.simpleChoiceCursor + 1) % this.simpleChoiceLabels.length);
          this._sDownLatch = true;
        }
      } else this._sDownLatch = false;
      if (Phaser.Input.Keyboard.JustDown(this.keyE) || Phaser.Input.Keyboard.JustDown(this.keySpace) || Phaser.Input.Keyboard.JustDown(this.input.keyboard!.addKey('ENTER'))) {
        this._confirmSimpleChoice();
      }
      if (Phaser.Input.Keyboard.JustDown(this.keyEsc)) this.closeSimpleChoice();
      return;
    }
    // 放大查看界面开启时：仅处理关闭按键，屏蔽其余交互
    if (this.zoomOverlay) {
      if (Phaser.Input.Keyboard.JustDown(this.keyE) || Phaser.Input.Keyboard.JustDown(this.keySpace)) {
        this.closeZoomView();
      }
      return;
    }
    this.dialogueSystem.update();
    this.handleMovement();
    this.handleInteraction();
    this.handleDebugKeys();
    this.updateTaskUI();
    this.updateChapterLabel();
    if (this.debugVisible) this.updateDebug();
  }

  protected handleMovement(): void {
    if (this.inputLocked) {
      this.player.setVelocity(0, 0);
      this.player.anims.stop();
      this.player.setFrame(`${this.currentDir}_1`);
      return;
    }
    const left = !!this.cursors.left?.isDown || this.keyA.isDown;
    const right = !!this.cursors.right?.isDown || this.keyD.isDown;
    const up = !!this.cursors.up?.isDown || this.keyW.isDown;
    const down = !!this.cursors.down?.isDown || this.keyS.isDown;
    const running = this.keyShift.isDown;

    let vx = 0, vy = 0;
    if (left) vx -= 1;
    if (right) vx += 1;
    if (up) vy -= 1;
    if (down) vy += 1;

    const moving = vx !== 0 || vy !== 0;
    if (moving) {
      if (vx !== 0 && vy !== 0) { vx *= 0.7071; vy *= 0.7071; }
      const speed = running ? PLAYER_SPEED * 1.65 : PLAYER_SPEED;
      this.player.setVelocity(vx * speed, vy * speed);
      let dir: Direction;
      if (Math.abs(vy) >= Math.abs(vx)) dir = vy < 0 ? 'up' : 'down';
      else dir = vx < 0 ? 'left' : 'right';
      const animKey = `player_walk_${dir}`;
      if (this.player.anims.currentAnim?.key !== animKey) this.player.play(animKey, true);
      // 奔跑时加快动画节奏
      this.player.anims.timeScale = running ? 1.5 : 1;
      this.currentDir = dir;
      // 奔跑尘土
      if (running) this.maybeSpawnDust();
    } else {
      this.player.setVelocity(0, 0);
      this.player.anims.stop();
      this.player.anims.timeScale = 1;
      this.player.setFrame(`${this.currentDir}_1`);
    }
  }

  // 奔跑时脚下偶发尘土
  private dustCooldown = 0;
  private maybeSpawnDust(): void {
    const now = this.time.now;
    if (now < this.dustCooldown) return;
    this.dustCooldown = now + 120;
    const d = this.add.image(this.player.x, this.player.y + 14, 'spark')
      .setDepth(4).setTint(0x8a8275).setScale(0.5).setAlpha(0.5);
    this.tweens.add({
      targets: d,
      alpha: 0, scale: 0.1,
      x: d.x + Phaser.Math.Between(-6, 6),
      y: d.y + Phaser.Math.Between(0, 4),
      duration: 280, ease: 'Sine.easeOut',
      onComplete: () => d.destroy()
    });
  }

  protected handleInteraction(): void {
    if (this.inputLocked) return;
    const all: Interactable[] = [...this.pois, ...this.npcInteractables];
    let closest: Interactable | null = null;
    let minDist = 64; // 略微放宽交互距离，提升手感
    for (const it of all) {
      const x = it.tileX * TILE_SIZE + TILE_SIZE / 2;
      const y = it.tileY * TILE_SIZE + TILE_SIZE / 2;
      const d = Phaser.Math.Distance.Between(this.player.x, this.player.y, x, y);
      if (d < minDist) { minDist = d; closest = it; }
    }

    if (closest !== this.nearby) {
      // 恢复上一个交互对象的默认外观
      const prev = this.nearby as (Poi | NpcInteractable) | null;
      if (prev) {
        if ('marker' in prev) {
          this.tweens.add({ targets: prev.marker, scale: 1, duration: 200 });
          prev.labelText?.setAlpha(0.75);
        } else if ('nameText' in prev) {
          prev.nameText.setColor('#e8e4d8');
        }
      }
      // 高亮当前交互对象
      this.nearby = closest;
      const cur = closest as (Poi | NpcInteractable) | null;
      if (cur) {
        if ('marker' in cur) {
          this.tweens.add({ targets: cur.marker, scale: 1.35, duration: 200 });
          cur.labelText?.setAlpha(1);
        } else if ('nameText' in cur) {
          cur.nameText.setColor('#f5c97a');
        }
      }
      if (closest) this.promptText.setText(`${t('press_e')}${closest.label}`).setVisible(true);
      else this.promptText.setVisible(false);
    }

    if (closest && Phaser.Input.Keyboard.JustDown(this.keyE)) {
      closest.onInteract();
    }
  }

  protected handleDebugKeys(): void {
    if (Phaser.Input.Keyboard.JustDown(this.keyP)) {
      this.debugVisible = !this.debugVisible;
      this.debugText.setVisible(this.debugVisible);
    }
    if (Phaser.Input.Keyboard.JustDown(this.keyT)) this.advanceChapter();
    if (Phaser.Input.Keyboard.JustDown(this.keyR)) this.resetGame();
    if (Phaser.Input.Keyboard.JustDown(this.keyEsc)) this.quitToTitle();
  }
}
