// 人物框架：人物档案（NpcProfile）+ 场景位置（NpcPlacement）+ 人物关系模型（NPC_RELATIONS）
// 数据来源：文档第六节（人物设定）与第七节（人物关系模型）
// - NpcProfile：人物固有档案（不变），统一管理，供对话/任务/结局引用
// - NpcPlacement：场景相关位置（每个场景各自定义），从档案取 name/textureKey
// 注：主角杰米由玩家操控，非 NPC；其档案见 GameConfig.PLAYER_NAME 与文档 6.1

import { Direction } from '../config/GameConfig';
import { L, t } from '../systems/I18n';

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
    fullName: L('伊莱亚斯·韦尔', 'Elias Vale'),
    name: t('npc_elias'),
    age: 20,
    archetype: L('旧日约定的坚守者', 'Guardian of Old Promises'),
    surface: L('富有魅力、务实、护短、行事果决', 'Charming, pragmatic, protective, decisive'),
    strengths: L('勤恳踏实，不会抛下身边的人', 'Diligent and steadfast; never abandons his people'),
    flaws: L('把改变视作背叛，把自己的关怀当成替他人做决定的权限', 'Sees change as betrayal; mistakes his care for the right to decide for others'),
    innerDrive: L('恐惧：如果北上之旅落空，他过往所有牺牲连同自身存在意义都会化为乌有', 'Fear: if the northbound journey comes to nothing, all his past sacrifices—and his very meaning—will be for nothing'),
    background: L('数年前哥哥离开格雷布里奇，偶尔寄回讲述别处生活的明信片。伊莱亚斯把这些只言片语拼凑成一份奔赴远方的救赎约定，多年来攒钱、搜集零件、筹划远行。他珍视这群朋友，也正因如此，他施加的压力才显得真实可信。认定杰米是唯一不会抛下自己的人。', 'Years ago his older brother left Greybridge, occasionally sending postcards describing life elsewhere. Elias pieced these fragments into a redemptive pact to journey far away, saving money, gathering parts, and planning the trip for years. He treasures these friends—which is exactly why the pressure he applies feels so genuine. He is convinced Jamie is the one person who will never leave him behind.'),
    coreLine: L('我没有强迫任何人。我只是提醒他们，当初说过想要成为什么样的人。', 'I never forced anyone. I only reminded them of who they said they wanted to become.'),
    textureKey: 'elias'
  },

  // —— 6.3 陈玛雅——敢于接纳自我改变的人 ——
  maya: {
    id: 'maya',
    fullName: L('陈玛雅', 'Maya Chen'),
    name: t('npc_maya'),
    age: 18,
    archetype: L('敢于接纳自我改变的人', 'One who dares to embrace self-change'),
    surface: L('洞察力强，说话自带冷幽默，看上去十分自信', 'Perceptive, with a dry wit; appears wholly self-assured'),
    strengths: L('总能察觉到其他人刻意回避的世事变迁', 'Always notices the changes others deliberately avoid'),
    flaws: L('害怕面对失败，于是假装那些人生机遇对自己无关紧要', 'Fears failure, so she pretends life\'s opportunities don\'t matter to her'),
    innerDrive: L('向往成为艺术家；曾以为搞艺术必须远走高飞，本地一场艺术展览为她铺开另一条出路', 'Yearns to be an artist; once believed art required fleeing far away, until a local exhibition revealed another path'),
    background: L('十二岁那年画过一幅画，画里五个好友一同驱车向北。伊莱亚斯把画妥善保存当作约定的凭证，可玛雅渐渐心生抵触——这幅画把她永远禁锢在了年少的自己。', 'At twelve she painted a picture of five friends driving north together. Elias kept the painting as proof of their pact, but Maya grew to resent it—the image trapped her forever as her younger self.'),
    coreLine: L('玛雅："那是我十二岁画的。" 伊莱亚斯："那画的是我们所有人共同的向往。" 玛雅："不是。那画的，是我以为能留住我们所有人的办法。"', 'Maya: "I painted that when I was twelve." Elias: "It depicts what we all yearn for." Maya: "No. It depicts what I thought could keep us all together."'),
    textureKey: 'maya'
  },

  // —— 6.4 诺亚·韦尔——必须说出心里话的人 ——
  noah: {
    id: 'noah',
    fullName: L('诺亚·韦尔', 'Noah Vale'),
    name: t('npc_noah'),
    age: 17,
    archetype: L('必须说出心里话的人', 'One who must speak his heart'),
    surface: L('心思缜密、待人谦和，动手能力出众', 'Meticulous, gentle with others, exceptionally hands-on'),
    strengths: L('耐心、做事稳妥可靠', 'Patient, steady, and reliable'),
    flaws: L('错把沉默当成相安无事', 'Mistakes silence for harmony'),
    innerDrive: L('家庭期许留在家族电子铺；个人热爱是声音设计、实地录音、社区广播', 'Family expects him to stay at the family electronics shop; his passion is sound design, field recording, and community radio'),
    background: L('伊莱亚斯的表弟。这层亲属关系让北上计划不止是朋友约定，更带上家族意味。他需要同时挣脱父亲的安排与伊莱亚斯的规划，才能走出属于自己的道路。', "Elias's cousin. This kinship makes the northbound plan more than a friends' pact—it carries family weight. He must break free of both his father's arrangements and Elias's plans to walk a path truly his own."),
    coreLine: L('我父亲为我规划好了人生，伊莱亚斯也为我规划好了人生。说来讽刺，从他们口中说出时，每一份规划，听起来都像是责任。', 'My father planned out my life, and Elias planned out my life too. Ironically, from their mouths, every plan sounds like a duty.'),
    textureKey: 'noah'
  },

  // —— 6.5 利奥·阿尔瓦雷斯——坦然直面内心羁绊的人 ——
  leo: {
    id: 'leo',
    fullName: L('利奥·阿尔瓦雷斯', 'Leo Alvarez'),
    name: t('npc_leo'),
    age: 18,
    archetype: L('坦然直面内心羁绊的人', 'One who faces his inner ties honestly'),
    surface: L('擅长社交、风趣幽默，共情能力强', 'Sociable, witty, deeply empathetic'),
    strengths: L('擅长缓和气氛，会用实际行动照顾身边的人', 'Eases tensions; cares for others through concrete actions'),
    flaws: L('习惯用玩笑掩饰内心，不敢展露真实脆弱', 'Hides behind humor, afraid to show real vulnerability'),
    innerDrive: L('隐秘的牵挂：要照料祖母，也牵挂露丝餐厅和店里的熟客', 'A hidden tie: caring for his grandmother, and for Ruth\'s diner and its regulars'),
    background: L('嘴上总嚷嚷着要离开此地，因为他害怕——倘若流露出想要留下的念头，就会被视作懦弱。', 'He keeps saying he wants to leave this place, because he fears that if he ever showed he wanted to stay, he would be seen as a coward.'),
    coreLine: L('杰米："你以前说过你讨厌这个地方。" 利奥："我讨厌的，是这片土地带给人的遭遇。" 杰米："这两回事并不一样。" 利奥："是啊，我花了好久才想明白。"', 'Jamie: "You said you hated this place." Leo: "What I hate is what this land does to people." Jamie: "Those are two different things." Leo: "Yeah. Took me a long time to figure that out."'),
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
  { id: 'elias', tileX: 5,  tileY: 7,  facing: 'down', label: t('talk_to_elias') },
  { id: 'maya',  tileX: 19, tileY: 7,  facing: 'down', label: t('talk_to_maya') },
  { id: 'noah',  tileX: 8,  tileY: 11, facing: 'up',   label: t('talk_to_noah') },
  { id: 'leo',   tileX: 16, tileY: 12, facing: 'up',   label: t('talk_to_leo') }
];

