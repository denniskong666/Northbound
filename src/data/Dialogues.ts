// 对话数据：4 位伙伴的占位对话（含分支选项与好感影响）
// 仅供程序验证流程，正式台词由外部提供后替换即可
// effects 结构对齐新状态模型：commitment/rootedness/agency + bond{maya,noah,leo}
// 语义对照（文档第十节）：
//   Elias 相关 → commitment（信守约定）
//   Maya 画展/诺亚热爱/利奥留恋 → rootedness 或 agency + 对应 bond
//   质疑约定/接纳未知 → agency（自我主导）

import { L } from '../systems/I18n';
import { DialogueData } from '../systems/DialogueSystem';
import { NpcId } from './NpcDefs';

export const DIALOGUES: Record<NpcId, DialogueData> = {
  // —— 伊莱亚斯 Elias：旧日约定的坚守者 ——
  elias: {
    id: 'elias_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: L('伊莱亚斯', 'Elias'),
        text: L('又见面了。零件攒得差不多了吧？说好的，攒够就一起往北走。',
                "Long time no see. You've got most of the parts, right? Like we agreed — once we have enough, we head North together."),
        next: 'ask'
      },
      ask: {
        speaker: L('伊莱亚斯', 'Elias'),
        text: L('你心里还记着那件事吧？告诉我——我们一定会一起走的，对吗？',
                "You still remember that, right? Tell me — we're going North together, aren't we?"),
        choices: [
          { label: L('当然，我们说好的。', 'Of course. We made a promise.'),   next: 'confirm', effects: { commitment: 5 } },
          { label: L('我还不知道。',         "I'm not sure yet."),               next: 'doubt',  effects: { commitment: -5, agency: 5 } }
        ]
      },
      confirm: {
        speaker: L('伊莱亚斯', 'Elias'),
        text: L('好。记住你今天说的话。北边在等我们所有人。',
                'Good. Remember what you said today. The North is waiting for all of us.')
      },
      doubt: {
        speaker: L('伊莱亚斯', 'Elias'),
        text: L('……我不知道。这话从你嘴里说出来，比从他们嘴里说出来更重。',
                "...I don't know. Hearing that from you hits harder than from anyone else.")
      }
    }
  },

  // —— 玛雅 Maya：敢于接纳自我改变的人 ——
  maya: {
    id: 'maya_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: L('玛雅', 'Maya'),
        text: L('你看这张——我把街角那盏路灯画进去了。颜色好像有点太亮？',
                "Look at this one — I painted in that streetlight on the corner. The color's a bit too bright, isn't it?"),
        next: 'ask'
      },
      ask: {
        speaker: L('玛雅', 'Maya'),
        text: L('其实……本地有人问我愿不愿意拿去参展。你说我该去吗？',
                'Actually... someone local asked if I would submit it for an exhibition. Should I?'),
        choices: [
          { label: L('画得真好，该去。',     "It's beautiful. You should go."),        next: 'encourage', effects: { rootedness: 5, bond: { maya: 10 } } },
          { label: L('别分心，先修车要紧。', "Don't get distracted — fixing the car comes first."), next: 'dismiss',   effects: { commitment: 5, bond: { maya: -5 } } }
        ]
      },
      encourage: {
        speaker: L('玛雅', 'Maya'),
        text: L('……谢谢你。我都有点不敢相信，这条街的颜色也能被人看见。',
                '...Thank you. I can hardly believe it — the colors of this street, seen by someone else.')
      },
      dismiss: {
        speaker: L('玛雅', 'Maya'),
        text: L('嗯……也是。修车的事比较急。画展的事，再算吧。',
                "Yeah... true. The car's more urgent. The exhibition — let's leave it for later.")
      }
    }
  },

  // —— 诺亚 Noah：必须说出心里话的人 ——
  noah: {
    id: 'noah_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: L('诺亚', 'Noah'),
        text: L('家里又让我去考那个稳妥的学校。可我最近总在想……我真正想做的是什么。',
                'My family wants me to apply to that "safe" school again. But lately I keep wondering... what is it I really want to do?'),
        next: 'ask'
      },
      ask: {
        speaker: L('诺亚', 'Noah'),
        text: L('你觉得，是听家人的安排稳当，还是去找自己真正想做的事？',
                "What do you think — follow the family's plan and play it safe, or go find what I really want to do?"),
        choices: [
          { label: L('想做什么就去做。',   'Go do what you want.'),                      next: 'brave',    effects: { agency: 5, bond: { noah: 10 } } },
          { label: L('听家人的更稳妥。',   "It's safer to listen to your family."),       next: 'obedient', effects: { commitment: 3, bond: { noah: -5 } } }
        ]
      },
      brave: {
        speaker: L('诺亚', 'Noah'),
        text: L('……你说得对。总要试一次，才知道自己到底是什么人。',
                "...You're right. I have to try at least once, to find out who I really am.")
      },
      obedient: {
        speaker: L('诺亚', 'Noah'),
        text: L('嗯，也许你说得对。先按部就班，别节外生枝。',
                "Yeah, maybe you're right. Stick to the plan, don't stir things up.")
      }
    }
  },

  // —— 利奥 Leo：坦然直面内心羁绊的人 ——
  leo: {
    id: 'leo_intro',
    start: 'greet',
    nodes: {
      greet: {
        speaker: L('利奥', 'Leo'),
        text: L('又在这条街晃。我嘴上老说想走，可每次走到路口又停下来了。',
                'Wandering this street again. I keep saying I want to leave, but every time I reach the intersection, I stop.'),
        next: 'ask'
      },
      ask: {
        speaker: L('利奥', 'Leo'),
        text: L('你说，我是真想走，还是只是舍不得这里？',
                'Tell me — do I really want to leave, or am I just reluctant to let this place go?'),
        choices: [
          { label: L('你舍不得这里。', "You can't bear to leave this place."), next: 'truth', effects: { rootedness: 5, bond: { leo: 10 } } },
          { label: L('那就一起走呗。', 'Then come with us.'),                 next: 'leave', effects: { commitment: 3, bond: { leo: -5 } } }
        ]
      },
      truth: {
        speaker: L('利奥', 'Leo'),
        text: L('……被你看穿了。留下，好像也不算懦弱吧。',
                "...You saw right through me. Staying — that's not cowardice, is it?")
      },
      leave: {
        speaker: L('利奥', 'Leo'),
        text: L('嗯。也许离开一阵子，我才能看清自己到底舍不得什么。',
                "Yeah. Maybe I need to leave for a while, to see clearly what it is I can't let go of.")
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
        speaker: L('伊莱亚斯', 'Elias'),
        text: L('零件的事不急，先把手头的活干完。北边不会跑掉的。',
                "No rush on the parts — finish what's in front of you first. The North isn't going anywhere.")
      }
    }
  },
  maya: {
    id: 'maya_daily',
    start: 'line',
    nodes: {
      line: {
        speaker: L('玛雅', 'Maya'),
        text: L('今天光线不错，回头我画一张这条街的速写给你看。',
                "The light's nice today. I'll sketch this street for you later.")
      }
    }
  },
  noah: {
    id: 'noah_daily',
    start: 'line',
    nodes: {
      line: {
        speaker: L('诺亚', 'Noah'),
        text: L('录音机昨天又录到一段不错的风声。走了以后，大概会想念这些声音吧。',
                "The recorder caught a nice stretch of wind yesterday. After we leave, I'll probably miss these sounds.")
      }
    }
  },
  leo: {
    id: 'leo_daily',
    start: 'line',
    nodes: {
      line: {
        speaker: L('利奥', 'Leo'),
        text: L('餐厅今天有新菜。说真的，这地方的吃的，到哪儿都替代不了。',
                "The diner has a new dish today. Honestly, the food here — nothing anywhere else can replace it.")
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
    title: L('北方极光·明信片', 'Northern Aurora · Postcard'),
    text: L('有人从北方寄来的明信片。照片里，绿色和紫色的光带在夜空中翻涌，像神话里才会有的景象。\n背面写着：「这里的天空，每晚都在跳舞。」',
            `A postcard sent from the North. In the photo, ribbons of green and violet light churn across the night sky, like something out of a myth.\nOn the back: "The sky here dances every night."`)
  },
  harbor: {
    title: L('黄昏港口·明信片', 'Dusk Harbor · Postcard'),
    text: L('北方新港区的黄昏剪影。高楼窗户亮着成百上千盏灯，海面倒映着金色晚霞。\n角落里有人用钢笔圈出了三个字：「包食宿」。',
            `A silhouette of dusk in New Harbor. Hundreds of lights glow in the towers' windows, and the sea reflects the golden sunset.\nIn the corner, someone circled three words in ink: "Room & board included."`)
  },
  mountain: {
    title: L('北方旷野·明信片', 'Northern Wilds · Postcard'),
    text: L('层层叠叠的蓝灰色山脉，近处是深褐色的旷野。最高的那座山峰还顶着一点未化的白雪。\n背面写着：「趁年轻，去看看山的那一边。」',
            `Ridges of blue-gray mountains, with deep-brown wilderness in the foreground. The highest peak still wears a cap of unmelting snow.\nOn the back: "While you're young — go see what lies beyond the mountains."`)
  },
  gallery: {
    title: L('新港区美术馆·明信片', 'New Harbor Art Museum · Postcard'),
    text: L('新港区美术馆的外观。外墙是浅米色的，屋顶尖尖的，大门上方挂着「新锐画家征稿中」的小招牌。\n玛雅在旁边用铅笔打了个小星星。',
            `The facade of the New Harbor Art Museum. Pale beige walls, a sharp peaked roof, and a small sign above the entrance: "Emerging Artists — Submissions Open."\nMaya penciled a little star next to it.`)
  }
};

// 北方宣传看板描述
export const CH0_BOARD_DESC = {
  title: L('北方联合宣传·公告板', 'Northern United Promotion · Bulletin Board'),
  text: L('街区中央的公告板被一张大海报占满了。\n\n大字写着「— 北 方 —」\n副标题：机会 · 自由 · 新的人生\n\n下方小字：\n· 新港区招工 · 薪资三倍 · 包食宿\n· 新港区美术馆新锐画家征稿 · 有奖金\n\n右下角用绿色马克笔写着：「一起北上 →」',
            `A bulletin board in the middle of the district, covered by one giant poster.\n\nIn huge letters: "— THE NORTH —"\nSubtitle: Opportunity · Freedom · A New Life\n\nSmall print below:\n· New Harbor hiring · Triple wages · Room & board included\n· New Harbor Art Museum seeking emerging artists · Cash prizes\n\nIn the lower right, in green marker: "Northbound, together →"`)
};

// 愿望墙描述
export const CH0_WISHWALL_DESC = {
  title: L('街角愿望板', 'Corner Wish Board'),
  text: L('一块钉满便签的旧木板。\n\n绿色便签（诺亚）：逃离家人 做手工！\n黄色便签（伊莱亚斯）：薪资三倍 走出去！\n粉色便签（玛雅）：画极光 办画展！\n蓝色便签（利奥）：看大海 闯天下！\n\n正中央那张白色便签写着「你的愿望？」——走过去，也许可以写下。',
            `An old board studded with sticky notes.\n\nGreen note (Noah): Escape family — make things by hand!\nYellow note (Elias): Triple pay — get out there!\nPink note (Maya): Paint the aurora — hold an exhibition!\nBlue note (Leo): See the sea — make a name!\n\nIn the very center, a white note reads "Your wish?" — walk over, maybe you can write one.`)
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
    label: L('赚大钱，出人头地', 'Make a fortune, stand out'),
    toast: L('你写下了「赚大钱 出人头地」', `You wrote: "Make a fortune — stand out."`),
    effect: { commitment: 0.5 }
  },
  {
    id: 'freedom',
    label: L('看看世界，自由自在', 'See the world, be free'),
    toast: L('你写下了「看世界 自由自在」', `You wrote: "See the world — be free."`),
    effect: { agency: 0.3 }
  },
  {
    id: 'art',
    label: L('做喜欢的事（画画/音乐）', 'Do what I love (painting/music)'),
    toast: L('你写下了「画遍山河 办画展」', `You wrote: "Paint every river and mountain — hold an exhibition."`),
    effect: { agency: 0.3 }
  },
  {
    id: 'friends',
    label: L('和朋友永远在一起', 'Stay with friends forever'),
    toast: L('你写下了「大家一起 永不分开」', `You wrote: "All of us together — never apart."`),
    effect: { commitment: 0.3, agency: 0.2 }
  },
  {
    id: 'path',
    label: L('找到属于自己的路', 'Find my own path'),
    toast: L('你写下了「找到属于 自己的路」', `You wrote: "Find a path — my own."`),
    effect: { commitment: 0.2, agency: 0.2 }
  }
];

