// 对话数据：4 位伙伴的占位对话（含分支选项与好感影响）
// 仅供程序验证流程，正式台词由外部提供后替换即可
// effects 结构对齐新状态模型：commitment/rootedness/agency + bond{maya,noah,leo}
// 语义对照（文档第十节）：
//   Elias 相关 → commitment（信守约定）
//   Maya 画展/诺亚热爱/利奥留恋 → rootedness 或 agency + 对应 bond
//   质疑约定/接纳未知 → agency（自我主导）

import { DialogueData } from '../systems/DialogueSystem';
import { NpcId } from './NpcDefs';

export const DIALOGUES: Record<NpcId, DialogueData> = {
  // —— 伊莱亚斯 Elias：旧日约定的坚守者 ——
  elias: {
    id: 'elias_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: '伊莱亚斯',
        text: '又见面了。零件攒得差不多了吧？说好的，攒够就一起往北走。',
        next: 'ask'
      },
      ask: {
        speaker: '伊莱亚斯',
        text: '你心里还记着那件事吧？告诉我——我们一定会一起走的，对吗？',
        choices: [
          { label: '当然，我们说好的。',   next: 'confirm', effects: { commitment: 5 } },
          { label: '我还不知道。',         next: 'doubt',  effects: { commitment: -5, agency: 5 } }
        ]
      },
      confirm: {
        speaker: '伊莱亚斯',
        text: '好。记住你今天说的话。北边在等我们所有人。'
      },
      doubt: {
        speaker: '伊莱亚斯',
        text: '……我不知道。这话从你嘴里说出来，比从他们嘴里说出来更重。'
      }
    }
  },

  // —— 玛雅 Maya：敢于接纳自我改变的人 ——
  maya: {
    id: 'maya_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: '玛雅',
        text: '你看这张——我把街角那盏路灯画进去了。颜色好像有点太亮？',
        next: 'ask'
      },
      ask: {
        speaker: '玛雅',
        text: '其实……本地有人问我愿不愿意拿去参展。你说我该去吗？',
        choices: [
          { label: '画得真好，该去。',     next: 'encourage', effects: { rootedness: 5, bond: { maya: 10 } } },
          { label: '别分心，先修车要紧。', next: 'dismiss',   effects: { commitment: 5, bond: { maya: -5 } } }
        ]
      },
      encourage: {
        speaker: '玛雅',
        text: '……谢谢你。我都有点不敢相信，这条街的颜色也能被人看见。'
      },
      dismiss: {
        speaker: '玛雅',
        text: '嗯……也是。修车的事比较急。画展的事，再算吧。'
      }
    }
  },

  // —— 诺亚 Noah：必须说出心里话的人 ——
  noah: {
    id: 'noah_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: '诺亚',
        text: '家里又让我去考那个稳妥的学校。可我最近总在想……我真正想做的是什么。',
        next: 'ask'
      },
      ask: {
        speaker: '诺亚',
        text: '你觉得，是听家人的安排稳当，还是去找自己真正想做的事？',
        choices: [
          { label: '想做什么就去做。', next: 'brave',    effects: { agency: 5, bond: { noah: 10 } } },
          { label: '听家人的更稳妥。', next: 'obedient', effects: { commitment: 3, bond: { noah: -5 } } }
        ]
      },
      brave: {
        speaker: '诺亚',
        text: '……你说得对。总要试一次，才知道自己到底是什么人。'
      },
      obedient: {
        speaker: '诺亚',
        text: '嗯，也许你说得对。先按部就班，别节外生枝。'
      }
    }
  },

  // —— 利奥 Leo：坦然直面内心羁绊的人 ——
  leo: {
    id: 'leo_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: '利奥',
        text: '又在这条街晃。我嘴上老说想走，可每次走到路口又停下来了。',
        next: 'ask'
      },
      ask: {
        speaker: '利奥',
        text: '你说，我是真想走，还是只是舍不得这里？',
        choices: [
          { label: '你舍不得这里。',   next: 'truth', effects: { rootedness: 5, bond: { leo: 10 } } },
          { label: '那就一起走呗。',   next: 'leave', effects: { commitment: 3, bond: { leo: -5 } } }
        ]
      },
      truth: {
        speaker: '利奥',
        text: '……被你看穿了。留下，好像也不算懦弱吧。'
      },
      leave: {
        speaker: '利奥',
        text: '嗯。也许离开一阵子，我才能看清自己到底舍不得什么。'
      }
    }
  }
};

