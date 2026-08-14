// 标题界面：标题、引言、远方灯火装饰、语言切换、新游戏/继续游戏
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
    const W = this.scale.width;
    const H = this.scale.height;

    // 背景渐变
    const bg = this.add.graphics();
    bg.fillGradientStyle(0x08080d, 0x08080d, 0x1a1622, 0x14111a, 1);
    bg.fillRect(0, 0, W, H);

    // 远方灯火
    const lights = this.add.graphics();
    for (let i = 0; i < 26; i++) {
      const x = Phaser.Math.Between(0, W);
      const y = Phaser.Math.Between(0, Math.floor(H * 0.42));
      const r = Phaser.Math.FloatBetween(0.6, 1.8);
      const a = Phaser.Math.FloatBetween(0.25, 0.85);
      lights.fillStyle(0xf5c97a, a);
      lights.fillCircle(x, y, r);
    }
    lights.setAlpha(0.7);
    this.tweens.add({
      targets: lights, alpha: { from: 0.7, to: 1 },
      duration: 2600, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
    });

    // 地平线雾霭
    const haze = this.add.graphics();
    haze.fillStyle(0x2a3550, 0.18);
    haze.fillRect(0, Math.floor(H * 0.42), W, 2);
    haze.fillStyle(0x2a3550, 0.08);
    haze.fillRect(0, Math.floor(H * 0.42) - 14, W, 14);

    // 标题
    const title = this.add.text(W / 2, H / 2 - 80, t('title'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '72px', color: '#e8e4d8', fontStyle: 'bold'
    }).setOrigin(0.5).setAlpha(0);

    // 副标题
    const sub = this.add.text(W / 2, H / 2 - 14, t('subtitle'), {
      fontFamily: 'serif', fontSize: '14px', color: '#8a8275', letterSpacing: 8
    }).setOrigin(0.5).setAlpha(0);

    // 引言
    this.quoteText = this.add.text(W / 2, H / 2 + 56, t('quote'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",serif',
      fontSize: '14px', color: '#6b6557', align: 'center', lineSpacing: 4
    }).setOrigin(0.5).setAlpha(0);

    // 操作提示
    this.controlsText = this.add.text(W / 2, H - 38, t('controls'), {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '11px', color: '#4a4438'
    }).setOrigin(0.5).setAlpha(0);

    // —— 语言切换按钮（右上角）——
    this.langLabelText = this.add.text(W - 110, 24, t('langLabel'), {
      fontFamily: 'sans-serif', fontSize: '11px', color: '#4a4438'
    }).setOrigin(0.5).setAlpha(0);

    this.langBtn = this.add.text(W - 60, 24, getLang() === 'zh' ? '中文' : 'EN', {
      fontFamily: 'sans-serif', fontSize: '14px', color: '#f5c97a'
    }).setOrigin(0.5).setAlpha(0).setInteractive({ useHandCursor: true });

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
    }).setOrigin(0.5).setAlpha(0).setInteractive({ useHandCursor: true });

    this.newGameBtn.on('pointerover', () => this.newGameBtn!.setColor('#ffe9b3'));
    this.newGameBtn.on('pointerout',  () => this.newGameBtn!.setColor('#f5c97a'));
    this.newGameBtn.on('pointerdown', () => this.begin(true));

    if (hasSave) {
      this.continueBtn = this.add.text(W / 2, H - 82,
        `${t('continueGame')} · ${chapterMeta(gs.chapter).title}`, {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '14px', color: '#8a8275'
      }).setOrigin(0.5).setAlpha(0).setInteractive({ useHandCursor: true });

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