// —— 序章·数北方灯火小游戏剧情文本 ——
export const CH0_LIGHTGAME_INTRO = {
  title: L('街角橱窗·北方灯火展', 'Corner Window · Northern Lights Display'),
  text: L('Elias 在橱窗里贴了一张北方夜空的照片，笑着说：「能看到这些灯火的人，未来都不会差。\n来玩个小游戏——20 秒内点亮 8 盏灯，怎么样？」',
          `Elias pinned a photo of the northern night sky in the shop window and grinned: "Anyone who can see these lights is bound for a good future.\nLet's play a little game — light up 8 of them in 20 seconds, what do you say?"`)
};

export const CH0_LIGHTGAME_WIN = L('你点亮了 8 盏北方的灯火。Elias 拍你肩膀：「看，北方在等我们！」',
                                      `You lit 8 of the northern lights. Elias claps your shoulder: "See — the North is waiting for us!"`);
export const CH0_LIGHTGAME_LOSE = (count: number) =>
  count >= 5 ? L(`只差一点了！${count} 盏也够亮了。玛雅笑着说：「反正到了北方，我们能看个够。」`,
                  `So close! ${count} is bright enough. Maya laughs: "Once we reach the North, we'll see them as much as we want."`)
              : L(`点了 ${count} 盏灯。Leo 摊手：「别急，北方的灯火看一辈子都看不完。」`,
                  `You lit ${count} lights. Leo shrugs: "Take it easy — you could spend a lifetime watching the northern lights and never finish."`);

