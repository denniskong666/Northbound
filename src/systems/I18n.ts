// 国际化系统：支持中文/英语切换
// 语言偏好持久化到 localStorage
// 切换语言后重启游戏，所有内容（对话、名字、物品提示等）随之切换

export type Lang = 'zh' | 'en';

const LANG_KEY = 'northbound_lang';

// ============================================================
// UI 文本翻译字典
// ============================================================
const TRANSLATIONS: Record<string, { zh: string; en: string }> = {
  // —— 标题界面 ——
  title:          { zh: '向北',           en: 'Northbound' },
  subtitle:       { zh: 'N O R T H B O U N D', en: 'N O R T H B O U N D' },
  quote:          { zh: '设计人生，不是死守早年的计划，\n而是敢于在中途重新选择。',
                    en: 'Designing life is not clinging to early plans,\nbut daring to choose anew midway.' },
  newGame:        { zh: '新的游戏',        en: 'New Game' },
  continueGame:   { zh: '继续游戏',        en: 'Continue' },
  controls:       { zh: 'WASD 移动   ·   Shift 奔跑   ·   E 交互   ·   空格/点击 推进对话   ·   ESC 退出',
                    en: 'WASD Move  ·  Shift Run  ·  E Interact  ·  Space/Click Advance  ·  ESC Quit' },
  langLabel:      { zh: '语言',            en: 'Language' },
  noSave:         { zh: '（暂无存档）',     en: '(No save data)' },

  // —— 章节标签 ——
  chapter0:       { zh: '序章',            en: 'Prologue' },
  chapter1:       { zh: '第一章',          en: 'Chapter 1' },
  chapter2:       { zh: '第二章',          en: 'Chapter 2' },
  chapter3:       { zh: '第三章',          en: 'Chapter 3' },
  chapter4:       { zh: '第四章',          en: 'Chapter 4' },
  finale:         { zh: '终章',            en: 'Finale' },

  // —— 章节标题 ——
  ch0_title:      { zh: '序章 · 北方的召唤',     en: 'Prologue · The Call of the North' },
  ch1_title:      { zh: '第一章 · 既定计划',     en: 'Chapter 1 · The Plan' },
  ch2_title:      { zh: '第二章 · 裂痕显现',     en: 'Chapter 2 · Cracks Appear' },
  ch3_title:      { zh: '第三章 · 两难抉择',     en: 'Chapter 3 · The Dilemma' },
  ch4_title:      { zh: '第四章 · 北上成为枷锁', en: 'Chapter 4 · The Burden of Northbound' },
  epilogue_title:  { zh: '终章 · 你来吗？',       en: 'Finale · Will You Come?' },

  // —— 章节副标题 ——
  ch0_sub:    { zh: '全员向往北方，北方是希望与崭新人生的象征。', en: 'Everyone yearns for the North — a symbol of hope and a new life.' },
  ch1_sub:    { zh: '攒路费途中，第一道裂痕悄然出现。', en: 'While saving for the journey, the first crack quietly appears.' },
  ch2_sub:    { zh: '朋友们的人生开始出现分歧。', en: 'Friends\' lives begin to diverge.' },
  ch3_sub:    { zh: '同一时段刷新互斥任务，只能选其一。', en: 'Conflicting tasks arise — you can only choose one.' },
  ch4_sub:    { zh: '北边从希望变成所有人的心理负担。', en: 'The North becomes a burden for everyone.' },
  epilogue_sub: { zh: '旧车已修好，你来吗？', en: 'The old car is ready. Will you come?' },

  // —— 通用 UI 提示 ——
  press_e:          { zh: '按 E — ',          en: 'Press E — ' },
  hint_continue:    { zh: '空格/点击 继续',    en: 'Space/Click to continue' },
  hint_choose:      { zh: '↑↓/鼠标 选择  回车/点击 确认', en: '↑↓/Mouse to select  Enter/Click to confirm' },
  hint_close:       { zh: '按 E / 空格 / 点击 关闭', en: 'Press E / Space / Click to close' },
  task_prefix:      { zh: '【任务】',          en: '[Quest] ' },
  task_complete:    { zh: '任务完成 · ',       en: 'Quest Complete · ' },
  save_reset:       { zh: '存档已重置。',      en: 'Save data reset.' },
  already_finale:   { zh: '已是终章。',        en: 'Already at the finale.' },
  simple_choice_hint: { zh: '↑↓ 选择  ·  回车/点击 确认', en: '↑↓ Select  ·  Enter/Click Confirm' },

  // —— 数北方灯火小游戏 ——
  lightgame_title:    { zh: '— 数 北 方 的 灯 —',     en: '— Count the Northern Lights —' },
  lightgame_subtitle: { zh: '用鼠标点击浮现的灯火 · 20 秒内点亮 8 盏', en: 'Click the lights · Light up 8 in 20 seconds' },
  lightgame_howto:    { zh: '▼ 鼠标点击光点即可点亮 ▼', en: '▼ Click the lights to ignite them ▼' },
  lightgame_progress: { zh: '灯火',   en: 'Lights' },
  lightgame_esc:      { zh: 'ESC 放弃小游戏', en: 'ESC to quit' },
  lightgame_result_win:  { zh: '✨ 点亮了',  en: '✨ Lit up' },
  lightgame_result_lose: { zh: '点了',      en: 'Lit' },
  lightgame_lights:      { zh: '盏灯火',    en: 'lights' },
  lightgame_seconds:     { zh: '盏灯火！✨', en: 'lights! ✨' },

  // —— NPC 名字 ——
  npc_elias:  { zh: '伊莱亚斯', en: 'Elias' },
  npc_maya:   { zh: '玛雅',     en: 'Maya' },
  npc_noah:   { zh: '诺亚',     en: 'Noah' },
  npc_leo:    { zh: '利奥',     en: 'Leo' },
  npc_jamie:  { zh: '杰米',     en: 'Jamie' },

  // —— NPC 交互标签 ——
  talk_to_elias: { zh: '和伊莱亚斯说话', en: 'Talk to Elias' },
  talk_to_maya:  { zh: '和玛雅说话',     en: 'Talk to Maya' },
  talk_to_noah:  { zh: '和诺亚说话',     en: 'Talk to Noah' },
  talk_to_leo:   { zh: '和利奥说话',     en: 'Talk to Leo' },
  talk_to:       { zh: '和',  en: 'Talk to ' },
  talk_suffix:   { zh: '说话', en: '' },

  // —— 结局标签 ——
  ending_go_north:      { zh: '同赴远方',     en: 'Journey Together' },
  ending_return_home:   { zh: '故土相守',     en: 'Staying Home' },
  ending_unknown_path:  { zh: '独行新路',     en: 'A New Path Alone' },
  ending_pause_journey: { zh: '暂缓前行',     en: 'Pausing the Journey' },
  ending_with_maya:     { zh: '相伴同行 · 玛雅', en: 'Together · Maya' },
  ending_with_noah:     { zh: '相伴同行 · 诺亚', en: 'Together · Noah' },
  ending_with_leo:      { zh: '相伴同行 · 利奥', en: 'Together · Leo' },

  // —— 携带物品标签 ——
  carry_group_photo:    { zh: '团体合照',     en: 'Group Photo' },
  carry_blank_notebook: { zh: '空白笔记本',   en: 'Blank Notebook' },
  carry_house_key:      { zh: '家门钥匙',     en: 'House Key' },
  carry_old_map:        { zh: '旧地图',       en: 'Old Map' },

  // —— 后备箱物品标签 ——
  trunk_tools:          { zh: '维修工具',     en: 'Repair Tools' },
  trunk_memory_box:    { zh: '童年纪念盒',   en: 'Childhood Memory Box' },
  trunk_maya_painting: { zh: '玛雅的画作',   en: "Maya's Painting" },
  trunk_noah_recorder: { zh: '诺亚的录音机', en: "Noah's Recorder" },
  trunk_leo_bag:       { zh: '利奥的旅行包', en: "Leo's Travel Bag" },

  // —— 结局前置条件描述 ——
  precond_go_north:      { zh: '前置条件：A 系列印记占主导，Elias 全局好感最高', en: 'Prerequisite: A-marks dominant, highest bond with Elias' },
  precond_return_home:   { zh: '前置条件：C 系列印记占主导，自我倾向全局最高', en: 'Prerequisite: C-marks dominant, highest self-agency' },
  precond_unknown_path:  { zh: '前置条件：全程大量中立 B 印记，两边好感差距小', en: 'Prerequisite: Mostly neutral B-marks, balanced bonds' },
  precond_pause_journey: { zh: '前置条件：印记两极反复摇摆，一会偏向计划、一会偏向自我', en: 'Prerequisite: Marks swing between plan and self' },

  // —— 调试面板 ——
  dbg_protagonist:  { zh: '主角',     en: 'Player' },
  dbg_scene:        { zh: '场景',     en: 'Scene' },
  dbg_chapter:      { zh: '章节',     en: 'Chapter' },
  dbg_countdown:    { zh: '倒计时',   en: 'Countdown' },
  dbg_days:         { zh: '天',       en: 'days' },
  dbg_commitment:   { zh: '信守约定', en: 'Commitment' },
  dbg_rootedness:   { zh: '联结故土', en: 'Rootedness' },
  dbg_agency:       { zh: '自我主导', en: 'Agency' },
  dbg_bond:         { zh: '羁绊',     en: 'Bond' },
  dbg_highest:      { zh: '最高',     en: 'Top' },
  dbg_resolved:     { zh: '已选互斥', en: 'Resolved' },
  dbg_carry:        { zh: '携带',     en: 'Carrying' },
  dbg_none:         { zh: '无',       en: 'None' },
  dbg_flags:        { zh: '叙事flag', en: 'Flags' },
  dbg_ending:        { zh: '结局',     en: 'Ending' },
  dbg_undecided:    { zh: '未决定',   en: 'Undecided' },
  dbg_next_chapter: { zh: 'T=下一章  R=重置  P=开关面板  ESC=退出', en: 'T=Next Chapter  R=Reset  P=Toggle Panel  ESC=Quit' },

  // —— 调试面板羁绊名 ——
  dbg_bond_maya: { zh: '玛雅', en: 'Maya' },
  dbg_bond_noah: { zh: '诺亚', en: 'Noah' },
  dbg_bond_leo:  { zh: '利奥', en: 'Leo' },

  // —— 通用 ——
  none_label: { zh: '无', en: 'None' },
};

let currentLang: Lang = 'zh';

// 从 localStorage 加载语言偏好
(function loadLang(): void {
  try {
    const saved = localStorage.getItem(LANG_KEY);
    if (saved === 'zh' || saved === 'en') currentLang = saved;
  } catch { /* ignore */ }
})();

export function getLang(): Lang {
  return currentLang;
}

export function setLang(lang: Lang): void {
  currentLang = lang;
  try { localStorage.setItem(LANG_KEY, lang); } catch { /* ignore */ }
}

export function toggleLang(): Lang {
  setLang(currentLang === 'zh' ? 'en' : 'zh');
  return currentLang;
}

// 翻译键 → 当前语言文本
export function t(key: string): string {
  const entry = TRANSLATIONS[key];
  if (!entry) return key;
  return entry[currentLang];
}

// 双语文本辅助：根据当前语言选择
export function L(zh: string, en: string): string {
  return currentLang === 'zh' ? zh : en;
}
