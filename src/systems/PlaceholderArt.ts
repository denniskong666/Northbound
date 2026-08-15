import Phaser from 'phaser';

// 占位美术生成器：用 Canvas 程序化生成剪影角色与 tile 贴图
// 风格：极简剪影 + 情绪色块。后续外包美术到位后替换即可。

const FRAME = 48;
const FRAMES_PER_DIR = 4;
const DIRS = ['down', 'up', 'left', 'right'] as const;

interface CharSpec {
  key: string;
  color: number;   // 主色
  accent: number;  // 朝向/眼睛点缀色
}

// 5 位主要角色配色
const CHARACTERS: CharSpec[] = [
  { key: 'player', color: 0xe8e4d8, accent: 0xf5c97a }, // 主角 暖白
  { key: 'elias',  color: 0x6b8cae, accent: 0xb8d4f0 }, // Elias 钢蓝
  { key: 'maya',   color: 0xe07a5f, accent: 0xf2cc8f }, // Maya 珊瑚
  { key: 'noah',   color: 0x81b29a, accent: 0xe07a5f }, // Noah 青绿
  { key: 'leo',    color: 0xe6b85c, accent: 0xc77dff }  // Leo 琥珀
];

function toCss(hex: number): string {
  return '#' + hex.toString(16).padStart(6, '0');
}

// 调整颜色明度，amt 负值变暗
function shade(hex: number, amt: number): string {
  const c = hex.toString(16).padStart(6, '0');
  const r = Math.max(0, Math.min(255, parseInt(c.substr(0, 2), 16) + Math.round(255 * amt)));
  const g = Math.max(0, Math.min(255, parseInt(c.substr(2, 2), 16) + Math.round(255 * amt)));
  const b = Math.max(0, Math.min(255, parseInt(c.substr(4, 2), 16) + Math.round(255 * amt)));
  return `rgb(${r},${g},${b})`;
}

function roundedRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

// 绘制单帧角色剪影（ox, oy 为帧左上角在画布上的偏移）
function drawCharFrame(
  ctx: CanvasRenderingContext2D,
  ox: number, oy: number,
  spec: CharSpec,
  dir: typeof DIRS[number],
  frame: number
) {
  ctx.save();
  ctx.translate(ox, oy);

  // 脚下投影
  ctx.fillStyle = 'rgba(0,0,0,0.35)';
  ctx.beginPath();
  ctx.ellipse(24, 44, 9, 3, 0, 0, Math.PI * 2);
  ctx.fill();

  // 行走 bob（上下浮动）
  const bob = (frame === 0 || frame === 2) ? -1 : 0;
  ctx.translate(0, bob);

  // 腿部摆动
  const swing = frame === 0 ? -2 : frame === 2 ? 2 : 0;
  ctx.fillStyle = shade(spec.color, -0.32);
  roundedRect(ctx, 19 + swing, 33, 5, 9, 2); ctx.fill();
  roundedRect(ctx, 24 - swing, 33, 5, 9, 2); ctx.fill();

  // 躯干（披风式剪影）
  ctx.fillStyle = toCss(spec.color);
  roundedRect(ctx, 16, 21, 16, 15, 6); ctx.fill();
  // 躯干高光
  ctx.fillStyle = shade(spec.color, 0.12);
  roundedRect(ctx, 18, 22, 4, 11, 2); ctx.fill();

  // 头部
  ctx.fillStyle = toCss(spec.color);
  ctx.beginPath();
  ctx.arc(24, 16, 7, 0, Math.PI * 2);
  ctx.fill();
  // 头部高光
  ctx.fillStyle = shade(spec.color, 0.18);
  ctx.beginPath();
  ctx.arc(22, 14, 2.5, 0, Math.PI * 2);
  ctx.fill();

  // 朝向指示（眼睛点）
  ctx.fillStyle = toCss(spec.accent);
  if (dir === 'down') {
    ctx.fillRect(21, 16, 2, 2);
    ctx.fillRect(25, 16, 2, 2);
  } else if (dir === 'up') {
    // 背面，仅一点后脑高光
    ctx.fillStyle = 'rgba(255,255,255,0.12)';
    ctx.beginPath();
    ctx.arc(24, 14, 4.5, 0, Math.PI * 2);
    ctx.fill();
  } else if (dir === 'left') {
    ctx.fillRect(18, 16, 2, 2);
  } else { // right
    ctx.fillRect(28, 16, 2, 2);
  }

  ctx.restore();
}

