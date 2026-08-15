// 标题界面：标题、引言、动态星空、地平线上风中人物剪影、语言切换、新游戏/继续游戏
// 入场动画层层淡入；点击按钮进入游戏

import Phaser from 'phaser';
import { GameState } from '../state/GameState';
import { chapterMeta } from '../state/Chapter';
import { t, getLang, toggleLang } from '../systems/I18n';

export class TitleScene extends Phaser.Scene {
  private started = false;
  private langBtn?: Phaser.GameObjects.Text;
  private newGameBtn?: Phaser.GameObjects.Text;
  private continueBtn?: Phaser.GameObjects.Text;
  private quoteText?: Phaser.GameObjects.Text;
  private controlsText?: Phaser.GameObjects.Text;
  private langLabelText?: Phaser.GameObjects.Text;

  constructor() {
    super('TitleScene');
  }

  create(): void {
    // Phaser 的 scene.start() 重用同一场景实例，类字段不会自动重置
    // 必须在此重置 started，否则游戏结束后再点"新游戏"会被忽略
    this.started = false;

    const W = this.scale.width;
    const H = this.scale.height;
    const horizonY = Math.floor(H * 0.68);

    // 背景渐变
    const bg = this.add.graphics();
    bg.fillGradientStyle(0x08080d, 0x08080d, 0x1a1622, 0x14111a, 1);
    bg.fillRect(0, 0, W, H);

    // —— 动态星空：每颗星独立闪烁（渐变感）——
    const starColors = [0xf5c97a, 0xe8e4d8, 0x8ad8ff, 0xc9b890];
    for (let i = 0; i < 50; i++) {
      const x = Phaser.Math.Between(0, W);
      const y = Phaser.Math.Between(0, horizonY);
      const r = Phaser.Math.FloatBetween(0.5, 1.6);
      const color = starColors[Phaser.Math.Between(0, starColors.length - 1)];

      const star = this.add.circle(x, y, r, color, Phaser.Math.FloatBetween(0.5, 1))
        .setDepth(1);

      const duration = Phaser.Math.Between(1200, 2600);
      const delay = Phaser.Math.Between(0, 1500);
      this.tweens.add({
        targets: star,
        alpha: { from: star.alpha, to: Phaser.Math.FloatBetween(0.05, 0.2) },
        duration: duration,
        yoyo: true,
        repeat: -1,
        ease: 'Sine.easeInOut',
        delay: delay
      });
    }

    // 几颗较大的亮星（带光晕）
    for (let i = 0; i < 6; i++) {
      const x = Phaser.Math.Between(W * 0.1, W * 0.9);
      const y = Phaser.Math.Between(20, horizonY - 20);
      const glowR = Phaser.Math.Between(4, 7);

      const glow = this.add.circle(x, y, glowR, 0xf5c97a, 0.2).setDepth(1);
      this.tweens.add({
        targets: glow,
        alpha: { from: 0.03, to: 0.4 },
        duration: Phaser.Math.Between(1500, 2800),
        yoyo: true, repeat: -1, ease: 'Sine.easeInOut',
        delay: Phaser.Math.Between(0, 1000)
      });

      const core = this.add.circle(x, y, 1.5, 0xffe9b3, 1).setDepth(2);
      this.tweens.add({
        targets: core,
        alpha: { from: 0.3, to: 1 },
        duration: Phaser.Math.Between(1200, 2200),
        yoyo: true, repeat: -1, ease: 'Sine.easeInOut',
        delay: Phaser.Math.Between(0, 800)
      });
    }

    // 地平线雾霭
    const haze = this.add.graphics();
    haze.fillStyle(0x2a3550, 0.18);
    haze.fillRect(0, horizonY, W, 2);
    haze.fillStyle(0x2a3550, 0.08);
    haze.fillRect(0, horizonY - 14, W, 14);

    // —— 地平线上风中人物剪影 ——
    this.spawnSilhouettes(W, horizonY);

    // 标题
    const title = this.add.text(W / 2, H / 2 - 80, t('title'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '72px', color: '#e8e4d8', fontStyle: 'bold'
    }).setOrigin(0.5).setAlpha(0).setDepth(10);

    // 副标题
    const sub = this.add.text(W / 2, H / 2 - 14, t('subtitle'), {
      fontFamily: 'serif', fontSize: '14px', color: '#8a8275', letterSpacing: 8
    }).setOrigin(0.5).setAlpha(0).setDepth(10);

    // 引言
    this.quoteText = this.add.text(W / 2, H / 2 + 56, t('quote'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '14px', color: '#6b6557', align: 'center', lineSpacing: 4
    }).setOrigin(0.5).setAlpha(0).setDepth(10);

    // 操作提示
    this.controlsText = this.add.text(W / 2, H - 38, t('controls'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '11px', color: '#4a4438'
    }).setOrigin(0.5).setAlpha(0).setDepth(10);

    // —— 语言切换按钮（右上角）——
    this.langLabelText = this.add.text(W - 110, 24, t('langLabel'), {
      fontFamily: 'sans-serif', fontSize: '11px', color: '#4a4438'
    }).setOrigin(0.5).setAlpha(0).setDepth(10);

    this.langBtn = this.add.text(W - 60, 24, getLang() === 'zh' ? '中文' : 'EN', {
      fontFamily: 'sans-serif', fontSize: '14px', color: '#f5c97a'
    }).setOrigin(0.5).setAlpha(0).setDepth(10).setInteractive({ useHandCursor: true });

    this.langBtn.on('pointerdown', () => {
      toggleLang();
      this.refreshTexts();
    });

    // —— 新游戏 / 继续游戏 按钮 ——
    const gs = GameState.inst;
    const hasSave = gs.resolvedChoices.size > 0 || gs.chapter !== 'ch1' || gs.flags.size > 0;

    this.newGameBtn = this.add.text(W / 2, H - 120, t('newGame'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '18px', color: '#f5c97a'
    }).setOrigin(0.5).setAlpha(0).setDepth(10).setInteractive({ useHandCursor: true });

    this.newGameBtn.on('pointerover', () => this.newGameBtn!.setColor('#ffe9b3'));
    this.newGameBtn.on('pointerout',  () => this.newGameBtn!.setColor('#f5c97a'));
    this.newGameBtn.on('pointerdown', () => this.begin(true));

    if (hasSave) {
      this.continueBtn = this.add.text(W / 2, H - 82,
        `${t('continueGame')} · ${chapterMeta(gs.chapter).title}`, {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '14px', color: '#8a8275'
      }).setOrigin(0.5).setAlpha(0).setDepth(10).setInteractive({ useHandCursor: true });

      this.continueBtn.on('pointerover', () => this.continueBtn!.setColor('#e8e4d8'));
      this.continueBtn.on('pointerout',  () => this.continueBtn!.setColor('#8a8275'));
      this.continueBtn.on('pointerdown', () => this.begin(false));
    }

    // 入场动画
    this.tweens.add({ targets: title, alpha: 1, y: H / 2 - 88, duration: 800, ease: 'Quad.easeOut' });
    this.tweens.add({ targets: sub, alpha: 1, duration: 800, delay: 220, ease: 'Quad.easeOut' });
    this.tweens.add({ targets: this.quoteText, alpha: 1, duration: 900, delay: 520, ease: 'Quad.easeOut' });
    this.tweens.add({ targets: this.controlsText, alpha: 1, duration: 600, delay: 760 });
    this.tweens.add({ targets: this.langLabelText, alpha: 1, duration: 400, delay: 900 });
    this.tweens.add({ targets: this.langBtn, alpha: 1, duration: 400, delay: 1000 });
    this.tweens.add({
      targets: this.newGameBtn, alpha: 1, duration: 500, delay: 1100
    });
    if (this.continueBtn) {
      this.tweens.add({ targets: this.continueBtn, alpha: 1, duration: 500, delay: 1250 });
    }

    this.cameras.main.fadeIn(500, 0, 0, 0);
  }