// 序章 NPC 对话：Elias — 兴奋地谈论北方的机会
export const CH0_ELIAS_DIALOGUE: DialogueData = {
  id: 'ch0_elias',
  start: 'greet',
  nodes: {
    greet: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('我昨晚又查了一遍路线。沿着北线公路走，三天就能到新港区——那里正在招工，工资是这里的三倍。',
              "I checked the route again last night. Take the North Highway and you reach New Harbor in three days — they're hiring, and the pay is triple what we get here."),
      next: 'excited'
    },
    excited: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('我们五个一起走，一起干，用不了一年就能站稳脚跟。这才是我们该过的人生！',
              "The five of us go together, work together — give it a year and we'll be on our feet. That's the life we're meant to live!"),
      next: 'ask'
    },
    ask: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('你也在期待吧？北方在等我们。',
              "You're looking forward to it too, aren't you? The North is waiting for us."),
      choices: [
        { label: L('当然，我已经等不及了。', 'Of course. I can\'t wait.'),  next: 'confirm', effects: { commitment: 1 } },
        { label: L('想想就让人激动。',       'Just thinking about it gets me excited.'), next: 'confirm', effects: { commitment: 0.5 } }
      ]
    },
    confirm: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('那就说好了——我们一起往北走，谁也不许掉队！',
              "Then it's settled — we head North together, and nobody falls behind!")
    }
  }
};