// ============================================================
// NPC 日常对话（剧情对话完成后切换，无好感影响，简短闲聊）
// 通过 flag `npc_<id>_talked` 判断是否已触发剧情对话
// ============================================================
export const DIALOGUES_DAILY: Record<NpcId, DialogueData> = {
  elias: {
    id: 'elias_daily',
    start: 'line',
    nodes: {
      line: {
        speaker: '伊莱亚斯',
        text: '零件的事不急，先把手头的活干完。北边不会跑掉的。'
      }
    }
  },
  maya: {
    id: 'maya_daily',
    start: 'line',
    nodes: {
      line: {
        speaker: '玛雅',
        text: '今天光线不错，回头我画一张这条街的速写给你看。'
      }
    }
  },
  noah: {
    id: 'noah_daily',
    start: 'line',
    nodes: {
      line: {
        speaker: '诺亚',
        text: '录音机昨天又录到一段不错的风声。走了以后，大概会想念这些声音吧。'
      }
    }
  },
  leo: {
    id: 'leo_daily',
    start: 'line',
    nodes: {
      line: {
        speaker: '利奥',
        text: '餐厅今天有新菜。说真的，这地方的吃的，到哪儿都替代不了。'
      }
    }
  }
};

// ============================================================
// 序章剧情对话：全员向往北方，北方=希望与崭新人生
// 出场：Elias、Maya、Noah、Leo（全员在场，氛围欢愉）
// 无剧情印记（序章为纯氛围铺垫，不影响后续分支判定）
// ============================================================

// —— 序章互动物品描述（明信片 / 北方看板 / 愿望墙） ——
// 4 张明信片对应 4 种北方意象
export const CH0_POSTCARD_DESC: Record<'aurora' | 'harbor' | 'mountain' | 'gallery', { title: string; text: string }> = {
  aurora: {
    title: '北方极光·明信片',
    text: '有人从北方寄来的明信片。照片里，绿色和紫色的光带在夜空中翻涌，像神话里才会有的景象。\n背面写着：「这里的天空，每晚都在跳舞。」'
  },
  harbor: {
    title: '黄昏港口·明信片',
    text: '北方新港区的黄昏剪影。高楼窗户亮着成百上千盏灯，海面倒映着金色晚霞。\n角落里有人用钢笔圈出了三个字：「包食宿」。'
  },
  mountain: {
    title: '北方旷野·明信片',
    text: '层层叠叠的蓝灰色山脉，近处是深褐色的旷野。最高的那座山峰还顶着一点未化的白雪。\n背面写着：「趁年轻，去看看山的那一边。」'
  },
  gallery: {
    title: '新港区美术馆·明信片',
    text: '新港区美术馆的外观。外墙是浅米色的，屋顶尖尖的，大门上方挂着「新锐画家征稿中」的小招牌。\n玛雅在旁边用铅笔打了个小星星。'
  }
};

// 北方宣传看板描述
export const CH0_BOARD_DESC = {
  title: '北方联合宣传·公告板',
  text: '街区中央的公告板被一张大海报占满了。\n\n大字写着「— 北 方 —」\n副标题：机会 · 自由 · 新的人生\n\n下方小字：\n· 新港区招工 · 薪资三倍 · 包食宿\n· 新港区美术馆新锐画家征稿 · 有奖金\n\n右下角用绿色马克笔写着：「一起北上 →」'
};

