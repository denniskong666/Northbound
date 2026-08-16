// 终章场景：你来吗？
// 全程读取 1-4 章所有选择，根据全套印记与好感展示差异化结局画面
// 四种结局，每种结局有特定的画面彩蛋和物品描述
// 物品描述会根据玩家在 1-4 章的具体印记而变化，体现前后关联
//
// 完整决策链：
//   ch1 印记(A1/B1/C1) → 记账本描述、Leo旧物描述、ch3开场台词、ch4开场台词
//   ch2 印记(A1/B1/C1) → ch3开场台词、ch4开场台词
//   ch3 印记(A3/B3/C3) → 通行材料描述、Maya画描述、小幅画作描述、ch4开场台词
//   ch4 印记(A4/B4/C4) → 旅行轿车描述、封存材料描述、远行物资描述、行李描述
//   isHighCommitment   → 北上结局/暂缓结局的准备程度细节
//   isHighRootedness   → 故土结局的联结程度细节
//   topBond()          → 各结局中最高羁绊角色的突出呈现
//   carriedItem        → 北上结局中携带的具体物品
//   bond.{maya,noah,leo} → 各结局的羁绊物品描述分支

import Phaser from 'phaser';
import { BaseScene, Poi, PoiType } from './BaseScene';
import { GameState, EndingType, ENDING_LABEL, CARRY_ITEM_LABEL } from '../state/GameState';
import { ChapterId } from '../state/Chapter';
import { L, t } from '../systems/I18n';

// 羁绊角色名映射
const BOND_NAMES: Record<'maya' | 'noah' | 'leo', string> = {
  maya: t('npc_maya'), noah: t('npc_noah'), leo: t('npc_leo')
};

// 所有结局均使用静态背景+正常tile走动+按E交互
const CUSTOM_ENDINGS: EndingType[] = [];

const POI_TINT: Record<PoiType, number> = {
  item: 0xf5c97a, task: 0x6bd4f0, door: 0x9adf8a, info: 0xd8c9a0
};

// 终章地图（仅用于碰撞边界，自定义结局时会被隐藏）
const EPILOGUE_MAP: string[] = [
  '1111111111111111111111111',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1111111111111111111111111'
];

export class EpilogueScene extends BaseScene {
  // 汽车场景滚动层
  private roadTile?: Phaser.GameObjects.TileSprite;
  private farTile?: Phaser.GameObjects.TileSprite;
  private midTile?: Phaser.GameObjects.TileSprite;
  // 雨夜场景雨滴
  private rainDrops: Phaser.GameObjects.Rectangle[] = [];
  // marker纹理是否已创建
  private markerCreated = false;

  constructor() {
    super('EpilogueScene');
  }

  protected sceneKey(): string { return 'EpilogueScene'; }
  protected getMap(): string[] { return EPILOGUE_MAP; }
  protected getSpawnTile(): { x: number; y: number } { return { x: 12, y: 7 }; }

  // 自定义结局：隐藏tile地图
  protected buildMap(): void {
    super.buildMap();
    const ending = GameState.inst.computeEnding();
    if (ending && CUSTOM_ENDINGS.includes(ending)) {
      this.children.list.forEach((child: any) => {
        const k = child?.texture?.key ?? '';
        if (k.startsWith('tile_ground') || k.startsWith('tile_wall')) {
          child.setAlpha(0);
        }
      });
      if (this.walls) {
        (this.walls.children as any).iterate((child: any) => { if (child) child.setAlpha(0); });
      }
      if (this.player) {
        this.player.setVisible(false);
        if (this.player.body) this.player.body.enable = false;
      }
    }
  }

  // 自定义结局：禁止玩家移动
  protected handleMovement(): void {
    const ending = GameState.inst.computeEnding();
    if (ending && CUSTOM_ENDINGS.includes(ending)) return;
    super.handleMovement();
  }

  // 自定义结局：用屏幕像素坐标处理POI交互
  protected handleInteraction(): void {
    const ending = GameState.inst.computeEnding();
    if (ending && CUSTOM_ENDINGS.includes(ending)) {
      if (this.nearby && this.input.keyboard!.addKey('E').isDown) {
        this.nearby.onInteract();
        this.inputLocked = true;
        this.time.delayedCall(300, () => { this.inputLocked = false; });
      }
      return;
    }
    super.handleInteraction();
  }

  // 汽车场景滚动 + 雨夜雨滴更新
  update(): void {
    super.update();
    if (this.roadTile) this.roadTile.tilePositionX -= 5;
    if (this.midTile) this.midTile.tilePositionX -= 2;
    if (this.farTile) this.farTile.tilePositionX -= 0.8;
    if (this.rainDrops.length > 0) {
      const W = this.scale.width;
      const H = this.scale.height;
      for (const drop of this.rainDrops) {
        drop.y += 12;
        drop.x -= 2;
        if (drop.y > H) { drop.y = -20; drop.x = Phaser.Math.Between(0, W + 40); }
      }
    }
  }

  // 色调：各结局不同氛围
  protected getChapterTint(_ch: ChapterId): { color: number; alpha: number } {
    switch (GameState.inst.computeEnding()) {
      case 'go_north':      return { color: 0x0a0a2a, alpha: 0.35 };  // 深蓝夜空
      case 'return_home':   return { color: 0x4a3520, alpha: 0.20 };  // 暖黄黄昏
      case 'unknown_path':  return { color: 0x2a2a30, alpha: 0.25 };  // 灰蓝黎明
      case 'pause_journey': return { color: 0x0a1020, alpha: 0.35 };  // 暗蓝雨夜
      default:              return { color: 0x1a1620, alpha: 0.12 };
    }
  }

  protected spawnContent(): void {
    const ending = GameState.inst.computeEnding();

    this.ensureMarkerTexture();

    // 背景氛围（depth=-1 静态背景）
    this.spawnAtmosphere(ending);

    // 结局标题
    this.showEndingTitle(ending);

    if (!ending) {
      this.addPoi(12, 5, L('回到标题', 'Return to Title'), {
        onInteract: () => { GameState.inst.reset(); this.scene.start('TitleScene'); }
      });
      return;
    }

    // 所有结局：正常tile走动 + addPoi
    switch (ending) {
      case 'go_north':      this.spawnGoNorthItems(); break;
      case 'return_home':   this.spawnReturnHomeItems(); break;
      case 'unknown_path':  this.spawnUnknownPathItems(); break;
      case 'pause_journey': this.spawnPauseJourneyItems(); break;
    }

    // 重新开始（tile坐标，右下角）
    this.addPoi(22, 8, L('重新开始', 'Restart'), {
      onInteract: () => { GameState.inst.reset(); this.scene.start('TitleScene'); }
    });
  }

  // 确保marker光点纹理存在
  private ensureMarkerTexture(): void {
    if (this.markerCreated) return;
    this.markerCreated = true;
    if (!this.textures.exists('marker')) {
      const tw = 20, th = 28;
      const tex = this.textures.createCanvas('marker', tw, th);
      const ctx = tex!.getContext();
      ctx.beginPath();
      ctx.moveTo(tw / 2, 2);
      ctx.lineTo(tw - 2, th / 2);
      ctx.lineTo(tw / 2, th - 2);
      ctx.lineTo(2, th / 2);
      ctx.closePath();
      ctx.fillStyle = '#f5c97a';
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(tw / 2, 6);
      ctx.lineTo(tw - 6, th / 2);
      ctx.lineTo(tw / 2, th - 6);
      ctx.lineTo(6, th / 2);
      ctx.closePath();
      ctx.fillStyle = '#fff5d0';
      ctx.fill();
      tex!.refresh();
    }
  }