// 序章 NPC 对话：Maya — 想去北方画新的色彩
export const CH0_MAYA_DIALOGUE: DialogueData = {
  id: 'ch0_maya',
  start: 'greet',
  nodes: {
    greet: {
      speaker: L('玛雅', 'Maya'),
      text: L('你猜我在画什么？——北方的极光！我在杂志上看过照片，那种颜色这里根本见不到。',
              "Guess what I'm painting? — the Northern Aurora! I saw a photo in a magazine. You can't find colors like that here."),
      next: 'dream'
    },
    dream: {
      speaker: L('玛雅', 'Maya'),
      text: L('听说新港区的美术馆正在征集新锐画师。如果我们去了北方，我的画说不定能被挂上去！',
              "I heard the New Harbor Art Museum is looking for emerging painters. If we go North, maybe my work will end up on their walls!"),
      next: 'ask'
    },
    ask: {
      speaker: L('玛雅', 'Maya'),
      text: L('你说，北方的天空到底是什么颜色的？',
              'Tell me — what color is the sky in the North, really?'),
      choices: [
        { label: L('一定比这里更辽阔。', 'It must be vaster than here.'), next: 'confirm', effects: { bond: { maya: 2 } } },
        { label: L('去了就知道了。',   "We'll know when we get there."),    next: 'confirm', effects: { bond: { maya: 1 } } }
      ]
    },
    confirm: {
      speaker: L('玛雅', 'Maya'),
      text: L('哈哈，对！等我们到了北方，我要把所有的颜色都画下来！',
              "Haha, yes! Once we reach the North, I'm going to paint every single color!")
    }
  }
};

// 序章 NPC 对话：Noah — 想去北方逃离家人的安排
export const CH0_NOAH_DIALOGUE: DialogueData = {
  id: 'ch0_noah',
  start: 'greet',
  nodes: {
    greet: {
      speaker: L('诺亚', 'Noah'),
      text: L('我妈昨天又给我报了个「稳妥」的培训班。她觉得我这辈子就该按她画好的路线走。',
              `My mom signed me up for another "safe" training class yesterday. She thinks I should follow the path she's drawn for me, my whole life.`),
      next: 'rebel'
    },
    rebel: {
      speaker: L('诺亚', 'Noah'),
      text: L('但我不想！北方谁也不认识我，我可以重新开始——做手工、学音乐，做什么都行。',
              "But I don't want to! In the North, nobody knows me — I can start over. Make things by hand, learn music, whatever I want."),
      next: 'ask'
    },
    ask: {
      speaker: L('诺亚', 'Noah'),
      text: L('到了北方，第一件事你想做什么？',
              "Once we get to the North — what's the first thing you want to do?"),
      choices: [
        { label: L('先好好看看那座城市。',         'Take a good look at the city first.'),      next: 'confirm', effects: { bond: { noah: 2 } } },
        { label: L('大睡一觉，醒来就是新生活。',   'Sleep in. Wake up to a new life.'),          next: 'confirm', effects: { bond: { noah: 1 } } }
      ]
    },
    confirm: {
      speaker: L('诺亚', 'Noah'),
      text: L('哈哈，好！反正到了北方，一切都是新的——连空气都是自由的味道。',
              "Haha, alright! Once we're in the North, everything will be new — even the air will taste of freedom.")
    }
  }
};

