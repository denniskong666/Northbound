// 任务系统：管理章节任务链的激活、完成与当前任务提示
// 设计契合文档：
// - 任务以叙事事实记录（flag: task_<id>_done），不展示属性面板
// - 任务链有序推进：完成前一个才解锁下一个
// - 序章：和伙伴们聊聊北方的计划 → 屋顶聚会（全员欢愉，北方=希望）
// - 第一章4任务：上岗开工 / 失踪的套筒扳手 / 未来的零部件 / 屋顶清点物资
// 任务完成状态持久化于 GameState.flags，跨场景/读档保持一致

import { GameState } from '../state/GameState';
import { ChapterId } from '../state/Chapter';
import { L } from './I18n';

export interface TaskDef {
  id: string;
  chapter: ChapterId;
  title: string;          // 任务名
  goal: string;           // 目标提示（UI 显示）
  startsAfter?: string;   // 前置任务 id（完成后才激活）
  onChapterComplete?: boolean;  // 完成此任务是否推进到下一章
  advanceDayOnComplete?: boolean; // 完成此任务是否推进倒计时
}

// 序章任务链：全员向往北方，氛围铺垫
export const TASKS: TaskDef[] = [
  {
    id: 'ch0_posters',
    chapter: 'ch0',
    title: L('北方的讯息', 'Messages from the North'),
    goal: L('收集老街区散落的 4 张北方宣传明信片', 'Collect 4 northbound postcards scattered around the old district'),
    onChapterComplete: false
  },
  {
    id: 'ch0_talk',
    chapter: 'ch0',
    title: L('北方的召唤', 'The Call of the North'),
    goal: L('和老街区的伙伴们聊聊大家对北方的期待', 'Talk with friends in the old district about their hopes for the North'),
    startsAfter: 'ch0_posters',
    onChapterComplete: false
  },
  {
    id: 'ch0_rooftop',
    chapter: 'ch0',
    title: L('屋顶聚会', 'Rooftop Gathering'),
    goal: L('上屋顶，和大家一起眺望北方', 'Head to the rooftop and gaze north with everyone'),
    startsAfter: 'ch0_talk'
    // 序章收尾：全员屋顶欢愉对话 → 丝滑转场推进 ch1
  },

  // 第一章任务链（文档 8.2）
  {
    id: 'ch1_work',
    chapter: 'ch1',
    title: L('上岗开工', 'On the Job'),
    goal: L('在露丝餐厅打工：拾取餐品并送到桌位（3 单）', 'Work at Ruth\'s Diner: pick up dishes and deliver them to tables (3 orders)'),
    startsAfter: 'ch0_rooftop',
    onChapterComplete: false
  },
  {
    id: 'ch1_wrench',
    chapter: 'ch1',
    title: L('失踪的套筒扳手', 'The Missing Socket Wrench'),
    goal: L('在修理厂后巷翻找，找回套筒扳手', 'Search the alley behind the repair shop and recover the socket wrench'),
    startsAfter: 'ch1_work'
  },
  {
    id: 'ch1_parts',
    chapter: 'ch1',
    title: L('未来的零部件', 'Parts for the Future'),
    goal: L('取回风扇皮带、保险丝、工具箱（3 件）', 'Retrieve the fan belt, fuses, and toolbox (3 items)'),
    startsAfter: 'ch1_wrench'
  },
  {
    id: 'ch1_rooftop',
    chapter: 'ch1',
    title: L('屋顶清点物资', 'Rooftop Inventory'),
    goal: L('上屋顶，和伙伴们交谈', 'Head to the rooftop and talk with your friends'),
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
    title: L('收集远行物资', 'Gathering Supplies'),
    goal: L('去杂货铺、市场、修理厂收集远行物资（3 处）', 'Collect travel supplies from the grocery, market, and repair shop (3 places)'),
    startsAfter: 'ch1_rooftop'
  },
  {
    id: 'ch2_rooftop',
    chapter: 'ch2',
    title: L('屋顶雨夜', 'Rainy Rooftop Night'),
    goal: L('上屋顶，看看大家', 'Head to the rooftop and check on everyone'),
    startsAfter: 'ch2_supplies'
    // 章节收尾：Maya+Noah 屋顶对话 → 丝滑转场推进 ch3 + 倒计时 4→3
  },

  // 第三章任务链：办理出城通行材料
  // 出场：Elias + Maya（Noah/Leo 下线）
  // 跨章印记：A1 → Elias 态度温和提供加急便利
  {
    id: 'ch3_pass',
    chapter: 'ch3',
    title: L('办理出城通行材料', 'Travel Papers'),
    goal: L('去市政厅办理出城通行材料（Elias 在场）', 'Go to the town hall to process the travel papers (Elias is present)'),
    startsAfter: 'ch2_rooftop'
  },
  {
    id: 'ch3_rooftop',
    chapter: 'ch3',
    title: L('屋顶抉择', 'Rooftop Decision'),
    goal: L('上屋顶，看看大家', 'Head to the rooftop and check on everyone'),
    startsAfter: 'ch3_pass'
    // 章节收尾 → 丝滑转场推进 ch4 + 倒计时 3→2
  },
  // 第三章可选支线：帮 Maya 整理画展（不阻塞主线，ch3_rooftop 仍以 ch3_pass 为前置）
  // 体现"两难抉择"中与 Maya 的联结，完成后影响结局细节
  {
    id: 'ch3_maya_help',
    chapter: 'ch3',
    title: L('帮 Maya 整理画展', 'Help Maya Set Up the Exhibition'),
    goal: L('搬画架、找画册，帮 Maya 准备画展（可选支线）', 'Move easels and find catalogs to help Maya prepare the exhibition (optional side story)'),
    startsAfter: 'ch3_pass'
  },

  // 第四章任务链：北边成为枷锁
  // 出场：Noah + Leo（Elias/Maya 下线）
  // 开场台词读取前三章全部印记，主线三选一产生 ch4 印记
  {
    id: 'ch4_organize',
    chapter: 'ch4',
    title: L('整理回忆', 'Sorting Memories'),
    goal: L('在老街区整理物资，和 Noah、Leo 交谈', 'Sort supplies in the old district; talk with Noah and Leo'),
    startsAfter: 'ch3_rooftop'
  },
  {
    id: 'ch4_rooftop',
    chapter: 'ch4',
    title: L('最终抉择', 'The Final Choice'),
    goal: L('上屋顶，做出最终选择', 'Head to the rooftop and make your final choice'),
    startsAfter: 'ch4_organize'
    // 章节收尾 → 四选一直接锁定结局 → 丝滑转场推进 epilogue
  },
  // 第四章可选支线：重走老街的承诺（不阻塞主线）
  // 探访三处回忆之地，呼应"北上成为枷锁"中对过往的回望
  {
    id: 'ch4_memory_walk',
    chapter: 'ch4',
    title: L('重走老街的承诺', 'Promises of the Old Streets'),
    goal: L('探访合照墙、Noah 的录音机、Leo 的老街角（可选支线）', 'Visit the group-photo wall, Noah\'s recorder, and Leo\'s old corner (optional side story)'),
    startsAfter: 'ch4_organize'
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