  // 屏幕像素坐标版addPoi（用于自定义结局场景）
  protected addPoiAtPixel(x: number, y: number, label: string, opts: { line?: string; type?: PoiType; onInteract?: () => void } = {}): Poi {
    const type: PoiType = opts.type ?? 'info';
    const marker = this.add.image(x, y, 'marker')
      .setDepth(50).setScrollFactor(0).setTint(POI_TINT[type]);
    this.tweens.add({
      targets: marker,
      scale: { from: 0.7, to: 1.2 },
      alpha: { from: 0.6, to: 1 },
      duration: 1200, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
    });
    const labelText = this.add.text(x, y - 24, label, {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '12px', color: '#f5c97a', stroke: '#000000', strokeThickness: 3,
    }).setOrigin(0.5).setDepth(51).setScrollFactor(0).setAlpha(0.85);

    const hitArea = this.add.circle(x, y, 22, 0xffff00, 0)
      .setDepth(49).setScrollFactor(0).setInteractive({ useHandCursor: true });
    hitArea.on('pointerdown', () => {
      this.nearby = poi;
      this.handleInteraction();
    });

    const poi: Poi = {
      marker, labelText, tileX: -1, tileY: -1, label, type,
      onInteract: opts.onInteract ?? (() => this.showSpeech(opts.line ?? ''))
    };
    this.pois.push(poi);
    return poi;
  }

  // —— 背景氛围 ——
  private spawnAtmosphere(ending: EndingType | null): void {
    const W = this.scale.width;
    const H = this.scale.height;

    // 北上结局：动态汽车公路场景
    if (ending === 'go_north') {
      this.spawnCarScene();
      return;
    }
    // 留下结局：明亮旧街区场景
    if (ending === 'return_home') {
      this.spawnHomeScene();
      return;
    }
    // 独行结局：独自行走公路场景
    if (ending === 'unknown_path') {
      this.spawnWalkScene();
      return;
    }
    // 暂缓结局：雨夜窗边场景
    if (ending === 'pause_journey') {
      this.spawnRainScene();
      return;
    }

    // 兜底
    const bg = this.add.graphics();
    bg.fillGradientStyle(0x08080d, 0x08080d, 0x14111a, 0x14111a, 1);
    bg.fillRect(0, 0, W, H);
  }

  // —— 北上结局：静态星空背景（玩家在tile地图上走动）——
  private spawnCarScene(): void {
    const W = this.scale.width;
    const H = this.scale.height;

    // 静态夜空渐变背景（depth=-1，在tile之下）
    const sky = this.add.graphics().setDepth(-1);
    sky.fillGradientStyle(0x06081a, 0x06081a, 0x0a0a2a, 0x101030, 1);
    sky.fillRect(0, 0, W, H);

    // 静态星星（不闪烁，在tile之下）
    for (let i = 0; i < 60; i++) {
      const x = Phaser.Math.Between(0, W);
      const y = Phaser.Math.Between(0, H);
      const r = Phaser.Math.FloatBetween(0.5, 1.6);
      this.add.circle(x, y, r, 0xe8e4d8, Phaser.Math.FloatBetween(0.3, 0.9)).setDepth(-1);
    }

    // 几颗大亮星（带光晕）
    for (let i = 0; i < 8; i++) {
      const x = Phaser.Math.Between(0, W);
      const y = Phaser.Math.Between(0, Math.floor(H * 0.5));
      this.add.circle(x, y, 6, 0xe8e4d8, 0.06).setDepth(-1);
      this.add.circle(x, y, 2, 0xfff5d0, 0.9).setDepth(-1);
    }
  }

  // 生成公路、远山、中景的 tileSprite 纹理
  private makeRoadTextures(): void {
    // —— 路面纹理 ——
    if (!this.textures.exists('bg_road')) {
      const rw = 96, rh = 200;
      const t = this.textures.createCanvas('bg_road', rw, rh);
      const ctx = t!.getContext();
      // 深色路面
      ctx.fillStyle = '#1a1a1e';
      ctx.fillRect(0, 0, rw, rh);
      // 路面噪点
      for (let i = 0; i < 40; i++) {
        ctx.fillStyle = `rgba(${Phaser.Math.Between(20,40)},${Phaser.Math.Between(20,40)},${Phaser.Math.Between(24,44)},0.6)`;
        ctx.fillRect(Phaser.Math.Between(0, rw), Phaser.Math.Between(0, rh), 2, 2);
      }
      // 中间黄色虚线
      ctx.fillStyle = '#c9a040';
      for (let y = 0; y < rh; y += 40) {
        ctx.fillRect(rw / 2 - 2, y, 4, 20);
      }
      // 路肩
      ctx.fillStyle = '#2a2a20';
      ctx.fillRect(0, 0, 6, rh);
      ctx.fillRect(rw - 6, 0, 6, rh);
      t!.refresh();
    }

    // —— 远山剪影 ——
    if (!this.textures.exists('bg_far')) {
      const fw = 200, fh = 80;
      const t = this.textures.createCanvas('bg_far', fw, fh);
      const ctx = t!.getContext();
      ctx.fillStyle = '#0e1020';
      // 起伏山脉
      ctx.beginPath();
      ctx.moveTo(0, fh);
      ctx.lineTo(0, 50);
      ctx.lineTo(30, 30); ctx.lineTo(60, 45); ctx.lineTo(90, 20);
      ctx.lineTo(120, 35); ctx.lineTo(150, 25); ctx.lineTo(180, 40);
      ctx.lineTo(200, 30); ctx.lineTo(200, fh);
      ctx.closePath();
      ctx.fill();
      t!.refresh();
    }

    // —— 中景树林/电线杆 ——
    if (!this.textures.exists('bg_mid')) {
      const mw = 200, mh = 60;
      const t = this.textures.createCanvas('bg_mid', mw, mh);
      const ctx = t!.getContext();
      ctx.fillStyle = '#080a14';
      // 几棵树剪影
      for (let i = 0; i < 5; i++) {
        const x = 20 + i * 40;
        const treeH = Phaser.Math.Between(30, 45);
        ctx.beginPath();
        ctx.moveTo(x, mh);
        ctx.lineTo(x - 5, mh - treeH + 8);
        ctx.lineTo(x, mh - treeH);
        ctx.lineTo(x + 5, mh - treeH + 8);
        ctx.closePath();
        ctx.fill();
        ctx.fillRect(x - 1, mh - treeH + 5, 2, treeH);
      }
      // 电线杆
      ctx.fillRect(100, 10, 2, mh - 10);
      ctx.fillRect(95, 12, 12, 2);
      t!.refresh();
    }
  }

  // 汽车内饰（完整车头截面 + 双人坐姿 + 仪表盘 + 方向盘）
  private spawnCarInterior(W: number, H: number): void {
    this.ensureSitTextures();
    const cx = W / 2;
    const dashTop = Math.floor(H * 0.60);

    // —— 挡风玻璃（上半部分，暗蓝色天空透过）——
    const windshield = this.add.graphics().setDepth(5).setScrollFactor(0);
    windshield.fillStyle(0x080a16, 0.25);
    windshield.fillRect(24, 12, W - 48, dashTop - 24);
    // 挡风玻璃边框
    windshield.fillStyle(0x1a1410, 1);
    windshield.fillRect(0, 0, W, 14);
    windshield.fillRect(0, 0, 24, dashTop);
    windshield.fillRect(W - 24, 0, 24, dashTop);
    // 挡风玻璃下边框（即仪表盘上沿）
    windshield.fillStyle(0x1e1818, 1);
    windshield.fillRect(0, dashTop - 6, W, 8);

    // —— 仪表盘主体 ——
    const dash = this.add.graphics().setDepth(6).setScrollFactor(0);
    dash.fillStyle(0x0d0a0e, 1);
    dash.beginPath();
    dash.moveTo(0, H);
    dash.lineTo(0, dashTop + 14);
    dash.lineTo(cx - 120, dashTop);
    dash.lineTo(cx + 120, dashTop);
    dash.lineTo(W, dashTop + 14);
    dash.lineTo(W, H);
    dash.closePath();
    dash.fillPath();
    dash.lineStyle(2, 0x3a3a48, 0.7);
    dash.beginPath();
    dash.moveTo(0, dashTop + 14);
    dash.lineTo(cx - 120, dashTop);
    dash.lineTo(cx + 120, dashTop);
    dash.lineTo(W, dashTop + 14);
    dash.strokePath();

    // —— 仪表灯（更大更亮）——
    const lightPositions = [cx - 90, cx - 35, cx + 35, cx + 90];
    for (const lx of lightPositions) {
      const g = this.add.circle(lx, dashTop + 10, 4, 0xf5a040, 0.85).setDepth(7).setScrollFactor(0);
      this.tweens.add({ targets: g, alpha: { from: 0.3, to: 1 }, duration: Phaser.Math.Between(600, 1400), yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });
      this.add.circle(lx, dashTop + 10, 14, 0xf5a040, 0.07).setDepth(6).setScrollFactor(0);
    }

    // —— 方向盘（更大）——
    const wheelX = cx - 70;
    const wheelY = dashTop + 50;
    const wheel = this.add.graphics().setDepth(8).setScrollFactor(0);
    wheel.lineStyle(4, 0x1a1418, 1);
    wheel.strokeCircle(wheelX, wheelY, 28);
    wheel.fillStyle(0x120e12, 1);
    wheel.fillCircle(wheelX, wheelY, 15);
    wheel.lineStyle(2, 0x3a3038, 0.6);
    wheel.lineBetween(wheelX - 22, wheelY, wheelX + 22, wheelY);
    wheel.lineBetween(wheelX, wheelY - 22, wheelX, wheelY + 22);

    // —— 坐姿人物（更大更显眼，scale 4.5）——
    const sitY = dashTop - 8;
    const p1 = this.add.image(cx - 55, sitY, 'sit_a')
      .setOrigin(0.5, 1).setScale(4.5).setTint(0x050508).setAlpha(0.95).setDepth(9).setScrollFactor(0);
    const p2 = this.add.image(cx + 55, sitY, 'sit_b')
      .setOrigin(0.5, 1).setScale(4.2).setTint(0x050508).setAlpha(0.95).setDepth(9).setScrollFactor(0);

    // 颠簸动画
    this.tweens.add({
      targets: [p1, p2, dash, windshield, wheel],
      y: '-=1', duration: 180, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
    });

    // 说明文字
    this.add.text(cx, dashTop - 100, L('北方很远，但你们一直在走。', "The North is far, but you keep walking."), {
      fontFamily: 'serif', fontSize: '14px', color: '#c8c0b0', letterSpacing: 3
    }).setOrigin(0.5).setDepth(60).setScrollFactor(0).setAlpha(0.7);
  }