// 序章 NPC 对话：Leo — 也向往北方的冒险
export const CH0_LEO_DIALOGUE: DialogueData = {
  id: 'ch0_leo',
  start: 'greet',
  nodes: {
    greet: {
      speaker: L('利奥', 'Leo'),
      text: L('你知道吗，这条街我走了十八年，闭着眼都能数清每块砖。说真的，太闷了。',
              "You know, I've walked this street for eighteen years. Eyes closed, I could count every brick. Honestly — it's stifling."),
      next: 'adventure'
    },
    adventure: {
      speaker: L('利奥', 'Leo'),
      text: L('北方有海、有山、有我们从没见过的东西。趁着年轻，就该出去闯一闯！',
              "The North has the sea, the mountains, things we've never seen. While we're young — we ought to go out and make something of ourselves!"),
      next: 'ask'
    },
    ask: {
      speaker: L('利奥', 'Leo'),
      text: L('我们从小就说要一起走出去——这次是真的了吧？',
              "Since we were kids we've talked about getting out together — this time it's real, isn't it?"),
      choices: [
        { label: L('这次是真的，我们一起走。', "This time it's real. We go together."), next: 'confirm', effects: { commitment: 0.5, bond: { leo: 2 } } },
        { label: L('而且绝不回头。',           "And we don't look back."),                  next: 'confirm', effects: { commitment: 1, bond: { leo: 1 } } }
      ]
    },
    confirm: {
      speaker: L('利奥', 'Leo'),
      text: L('这才对嘛！老街的日子虽然也不错，但北方才是我们要去的地方！',
              "That's the spirit! The Old Street's been all right, but the North — that's where we're headed!")
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
      text: L('黄昏。五个人挤在屋顶上，面朝北方。远处城市的灯火连成一片，像是在召唤。',
              "Dusk. Five of us crammed onto the rooftop, facing North. The city's lights in the distance stretched out in a sheet, as if calling to us."),
      next: 'elias'
    },
    elias: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('看到了吗？那边就是北方。只要攒够路费，我们很快就能出发。',
              "See that? That's the North. Once we've saved enough for the trip, we can leave soon."),
      next: 'maya'
    },
    maya: {
      speaker: L('玛雅', 'Maya'),
      text: L('我要画下我们出发那天的天空——一定比现在更漂亮。',
              "I'm going to paint the sky on the day we leave — it's bound to be more beautiful than this."),
      next: 'noah'
    },
    noah: {
      speaker: L('诺亚', 'Noah'),
      text: L('到了北方，第一件事就是把家人的电话拉黑——开玩笑的。大概吧。',
              "First thing in the North — block my family's number. Kidding. Probably."),
      next: 'leo'
    },
    leo: {
      speaker: L('利奥', 'Leo'),
      text: L('嘿，十八年了，终于要走出这条街了。北方，我们来了！',
              "Hey — eighteen years, and we're finally leaving this street. North — here we come!"),
      next: 'ask'
    },
    ask: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('说好了——我们五个人，一起往北走。谁也不许掉队。',
              "It's settled — the five of us head North together. Nobody falls behind."),
      choices: [
        { label: L('一起走，绝不掉队！', 'Together — no one falls behind!'), next: 'pact', effects: { commitment: 1 } },
        { label: L('北方在等我们。',     'The North is waiting for us.'),       next: 'pact', effects: { commitment: 0.5 } }
      ]
    },
    pact: {
      speaker: '',
      text: L('五个人在夕阳下击掌。那个瞬间，北方不只是一个方向——它是所有人共同的希望。',
              "Five of us high-fived in the sunset. In that moment, the North wasn't just a direction — it was a hope we all shared.")
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
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('多打几份工，早点凑齐路费，我们就能彻底离开这里。',
              'Pick up a few more jobs, save up the travel money faster — and we can leave this place for good.'),
      next: 'l_open'
    },
    l_open: {
      speaker: L('利奥', 'Leo'),
      text: L('可这条街上每一家店、每一条巷子，都是我们从小到大的回忆，走了就再也回不到从前了。',
              "But every shop on this street, every alley — they're all memories from when we were kids. Once we leave, we can never come back to the way things were."),
      next: 'ask'
    },
    ask: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('你怎么想？', 'What do you think?'),
      choices: [
        {
          label: L('早点攒钱出发才是正事，回忆不能当生活过',
                   "Saving up and leaving is what matters — memories don't pay the bills"),
          next: 'a_e',
          effects: { commitment: 1, agency: -0.5 }
        },
        {
          label: L('攒钱要紧，但我们也可以偶尔停下来怀念老街',
                   "Saving matters, but we can pause now and then to miss the Old Street"),
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: L('没必要急着走，这里的生活其实也不差',
                   "No need to rush — life here isn't bad, really"),
          next: 'c_l',
          effects: { commitment: -0.5, agency: 1 }
        }
      ]
    },
    // 选项 A 分支
    a_e: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('还是你懂我，不能被旧日子绊住脚步。',
              "You get it. Can't let the old days trip us up."),
      next: 'a_l'
    },
    a_l: {
      speaker: '',
      text: L('利奥低头踢石子，不再说话。',
              'Leo looks down and kicks at stones, saying nothing more.')
    },
    // 选项 B 分支
    b_narration: {
      speaker: '',
      text: L('Elias 勉强点头，Leo 舒展眉头。',
              "Elias nods reluctantly; Leo's brow relaxes.")
    },
    // 选项 C 分支
    c_l: {
      speaker: L('利奥', 'Leo'),
      text: L('终于有人能明白我的感受。',
              'Finally, someone who understands how I feel.'),
      next: 'c_e'
    },
    c_e: {
      speaker: '',
      text: L('Elias 面色凝重，不再搭话。',
              "Elias's face tightens; he says no more.")
    }
  }
};

