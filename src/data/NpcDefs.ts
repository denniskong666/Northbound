// 人物框架：人物档案（NpcProfile）+ 场景位置（NpcPlacement）+ 人物关系模型（NPC_RELATIONS）
// 数据来源：文档第六节（人物设定）与第七节（人物关系模型）
// - NpcProfile：人物固有档案（不变），统一管理，供对话/任务/结局引用
// - NpcPlacement：场景相关位置（每个场景各自定义），从档案取 name/textureKey
// 注：主角杰米由玩家操控，非 NPC；其档案见 GameConfig.PLAYER_NAME 与文档 6.1

import { Direction } from '../config/GameConfig';

// —— NPC 标识 ——
export type NpcId = 'elias' | 'maya' | 'noah' | 'leo';

// —— 人物档案（文档第六节）——
export interface NpcProfile {
  id: NpcId;
  fullName: string;       // 全名
  name: string;           // 简称（名牌/对话 speaker 用）
  age: number;            // 年龄
  archetype: string;      // 角色定位
  surface: string;        // 表层性格
  strengths: string;      // 长处
  flaws: string;          // 缺点
  innerDrive: string;     // 内心诉求/恐惧/矛盾
  background: string;     // 背景
  coreLine: string;       // 核心台词/对话片段
  textureKey: string;     // 纹理 key（与 PlaceholderArt 一致）
}

export const NPC_PROFILES: Record<NpcId, NpcProfile> = {
  // —— 6.2 伊莱亚斯·韦尔——旧日约定的坚守者 ——
  elias: {
    id: 'elias',
    fullName: '伊莱亚斯·韦尔',
    name: '伊莱亚斯',
    age: 20,
    archetype: '旧日约定的坚守者',
    surface: '富有魅力、务实、护短、行事果决',
    strengths: '勤恳踏实，不会抛下身边的人',
    flaws: '把改变视作背叛，把自己的关怀当成替他人做决定的权限',
    innerDrive: '恐惧：如果北上之旅落空，他过往所有牺牲连同自身存在意义都会化为乌有',
    background: '数年前哥哥离开格雷布里奇，偶尔寄回讲述别处生活的明信片。伊莱亚斯把这些只言片语拼凑成一份奔赴远方的救赎约定，多年来攒钱、搜集零件、筹划远行。他珍视这群朋友，也正因如此，他施加的压力才显得真实可信。认定杰米是唯一不会抛下自己的人。',
    coreLine: '我没有强迫任何人。我只是提醒他们，当初说过想要成为什么样的人。',
    textureKey: 'elias'
  },

  // —— 6.3 陈玛雅——敢于接纳自我改变的人 ——
  maya: {
    id: 'maya',
    fullName: '陈玛雅',
    name: '玛雅',
    age: 18,
    archetype: '敢于接纳自我改变的人',
    surface: '洞察力强，说话自带冷幽默，看上去十分自信',
    strengths: '总能察觉到其他人刻意回避的世事变迁',
    flaws: '害怕面对失败，于是假装那些人生机遇对自己无关紧要',
    innerDrive: '向往成为艺术家；曾以为搞艺术必须远走高飞，本地一场艺术展览为她铺开另一条出路',
    background: '十二岁那年画过一幅画，画里五个好友一同驱车向北。伊莱亚斯把画妥善保存当作约定的凭证，可玛雅渐渐心生抵触——这幅画把她永远禁锢在了年少的自己。',
    coreLine: '玛雅："那是我十二岁画的。" 伊莱亚斯："那画的是我们所有人共同的向往。" 玛雅："不是。那画的，是我以为能留住我们所有人的办法。"',
    textureKey: 'maya'
  },

  // —— 6.4 诺亚·韦尔——必须说出心里话的人 ——
  noah: {
    id: 'noah',
    fullName: '诺亚·韦尔',
    name: '诺亚',
    age: 17,
    archetype: '必须说出心里话的人',
    surface: '心思缜密、待人谦和，动手能力出众',
    strengths: '耐心、做事稳妥可靠',
    flaws: '错把沉默当成相安无事',
    innerDrive: '家庭期许留在家族电子铺；个人热爱是声音设计、实地录音、社区广播',
    background: '伊莱亚斯的表弟。这层亲属关系让北上计划不止是朋友约定，更带上家族意味。他需要同时挣脱父亲的安排与伊莱亚斯的规划，才能走出属于自己的道路。',
    coreLine: '我父亲为我规划好了人生，伊莱亚斯也为我规划好了人生。说来讽刺，从他们口中说出时，每一份规划，听起来都像是责任。',
    textureKey: 'noah'
  },

  // —— 6.5 利奥·阿尔瓦雷斯——坦然直面内心羁绊的人 ——
  leo: {
    id: 'leo',
    fullName: '利奥·阿尔瓦雷斯',
    name: '利奥',
    age: 18,
    archetype: '坦然直面内心羁绊的人',
    surface: '擅长社交、风趣幽默，共情能力强',
    strengths: '擅长缓和气氛，会用实际行动照顾身边的人',
    flaws: '习惯用玩笑掩饰内心，不敢展露真实脆弱',
    innerDrive: '隐秘的牵挂：要照料祖母，也牵挂露丝餐厅和店里的熟客',
    background: '嘴上总嚷嚷着要离开此地，因为他害怕——倘若流露出想要留下的念头，就会被视作懦弱。',
    coreLine: '杰米："你以前说过你讨厌这个地方。" 利奥："我讨厌的，是这片土地带给人的遭遇。" 杰米："这两回事并不一样。" 利奥："是啊，我花了好久才想明白。"',
    textureKey: 'leo'
  }
};