  // —— 留下结局：暖色黄昏静态背景 ——
  private spawnHomeScene(): void {
    const W = this.scale.width;
    const H = this.scale.height;

    // 暖色黄昏天空（depth=-1，在tile之下）
    const sky = this.add.graphics().setDepth(-1);
    sky.fillGradientStyle(0x3a2a1a, 0x3a2a1a, 0x5a4030, 0x4a3525, 1);
    sky.fillRect(0, 0, W, H);

    // 暖色光晕
    this.add.circle(W * 0.7, Math.floor(H * 0.3), 120, 0xf5c97a, 0.08).setDepth(-1);

    // 建筑剪影
    const buildings = this.add.graphics().setDepth(-1);
    buildings.fillStyle(0x2a1a10, 0.9);
    const bldY = Math.floor(H * 0.42);
    let bx = 0;
    for (const bh of [60, 45, 80, 55, 70, 40, 65, 50, 75, 45, 60, 55]) {
      const bw = Phaser.Math.Between(40, 70);
      buildings.fillRect(bx, bldY - bh, bw, bh + 2);
      for (let wy = bldY - bh + 10; wy < bldY - 5; wy += 12) {
        for (let wx = bx + 5; wx < bx + bw - 5; wx += 10) {
          if (Math.random() > 0.4) {
            buildings.fillStyle(0xf5c97a, Phaser.Math.FloatBetween(0.3, 0.7));
            buildings.fillRect(wx, wy, 4, 6);
            buildings.fillStyle(0x2a1a10, 0.9);
          }
        }
      }
      bx += bw + Phaser.Math.Between(2, 8);
      if (bx > W) break;
    }

    // 街灯暖光点
    for (const lx of [W * 0.15, W * 0.45, W * 0.75, W * 0.92]) {
      this.add.circle(lx, bldY - 28, 8, 0xf5c97a, 0.12).setDepth(-1);
      this.add.circle(lx, bldY - 28, 2, 0xffe9b3, 0.8).setDepth(-1);
    }
  }

  // —— 独行结局：灰蓝黎明静态背景 ——
  private spawnWalkScene(): void {
    const W = this.scale.width;
    const H = this.scale.height;

    // 灰蓝黎明天空（depth=-1）
    const sky = this.add.graphics().setDepth(-1);
    sky.fillGradientStyle(0x1a1a22, 0x1a1a22, 0x2a2a30, 0x3a3a40, 1);
    sky.fillRect(0, 0, W, H);

    // 稀疏晨星
    for (let i = 0; i < 20; i++) {
      const x = Phaser.Math.Between(0, W);
      const y = Phaser.Math.Between(0, Math.floor(H * 0.4));
      this.add.circle(x, y, Phaser.Math.FloatBetween(0.5, 1.4), 0xa0a0b0, Phaser.Math.FloatBetween(0.2, 0.6)).setDepth(-1);
    }

    // 远山剪影
    const mountains = this.add.graphics().setDepth(-1);
    mountains.fillStyle(0x0e1020, 0.8);
    mountains.beginPath();
    mountains.moveTo(0, H);
    mountains.lineTo(0, Math.floor(H * 0.5));
    mountains.lineTo(W * 0.15, Math.floor(H * 0.35));
    mountains.lineTo(W * 0.3, Math.floor(H * 0.45));
    mountains.lineTo(W * 0.5, Math.floor(H * 0.30));
    mountains.lineTo(W * 0.7, Math.floor(H * 0.40));
    mountains.lineTo(W * 0.85, Math.floor(H * 0.32));
    mountains.lineTo(W, Math.floor(H * 0.42));
    mountains.lineTo(W, H);
    mountains.closePath();
    mountains.fillPath();
  }

  // —— 暂缓结局：暗蓝雨夜静态背景 ——
  private spawnRainScene(): void {
    const W = this.scale.width;
    const H = this.scale.height;

    // 暗蓝夜色（depth=-1）
    const sky = this.add.graphics().setDepth(-1);
    sky.fillGradientStyle(0x080a14, 0x080a14, 0x0e1020, 0x141828, 1);
    sky.fillRect(0, 0, W, H);

    // 暗色建筑剪影
    const buildings = this.add.graphics().setDepth(-1);
    buildings.fillStyle(0x060810, 0.9);
    const bldY = Math.floor(H * 0.45);
    let bx = 0;
    while (bx < W) {
      const bw = Phaser.Math.Between(35, 60);
      const bh = Phaser.Math.Between(30, 70);
      buildings.fillRect(bx, bldY - bh, bw, bh + 2);
      if (Math.random() > 0.5) {
        buildings.fillStyle(0x3a3520, 0.5);
        buildings.fillRect(bx + Phaser.Math.Between(3, bw - 8), bldY - bh + Phaser.Math.Between(5, 15), 3, 4);
        buildings.fillStyle(0x060810, 0.9);
      }
      bx += bw + Phaser.Math.Between(1, 4);
    }

    // 静态雨线（斜线）
    const rain = this.add.graphics().setDepth(-1);
    rain.lineStyle(1, 0x8a9ab0, 0.15);
    for (let i = 0; i < 40; i++) {
      const x = Phaser.Math.Between(0, W);
      const y = Phaser.Math.Between(0, H);
      rain.lineBetween(x, y, x - 6, y + 18);
    }
  }

  // —— 结局标题 ——
  private showEndingTitle(ending: EndingType | null): void {
    const W = this.scale.width;
    const label = ending ? ENDING_LABEL[ending] : L('未完的旅程', 'Unfinished Journey');
    const subtitle: Record<EndingType, string> = {
      go_north:      L('北方很远，但你们一直在走。', "The North is far, but you keep walking."),
      return_home:   L('这里的灯火，就是你要去的地方。', 'These lights are where you belong.'),
      unknown_path:  L('方向是你自己的。', 'The direction is your own.'),
      pause_journey: L('有些路，需要先停下来才能看清。', 'Some roads can only be seen clearly when you stop first.'),
      with_maya: '', with_noah: '', with_leo: ''
    };
    const sub = ending ? subtitle[ending] : '';
    const precond = ending ? GameState.inst.getEndingPrecondition(ending) : '';

    const title = this.add.text(W / 2, 48, `${L('【结局】', '[Ending] ')}${label}`, {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '30px', color: '#e8e4d8', fontStyle: 'bold'
    }).setOrigin(0.5).setAlpha(0).setDepth(60).setScrollFactor(0);

    const subText = this.add.text(W / 2, 84, sub, {
      fontFamily: 'serif', fontSize: '13px', color: '#8a8275', letterSpacing: 4
    }).setOrigin(0.5).setAlpha(0).setDepth(60).setScrollFactor(0);

    const precondText = this.add.text(W / 2, 104, precond, {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '11px', color: '#6b5d42',
      backgroundColor: 'rgba(15,13,18,0.55)',
      padding: { x: 10, y: 4 }
    }).setOrigin(0.5).setAlpha(0).setDepth(60).setScrollFactor(0);

    this.tweens.add({ targets: title, alpha: 1, duration: 1500, ease: 'Sine.easeOut' });
    this.tweens.add({ targets: subText, alpha: 1, duration: 2000, delay: 600, ease: 'Sine.easeOut' });
    this.tweens.add({ targets: precondText, alpha: 0.8, duration: 2000, delay: 1200, ease: 'Sine.easeOut' });
  }

