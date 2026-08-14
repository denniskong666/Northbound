// 屋顶观景台：序章/决裂/尾声复用场景，可眺望北方灯火
// 任务4：清点物资——完成后推进到第二章 + 倒计时-1
// 地图编码：1=墙体 4=屋顶地面
import Phaser from 'phaser';
import { BaseScene, Poi } from './BaseScene';
import { TILE_SIZE } from '../config/GameConfig';
import { TaskSystem } from '../systems/TaskSystem';
import { GameState } from '../state/GameState';
import { CH1_BOOTH_DIALOGUE, CH1_ROOFTOP_DIALOGUE, CH2_ROOFTOP_DIALOGUE, CH3_ROOFTOP_DIALOGUE } from '../data/Dialogues';
import { ROOFTOP_CH1_NPCS } from '../data/NpcDefs';
import { NpcPlacement } from '../data/NpcDefs';
import { StoryMark } from '../state/GameState';

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

    // —— 第一章任务4：清点物资 ——
    // 四人齐聚天台，玩家走到中央依次触发对话1（攒路费的意义）→ 对话2（远方vs眼下）→ 丝滑转场推进章节
    // 两段对话一次性触发，POI 触发后立即移除，玩家无法重复刷好感
    const isCh1Rooftop = TaskSystem.inst.isUnlocked('ch1_rooftop') && !TaskSystem.inst.isDone('ch1_rooftop');
    const isCh2Rooftop = TaskSystem.inst.isUnlocked('ch2_rooftop') && !TaskSystem.inst.isDone('ch2_rooftop');
    const isCh3Rooftop = TaskSystem.inst.isUnlocked('ch3_rooftop') && !TaskSystem.inst.isDone('ch3_rooftop');
    if (isCh1Rooftop) {
      // 四个 NPC 出现在屋顶
      this.spawnNpcs(ROOFTOP_CH1_NPCS);

      // 清点物资：对话1 → 对话2 → 推进章节
      this.rooftopPoi = this.addPoi(6, 4, '清点物资', {
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
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
        { id: 'maya', tileX: 4, tileY: 3, facing: 'down', label: '和玛雅说话' },
        { id: 'noah', tileX: 8, tileY: 3, facing: 'down', label: '和诺亚说话' }
      ];
      this.spawnNpcs(ch2Npcs);

      this.rooftopPoi = this.addPoi(6, 4, '走近他们', {
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
          this.dialogueSystem.start(CH2_ROOFTOP_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch2_rooftop_dlg_done' });
            // 固化 ch2 印记
            const { commitment, agency } = GameState.inst.tendency;
            const mark: StoryMark = commitment > agency + 0.5 ? 'A1' : (agency > commitment + 0.5 ? 'C1' : 'B1');
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
      // —— 第三章章节收尾：屋顶抉择 ——
      // Elias + Maya 在场 → 丝滑转场推进 ch4 + 倒计时 3→2
      const ch3Npcs: NpcPlacement[] = [
        { id: 'elias', tileX: 4, tileY: 3, facing: 'down', label: '和伊莱亚斯说话' },
        { id: 'maya',  tileX: 8, tileY: 3, facing: 'down', label: '和玛雅说话' }
      ];
      this.spawnNpcs(ch3Npcs);

      this.rooftopPoi = this.addPoi(6, 4, '走近他们', {
        onInteract: () => {
          if (this.rooftopPoi) { this.removePoi(this.rooftopPoi); this.rooftopPoi = undefined; }
          this.dialogueSystem.start(CH3_ROOFTOP_DIALOGUE, () => {
            GameState.inst.applyEffects({ flag: 'ch3_rooftop_dlg_done' });
            const { commitment, agency } = GameState.inst.tendency;
            const mark: StoryMark = commitment > agency + 0.5 ? 'A1' : (agency > commitment + 0.5 ? 'C1' : 'B1');
            GameState.inst.setStoryMark('ch3', mark);
            TaskSystem.inst.complete('ch3_rooftop');
            this.time.delayedCall(600, () => {
              this.playChapterTransition(() => {
                GameState.inst.advance();      // ch3 → ch4
                GameState.inst.advanceDay();   // 3 → 2
                this.applyChapterContent(GameState.inst.chapter);
              });
            });
          });
        }
      });
    } else {
      // 非任务4状态：纯眺望
      this.addPoi(6, 1, '眺望远方', {
        line: '远方城市的灯火，在夜色里格外明亮。今晚，那就是北方。'
      });
    }

    // 生锈的折叠椅
    this.addPoi(3, 3, '折叠椅', {
      line: '一把生锈的折叠椅。坐在这里，能看见所有人的来路。'
    });
    // 粉笔箭头与众人名字缩写
    this.addPoi(9, 3, '粉笔箭头', {
      line: '褪色的粉笔箭头指向北方，旁边写着五个人的名字缩写。'
    });
    // 饱经风霜的旧地图
    this.addPoi(2, 2, '旧地图', {
      line: '一张饱经风霜的旧地图，标注着北方的方向。'
    });

    // 门：回老街区
    this.addDoor(6, 5, '下楼回老街区', 'OldDistrictScene');
  }
}
