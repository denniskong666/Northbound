// 对话系统：管理对话流程（行/跳转/选择）
// 负责：对话框 UI、打字机效果、选项展示与选择、移动锁定回调
// 数据驱动：对话以 DialogueData 形式注入，便于后续替换正式台词

import Phaser from 'phaser';
import { GameState, ChoiceEffects } from '../state/GameState';

// 一次对话中的一个选项
export interface DialogueChoice {
  label: string;                       // 选项文字
  next?: string;                       // 跳转的节点 id（无则结束对话）
  effects?: ChoiceEffects;             // 选择此项时应用的影响（好感/倾向）
}

// 一次对话中的一个节点（一句/段台词）
export interface DialogueNode {
  speaker?: string;                    // 说话者名（留空表示旁白）
  text: string;                        // 文本内容
  next?: string;                       // 自动跳转的下一节点 id
  choices?: DialogueChoice[];          // 选项（若有则等待玩家选择）
  effects?: ChoiceEffects;             // 进入此节点时自动应用的影响
}

// 一段对话
export interface DialogueData {
  id: string;
  start: string;                       // 起始节点 id
  nodes: Record<string, DialogueNode>;
}

// 宿主接口：场景需提供锁定/解锁输入的回调
export interface DialogueHost {
  scene: Phaser.Scene;
  onLockInput: () => void;
  onUnlockInput: () => void;
}

const TYPE_SPEED = 28;   // 每字符 ms
const BOX_H = 200;       // 对话框高度
const BOX_PAD = 16;
const BOX_MARGIN_X = 40;
const BOX_MARGIN_BOTTOM = 20;

export class DialogueSystem {
  private active = false;
  private data: DialogueData | null = null;
  private currentNode: DialogueNode | null = null;
  // 动态节点改写钩子：start 时传入，在 gotoNode 之前调用
  // 可根据节点 id 改写该节点的 speaker/text/choices，支持根据玩家状态动态分支
  private nodeHook?: (nodeId: string, node: DialogueNode) => DialogueNode | null;

  private typing = false;
  private fullText = '';
  private typedChars = 0;
  private typeTimer: Phaser.Time.TimerEvent | null = null;

  // UI
  private container!: Phaser.GameObjects.Container;
  private box!: Phaser.GameObjects.Image;
  private speakerText!: Phaser.GameObjects.Text;
  private bodyText!: Phaser.GameObjects.Text;
  private hint!: Phaser.GameObjects.Text;
  private choiceContainer!: Phaser.GameObjects.Container;
  private choiceTexts: Phaser.GameObjects.Text[] = [];
  private choiceCursor = 0;
  private portrait!: Phaser.GameObjects.Image;
  private currentPortraitKey: string | null = null;
  private breathingTween?: Phaser.Tweens.Tween;
  private textOffsetX = BOX_PAD;   // 正文左偏移（有立绘时让出空间）

  // 输入
  private keySpace!: Phaser.Input.Keyboard.Key;
  private keyEnter!: Phaser.Input.Keyboard.Key;
  private keyUp!: Phaser.Input.Keyboard.Key;
  private keyDown!: Phaser.Input.Keyboard.Key;
  private keyW!: Phaser.Input.Keyboard.Key;
  private keyS!: Phaser.Input.Keyboard.Key;

  private host: DialogueHost;
  private onComplete?: () => void;

  constructor(host: DialogueHost) {
    this.host = host;
    this.buildBoxTexture();
    this.buildUI();
    this.bindInput();
    this.bindMouse();
  }

  isActive(): boolean { return this.active; }

  // 生成对话框底纹纹理（一次性）
  private buildBoxTexture(): void {
    const scene = this.host.scene;
    const W = scene.scale.width;
    const boxW = W - BOX_MARGIN_X * 2;
    if (scene.textures.exists('dlg_box')) scene.textures.remove('dlg_box');
    const g = scene.make.graphics({ x: 0, y: 0 }, false);
    g.fillStyle(0x0f0d12, 0.94);
    g.fillRoundedRect(0, 0, boxW, BOX_H, 12);
    g.lineStyle(2, 0xf5c97a, 0.55);
    g.strokeRoundedRect(1, 1, boxW - 2, BOX_H - 2, 12);
    g.generateTexture('dlg_box', boxW, BOX_H);
    g.destroy();
  }