  // 顶部信息条：显示全章节印记摘要 + 结局倾向提示
  private showChoiceSummary(): void {
    const gs = GameState.inst;
    const m1 = gs.getStoryMark('ch1') ?? '—';
    const m2 = gs.getStoryMark('ch2') ?? '—';
    const m3 = gs.getStoryMark('ch3') ?? '—';
    const m4 = gs.getStoryMark('ch4') ?? '—';
    const top = gs.topBond();
    const topText = top ? `${L('最高羁绊', 'Top Bond')}: ${BOND_NAMES[top]}` : t('none_label');
    const commitment = gs.isHighCommitment() ? L('信守', 'Steadfast') : L('动摇', 'Wavering');
    const rootedness = gs.isHighRootedness() ? L('留恋', 'Attached') : L('疏离', 'Detached');
    const suggested = gs.suggestEnding();
    const suggestLabel = suggested ? ENDING_LABEL[suggested] : '—';
    const precond = gs.getEndingPrecondition(suggested);

    const W = this.scale.width;
    const summary = this.add.text(W / 2, 472,
      `${L('印记', 'Marks')}: ${m1}→${m2}→${m3}→${m4}  |  ${commitment}·${rootedness}  |  ${topText}\n` +
      `${L('你倾向的结局', 'Your leaning ending')}: ${suggestLabel}\n${precond}`,
      {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '11px', color: '#6b5d42',
        backgroundColor: 'rgba(15,13,18,0.7)',
        padding: { x: 12, y: 6 },
        letterSpacing: 1,
        lineSpacing: 3
      }
    ).setOrigin(0.5).setDepth(55).setAlpha(0).setScrollFactor(0);

    this.tweens.add({ targets: summary, alpha: 0.85, duration: 2500, delay: 800, ease: 'Sine.easeOut' });
  }

  // ================================================================
  // 结局 1【同赴远方】go_north（tile坐标，玩家走动按E交互）
  // ================================================================
  private spawnGoNorthItems(): void {
    this.showChoiceSummary();

    // 第一排（y=3）：核心物品，间距5格
    this.addPoi(4, 3, L('记账本', 'Ledger'), {
      onInteract: () => this.showSpeech(this.getAccountBookDesc('north'))
    });
    this.addPoi(9, 3, L('通行材料', 'Travel Papers'), {
      onInteract: () => this.showSpeech(this.getPassMaterialDesc('north'))
    });
    this.addPoi(14, 3, L('旅行轿车', 'Station Wagon'), {
      onInteract: () => this.showSpeech(this.getCarDesc('north'))
    });
    this.addPoi(19, 3, this.getCopilotLabel(), {
      onInteract: () => this.showSpeech(this.getCopilotDesc())
    });
    // 第二排（y=5）：携带/后备/启程
    this.addPoi(6, 5, this.getCarryItemLabel(), {
      onInteract: () => this.showSpeech(this.getCarryItemDesc())
    });
    this.addPoi(12, 5, this.getTrunkItemLabel(), {
      onInteract: () => this.showSpeech(this.getTrunkItemDesc())
    });
    this.addPoi(18, 5, L('启程', 'Departure'), {
      onInteract: () => this.showSpeech(this.getNorthDepartDesc())
    });
  }

  // ================================================================
  // 结局 2【故土相守】return_home（tile坐标，玩家走动按E交互）
  // ================================================================
  private spawnReturnHomeItems(): void {
    this.showChoiceSummary();

    // 第一排（y=3）：核心物品，间距5格
    this.addPoi(4, 3, L('Maya 的画', "Maya's Painting"), {
      onInteract: () => this.showSpeech(this.getMayaPaintingDesc())
    });
    this.addPoi(9, 3, L('Noah 的手工', "Noah's Craft"), {
      onInteract: () => this.showSpeech(this.getNoahCraftDesc())
    });
    this.addPoi(14, 3, L('老街旧物', 'Old Street Keepsake'), {
      onInteract: () => this.showSpeech(this.getLeoOldItemDesc())
    });
    this.addPoi(19, 3, L('封存的材料', 'Sealed Papers'), {
      onInteract: () => this.showSpeech(this.getSealedMaterialDesc())
    });
    // 第二排（y=5）：窗外
    this.addPoi(12, 5, L('窗外', 'Window'), {
      onInteract: () => this.showSpeech(this.getHomeStayDesc())
    });
  }

  // ================================================================
  // 结局 3【独行新路】unknown_path（tile坐标，玩家走动按E交互）
  // ================================================================
  private spawnUnknownPathItems(): void {
    this.showChoiceSummary();

    // 第一排（y=3）：核心物品
    this.addPoi(6, 3, L('远行物资', 'Travel Supplies'), {
      onInteract: () => this.showSpeech(this.getTravelSuppliesDesc())
    });
    this.addPoi(12, 3, L('小幅画作', 'Small Painting'), {
      onInteract: () => this.showSpeech(this.getSmallPaintingDesc())
    });
    this.addPoi(18, 3, L('背包', 'Backpack'), {
      onInteract: () => this.showSpeech(this.getBackpackDesc())
    });
    // 第二排（y=5）：前方
    this.addPoi(12, 5, L('前方', 'The Road Ahead'), {
      onInteract: () => this.showSpeech(this.getUnknownPathDesc())
    });
  }

  // ================================================================
  // 结局 4【暂缓前行】pause_journey（tile坐标，玩家走动按E交互）
  // ================================================================
  private spawnPauseJourneyItems(): void {
    this.showChoiceSummary();

    // 第一排（y=3）：核心物品，间距5格
    this.addPoi(4, 3, L('半打包行李', 'Half-Packed Bag'), {
      onInteract: () => this.showSpeech(this.getHalfPackedDesc())
    });
    this.addPoi(9, 3, L('记账本', 'Ledger'), {
      onInteract: () => this.showSpeech(this.getAccountBookDesc('pause'))
    });
    this.addPoi(14, 3, L('散落的画稿', 'Scattered Sketches'), {
      onInteract: () => this.showSpeech(this.getScatteredDrawingsDesc())
    });
    this.addPoi(19, 3, L('手工摆件', 'Fallen Craft'), {
      onInteract: () => this.showSpeech(this.getFallenCraftDesc())
    });
    // 第二排（y=5）：窗边
    this.addPoi(12, 5, L('窗边', 'Windowside'), {
      onInteract: () => this.showSpeech(this.getPauseWindowDesc())
    });
  }

  // ================================================================
  // 辅助方法：读取全章印记/状态的快捷函数
  // ================================================================

  private getM1(): string | undefined { return GameState.inst.getStoryMark('ch1'); }
  private getM3(): string | undefined { return GameState.inst.getStoryMark('ch3'); }
  private getM4(): string | undefined { return GameState.inst.getStoryMark('ch4'); }
  private getTop(): 'maya' | 'noah' | 'leo' | null { return GameState.inst.topBond(); }
  private isTopBond(who: 'maya' | 'noah' | 'leo'): boolean { return this.getTop() === who; }
  private isHighC(): boolean { return GameState.inst.isHighCommitment(); }
  private isHighR(): boolean { return GameState.inst.isHighRootedness(); }

  // ================================================================
  // 印记联动描述生成器
  // 根据玩家在 1-4 章的具体印记，生成差异化的物品描述
  // ================================================================