// 对话2｜屋顶黄昏眺望北方：远方 vs 眼下
export const CH1_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch1_rooftop_dlg',
  start: 'e_open',
  nodes: {
    e_open: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('北边有全新的机会，留在这座城市只会被困死。',
              "The North has fresh opportunities. Staying in this city — we'll just be trapped here."),
      next: 'l_open'
    },
    l_open: {
      speaker: L('利奥', 'Leo'),
      text: L('远方未必更好，我们只是把所有希望都寄托在看不见的北边而已。',
              "Far-off places aren't necessarily better. We're just pinning all our hopes on a North we can't even see."),
      next: 'ask'
    },
    ask: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('那你倒是说说，我们该怎么选？',
              'Then tell me — what should we choose?'),
      choices: [
        {
          label: L('北边是唯一出路，必须坚持攒钱出发',
                   'The North is the only way out — keep saving and stick to the plan'),
          next: 'a_e',
          effects: { commitment: 1, agency: -1 }
        },
        {
          label: L('可以去远方，但不用彻底斩断和这里的联结',
                   "We can go far, but we don't have to cut all ties with home"),
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: L('比起未知的北方，我更珍惜眼下熟悉的一切',
                   'More than an unknown North, I treasure what I know right here'),
          next: 'c_l',
          effects: { commitment: -1, agency: 1 }
        }
      ]
    },
    a_e: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('只要我们齐心协力，很快就能启程。',
              "As long as we pull together, we'll set off soon."),
      next: 'a_l'
    },
    a_l: {
      speaker: '',
      text: L('Leo 独自走到屋顶边缘，沉默不语。',
              'Leo walks alone to the edge of the rooftop and says nothing.')
    },
    b_narration: {
      speaker: '',
      text: L('两人不再争执，安静望向远处灯火。',
              'The two stop arguing and quietly watch the distant lights.')
    },
    c_l: {
      speaker: L('利奥', 'Leo'),
      text: L('（拍了拍你的肩膀，没再说话。）',
              '(Pats your shoulder, says nothing more.)'),
      next: 'c_e'
    },
    c_e: {
      speaker: '',
      text: L('Elias 提前独自下楼。',
              'Elias heads downstairs alone, ahead of the rest.')
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
      speaker: L('玛雅', 'Maya'),
      text: L('画廊给了我长期展位，如果跟着北上，我就要彻底放弃画画。',
              "The gallery offered me a permanent slot. If I go North, I'll have to give up painting for good."),
      next: 'n_open'
    },
    n_open: {
      speaker: L('诺亚', 'Noah'),
      text: L('家人逼我做不喜欢的工作，北上本来是我的退路，可我最近很沉迷手工创作。',
              "My family is forcing me into work I hate. Going North was supposed to be my way out — but lately I've gotten deep into crafting."),
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: L('——你怎么看？', '— What do you think?'),
      choices: [
        {
          label: L('先完成北上计划，个人爱好和机会都可以延后',
                   'Finish the Northbound plan first — hobbies and chances can wait'),
          next: 'a_n',
          effects: { commitment: 1, bond: { noah: 1, maya: -0.5 } }
        },
        {
          label: L('北上和个人热爱很难兼顾，我们都有各自的难处',
                   "It's hard to balance the North and what we love — we all have our struggles"),
          next: 'b_narration',
          effects: { commitment: 0.3, bond: { noah: 0.3, maya: 0.3 } }
        },
        {
          label: L('自己的热爱不该让步，不必为集体计划牺牲自我',
                   "What you love shouldn't take a back seat — don't sacrifice yourself for the group plan"),
          next: 'c_m',
          effects: { agency: 1, bond: { maya: 1, noah: -0.5 } }
        }
      ]
    },
    a_n: {
      speaker: L('诺亚', 'Noah'),
      text: L('至少有人理解我想逃离家庭的想法。',
              'At least someone gets why I want to escape my family.'),
      next: 'a_m'
    },
    a_m: {
      speaker: '',
      text: L('Maya 攥紧画稿，神色失落。',
              'Maya grips her sketches, her face fallen.')
    },
    b_narration: {
      speaker: '',
      text: L('两人认同你的说法，气氛缓和。',
              'The two accept what you say, and the mood eases.')
    },
    c_m: {
      speaker: L('玛雅', 'Maya'),
      text: L('……谢谢你。', '...Thank you.'),
      next: 'c_n'
    },
    c_n: {
      speaker: '',
      text: L('Maya 眼里亮起光，Noah 低头沉默。',
              "Light kindles in Maya's eyes; Noah looks down, silent.")
    }
  }
};

