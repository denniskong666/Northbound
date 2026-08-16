// 屋顶观景台：序章/决裂/尾声复用场景，可眺望北方灯火
// 任务4：清点物资——完成后推进到第二章 + 倒计时-1
// 地图编码：1=墙体 4=屋顶地面
import Phaser from 'phaser';
import { BaseScene, Poi } from './BaseScene';
import { TILE_SIZE } from '../config/GameConfig';
import { TaskSystem } from '../systems/TaskSystem';
import { GameState } from '../state/GameState';
import { CH0_ROOFTOP_DIALOGUE, CH1_BOOTH_DIALOGUE, CH1_ROOFTOP_DIALOGUE, CH2_ROOFTOP_DIALOGUE, CH3_ROOFTOP_DIALOGUE, CH4_ROOFTOP_DIALOGUE } from '../data/Dialogues';
import { DialogueNode, DialogueChoice } from '../systems/DialogueSystem';
import { ROOFTOP_CH1_NPCS } from '../data/NpcDefs';
import { NpcPlacement } from '../data/NpcDefs';
import { StoryMark } from '../state/GameState';
import { L, t } from '../systems/I18n';

const MAP: string[] = [
  '1111111111111',
  '1444444444441',
  '1444444444441',
  '1444444444441',
  '1444444444441',
  '1444444444441',
  '1111111111111'
];

export class RooftopScene extends BaseScene {
  private rooftopPoi?: Poi;

  constructor() {
    super('RooftopScene');
  }

  protected sceneKey(): string { return 'RooftopScene'; }
  protected getMap(): string[] { return MAP; }
  protected getSpawnTile(): { x: number; y: number } { return { x: 6, y: 5 }; }

