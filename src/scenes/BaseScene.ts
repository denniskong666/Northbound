// 场景基类：提取玩家移动、输入、相机、对话系统、调试面板、POI/NPC/门交互等通用逻辑
// 子类只需实现 getMap / getSpawnTile / spawnContent，并可覆盖 applyChapterContent / registerChoices
// 门（addDoor）是特殊的 POI，交互后 fadeOut 切换到目标场景

import Phaser from 'phaser';
import { TILE_SIZE, PLAYER_SPEED, Direction, PLAYER_NAME } from '../config/GameConfig';
import { GameState, ENDING_LABEL, CARRY_ITEM_LABEL } from '../state/GameState';
import { chapterMeta, ChapterId } from '../state/Chapter';
import { ChoiceSystem } from '../systems/ChoiceSystem';
import { DialogueSystem } from '../systems/DialogueSystem';
import { TaskSystem } from '../systems/TaskSystem';
import { NpcPlacement, NpcProfile, getNpcProfile } from '../data/NpcDefs';
import { DIALOGUES, DIALOGUES_DAILY } from '../data/Dialogues';

// 可交互点（POI：发光标记 + 文字交互）
export interface Poi {
  marker: Phaser.GameObjects.Image;
  tileX: number;
  tileY: number;
  label: string;
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
  protected keyW!: Phaser.Input.Keyboard.Key;
  protected keyA!: Phaser.Input.Keyboard.Key;
  protected keyS!: Phaser.Input.Keyboard.Key;
  protected keyD!: Phaser.Input.Keyboard.Key;
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

    this.registerChoices();
    this.buildMap();
    this.createPlayer();
    this.setupInput();
    this.setupCamera();
    this.createUI();
    this.spawnContent();
    this.setupDialogue();
    this.applyChapterContent(GameState.inst.chapter);
    this.updateChapterLabel();