  // 生成地平线上风中人物剪影（坐姿，靠在一起）
  private spawnSilhouettes(W: number, groundY: number): void {
    this.makeSitTexture('sit_a', 16, 14);
    this.makeSitTexture('sit_b', 14, 13);
    this.makeSitTexture('sit_c', 18, 15);

    // 5 人紧挨着坐在一起，间距很小
    const centerX = W * 0.5;
    const gap = 11;
    const silhouettes = [
      { key: 'sit_b', x: centerX - gap * 2, scale: 1.7, swayDelay: 0 },
      { key: 'sit_a', x: centerX - gap,     scale: 1.8, swayDelay: 400 },
      { key: 'sit_c', x: centerX,           scale: 2.0, swayDelay: 200 },
      { key: 'sit_a', x: centerX + gap,     scale: 1.7, swayDelay: 600 },
      { key: 'sit_b', x: centerX + gap * 2, scale: 1.8, swayDelay: 300 }
    ];

    for (const s of silhouettes) {
      const img = this.add.image(s.x, groundY + 2, s.key)
        .setOrigin(0.5, 1)
        .setScale(s.scale)
        .setAlpha(0.9)
        .setDepth(3)
        .setTint(0x0a0814);

      // 坐姿微晃（风吹，幅度小，不像站着那么夸张）
      this.tweens.add({
        targets: img,
        angle: { from: -1, to: 1 },
        duration: Phaser.Math.Between(3000, 4500),
        yoyo: true,
        repeat: -1,
        ease: 'Sine.easeInOut',
        delay: s.swayDelay
      });
    }
  }