// 愿望墙描述
export const CH0_WISHWALL_DESC = {
  title: '街角愿望板',
  text: '一块钉满便签的旧木板。\n\n绿色便签（诺亚）：逃离家人 做手工！\n黄色便签（伊莱亚斯）：薪资三倍 走出去！\n粉色便签（玛雅）：画极光 办画展！\n蓝色便签（利奥）：看大海 闯天下！\n\n正中央那张白色便签写着「你的愿望？」——走过去，也许可以写下。'
};

// —— 序章·愿望选择小游戏：玩家填写中央空白便签 ——
export type WishType = 'wealth' | 'freedom' | 'art' | 'friends' | 'path';

export const CH0_WISH_OPTIONS: Array<{
  id: WishType;
  label: string;
  toast: string;
  effect: { commitment?: number; agency?: number };
}> = [
  {
    id: 'wealth',
    label: '赚大钱，出人头地',
    toast: '你写下了「赚大钱 出人头地」',
    effect: { commitment: 0.5 }
  },
  {
    id: 'freedom',
    label: '看看世界，自由自在',
    toast: '你写下了「看世界 自由自在」',
    effect: { agency: 0.3 }
  },
  {
    id: 'art',
    label: '做喜欢的事（画画/音乐）',
    toast: '你写下了「画遍山河 办画展」',
    effect: { agency: 0.3 }
  },
  {
    id: 'friends',
    label: '和朋友永远在一起',
    toast: '你写下了「大家一起 永不分开」',
    effect: { commitment: 0.3, agency: 0.2 }
  },
  {
    id: 'path',
    label: '找到属于自己的路',
    toast: '你写下了「找到属于 自己的路」',
    effect: { commitment: 0.2, agency: 0.2 }
  }
];

// —— 序章·数北方灯火小游戏剧情文本 ——
export const CH0_LIGHTGAME_INTRO = {
  title: '街角橱窗·北方灯火展',
  text: 'Elias 在橱窗里贴了一张北方夜空的照片，笑着说：「能看到这些灯火的人，未来都不会差。\n来玩个小游戏——20 秒内点亮 8 盏灯，怎么样？」'
};

export const CH0_LIGHTGAME_WIN = '你点亮了 8 盏北方的灯火。Elias 拍你肩膀：「看，北方在等我们！」';
export const CH0_LIGHTGAME_LOSE = (count: number) =>
  count >= 5 ? `只差一点了！${count} 盏也够亮了。玛雅笑着说：「反正到了北方，我们能看个够。」`
              : `点了 ${count} 盏灯。Leo 摊手：「别急，北方的灯火看一辈子都看不完。」`;

// 序章 NPC 对话：Elias — 兴奋地谈论北方的机会
export const CH0_ELIAS_DIALOGUE: DialogueData = {
  id: 'ch0_elias',
  start: 'greet',
  nodes: {
    greet: {
      speaker: '伊莱亚斯',
      text: '我昨晚又查了一遍路线。沿着北线公路走，三天就能到新港区——那里正在招工，工资是这里的三倍。',
      next: 'excited'
    },
    excited: {
      speaker: '伊莱亚斯',
      text: '我们五个一起走，一起干，用不了一年就能站稳脚跟。这才是我们该过的人生！',
      next: 'ask'
    },
    ask: {
      speaker: '伊莱亚斯',
      text: '你也在期待吧？北方在等我们。',
      choices: [
        { label: '当然，我已经等不及了。', next: 'confirm', effects: { commitment: 1 } },
        { label: '想想就让人激动。',       next: 'confirm', effects: { commitment: 0.5 } }
      ]
    },
    confirm: {
      speaker: '伊莱亚斯',
      text: '那就说好了——我们一起往北走，谁也不许掉队！'
    }
  }
};