export function getNpcProfile(id: NpcId): NpcProfile {
  return NPC_PROFILES[id];
}

// —— 场景位置（场景相关，各场景自行定义）——
export interface NpcPlacement {
  id: NpcId;
  tileX: number;
  tileY: number;
  facing: Direction;
  label?: string;          // 交互提示标签，缺省用"和 XX 说话"
}

// 老街区 4 人位置（避开墙体与既有 POI）
export const OLD_DISTRICT_NPCS: NpcPlacement[] = [
  { id: 'elias', tileX: 5,  tileY: 7,  facing: 'down', label: '和伊莱亚斯说话' },
  { id: 'maya',  tileX: 19, tileY: 7,  facing: 'down', label: '和玛雅说话' },
  { id: 'noah',  tileX: 8,  tileY: 11, facing: 'up',   label: '和诺亚说话' },
  { id: 'leo',   tileX: 16, tileY: 12, facing: 'up',   label: '和利奥说话' }
];

// 屋顶第一章结尾位置（任务4清点物资时，四人齐聚天台）
export const ROOFTOP_CH1_NPCS: NpcPlacement[] = [
  { id: 'elias', tileX: 4, tileY: 3, facing: 'down', label: '和伊莱亚斯说话' },
  { id: 'maya',  tileX: 8, tileY: 3, facing: 'down', label: '和玛雅说话' },
  { id: 'noah',  tileX: 3, tileY: 4, facing: 'up',   label: '和诺亚说话' },
  { id: 'leo',   tileX: 9, tileY: 4, facing: 'up',   label: '和利奥说话' }
];

// —— 人物关系模型（文档第七节）——
// 杰米在关系图中作为 'jamie' 出现（非 NPC）
export type CharacterId = NpcId | 'jamie';

export interface Relation {
  pair: [CharacterId, CharacterId];
  desc: string;
}

export const NPC_RELATIONS: Relation[] = [
  { pair: ['jamie', 'elias'], desc: '感恩与依赖，逐步演变成关于人生归属权的拉扯。' },
  { pair: ['elias', 'maya'],  desc: '曾经团体里最富于想象力的两个人，如今却产生最尖锐的观念冲突。' },
  { pair: ['elias', 'noah'],  desc: '伊莱亚斯看重诺亚的能力，却把诺亚的顺从误当作真心认同。' },
  { pair: ['elias', 'leo'],   desc: '玩笑话掩盖着一个心知肚明的事实——利奥并不想要离开家乡。' },
  { pair: ['maya', 'noah'],   desc: '二人不必当众言说，就已经默默察觉到彼此身上发生的改变。' },
  { pair: ['maya', 'leo'],    desc: '玛雅看穿利奥故作洒脱的伪装，但选择留给利奥自己说出真心话的空间。' },
  { pair: ['jamie', 'maya'],  desc: '通过可选支线与互相冲突的任务，决定杰米真正读懂谁。' },
  { pair: ['jamie', 'noah'],  desc: '通过可选支线与互相冲突的任务，决定杰米真正读懂谁。' },
  { pair: ['jamie', 'leo'],   desc: '通过可选支线与互相冲突的任务，决定杰米真正读懂谁。' }
];
