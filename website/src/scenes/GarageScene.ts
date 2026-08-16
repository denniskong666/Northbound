// 瓦尔汽车修理厂：伊莱亚斯的工作场所，好友大本营
// 含褪色蓝色旅行轿车、玛雅儿时画作、老旧屋顶地图、Elias
// 任务2：失踪的套筒扳手——翻找3个点位，随机1处藏有扳手
// 地图编码：0=走廊地面 1=墙体 3=修理厂地面
import { BaseScene, Poi } from './BaseScene';
import { TILE_SIZE } from '../config/GameConfig';
import { NpcPlacement } from '../data/NpcDefs';
import { TaskSystem } from '../systems/TaskSystem';
import { GameState } from '../state/GameState';
import { L, t } from '../systems/I18n';

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
  { id: 'elias', tileX: 9, tileY: 4, facing: 'left', label: t('talk_to_elias') }
];

// 修理厂内的诺亚位置（第二章，任务6录音机）
const GARAGE_CH2_NPCS: NpcPlacement[] = [
  { id: 'noah', tileX: 9, tileY: 4, facing: 'left', label: t('talk_to_noah') }
];

// 任务2：翻找点位（随机1处藏有套筒扳手）
const SEARCH_SPOTS: { tx: number; ty: number; label: string; empty: string }[] = [
  { tx: 4,  ty: 3, label: L('翻找杂物堆', 'Search the Pile'), empty: L('一堆旧零件，没有套筒扳手。', "A pile of old parts — no socket wrench.") },
  { tx: 11, ty: 5, label: L('翻找工具柜', 'Search the Tool Cabinet'), empty: L('工具柜里只有生锈的螺丝。', 'Only rusted screws in the cabinet.') },
  { tx: 7,  ty: 5, label: L('翻找旧纸箱', 'Search the Old Boxes'), empty: L('纸箱里是空的。', 'The box is empty.') }
];

export class GarageScene extends BaseScene {
  private searchPois: Poi[] = [];

  constructor() {
    super('GarageScene');
  }

  protected sceneKey(): string { return 'GarageScene'; }
  protected getMap(): string[] { return MAP; }
  protected getSpawnTile(): { x: number; y: number } { return { x: 7, y: 7 }; }

  // —— 动态描述：根据玩家选择/印记变化 ——
  private getCarDescription(): string {
    const gs = GameState.inst;
    const m1 = gs.getStoryMark('ch1');
    const m2 = gs.getStoryMark('ch2');
    const m3 = gs.getStoryMark('ch3');
    const m4 = gs.getStoryMark('ch4');

    if (m4 === 'A4') return L('褪色的蓝色旅行轿车。你坚持北上的念头没有动摇——五个人的名字依然清晰刻在车门内侧。', "The faded blue station wagon. Your resolve to head north never wavered — the five names are still clearly carved inside the door.");
    if (m4 === 'C4') return L('褪色的蓝色旅行轿车。你没有选择北上，但车门上的刻痕提醒你，这曾经是所有人的共同方向。', "The faded blue station wagon. You didn't choose to head north, but the carvings on the door remind you this was once everyone's shared direction.");
    if (m3 === 'A3') return L('褪色的蓝色旅行轿车。你选择了集体计划优先，车里的行李已经提前为北上打包。', "The faded blue station wagon. You prioritized the group plan — the luggage is already packed for the northbound trip.");
    if (m3 === 'C3') return L('褪色的蓝色旅行轿车。你选择支持 Maya 而放慢了手续，车上还留着画展的宣传册。', "The faded blue station wagon. You chose to support Maya and slowed the paperwork — the exhibit flyer is still in the car.");
    if (m1 === 'A1') return L('褪色的蓝色旅行轿车。车门内侧，刻着五个名字的首字母。你从一开始就坚定要走。', "The faded blue station wagon. Inside the door, five initials are carved. You were determined to leave from the very start.");
    if (m1 === 'C1') return L('褪色的蓝色旅行轿车。你曾动摇过，但名字刻痕提醒你——无论去留，这些人都与你同行过。', "The faded blue station wagon. You wavered once, but the carved names remind you — whether leaving or staying, these people traveled with you.");
    return L('褪色的蓝色旅行轿车。车门内侧，刻着五个名字的首字母。', "The faded blue station wagon. Inside the door, five initials are carved.");
  }