// 序章 NPC 对话：Maya — 想去北方画新的色彩
export const CH0_MAYA_DIALOGUE: DialogueData = {
  id: 'ch0_maya',
  start: 'greet',
  nodes: {
    greet: {
      speaker: '玛雅',
      text: '你猜我在画什么？——北方的极光！我在杂志上看过照片，那种颜色这里根本见不到。',
      next: 'dream'
    },
    dream: {
      speaker: '玛雅',
      text: '听说新港区的美术馆正在征集新锐画师。如果我们去了北方，我的画说不定能被挂上去！',
      next: 'ask'
    },
    ask: {
      speaker: '玛雅',
      text: '你说，北方的天空到底是什么颜色的？',
      choices: [
        { label: '一定比这里更辽阔。', next: 'confirm', effects: { bond: { maya: 2 } } },
        { label: '去了就知道了。',     next: 'confirm', effects: { bond: { maya: 1 } } }
      ]
    },
    confirm: {
      speaker: '玛雅',
      text: '哈哈，对！等我们到了北方，我要把所有的颜色都画下来！'
    }
  }
};

// 序章 NPC 对话：Noah — 想去北方逃离家人的安排
export const CH0_NOAH_DIALOGUE: DialogueData = {
  id: 'ch0_noah',
  start: 'greet',
  nodes: {
    greet: {
      speaker: '诺亚',
      text: '我妈昨天又给我报了个「稳妥」的培训班。她觉得我这辈子就该按她画好的路线走。',
      next: 'rebel'
    },
    rebel: {
      speaker: '诺亚',
      text: '但我不想！北方谁也不认识我，我可以重新开始——做手工、学音乐，做什么都行。',
      next: 'ask'
    },
    ask: {
      speaker: '诺亚',
      text: '到了北方，第一件事你想做什么？',
      choices: [
        { label: '先好好看看那座城市。',         next: 'confirm', effects: { bond: { noah: 2 } } },
        { label: '大睡一觉，醒来就是新生活。',   next: 'confirm', effects: { bond: { noah: 1 } } }
      ]
    },
    confirm: {
      speaker: '诺亚',
      text: '哈哈，好！反正到了北方，一切都是新的——连空气都是自由的味道。'
    }
  }
};

// 序章 NPC 对话：Leo — 也向往北方的冒险
export const CH0_LEO_DIALOGUE: DialogueData = {
  id: 'ch0_leo',
  start: 'greet',
  nodes: {
    greet: {
      speaker: '利奥',
      text: '你知道吗，这条街我走了十八年，闭着眼都能数清每块砖。说真的，太闷了。',
      next: 'adventure'
    },
    adventure: {
      speaker: '利奥',
      text: '北方有海、有山、有我们从没见过的东西。趁着年轻，就该出去闯一闯！',
      next: 'ask'
    },
    ask: {
      speaker: '利奥',
      text: '我们从小就说要一起走出去——这次是真的了吧？',
      choices: [
        { label: '这次是真的，我们一起走。', next: 'confirm', effects: { commitment: 0.5, bond: { leo: 2 } } },
        { label: '而且绝不回头。',           next: 'confirm', effects: { commitment: 1, bond: { leo: 1 } } }
      ]
    },
    confirm: {
      speaker: '利奥',
      text: '这才对嘛！老街的日子虽然也不错，但北方才是我们要去的地方！'
    }
  }
};

// 序章屋顶聚会：全员眺望北方，约定一起出发
export const CH0_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch0_rooftop',
  start: 'narration',
  nodes: {
    narration: {
      speaker: '',
      text: '黄昏。五个人挤在屋顶上，面朝北方。远处城市的灯火连成一片，像是在召唤。',
      next: 'elias'
    },
    elias: {
      speaker: '伊莱亚斯',
      text: '看到了吗？那边就是北方。只要攒够路费，我们很快就能出发。',
      next: 'maya'
    },
    maya: {
      speaker: '玛雅',
      text: '我要画下我们出发那天的天空——一定比现在更漂亮。',
      next: 'noah'
    },
    noah: {
      speaker: '诺亚',
      text: '到了北方，第一件事就是把家人的电话拉黑——开玩笑的。大概吧。',
      next: 'leo'
    },
    leo: {
      speaker: '利奥',
      text: '嘿，十八年了，终于要走出这条街了。北方，我们来了！',
      next: 'ask'
    },
    ask: {
      speaker: '伊莱亚斯',
      text: '说好了——我们五个人，一起往北走。谁也不许掉队。',
      choices: [
        { label: '一起走，绝不掉队！', next: 'pact', effects: { commitment: 1 } },
        { label: '北方在等我们。',     next: 'pact', effects: { commitment: 0.5 } }
      ]
    },
    pact: {
      speaker: '',
      text: '五个人在夕阳下击掌。那个瞬间，北方不只是一个方向——它是所有人共同的希望。'
    }
  }
};