  // 记账本描述（ch1 印记联动）
  private getAccountBookDesc(context: 'north' | 'pause'): string {
    const m1 = this.getM1();
    if (context === 'north') {
      if (m1 === 'A1') return L('从第一天起你就认真记账。每一笔打工攒下的钱，都是向北方的路费。', 'From the first day you kept careful accounts. Every bit of money saved from odd jobs was travel fare for the North.');
      if (m1 === 'C1') return L('记账本里夹着一张老街速写。你说这是给未来的纪念——但最终还是选择了北方。', 'A sketch of the Old Street is tucked in the ledger. You said it was a keepsake for the future — but in the end you still chose the North.');
      return L('记账本记得整整齐齐。偶尔夹着老街的速写，但目的地始终是北方。', 'The ledger is kept neat and tidy. Occasionally a sketch of the Old Street is tucked inside, but the destination was always the North.');
    }
    // pause
    if (m1 === 'A1') return L('记账本摊开在地，停在最后一笔。你本想攒够路费北上，如今却犹豫了。', 'The ledger lies open on the floor, stopped at the last entry. You had meant to save enough fare for the North, but now you hesitate.');
    if (m1 === 'C1') return L('记账本摊开在地，里面夹着老街速写。你曾想留下，又曾想出发——两样都没做到。', 'The ledger lies open on the floor, a sketch of the Old Street tucked inside. You once wanted to stay, and once wanted to leave — you did neither.');
    return L('记账本摊开在地。数字记了一半，方向也没想清。', 'The ledger lies open on the floor. Half the numbers are recorded, and the direction is still unclear.');
  }

  // 通行材料描述（ch3 印记联动）
  private getPassMaterialDesc(context: 'north'): string {
    const m3 = this.getM3();
    if (m3 === 'A3') return L('Elias 帮你加急办好的全套通行材料，手续齐全，没有遗漏。\n他看到你最终选择北上，嘴角终于有了笑意。', 'Elias helped expedite the full set of travel papers — everything in order, nothing missing.\nSeeing you finally choose to head north, a faint smile finally crossed his lips.');
    if (m3 === 'C3') return L('通行材料最终还是办齐了——虽然你曾一度想为了画展放弃。\nElias 没有说什么，只是把它们整整齐齐交给你。', 'The travel papers were eventually completed — though you once thought of giving them up for the art show.\nElias said nothing, only handed them to you neat and tidy.');
    return L('通行材料办得不多不少，刚好够用。你两边都兼顾了，手续也完成了。', 'The travel papers were neither too many nor too few — just enough. You managed both sides, and the paperwork is done.');
  }

  // 旅行轿车描述（ch4 印记 + isHighCommitment 联动）
  private getCarDesc(context: 'north' | 'unknown_path'): string {
    const m4 = this.getM4();
    const highC = this.isHighC();
    if (context === 'north') {
      if (m4 === 'A4') {
        return L('褪色的蓝色旅行轿车。车门内侧，五个名字的首字母依然清晰——那是很多年前刻下的。\nNoah 和 Leo 最终也同意了北上。车厢里备满了干粮和地图——你一直是对的。', 'The faded blue station wagon. Inside the door, the initials of five names are still clear — carved many years ago.\nNoah and Leo finally agreed to head north. The cabin is stocked with rations and maps — you were right all along.');
      }
      if (m4 === 'C4') {
        return L('褪色的蓝色旅行轿车。你最终还是选择了远方，尽管内心还在挣扎。\nNoah 和 Leo 没有来送行——他们选择了自己的路。', 'The faded blue station wagon. You ultimately chose the distance, though your heart still struggles.\nNoah and Leo did not come to see you off — they chose their own paths.');
      }
      // B4 或未设置
      return highC
        ? L('褪色的蓝色旅行轿车。你做了折中选择——两边都没有完全说服，但终究还是踏上了路。\n车厢里有准备好的物资，也有 Maya 塞给你的小幅画作。', 'The faded blue station wagon. You made a compromise — neither side fully persuaded, but you set out on the road at last.\nIn the cabin are prepared supplies, and the small painting Maya slipped you.')
        : L('褪色的蓝色旅行轿车。你决定北上，但准备并不充分——车票是凑的，地图是旧的。\n也许，这就是你想要的冒险。', 'The faded blue station wagon. You decided to head north, but you were not well-prepared — the ticket was patched together, the map was old.\nPerhaps this is the adventure you wanted.');
    }
    // unknown_path
    return L('那辆褪色的蓝色旅行轿车停在路边。你没有开它——你想走一条完全不同的路。', 'That faded blue station wagon sits by the road. You did not drive it — you wanted to walk a completely different path.');
  }

  // 副驾驶/空位描述（ch4 + topBond 联动）
  private getCopilotLabel(): string {
    const top = this.getTop();
    if (top) return `${BOND_NAMES[top]}${L('的位置', "'s Seat")}`;
    return L('空位', 'Empty Seat');
  }

  private getCopilotDesc(): string {
    const m4 = this.getM4();
    const top = this.getTop();
    if (m4 === 'A4') {
      return L('副驾驶坐满了。Elias 在前排，Noah 和 Leo 在后排——这是你们当年约定的模样。', 'The passenger seat is full. Elias is up front, Noah and Leo in the back — this is the shape of what you all agreed on years ago.');
    }
    if (top === 'maya') {
      return L('副驾驶放着 Maya 的画框。她没来，但她的画陪你上路。\n你和她的羁绊最深——即便选择了北上，她依然在你心里。', "Maya's picture frame rests in the passenger seat. She did not come, but her painting rides with you.\nYour bond with her is the deepest — even having chosen the North, she is still in your heart.");
    }
    if (top === 'noah') {
      return L('副驾驶放着 Noah 手工做的小摆件。他没来送行，但偷偷把它塞进了你的包里。', "A small craft Noah made by hand sits in the passenger seat. He did not come to see you off, but secretly slipped it into your bag.");
    }
    if (top === 'leo') {
      return L('副驾驶放着 Leo 常带的那本旧旅行日志。他说过他想走，现在你替他走了。', "The old travel journal Leo always carried sits in the passenger seat. He said he wanted to go — now you go in his place.");
    }
    return L('副驾驶空着。没有人来送行，也没有人选择同行——这是你自己的路。', 'The passenger seat is empty. No one came to see you off, no one chose to come along — this is your own road.');
  }

  // 携带物品描述（carriedItem 联动）
  private getCarryItemLabel(): string {
    const ci = GameState.inst.carriedItem;
    return ci ? CARRY_ITEM_LABEL[ci] : L('随身物品', 'Personal Item');
  }

  private getCarryItemDesc(): string {
    const ci = GameState.inst.carriedItem;
    if (!ci) return L('你没有特意挑选什么带走——空着手，也可以走很远。', 'You did not pick anything in particular to take — empty-handed, you can still walk a long way.');
    const labels: Record<string, string> = {
      group_photo: L('一张褪色的团体合照。五个人站在老街路口，笑容灿烂——那是北上计划最初的样子。\n你选择带上它，提醒自己这段旅程从何开始。', 'A faded group photo. Five people stand at the corner of the Old Street, smiles bright — that was the original shape of the northbound plan.\nYou chose to bring it, to remind yourself where this journey began.'),
      blank_notebook: L('一本空白的笔记本。你决定不写旧计划，要在路上重新书写自己的故事。', 'A blank notebook. You decided not to write down the old plan — you will rewrite your own story on the road.'),
      house_key: L('一把老街家门的钥匙。你没有封存它——也许有一天，你会回来。', 'A key to the door of home on the Old Street. You did not seal it away — perhaps one day you will come back.'),
      old_map: L('一张泛黄的旧地图，标注着北方的方向。这是你出发时唯一需要的东西。', 'A yellowed old map, marking the way to the North. This is the only thing you need when setting out.')
    };
    return labels[ci] ?? L('一件随身物品，陪伴你踏上北方之路。', 'A personal item, accompanying you on the road north.');
  }

  // 后备箱物品（trunkItem 联动）
  private getTrunkItemLabel(): string {
    const ti = GameState.inst.trunkItem;
    const labels: Record<string, string> = {
      tools: t('trunk_tools'),
      memory_box: t('trunk_memory_box'),
      maya_painting: t('trunk_maya_painting'),
      noah_recorder: t('trunk_noah_recorder'),
      leo_bag: t('trunk_leo_bag')
    };
    return ti ? (labels[ti] ?? L('后备箱物品', 'Trunk Item')) : L('后备箱', 'Trunk');
  }