export class PlaceholderArt {
  constructor(private scene: Phaser.Scene) {}

  generateAll(): void {
    this.makeCharacters();
    this.makeTiles();
    this.makeMarker();
    this.makeSpark();
    this.makePortraits();
    this.makeCharacterAnimations();
  }

  // 收集反馈用的小光点纹理
  private makeSpark(): void {
    const size = 16;
    const tex = this.scene.textures.createCanvas('spark', size, size);
    if (!tex) return;
    const ctx = tex.getContext();
    const cx = size / 2, cy = size / 2;
    const grad = ctx.createRadialGradient(cx, cy, 0, cx, cy, size / 2);
    grad.addColorStop(0, 'rgba(255,243,214,1)');
    grad.addColorStop(0.4, 'rgba(245,201,122,0.85)');
    grad.addColorStop(1, 'rgba(245,201,122,0)');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(cx, cy, size / 2, 0, Math.PI * 2);
    ctx.fill();
    tex.refresh();
  }

  // 生成角色对话立绘（头肩大图，用于对话框左侧）
  private makePortraits(): void {
    for (const spec of CHARACTERS) {
      const W = 72, H = 88;
      const tex = this.scene.textures.createCanvas(`${spec.key}_portrait`, W, H);
      if (!tex) continue;
      const ctx = tex.getContext();

      // 背景：深色圆角
      ctx.fillStyle = 'rgba(15,13,18,0.96)';
      roundedRect(ctx, 0, 0, W, H, 10);
      ctx.fill();

      // 顶部色光（角色主色微泛）
      const grad = ctx.createLinearGradient(0, 0, 0, H);
      grad.addColorStop(0, shade(spec.color, 0.18));
      grad.addColorStop(0.55, 'rgba(0,0,0,0)');
      ctx.globalAlpha = 0.28;
      ctx.fillStyle = grad;
      roundedRect(ctx, 2, 2, W - 4, H - 4, 8);
      ctx.fill();
      ctx.globalAlpha = 1;

      // 内描边
      ctx.strokeStyle = shade(spec.color, 0.25);
      ctx.lineWidth = 1.5;
      roundedRect(ctx, 1.5, 1.5, W - 3, H - 3, 9);
      ctx.stroke();

      // 角色头肩（放大正面像）
      ctx.save();
      ctx.translate(W / 2, H / 2 + 8);
      const s = 1.7;
      ctx.scale(s, s);

      // 肩部投影
      ctx.fillStyle = 'rgba(0,0,0,0.45)';
      ctx.beginPath();
      ctx.ellipse(0, 22, 13, 3, 0, 0, Math.PI * 2);
      ctx.fill();

      // 躯干外层（暗）
      ctx.fillStyle = shade(spec.color, -0.1);
      roundedRect(ctx, -12, 8, 24, 18, 6); ctx.fill();
      // 躯干主色
      ctx.fillStyle = toCss(spec.color);
      roundedRect(ctx, -10, 9, 20, 16, 5); ctx.fill();
      // 躯干高光
      ctx.fillStyle = shade(spec.color, 0.18);
      roundedRect(ctx, -8, 10, 3.5, 12, 2); ctx.fill();

      // 头部
      ctx.fillStyle = toCss(spec.color);
      ctx.beginPath();
      ctx.arc(0, -4, 11, 0, Math.PI * 2);
      ctx.fill();
      // 头部高光
      ctx.fillStyle = shade(spec.color, 0.22);
      ctx.beginPath();
      ctx.arc(-3, -7, 4, 0, Math.PI * 2);
      ctx.fill();

      // 眼睛（正面）
      ctx.fillStyle = toCss(spec.accent);
      ctx.fillRect(-5, -4, 2.5, 2.5);
      ctx.fillRect(2.5, -4, 2.5, 2.5);

      ctx.restore();

      tex.refresh();
    }
  }

