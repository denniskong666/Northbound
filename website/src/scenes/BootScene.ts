import Phaser from 'phaser';

// Boot 场景：极简启动，直接进入 Preload 生成占位美术
export class BootScene extends Phaser.Scene {
  constructor() {
    super('BootScene');
  }

  create(): void {
    this.scene.start('PreloadScene');
  }
}
