// 章节定义与推进逻辑
// 六章：序章·北方的召唤 → 既定计划 → 裂痕显现 → 两难抉择 → 北上成为枷锁 → 终章：你来吗？

import { t } from '../systems/I18n';

export type ChapterId = 'ch0' | 'ch1' | 'ch2' | 'ch3' | 'ch4' | 'epilogue';

export interface ChapterMeta {
  id: ChapterId;
  title: string;   // 章节标题（用于标题卡）
  subtitle: string; // 章节简述（调试/文档用）
}

export const CHAPTERS: ChapterMeta[] = [
  { id: 'ch0',       title: t('ch0_title'),     subtitle: t('ch0_sub') },
  { id: 'ch1',       title: t('ch1_title'),     subtitle: t('ch1_sub') },
  { id: 'ch2',       title: t('ch2_title'),     subtitle: t('ch2_sub') },
  { id: 'ch3',       title: t('ch3_title'),     subtitle: t('ch3_sub') },
  { id: 'ch4',       title: t('ch4_title'),     subtitle: t('ch4_sub') },
  { id: 'epilogue',  title: t('epilogue_title'), subtitle: t('epilogue_sub') }
];

const ORDER: ChapterId[] = ['ch0', 'ch1', 'ch2', 'ch3', 'ch4', 'epilogue'];

export function nextChapter(c: ChapterId): ChapterId | null {
  const i = ORDER.indexOf(c);
  return i >= 0 && i < ORDER.length - 1 ? ORDER[i + 1] : null;
}

export function chapterMeta(id: ChapterId): ChapterMeta {
  return CHAPTERS.find(c => c.id === id) ?? CHAPTERS[0];
}

export function isFirstChapter(id: ChapterId): boolean {
  return id === ORDER[0];
}