    this.cameras.main.fadeIn(500, 0, 0, 0);
  }

  // 子类可覆盖：注册互斥任务（默认空）
  protected registerChoices(): void {}

  // 子类可覆盖：按章节刷新场景内容（默认空）
  protected applyChapterContent(_ch: ChapterId): void {}

  // —— 地图构建 ——
  protected buildMap(): void {
    const map = this.getMap();
    this.walls = this.physics.add.staticGroup();
    for (let row = 0; row < map.length; row++) {
      for (let col = 0; col < map[row].length; col++) {
        const code = map[row][col];
        const x = col * TILE_SIZE + TILE_SIZE / 2;
        const y = row * TILE_SIZE + TILE_SIZE / 2;
        if (code === '1') {
          this.walls.create(x, y, 'tile_wall');
        } else {
          const tex = TILE_TEXTURE[code] ?? 'tile_ground';
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
  }

  protected setupCamera(): void {
    const worldW = this.mapWidthTiles * TILE_SIZE;
    const worldH = this.mapHeightTiles * TILE_SIZE;
    this.physics.world.setBounds(0, 0, worldW, worldH);
    this.cameras.main.setBounds(0, 0, worldW, worldH);
    this.cameras.main.startFollow(this.player, true, 0.12, 0.12);
  }

  protected createUI(): void {
    this.promptText = this.add.text(this.scale.width / 2, this.scale.height - 40, '', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '16px',
      color: '#f5c97a',
      backgroundColor: 'rgba(20,18,24,0.75)',
      padding: { x: 12, y: 6 }
    }).setOrigin(0.5).setDepth(100).setScrollFactor(0).setVisible(false);

    // 任务提示面板（左上常驻）
    this.taskText = this.add.text(12, 12, '', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '13px',
      color: '#e8e4d8',
      backgroundColor: 'rgba(15,13,18,0.7)',
      padding: { x: 10, y: 7 },
      lineSpacing: 3
    }).setDepth(150).setScrollFactor(0).setVisible(false);

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

  // 刷新章节标识（只显示"第X章"序号，不显示副标题）
  protected updateChapterLabel(): void {
    const full = chapterMeta(GameState.inst.chapter).title; // 形如 "第一章 · 既定计划"
    this.chapterText.setText(full.split(' · ')[0]);
  }

  // 刷新任务提示（仅在任务变化时重设文本）
  protected updateTaskUI(): void {
    const t = TaskSystem.inst.currentTask(GameState.inst.chapter);
    const id = t?.id ?? null;
    if (id !== this.currentTaskId) {
      this.currentTaskId = id;
      if (t) {
        this.taskText.setText(`【任务】${t.title}\n${t.goal}`).setVisible(true);
      } else {
        this.taskText.setVisible(false);
      }
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
          // 剧情对话完后切换日常对话：第一次用 DIALOGUES（含好感影响），之后用 DIALOGUES_DAILY（闲聊）
          const talkedFlag = `npc_${placement.id}_talked`;
          if (!GameState.inst.hasFlag(talkedFlag)) {
            // 第二章首次对话：根据 ch1 印记显示跨章连锁开场白
            const markLine = this.getMarkIntroLine(placement.id);
            if (markLine) {
              this.showSpeech(markLine);
              this.time.delayedCall(2000, () => {
                this.dialogueSystem.start(DIALOGUES[placement.id], () => {
                  GameState.inst.applyEffects({ flag: talkedFlag });
                });
              });
            } else {
              this.dialogueSystem.start(DIALOGUES[placement.id], () => {
                GameState.inst.applyEffects({ flag: talkedFlag });
              });
            }
          } else {
            this.dialogueSystem.start(DIALOGUES_DAILY[placement.id]);
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

  // 跨章印记连锁：第二章首次对话时，根据 ch1 印记返回开场白
  private getMarkIntroLine(npcId: string): string | null {
    if (GameState.inst.chapter !== 'ch2') return null;
    const mark = GameState.inst.getStoryMark('ch1');
    if (!mark) return null;
    // 仅 Maya/Noah 在第二章有跨章连锁台词
    if (npcId !== 'maya' && npcId !== 'noah') return null;
    if (mark === 'A1') return `${npcId === 'maya' ? '玛雅' : '诺亚'}：「听说你一心只想攒钱北上。」`;
    if (mark === 'C1') return `${npcId === 'maya' ? '玛雅' : '诺亚'}：「原来你也舍不得这座城市。」`;
    if (mark === 'B1') return `${npcId === 'maya' ? '玛雅' : '诺亚'}：「你看起来不像会急着做决定的人。」`;
    return null;
  }

  // —— POI / 门 ——
  protected addPoi(tx: number, ty: number, label: string, opts: { line?: string; onInteract?: () => void } = {}): Poi {
    const x = tx * TILE_SIZE + TILE_SIZE / 2;
    const y = ty * TILE_SIZE + TILE_SIZE / 2;
    const marker = this.add.image(x, y, 'marker').setDepth(5);
    this.tweens.add({
      targets: marker,
      scale: { from: 0.85, to: 1.15 },
      alpha: { from: 0.7, to: 1 },
      duration: 1200,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    });
    const poi: Poi = {
      marker, tileX: tx, tileY: ty, label,
      onInteract: opts.onInteract ?? (() => this.showSpeech(opts.line ?? ''))
    };
    this.pois.push(poi);
    return poi;
  }

  // 门：交互后 fadeOut 切换场景
  protected addDoor(tx: number, ty: number, label: string, targetScene: string, data?: object): Poi {
    return this.addPoi(tx, ty, label, {
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
    if (!n) { this.showSpeech('已是终章。'); return; }
    this.applyChapterContent(n);
    this.updateChapterLabel();
  }

  protected resetGame(): void {
    GameState.inst.reset();
    this.applyChapterContent(GameState.inst.chapter);
    this.updateChapterLabel();
    this.showSpeech('存档已重置。');
  }

  // 章节丝滑转场：fadeOut → [CG 动画插入点] → mid（推进章节状态）→ fadeIn
  // 不显示章节标题弹窗，但右上角常驻章节标识会随章节推进更新。
  // 未来 CG 制作完成后，在黑屏停留处接入预渲染动画播放即可。
  protected playChapterTransition(mid: () => void): void {
    this.inputLocked = true;
    this.promptText.setVisible(false);
    this.cameras.main.fadeOut(600, 0, 0, 0);
    this.cameras.main.once('camerafadeoutcomplete', () => {
      // ===== CG 动画插入点 =====
      // 未来在此播放预渲染 CG（文档第十三节），播放完毕后继续下方流程。
      // 当前以黑屏短暂停留作为占位。
      mid();
      this.updateChapterLabel();
      this.time.delayedCall(600, () => {
        this.cameras.main.fadeIn(700, 0, 0, 0);
        this.cameras.main.once('camerafadeincomplete', () => {
          this.inputLocked = false;
        });
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

  protected updateDebug(): void {
    const s = GameState.inst;
    const t = s.tendency;
    const b = s.bond;
    const ending = s.computeEnding();
    const fmt = (v: number) => (v >= 0 ? '+' : '') + v;
    const resolved = [...s.resolvedChoices].map(id => {
      const c = this.choiceSystem.chosenOption(id);
      return c ? `${id}=${c}` : id;
    }).join(', ') || '无';
    const carried = s.carriedItem ? CARRY_ITEM_LABEL[s.carriedItem] : '无';
    this.debugText.setText([
      `主角: ${PLAYER_NAME}  ·  场景: ${this.sceneKey()}  ·  章节: ${s.chapter}  ·  倒计时: ${s.daysLeft}天`,
      chapterMeta(s.chapter).title,
      `信守约定 ${fmt(t.commitment)}  联结故土 ${fmt(t.rootedness)}  自我主导 ${fmt(t.agency)}`,
      `羁绊: 玛雅 ${b.maya} / 诺亚 ${b.noah} / 利奥 ${b.leo}  (最高: ${s.topBond() ?? '—'})`,
      `已选互斥: ${resolved}`,
      `携带: ${carried}  叙事flag: ${s.flags.size}`,
      `结局: ${ending ? ENDING_LABEL[ending] : '未决定'}`,
      '',
      '[调试] T=下一章  R=重置  P=开关面板'
    ].join('\n'));
  }

  update(): void {
    this.dialogueSystem.update();
    this.handleMovement();
    this.handleInteraction();
    this.handleDebugKeys();
    this.updateTaskUI();
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

    let vx = 0, vy = 0;
    if (left) vx -= 1;
    if (right) vx += 1;
    if (up) vy -= 1;
    if (down) vy += 1;

    const moving = vx !== 0 || vy !== 0;
    if (moving) {
      if (vx !== 0 && vy !== 0) { vx *= 0.7071; vy *= 0.7071; }
      this.player.setVelocity(vx * PLAYER_SPEED, vy * PLAYER_SPEED);
      let dir: Direction;
      if (Math.abs(vy) >= Math.abs(vx)) dir = vy < 0 ? 'up' : 'down';
      else dir = vx < 0 ? 'left' : 'right';
      const animKey = `player_walk_${dir}`;
      if (this.player.anims.currentAnim?.key !== animKey) this.player.play(animKey, true);
      this.currentDir = dir;
    } else {
      this.player.setVelocity(0, 0);
      this.player.anims.stop();
      this.player.setFrame(`${this.currentDir}_1`);
    }
  }

  protected handleInteraction(): void {
    if (this.inputLocked) return;
    const all: Interactable[] = [...this.pois, ...this.npcInteractables];
    let closest: Interactable | null = null;
    let minDist = 56;
    for (const it of all) {
      const x = it.tileX * TILE_SIZE + TILE_SIZE / 2;
      const y = it.tileY * TILE_SIZE + TILE_SIZE / 2;
      const d = Phaser.Math.Distance.Between(this.player.x, this.player.y, x, y);
      if (d < minDist) { minDist = d; closest = it; }
    }

    if (closest !== this.nearby) {
      this.nearby = closest;
      if (closest) this.promptText.setText(`按 E — ${closest.label}`).setVisible(true);
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
  }
}
