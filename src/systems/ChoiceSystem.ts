// 选择系统：互斥任务组（任务对子 A/B/C）
// 设计契合企划"行动代替文字选择"——玩家进入某分支触发区域即锁定该选项，
// 另一分支立即更新为"错过该事件"状态。全程不弹文字选项框。
// 文档第十五节：一旦确认选择，另一分支不会凭空消失，而是更新为合乎逻辑的"错过该事件"状态。

import { GameState, ChoiceEffects } from '../state/GameState';

export interface ChoiceOption {
  id: string;                          // 选项唯一 id
  label: string;                       // 显示用标签
  effects?: ChoiceEffects;             // 选中后应用的影响（倾向/羁绊/叙事事实等）
  onResolve?: () => void;              // 选中后的回调（如播放对白）
}

export interface MutualChoice {
  id: string;                          // 这一组互斥任务的 id
  prompt?: string;                     // 可选提示语
  options: ChoiceOption[];             // 通常两个，互斥
}

export class ChoiceSystem {
  private groups = new Map<string, MutualChoice>();
  private chosen = new Map<string, string>();     // choiceId -> optionId
  private lockListeners = new Map<string, (lockedOptionId: string) => void>();

  register(choice: MutualChoice): void {
    if (!this.groups.has(choice.id)) this.groups.set(choice.id, choice);
  }

  // 当玩家行动选择了某个 option；成功记录返回 true
  resolve(choiceId: string, optionId: string): boolean {
    const g = this.groups.get(choiceId);
    if (!g) return false;
    const state = GameState.inst;
    if (state.hasResolved(choiceId)) return false;
    const opt = g.options.find(o => o.id === optionId);
    if (!opt) return false;

    state.recordChoice(choiceId, opt.effects ?? {});
    this.chosen.set(choiceId, optionId);
    opt.onResolve?.();
    // 通知场景锁定其它选项（使其消失/转为"错过"状态）
    this.lockListeners.get(choiceId)?.(optionId);
    return true;
  }

  isResolved(choiceId: string): boolean {
    return GameState.inst.hasResolved(choiceId);
  }

  chosenOption(choiceId: string): string | null {
    return this.chosen.get(choiceId) ?? null;
  }

  // 某 option 是否已被锁（组已 resolved 且选中的不是它）
  isOptionLocked(choiceId: string, optionId: string): boolean {
    if (!this.isResolved(choiceId)) return false;
    return this.chosen.get(choiceId) !== optionId;
  }

  // 场景注册"锁定回调"：当某组被解决时，移除或转换其它选项
  onLock(choiceId: string, cb: (lockedOptionId: string) => void): void {
    this.lockListeners.set(choiceId, cb);
    // 若已解决（读档场景），立即用已选 option 触发一次
    if (this.isResolved(choiceId)) {
      const c = this.chosen.get(choiceId);
      if (c) cb(c);
    }
  }
}
