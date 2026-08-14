// 全局游戏状态（单例）：章节、倒计时、隐藏倾向、人物羁绊、叙事事实、携带道具、结局
// 设计原则（文档第十节）：
// - 界面不显示道德值/好感度/倾向，仅后台记录，改变台词与细节画面
// - 结局由玩家物理走向决定，数值只影响结局细节（台词/光照/道具动作）
// - 选择以叙事事实记录，而非赤裸数字（如"已参加玛雅展览"）
// 持久化到 localStorage，符合 website 形态的存档需求。

import { ChapterId, nextChapter } from './Chapter';

// ---- 结局类型（文档第九节，由玩家终章走向决定）----
export type EndingType =
  | 'go_north'      // 9.1 走向汽车
  | 'return_home'   // 9.2 回到小镇街区
  | 'unknown_path'  // 9.3 踏上无名小路
  | 'with_maya'     // 9.4 走向玛雅
  | 'with_noah'     // 9.4 走向诺亚
  | 'with_leo';     // 9.4 走向利奥

export const ENDING_LABEL: Record<EndingType, string> = {
  go_north: '向北远行',
  return_home: '归于故土',
  unknown_path: '无图之途',
  with_maya: '相伴同行 · 玛雅',
  with_noah: '相伴同行 · 诺亚',
  with_leo: '相伴同行 · 利奥'
};

// ---- 三项隐藏倾向（每项 -100..100）----
export interface Tendency {
  commitment: number;   // 信守约定：完成汽车维修、伊莱亚斯相关任务获得
  rootedness: number;   // 联结故土：支持好友、社区、本地场所获得
  agency: number;       // 自我主导：探索无名区域、质疑成见、接纳不确定性获得
}

// ---- 人物羁绊（伊莱亚斯无独立羁绊，由 commitment 体现）----
export interface Bond {
  maya: number;
  noah: number;
  leo: number;
}

// ---- 第四章任务8：杰米最终携带的一件物品 ----
export type CarryItem = 'group_photo' | 'blank_notebook' | 'house_key' | 'old_map';

export const CARRY_ITEM_LABEL: Record<CarryItem, string> = {
  group_photo: '团体合照',
  blank_notebook: '空白笔记本',
  house_key: '家门钥匙',
  old_map: '旧地图'
};

// ---- 第三章任务对子C：后备箱收纳选中的物品 ----
export type TrunkItem = 'tools' | 'memory_box' | 'maya_painting' | 'noah_recorder' | 'leo_bag';

export const TRUNK_ITEM_LABEL: Record<TrunkItem, string> = {
  tools: '维修工具',
  memory_box: '童年纪念盒',
  maya_painting: '玛雅的画作',
  noah_recorder: '诺亚的录音机',
  leo_bag: '利奥的旅行包'
};

// ---- 一次选择/对话带来的影响 ----
export interface ChoiceEffects {
  commitment?: number;
  rootedness?: number;
  agency?: number;
  bond?: Partial<Bond>;
  flag?: string;              // 叙事事实标记（如 'attended_maya_exhibit'）
  setChapter?: ChapterId;
  advanceDay?: boolean;       // 推进倒计时 -1
  carryItem?: CarryItem;      // 设置携带道具
  trunkItem?: TrunkItem;      // 设置后备箱选中物品
  ending?: EndingType;        // 直接设定结局（终章走向触发区域）
  storyMark?: { chapter: ChapterId; mark: StoryMark }; // 跨章剧情印记
}

// ---- 跨章剧情印记（每章结尾固化，下一章NPC主动提起）----
// A1=偏Elias/计划, B1=中立, C1=偏自我/故土
export type StoryMark = 'A1' | 'B1' | 'C1';

const STORAGE_KEY = 'northbound_save_v2'; // 升级版本，旧 v1 存档不兼容

function clamp(v: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, v));
}

export class GameState {
  chapter: ChapterId = 'ch1';
  daysLeft = 5;                          // 出发倒计时（5→4→3→2→1）
  tendency: Tendency = { commitment: 0, rootedness: 0, agency: 0 };
  bond: Bond = { maya: 0, noah: 0, leo: 0 };
  flags = new Set<string>();             // 叙事事实标记
  resolvedChoices = new Set<string>();   // 已做出的互斥任务选择（存 choiceId）
  carriedItem: CarryItem | null = null;  // 第四章携带道具
  trunkItem: TrunkItem | null = null;    // 第三章后备箱选中物品
  endingDecision: EndingType | null = null; // 终章玩家走向
  storyMarks: Partial<Record<ChapterId, StoryMark>> = {}; // 跨章剧情印记

  private static _inst: GameState | null = null;
  static get inst(): GameState {
    if (!this._inst) this._inst = new GameState();
    return this._inst;
  }
  private constructor() {
    this.load();
  }

  // 记录一次互斥选择的影响（幂等：同一 choiceId 只生效一次）
  recordChoice(id: string, fx: ChoiceEffects): void {
    if (this.resolvedChoices.has(id)) return;
    this.resolvedChoices.add(id);
    this.applyEffects(fx);
  }

