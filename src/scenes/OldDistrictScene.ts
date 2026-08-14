// 老街区场景（主地图）：4 个 NPC + 露丝餐厅打工 + 取零部件 + 通往修理厂/屋顶的两扇门
// 地图编码：0=地面(可行) 1=建筑墙体(碰撞) 2=道路(可行)
import { BaseScene, Poi } from './BaseScene';
import { ChapterId } from '../state/Chapter';
import { OLD_DISTRICT_NPCS } from '../data/NpcDefs';
import { TaskSystem } from '../systems/TaskSystem';
import { GameState } from '../state/GameState';
import { CH2_SUPPLIES_DIALOGUE, CH3_PASS_DIALOGUE } from '../data/Dialogues';

const MAP: string[] = [
  '1111111111111111111111111',
  '1000000002222000000000001',
  '1001110002222000011110001',
  '1001010000000000001010001',
  '1001010000000000001010001',
  '1001010000000000000000001',
  '1001110000000000011110001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1000000000000000000000001',
  '1001110000000000011110001',
  '1001010000000000001010001',
  '1001010000000000001010001',
  '1001110000000000001010001',
  '1000000002222000000000001',
  '1111111111111111111111111'
];

// 第三章任务对子A标识（演示用，阶段3任务系统会重新分配到各场景）
const CH3_CHOICE_ID = 'ch3_first';

// 任务3：零部件取物点（文档8.2任务3：风扇皮带/保险丝/工具箱）
const PARTS_TARGETS: { id: string; tx: number; ty: number; label: string; line: string }[] = [
  { id: 'belt',     tx: 20, ty: 8, label: '取风扇皮带', line: '从布鲁克斯市场取到风扇皮带。' },
  { id: 'fuse',     tx: 8,  ty: 8, label: '取保险丝',     line: '从电子店拿到保险丝。' },
  { id: 'toolbox',  tx: 4,  ty: 5, label: '取工具箱',     line: '从露丝餐厅取回工具箱。' }
];

export class OldDistrictScene extends BaseScene {
  private choiceMarkers: Poi[] = [];

  // 任务1 打工送餐小游戏状态
  private workPoi?: Poi;
  private deliverPickup?: Poi;
  private deliverDropoff?: Poi;
  private carrying = false;
  private deliverCount = 0;
  private readonly DELIVER_TOTAL = 3;

  // 任务3 取物状态
  private partsPois: Poi[] = [];
  private partsCollected = 0;

  constructor() {
    super('OldDistrictScene');
  }

  protected sceneKey(): string { return 'OldDistrictScene'; }
  protected getMap(): string[] { return MAP; }
  protected getSpawnTile(): { x: number; y: number } { return { x: 12, y: 8 }; }

  protected registerChoices(): void {
    this.choiceSystem.register({
      id: CH3_CHOICE_ID,
      prompt: '更换交流发电机（伊莱亚斯）/ 拂晓微光（玛雅画展）',
      options: [
        { id: 'elias_alternator', label: '更换交流发电机', effects: { commitment: 15, flag: 'task_alternator_done' } },
        { id: 'maya_exhibit',     label: '拂晓微光 · 玛雅画展', effects: { rootedness: 15, bond: { maya: 10 }, flag: 'attended_maya_exhibit' } }
      ]
    });
  }

