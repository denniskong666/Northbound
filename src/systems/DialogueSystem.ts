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

    this.container.add([this.box, this.speakerText, this.bodyText, this.hint, this.choiceContainer]);
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

  // 开始一段对话
  start(data: DialogueData, onComplete?: () => void): void {
    if (this.active) return;
    this.active = true;
    this.data = data;
    this.onComplete = onComplete;
    this.host.onLockInput();
    this.container.setVisible(true);
    this.gotoNode(data.start);
  }

  private gotoNode(id: string): void {
    const node = this.data?.nodes[id];
    if (!node) { this.end(); return; }
    this.currentNode = node;

    // 进入节点时自动应用影响（如剧情 flag）
    if (node.effects) GameState.inst.applyEffects(node.effects);

    this.speakerText.setText(node.speaker ?? '');
    this.bodyText.setText('');
    this.clearChoices();
    this.choiceContainer.setVisible(false);
    this.hint.setText('空格 继续');

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
      this.hint.setText('↑↓ 选择  回车 确认');
    } else {
      this.hint.setText('空格 继续');
    }
  }

  private showChoices(choices: DialogueChoice[]): void {
    this.clearChoices();
    this.choiceCursor = 0;
    const scene = this.host.scene;
    const H = scene.scale.height;
    const boxX = BOX_MARGIN_X;
    // 选项紧跟正文下方，至少留出可视区域
    const bodyBottom = this.bodyText.y + (this.bodyText.height || 20);
    const minTop = H - BOX_H - BOX_MARGIN_BOTTOM + 100;
    let startY = Math.max(bodyBottom + 12, minTop);

    choices.forEach((c, i) => {
      const t = scene.add.text(boxX + BOX_PAD + 4, startY + i * 26, '', {
        fontFamily: '"PingFang SC","Microsoft YaHei",sans-serif',
        fontSize: '15px',
        color: '#a89e8a',
        padding: { x: 4, y: 2 }
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