  private getPaintingDescription(): string {
    const gs = GameState.inst;
    const m1 = gs.getStoryMark('ch1');
    const m2 = gs.getStoryMark('ch2');
    const m3 = gs.getStoryMark('ch3');

    // 第三四章：画作随玩家选择而变
    if (gs.chapter === 'ch3' || gs.chapter === 'ch4') {
      if (m3 === 'C3' || m2 === 'C2') {
        // 玩家倾向于 Maya/留下
        return L('墙上 Maya 新画的街区风光——她没有再描绘北方的美丽，而是街区昔日玩伴的合照。画中人的笑容比天空更明亮。', "Maya's new painting on the wall — she no longer paints the beauty of the North, but a group photo of old street friends. The smiles in the painting shine brighter than the sky.");
      }
      if (m3 === 'A3' || m2 === 'A2') {
        // 玩家倾向于北上
        return L('墙上 Maya 的画作变得悲伤而孤独——她画了空荡的老街巷口，画上没有任何人。画角写着：「如果你们都走了，我就画这座城的空。」', "Maya's painting on the wall has become sad and lonely — she painted the empty old street corner, with no one in it. In the corner she wrote: 'If you all leave, I'll paint the emptiness of this city.'");
      }
      // 中立
      return L('墙上 Maya 的画作。画面一半是老街的喧嚣，一半是北方的旷野——她在两种生活之间寻找平衡。', "Maya's painting on the wall. Half the canvas is the bustle of the old street, half the wilderness of the North — she seeks balance between two lives.");
    }

    // 第一章/第二章：儿时的画
    if (m1 === 'A1') return L('画里五个好友一同驱车向北。伊莱亚斯把它当作约定的凭证——你选择了坚守这个约定。', "The painting shows five friends driving north together. Elias kept it as proof of the promise — you chose to hold onto that promise.");
    if (m1 === 'C1') return L('画里五个好友一同驱车向北。你也曾犹豫，但 Maya 画里的笑脸让你无法否认这段友情的重量。', "The painting shows five friends driving north together. You hesitated, but the smiles in Maya's painting make you unable to deny the weight of this friendship.");
    return L('画里五个好友一同驱车向北。伊莱亚斯把它当作约定的凭证。', "The painting shows five friends driving north together. Elias kept it as proof of the promise.");
  }

  private getMapDescription(): string {
    const gs = GameState.inst;
    const m1 = gs.getStoryMark('ch1');
    const m3 = gs.getStoryMark('ch3');

    if (gs.chapter === 'ch4') {
      if (m3 === 'A3') return L('一张旧地图，背面签着五个人的名字。你标记的北上路线依然清晰，是集体决定的见证。', "An old map with five names signed on the back. The northbound route you marked is still clear — a witness to a collective decision.");
      if (m3 === 'C3') return L('一张旧地图，背面签着五个人的名字。你画了一条不走的路——通往另一种可能的自己。', "An old map with five names signed on the back. You drew a road not taken — leading to another possible version of yourself.");
    }
    if (m1 === 'A1') return L('一张旧地图，背面签着五个人的名字。北上的路线被你用红笔圈出，这是你最早的决定。', "An old map with five names signed on the back. The northbound route is circled in red — your earliest decision.");
    if (m1 === 'C1') return L('一张旧地图，背面签着五个人的名字。你没有在任何路线上做标记，似乎还在思考。', "An old map with five names signed on the back. You haven't marked any route — still thinking, it seems.");
    return L('一张旧地图，背面签着五个人的名字。', "An old map with five names signed on the back.");
  }