  protected spawnContent(): void {
    const ch = GameState.inst.chapter;

    // —— NPC 生成（按章节区分）——
    if (ch === 'ch1') {
      // 第一章：4 人都在老街区
      this.spawnNpcs(OLD_DISTRICT_NPCS);
    } else if (ch === 'ch2') {
      // 第二章：Elias/Leo 下线，日常只有 Maya + Noah
      this.spawnNpcs(OLD_DISTRICT_NPCS.filter(n => n.id === 'maya' || n.id === 'noah'));
    } else if (ch === 'ch3') {
      // 第三章：Noah/Leo 下线，Elias + Maya 出场
      this.spawnNpcs(OLD_DISTRICT_NPCS.filter(n => n.id === 'elias' || n.id === 'maya'));
    } else {
      // 其他章节：按需生成
      this.spawnNpcs(OLD_DISTRICT_NPCS);
    }

    // —— 第一章任务 ——
    // 任务1：露丝餐厅打工（未完成时显示）
    if (!TaskSystem.inst.isDone('ch1_work')) {
      this.workPoi = this.addPoi(3, 5, '露丝餐厅打工', { onInteract: () => this.startWorkMinigame() });
    }

    // 任务3：取零部件（任务3激活时显示）
    if (TaskSystem.inst.isUnlocked('ch1_parts') && !TaskSystem.inst.isDone('ch1_parts')) {
      this.spawnPartsPois();
    }

    // —— 第二章任务：收集远行物资（3 处）——
    // 杂货铺（触发对话1，Maya+Noah 双人）+ 市场食物 + 修理厂工具
    if (TaskSystem.inst.isUnlocked('ch2_supplies') && !TaskSystem.inst.isDone('ch2_supplies')) {
      this.spawnCh2SuppliesPois();
    }

    // —— 第三章任务：办理出城通行材料 ——
    // 市政厅POI，触发 Elias+Maya 对话
    if (TaskSystem.inst.isUnlocked('ch3_pass') && !TaskSystem.inst.isDone('ch3_pass')) {
      this.addPoi(15, 6, '市政厅', {
        onInteract: () => {
          // A1 印记连锁：Elias 态度温和提供加急便利
          const mark = GameState.inst.getStoryMark('ch1');
          if (mark === 'A1') {
            this.showSpeech('伊莱亚斯：「你来得正好。通行材料我已经加急办好了——你说过要一起走的，我记着。」');
          }
          this.dialogueSystem.start(CH3_PASS_DIALOGUE, () => {
            TaskSystem.inst.complete('ch3_pass');
          });
        }
      });
    }

    // 两扇门：通往修理厂 / 屋顶
    this.addDoor(12, 14, '进修理厂', 'GarageScene');
    this.addDoor(21, 5, '上屋顶', 'RooftopScene');
  }

  protected applyChapterContent(ch: ChapterId): void {
    this.clearChoiceMarkers();
    if (ch === 'ch3' && !this.choiceSystem.isResolved(CH3_CHOICE_ID)) {
      this.spawnChapter3Choice();
    }
    // 第二章世界状态：布鲁克斯市场"最后一周营业"告示
    if (ch === 'ch2') {
      this.showSpeech('布鲁克斯市场挂出告示：「最后一周营业」。不少住宅门外堆起了搬家纸箱。');
    }
  }

  // —— 任务1：打工送餐小游戏 ——
  private startWorkMinigame(): void {
    if (TaskSystem.inst.isDone('ch1_work')) {
      this.showSpeech('今天的班已经上完了。');
      return;
    }
    if (this.carrying || this.deliverCount > 0) {
      this.showSpeech('先把这单送出去。');
      return;
    }
    this.showSpeech('利奥：「再干五班，我就彻底告别打工生涯。」杰米：「你才来这里干三周而已。」');
    this.spawnDeliverPickup();
  }

  private spawnDeliverPickup(): void {
    this.deliverPickup = this.addPoi(3, 6, '取餐', {
      onInteract: () => {
        if (!this.deliverPickup) return;
        this.removePoi(this.deliverPickup);
        this.deliverPickup = undefined;
        this.carrying = true;
        this.showSpeech('取到餐品，送到客人桌上。');
        this.spawnDeliverDropoff();
      }
    });
  }

  private spawnDeliverDropoff(): void {
    const tables = [{ x: 5, y: 3 }, { x: 8, y: 5 }, { x: 6, y: 8 }, { x: 10, y: 4 }];
    const t = tables[Math.floor(Math.random() * tables.length)];
    this.deliverDropoff = this.addPoi(t.x, t.y, '送达桌位', {
      onInteract: () => {
        if (!this.deliverDropoff) return;
        this.removePoi(this.deliverDropoff);
        this.deliverDropoff = undefined;
        this.carrying = false;
        this.deliverCount++;
        if (this.deliverCount >= this.DELIVER_TOTAL) {
          this.completeWork();
        } else {
          this.showSpeech(`送到了。（${this.deliverCount}/${this.DELIVER_TOTAL}）`);
          this.spawnDeliverPickup();
        }
      }
    });
  }

  private completeWork(): void {
    this.showSpeech('打工所得，存入众人共用的旅行基金。');
    TaskSystem.inst.complete('ch1_work');
    this.deliverCount = 0;
    if (this.workPoi) { this.removePoi(this.workPoi); this.workPoi = undefined; }
  }