  hasResolved(id: string): boolean {
    return this.resolvedChoices.has(id);
  }

  hasFlag(flag: string): boolean {
    return this.flags.has(flag);
  }

  // 应用一次对话/选择的影响（可重复，不进入 resolvedChoices）
  applyEffects(fx: ChoiceEffects): void {
    if (fx.commitment) this.tendency.commitment = clamp(this.tendency.commitment + fx.commitment, -100, 100);
    if (fx.rootedness) this.tendency.rootedness = clamp(this.tendency.rootedness + fx.rootedness, -100, 100);
    if (fx.agency) this.tendency.agency = clamp(this.tendency.agency + fx.agency, -100, 100);
    if (fx.bond) {
      (Object.keys(fx.bond) as (keyof Bond)[]).forEach(k => {
        const v = fx.bond![k];
        if (typeof v === 'number') this.bond[k] = clamp(this.bond[k] + v, -100, 100);
      });
    }
    if (fx.flag) this.flags.add(fx.flag);
    if (fx.setChapter) this.chapter = fx.setChapter;
    if (fx.advanceDay && this.daysLeft > 1) this.daysLeft -= 1;
    if (fx.carryItem) this.carriedItem = fx.carryItem;
    if (fx.trunkItem) this.trunkItem = fx.trunkItem;
    if (fx.ending) this.endingDecision = fx.ending;
    if (fx.storyMark) this.storyMarks[fx.storyMark.chapter] = fx.storyMark.mark;
    this.save();
  }

  // —— 跨章剧情印记 ——
  setStoryMark(chapter: ChapterId, mark: StoryMark): void {
    this.storyMarks[chapter] = mark;
    this.save();
  }

  getStoryMark(chapter: ChapterId): StoryMark | undefined {
    return this.storyMarks[chapter];
  }

  setChapter(c: ChapterId): void {
    this.chapter = c;
    this.save();
  }

  // 推进到下一章，返回新章节或 null（已在终章）
  advance(): ChapterId | null {
    const n = nextChapter(this.chapter);
    if (n) {
      this.chapter = n;
      this.save();
    }
    return n;
  }

  // 推进倒计时一天
  advanceDay(): void {
    if (this.daysLeft > 1) {
      this.daysLeft -= 1;
      this.save();
    }
  }

  // 设定终章结局（由走向触发区域调用）
  setEnding(e: EndingType): void {
    this.endingDecision = e;
    this.save();
  }

  // 结局判定：返回玩家终章走向（未决定则 null）
  computeEnding(): EndingType | null {
    return this.endingDecision;
  }

  // —— 结局细节判定（数值只影响细节，不决定结局）——

  // 9.1 向北远行：高信守 vs 低信守分支
  isHighCommitment(): boolean {
    return this.tendency.commitment >= 0;
  }

  // 9.2 归于故土：高联结（点亮灯火重建）vs 低联结（坐公交站台）
  isHighRootedness(): boolean {
    return this.tendency.rootedness >= 0;
  }

  // 羁绊最高者（用于人物高光动画选择；打平时按 maya>noah>leo 优先）
  topBond(): 'maya' | 'noah' | 'leo' | null {
    const { maya, noah, leo } = this.bond;
    const max = Math.max(maya, noah, leo);
    if (max <= 0) return null;
    if (maya === max) return 'maya';
    if (noah === max) return 'noah';
    return 'leo';
  }

  reset(): void {
    this.chapter = 'ch1';
    this.daysLeft = 5;
    this.tendency = { commitment: 0, rootedness: 0, agency: 0 };
    this.bond = { maya: 0, noah: 0, leo: 0 };
    this.flags.clear();
    this.resolvedChoices.clear();
    this.carriedItem = null;
    this.trunkItem = null;
    this.endingDecision = null;
    this.storyMarks = {};
    this.save();
  }

  // ---- localStorage 持久化 ----
  private save(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({
        chapter: this.chapter,
        daysLeft: this.daysLeft,
        tendency: this.tendency,
        bond: this.bond,
        flags: [...this.flags],
        resolvedChoices: [...this.resolvedChoices],
        carriedItem: this.carriedItem,
        trunkItem: this.trunkItem,
        endingDecision: this.endingDecision,
        storyMarks: this.storyMarks
      }));
    } catch {
      /* 存储不可用时静默忽略 */
    }
  }

  private load(): void {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return;
      const d = JSON.parse(raw);
      this.chapter = d.chapter ?? 'ch1';
      this.daysLeft = d.daysLeft ?? 5;
      this.tendency = d.tendency ?? { commitment: 0, rootedness: 0, agency: 0 };
      this.bond = d.bond ?? { maya: 0, noah: 0, leo: 0 };
      this.flags = new Set<string>(d.flags ?? []);
      this.resolvedChoices = new Set<string>(d.resolvedChoices ?? []);
      this.carriedItem = d.carriedItem ?? null;
      this.trunkItem = d.trunkItem ?? null;
      this.endingDecision = d.endingDecision ?? null;
      this.storyMarks = d.storyMarks ?? {};
    } catch {
      /* 损坏存档忽略 */
    }
  }
}
