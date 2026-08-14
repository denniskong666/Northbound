// 任务系统：管理章节任务链的激活、完成与当前任务提示
// 设计契合文档：
// - 任务以叙事事实记录（flag: task_<id>_done），不展示属性面板
// - 任务链有序推进：完成前一个才解锁下一个
// - 第一章4任务（文档8.2）：上岗开工 / 失踪的套筒扳手 / 未来的零部件 / 屋顶清点物资
// 任务完成状态持久化于 GameState.flags，跨场景/读档保持一致

import { GameState } from '../state/GameState';
import { ChapterId } from '../state/Chapter';

export interface TaskDef {
  id: string;
  chapter: ChapterId;
  title: string;          // 任务名
  goal: string;           // 目标提示（UI 显示）
  startsAfter?: string;   // 前置任务 id（完成后才激活）
  onChapterComplete?: boolean;  // 完成此任务是否推进到下一章
  advanceDayOnComplete?: boolean; // 完成此任务是否推进倒计时
}

// 第一章任务链（文档 8.2）
export const TASKS: TaskDef[] = [
  {
    id: 'ch1_work',
    chapter: 'ch1',
    title: '上岗开工',
    goal: '在露丝餐厅打工：拾取餐品并送到桌位（3 单）',
    onChapterComplete: false
  },
  {
    id: 'ch1_wrench',
    chapter: 'ch1',
    title: '失踪的套筒扳手',
    goal: '在修理厂后巷翻找，找回套筒扳手',
    startsAfter: 'ch1_work'
  },
  {
    id: 'ch1_parts',
    chapter: 'ch1',
    title: '未来的零部件',
    goal: '取回风扇皮带、保险丝、工具箱（3 件）',
    startsAfter: 'ch1_wrench'
  },
  {
    id: 'ch1_rooftop',
    chapter: 'ch1',
    title: '屋顶清点物资',
    goal: '上屋顶，和伙伴们交谈',
    startsAfter: 'ch1_parts'
    // 章节推进与倒计时不再由 TaskSystem 自动触发，
    // 改由场景的 playChapterTransition 丝滑转场手动推进（留 CG 动画插入点）。
  },

  // 第二章任务链（文档 8.3）：裂痕渐生
  // 主线：收集远行物资；出场 Maya+Noah（Elias/Leo 下线）
  // 世界状态：布鲁克斯市场"最后一周营业"告示、住宅门外搬家纸箱
  {
    id: 'ch2_supplies',
    chapter: 'ch2',
    title: '收集远行物资',
    goal: '去杂货铺、市场、修理厂收集远行物资（3 处）',
    startsAfter: 'ch1_rooftop'
  },
  {
    id: 'ch2_rooftop',
    chapter: 'ch2',
    title: '屋顶雨夜',
    goal: '上屋顶，看看大家',
    startsAfter: 'ch2_supplies'
    // 章节收尾：Maya+Noah 屋顶对话 → 丝滑转场推进 ch3 + 倒计时 4→3
  },

  // 第三章任务链：办理出城通行材料
  // 出场：Elias + Maya（Noah/Leo 下线）
  // 跨章印记：A1 → Elias 态度温和提供加急便利
  {
    id: 'ch3_pass',
    chapter: 'ch3',
    title: '办理出城通行材料',
    goal: '去市政厅办理出城通行材料（Elias 在场）',
    startsAfter: 'ch2_rooftop'
  },
  {
    id: 'ch3_rooftop',
    chapter: 'ch3',
    title: '屋顶抉择',
    goal: '上屋顶，看看大家',
    startsAfter: 'ch3_pass'
    // 章节收尾 → 丝滑转场推进 ch4 + 倒计时 3→2
  }
];

export class TaskSystem {
  private static _inst: TaskSystem | null = null;
  static get inst(): TaskSystem {
    if (!this._inst) this._inst = new TaskSystem();
    return this._inst;
  }
  private constructor() {}

  // 任务是否已完成（flag: task_<id>_done）
  isDone(id: string): boolean {
    return GameState.inst.hasFlag(`task_${id}_done`);
  }

  // 完成任务：记录 flag，按需推进章节/倒计时，返回是否触发了章节推进
  complete(id: string): boolean {
    if (this.isDone(id)) return false;
    const def = TASKS.find(t => t.id === id);
    if (!def) return false;
    GameState.inst.applyEffects({ flag: `task_${id}_done` });
    if (def.advanceDayOnComplete) GameState.inst.advanceDay();
    if (def.onChapterComplete) {
      GameState.inst.advance();
      return true;
    }
    return false;
  }

  // 当前章节的活跃任务（第一个未完成且前置已完成）
  currentTask(chapter: ChapterId): TaskDef | null {
    for (const t of TASKS) {
      if (t.chapter !== chapter) continue;
      if (this.isDone(t.id)) continue;
      if (t.startsAfter && !this.isDone(t.startsAfter)) continue;
      return t;
    }
    return null;
  }

  // 任务是否已解锁（前置完成或无前置）
  isUnlocked(id: string): boolean {
    const def = TASKS.find(t => t.id === id);
    if (!def) return false;
    if (this.isDone(id)) return false;
    if (def.startsAfter && !this.isDone(def.startsAfter)) return false;
    return true;
  }
}