  // —— 任务3：取零部件 ——
  private spawnPartsPois(): void {
    for (const t of PARTS_TARGETS) {
      const poi = this.addPoi(t.tx, t.ty, t.label, {
        onInteract: () => {
          this.showSpeech(t.line);
          this.removePoi(poi);
          const i = this.partsPois.indexOf(poi);
          if (i >= 0) this.partsPois.splice(i, 1);
          this.partsCollected++;
          if (this.partsCollected >= PARTS_TARGETS.length) {
            this.showSpeech('三件零部件齐了。带回修理厂。');
            TaskSystem.inst.complete('ch1_parts');
          }
        }
      });
      this.partsPois.push(poi);
    }
  }

  // —— 第二章：收集远行物资（3 处）——
  // 杂货铺（触发对话1，Maya+Noah 双人）+ 市场食物 + 修理厂工具
  // 用 GameState flag 跨场景记录收集进度：ch2_supply_grocery / market / garage
  private static readonly CH2_SUPPLY_FLAGS = ['ch2_supply_grocery', 'ch2_supply_market', 'ch2_supply_garage'];
  private spawnCh2SuppliesPois(): void {
    // 杂货铺：触发对话1（Maya+Noah），对话结束后算收集1件
    if (!GameState.inst.hasFlag('ch2_supply_grocery')) {
      const groceryPoi = this.addPoi(19, 9, '杂货铺', {
        onInteract: () => {
          this.removePoi(groceryPoi);
          this.dialogueSystem.start(CH2_SUPPLIES_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch2_supply_grocery' });
            this.showSpeech('收集到一份远行物资（杂货铺）。');
            this.checkCh2SuppliesComplete();
          });
        }
      });
    }

    // 市场食物：纯收集
    if (!GameState.inst.hasFlag('ch2_supply_market')) {
      const marketPoi = this.addPoi(20, 5, '市场', {
        onInteract: () => {
          this.showSpeech('收集到一份远行物资（市场食物）。');
          this.removePoi(marketPoi);
          GameState.inst.applyEffects({ flag: 'ch2_supply_market' });
          this.checkCh2SuppliesComplete();
        }
      });
    }

    // 如果修理厂那处已在 GarageScene 收集，检查是否全部完成
    this.checkCh2SuppliesComplete();
  }

  private checkCh2SuppliesComplete(): void {
    const collected = OldDistrictScene.CH2_SUPPLY_FLAGS.filter(f => GameState.inst.hasFlag(f)).length;
    if (collected >= OldDistrictScene.CH2_SUPPLY_FLAGS.length && !TaskSystem.inst.isDone('ch2_supplies')) {
      this.showSpeech('远行物资齐了。该上屋顶看看大家了。');
      TaskSystem.inst.complete('ch2_supplies');
    }
  }

  // —— 第三章任务对子A（演示）——
  private spawnChapter3Choice(): void {
    const a = this.addPoi(10, 13, '更换交流发电机', {
      onInteract: () => {
        if (this.choiceSystem.resolve(CH3_CHOICE_ID, 'elias_alternator')) {
          this.showSpeech('「他说离开家的第一晚，才第一次真正喘过气。」伊莱亚斯低声说。');
        } else {
          this.showSpeech('这件事已经过去了。');
        }
      }
    });
    const b = this.addPoi(19, 4, '拂晓微光 · 玛雅画展', {
      onInteract: () => {
        if (this.choiceSystem.resolve(CH3_CHOICE_ID, 'maya_exhibit')) {
          this.showSpeech('玛雅的画里，全是格雷布里奇一处处歇业消亡的景象，而非北方。');
        } else {
          this.showSpeech('这件事已经过去了。');
        }
      }
    });
    this.choiceMarkers.push(a, b);

    this.choiceSystem.onLock(CH3_CHOICE_ID, (chosenId) => {
      const remove = chosenId === 'elias_alternator' ? b : a;
      this.removePoi(remove);
      const i = this.choiceMarkers.indexOf(remove);
      if (i >= 0) this.choiceMarkers.splice(i, 1);
    });
  }

  private clearChoiceMarkers(): void {
    for (const p of this.choiceMarkers) this.removePoi(p);
    this.choiceMarkers = [];
  }
}
