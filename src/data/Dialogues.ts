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
// 第三章剧情对话（占位，待正式台词替换）
// 出场：Elias + Maya（Noah/Leo 下线）
// 任务：办理出城通行材料
// 跨章印记：A1 → Elias 态度温和提供加急便利
// ============================================================

// 对话1｜市政厅办理通行材料（Elias+Maya，含印记连锁开场）
export const CH3_PASS_DIALOGUE: DialogueData = {
  id: 'ch3_pass',
  start: 'e_open',
  nodes: {
    e_open: {
      speaker: '伊莱亚斯',
      text: '通行材料的事交给我。你来了就好。——我们离出发又近了一步。',
      next: 'm_open'
    },
    m_open: {
      speaker: '玛雅',
      text: '……你真的觉得，出了这道门，一切就会不一样吗？',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '——你怎么看？',
      choices: [
        {
          label: '办完材料就出发，别再犹豫了',
          next: 'a_e',
          effects: { commitment: 1, agency: -0.5 }
        },
        {
          label: '材料可以办，但出发日期还能再商量',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: '也许我们该想清楚，到底为什么要走',
          next: 'c_m',
          effects: { commitment: -0.5, agency: 1 }
        }
      ]
    },
    a_e: {
      speaker: '伊莱亚斯',
      text: '好。材料我今天就能加急办好。'
    },
    b_narration: {
      speaker: '',
      text: '两人沉默片刻，各自填着手里的表格。'
    },
    c_m: {
      speaker: '玛雅',
      text: '……终于有人愿意问这个问题了。'
    }
  }
};

// 章节收尾｜屋顶抉择（占位，待正式台词替换）
export const CH3_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch3_rooftop_finale',
  start: 'e_open',
  nodes: {
    e_open: {
      speaker: '伊莱亚斯',
      text: '材料齐了。只剩两天。',
      next: 'm_open'
    },
    m_open: {
      speaker: '玛雅',
      text: '两天……够想清楚很多事，也够让所有事变得更混乱。',
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: '——你怎么看？',
      choices: [
        {
          label: '按计划出发，不能再拖了',
          next: 'a_e',
          effects: { commitment: 1, agency: -0.5 }
        },
        {
          label: '再给自己一点时间',
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: '也许有些人根本不该走',
          next: 'c_m',
          effects: { commitment: -0.5, agency: 1 }
        }
      ]
    },
    a_e: {
      speaker: '伊莱亚斯',
      text: '……终于听到你这么说。'
    },
    b_narration: {
      speaker: '',
      text: '夜风渐凉，没人再说话。'
    },
    c_m: {
      speaker: '玛雅',
      text: '……谢谢你愿意说出来。'
    }
  }
};