  protected spawnContent(): void {
    // 重置屋顶 POI 状态（防止跨周目残留）
    this.rooftopPoi = undefined;

    // —— Inmost 风格屋顶装饰 ——
    this.spawnRooftopDecorations();

    // 远方灯火装饰（呼应序章"远方城市的灯火"）
    const lights = this.add.graphics().setDepth(2);
    const worldW = this.mapWidthTiles * TILE_SIZE;
    for (let i = 0; i < 18; i++) {
      const x = Phaser.Math.Between(8, worldW - 8);
      const y = Phaser.Math.Between(6, 20);
      const r = Phaser.Math.FloatBetween(0.6, 1.6);
      lights.fillStyle(0xf5c97a, Phaser.Math.FloatBetween(0.35, 0.85));
      lights.fillCircle(x, y, r);
    }
    this.tweens.add({
      targets: lights,
      alpha: { from: 0.7, to: 1 },
      duration: 2600,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    });

    // —— 章节屋顶任务判定 ——
    const isCh0Rooftop = TaskSystem.inst.isUnlocked('ch0_rooftop') && !TaskSystem.inst.isDone('ch0_rooftop') && !GameState.inst.hasFlag('ch0_rooftop_dlg_started');
    const isCh1Rooftop = TaskSystem.inst.isUnlocked('ch1_rooftop') && !TaskSystem.inst.isDone('ch1_rooftop') && !GameState.inst.hasFlag('ch1_rooftop_dlg_started');
    const isCh2Rooftop = TaskSystem.inst.isUnlocked('ch2_rooftop') && !TaskSystem.inst.isDone('ch2_rooftop') && !GameState.inst.hasFlag('ch2_rooftop_dlg_started');
    const isCh3Rooftop = TaskSystem.inst.isUnlocked('ch3_rooftop') && !TaskSystem.inst.isDone('ch3_rooftop') && !GameState.inst.hasFlag('ch3_rooftop_dlg_started');
    const isCh4Rooftop = TaskSystem.inst.isUnlocked('ch4_rooftop') && !TaskSystem.inst.isDone('ch4_rooftop') && !GameState.inst.hasFlag('ch4_rooftop_dlg_started');

    // —— 序章屋顶聚会：全员眺望北方，约定一起出发 ——
    if (isCh0Rooftop) {
      // 四个 NPC 出现在屋顶
      this.spawnNpcs(ROOFTOP_CH1_NPCS);

      // 屋顶聚会 POI
      this.rooftopPoi = this.addPoi(6, 4, L('屋顶聚会', 'Rooftop Gathering'), {
        type: 'task',
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
          GameState.inst.applyEffects({ flag: 'ch0_rooftop_dlg_started' });
          // 序章屋顶对话：全员欢愉，约定一起出发
          this.dialogueSystem.start(CH0_ROOFTOP_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch0_rooftop_dlg_done' });
            TaskSystem.inst.complete('ch0_rooftop');
            // 丝滑黑屏转场：序章 → 第一章（不消耗倒计时天数，不生成印记）
            this.time.delayedCall(600, () => {
              this.playChapterTransition(() => {
                GameState.inst.advance();  // ch0 → ch1
                this.applyChapterContent(GameState.inst.chapter);
              });
            });
          });
        }
      });
    } else if (isCh1Rooftop) {
      // —— 第一章任务4：清点物资 ——
      // 四人齐聚天台，玩家走到中央依次触发对话1（攒路费的意义）→ 对话2（远方vs眼下）→ 丝滑转场推进章节
      // 两段对话一次性触发，POI 触发后立即移除，玩家无法重复刷好感
      // 四个 NPC 出现在屋顶
      this.spawnNpcs(ROOFTOP_CH1_NPCS);

      // 清点物资：对话1 → 对话2 → 推进章节
      this.rooftopPoi = this.addPoi(6, 4, L('清点物资', 'Inventory Check'), {
        type: 'task',
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
          GameState.inst.applyEffects({ flag: 'ch1_rooftop_dlg_started' });
          // 对话1：攒路费的意义
          this.dialogueSystem.start(CH1_BOOTH_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch1_booth_done' });
            // 对话2：远方 vs 眼下
            this.dialogueSystem.start(CH1_ROOFTOP_DIALOGUE, () => {
              GameState.inst.applyEffects({ flag: 'ch1_rooftop_dlg_done' });
              // 固化 ch1 剧情印记：根据累计 commitment vs agency 判断
              const { commitment, agency } = GameState.inst.tendency;
              const mark: StoryMark = commitment > agency + 0.5 ? 'A1' : (agency > commitment + 0.5 ? 'C1' : 'B1');
              GameState.inst.setStoryMark('ch1', mark);
              TaskSystem.inst.complete('ch1_rooftop');
              // 丝滑黑屏转场（CG 动画插入点），不立即跳章节、不显示标题
              this.time.delayedCall(600, () => {
                this.playChapterTransition(() => {
                  GameState.inst.advance();      // ch1 → ch2
                  GameState.inst.advanceDay();   // 5 → 4
                  this.applyChapterContent(GameState.inst.chapter);
                });
              });
            });
          });
        }
      });
    } else if (isCh2Rooftop) {
      // —— 第二章章节收尾：屋顶雨夜 ——
      // Maya + Noah 在场，触发关于取舍的讨论 → 丝滑转场推进 ch3 + 倒计时 4→3
      const ch2Npcs: NpcPlacement[] = [
        { id: 'maya', tileX: 4, tileY: 3, facing: 'down', label: t('talk_to_maya') },
        { id: 'noah', tileX: 8, tileY: 3, facing: 'down', label: t('talk_to_noah') }
      ];
      this.spawnNpcs(ch2Npcs);

      this.rooftopPoi = this.addPoi(6, 4, L('走近他们', 'Approach Them'), {
        type: 'task',
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
          GameState.inst.applyEffects({ flag: 'ch2_rooftop_dlg_started' });
          this.dialogueSystem.start(CH2_ROOFTOP_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch2_rooftop_dlg_done' });
            // 固化 ch2 印记
            const { commitment, agency } = GameState.inst.tendency;
            const mark: StoryMark = commitment > agency + 0.5 ? 'A2' : (agency > commitment + 0.5 ? 'C2' : 'B2');
            GameState.inst.setStoryMark('ch2', mark);
            TaskSystem.inst.complete('ch2_rooftop');
            this.time.delayedCall(600, () => {
              this.playChapterTransition(() => {
                GameState.inst.advance();      // ch2 → ch3
                GameState.inst.advanceDay();   // 4 → 3
                this.applyChapterContent(GameState.inst.chapter);
              });
            });
          });
        }
      });
    } else if (isCh3Rooftop) {
      // —— 第三章章节收尾：屋顶矛盾强制剧情 ——
      // Elias + Maya 在场 → 丝滑转场推进 ch4 + 倒计时 3→2
      // 注意：ch3 印记已由核心任务对话（社区办事处）固化为 A3/B3/C3，此处不再重算
      // 【动态台词分支】：偏Elias→Elias开场满意；偏Maya→Elias抱怨；中立→调和原词
      const ch3Npcs: NpcPlacement[] = [
        { id: 'elias', tileX: 4, tileY: 3, facing: 'down', label: t('talk_to_elias') },
        { id: 'maya',  tileX: 8, tileY: 3, facing: 'down', label: t('talk_to_maya') }
      ];
      this.spawnNpcs(ch3Npcs);

      this.rooftopPoi = this.addPoi(6, 4, L('走近他们', 'Approach Them'), {
        type: 'task',
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
          GameState.inst.applyEffects({ flag: 'ch3_rooftop_dlg_started' });

          const favElias = GameState.inst.isFavoredElias();
          const favMaya = GameState.inst.isFavoredMaya();

          // 第三章屋顶对话 nodeHook：动态改写 e_open 开场台词
          const ch3Hook = (nid: string, base: DialogueNode): DialogueNode | null => {
            if (nid !== 'e_open') return null;
            let eliasText: string;
            let nextMaya: string;
            if (favElias) {
              // 偏向Elias：他感到满意，不提抱怨
              eliasText = L('太好了。从攒路费到办材料，你一直和我站在一起。等所有人就位，我们就可以出发了。', "Great. From saving up to arranging papers, you've stood with me all along. Once everyone's ready, we can set out.");
              nextMaya = 'm_open_satisfied';
            } else if (favMaya) {
              // 偏向Maya：原版抱怨
              eliasText = L('所有人都随心所欲打乱计划，只有我死守约定。', "Everyone disrupts the plan as they please, while I'm the only one holding onto our promise.");
              nextMaya = 'm_open';
            } else {
              // 中立：温和版
              eliasText = L('我知道大家各有各的难处，但约定还是尽量不要打破——我们已经走到这一步了。', "I know everyone has their own struggles, but let's try not to break the promise — we've come this far.");
              nextMaya = 'm_open_neutral';
            }
            return { speaker: base.speaker, text: eliasText, next: nextMaya };
          };

          // 动态注入 Maya 的补充回应节点（按需）
          const extendedNodes: Record<string, DialogueNode> = {
            ...CH3_ROOFTOP_DIALOGUE.nodes,
            m_open_satisfied: {
              speaker: t('npc_maya'),
              text: L('……好吧。既然你这么坚持，那我会尽量跟上你们的节奏。', "...Alright. If you insist that much, I'll try to keep up with your pace."),
              next: 'ask'
            },
            m_open_neutral: {
              speaker: t('npc_maya'),
              text: L('但约定也要人乐意遵守才行。每个人都有选择自己人生的权利。', "But a promise should be kept willingly. Everyone has the right to choose their own life."),
              next: 'ask'
            }
          };
          const ch3Data = { ...CH3_ROOFTOP_DIALOGUE, nodes: extendedNodes };

          this.dialogueSystem.start(ch3Data, () => {
            GameState.inst.applyEffects({ flag: 'ch3_rooftop_dlg_done' });
            // ch3 印记已由社区办事处对话固化（A3/B3/C3），此处不再覆盖
            TaskSystem.inst.complete('ch3_rooftop');
            this.time.delayedCall(600, () => {
              this.playChapterTransition(() => {
                GameState.inst.advance();      // ch3 → ch4
                GameState.inst.advanceDay();   // 3 → 2
                this.applyChapterContent(GameState.inst.chapter);
              });
            });
          }, ch3Hook);
        }
      });
    } else if (isCh4Rooftop) {
      // —— 第四章章节收尾：屋顶终章前置对话 ——
      // Noah + Leo 在场，四选一直接锁定结局大方向 → 丝滑转场推进 epilogue
      // 注意：ch4 印记已由主线对话（整理物资）固化为 A4/B4/C4，此处不再重算
      // 【选项动态隐藏】：强偏北上→移除留下选项；强偏留下→移除北上选项
      const ch4Npcs: NpcPlacement[] = [
        { id: 'noah', tileX: 4, tileY: 3, facing: 'down', label: t('talk_to_noah') },
        { id: 'leo',  tileX: 8, tileY: 3, facing: 'down', label: t('talk_to_leo') }
      ];
      this.spawnNpcs(ch4Npcs);

      this.rooftopPoi = this.addPoi(6, 4, L('最终抉择', 'Final Choice'), {
        type: 'task',
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
          GameState.inst.applyEffects({ flag: 'ch4_rooftop_dlg_started' });

          const leansNorth = GameState.inst.leansNorthbound();
          const leansStay = GameState.inst.leansStaying();

          // 第四章屋顶 nodeHook：动态改写 ask 节点的 choices
          // 偏向Elias/北上 → 只留「北上」(0) + 「独行」(2)
          // 偏向留下       → 只留「留下」(1) + 「独行」(2)
          // 飘忽不定       → 四个选项全保留
          const ch4Hook = (nid: string, base: DialogueNode): DialogueNode | null => {
            if (nid !== 'ask') return null;
            let choices = base.choices?.slice() ?? [];
            if (leansNorth) {
              // 偏向北上：只保留 index 0（北上）和 2（独行）
              choices = choices.filter((_, i) => i === 0 || i === 2);
            } else if (leansStay) {
              // 偏向留下：只保留 index 1（留下）和 2（独行）
              choices = choices.filter((_, i) => i === 1 || i === 2);
            }
            return { ...base, choices };
          };

          this.dialogueSystem.start(CH4_ROOFTOP_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch4_rooftop_dlg_done' });
            // ch4 印记已由整理物资对话固化（A4/B4/C4），此处不再覆盖
            TaskSystem.inst.complete('ch4_rooftop');
            // 结局已由四选一对话直接设定（go_north / return_home / unknown_path / pause_journey）
            // 全局数据导入终章 → 丝滑转场到 EpilogueScene
            this.time.delayedCall(600, () => {
              this.inputLocked = true;
              this.cameras.main.fadeOut(1200, 0, 0, 0);
              this.cameras.main.once('camerafadeoutcomplete', () => {
                GameState.inst.advance();      // ch4 → epilogue
                this.scene.start('EpilogueScene');
              });
            });
          }, ch4Hook);
        }
      });
    } else {
      // 非任务状态：纯眺望
      this.addPoi(6, 1, L('眺望远方', 'Gaze Afar'), {
        line: L('远方城市的灯火，在夜色里格外明亮。今晚，那就是北方。', "Distant city lights glow bright in the night. Tonight, that is the North.")
      });
    }

    // 生锈的折叠椅
    this.addPoi(3, 3, L('折叠椅', 'Folding Chair'), {
      line: L('一把生锈的折叠椅。坐在这里，能看见所有人的来路。', "A rusted folding chair. Sitting here, you can see where everyone came from.")
    });
    // 粉笔箭头与众人名字缩写
    this.addPoi(9, 3, L('粉笔箭头', 'Chalk Arrow'), {
      line: L('褪色的粉笔箭头指向北方，旁边写着五个人的名字缩写。', "A faded chalk arrow points north, with five people's initials scribbled beside it.")
    });
    // 饱经风霜的旧地图
    this.addPoi(2, 2, L('旧地图', 'Old Map'), {
      line: L('一张饱经风霜的旧地图，标注着北方的方向。', 'A weathered old map marking the direction to the North.')
    });

    // 门：回老街区
    this.addDoor(6, 5, L('下楼回老街区', 'Down to the Old District'), 'OldDistrictScene');
  }

  // —— Inmost 风格屋顶装饰 ——
  private spawnRooftopDecorations(): void {
    const T = TILE_SIZE;
    const worldW = this.mapWidthTiles * T;

    // 远处城市灯光带
    this.sceneArt.placeDistantCityLights(8, worldW);

    // 天线（2 根）
    this.sceneArt.placeAntenna(2 * T + T / 2, 1 * T);
    this.sceneArt.placeAntenna(10 * T + T / 2, 1 * T);

    // 空调外机（2 台）
    this.sceneArt.placeACUnit(3 * T + T / 2, 4 * T + T / 2);
    this.sceneArt.placeACUnit(9 * T + T / 2, 4 * T + T / 2);

    // 栏杆（屋顶边缘）
    this.sceneArt.placeRailing(1 * T, 1 * T, 11);

    // 管道
    this.sceneArt.placePipe(2 * T + T / 2, 4 * T);
    this.sceneArt.placePipe(8 * T + T / 2, 4 * T);

    // 木箱
    this.sceneArt.placeCrateStack(4 * T + T / 2, 4 * T + T / 2);

    // 街灯（屋顶边缘一盏，照向远方）
    this.sceneArt.placeStreetLamp(6 * T + T / 2, 1 * T + T / 2);
  }
}