// ============================================================
// 第一章剧情对话（双人对话，由场景 POI 触发，非单 NPC 对话）
// 出场：Elias + Leo（第一章结束二人暂时下线，二三章不再登场）
// 数值体系沿用参考文本：+1 / -0.5 / -1（小数累加，GameState 内部支持）
// 语义：Elias 好感由 commitment 体现；Leo 好感由 bond.leo 体现
// ============================================================

// 对话1｜老街区摆摊处：打工攒路费的意义
export const CH1_BOOTH_DIALOGUE: DialogueData = {
  id: 'ch1_booth',
  start: 'e_open',
  nodes: {
    e_open: {
      speaker: '伊莱亚斯',
      text: '多打几份工，早点凑齐路费，我们就能彻底离开这里。',
      next: 'l_open'
    },
    l_open: {
      speaker: '利奥',
      text: '可这条街上每一家店、每一条巷子，都是我们从小到大的回忆，走了就再也回不到从前了。',
      next: 'ask'
    },
    ask: {
      speaker: '伊莱亚斯',
      text: '你怎么想？',
      choices: [
        {
          label: '早点攒钱出发才是正事，回忆不能当生活过',
          next: 'a_e',
          effects: { commitment: 1, agency: -0.5 }
        },
        {
          label: '攒钱要紧，但我们也可以偶尔停下来怀念老街',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: '没必要急着走，这里的生活其实也不差',
          next: 'c_l',
          effects: { commitment: -0.5, agency: 1 }
        }
      ]
    },
    // 选项 A 分支
    a_e: {
      speaker: '伊莱亚斯',
      text: '还是你懂我，不能被旧日子绊住脚步。',
      next: 'a_l'
    },
    a_l: {
      speaker: '',
      text: '利奥低头踢石子，不再说话。'
    },
    // 选项 B 分支
    b_narration: {
      speaker: '',
      text: 'Elias 勉强点头，Leo 舒展眉头。'
    },
    // 选项 C 分支
    c_l: {
      speaker: '利奥',
      text: '终于有人能明白我的感受。',
      next: 'c_e'
    },
    c_e: {
      speaker: '',
      text: 'Elias 面色凝重，不再搭话。'
    }
  }
};

// 对话2｜屋顶黄昏眺望北方：远方 vs 眼下
export const CH1_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch1_rooftop_dlg',
  start: 'e_open',
  nodes: {
    e_open: {
      speaker: '伊莱亚斯',
      text: '北边有全新的机会，留在这座城市只会被困死。',
      next: 'l_open'
    },
    l_open: {
      speaker: '利奥',
      text: '远方未必更好，我们只是把所有希望都寄托在看不见的北边而已。',
      next: 'ask'
    },
    ask: {
      speaker: '伊莱亚斯',
      text: '那你倒是说说，我们该怎么选？',
      choices: [
        {
          label: '北边是唯一出路，必须坚持攒钱出发',
          next: 'a_e',
          effects: { commitment: 1, agency: -1 }
        },
        {
          label: '可以去远方，但不用彻底斩断和这里的联结',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: '比起未知的北方，我更珍惜眼下熟悉的一切',
          next: 'c_l',
          effects: { commitment: -1, agency: 1 }
        }
      ]
    },
    a_e: {
      speaker: '伊莱亚斯',
      text: '只要我们齐心协力，很快就能启程。',
      next: 'a_l'
    },
    a_l: {
      speaker: '',
      text: 'Leo 独自走到屋顶边缘，沉默不语。'
    },
    b_narration: {
      speaker: '',
      text: '两人不再争执，安静望向远处灯火。'
    },
    c_l: {
      speaker: '利奥',
      text: '（拍了拍你的肩膀，没再说话。）',
      next: 'c_e'
    },
    c_e: {
      speaker: '',
      text: 'Elias 提前独自下楼。'
    }
  }
};

