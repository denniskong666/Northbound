// 章节定义与推进逻辑
// 六章：序章·北方的召唤 → 既定计划 → 裂痕显现 → 两难抉择 → 北上成为枷锁 → 终章：你来吗？

export type ChapterId = 'ch0' | 'ch1' | 'ch2' | 'ch3' | 'ch4' | 'epilogue';

export interface ChapterMeta {
  id: ChapterId;
  title: string;   // 章节标题（用于标题卡）
  subtitle: string; // 章节简述（调试/文档用）
}

export const CHAPTERS: ChapterMeta[] = [
  { id: 'ch0',       title: '序章 · 北方的召唤',     subtitle: '全员向往北方，北方是希望与崭新人生的象征。' },
  { id: 'ch1',       title: '第一章 · 既定计划',     subtitle: '攒路费途中，第一道裂痕悄然出现。' },
  { id: 'ch2',       title: '第二章 · 裂痕显现',     subtitle: '朋友们的人生开始出现分歧。' },
  { id: 'ch3',       title: '第三章 · 两难抉择',     subtitle: '同一时段刷新互斥任务，只能选其一。' },
  { id: 'ch4',       title: '第四章 · 北上成为枷锁', subtitle: '北边从希望变成所有人的心理负担。' },
  { id: 'epilogue',  title: '终章 · 你来吗？',       subtitle: '旧车已修好，你来吗？' }
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