// 对话2｜屋顶雨夜讨论取舍（章节收尾）
export const CH2_ROOFTOP_DIALOGUE: DialogueData = {
  id: 'ch2_rooftop_finale',
  start: 'm_open',
  nodes: {
    m_open: {
      speaker: L('玛雅', 'Maya'),
      text: L('强行奔赴远方，放弃自己真正热爱的事，就算到了北边也不会快乐。',
              "Forcing ourselves to go far away and give up what we love — even if we reach the North, we won't be happy."),
      next: 'n_open'
    },
    n_open: {
      speaker: L('诺亚', 'Noah'),
      text: L('可留在家里，我一辈子都要活在家人的安排里，没有自由。',
              "But if I stay home, I'll live my whole life by my family's plan — no freedom."),
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: L('雨声渐大。两人都看向你了。',
              'The rain grows louder. Both of them look to you.'),
      choices: [
        {
          label: L('优先完成集体约定，个人热爱暂时搁置',
                   "Honor the group's pact first — set aside what we love for now"),
          next: 'a_n',
          effects: { commitment: 2, bond: { noah: 2, maya: -1 } }
        },
        {
          label: L('可以折中，抽空兼顾爱好，不彻底放弃任何一方',
                   'Compromise — make time for our passions without fully giving up either side'),
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3, bond: { noah: 0.3, maya: 0.3 } }
        },
        {
          label: L('遵从内心最重要，不必为了一群人的约定委屈自己',
                   "Following your heart comes first — no need to deny yourself for a group promise"),
          next: 'c_m',
          effects: { agency: 2, bond: { maya: 2, noah: -1 } }
        }
      ]
    },
    a_n: {
      speaker: L('诺亚', 'Noah'),
      text: L('逃离束缚对我而言更重要。',
              'Escaping these constraints matters more to me.'),
      next: 'a_m'
    },
    a_m: {
      speaker: '',
      text: L('Maya 收拾画具，独自离开屋顶。',
              'Maya packs up her art supplies and leaves the rooftop alone.')
    },
    b_narration: {
      speaker: '',
      text: L('两人各退一步，不再争吵。',
              'Each side takes a step back; the arguing stops.')
    },
    c_m: {
      speaker: L('玛雅', 'Maya'),
      text: L('……谢谢你懂我。', '...Thank you for understanding me.'),
      next: 'c_n'
    },
    c_n: {
      speaker: '',
      text: L('Maya 露出笑意，Noah 叹气不再反驳。',
              'Maya smiles; Noah sighs and stops arguing.')
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
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('所有通行材料必须办齐，不能打乱集体出发时间。',
              "All the travel paperwork has to be in order — we can't throw off the group's departure time."),
      next: 'm_open'
    },
    m_open: {
      speaker: L('玛雅', 'Maya'),
      text: L('繁琐手续浪费时间，我不想错过首展，每个人节奏不必统一。',
              "All this red tape is a waste of time. I don't want to miss the opening show — we don't all have to move at the same pace."),
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: L('——你怎么选？', '— What do you choose?'),
      choices: [
        {
          label: L('优先办好全部材料，集体计划不能拖延',
                   "Get all the paperwork done first — the group plan can't slip"),
          next: 'a_e',
          effects: { commitment: 3, agency: -2, storyMark: { chapter: 'ch3', mark: 'A3' }, trunkItem: 'tools' }
        },
        {
          label: L('先办基础材料，抽空兼顾画展',
                   'Handle the basics first, then make time for the exhibition'),
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3, storyMark: { chapter: 'ch3', mark: 'B3' }, trunkItem: 'memory_box' }
        },
        {
          label: L('手续放缓，我要去支持你的画展',
                   "Slow down on the paperwork — I'm going to support your exhibition"),
          next: 'c_m',
          effects: { commitment: -3, agency: 3, storyMark: { chapter: 'ch3', mark: 'C3' }, trunkItem: 'maya_painting' }
        }
      ]
    },
    // 选项 A：极致坚守计划
    a_e: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('好。手续我来帮你加急办。',
              "All right. I'll fast-track the paperwork for you."),
      next: 'a_m'
    },
    a_m: {
      speaker: '',
      text: L('Maya 失望地别过脸，画展支线暂时锁定。',
              'Maya turns away in disappointment; the exhibition subplot is locked for now.')
    },
    // 选项 B：折中
    b_narration: {
      speaker: '',
      text: L('Elias 勉强点头，Maya 算是接受——两边都没真正满意，但也都没撕破脸。',
              "Elias nods grudgingly, and Maya sort of accepts — neither side is truly satisfied, but neither has fallen out with the other.")
    },
    // 选项 C：优先个人与朋友
    c_m: {
      speaker: L('玛雅', 'Maya'),
      text: L('……谢谢你。这张手绘北方地图送给你。',
              '...Thank you. Here — this hand-drawn map of the North is for you.'),
      next: 'c_e'
    },
    c_e: {
      speaker: '',
      text: L('Elias 神色冷淡，不再提供任何办事便利。',
              "Elias turns cold and offers no more help with the paperwork.")
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
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('所有人都随心所欲打乱计划，只有我死守约定。',
              "Everyone does whatever they want and wrecks the plan. I'm the only one still holding the pact together."),
      next: 'm_open'
    },
    m_open: {
      speaker: L('玛雅', 'Maya'),
      text: L('约定不能捆绑他人，每个人都有选择人生的权利。',
              "A pact can't bind other people. Everyone has the right to choose their own life."),
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: L('——你怎么选？', '— What do you choose?'),
      choices: [
        {
          label: L('站 Elias：约定不能轻易打破', "Side with Elias: a pact shouldn't be broken lightly"),
          next: 'a_e',
          effects: { commitment: 2, agency: -1 }
        },
        {
          label: L('中立调和：双方都有道理', 'Stay neutral: both sides have a point'),
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3 }
        },
        {
          label: L('站 Maya：每个人都有选择权', "Side with Maya: everyone has the right to choose"),
          next: 'c_m',
          effects: { agency: 2, commitment: -2 }
        }
      ]
    },
    a_e: {
      speaker: L('伊莱亚斯', 'Elias'),
      text: L('……终于有人还记得我们当初为什么出发。',
              '...Finally, someone remembers why we set out in the first place.')
    },
    b_narration: {
      speaker: '',
      text: L('夜风渐凉，没人再说话。',
              'The night wind turns cold; no one speaks again.')
    },
    c_m: {
      speaker: L('玛雅', 'Maya'),
      text: L('……谢谢你愿意站在这一边。',
              '...Thank you for standing on this side.')
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
      speaker: L('诺亚', 'Noah'),
      text: L('我找到了真正热爱的手工，没必要只为逃离家人奔赴北边。',
              "I've found the craft I truly love — there's no need to rush North just to escape my family."),
      next: 'l_open'
    },
    l_open: {
      speaker: L('利奥', 'Leo'),
      text: L('北上从来只是 Elias 一个人的执念，这座城市才是我们的根。',
              "Going North was always just Elias's obsession. This city is where we're rooted."),
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: L('——你怎么看？', '— What do you think?'),
      choices: [
        {
          label: L('北上是早年约定，不能半途而废',
                   "Northbound was our old pact — we can't give up halfway"),
          next: 'a_n',
          effects: { commitment: 3, agency: -2, storyMark: { chapter: 'ch4', mark: 'A4' }, carryItem: 'group_photo' }
        },
        {
          label: L('留下或离开没有对错，不后悔即可',
                   "Staying or leaving — neither is right or wrong. Just don't regret it"),
          next: 'b_narration',
          effects: { commitment: 0.3, agency: 0.3, storyMark: { chapter: 'ch4', mark: 'B4' }, carryItem: 'blank_notebook' }
        },
        {
          label: L('适合自己最重要，不必死守从前计划',
                   "What fits you matters most — no need to cling to old plans"),
          next: 'c_l',
          effects: { commitment: -3, agency: 3, storyMark: { chapter: 'ch4', mark: 'C4' }, carryItem: 'house_key' }
        }
      ]
    },
    // 选项 A：坚持北上约定
    a_n: {
      speaker: L('诺亚', 'Noah'),
      text: L('……我明白了。你和 Elias 是一路人。',
              '...I see. You and Elias are the same kind of people.'),
      next: 'a_l'
    },
    a_l: {
      speaker: '',
      text: L('Leo 不再说话，Noah 低头整理手边的工具。两人情绪低落，不再分享留守规划。',
              'Leo says nothing more; Noah looks down and sorts through the tools beside him. Both are downcast, and neither shares any more plans about staying.')
    },
    // 选项 B：没有对错
    b_narration: {
      speaker: '',
      text: L('Noah 和 Leo 对视一眼，各自点了点头——虽然没有被说服，但也没有反驳。',
              'Noah and Leo glance at each other and nod — not convinced, but not arguing either.')
    },
    // 选项 C：适合自己最重要
    c_l: {
      speaker: L('利奥', 'Leo'),
      text: L('……你能这么想，我很高兴。',
              "...I'm glad you see it that way."),
      next: 'c_n'
    },
    c_n: {
      speaker: '',
      text: L('Noah 主动聊起了手工工坊的事，Leo 也开始分享老街的日常。三人聊了很久。',
              'Noah starts talking about the craft workshop, and Leo begins sharing bits of everyday life on the Old Street. The three of them talk for a long while.')
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
      speaker: L('诺亚', 'Noah'),
      text: L('留下来，我可以安心做手工，不用躲避家人。',
              'If I stay, I can focus on my craft in peace — no more dodging my family.'),
      next: 'l_open'
    },
    l_open: {
      speaker: L('利奥', 'Leo'),
      text: L('留在老街，所有回忆都会一直陪伴我们。',
              'Staying on the Old Street — all our memories will keep us company, always.'),
      next: 'ask'
    },
    ask: {
      speaker: '',
      text: L('——你的最终选择是？', "— What's your final choice?"),
      choices: [
        {
          label: L('坚持和 Elias 北上，赴远方', 'Go North with Elias — journey far'),
          next: 'end_north',
          effects: { ending: 'go_north', carryItem: 'old_map' }
        },
        {
          label: L('留在城市，陪伴众人', 'Stay in the city — be with everyone'),
          next: 'end_home',
          effects: { ending: 'return_home' }
        },
        {
          label: L('不依附任何一方，独自开辟新路', 'Take neither side — forge a new path alone'),
          next: 'end_unknown',
          effects: { ending: 'unknown_path', carryItem: 'blank_notebook' }
        },
        {
          label: L('暂时停下，独自沉淀思考', 'Pause for now — sit with it alone'),
          next: 'end_pause',
          effects: { ending: 'pause_journey' }
        }
      ]
    },
    end_north: {
      speaker: '',
      text: L('你转身望向北方。远方的灯火在夜色里格外明亮——那是一条早已约定好的路。',
              'You turn to face the North. The distant lights burn brighter in the dark — a road long since promised.')
    },
    end_home: {
      speaker: '',
      text: L('你看向脚下的老街。每一盏灯、每一条巷子，都是你长大的痕迹——这里就是你的根。',
              "You look down at the Old Street beneath you. Every light, every alley — they're the marks of your growing up. This is where you're rooted.")
    },
    end_unknown: {
      speaker: '',
      text: L('你独自走向一条无名小路。既非北上，也非留守——你要走出属于自己的方向。',
              'You walk alone down an unnamed path. Not North, not staying — a direction all your own.')
    },
    end_pause: {
      speaker: '',
      text: L('你在屋顶坐下，没有立刻做决定。夜风渐凉，你需要一些时间，独自沉淀。',
              'You sit down on the rooftop, not deciding just yet. The night wind is cooling — you need some time, alone with your thoughts.')
    }
  }
};