// ============================================================
// 第二章剧情对话（正式台词）
// 出场：Maya + Noah（Elias/Leo 第二章下线）
// 主线：收集远行物资
// 对话1：老街区杂货铺收集物资（Maya+Noah 双人）
// 对话2：屋顶雨夜讨论取舍（章节收尾，推进 ch3）
// ============================================================

// 对话1｜老街区杂货铺收集物资
export const CH2_SUPPLIES_DIALOGUE: DialogueData = {
  id: 'ch2_supplies',
  start: 'm_open',
  nodes: {
    m_open: {
      speaker: '玛雅',
      text: '画廊给了我长期展位，如果跟着北上，我就要彻底放弃画画。',
      next: 'n_open'
    },
    n_open: {
      speaker: '诺亚',
      text: '家人逼我做不喜欢的工作，北上本来是我的退路，可我最近很沉迷手工创作。',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '——你怎么看？',
      choices: [
        {
          label: '先完成北上计划，个人爱好和机会都可以延后',
          next: 'a_n',
          effects: { commitment: 1, bond: { noah: 1, maya: -0.5 } }
        },
        {
          label: '北上和个人热爱很难兼顾，我们都有各自的难处',
          next: 'b_narration',
          effects: { commitment: 0.3, bond: { noah: 0.3, maya: 0.3 } }
        },
        {
          label: '自己的热爱不该让步，不必为集体计划牺牲自我',
          next: 'c_m',
          effects: { agency: 1, bond: { maya: 1, noah: -0.5 } }
        }
      ]
    },
    a_n: {
      speaker: '诺亚',
      text: '至少有人理解我想逃离家庭的想法。',
      next: 'a_m'
    },
    a_m: {
      speaker: '',
      text: 'Maya 攥紧画稿，神色失落。'
    },
    b_narration: {
      speaker: '',
      text: '两人认同你的说法，气氛缓和。'
    },
    c_m: {
      speaker: '玛雅',
      text: '……谢谢你。',
      next: 'c_n'
    },
    c_n: {
      speaker: '',
      text: 'Maya 眼里亮起光，Noah 低头沉默。'
    }
  }
};

// 对话2｜屋顶雨夜讨论取舍（章节收尾）
export const CH2_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch2_rooftop_finale',
  start: 'm_open',
  nodes: {
    m_open: {
      speaker: '玛雅',
      text: '强行奔赴远方，放弃自己真正热爱的事，就算到了北边也不会快乐。',
      next: 'n_open'
    },
    n_open: {
      speaker: '诺亚',
      text: '可留在家里，我一辈子都要活在家人的安排里，没有自由。',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '雨声渐大。两人都看向你了。',
      choices: [
        {
          label: '优先完成集体约定，个人热爱暂时搁置',
          next: 'a_n',
          effects: { commitment: 2, bond: { noah: 2, maya: -1 } }
        },
        {
          label: '可以折中，抽空兼顾爱好，不彻底放弃任何一方',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3, bond: { noah: 0.3, maya: 0.3 } }
        },
        {
          label: '遵从内心最重要，不必为了一群人的约定委屈自己',
          next: 'c_m',
          effects: { agency: 2, bond: { maya: 2, noah: -1 } }
        }
      ]
    },
    a_n: {
      speaker: '诺亚',
      text: '逃离束缚对我而言更重要。',
      next: 'a_m'
    },
    a_m: {
      speaker: '',
      text: 'Maya 收拾画具，独自离开屋顶。'
    },
    b_narration: {
      speaker: '',
      text: '两人各退一步，不再争吵。'
    },
    c_m: {
      speaker: '玛雅',
      text: '……谢谢你懂我。',
      next: 'c_n'
    },
    c_n: {
      speaker: '',
      text: 'Maya 露出笑意，Noah 叹气不再反驳。'
    }
  }
};

