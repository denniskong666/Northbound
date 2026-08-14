// 简易国际化系统：支持中文/英语切换
// 语言偏好持久化到 localStorage

export type Lang = 'zh' | 'en';

const LANG_KEY = 'northbound_lang';

const TRANSLATIONS: Record<string, { zh: string; en: string }> = {
  // 标题界面
  title:        { zh: '向北',           en: 'Northbound' },
  subtitle:     { zh: 'N O R T H B O U N D', en: 'N O R T H B O U N D' },
  quote:        { zh: '设计人生，不是死守早年的计划，\n而是敢于在中途重新选择。',
                  en: 'Designing life is not clinging to early plans,\nbut daring to choose anew midway.' },
  newGame:      { zh: '新的游戏',        en: 'New Game' },
  continueGame: { zh: '继续游戏',        en: 'Continue' },
  controls:     { zh: 'WASD 移动   ·   E 交互   ·   空格 推进对话',
                  en: 'WASD Move  ·  E Interact  ·  Space Advance' },
  langLabel:    { zh: '语言',            en: 'Language' },
  noSave:       { zh: '（暂无存档）',     en: '(No save data)' },

  // 章节标签
  chapter:      { zh: '第一章',          en: 'Chapter 1' },
  chapter2:     { zh: '第二章',          en: 'Chapter 2' },
  chapter3:     { zh: '第三章',          en: 'Chapter 3' },
  chapter4:     { zh: '第四章',          en: 'Chapter 4' },
  finale:       { zh: '终章',            en: 'Finale' },
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
