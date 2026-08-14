import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT } from './config/GameConfig';
import { BootScene } from './scenes/BootScene';
import { PreloadScene } from './scenes/PreloadScene';
import { TitleScene } from './scenes/TitleScene';
import { OldDistrictScene } from './scenes/OldDistrictScene';
import { GarageScene } from './scenes/GarageScene';
import { RooftopScene } from './scenes/RooftopScene';
import { GameState } from './state/GameState';

// 游戏入口：注册所有场景并启动
const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  parent: 'game',
  width: GAME_WIDTH,
  height: GAME_HEIGHT,
  pixelArt: true,
  backgroundColor: '#0a0a0f',
  physics: {
    default: 'arcade',
    arcade: {
      debug: false,
      gravity: { x: 0, y: 0 }
    }
  },
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH
  },
  scene: [BootScene, PreloadScene, TitleScene, OldDistrictScene, GarageScene, RooftopScene]
};

const game = new Phaser.Game(config);

// 暴露到 window 便于运行时调试与自动化验证（发布时可移除）
(window as any).__GAME = game;
(window as any).__GS = GameState.inst;