  // 生成坐姿人物剪影纹理（背影，膝盖弯曲前伸）
  private makeSitTexture(key: string, w: number, h: number): void {
    if (this.textures.exists(key)) return;
    const tex = this.textures.createCanvas(key, w, h);
    if (!tex) return;
    const ctx = tex.getContext();
    ctx.fillStyle = '#000';

    const cx = Math.floor(w / 2);

    // 头部（圆）
    const headR = Math.max(2, Math.floor(w * 0.2));
    const headCy = Math.floor(h * 0.22);
    ctx.beginPath();
    ctx.arc(cx, headCy, headR, 0, Math.PI * 2);
    ctx.fill();

    // 脖子
    ctx.fillRect(cx - 1, headCy + headR - 1, 2, 2);

    // 身体（躯干，坐着较短，梯形）
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

    // 腿（向前伸出，膝盖弯曲）—— 底部宽矩形
    const legY = waistY;
    const legW = Math.floor(w * 0.8);
    const legH = h - legY - 1;
    ctx.fillRect(cx - Math.floor(legW / 2), legY, legW, legH);

    // 脚部微翘（两端各加一个小方块）
    ctx.fillRect(cx - Math.floor(legW / 2) - 1, legY + legH - 2, 2, 2);
    ctx.fillRect(cx + Math.floor(legW / 2) - 1, legY + legH - 2, 2, 2);

    tex.refresh();
  }

  // 切换语言后刷新所有文本
  private refreshTexts(): void {
    if (this.quoteText) this.quoteText.setText(t('quote'));
    if (this.controlsText) this.controlsText.setText(t('controls'));
    if (this.langLabelText) this.langLabelText.setText(t('langLabel'));
    if (this.langBtn) this.langBtn.setText(getLang() === 'zh' ? '中文' : 'EN');
    if (this.newGameBtn) this.newGameBtn.setText(t('newGame'));
    if (this.continueBtn) {
      const gs = GameState.inst;
      this.continueBtn.setText(`${t('continueGame')} · ${chapterMeta(gs.chapter).title}`);
    }
  }

  private begin(isNew: boolean): void {
    if (this.started) return;
    this.started = true;
    if (isNew) {
      GameState.inst.reset();
    }
    this.cameras.main.fadeOut(500, 0, 0, 0, () => {
      this.scene.start('OldDistrictScene');
    });
  }
}