// ============================================================
// 第三章剧情对话（正式台词）
// 出场：Elias + Maya（Noah/Leo 下线）
// 任务：办理出城通行材料（权重最高，一二章印记全部生效）
// 开场对话直接联动前两章全部选择（A1+A2 / C1+C2 / 混合中立）
// 核心任务三选项产生 ch3 印记 A3/B3/C3
// ============================================================

// 核心任务对话｜社区办事处（Elias+Maya，三选项 → A3/B3/C3）
export const CH3_PASS_DIALOGUE: DialogueData = {
  id: 'ch3_pass',
  start: 'e_open',
  nodes: {
    e_open: {
      speaker: '伊莱亚斯',
      text: '所有通行材料必须办齐，不能打乱集体出发时间。',
      next: 'm_open'
    },
    m_open: {
      speaker: '玛雅',
      text: '繁琐手续浪费时间，我不想错过首展，每个人节奏不必统一。',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '——你怎么选？',
      choices: [
        {
          label: '优先办好全部材料，集体计划不能拖延',
          next: 'a_e',
          effects: { commitment: 3, agency: -2, storyMark: { chapter: 'ch3', mark: 'A3' }, trunkItem: 'tools' }
        },
        {
          label: '先办基础材料，抽空兼顾画展',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3, storyMark: { chapter: 'ch3', mark: 'B3' }, trunkItem: 'memory_box' }
        },
        {
          label: '手续放缓，我要去支持你的画展',
          next: 'c_m',
          effects: { commitment: -3, agency: 3, storyMark: { chapter: 'ch3', mark: 'C3' }, trunkItem: 'maya_painting' }
        }
      ]
    },
    // 选项 A：极致坚守计划
    a_e: {
      speaker: '伊莱亚斯',
      text: '好。手续我来帮你加急办。',
      next: 'a_m'
    },
    a_m: {
      speaker: '',
      text: 'Maya 失望地别过脸，画展支线暂时锁定。'
    },
    // 选项 B：折中
    b_narration: {
      speaker: '',
      text: 'Elias 勉强点头，Maya 算是接受——两边都没真正满意，但也都没撕破脸。'
    },
    // 选项 C：优先个人与朋友
    c_m: {
      speaker: '玛雅',
      text: '……谢谢你。这张手绘北方地图送给你。',
      next: 'c_e'
    },
    c_e: {
      speaker: '',
      text: 'Elias 神色冷淡，不再提供任何办事便利。'
    }
  }
};

// 章节收尾｜屋顶矛盾强制剧情（联动前序所有矛盾）
// 三选项：站Elias / 中立调和 / 站Maya
export const CH3_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch3_rooftop_finale',
  start: 'e_open',
  nodes: {
    e_open: {
      speaker: '伊莱亚斯',
      text: '所有人都随心所欲打乱计划，只有我死守约定。',
      next: 'm_open'
    },
    m_open: {
      speaker: '玛雅',
      text: '约定不能捆绑他人，每个人都有选择人生的权利。',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '——你怎么选？',
      choices: [
        {
          label: '站 Elias：约定不能轻易打破',
          next: 'a_e',
          effects: { commitment: 2, agency: -1 }
        },
        {
          label: '中立调和：双方都有道理',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: '站 Maya：每个人都有选择权',
          next: 'c_m',
          effects: { agency: 2, commitment: -2 }
        }
      ]
    },
    a_e: {
      speaker: '伊莱亚斯',
      text: '……终于有人还记得我们当初为什么出发。'
    },
    b_narration: {
      speaker: '',
      text: '夜风渐凉，没人再说话。'
    },
    c_m: {
      speaker: '玛雅',
      text: '……谢谢你愿意站在这一边。'
    }
  }
};

// ============================================================
// 第四章剧情对话（正式台词）
// 出场：Noah + Leo（Elias/Maya 下线）
// 任务：整理回忆，最终抉择
// 开场台词完全读取前三章全部印记（A1A2A3 / C1C2C3 / 混合中立）
// 主线三选项产生 ch4 印记 A4/B4/C4，叠加全局权重
// 屋顶四选一直接锁定结局大方向
// ============================================================

