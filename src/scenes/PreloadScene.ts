import Phaser from 'phaser';
import { PlaceholderArt } from '../systems/PlaceholderArt';

// Preload 场景：生成全部占位美术，短暂过渡后进入标题界面
export class PreloadScene extends Phaser.Scene {
  constructor() {
    super('PreloadScene');
  }

  create(): void {
    const { width, height } = this.scale;

    // 极简加载提示（美术瞬间生成，仅作过渡）
    this.add.text(width / 2, height / 2, 'loading…', {
      fontFamily: 'serif',
      fontSize: '14px',
      color: '#4a4438'
    }).setOrigin(0.5);

    // 生成占位美术（同步，瞬间完成）
    new PlaceholderArt(this).generateAll();

    this.cameras.main.fadeIn(200, 0, 0, 0);
    this.time.delayedCall(180, () => {
      this.cameras.main.fadeOut(300, 0, 0, 0, () => {
        this.scene.start('TitleScene');
      });
    });
  }
}