  private buildUI(): void {
    const scene = this.host.scene;
    const W = scene.scale.width;
    const H = scene.scale.height;
    const boxW = W - BOX_MARGIN_X * 2;
    const boxX = BOX_MARGIN_X;
    const boxY = H - BOX_H - BOX_MARGIN_BOTTOM;

    this.container = scene.add.container(0, 0)
      .setDepth(300)
      .setScrollFactor(0)
      .setVisible(false);

    this.box = scene.add.image(boxX + boxW / 2, boxY + BOX_H / 2, 'dlg_box').setOrigin(0.5);
    // 对话立绘（左侧头肩像，说话时呼吸缩放）
    this.portrait = scene.add.image(boxX + BOX_PAD + 36, boxY + BOX_H / 2, '__none')
      .setOrigin(0.5).setVisible(false).setAlpha(0);
    this.speakerText = scene.add.text(boxX + BOX_PAD, boxY + 10, '', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '16px',
      color: '#f5c97a',
      fontStyle: 'bold'
    });
    this.bodyText = scene.add.text(boxX + BOX_PAD, boxY + 38, '', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '16px',
      color: '#f0ece0',
      lineSpacing: 4,
      wordWrap: { width: boxW - BOX_PAD * 2 }
    });
    this.hint = scene.add.text(boxX + boxW - BOX_PAD, boxY + BOX_H - 22, '空格 继续', {
      fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
      fontSize: '12px',
      color: '#8a8275'
    }).setOrigin(1, 0);
    this.choiceContainer = scene.add.container(0, 0).setVisible(false);

    this.container.add([this.box, this.portrait, this.speakerText, this.bodyText, this.hint, this.choiceContainer]);
  }

  // 说话者名 → 立绘纹理 key（无匹配返回 null，表示旁白不显示立绘）
  private speakerToPortrait(speaker: string): string | null {
    const map: Record<string, string> = {
      '伊莱亚斯': 'elias_portrait',
      '玛雅': 'maya_portrait',
      '诺亚': 'noah_portrait',
      '利奥': 'leo_portrait',
      '杰米': 'player_portrait'
    };
    return map[speaker] ?? null;
  }

  // 根据是否有立绘调整正文/说话者水平位置与换行宽度
  private layoutText(withPortrait: boolean): void {
    const boxW = this.host.scene.scale.width - BOX_MARGIN_X * 2;
    if (withPortrait) {
      this.textOffsetX = BOX_PAD + 80; // 立绘宽 72 + 间距 8
    } else {
      this.textOffsetX = BOX_PAD;
    }
    const boxX = BOX_MARGIN_X;
    this.speakerText.setX(boxX + this.textOffsetX);
    this.bodyText.setX(boxX + this.textOffsetX);
    this.bodyText.setWordWrapWidth(boxW - this.textOffsetX - BOX_PAD, true);
  }

  // 切换立绘：旧立绘淡出 → 新立绘弹入 → 启动呼吸缩放
  private swapPortrait(key: string | null): void {
    const scene = this.host.scene;
    this.breathingTween?.stop();
    if (!key) {
      // 旁白：隐藏立绘
      this.currentPortraitKey = null;
      this.layoutText(false);
      scene.tweens.add({
        targets: this.portrait,
        alpha: 0, scale: 0.85,
        duration: 140, ease: 'Sine.easeIn',
        onComplete: () => this.portrait.setVisible(false)
      });
      return;
    }
    this.layoutText(true);
    if (this.currentPortraitKey === key) return; // 同一说话者，不重复切换
    this.currentPortraitKey = key;

    const showNew = () => {
      this.portrait.setVisible(true);
      this.portrait.setTexture(key);
      this.portrait.setAlpha(0).setScale(0.85);
      scene.tweens.add({
        targets: this.portrait,
        alpha: { from: 0, to: 1 },
        scale: { from: 0.85, to: 1 },
        duration: 220, ease: 'Sine.easeOut',
        onComplete: () => this.startBreathing()
      });
    };

    if (this.portrait.visible && this.portrait.alpha > 0.1) {
      scene.tweens.add({
        targets: this.portrait,
        alpha: 0, scale: 0.85,
        duration: 120, ease: 'Sine.easeIn',
        onComplete: showNew
      });
    } else {
      showNew();
    }
  }

  // 立绘呼吸：轻微缩放循环，让角色"活着"
  private startBreathing(): void {
    this.breathingTween?.stop();
    this.breathingTween = this.host.scene.tweens.add({
      targets: this.portrait,
      scaleX: { from: 1, to: 1.035 },
      scaleY: { from: 1, to: 1.035 },
      duration: 1500,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    });
  }

  private bindInput(): void {
    const kb = this.host.scene.input.keyboard!;
    this.keySpace = kb.addKey(Phaser.Input.Keyboard.KeyCodes.SPACE);
    this.keyEnter = kb.addKey(Phaser.Input.Keyboard.KeyCodes.ENTER);
    this.keyUp = kb.addKey(Phaser.Input.Keyboard.KeyCodes.UP);
    this.keyDown = kb.addKey(Phaser.Input.Keyboard.KeyCodes.DOWN);
    this.keyW = kb.addKey(Phaser.Input.Keyboard.KeyCodes.W);
    this.keyS = kb.addKey(Phaser.Input.Keyboard.KeyCodes.S);
  }

  // 鼠标点击支持：点击跳过打字机/推进对话，点击选项直接选择
  private bindMouse(): void {
    this.host.scene.input.on('pointerdown', () => {
      if (!this.active) return;
      if (this.typing) { this.skipTyping(); return; }
      // 选项展示时由选项自身的 interactive 处理，不在此推进
      if (this.currentNode?.choices && this.currentNode.choices.length > 0) return;
      this.advance();
    });
  }

  // 开始一段对话（可选 nodeHook：按节点 id 动态改写节点，支持根据玩家状态分支）
  start(data: DialogueData, onComplete?: () => void, nodeHook?: (nodeId: string, node: DialogueNode) => DialogueNode | null): void {
    if (this.active) return;
    this.active = true;
    this.data = data;
    this.nodeHook = nodeHook;
    this.onComplete = onComplete;
    this.host.onLockInput();
    this.container.setVisible(true);
    this.container.setAlpha(0);
    this.host.scene.tweens.add({
      targets: this.container,
      alpha: 1,
      duration: 200,
      ease: 'Sine.easeOut'
    });
    this.gotoNode(data.start);
  }

  private gotoNode(id: string): void {
    const baseNode = this.data?.nodes[id];
    if (!baseNode) { this.end(); return; }
    const node = this.nodeHook ? (this.nodeHook(id, baseNode) ?? baseNode) : baseNode;
    this.currentNode = node;

    // 进入节点时自动应用影响（如剧情 flag）
    if (node.effects) GameState.inst.applyEffects(node.effects);

    const speaker = node.speaker ?? '';
    this.speakerText.setText(speaker);
    // 切换立绘（说话者变化时弹入，旁白时隐藏）
    this.swapPortrait(this.speakerToPortrait(speaker));
    this.bodyText.setText('');
    this.clearChoices();
    this.choiceContainer.setVisible(false);
    this.hint.setText('空格/点击 继续');

    // 打字机
    this.fullText = node.text;
    this.typedChars = 0;
    this.typing = true;
    this.startTyping();
  }

  private startTyping(): void {
    this.typeTimer?.remove();
    this.typeTimer = this.host.scene.time.addEvent({
      delay: TYPE_SPEED,
      loop: true,
      callback: () => {
        if (!this.typing) return;
        this.typedChars++;
        this.bodyText.setText(this.fullText.slice(0, this.typedChars));
        if (this.typedChars >= this.fullText.length) {
          this.typing = false;
          this.typeTimer?.remove();
          this.onTypeDone();
        }
      }
    });
  }

  private skipTyping(): void {
    this.typing = false;
    this.typeTimer?.remove();
    this.bodyText.setText(this.fullText);
    this.onTypeDone();
  }

  private onTypeDone(): void {
    const node = this.currentNode!;
    if (node.choices && node.choices.length > 0) {
      this.showChoices(node.choices);
      this.hint.setText('↑↓/鼠标 选择  回车/点击 确认');
    } else {
      this.hint.setText('空格/点击 继续');
    }
  }

  private showChoices(choices: DialogueChoice[]): void {
    this.clearChoices();
    this.choiceCursor = 0;
    const scene = this.host.scene;
    const H = scene.scale.height;
    const boxX = BOX_MARGIN_X;
    // 选项紧跟正文下方，至少留出可视区域；水平对齐正文（让出立绘空间）
    const bodyBottom = this.bodyText.y + (this.bodyText.height || 20);
    const minTop = H - BOX_H - BOX_MARGIN_BOTTOM + 100;
    let startY = Math.max(bodyBottom + 12, minTop);

    choices.forEach((c, i) => {
      const t = scene.add.text(boxX + this.textOffsetX + 4, startY + i * 26, '', {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '15px',
        color: '#a89e8a',
        padding: { x: 4, y: 2 }
      });
      t.setInteractive({ useHandCursor: true });
      t.on('pointerover', () => {
        this.choiceCursor = i;
        this.updateChoiceHighlight();
      });
      t.on('pointerdown', () => {
        this.choose(i);
      });
      this.choiceTexts.push(t);
      this.choiceContainer.add(t);
    });
    this.choiceContainer.setVisible(true);
    this.updateChoiceHighlight();
  }

  private clearChoices(): void {
    for (const t of this.choiceTexts) t.destroy();
    this.choiceTexts = [];
  }

  private updateChoiceHighlight(): void {
    const choices = this.currentNode?.choices ?? [];
    this.choiceTexts.forEach((t, i) => {
      const c = choices[i];
      if (i === this.choiceCursor) {
        t.setColor('#f5c97a');
        t.setText(`▶ ${c?.label ?? ''}`);
      } else {
        t.setColor('#a89e8a');
        t.setText(`  ${c?.label ?? ''}`);
      }
    });
  }

  private advance(): void {
    const node = this.currentNode!;
    if (node.next) this.gotoNode(node.next);
    else this.end();
  }

  private choose(idx: number): void {
    const node = this.currentNode!;
    if (!node.choices) return;
    const c = node.choices[idx];
    if (!c) return;
    if (c.effects) GameState.inst.applyEffects(c.effects);
    if (c.next) this.gotoNode(c.next);
    else this.end();
  }

  private end(): void {
    this.active = false;
    this.typeTimer?.remove();
    this.breathingTween?.stop();
    this.breathingTween = undefined;
    this.currentPortraitKey = null;
    this.portrait.setVisible(false);
    this.clearChoices();
    this.container.setVisible(false);
    this.data = null;
    this.currentNode = null;
    this.host.onUnlockInput();
    const cb = this.onComplete;
    this.onComplete = undefined;
    cb?.();
  }

  // 由场景 update 调用，处理对话输入
  update(): void {
    if (!this.active) return;

    if (Phaser.Input.Keyboard.JustDown(this.keySpace) || Phaser.Input.Keyboard.JustDown(this.keyEnter)) {
      if (this.typing) {
        this.skipTyping();
      } else if (this.currentNode?.choices && this.currentNode.choices.length > 0) {
        this.choose(this.choiceCursor);
      } else {
        this.advance();
      }
      return;
    }

    if (!this.typing && this.currentNode?.choices && this.currentNode.choices.length > 0) {
      if (Phaser.Input.Keyboard.JustDown(this.keyUp) || Phaser.Input.Keyboard.JustDown(this.keyW)) {
        this.choiceCursor = (this.choiceCursor - 1 + this.choiceTexts.length) % this.choiceTexts.length;
        this.updateChoiceHighlight();
      } else if (Phaser.Input.Keyboard.JustDown(this.keyDown) || Phaser.Input.Keyboard.JustDown(this.keyS)) {
        this.choiceCursor = (this.choiceCursor + 1) % this.choiceTexts.length;
        this.updateChoiceHighlight();
      }
    }
  }
}