  protected spawnContent(): void {
    const ch = GameState.inst.chapter;

    // 重置找扳手小游戏状态（防止跨周目残留）
    this.searchPois = [];

    // —— Inmost 风格修理厂装饰 ——
    this.spawnGarageDecorations();

    // NPC：第一章 Elias，第二章 Noah（Elias 下线）
    if (ch === 'ch2') {
      this.spawnNpcs(GARAGE_CH2_NPCS);
    } else {
      this.spawnNpcs(GARAGE_NPCS);
    }

    // —— 关键物品实体（像素 Inmost 风剪影）——
    // 旅行轿车 / 玛雅画作 / 墙面旧地图
    this.sceneArt.placeCar(7 * TILE_SIZE + TILE_SIZE / 2, 3 * TILE_SIZE + TILE_SIZE / 2);
    this.sceneArt.placePainting(2 * TILE_SIZE + TILE_SIZE / 2, 2 * TILE_SIZE + TILE_SIZE / 2);
    this.sceneArt.placeWallMap(12 * TILE_SIZE + TILE_SIZE / 2, 2 * TILE_SIZE + TILE_SIZE / 2);

    // 褪色的蓝色旅行轿车（可放大查看，动态描述）
    this.addZoomablePoi(7, 3, L('检查旅行轿车', 'Inspect the Car'), 'deco_car', 3, L('旅行轿车', 'Station Wagon'), this.getCarDescription());
    // 墙上玛雅儿时的画（可放大查看，动态描述）
    this.addZoomablePoi(2, 2, L('玛雅的画', "Maya's Painting"), 'deco_painting', 3, L('玛雅的画作', "Maya's Painting"), this.getPaintingDescription());
    // 老旧的屋顶地图（可放大查看，动态描述）
    this.addZoomablePoi(12, 2, L('老旧的屋顶地图', 'Old Rooftop Map'), 'deco_wallmap', 3, L('老旧的屋顶地图', 'Old Rooftop Map'), this.getMapDescription());

    // 任务2：失踪的套筒扳手（第一章）
    if (TaskSystem.inst.isUnlocked('ch1_wrench') && !TaskSystem.inst.isDone('ch1_wrench')) {
      this.spawnSearchPois();
    }

    // 任务：第二章修理厂物资收集点（收集工具，计入 ch2_supplies 的第3处）
    if (TaskSystem.inst.isUnlocked('ch2_supplies') && !TaskSystem.inst.isDone('ch2_supplies') && !GameState.inst.hasFlag('ch2_supply_garage')) {
      this.addPoi(11, 4, L('工具架', 'Tool Rack'), {
        type: 'item',
        onInteract: () => {
          this.showSpeech(L('收集到一份远行物资（修理厂工具）。', 'Collected travel supplies (Garage tools).'));
          this.burstSparkle(11 * TILE_SIZE + TILE_SIZE / 2, 4 * TILE_SIZE + TILE_SIZE / 2, 0x81b29a);
          GameState.inst.applyEffects({ flag: 'ch2_supply_garage' });
          const flags = ['ch2_supply_grocery', 'ch2_supply_market', 'ch2_supply_garage'];
          if (flags.every(f => GameState.inst.hasFlag(f)) && !TaskSystem.inst.isDone('ch2_supplies')) {
            this.showSpeech(L('远行物资齐了。该上屋顶看看大家了。', "Travel supplies are all gathered. Time to head up to the Rooftop and check on everyone."));
            this.completeTaskWithToast('ch2_supplies', L('收集远行物资', 'Gather Travel Supplies'));
          }
        }
      });
    }

    // 门：回老街区
    this.addDoor(7, 7, L('回老街区', 'Back to the Old District'), 'OldDistrictScene');
  }

  // —— 任务2：找扳手小游戏 ——
  private spawnSearchPois(): void {
    const wrenchIdx = Math.floor(Math.random() * SEARCH_SPOTS.length);
    SEARCH_SPOTS.forEach((s, i) => {
      const poi = this.addPoi(s.tx, s.ty, s.label, {
        type: 'item',
        onInteract: () => {
          if (i === wrenchIdx) {
            this.showSpeech(L('找到了！套筒扳手就在这里。伊莱亚斯：「周五，早上六点。不用长篇大论，不许拖延。」', "Found it! The socket wrench is here. Elias: 'Friday, six AM. No long speeches, no delays.'"));
            this.burstSparkle(s.tx * TILE_SIZE + TILE_SIZE / 2, s.ty * TILE_SIZE + TILE_SIZE / 2, 0x6b8cae);
            this.completeTaskWithToast('ch1_wrench', L('失踪的套筒扳手', 'The Missing Socket Wrench'));
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

  // —— Inmost 风格修理厂装饰 ——
  private spawnGarageDecorations(): void {
    const T = TILE_SIZE;
    // 悬挂灯（3 盏，照亮修理区）
    this.sceneArt.placeHangingLight(5 * T + T / 2, 1 * T);
    this.sceneArt.placeHangingLight(9 * T + T / 2, 1 * T);
    this.sceneArt.placeHangingLight(12 * T + T / 2, 1 * T);

    // 工具架（墙面）
    this.sceneArt.placeToolRack(2 * T + T / 2, 3 * T + T / 2);
    this.sceneArt.placeToolRack(12 * T + T / 2, 3 * T + T / 2);

    // 工作台
    this.sceneArt.placeWorkbench(4 * T + T / 2, 6 * T + T / 2);
    this.sceneArt.placeWorkbench(10 * T + T / 2, 6 * T + T / 2);

    // 轮胎堆
    this.sceneArt.placeTire(2 * T + T / 2, 5 * T + T / 2);
    this.sceneArt.placeTire(2 * T + T / 2, 5 * T + T / 2 + 14);
    this.sceneArt.placeTire(13 * T + T / 2, 5 * T + T / 2);

    // 管道（沿天花板）
    this.sceneArt.placePipe(3 * T + T / 2, 1 * T + T / 4);
    this.sceneArt.placePipe(8 * T + T / 2, 1 * T + T / 4);

    // 窗户（高处）
    this.sceneArt.placeWindow(1 * T + T / 2, 2 * T + T / 2);
    this.sceneArt.placeWindow(13 * T + T / 2, 2 * T + T / 2);

    // 纸箱
    this.sceneArt.placeBox(11 * T + T / 2, 6 * T + T / 2);
  }
}