  // 生成 5 个角色 spritesheet（带命名帧）
  private makeCharacters(): void {
    for (const spec of CHARACTERS) {
      const w = FRAME * FRAMES_PER_DIR;
      const h = FRAME * DIRS.length;
      const tex = this.scene.textures.createCanvas(spec.key, w, h);
      if (!tex) continue;
      const ctx = tex.getContext();

      DIRS.forEach((dir, row) => {
        for (let f = 0; f < FRAMES_PER_DIR; f++) {
          drawCharFrame(ctx, f * FRAME, row * FRAME, spec, dir, f);
        }
      });
      tex.refresh();

      // 注册命名帧：{dir}_{frame}
      const texture = this.scene.textures.get(spec.key);
      DIRS.forEach((dir, row) => {
        for (let f = 0; f < FRAMES_PER_DIR; f++) {
          texture.add(`${dir}_${f}`, 0, f * FRAME, row * FRAME, FRAME, FRAME);
        }
      });
    }
  }

  // 生成 tile 贴图 — Inmost 风格：深沉暗色、裂纹、污渍、青苔
  private makeTiles(): void {
    const tiles: Array<{ key: string; base: number; dark: number; light: number; accent: number }> = [
      // 老街区地面：龟裂沥青+青苔
      { key: 'tile_ground', base: 0x2a2520, dark: 0x1a1612, light: 0x3a3228, accent: 0x3d4a2a },
      // 道路：湿冷石板
      { key: 'tile_road',   base: 0x1a1820, dark: 0x100e14, light: 0x242028, accent: 0x2a3040 },
      // 建筑墙体：斑驳砖墙
      { key: 'tile_wall',   base: 0x141218, dark: 0x0a080c, light: 0x1e1a24, accent: 0x2a2020 },
      // 屋顶：水泥+裂缝
      { key: 'tile_roof',   base: 0x1e2434, dark: 0x121620, light: 0x283044, accent: 0x3a3a2a },
      // 修车棚：油渍地面
      { key: 'tile_garage', base: 0x1c1a16, dark: 0x100e0a, light: 0x26221c, accent: 0x2a2014 },
      // —— 序章专用：阳光明媚的暖色变体（体现出游兴奋）——
      // 地面：温暖沙土色，阳光照射的街道
      { key: 'tile_ground_ch0', base: 0x7a6a4e, dark: 0x5a4e3a, light: 0x968268, accent: 0x6a8238 },
      // 墙体：阳光下的暖砖墙
      { key: 'tile_wall_ch0',   base: 0x524232, dark: 0x3a2e22, light: 0x6a5644, accent: 0x6a5028 },
      // 道路：浅色暖石板
      { key: 'tile_road_ch0',   base: 0x5a5260, dark: 0x443e4a, light: 0x6e6678, accent: 0x4a5a5a }
    ];

    for (const t of tiles) {
      const tex = this.scene.textures.createCanvas(t.key, FRAME, FRAME);
      if (!tex) continue;
      const ctx = tex.getContext();
      const rng = mulberry(hashStr(t.key));

      // 底色渐变（上暗下亮，模拟环境光）
      const grad = ctx.createLinearGradient(0, 0, 0, FRAME);
      grad.addColorStop(0, toCss(t.dark));
      grad.addColorStop(0.5, toCss(t.base));
      grad.addColorStop(1, toCss(t.dark));
      ctx.fillStyle = grad;
      ctx.fillRect(0, 0, FRAME, FRAME);

      // 颗粒噪点
      for (let i = 0; i < 80; i++) {
        const x = rng() * FRAME;
        const y = rng() * FRAME;
        const sz = rng() < 0.3 ? 2 : 1;
        ctx.fillStyle = rng() < 0.5 ? toCss(t.light) : toCss(t.dark);
        ctx.globalAlpha = 0.3 + rng() * 0.4;
        ctx.fillRect(x, y, sz, sz);
      }
      ctx.globalAlpha = 1;

      // 裂纹（2-3条随机走向）
      for (let c = 0; c < 3; c++) {
        ctx.strokeStyle = toCss(t.dark);
        ctx.globalAlpha = 0.5 + rng() * 0.3;
        ctx.lineWidth = rng() < 0.5 ? 1 : 0.5;
        ctx.beginPath();
        let cx = rng() * FRAME, cy = rng() * FRAME;
        ctx.moveTo(cx, cy);
        const segments = 3 + Math.floor(rng() * 3);
        for (let s = 0; s < segments; s++) {
          cx += (rng() - 0.5) * 16;
          cy += (rng() - 0.5) * 16;
          ctx.lineTo(cx, cy);
        }
        ctx.stroke();
      }
      ctx.globalAlpha = 1;

      // 污渍/油迹（2-3个不规则斑块）
      for (let s = 0; s < 3; s++) {
        const sx = rng() * FRAME, sy = rng() * FRAME;
        const sr = 3 + rng() * 8;
        ctx.fillStyle = toCss(t.dark);
        ctx.globalAlpha = 0.15 + rng() * 0.2;
        ctx.beginPath();
        ctx.ellipse(sx, sy, sr, sr * 0.7, rng() * Math.PI, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.globalAlpha = 1;

      // 青苔/锈迹点缀（accent 色，少量）
      for (let m = 0; m < 8; m++) {
        const mx = rng() * FRAME, my = rng() * FRAME;
        ctx.fillStyle = toCss(t.accent);
        ctx.globalAlpha = 0.12 + rng() * 0.2;
        ctx.fillRect(mx, my, 1 + rng() * 2, 1);
      }
      ctx.globalAlpha = 1;

      // 极细内边框
      ctx.strokeStyle = 'rgba(0,0,0,0.4)';
      ctx.lineWidth = 1;
      ctx.strokeRect(0.5, 0.5, FRAME - 1, FRAME - 1);

      tex.refresh();
    }
  }

  // 可交互点标记（发光圆点）
  private makeMarker(): void {
    const size = 32;
    const tex = this.scene.textures.createCanvas('marker', size, size);
    if (!tex) return;
    const ctx = tex.getContext();
    const cx = size / 2, cy = size / 2;

    const grad = ctx.createRadialGradient(cx, cy, 0, cx, cy, 14);
    grad.addColorStop(0, 'rgba(245,201,122,0.9)');
    grad.addColorStop(0.5, 'rgba(245,201,122,0.35)');
    grad.addColorStop(1, 'rgba(245,201,122,0)');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(cx, cy, 14, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#fff3d6';
    ctx.beginPath();
    ctx.arc(cx, cy, 3, 0, Math.PI * 2);
    ctx.fill();

    tex.refresh();
  }

  // 为每个角色注册 4 方向行走动画
  private makeCharacterAnimations(): void {
    for (const spec of CHARACTERS) {
      for (const dir of DIRS) {
        this.scene.anims.create({
          key: `${spec.key}_walk_${dir}`,
          frames: this.scene.anims.generateFrameNames(spec.key, {
            prefix: `${dir}_`,
            start: 0,
            end: FRAMES_PER_DIR - 1
          }),
          frameRate: 8,
          repeat: -1
        });
      }
    }
  }
}

// ---- 小工具：确定性随机（让 tile 纹理稳定不抖） ----
function hashStr(s: string): number {
  let h = 2166136261;
  for (let i = 0; i < s.length; i++) {
    h ^= s.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return h >>> 0;
}

function mulberry(seed: number): () => number {
  let a = seed;
  return () => {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