  private getTrunkItemDesc(): string {
    const ti = GameState.inst.trunkItem;
    if (!ti) return L('后备箱里放着几件旅途必需品——你没有特意装什么纪念物。', 'A few travel essentials lie in the trunk — you did not pack any keepsakes in particular.');
    const labels: Record<string, string> = {
      tools: L('维修工具整齐码在后备箱。这是你当初选择优先办理通行材料时决定带上的——实用，而没有多余的东西。', 'Repair tools are neatly stacked in the trunk. You decided to bring them when you chose to prioritize the travel papers — practical, with nothing extra.'),
      memory_box: L('童年纪念盒安静地躺在后备箱。你选择了折中——既没有完全抛弃过去，也没有被它束缚。', 'The childhood memory box lies quietly in the trunk. You chose a compromise — neither fully discarding the past, nor bound by it.'),
      maya_painting: L('玛雅的画作靠在后备箱壁上。你当初为了支持她的画展而推迟手续——如今这幅画陪你一同北上。', "Maya's painting leans against the wall of the trunk. You once delayed the paperwork to support her show — now this painting rides north with you."),
      noah_recorder: L('诺亚的录音机放在后备箱。你曾在第二章选择支持他的自我方向——如今他的声音录在这盒磁带里。', "Noah's recorder sits in the trunk. You once chose to support his own direction in Chapter 2 — now his voice is recorded on this tape."),
      leo_bag: L('利奥的旅行包塞在后备箱角落。你曾在第一章选择理解他的不舍——如今他的东西陪你上路。', "Leo's travel bag is stuffed in the corner of the trunk. You once chose to understand his reluctance in Chapter 1 — now his things ride along with you.")
    };
    return labels[ti] ?? L('后备箱里的一件物品，陪伴你踏上旅途。', 'An item in the trunk, accompanying you on the journey.');
  }

  // 启程描述（ch4 印记 + isHighCommitment 联动）
  private getNorthDepartDesc(): string {
    const m1 = this.getM1();
    const m2 = GameState.inst.getStoryMark('ch2');
    const m3 = this.getM3();
    const m4 = this.getM4();
    const highC = this.isHighC();
    const fullA = m1 === 'A1' && m2 === 'A2' && m3 === 'A3' && m4 === 'A4';

    if (fullA && highC) {
      return L('引擎发动。北方的灯火越来越近。\n车厢里只有记账本和全套通行材料——没有 Maya 画展相关的任何物品。\n这就是你们说好的路——从第一天起，你就从未动摇。', 'The engine starts. The lights of the North draw ever closer.\nIn the cabin are only the ledger and the full set of travel papers — nothing related to Maya\'s art show.\nThis is the road you all agreed on — from the first day, you never wavered.');
    }
    if (m4 === 'A4' && highC) {
      return L('引擎发动。北方的灯火越来越近。\n这就是你们说好的路——从第一天起，你就从未动摇。', 'The engine starts. The lights of the North draw ever closer.\nThis is the road you all agreed on — from the first day, you never wavered.');
    }
    if (m4 === 'C4') {
      return L('引擎发动。北方的灯火越来越近。\n你曾想为 Maya 放弃这一切——但最终，你还是选择了远方。\n这个选择，你会花很长时间去消化。', 'The engine starts. The lights of the North draw ever closer.\nYou once wanted to give up all this for Maya — but in the end, you still chose the distance.\nIt will take you a long time to digest this choice.');
    }
    return highC
      ? L('引擎发动。北方的灯火越来越近。\n你做了折中选择——两边都顾及了，但方向始终是北方。', 'The engine starts. The lights of the North draw ever closer.\nYou made a compromise — you took care of both sides, but the direction was always north.')
      : L('引擎发动。北方的灯火越来越近。\n你选择了一个不算完美的出发——但也许，这就是最好的出发方式。', 'The engine starts. The lights of the North draw ever closer.\nYou chose a less-than-perfect departure — but perhaps this is the best way to set out.');
  }

  // Maya 的画描述（bond.maya + ch3 + ch4 印记联动）
  private getMayaPaintingDesc(): string {
    const m3 = this.getM3();
    const m4 = this.getM4();
    const bond = GameState.inst.bond.maya;
    if (m3 === 'C3' && m4 === 'C4') {
      return L('墙上挂着 Maya 赠送的手绘北方地图。\n为了陪她看画展，你曾推迟了出城手续——如今你们都留下了。\n「你看，留下来也挺好的。」她轻声说。', 'On the wall hangs a hand-drawn map of the North that Maya gave you.\nTo go to her show with her, you once delayed the departure paperwork — now you have both stayed.\n"See, staying is quite nice too," she says softly.');
    }
    if (m4 === 'A4') {
      return L('墙上挂着 Maya 的手绘。你曾想北上，她曾挽留——但最终你们都留在了这里。\n「至少你回来了。」她说。', "On the wall hangs Maya's hand-drawn work. You once wanted to head north, she once asked you to stay — but in the end you both remained here.\n\"At least you came back,\" she says.");
    }
    if (bond >= 10) return L('Maya 的手绘画挂在墙上最显眼的位置。她笑说你终于来看展了，而且再也不会走。', "Maya's hand-drawn painting hangs in the most prominent spot on the wall. She jokes that you finally came to her show, and will never leave again.");
    return L('墙上挂着 Maya 的手绘。她的首展你最终还是来了——以留下者的身份。', "On the wall hangs Maya's hand-drawn work. You finally came to her first show — as one who stays.");
  }

  // Noah 的手工描述（bond.noah + ch4 印记联动）
  private getNoahCraftDesc(): string {
    const m4 = this.getM4();
    const bond = GameState.inst.bond.noah;
    if (m4 === 'C4') {
      return L('Noah 的手工信物摆在窗台。他找到了真正想做的事——和你当初鼓励他的一样。\n「谢谢你让我想清楚。」他说。', "Noah's handmade token sits on the windowsill. He found what he truly wants to do — just as you once encouraged him.\n\"Thank you for helping me think it through,\" he says.");
    }
    if (bond >= 10) return L('Noah 的手工信物摆在窗台。他说找到了真正想做的事，谢谢你当初的理解。', "Noah's handmade token sits on the windowsill. He says he has found what he truly wants to do, and thanks you for your understanding back then.");
    return L('Noah 的手工信物摆在窗台。他找到了自己的方向，和你的选择一样——留下。', "Noah's handmade token sits on the windowsill. He has found his own direction, the same as your choice — to stay.");
  }

  // Leo 的老街旧物描述（ch1 + bond.leo + ch4 印记联动）
  private getLeoOldItemDesc(): string {
    const m1 = this.getM1();
    const m4 = this.getM4();
    const bond = GameState.inst.bond.leo;
    if (m1 === 'C1' && bond >= 10 && m4 === 'C4') {
      return L('你和 Leo 在屋顶聊过的老街旧物，安静地放在角落。\n从第一章起，他就知道你和他一样舍不得这里。\n现在，你们谁也没走。', 'The Old Street keepsake you and Leo talked about on the rooftop lies quietly in the corner.\nFrom the first chapter, he knew you were as reluctant to leave as he was.\nNow, neither of you has left.');
    }
    if (m1 === 'A1' && m4 === 'A4') {
      return L('你和 Leo 在老街聊过的旧物件，安静地放在角落。\n他曾以为你会走，你也曾以为自己会走——没想到最终，你们都留下了。', 'The old keepsake you and Leo talked about on the Old Street lies quietly in the corner.\nHe once thought you would leave, and you once thought so too — unexpectedly, in the end you both stayed.');
    }
    if (m1 === 'C1') {
      return L('你和 Leo 在老街聊过的旧物件，安静地放在角落。\n他一直舍不得这里——现在你也是。', 'The old keepsake you and Leo talked about on the Old Street lies quietly in the corner.\nHe has always been reluctant to leave here — and now so are you.');
    }
    return L('你和 Leo 在老街聊过的旧物件。他一直舍不得这里，现在你也是。', 'The old keepsake you and Leo talked about on the Old Street. He has always been reluctant to leave here — and now so are you.');
  }

