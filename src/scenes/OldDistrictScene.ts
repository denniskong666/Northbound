// 老街区场景（主地图）：4 个 NPC + 露丝餐厅打工 + 取零部件 + 通往修理厂/屋顶的两扇门
// 地图编码：0=地面(可行) 1=建筑墙体(碰撞) 2=道路(可行)
import { BaseScene, Poi } from './BaseScene';
import { ChapterId } from '../state/Chapter';
import { TILE_SIZE } from '../config/GameConfig';
import { OLD_DISTRICT_NPCS, NpcId } from '../data/NpcDefs';
import { TaskSystem } from '../systems/TaskSystem';
import { GameState, ChoiceEffects } from '../state/GameState';
import { CH0_POSTCARD_DESC, CH0_BOARD_DESC, CH0_WISH_OPTIONS, WishType, CH2_SUPPLIES_DIALOGUE, CH3_PASS_DIALOGUE, CH4_MAIN_DIALOGUE } from '../data/Dialogues';
import { DialogueData } from '../systems/DialogueSystem';

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
  { id: 'toolbox',  tx: 4,  ty: 7, label: '取工具箱',     line: '从露丝餐厅取回工具箱。' }
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
  // 用 GameState flag 跨场景记录收集进度：ch1_part_belt / fuse / toolbox
  private static readonly CH1_PART_FLAGS = ['ch1_part_belt', 'ch1_part_fuse', 'ch1_part_toolbox'];

  // 序章：明信片收集状态
  private postcardPois: Array<{ poi: Poi; type: 'aurora' | 'harbor' | 'mountain' | 'gallery'; collected: boolean }> = [];
  private static readonly POSTCARD_FLAGS = ['ch0_card_aurora', 'ch0_card_harbor', 'ch0_card_mountain', 'ch0_card_gallery'] as const;

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

    // 重置打工小游戏状态（防止跨周目残留导致无法重新开始送餐）
    this.carrying = false;
    this.deliverCount = 0;
    this.workPoi = undefined;
    this.deliverPickup = undefined;
    this.deliverDropoff = undefined;
    this.partsPois = [];
    this.choiceMarkers = [];
    // 重置序章明信片收集状态
    this.postcardPois = [];

    // —— Inmost 风格场景装饰 ——
    this.spawnStreetDecorations();

    // —— 序章专属装饰与内容（明信片、看板、愿望墙、宣传海报）——
    if (ch === 'ch0') {
      this.spawnCh0Content();
    }

    // —— NPC 生成（按章节区分）——
    if (ch === 'ch0') {
      // 序章：全员在场，氛围欢愉
      this.spawnNpcs(OLD_DISTRICT_NPCS);
    } else if (ch === 'ch1') {
      // 第一章：4 人都在老街区
      this.spawnNpcs(OLD_DISTRICT_NPCS);
    } else if (ch === 'ch2') {
      // 第二章：Elias/Leo 下线，日常只有 Maya + Noah
      this.spawnNpcs(OLD_DISTRICT_NPCS.filter(n => n.id === 'maya' || n.id === 'noah'));
    } else if (ch === 'ch3') {
      // 第三章：Noah/Leo 下线，Elias + Maya 出场
      this.spawnNpcs(OLD_DISTRICT_NPCS.filter(n => n.id === 'elias' || n.id === 'maya'));
    } else if (ch === 'ch4') {
      // 第四章：Elias/Maya 下线，Noah + Leo 出场
      this.spawnNpcs(OLD_DISTRICT_NPCS.filter(n => n.id === 'noah' || n.id === 'leo'));
    } else {
      // 其他章节：按需生成
      this.spawnNpcs(OLD_DISTRICT_NPCS);
    }

    // —— 第一章任务 ——
    // 任务1：露丝餐厅打工（未完成时显示）
    if (TaskSystem.inst.isUnlocked('ch1_work') && !TaskSystem.inst.isDone('ch1_work')) {
      this.workPoi = this.addPoi(3, 5, '露丝餐厅打工', { type: 'task', onInteract: () => this.startWorkMinigame() });
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
    if (TaskSystem.inst.isUnlocked('ch3_pass') && !TaskSystem.inst.isDone('ch3_pass') && !GameState.inst.hasFlag('ch3_pass_dlg_started')) {
      const passPoi = this.addPoi(12, 6, '社区办事处', {
        type: 'task',
        onInteract: () => {
          this.removePoi(passPoi);
          GameState.inst.applyEffects({ flag: 'ch3_pass_dlg_started' });
          const opening = this.getCh3OpeningDialogue();
          const startMain = () => {
            this.dialogueSystem.start(CH3_PASS_DIALOGUE, () => {
              this.completeTaskWithToast('ch3_pass', '办理出城通行材料');
            });
          };
          if (opening) {
            // 开场台词 → 对话系统播放，完成后接主线对话
            this.dialogueSystem.start(opening, startMain);
          } else {
            startMain();
          }
        }
      });
    }

    // —— 第四章任务：整理回忆 ——
    if (TaskSystem.inst.isUnlocked('ch4_organize') && !TaskSystem.inst.isDone('ch4_organize') && !GameState.inst.hasFlag('ch4_organize_dlg_started')) {
      const orgPoi = this.addPoi(12, 10, '整理物资', {
        type: 'task',
        onInteract: () => {
          this.removePoi(orgPoi);
          GameState.inst.applyEffects({ flag: 'ch4_organize_dlg_started' });
          const opening = this.getCh4OpeningDialogue();
          const startMain = () => {
            this.dialogueSystem.start(CH4_MAIN_DIALOGUE, () => {
              this.completeTaskWithToast('ch4_organize', '整理回忆');
            });
          };
          if (opening) {
            this.dialogueSystem.start(opening, startMain);
          } else {
            startMain();
          }
        }
      });
    }

    // —— 第三章可选支线：帮 Maya 整理画展（ch3_pass 完成后出现）——
    // 全套A印记时画展支线需要更高好感才能解锁（剧本联动）
    if (ch === 'ch3' && TaskSystem.inst.isUnlocked('ch3_maya_help') && !TaskSystem.inst.isDone('ch3_maya_help')) {
      const m1 = GameState.inst.getStoryMark('ch1');
      const m2 = GameState.inst.getStoryMark('ch2');
      const m3 = GameState.inst.getStoryMark('ch3');
      const fullA = m1 === 'A1' && m2 === 'A2' && m3 === 'A3';
      if (fullA) {
        // 全套计划印记：需要 Maya 羁绊 >= 5 才能解锁画展支线
        if (GameState.inst.bond.maya >= 5) {
          this.spawnCh3MayaHelpPois();
        } else {
          this.showToast('画展支线：需要与玛雅有更深的羁绊');
        }
      } else {
        this.spawnCh3MayaHelpPois();
      }
    }

    // —— 第四章可选支线：重走老街的承诺（ch4_organize 完成后出现）——
    if (ch === 'ch4' && TaskSystem.inst.isUnlocked('ch4_memory_walk') && !TaskSystem.inst.isDone('ch4_memory_walk')) {
      this.spawnCh4MemoryWalkPois();
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
    // 序章开场旁白（对话系统形式，带对话框UI+打字机效果）
    if (ch === 'ch0') {
      this.time.delayedCall(500, () => {
        this.playNarration('老街区的午后，阳光正好。最近大家都在聊同一件事——去北方。');
      });
    }
    // 第二章世界状态：布鲁克斯市场"最后一周营业"告示
    if (ch === 'ch2') {
      this.time.delayedCall(500, () => {
        this.playNarration('布鲁克斯市场挂出告示：「最后一周营业」。不少住宅门外堆起了搬家纸箱。');
      });
    }
    // 第四章：确保 Noah/Leo 不重复第一章的 intro 对话
    if (ch === 'ch4') {
      GameState.inst.applyEffects({ flag: 'npc_noah_talked' });
      GameState.inst.applyEffects({ flag: 'npc_leo_talked' });
    }
  }

  // 序章：和所有伙伴聊完后，完成 ch0_talk 任务
  protected onNpcDialogueComplete(npcId: NpcId): void {
    if (GameState.inst.chapter !== 'ch0') return;
    if (TaskSystem.inst.isDone('ch0_talk')) return;

    const allTalked = ['elias', 'maya', 'noah', 'leo'].every(id =>
      GameState.inst.hasFlag(`npc_${id}_talked_ch0`)
    );
    if (allTalked) {
      this.showSpeech('大家都聊过了。该上屋顶汇合了——Elias 说要在那等大家。');
      this.burstSparkle(this.player.x, this.player.y - 8, 0xf5c97a);
      this.completeTaskWithToast('ch0_talk', '北方的召唤');
    } else {
      const remaining = 4 - ['elias', 'maya', 'noah', 'leo'].filter(id =>
        GameState.inst.hasFlag(`npc_${id}_talked_ch0`)
      ).length;
      this.showToast(`还差 ${remaining} 位伙伴没聊`);
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
    this.burstSparkle(this.player.x, this.player.y - 8, 0xf2cc8f);
    this.completeTaskWithToast('ch1_work', '上岗开工');
    this.deliverCount = 0;
    if (this.workPoi) { this.removePoi(this.workPoi); this.workPoi = undefined; }
  }

  // —— 任务3：取零部件 ——
  // 用 GameState flag 持久化收集进度，离开场景再回来不会重置
  private spawnPartsPois(): void {
    // 容错：若三件已全部收集但任务未完成（如旧存档），直接补完
    const already = OldDistrictScene.CH1_PART_FLAGS.filter(f => GameState.inst.hasFlag(f)).length;
    if (already >= PARTS_TARGETS.length) {
      TaskSystem.inst.complete('ch1_parts');
      return;
    }

    for (const t of PARTS_TARGETS) {
      // 已收集的零件不再生成 POI
      if (GameState.inst.hasFlag(`ch1_part_${t.id}`)) continue;

      const poi = this.addPoi(t.tx, t.ty, t.label, {
        type: 'item',
        onInteract: () => {
          this.showSpeech(t.line);
          const px = t.tx * TILE_SIZE + TILE_SIZE / 2;
          const py = t.ty * TILE_SIZE + TILE_SIZE / 2;
          this.burstSparkle(px, py);
          this.removePoi(poi);
          const i = this.partsPois.indexOf(poi);
          if (i >= 0) this.partsPois.splice(i, 1);
          // 记录收集进度到 GameState（跨场景持久化）
          GameState.inst.applyEffects({ flag: `ch1_part_${t.id}` });
          // 检查是否全部收集完成
          const collected = OldDistrictScene.CH1_PART_FLAGS.filter(f => GameState.inst.hasFlag(f)).length;
          if (collected >= PARTS_TARGETS.length) {
            this.showSpeech('三件零部件齐了。该上屋顶和大家汇合了。');
            this.completeTaskWithToast('ch1_parts', '未来的零部件');
          } else {
            this.showToast(`收集 ${collected}/${PARTS_TARGETS.length} · ${t.label}`);
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
        type: 'item',
        onInteract: () => {
          this.removePoi(groceryPoi);
          this.dialogueSystem.start(CH2_SUPPLIES_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch2_supply_grocery' });
            this.burstSparkle(19 * TILE_SIZE + TILE_SIZE / 2, 9 * TILE_SIZE + TILE_SIZE / 2, 0xe07a5f);
            this.showSpeech('收集到一份远行物资（杂货铺）。');
            this.checkCh2SuppliesComplete();
          });
        }
      });
    }

    // 市场食物：纯收集
    if (!GameState.inst.hasFlag('ch2_supply_market')) {
      const marketPoi = this.addPoi(20, 5, '市场', {
        type: 'item',
        onInteract: () => {
          this.showSpeech('收集到一份远行物资（市场食物）。');
          this.burstSparkle(20 * TILE_SIZE + TILE_SIZE / 2, 5 * TILE_SIZE + TILE_SIZE / 2, 0xe07a5f);
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
      this.completeTaskWithToast('ch2_supplies', '收集远行物资');
    }
  }

  // 第三章开场台词：根据 ch1+ch2 印记组合分支
  // 情况1：A1+A2（全程偏北上）→ Elias加急便利 + Maya画展暂锁
  // 情况2：C1+C2（全程偏自我）→ Elias冷淡 + Maya邀请首展
  // 情况3：混合中立 → Elias手续不拖 + Maya延后开展
  // 第三章开场对话：根据 ch1+ch2 印记组合，Elias + Maya 各一句（对话系统播放）
  private getCh3OpeningDialogue(): DialogueData | null {
    const m1 = GameState.inst.getStoryMark('ch1');
    const m2 = GameState.inst.getStoryMark('ch2');
    if (!m1 || !m2) return null;

    const bothA = m1 === 'A1' && m2 === 'A2';
    const bothC = m1 === 'C1' && m2 === 'C2';

    let eliasText: string, mayaText: string;
    if (bothA) {
      eliasText = '之前听 Leo、Maya 说，从攒路费到收集物资，你一直都以我们共同的北上约定为先。办通行材料我帮你加急。';
      mayaText = '我知道你的重心一直在远行，我的画展你大概率没时间来看，我不勉强你。';
    } else if (bothC) {
      eliasText = '我听说你一直认同 Leo，还支持 Maya 留下来画画，看来你早就不把我们年少的约定放在心上了。';
      mayaText = '我很早就想和你聊聊，难得有人能理解我不想盲目离开的想法。首展我特别希望你到场。';
    } else {
      eliasText = '我知道你两边都顾及，不会完全偏袒谁，但通行手续不能拖。';
      mayaText = '如果你愿意抽空过来，我可以把开展时间延后一点。';
    }
    return {
      id: 'ch3_opening',
      start: 'elias',
      nodes: {
        elias: { speaker: '伊莱亚斯', text: eliasText, next: 'maya' },
        maya:  { speaker: '玛雅', text: mayaText }
      }
    };
  }

  // 第四章开场对话：读取前三章全部印记（A1A2A3 / C1C2C3 / 混合中立），Noah + Leo 各一句
  private getCh4OpeningDialogue(): DialogueData | null {
    const m1 = GameState.inst.getStoryMark('ch1');
    const m2 = GameState.inst.getStoryMark('ch2');
    const m3 = GameState.inst.getStoryMark('ch3');
    if (!m1 || !m2 || !m3) return null;

    const fullA = m1 === 'A1' && m2 === 'A2' && m3 === 'A3';
    const fullC = m1 === 'C1' && m2 === 'C2' && m3 === 'C3';

    let noahText: string, leoText: string;
    if (fullA) {
      noahText = 'Maya 和我说，办通行材料的时候你毫不犹豫选择优先北上手续，放弃了她的画展。你从头到尾都只想离开这座城市。';
      leoText = '当初我和你聊老街回忆的时候，你完全不在意，现在看来我们本来就不是一路人。';
    } else if (fullC) {
      noahText = 'Maya 告诉我，为了陪她看画展，你推迟了出城手续。我现在也不想为了逃避家人盲目北上。';
      leoText = '第一章我们在屋顶聊家乡的时候，我就知道你和我一样，舍不得这里的一切。';
    } else {
      noahText = '我听 Maya、Elias 说，一路上你谁都没有刻意辜负，一直在平衡远行和留在本地两种生活。';
      leoText = '不管是走是留，至少你从来没有强迫任何人遵从某一种选择。';
    }
    return {
      id: 'ch4_opening',
      start: 'noah',
      nodes: {
        noah: { speaker: '诺亚', text: noahText, next: 'leo' },
        leo:  { speaker: '利奥', text: leoText }
      }
    };
  }

  // —— 第三章可选支线：帮 Maya 整理画展 ——
  // 两处收集：搬画架、找画册；完成后给予 bond.maya 加成
  private static readonly CH3_HELP_FLAGS = ['ch3_help_easel', 'ch3_help_catalog'];
  private spawnCh3MayaHelpPois(): void {
    const targets = [
      { tx: 15, ty: 3, flag: 'ch3_help_easel',   label: '搬画架', line: '你帮 Maya 把画架搬到画展场地。她轻声说：「没想到你真的愿意抽空帮忙。」' },
      { tx: 7,  ty: 3, flag: 'ch3_help_catalog', label: '找画册', line: '你在杂货铺后找到 Maya 丢失的画册。她翻开后停在一页——画里是五个好友一同驱车向北。' }
    ];
    for (const t of targets) {
      if (GameState.inst.hasFlag(t.flag)) continue;
      const poi = this.addPoi(t.tx, t.ty, t.label, {
        type: 'item',
        onInteract: () => {
          this.removePoi(poi);
          this.showSpeech(t.line);
          this.burstSparkle(t.tx * TILE_SIZE + TILE_SIZE / 2, t.ty * TILE_SIZE + TILE_SIZE / 2, 0xe07a5f);
          GameState.inst.applyEffects({ flag: t.flag, bond: { maya: 1 }, rootedness: 1 });
          this.checkCh3MayaHelpComplete();
        }
      });
    }
  }

  private checkCh3MayaHelpComplete(): void {
    const collected = OldDistrictScene.CH3_HELP_FLAGS.filter(f => GameState.inst.hasFlag(f)).length;
    if (collected >= OldDistrictScene.CH3_HELP_FLAGS.length && !TaskSystem.inst.isDone('ch3_maya_help')) {
      GameState.inst.applyEffects({ bond: { maya: 2 } });
      this.showSpeech('Maya：「谢谢你。不管你最终怎么选，这幅画我都想留给你。」');
      this.completeTaskWithToast('ch3_maya_help', '帮 Maya 整理画展');
    } else {
      this.showToast(`画展准备 ${collected}/${OldDistrictScene.CH3_HELP_FLAGS.length}`);
    }
  }

  // —— 第四章可选支线：重走老街的承诺 ——
  // 三处回忆：合照墙、Noah 的录音机、Leo 的老街角；完成后给予全员羁绊加成
  private static readonly CH4_MEM_FLAGS = ['ch4_mem_photo', 'ch4_mem_recorder', 'ch4_mem_street'];
  private spawnCh4MemoryWalkPois(): void {
    const gs = GameState.inst;
    const m2 = gs.getStoryMark('ch2');
    const m3 = gs.getStoryMark('ch3');

    const photoLine =
      (m3 === 'C3' || m2 === 'C2')
        ? '墙上褪色的合照。五个人站在老街路口——但 Maya 在照片边缘画了一朵小花，那是你们最初分歧的见证。'
        : (m3 === 'A3' || m2 === 'A2')
            ? '墙上褪色的合照。五个人紧紧相拥，脸上是对北上的坚定——这是你们最初的约定。'
            : '墙上褪色的合照。五个人站在老街路口，笑容灿烂——那是北上计划最初的样子。';

    const recorderLine =
      (m3 === 'C3' || m2 === 'C2')
        ? 'Noah 录的一段雨声。他说这是老街告别前的声音，温柔而不舍——走了就再也录不到同样的频率。'
        : (m3 === 'A3' || m2 === 'A2')
            ? 'Noah 录的一段风声。他说这是北方的前奏，从老街的屋檐掠过——走了会想念这里的风。'
            : 'Noah 录的一段风声。他说这是老街深夜的声音，走了就再也录不到同样的频率。';

    const streetLine =
      (m3 === 'C3' || m2 === 'C2')
        ? 'Leo 常站着的那个街角。他说每次想走，走到这里就会停下——这座城比想象中沉，你也选择了沉下来。'
        : (m3 === 'A3' || m2 === 'A2')
            ? 'Leo 常站着的那个街角。他说每次想走，走到这里就会停下——但你最终还是选择了出发。'
            : 'Leo 常站着的那个街角。他说每次想离开，走到这里就会停下——这座城比想象中沉。';

    const targets = [
      { tx: 5,  ty: 9, flag: 'ch4_mem_photo',    label: '合照墙', line: photoLine },
      { tx: 19, ty: 9, flag: 'ch4_mem_recorder', label: 'Noah 的录音机', line: recorderLine },
      { tx: 12, ty: 2, flag: 'ch4_mem_street',   label: 'Leo 的老街角', line: streetLine }
    ];
    for (const t of targets) {
      if (GameState.inst.hasFlag(t.flag)) continue;
      const poi = this.addPoi(t.tx, t.ty, t.label, {
        type: 'item',
        onInteract: () => {
          this.removePoi(poi);
          this.showSpeech(t.line);
          this.burstSparkle(t.tx * TILE_SIZE + TILE_SIZE / 2, t.ty * TILE_SIZE + TILE_SIZE / 2, 0xe6b85c);
          const fx: ChoiceEffects = { flag: t.flag };
          if (t.flag === 'ch4_mem_photo')        fx.bond = { maya: 1, noah: 1, leo: 1 };
          else if (t.flag === 'ch4_mem_recorder') fx.bond = { noah: 2 };
          else                                     fx.bond = { leo: 2 };
          GameState.inst.applyEffects(fx);
          this.checkCh4MemoryWalkComplete();
        }
      });
    }
  }

  private checkCh4MemoryWalkComplete(): void {
    const collected = OldDistrictScene.CH4_MEM_FLAGS.filter(f => GameState.inst.hasFlag(f)).length;
    if (collected >= OldDistrictScene.CH4_MEM_FLAGS.length && !TaskSystem.inst.isDone('ch4_memory_walk')) {
      GameState.inst.applyEffects({ rootedness: 2 });
      this.showSpeech('三个回忆都走过了。不管去留，这些都会一直陪着你。');
      this.completeTaskWithToast('ch4_memory_walk', '重走老街的承诺');
    } else {
      this.showToast(`回忆探访 ${collected}/${OldDistrictScene.CH4_MEM_FLAGS.length}`);
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

  // —— Inmost 风格街角装饰 ——
  private spawnStreetDecorations(): void {
    const T = TILE_SIZE;
    // 街灯（4 盏，分布在街道两侧）
    this.sceneArt.placeStreetLamp(3 * T + T / 2, 5 * T + T / 2);
    this.sceneArt.placeStreetLamp(20 * T + T / 2, 5 * T + T / 2);
    this.sceneArt.placeStreetLamp(7 * T + T / 2, 10 * T + T / 2);
    this.sceneArt.placeStreetLamp(17 * T + T / 2, 10 * T + T / 2);

    // 木箱堆（街角处）
    this.sceneArt.placeCrateStack(2 * T + T / 2, 9 * T + T / 2);
    this.sceneArt.placeCrateStack(21 * T + T / 2, 9 * T + T / 2);

    // 垃圾桶
    this.sceneArt.placeTrashCan(15 * T + T / 2, 2 * T + T / 2);
    this.sceneArt.placeTrashCan(9 * T + T / 2, 13 * T + T / 2);

    // 撕裂的海报（贴在墙上）
    this.sceneArt.placePoster(5 * T + T / 2, 2 * T + T / 2);
    this.sceneArt.placePoster(18 * T + T / 2, 2 * T + T / 2);

    // 水洼（道路上）
    this.sceneArt.placePuddle(12 * T + T / 2, 2 * T + T / 2);
    this.sceneArt.placePuddle(12 * T + T / 2, 14 * T + T / 2);

    // 窗户（建筑墙面上）
    this.sceneArt.placeWindow(5 * T + T / 2, 4 * T + T / 2);
    this.sceneArt.placeWindow(18 * T + T / 2, 4 * T + T / 2);
    this.sceneArt.placeWindow(5 * T + T / 2, 12 * T + T / 2);
    this.sceneArt.placeWindow(18 * T + T / 2, 12 * T + T / 2);

    // 管道（沿墙）
    this.sceneArt.placePipe(2 * T + T / 2, 8 * T + T / 2);
    this.sceneArt.placePipe(20 * T + T / 2, 8 * T + T / 2);

    // 纸箱（散落在街区）
    this.sceneArt.placeBox(10 * T + T / 2, 3 * T + T / 2);
    this.sceneArt.placeBox(14 * T + T / 2, 11 * T + T / 2);

    // 露丝餐厅餐桌（第一章送餐任务的桌位）
    const TABLES = [
      { x: 5, y: 3 }, { x: 8, y: 5 }, { x: 6, y: 8 }, { x: 10, y: 4 }
    ];
    for (const t of TABLES) {
      this.sceneArt.placeTable(t.x * T + T / 2, t.y * T + T / 2);
    }
  }

  // —— 序章专属内容：明信片收集、北方看板、愿望墙、宣传海报 ——
  private spawnCh0Content(): void {
    const T = TILE_SIZE;

    // —— 场景宣传海报装饰（只做氛围，不互动）——
    this.sceneArt.placeRecruitPoster(5 * T + T / 2, 2 * T + T / 2);
    this.sceneArt.placeRecruitPoster(8 * T + T / 2, 13 * T + T / 2);
    this.sceneArt.placeGalleryPoster(18 * T + T / 2, 2 * T + T / 2);
    this.sceneArt.placeGalleryPoster(15 * T + T / 2, 13 * T + T / 2);

    // —— 愿望墙（钉满便签的木板）——
    const wallX = 2 * T + T / 2;
    const wallY = 11 * T + T / 2;
    this.sceneArt.placeWishWall(wallX, wallY);

    // 如果已写过愿望，直接在墙上显示玩家便签；否则添加"写下愿望"POI
    if (GameState.inst.hasFlag('ch0_wish_done')) {
      // 已写过：找到玩家选的类型，直接贴上便签
      const wishType = this.getPlayerWishType();
      if (wishType) {
        this.sceneArt.placePlayerWishNote(wallX, wallY, wishType);
      }
    } else {
      // 未写过：添加互动 POI
      const wishPoi = this.addPoi(2, 11, '写下你的愿望', {
        type: 'info',
        onInteract: () => {
          // 弹出 5 选 1 愿望选项（不再先放大查看，直接选）
          const opts = CH0_WISH_OPTIONS.map(o => o.label);
          this.showSimpleChoices('你的北方愿望是什么？', opts, (idx) => {
            if (idx < 0) return; // ESC 取消
            const choice = CH0_WISH_OPTIONS[idx];
            // 记录 flag + 微数值变化
            GameState.inst.applyEffects({ flag: 'ch0_wish_done' });
            GameState.inst.applyEffects({ flag: `ch0_wish_${choice.id}` });
            if (choice.effect.commitment || choice.effect.agency) {
              const eff: ChoiceEffects = {};
              if (choice.effect.commitment) eff.commitment = choice.effect.commitment;
              if (choice.effect.agency) eff.agency = choice.effect.agency;
              GameState.inst.applyEffects(eff);
            }
            // 立即移除"写下愿望"POI，防止重复写
            if (wishPoi) this.removePoi(wishPoi);
            // 在愿望墙中央贴上玩家便签（直观体现）
            this.sceneArt.placePlayerWishNote(wallX, wallY, choice.id as WishType);
            // 反馈：图钉闪光 + Toast + 台词
            this.burstSparkle(wallX, wallY, 0xf5c97a);
            this.showToast(choice.toast);
            this.time.delayedCall(400, () => {
              this.showSpeech('便签被钉在了愿望板的中央。五个人的愿望终于凑齐了。');
            });
          });
        }
      });
    }

    // —— 北方宣传看板（街区中央大型海报，可放大查看）——
    this.sceneArt.placeNorthBoard(12 * T + T / 2, 8 * T + T / 2);
    this.addZoomablePoi(12, 8, '北方·公告板', 'deco_northboard', 2.5,
      CH0_BOARD_DESC.title, CH0_BOARD_DESC.text);

    // —— 4 张明信片（可放大查看 + 收集品）——
    if (TaskSystem.inst.isUnlocked('ch0_posters') && !TaskSystem.inst.isDone('ch0_posters')) {
      this.spawnPostcards();
    }
  }

  // 读取玩家选择的愿望类型
  private getPlayerWishType(): WishType | null {
    const types: WishType[] = ['wealth', 'freedom', 'art', 'friends', 'path'];
    for (const t of types) {
      if (GameState.inst.hasFlag(`ch0_wish_${t}`)) return t;
    }
    return null;
  }

  // 生成 4 张明信片 POI（根据 flag 跳过已收集的）
  private spawnPostcards(): void {
    const T = TILE_SIZE;
    type CardType = 'aurora' | 'harbor' | 'mountain' | 'gallery';
    const CARDS: Array<{ type: CardType; flag: string; tx: number; ty: number }> = [
      { type: 'aurora',   flag: 'ch0_card_aurora',   tx: 7,  ty: 6  },  // 街灯下
      { type: 'harbor',   flag: 'ch0_card_harbor',   tx: 17, ty: 6  },  // 墙角
      { type: 'mountain', flag: 'ch0_card_mountain', tx: 13, ty: 11 }, // 水洼旁
      { type: 'gallery',  flag: 'ch0_card_gallery',  tx: 3,  ty: 12 }  // 愿望墙旁
    ];

    for (const card of CARDS) {
      if (GameState.inst.hasFlag(card.flag)) continue; // 已收集不重生

      // 放置明信片视觉 + 浮动动画
      const visual = this.sceneArt.placePostcard(card.tx * T + T / 2, card.ty * T + T / 2, card.type);
      visual.setDepth(4);

      const texKey = 'postcard_' + card.type;
      const desc = CH0_POSTCARD_DESC[card.type];

      // 添加可放大 + 收集的 POI
      const poi = this.addPoi(card.tx, card.ty, '明信片', {
        type: 'item',
        onInteract: () => {
          // 先展示放大查看，看完后收集
          this.showZoomView(texKey, desc.text, desc.title, 3.0, () => {
            // 关闭放大后：收集！
            GameState.inst.applyEffects({ flag: card.flag });
            visual.destroy();
            if (poi) this.removePoi(poi);
            // 记录收集状态
            const entry = this.postcardPois.find(p => p.type === card.type);
            if (entry) entry.collected = true;
            // 粒子 + Toast
            this.burstSparkle(card.tx * T + T / 2, card.ty * T + T / 2, 0x8ad8ff);
            // 检查是否收集完 4 张
            const totalCollected = OldDistrictScene.POSTCARD_FLAGS.filter(
              f => GameState.inst.hasFlag(f)
            ).length;
            if (totalCollected >= 4) {
              this.completeTaskWithToast('ch0_posters', '北方的讯息');
              this.showSpeech('4 张来自北方的明信片都看完了。该去找伙伴们聊聊了——每个人都在期待北方！');
            } else {
              this.showToast(`明信片 ${totalCollected}/4`);
            }
          });
        }
      });

      this.postcardPois.push({ poi, type: card.type, collected: false });
    }
  }
}