// 屋顶第一章结尾位置（任务4清点物资时，四人齐聚天台）
export const ROOFTOP_CH1_NPCS: NpcPlacement[] = [
  { id: 'elias', tileX: 4, tileY: 3, facing: 'down', label: t('talk_to_elias') },
  { id: 'maya', tileX: 8, tileY: 3, facing: 'down', label: t('talk_to_maya') },
  { id: 'noah', tileX: 3, tileY: 4, facing: 'up',   label: t('talk_to_noah') },
  { id: 'leo',   tileX: 9, tileY: 4, facing: 'up',   label: t('talk_to_leo') }
];

// —— 人物关系模型（文档第七节）——
// 杰米在关系图中作为 'jamie' 出现（非 NPC）
export type CharacterId = NpcId | 'jamie';

export interface Relation {
  pair: [CharacterId, CharacterId];
  desc: string;
}

export const NPC_RELATIONS: Relation[] = [
  { pair: ['jamie', 'elias'], desc: L('感恩与依赖，逐步演变成关于人生归属权的拉扯。', 'Gratitude and dependence, gradually evolving into a tug-of-war over who owns Jamie\'s life.') },
  { pair: ['elias', 'maya'],  desc: L('曾经团体里最富于想象力的两个人，如今却产生最尖锐的观念冲突。', 'Once the two most imaginative members of the group, now the source of its sharpest conflict of visions.') },
  { pair: ['elias', 'noah'],  desc: L('伊莱亚斯看重诺亚的能力，却把诺亚的顺从误当作真心认同。', 'Elias values Noah\'s ability, but mistakes his compliance for genuine agreement.') },
  { pair: ['elias', 'leo'],   desc: L('玩笑话掩盖着一个心知肚明的事实——利奥并不想要离开家乡。', 'The banter hides a fact they both know—Leo does not want to leave home.') },
  { pair: ['maya', 'noah'],   desc: L('二人不必当众言说，就已经默默察觉到彼此身上发生的改变。', 'Without a word spoken aloud, they have silently noticed the changes in each other.') },
  { pair: ['maya', 'leo'],    desc: L('玛雅看穿利奥故作洒脱的伪装，但选择留给利奥自己说出真心话的空间。', 'Maya sees through Leo\'s carefree mask, but chooses to leave him space to say the truth himself.') },
  { pair: ['jamie', 'maya'],  desc: L('通过可选支线与互相冲突的任务，决定杰米真正读懂谁。', 'Through optional side stories and conflicting tasks, the player decides whom Jamie truly comes to understand.') },
  { pair: ['jamie', 'noah'],  desc: L('通过可选支线与互相冲突的任务，决定杰米真正读懂谁。', 'Through optional side stories and conflicting tasks, the player decides whom Jamie truly comes to understand.') },
  { pair: ['jamie', 'leo'],   desc: L('通过可选支线与互相冲突的任务，决定杰米真正读懂谁。', 'Through optional side stories and conflicting tasks, the player decides whom Jamie truly comes to understand.') }
];