  // 封存材料描述（ch4 印记 + trunkItem 联动）
  private getSealedMaterialDesc(): string {
    const m4 = this.getM4();
    const ti = GameState.inst.trunkItem;
    const trunkLabel = ti ? {
      tools: t('trunk_tools'),
      memory_box: t('trunk_memory_box'),
      maya_painting: t('trunk_maya_painting'),
      noah_recorder: t('trunk_noah_recorder'),
      leo_bag: t('trunk_leo_bag')
    }[ti] ?? L('某件物品', 'an item') : null;

    let base = '';
    if (m4 === 'C4') {
      base = L('出城通行材料被收进柜子封存——那是你为北上准备的，如今再也用不上了。', 'The departure travel papers have been sealed away in a cabinet — what you prepared for heading north, now never to be used.');
    } else if (m4 === 'A4') {
      base = L('出城通行材料被收进柜子封存。你曾那么坚定地要走——现在这一切都成了过去。', 'The departure travel papers have been sealed away in a cabinet. You were once so determined to leave — now all of this has become the past.');
    } else {
      base = L('出城通行材料被收进柜子封存。不再需要了。', 'The departure travel papers have been sealed away in a cabinet. No longer needed.');
    }

    if (trunkLabel) {
      base += L(
        `\n当初装在后备箱的${trunkLabel}，也一并放在了柜子里——不再需要带走了。`,
        `\nThe ${trunkLabel} once packed in the trunk has also been placed in the cabinet — no longer needed to take along.`
      );
    }
    base += L('\n柜门关上的那一刻，你反而觉得轻松。', '\nAs the cabinet door closes, you actually feel relieved.');
    return base;
  }

  // 故土留下描述（ch4 印记 + isHighRootedness 联动）
  private getHomeStayDesc(): string {
    const m4 = this.getM4();
    const highR = this.isHighR();
    if (m4 === 'C4' && highR) {
      return L('窗外是老街的灯火。每一盏灯、每一条巷子，都是你长大的痕迹。\n你曾想离开，最终留下——而这里，就是你的根。\n「欢迎回家。」你对自己说。', 'Outside the window are the lights of the Old Street. Every lamp, every alley, is the trace of your growing up.\nYou once wanted to leave, but in the end stayed — and this place is your root.\n"Welcome home," you say to yourself.');
    }
    if (m4 === 'A4') {
      return L('窗外是老街的灯火。你曾那么坚定地要走——现在却在这里。\n也许，这就是命运的安排。', 'Outside the window are the lights of the Old Street. You were once so determined to leave — yet here you are now.\nPerhaps this is the arrangement of fate.');
    }
    return highR
      ? L('窗外是老街的灯火。每一盏灯、每一条巷子，都是你长大的痕迹。\n这里就是你的根。', 'Outside the window are the lights of the Old Street. Every lamp, every alley, is the trace of your growing up.\nThis place is your root.')
      : L('窗外是老街的灯火。你留了下来，但心里还在想着远方。\n也许有一天，你会想清楚。', 'Outside the window are the lights of the Old Street. You stayed, but your heart is still on the distance.\nPerhaps one day, you will figure it out.');
  }

  // 远行物资描述（ch4 印记 + isHighCommitment 联动）
  private getTravelSuppliesDesc(): string {
    const m4 = this.getM4();
    const highC = this.isHighC();
    if (m4 === 'B4') {
      return L('少量远行物资，不多，刚好够一个人走一段。\n你既没有全部带上，也没有全部放下——就像你一直以来的选择。', 'A small amount of travel supplies, not much, just enough for one person to walk a stretch.\nYou neither took everything nor set everything down — just as you have always chosen.');
    }
    if (m4 === 'C4') {
      return L('少量远行物资，加上 Maya 塞给你的一幅小幅画作。\n你选择了自己的路——不依附任何人。', 'A small amount of travel supplies, plus a small painting Maya slipped you.\nYou chose your own road — dependent on no one.');
    }
    return highC
      ? L('远行物资准备充分。你本想北上，却最终选了一条不同的路。\n但你知道自己要什么。', 'Travel supplies are well-prepared. You had meant to head north, but in the end chose a different road.\nBut you know what you want.')
      : L('远行物资不多，只有随身的几件。你没有为任何选择做好充分准备——但这就是你。', 'Travel supplies are sparse, only a few items on hand. You did not fully prepare for any choice — but this is you.');
  }

  // 小幅画作描述（Maya 临别赠送，ch3 + ch4 印记联动）
  private getSmallPaintingDesc(): string {
    const m3 = this.getM3();
    const m4 = this.getM4();
    if (m3 === 'C3' && m4 === 'C4') {
      return L('一幅小幅画作，Maya 临别前塞给你的。\n你曾为了陪她看画展推迟了出城手续——如今这幅画陪你走上新路。\n「不管去哪里，带上它就好。」她说。', 'A small painting, slipped to you by Maya before parting.\nYou once delayed the departure paperwork to go to her show with her — now this painting accompanies you on the new road.\n"No matter where you go, just bring it along," she says.');
    }
    if (m3 === 'A3') {
      return L('一幅小幅画作，Maya 临别前塞给你的。\n你曾放弃了她的画展优先北上——但她还是把这幅画留给了你。', 'A small painting, slipped to you by Maya before parting.\nYou once gave up her show to prioritize heading north — but she still left this painting with you.');
    }
    return L('一幅小幅画作，Maya 临别前塞给你的。\n她说不管你去哪里，带上它就好。', 'A small painting, slipped to you by Maya before parting.\nShe says no matter where you go, just bring it along.');
  }

  // 背包描述（topBond + ch4 印记联动）
  private getBackpackDesc(): string {
    const m4 = this.getM4();
    const top = this.getTop();
    if (top === 'maya') {
      return L('背包不重。里面装着记账本的一页、老街的一颗石子，和 Maya 送你的那幅画。\n她是你最在意的人——但你选择了独自前行。', "The backpack is not heavy. Inside are a page from the ledger, a pebble from the Old Street, and the painting Maya gave you.\nShe is the one you care about most — but you chose to walk on alone.");
    }
    if (top === 'noah') {
      return L('背包不重。里面装着记账本的一页、老街的一颗石子，和 Noah 手工做的小摆件。\n他是你最在意的人——但你选择了独自前行。', "The backpack is not heavy. Inside are a page from the ledger, a pebble from the Old Street, and the small craft Noah made by hand.\nHe is the one you care about most — but you chose to walk on alone.");
    }
    if (top === 'leo') {
      return L('背包不重。里面装着记账本的一页、Leo 送你的旧旅行日志，和一张没有写完的清单。\n他是你最在意的人——但你选择了独自前行。', "The backpack is not heavy. Inside are a page from the ledger, the old travel journal Leo gave you, and an unfinished list.\nHe is the one you care about most — but you chose to walk on alone.");
    }
    if (m4 === 'B4') {
      return L('背包不重。里面装着记账本的一页、老街的一颗石子，和一张没有写完的清单。\n就像你一直以来的选择——平衡，但不彻底。', "The backpack is not heavy. Inside are a page from the ledger, a pebble from the Old Street, and an unfinished list.\nJust like your choices all along — balanced, but not thorough.");
    }
    return L('背包不重。里面装着记账本的一页、老街的一颗石子，和一张没有写完的清单。', 'The backpack is not heavy. Inside are a page from the ledger, a pebble from the Old Street, and an unfinished list.');
  }

  // 独行新路描述（ch4 + isHighCommitment/isHighRootedness 联动）
  private getUnknownPathDesc(): string {
    const m4 = this.getM4();
    const highC = this.isHighC();
    const highR = this.isHighR();
    if (m4 === 'B4') {
      return L('你走向一条没有名字的路。\n既非北上，也非留守——方向是你自己的。\n这是你最诚实的选择：不被任何计划束缚。', 'You walk toward a road without a name.\nNeither heading north nor staying behind — the direction is your own.\nThis is your most honest choice: bound by no plan.');
    }
    if (highC && !highR) {
      return L('你走向一条没有名字的路。\n你有足够的勇气去探索未知——但心里还留着对北方的承诺。', 'You walk toward a road without a name.\nYou have enough courage to explore the unknown — but your heart still holds a promise to the North.');
    }
    if (!highC && highR) {
      return L('你走向一条没有名字的路。\n你舍不得老街，但也不想留下——所以你选择了第三条路。', 'You walk toward a road without a name.\nYou cannot bear to leave the Old Street, but you do not want to stay either — so you chose a third road.');
    }
    return L('你走向一条没有名字的路。\n既非北上，也非留守——方向是你自己的。', 'You walk toward a road without a name.\nNeither heading north nor staying behind — the direction is your own.');
  }