// 主线对话｜老街区整理物资（Noah+Leo，三选项 → A4/B4/C4）
export const CH4_MAIN_DIALOGUE: DialogueData = {
  id: 'ch4_organize',
  start: 'n_open',
  nodes: {
    n_open: {
      speaker: '诺亚',
      text: '我找到了真正热爱的手工，没必要只为逃离家人奔赴北边。',
      next: 'l_open'
    },
    l_open: {
      speaker: '利奥',
      text: '北上从来只是 Elias 一个人的执念，这座城市才是我们的根。',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '——你怎么看？',
      choices: [
        {
          label: '北上是早年约定，不能半途而废',
          next: 'a_n',
          effects: { commitment: 3, agency: -2, storyMark: { chapter: 'ch4', mark: 'A4' }, carryItem: 'group_photo' }
        },
        {
          label: '留下或离开没有对错，不后悔即可',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3, storyMark: { chapter: 'ch4', mark: 'B4' }, carryItem: 'blank_notebook' }
        },
        {
          label: '适合自己最重要，不必死守从前计划',
          next: 'c_l',
          effects: { commitment: -3, agency: 3, storyMark: { chapter: 'ch4', mark: 'C4' }, carryItem: 'house_key' }
        }
      ]
    },
    // 选项 A：坚持北上约定
    a_n: {
      speaker: '诺亚',
      text: '……我明白了。你和 Elias 是一路人。',
      next: 'a_l'
    },
    a_l: {
      speaker: '',
      text: 'Leo 不再说话，Noah 低头整理手边的工具。两人情绪低落，不再分享留守规划。'
    },
    // 选项 B：没有对错
    b_narration: {
      speaker: '',
      text: 'Noah 和 Leo 对视一眼，各自点了点头——虽然没有被说服，但也没有反驳。'
    },
    // 选项 C：适合自己最重要
    c_l: {
      speaker: '利奥',
      text: '……你能这么想，我很高兴。',
      next: 'c_n'
    },
    c_n: {
      speaker: '',
      text: 'Noah 主动聊起了手工工坊的事，Leo 也开始分享老街的日常。三人聊了很久。'
    }
  }
};

// 屋顶终章前置对话｜四选一，直接锁定结局大方向
// 结合前三章累计印记细化结局画面细节
export const CH4_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch4_rooftop_finale',
  start: 'n_open',
  nodes: {
    n_open: {
      speaker: '诺亚',
      text: '留下来，我可以安心做手工，不用躲避家人。',
      next: 'l_open'
    },
    l_open: {
      speaker: '利奥',
      text: '留在老街，所有回忆都会一直陪伴我们。',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '——你的最终选择是？',
      choices: [
        {
          label: '坚持和 Elias 北上，赴远方',
          next: 'end_north',
          effects: { ending: 'go_north', carryItem: 'old_map' }
        },
        {
          label: '留在城市，陪伴众人',
          next: 'end_home',
          effects: { ending: 'return_home' }
        },
        {
          label: '不依附任何一方，独自开辟新路',
          next: 'end_unknown',
          effects: { ending: 'unknown_path', carryItem: 'blank_notebook' }
        },
        {
          label: '暂时停下，独自沉淀思考',
          next: 'end_pause',
          effects: { ending: 'pause_journey' }
        }
      ]
    },
    end_north: {
      speaker: '',
      text: '你转身望向北方。远方的灯火在夜色里格外明亮——那是一条早已约定好的路。'
    },
    end_home: {
      speaker: '',
      text: '你看向脚下的老街。每一盏灯、每一条巷子，都是你长大的痕迹——这里就是你的根。'
    },
    end_unknown: {
      speaker: '',
      text: '你独自走向一条无名小路。既非北上，也非留守——你要走出属于自己的方向。'
    },
    end_pause: {
      speaker: '',
      text: '你在屋顶坐下，没有立刻做决定。夜风渐凉，你需要一些时间，独自沉淀。'
    }
  }
};
