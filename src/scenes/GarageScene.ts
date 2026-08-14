// 瓦尔汽车修理厂：伊莱亚斯的工作场所，好友大本营
// 含褪色蓝色旅行轿车、玛雅儿时画作、老旧屋顶地图、Elias
// 任务2：失踪的套筒扳手——翻找3个点位，随机1处藏有扳手
// 地图编码：0=走廊地面 1=墙体 3=修理厂地面
import { BaseScene, Poi } from './BaseScene';
import { NpcPlacement } from '../data/NpcDefs';
import { TaskSystem } from '../systems/TaskSystem';
import { GameState } from '../state/GameState';

const MAP: string[] = [
  '111111111111111',
  '100000000000001',
  '103333333333301',
  '103333333333301',
  '103333333333301',
  '103333333333301',
  '103333333333301',
  '100000000000001',
  '111111111111111'
];

// 修理厂内的伊莱亚斯位置（第一章）
const GARAGE_NPCS: NpcPlacement[] = [
  { id: 'elias', tileX: 9, tileY: 4, facing: 'left', label: '和伊莱亚斯说话' }
];

// 修理厂内的诺亚位置（第二章，任务6录音机）
const GARAGE_CH2_NPCS: NpcPlacement[] = [
  { id: 'noah', tileX: 9, tileY: 4, facing: 'left', label: '和诺亚说话' }
];

// 任务2：翻找点位（随机1处藏有套筒扳手）
const SEARCH_SPOTS: { tx: number; ty: number; label: string; empty: string }[] = [
  { tx: 4,  ty: 3, label: '翻找杂物堆', empty: '一堆旧零件，没有套筒扳手。' },
  { tx: 11, ty: 5, label: '翻找工具柜', empty: '工具柜里只有生锈的螺丝。' },
  { tx: 7,  ty: 5, label: '翻找旧纸箱', empty: '纸箱里是空的。' }
];

export class GarageScene extends BaseScene {
  private searchPois: Poi[] = [];

  constructor() {
    super('GarageScene');
  }

  protected sceneKey(): string { return 'GarageScene'; }
  protected getMap(): string[] { return MAP; }
  protected getSpawnTile(): { x: number; y: number } { return { x: 7, y: 7 }; }

  protected spawnContent(): void {
    const ch = GameState.inst.chapter;

    // NPC：第一章 Elias，第二章 Noah（Elias 下线）
    if (ch === 'ch2') {
      this.spawnNpcs(GARAGE_CH2_NPCS);
    } else {
      this.spawnNpcs(GARAGE_NPCS);
    }

    // 褪色的蓝色旅行轿车
    this.addPoi(7, 3, '检查旅行轿车', {
      line: '褪色的蓝色旅行轿车。车门内侧，刻着五个名字的首字母。'
    });
    // 墙上玛雅儿时的画
    this.addPoi(2, 2, '玛雅儿时的画', {
      line: '画里五个好友一同驱车向北。伊莱亚斯把它当作约定的凭证。'
    });
    // 老旧的屋顶地图
    this.addPoi(12, 2, '老旧的屋顶地图', {
      line: '一张旧地图，背面签着五个人的名字。'
    });

    // 任务2：失踪的套筒扳手（第一章）
    if (TaskSystem.inst.isUnlocked('ch1_wrench') && !TaskSystem.inst.isDone('ch1_wrench')) {
      this.spawnSearchPois();
    }

    // 任务：第二章修理厂物资收集点（收集工具，计入 ch2_supplies 的第3处）
    if (TaskSystem.inst.isUnlocked('ch2_supplies') && !TaskSystem.inst.isDone('ch2_supplies') && !GameState.inst.hasFlag('ch2_supply_garage')) {
      this.addPoi(11, 4, '工具架', {
        onInteract: () => {
          this.showSpeech('收集到一份远行物资（修理厂工具）。');
          GameState.inst.applyEffects({ flag: 'ch2_supply_garage' });
          // 检查是否全部收集完成（跨场景，由 OldDistrictScene 的逻辑判断）
          const flags = ['ch2_supply_grocery', 'ch2_supply_market', 'ch2_supply_garage'];
          if (flags.every(f => GameState.inst.hasFlag(f)) && !TaskSystem.inst.isDone('ch2_supplies')) {
            this.showSpeech('远行物资齐了。该上屋顶看看大家了。');
            TaskSystem.inst.complete('ch2_supplies');
          }
        }
      });
    }

    // 门：回老街区
    this.addDoor(7, 7, '回老街区', 'OldDistrictScene');
  }

  // —— 任务2：找扳手小游戏 ——
  private spawnSearchPois(): void {
    const wrenchIdx = Math.floor(Math.random() * SEARCH_SPOTS.length);
    SEARCH_SPOTS.forEach((s, i) => {
      const poi = this.addPoi(s.tx, s.ty, s.label, {
        onInteract: () => {
          if (i === wrenchIdx) {
            this.showSpeech('找到了！套筒扳手就在这里。伊莱亚斯：「周五，早上六点。不用长篇大论，不许拖延。」');
            TaskSystem.inst.complete('ch1_wrench');
            this.clearSearchPois();
          } else {
            this.showSpeech(s.empty);
            this.removePoi(poi);
            const k = this.searchPois.indexOf(poi);
            if (k >= 0) this.searchPois.splice(k, 1);
          }
        }
      });
      this.searchPois.push(poi);
    });
  }

  private clearSearchPois(): void {
    for (const p of this.searchPois) this.removePoi(p);
    this.searchPois = [];
  }
}