  // 半打包行李描述（ch4 印记 + isHighCommitment 联动）
  private getHalfPackedDesc(): string {
    const m4 = this.getM4();
    const highC = this.isHighC();
    if (m4 === 'A4' && highC) {
      return L('行李半打包，拉链没拉上。\n你曾那么坚定地要走——现在却坐在窗边，不知道该带什么、该放下什么。', 'The luggage is half-packed, the zipper left open.\nYou were once so determined to leave — yet now you sit by the window, unsure what to take, what to set down.');
    }
    if (m4 === 'C4') {
      return L('行李半打包，拉链没拉上。\n你曾想为 Maya 放弃一切——现在两边都没做好。', 'The luggage is half-packed, the zipper left open.\nYou once wanted to give up everything for Maya — now neither side is done.');
    }
    return L('行李半打包，拉链没拉上。\n你装了又拆，拆了又装——已经反复好几次了。', 'The luggage is half-packed, the zipper left open.\nYou packed and unpacked, unpacked and packed — already several times over.');
  }

  // 散落画稿描述（Maya 相关 + topBond 联动）
  private getScatteredDrawingsDesc(): string {
    const top = this.getTop();
    const bond = GameState.inst.bond.maya;
    if (top === 'maya' || bond >= 10) {
      return L('Maya 的画稿散落一旁，角上沾了灰。\n你最在意的人是她——但你既没走成，也没留住她。\n「对不起。」你低声说。', "Maya's sketches lie scattered nearby, dust on the corners.\nShe is the one you care about most — but you neither left nor kept her.\n\"I'm sorry,\" you say softly.");
    }
    return L('Maya 的画稿散落一旁，角上沾了灰。\n你曾答应去看她的首展，又曾答应办好通行手续——两件事都没有做完。', "Maya's sketches lie scattered nearby, dust on the corners.\nYou once promised to go to her first show, and once promised to finish the travel paperwork — neither got done.");
  }

  // 手工摆件描述（Noah 相关 + topBond 联动）
  private getFallenCraftDesc(): string {
    const top = this.getTop();
    const bond = GameState.inst.bond.noah;
    if (top === 'noah' || bond >= 10) {
      return L('Noah 的手工摆件倒在箱边。\n你最在意的人是他——但你既没走成，也没留住他。\n你拿起又放下，始终没决定它该放进远行的背包还是留下的柜子。', "Noah's handmade craft has fallen over by the box.\nHe is the one you care about most — but you neither left nor kept him.\nYou pick it up and set it down, never deciding whether to put it in the travel backpack or the cabinet to stay.");
    }
    return L('Noah 的手工摆件倒在箱边。\n你拿起又放下，始终没决定它该放进远行的背包还是留下的柜子。', "Noah's handmade craft has fallen over by the box.\nYou pick it up and set it down, never deciding whether to put it in the travel backpack or the cabinet to stay.");
  }

  // 暂缓窗边描述（ch4 + isHighCommitment/isHighRootedness 联动）
  private getPauseWindowDesc(): string {
    const m4 = this.getM4();
    const highC = this.isHighC();
    const highR = this.isHighR();
    if (m4 === 'B4') {
      return L('你坐在窗边，没有出发，也没有留下。\n你的选择从来都是折中——两边都不想放弃，两边都没抓住。\n风从半开的窗子吹进来。也许明天，你会想清楚。', 'You sit by the window, neither setting out nor staying behind.\nYour choices have always been a compromise — unwilling to give up either side, you held onto neither.\nThe wind blows in through the half-open window. Perhaps tomorrow, you will figure it out.');
    }
    if (highC && highR) {
      return L('你坐在窗边。你既想走，又想留——两边的力量在拉扯你。\n风从半开的窗子吹进来。也许明天，你会想清楚。', 'You sit by the window. You want both to leave and to stay — the forces on both sides pull at you.\nThe wind blows in through the half-open window. Perhaps tomorrow, you will figure it out.');
    }
    if (highC) {
      return L('你坐在窗边。你本想北上，但迟迟没有动身——好像有什么东西把你留在这里。\n风从半开的窗子吹进来。也许明天，你会想清楚。', 'You sit by the window. You had meant to head north, but kept delaying — as if something kept you here.\nThe wind blows in through the half-open window. Perhaps tomorrow, you will figure it out.');
    }
    if (highR) {
      return L('你坐在窗边。你想留下，但心里还在想着远方——好像少了点什么。\n风从半开的窗子吹进来。也许明天，你会想清楚。', 'You sit by the window. You want to stay, but your heart still drifts to the distance — as if something is missing.\nThe wind blows in through the half-open window. Perhaps tomorrow, you will figure it out.');
    }
    return L('你坐在窗边，没有出发，也没有留下。\n风从半开的窗子吹进来。也许明天，你会想清楚。', 'You sit by the window, neither setting out nor staying behind.\nThe wind blows in through the half-open window. Perhaps tomorrow, you will figure it out.');
  }

  // —— Inmost 风格结局房间装饰（仅兜底使用）——
  private spawnEpilogueDecorations(ending: EndingType | null): void {
    if (ending && CUSTOM_ENDINGS.includes(ending)) return;
    const T = 48;
    this.sceneArt.placeWindow(2 * T, 2 * T + T / 2);
    this.sceneArt.placeWindow(22 * T, 2 * T + T / 2);
    this.sceneArt.placeBookshelf(1 * T + T / 2, 3 * T + T / 2);
    this.sceneArt.placeBookshelf(23 * T + T / 2, 3 * T + T / 2);
    this.sceneArt.placePipe(6 * T, T / 2);
    this.sceneArt.placePipe(16 * T, T / 2);
  }

  // 生成坐姿人物剪影纹理（与 TitleScene 同逻辑，确保结局场景独立可用）
  private makeEpilogueSitTexture(key: string, w: number, h: number): void {
    if (this.textures.exists(key)) return;
    const tex = this.textures.createCanvas(key, w, h);
    if (!tex) return;
    const ctx = tex.getContext();
    ctx.fillStyle = '#000';
    const cx = Math.floor(w / 2);
    const headR = Math.max(2, Math.floor(w * 0.2));
    const headCy = Math.floor(h * 0.22);
    ctx.beginPath();
    ctx.arc(cx, headCy, headR, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillRect(cx - 1, headCy + headR - 1, 2, 2);
    const shoulderY = headCy + headR + 1;
    const shoulderW = Math.floor(w * 0.5);
    const waistY = Math.floor(h * 0.65);
    ctx.beginPath();
    ctx.moveTo(cx - Math.floor(shoulderW / 2), shoulderY);
    ctx.lineTo(cx + Math.floor(shoulderW / 2), shoulderY);
    ctx.lineTo(cx + Math.floor(shoulderW / 2) + 1, waistY);
    ctx.lineTo(cx - Math.floor(shoulderW / 2) - 1, waistY);
    ctx.closePath();
    ctx.fill();
    const legY = waistY;
    const legW = Math.floor(w * 0.8);
    const legH = h - legY - 1;
    ctx.fillRect(cx - Math.floor(legW / 2), legY, legW, legH);
    ctx.fillRect(cx - Math.floor(legW / 2) - 1, legY + legH - 2, 2, 2);
    ctx.fillRect(cx + Math.floor(legW / 2) - 1, legY + legH - 2, 2, 2);
    tex.refresh();
  }

  // 确保汽车场景的 sit_a/sit_b 纹理存在（若从存档直接进入结局，跳过了标题场景）
  private ensureSitTextures(): void {
    this.makeEpilogueSitTexture('sit_a', 16, 14);
    this.makeEpilogueSitTexture('sit_b', 14, 13);
    this.makeEpilogueSitTexture('sit_c', 18, 15);
  }
}
