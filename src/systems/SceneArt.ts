import Phaser from 'phaser';

// Inmost 风格场景装饰系统
// 程序化生成暗色氛围道具纹理 + 光照效果
// 所有装饰物均为深色剪影 + 微弱暖光，营造孤寂荒凉感

function toCss(hex: number): string {
  return '#' + hex.toString(16).padStart(6, '0');
}

function shade(hex: number, amt: number): string {
  const c = hex.toString(16).padStart(6, '0');
  const r = Math.max(0, Math.min(255, parseInt(c.substr(0, 2), 16) + Math.round(255 * amt)));
  const g = Math.max(0, Math.min(255, parseInt(c.substr(2, 2), 16) + Math.round(255 * amt)));
  const b = Math.max(0, Math.min(255, parseInt(c.substr(4, 2), 16) + Math.round(255 * amt)));
  return `rgb(${r},${g},${b})`;
}

export class SceneArt {
  constructor(private scene: Phaser.Scene) {}

  /** 生成所有装饰纹理（在场景 create 时调用一次） */
  generateAll(): void {
    this.makeStreetLamp();
    this.makeCrate();
    this.makeTrashCan();
    this.makePoster();
    this.makePuddle();
    this.makePipe();
    this.makeToolRack();
    this.makeHangingLight();
    this.makeWorkbench();
    this.makeTire();
    this.makeAntenna();
    this.makeACUnit();
    this.makeRailing();
    this.makeWindow();
    this.makeBookshelf();
    this.makeBox();
    this.makeLampGlow();
    // 关键物品：餐桌 / 旅行轿车 / 玛雅画作 / 墙面旧地图
    this.makeTable();
    this.makeCar();
    this.makePainting();
    this.makeWallMap();
    // 序章新物品：明信片 / 北方宣传看板 / 愿望墙
    this.makePostcard();
    this.makeNorthBoard();
    this.makeWishWall();
    // 序章小游戏：玩家愿望便签（5 种选择变体）、北方灯火光点
    this.makeWishNoteVariants();
    this.makeNBLightDot();
    // 序章装饰：北方招工海报 / 美术馆征稿海报
    this.makeNpRecruitPoster();
    this.makeGalleryPoster();
  }

  // —— 街灯（老街区） ——
  private makeStreetLamp(): void {
    const W = 24, H = 80;
    const tex = this.scene.textures.createCanvas('deco_lamp', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 灯杆
    ctx.fillStyle = '#0e0c10';
    ctx.fillRect(10, 12, 4, 68);
    // 灯杆高光
    ctx.fillStyle = '#1e1a24';
    ctx.fillRect(10, 12, 1, 68);
    // 灯罩
    ctx.fillStyle = '#0e0c10';
    ctx.beginPath();
    ctx.moveTo(6, 8);
    ctx.lineTo(18, 8);
    ctx.lineTo(16, 16);
    ctx.lineTo(8, 16);
    ctx.closePath();
    ctx.fill();
    // 灯泡微光
    ctx.fillStyle = 'rgba(245,201,122,0.7)';
    ctx.beginPath();
    ctx.arc(12, 14, 3, 0, Math.PI * 2);
    ctx.fill();
    // 底座
    ctx.fillStyle = '#0a080c';
    ctx.fillRect(8, 78, 8, 2);

    tex.refresh();
  }

  // —— 木箱 ——
  private makeCrate(): void {
    const S = 36;
    const tex = this.scene.textures.createCanvas('deco_crate', S, S);
    if (!tex) return;
    const ctx = tex.getContext();

    // 主体
    ctx.fillStyle = '#1e1a14';
    ctx.fillRect(2, 2, S - 4, S - 4);
    // 木板纹理
    ctx.strokeStyle = '#100c08';
    ctx.lineWidth = 1;
    for (let i = 8; i < S - 4; i += 7) {
      ctx.beginPath();
      ctx.moveTo(2, i);
      ctx.lineTo(S - 2, i);
      ctx.stroke();
    }
    // 高光边
    ctx.strokeStyle = '#2a2418';
    ctx.strokeRect(2.5, 2.5, S - 5, S - 5);
    // 铁角
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(2, 2, 5, 5);
    ctx.fillRect(S - 7, 2, 5, 5);
    ctx.fillRect(2, S - 7, 5, 5);
    ctx.fillRect(S - 7, S - 7, 5, 5);

    tex.refresh();
  }

  // —— 垃圾桶 ——
  private makeTrashCan(): void {
    const W = 20, H = 28;
    const tex = this.scene.textures.createCanvas('deco_trash', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#0e0c0a';
    ctx.fillRect(4, 6, 12, 22);
    ctx.fillStyle = '#1a1612';
    ctx.fillRect(4, 6, 2, 22);
    // 盖子
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(2, 4, 16, 4);
    // 凹痕
    ctx.fillStyle = '#060404';
    ctx.fillRect(10, 10, 3, 8);

    tex.refresh();
  }

  // —— 撕裂的海报 ——
  private makePoster(): void {
    const W = 20, H = 28;
    const tex = this.scene.textures.createCanvas('deco_poster', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#1a1814';
    ctx.fillRect(2, 2, 14, 24);
    // 撕裂边
    ctx.fillStyle = '#0a0806';
    ctx.beginPath();
    ctx.moveTo(16, 2);
    ctx.lineTo(16, 10);
    ctx.lineTo(12, 14);
    ctx.lineTo(16, 18);
    ctx.lineTo(14, 26);
    ctx.lineTo(16, 26);
    ctx.lineTo(16, 2);
    ctx.fill();
    // 文字色块（模糊）
    ctx.fillStyle = 'rgba(180,160,100,0.15)';
    ctx.fillRect(4, 6, 10, 3);
    ctx.fillRect(4, 12, 8, 2);
    ctx.fillRect(4, 18, 10, 2);

    tex.refresh();
  }

  // —— 水洼 ——
  private makePuddle(): void {
    const W = 48, H = 24;
    const tex = this.scene.textures.createCanvas('deco_puddle', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    const grad = ctx.createRadialGradient(W / 2, H / 2, 2, W / 2, H / 2, W / 2);
    grad.addColorStop(0, 'rgba(40,50,70,0.4)');
    grad.addColorStop(0.6, 'rgba(20,25,35,0.25)');
    grad.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.ellipse(W / 2, H / 2, W / 2 - 2, H / 2 - 2, 0, 0, Math.PI * 2);
    ctx.fill();
    // 反光
    ctx.fillStyle = 'rgba(120,140,170,0.12)';
    ctx.beginPath();
    ctx.ellipse(W / 2 - 6, H / 2 - 2, 8, 2, 0, 0, Math.PI * 2);
    ctx.fill();

    tex.refresh();
  }

  // —— 管道 ——
  private makePipe(): void {
    const W = 48, H = 16;
    const tex = this.scene.textures.createCanvas('deco_pipe', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#141210';
    ctx.fillRect(0, 4, W, 8);
    ctx.fillStyle = '#1e1a16';
    ctx.fillRect(0, 4, W, 2);
    // 锈迹
    ctx.fillStyle = 'rgba(60,30,15,0.3)';
    ctx.fillRect(8, 6, 6, 4);
    ctx.fillRect(28, 8, 8, 3);
    // 接口
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(14, 2, 4, 12);
    ctx.fillRect(30, 2, 4, 12);

    tex.refresh();
  }

  // —— 工具架 ——
  private makeToolRack(): void {
    const W = 48, H = 36;
    const tex = this.scene.textures.createCanvas('deco_rack', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 背板
    ctx.fillStyle = '#0e0c08';
    ctx.fillRect(0, 0, W, H);
    // 挂钩
    ctx.fillStyle = '#1a1612';
    for (let i = 0; i < 4; i++) {
      ctx.fillRect(4 + i * 12, 4, 2, 6);
    }
    // 悬挂工具（剪影）
    ctx.fillStyle = '#060404';
    // 扳手
    ctx.fillRect(4, 10, 2, 18);
    ctx.fillRect(2, 10, 6, 3);
    // 锤子
    ctx.fillRect(16, 10, 2, 18);
    ctx.fillRect(14, 10, 6, 4);
    // 螺丝刀
    ctx.fillRect(28, 10, 2, 18);
    ctx.fillRect(27, 26, 4, 3);
    // 钳子
    ctx.fillRect(40, 10, 2, 18);
    ctx.fillRect(38, 10, 6, 2);

    tex.refresh();
  }

  // —— 悬挂灯（修理厂） ——
  private makeHangingLight(): void {
    const W = 16, H = 48;
    const tex = this.scene.textures.createCanvas('deco_hlight', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 线
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(7, 0, 2, 30);
    // 灯罩
    ctx.fillStyle = '#0e0c0a';
    ctx.beginPath();
    ctx.moveTo(4, 30);
    ctx.lineTo(12, 30);
    ctx.lineTo(10, 38);
    ctx.lineTo(6, 38);
    ctx.closePath();
    ctx.fill();
    // 灯泡
    ctx.fillStyle = 'rgba(245,201,122,0.6)';
    ctx.beginPath();
    ctx.arc(8, 36, 2.5, 0, Math.PI * 2);
    ctx.fill();

    tex.refresh();
  }

  // —— 工作台 ——
  private makeWorkbench(): void {
    const W = 48, H = 28;
    const tex = this.scene.textures.createCanvas('deco_bench', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 台面
    ctx.fillStyle = '#1a1610';
    ctx.fillRect(0, 0, W, 8);
    ctx.fillStyle = '#0e0c08';
    ctx.fillRect(0, 8, W, 2);
    // 桌腿
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(2, 10, 4, 18);
    ctx.fillRect(W - 6, 10, 4, 18);
    // 台面散落物
    ctx.fillStyle = '#060404';
    ctx.fillRect(8, 3, 6, 3);
    ctx.fillRect(20, 2, 4, 5);
    ctx.fillRect(30, 4, 8, 2);

    tex.refresh();
  }

  // —— 轮胎 ——
  private makeTire(): void {
    const S = 28;
    const tex = this.scene.textures.createCanvas('deco_tire', S, S);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#0a0806';
    ctx.beginPath();
    ctx.arc(S / 2, S / 2, S / 2 - 1, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#141210';
    ctx.beginPath();
    ctx.arc(S / 2, S / 2, S / 2 - 4, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#060404';
    ctx.beginPath();
    ctx.arc(S / 2, S / 2, 5, 0, Math.PI * 2);
    ctx.fill();

    tex.refresh();
  }

  // —— 天线（屋顶） ——
  private makeAntenna(): void {
    const W = 32, H = 48;
    const tex = this.scene.textures.createCanvas('deco_antenna', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#0a0806';
    // 主杆
    ctx.fillRect(15, 8, 2, 40);
    // 横臂
    ctx.fillRect(8, 14, 16, 1);
    ctx.fillRect(6, 22, 20, 1);
    ctx.fillRect(8, 30, 16, 1);
    // 底座
    ctx.fillRect(12, 46, 8, 2);

    tex.refresh();
  }

  // —— 空调外机 ——
  private makeACUnit(): void {
    const W = 40, H = 24;
    const tex = this.scene.textures.createCanvas('deco_ac', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#141218';
    ctx.fillRect(0, 0, W, H);
    ctx.fillStyle = '#0a080c';
    // 百叶窗
    for (let i = 0; i < 5; i++) {
      ctx.fillRect(4, 4 + i * 4, 18, 2);
    }
    // 风扇区
    ctx.fillRect(26, 4, 10, 16);
    ctx.fillStyle = '#1e1a24';
    ctx.beginPath();
    ctx.arc(31, 12, 4, 0, Math.PI * 2);
    ctx.fill();
    // 顶部高光
    ctx.fillStyle = '#1e1a24';
    ctx.fillRect(0, 0, W, 1);

    tex.refresh();
  }

  // —— 栏杆 ——
  private makeRailing(): void {
    const W = 48, H = 16;
    const tex = this.scene.textures.createCanvas('deco_rail', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#0a0806';
    // 横杆
    ctx.fillRect(0, 2, W, 2);
    ctx.fillRect(0, 12, W, 2);
    // 竖杆
    for (let i = 4; i < W; i += 8) {
      ctx.fillRect(i, 0, 2, H);
    }
    // 锈迹
    ctx.fillStyle = 'rgba(50,25,10,0.3)';
    ctx.fillRect(12, 2, 6, 2);
    ctx.fillRect(32, 12, 8, 2);

    tex.refresh();
  }

  // —— 窗户（带光） ——
  private makeWindow(): void {
    const W = 32, H = 36;
    const tex = this.scene.textures.createCanvas('deco_window', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 窗框
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(0, 0, W, H);
    // 玻璃微光
    ctx.fillStyle = 'rgba(245,201,122,0.08)';
    ctx.fillRect(3, 3, W - 6, H - 6);
    // 十字框
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(W / 2 - 1, 3, 2, H - 6);
    ctx.fillRect(3, H / 2 - 1, W - 6, 2);
    // 窗台
    ctx.fillStyle = '#141210';
    ctx.fillRect(-2, H - 3, W + 4, 3);

    tex.refresh();
  }

  // —— 书架/柜子 ——
  private makeBookshelf(): void {
    const W = 40, H = 56;
    const tex = this.scene.textures.createCanvas('deco_shelf', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#0e0c08';
    ctx.fillRect(0, 0, W, H);
    // 隔层
    ctx.fillStyle = '#060404';
    ctx.fillRect(0, 14, W, 2);
    ctx.fillRect(0, 28, W, 2);
    ctx.fillRect(0, 42, W, 2);
    // 书/物品剪影
    ctx.fillStyle = '#1a1612';
    ctx.fillRect(4, 4, 3, 8);
    ctx.fillRect(8, 4, 3, 8);
    ctx.fillRect(12, 4, 2, 8);
    ctx.fillRect(16, 4, 4, 8);
    ctx.fillRect(4, 18, 5, 8);
    ctx.fillRect(10, 18, 3, 8);
    ctx.fillRect(4, 32, 4, 8);
    ctx.fillRect(10, 32, 6, 8);
    ctx.fillRect(20, 32, 3, 8);

    tex.refresh();
  }

  // —— 纸箱 ——
  private makeBox(): void {
    const S = 28;
    const tex = this.scene.textures.createCanvas('deco_box', S, S);
    if (!tex) return;
    const ctx = tex.getContext();

    ctx.fillStyle = '#16120c';
    ctx.fillRect(2, 2, S - 4, S - 4);
    ctx.strokeStyle = '#0a0806';
    ctx.lineWidth = 1;
    ctx.strokeRect(2.5, 2.5, S - 5, S - 5);
    // 封箱带
    ctx.fillStyle = '#1e1a14';
    ctx.fillRect(2, S / 2 - 1, S - 4, 2);
    // 高光
    ctx.fillStyle = '#221e16';
    ctx.fillRect(2, 2, S - 4, 1);

    tex.refresh();
  }

  // —— 灯光光晕（叠加用） ——
  private makeLampGlow(): void {
    const S = 96;
    const tex = this.scene.textures.createCanvas('deco_glow', S, S);
    if (!tex) return;
    const ctx = tex.getContext();
    const cx = S / 2, cy = S / 2;
    const grad = ctx.createRadialGradient(cx, cy, 2, cx, cy, S / 2);
    grad.addColorStop(0, 'rgba(245,201,122,0.35)');
    grad.addColorStop(0.3, 'rgba(245,201,122,0.15)');
    grad.addColorStop(0.7, 'rgba(180,140,80,0.04)');
    grad.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, S, S);

    tex.refresh();
  }

  // —— 餐桌（露丝餐厅送餐位） ——
  private makeTable(): void {
    const W = 44, H = 32;
    const tex = this.scene.textures.createCanvas('deco_table', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 桌面（俯视椭圆）
    ctx.fillStyle = '#1c1812';
    ctx.beginPath();
    ctx.ellipse(W / 2, 12, W / 2 - 4, 9, 0, 0, Math.PI * 2);
    ctx.fill();
    // 桌面高光
    ctx.fillStyle = '#2a2418';
    ctx.beginPath();
    ctx.ellipse(W / 2, 10, W / 2 - 7, 5, 0, 0, Math.PI * 2);
    ctx.fill();
    // 桌腿（中心柱）
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(W / 2 - 2, 16, 4, 12);
    // 底座
    ctx.fillRect(W / 2 - 6, 27, 12, 3);
    // 桌面餐具剪影（盘子+杯子）
    ctx.fillStyle = '#0e0c08';
    ctx.beginPath();
    ctx.ellipse(W / 2 - 8, 12, 4, 3, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillRect(W / 2 + 6, 9, 3, 5);
    // 餐具微光
    ctx.fillStyle = 'rgba(245,201,122,0.12)';
    ctx.beginPath();
    ctx.ellipse(W / 2 - 8, 11, 2, 1.2, 0, 0, Math.PI * 2);
    ctx.fill();

    tex.refresh();
  }

  // —— 旅行轿车（修理厂） ——
  private makeCar(): void {
    const W = 96, H = 56;
    const tex = this.scene.textures.createCanvas('deco_car', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 车身阴影（地面投影）
    ctx.fillStyle = 'rgba(0,0,0,0.5)';
    ctx.beginPath();
    ctx.ellipse(W / 2, H - 4, W / 2 - 6, 5, 0, 0, Math.PI * 2);
    ctx.fill();

    // 车身主体（褪色蓝）
    ctx.fillStyle = '#1a2a3a';
    ctx.fillRect(8, 18, W - 16, 24);
    // 车顶
    ctx.fillStyle = '#142030';
    ctx.fillRect(22, 8, W - 44, 14);
    // 车身高光
    ctx.fillStyle = '#243648';
    ctx.fillRect(8, 18, W - 16, 3);
    ctx.fillRect(22, 8, W - 44, 2);
    // 车窗（深色玻璃）
    ctx.fillStyle = '#080810';
    ctx.fillRect(26, 11, W - 52, 9);
    // 车窗反光
    ctx.fillStyle = 'rgba(120,140,170,0.18)';
    ctx.fillRect(28, 12, 14, 3);
    // 车门分割线
    ctx.fillStyle = '#0a1018';
    ctx.fillRect(W / 2, 8, 1, 34);
    // 把手
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(W / 2 - 8, 26, 5, 2);
    ctx.fillRect(W / 2 + 4, 26, 5, 2);
    // 车轮
    ctx.fillStyle = '#060404';
    ctx.beginPath();
    ctx.arc(24, 44, 8, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(W - 24, 44, 8, 0, Math.PI * 2);
    ctx.fill();
    // 轮毂
    ctx.fillStyle = '#1a1612';
    ctx.beginPath();
    ctx.arc(24, 44, 3, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(W - 24, 44, 3, 0, Math.PI * 2);
    ctx.fill();
    // 前大灯（微弱暖光）
    ctx.fillStyle = 'rgba(245,201,122,0.5)';
    ctx.fillRect(W - 12, 24, 4, 4);
    // 车门内侧刻痕（五个名字首字母剪影）
    ctx.fillStyle = 'rgba(180,140,80,0.25)';
    ctx.fillRect(30, 30, 1, 4);
    ctx.fillRect(33, 30, 1, 4);
    ctx.fillRect(36, 30, 1, 4);
    ctx.fillRect(39, 30, 1, 4);
    ctx.fillRect(42, 30, 1, 4);

    tex.refresh();
  }

  // —— 玛雅的画作（墙上） ——
  private makePainting(): void {
    const W = 48, H = 40;
    const tex = this.scene.textures.createCanvas('deco_painting', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 画框
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(0, 0, W, H);
    // 画框高光
    ctx.fillStyle = '#1a1612';
    ctx.strokeRect(1.5, 1.5, W - 3, H - 3);
    // 画面底色（暖色渐变：黄昏天空）
    const grad = ctx.createLinearGradient(3, 3, 3, H - 3);
    grad.addColorStop(0, '#2a1a14');
    grad.addColorStop(0.5, '#3a2418');
    grad.addColorStop(1, '#1a1410');
    ctx.fillStyle = grad;
    ctx.fillRect(3, 3, W - 6, H - 6);
    // 画面内容剪影：远方山脉
    ctx.fillStyle = '#0e0a08';
    ctx.beginPath();
    ctx.moveTo(3, H - 8);
    ctx.lineTo(12, H - 18);
    ctx.lineTo(20, H - 12);
    ctx.lineTo(30, H - 22);
    ctx.lineTo(38, H - 14);
    ctx.lineTo(W - 3, H - 10);
    ctx.lineTo(W - 3, H - 3);
    ctx.lineTo(3, H - 3);
    ctx.closePath();
    ctx.fill();
    // 一辆车（剪影）驶向远方
    ctx.fillStyle = '#1a1612';
    ctx.fillRect(18, H - 14, 8, 4);
    ctx.fillRect(20, H - 16, 4, 2);
    // 微弱暖光（夕阳）
    ctx.fillStyle = 'rgba(245,201,122,0.15)';
    ctx.beginPath();
    ctx.arc(W - 12, 12, 5, 0, Math.PI * 2);
    ctx.fill();
    // 画框挂钉
    ctx.fillStyle = '#060404';
    ctx.fillRect(W / 2 - 1, 1, 2, 2);

    tex.refresh();
  }

  // —— 墙面旧地图（修理厂） ——
  private makeWallMap(): void {
    const W = 52, H = 40;
    const tex = this.scene.textures.createCanvas('deco_wallmap', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 地图纸张底色（泛黄暗色）
    ctx.fillStyle = '#1e1a14';
    ctx.fillRect(2, 2, W - 4, H - 4);
    // 纸张污渍
    ctx.fillStyle = 'rgba(60,40,20,0.25)';
    ctx.beginPath();
    ctx.ellipse(12, 10, 4, 3, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.ellipse(38, 26, 5, 3, 0, 0, Math.PI * 2);
    ctx.fill();
    // 地图线条（道路/路线）
    ctx.strokeStyle = '#3a2e1e';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(6, 8);
    ctx.lineTo(18, 14);
    ctx.lineTo(28, 10);
    ctx.lineTo(40, 18);
    ctx.lineTo(46, 30);
    ctx.stroke();
    // 支线
    ctx.beginPath();
    ctx.moveTo(18, 14);
    ctx.lineTo(22, 28);
    ctx.lineTo(14, 34);
    ctx.stroke();
    // 北上路线标记（红色圈，褪色）
    ctx.strokeStyle = 'rgba(160,50,30,0.6)';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(40, 18, 4, 0, Math.PI * 2);
    ctx.stroke();
    // 起点标记
    ctx.fillStyle = 'rgba(160,50,30,0.5)';
    ctx.fillRect(5, 7, 2, 2);
    // 五个名字首字母（背面签名剪影）
    ctx.fillStyle = 'rgba(180,140,80,0.3)';
    ctx.fillRect(8, 34, 1, 3);
    ctx.fillRect(11, 34, 1, 3);
    ctx.fillRect(14, 34, 1, 3);
    ctx.fillRect(17, 34, 1, 3);
    ctx.fillRect(20, 34, 1, 3);
    // 地图边缘卷曲
    ctx.fillStyle = '#0a0806';
    ctx.fillRect(2, H - 4, W - 4, 2);
    ctx.fillRect(2, 2, W - 4, 1);

    tex.refresh();
  }

  // ================================================================
  // 场景装饰放置方法（由各 Scene 调用）
  // ================================================================

  /** 在指定位置放置街灯 + 光晕 */
  placeStreetLamp(x: number, y: number): void {
    const lamp = this.scene.add.image(x, y, 'deco_lamp').setDepth(3);
    const glow = this.scene.add.image(x, y - 8, 'deco_glow').setDepth(2).setBlendMode(Phaser.BlendModes.ADD);
    // 微弱闪烁
    this.scene.tweens.add({
      targets: glow,
      alpha: { from: 0.7, to: 1 },
      duration: 2000 + Math.random() * 1000,
      yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
    });
  }

  /** 放置木箱堆 */
  placeCrateStack(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_crate').setDepth(3);
    this.scene.add.image(x - 4, y - 30, 'deco_crate').setDepth(3).setScale(0.85);
  }

  /** 放置垃圾桶 */
  placeTrashCan(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_trash').setDepth(3);
  }

  /** 放置海报 */
  placePoster(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_poster').setDepth(4);
  }

  /** 放置水洼 */
  placePuddle(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_puddle').setDepth(1.5);
  }

  /** 放置管道（水平） */
  placePipe(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_pipe').setDepth(4);
  }

  /** 放置工具架 */
  placeToolRack(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_rack').setDepth(4);
  }

  /** 放置悬挂灯 + 光晕 */
  placeHangingLight(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_hlight').setDepth(5);
    const glow = this.scene.add.image(x, y + 38, 'deco_glow').setDepth(2).setBlendMode(Phaser.BlendModes.ADD).setScale(0.8);
    this.scene.tweens.add({
      targets: glow,
      alpha: { from: 0.5, to: 0.85 },
      duration: 1800 + Math.random() * 800,
      yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
    });
  }

  /** 放置工作台 */
  placeWorkbench(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_bench').setDepth(4);
  }

  /** 放置轮胎 */
  placeTire(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_tire').setDepth(3);
  }

  /** 放置天线 */
  placeAntenna(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_antenna').setDepth(4);
  }

  /** 放置空调外机 */
  placeACUnit(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_ac').setDepth(4);
  }

  /** 放置栏杆段 */
  placeRailing(x: number, y: number, count: number = 1): void {
    for (let i = 0; i < count; i++) {
      this.scene.add.image(x + i * 48, y, 'deco_rail').setDepth(4).setOrigin(0, 0.5);
    }
  }

  /** 放置窗户（带微光） */
  placeWindow(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_window').setDepth(4);
    const glow = this.scene.add.image(x, y, 'deco_glow').setDepth(3).setBlendMode(Phaser.BlendModes.ADD).setScale(0.5);
    glow.alpha = 0.4;
  }

  /** 放置书架 */
  placeBookshelf(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_shelf').setDepth(4);
  }

  /** 放置纸箱 */
  placeBox(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_box').setDepth(3);
  }

  /** 创建远处的城市灯光带（屋顶用） */
  placeDistantCityLights(y: number, width: number): void {
    const lights = this.scene.add.graphics().setDepth(2);
    for (let i = 0; i < 30; i++) {
      const x = Phaser.Math.Between(4, width - 4);
      const r = Phaser.Math.FloatBetween(0.5, 1.4);
      const a = Phaser.Math.FloatBetween(0.2, 0.6);
      lights.fillStyle(0xf5c97a, a);
      lights.fillCircle(x, y, r);
    }
    this.scene.tweens.add({
      targets: lights,
      alpha: { from: 0.6, to: 1 },
      duration: 3000,
      yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
    });
  }

  /** 放置餐桌（露丝餐厅送餐位） */
  placeTable(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_table').setDepth(3);
  }

  /** 放置旅行轿车（修理厂） */
  placeCar(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_car').setDepth(4);
    // 车体微光晕
    const glow = this.scene.add.image(x, y + 6, 'deco_glow').setDepth(2).setBlendMode(Phaser.BlendModes.ADD).setScale(1.4);
    glow.alpha = 0.25;
  }

  /** 放置玛雅的画作（墙面） */
  placePainting(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_painting').setDepth(5);
  }

  /** 放置墙面旧地图 */
  placeWallMap(x: number, y: number): void {
    this.scene.add.image(x, y, 'deco_wallmap').setDepth(5);
  }

  // —— 北方宣传明信片（4 种变体，序章收集品） ——
  private makePostcard(): void {
    // 明信片 1：极光
    let tex = this.scene.textures.createCanvas('postcard_aurora', 48, 36);
    if (tex) {
      const ctx = tex.getContext();
      // 白色边框（明信片）
      ctx.fillStyle = '#d8c8a0';
      ctx.fillRect(0, 0, 48, 36);
      // 深色内框（照片）
      ctx.fillStyle = '#0a0814';
      ctx.fillRect(4, 4, 40, 24);
      // 极光渐变
      const grad = ctx.createLinearGradient(0, 6, 0, 24);
      grad.addColorStop(0, 'rgba(80,255,180,0.8)');
      grad.addColorStop(0.4, 'rgba(140,100,255,0.7)');
      grad.addColorStop(1, 'rgba(10,8,30,0)');
      ctx.fillStyle = grad;
      ctx.fillRect(4, 6, 40, 22);
      // 极光波浪
      ctx.strokeStyle = 'rgba(200,255,220,0.6)';
      ctx.lineWidth = 0.8;
      for (let i = 0; i < 3; i++) {
        ctx.beginPath();
        for (let x = 4; x <= 44; x += 2) {
          const y = 8 + i * 4 + Math.sin(x * 0.4 + i) * 2;
          if (x === 4) ctx.moveTo(x, y);
          else ctx.lineTo(x, y);
        }
        ctx.stroke();
      }
      // 星星
      for (let i = 0; i < 12; i++) {
        ctx.fillStyle = `rgba(255,255,220,${0.3 + Math.random() * 0.5})`;
        ctx.fillRect(6 + Math.random() * 36, 6 + Math.random() * 12, 1, 1);
      }
      // 邮票角贴纸
      ctx.fillStyle = '#d4a030';
      ctx.fillRect(38, 2, 6, 6);
      ctx.strokeStyle = '#a07820';
      ctx.lineWidth = 0.5;
      ctx.strokeRect(38.5, 2.5, 5, 5);
      tex.refresh();
    }

    // 明信片 2：港口城市
    tex = this.scene.textures.createCanvas('postcard_harbor', 48, 36);
    if (tex) {
      const ctx = tex.getContext();
      ctx.fillStyle = '#d8c8a0';
      ctx.fillRect(0, 0, 48, 36);
      // 天空渐变（黄昏）
      const grad = ctx.createLinearGradient(0, 4, 0, 28);
      grad.addColorStop(0, '#3a2a4a');
      grad.addColorStop(0.5, '#6a3a4a');
      grad.addColorStop(1, '#a0603a');
      ctx.fillStyle = grad;
      ctx.fillRect(4, 4, 40, 24);
      // 海面反光
      ctx.fillStyle = 'rgba(245,201,122,0.3)';
      ctx.fillRect(4, 18, 40, 10);
      // 城市剪影（高楼）
      ctx.fillStyle = '#0a080c';
      const heights = [6, 10, 8, 12, 7, 11, 9, 6, 10];
      for (let i = 0; i < 9; i++) {
        const hx = 6 + i * 4;
        const hh = heights[i];
        ctx.fillRect(hx, 24 - hh, 3, hh);
        // 窗户光点
        for (let w = 0; w < hh - 2; w += 2) {
          if (Math.random() > 0.4) {
            ctx.fillStyle = 'rgba(245,201,122,0.8)';
            ctx.fillRect(hx + 1, 24 - hh + w + 1, 1, 1);
            ctx.fillStyle = '#0a080c';
          }
        }
      }
      // 远处大船轮廓
      ctx.fillStyle = '#1a1420';
      ctx.fillRect(14, 22, 20, 2);
      ctx.fillRect(20, 19, 2, 3);
      ctx.fillRect(26, 18, 2, 4);
      // 邮票
      ctx.fillStyle = '#d4a030';
      ctx.fillRect(38, 2, 6, 6);
      tex.refresh();
    }

    // 明信片 3：山脉旷野
    tex = this.scene.textures.createCanvas('postcard_mountain', 48, 36);
    if (tex) {
      const ctx = tex.getContext();
      ctx.fillStyle = '#d8c8a0';
      ctx.fillRect(0, 0, 48, 36);
      // 天空
      const grad = ctx.createLinearGradient(0, 4, 0, 28);
      grad.addColorStop(0, '#5a8abf');
      grad.addColorStop(1, '#bfa68a');
      ctx.fillStyle = grad;
      ctx.fillRect(4, 4, 40, 24);
      // 远山层（蓝灰）
      ctx.fillStyle = '#3a4a6a';
      ctx.beginPath();
      ctx.moveTo(4, 22); ctx.lineTo(10, 10); ctx.lineTo(16, 18);
      ctx.lineTo(22, 8); ctx.lineTo(30, 16); ctx.lineTo(38, 12); ctx.lineTo(44, 20); ctx.lineTo(44, 28); ctx.lineTo(4, 28);
      ctx.closePath(); ctx.fill();
      // 雪山尖高光
      ctx.fillStyle = '#d8c8a0';
      ctx.beginPath(); ctx.moveTo(20, 12); ctx.lineTo(22, 8); ctx.lineTo(24, 12); ctx.closePath(); ctx.fill();
      // 近山层（深棕）
      ctx.fillStyle = '#2a2018';
      ctx.beginPath();
      ctx.moveTo(4, 28); ctx.lineTo(12, 20); ctx.lineTo(20, 26);
      ctx.lineTo(28, 18); ctx.lineTo(36, 24); ctx.lineTo(44, 22); ctx.lineTo(44, 28);
      ctx.closePath(); ctx.fill();
      // 旷野前景
      ctx.fillStyle = '#1a1810';
      ctx.fillRect(4, 24, 40, 4);
      // 邮票
      ctx.fillStyle = '#d4a030';
      ctx.fillRect(38, 2, 6, 6);
      tex.refresh();
    }

    // 明信片 4：美术馆
    tex = this.scene.textures.createCanvas('postcard_gallery', 48, 36);
    if (tex) {
      const ctx = tex.getContext();
      ctx.fillStyle = '#d8c8a0';
      ctx.fillRect(0, 0, 48, 36);
      // 外墙
      ctx.fillStyle = '#d8d0b8';
      ctx.fillRect(4, 8, 40, 20);
      // 屋顶
      ctx.fillStyle = '#8a7050';
      ctx.beginPath();
      ctx.moveTo(2, 10); ctx.lineTo(24, 2); ctx.lineTo(46, 10);
      ctx.closePath(); ctx.fill();
      // 大门
      ctx.fillStyle = '#3a2818';
      ctx.fillRect(20, 16, 8, 12);
      // 门上方半圆装饰
      ctx.strokeStyle = '#8a7050';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.arc(24, 16, 5, Math.PI, 0);
      ctx.stroke();
      // 窗户
      ctx.fillStyle = '#f5c97a';
      ctx.fillRect(8, 14, 6, 6);
      ctx.fillRect(34, 14, 6, 6);
      // 窗户边框
      ctx.strokeStyle = '#6a5038';
      ctx.lineWidth = 0.8;
      ctx.strokeRect(8.5, 14.5, 5, 5);
      ctx.strokeRect(34.5, 14.5, 5, 5);
      // 招牌
      ctx.fillStyle = '#4a3828';
      ctx.fillRect(14, 12, 20, 3);
      ctx.fillStyle = '#f5c97a';
      ctx.fillRect(16, 12.5, 16, 2);
      // 前景草地
      ctx.fillStyle = '#2a3a20';
      ctx.fillRect(4, 26, 40, 2);
      // 邮票
      ctx.fillStyle = '#d4a030';
      ctx.fillRect(38, 2, 6, 6);
      tex.refresh();
    }
  }

  // 放置明信片（4 种：aurora/harbor/mountain/gallery）
  placePostcard(x: number, y: number, type: 'aurora' | 'harbor' | 'mountain' | 'gallery' = 'aurora'): Phaser.GameObjects.Image {
    const key = 'postcard_' + type;
    const img = this.scene.add.image(x, y, key).setDepth(4);
    // 明信片轻微浮动
    this.scene.tweens.add({
      targets: img,
      y: y - 3,
      duration: 1800 + Math.random() * 400,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    });
    // 轻微旋转（模拟随手放置）
    img.setAngle((Math.random() - 0.5) * 8);
    return img;
  }

  // —— 北方宣传看板（大型海报，序章互动点） ——
  private makeNorthBoard(): void {
    const W = 120, H = 90;
    const tex = this.scene.textures.createCanvas('deco_northboard', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 木框
    ctx.fillStyle = '#3a2818';
    ctx.fillRect(0, 0, W, H);
    // 海报区域
    ctx.fillStyle = '#0a0e1a';
    ctx.fillRect(4, 4, W - 8, H - 8);
    // 顶部大字：去北方
    ctx.fillStyle = '#f5c97a';
    ctx.font = 'bold 14px serif';
    ctx.textAlign = 'center';
    ctx.fillText('— 北 方 —', W / 2, 20);
    // 副标题
    ctx.fillStyle = '#a0a8c0';
    ctx.font = '9px serif';
    ctx.fillText('机会 · 自由 · 新的人生', W / 2, 32);

    // 插图：北方城市剪影
    ctx.fillStyle = '#1a2030';
    const bh = [14, 22, 18, 28, 16, 24, 20, 26, 14, 22];
    for (let i = 0; i < 10; i++) {
      const bx = 14 + i * 9;
      const hh = bh[i];
      ctx.fillRect(bx, 60 - hh / 2, 7, hh / 2);
      // 窗户光点
      for (let wy = 0; wy < hh / 2 - 4; wy += 3) {
        if (Math.random() > 0.35) {
          ctx.fillStyle = 'rgba(245,201,122,0.7)';
          ctx.fillRect(bx + 2, 60 - hh / 2 + wy + 2, 1, 1);
          ctx.fillStyle = '#1a2030';
        }
      }
    }

    // 分隔线
    ctx.strokeStyle = '#f5c97a';
    ctx.lineWidth = 0.6;
    ctx.beginPath(); ctx.moveTo(20, 68); ctx.lineTo(W - 20, 68); ctx.stroke();

    // 底部招聘信息
    ctx.fillStyle = '#c0c8d8';
    ctx.font = '8px serif';
    ctx.textAlign = 'left';
    ctx.fillText('新港区招工 · 薪资三倍', 10, 78);
    ctx.fillText('美术馆新锐征稿', 10, 84);
    ctx.textAlign = 'right';
    ctx.fillStyle = '#8abf8a';
    ctx.fillText('一起北上 →', W - 10, 84);

    tex.refresh();
  }

  placeNorthBoard(x: number, y: number): Phaser.GameObjects.Image {
    return this.scene.add.image(x, y, 'deco_northboard').setDepth(4);
  }

  // —— 愿望墙（钉满便签的木板，序章装饰） ——
  private makeWishWall(): void {
    const W = 96, H = 72;
    const tex = this.scene.textures.createCanvas('deco_wishwall', W, H);
    if (!tex) return;
    const ctx = tex.getContext();

    // 木板底
    ctx.fillStyle = '#2a1e14';
    ctx.fillRect(0, 0, W, H);
    // 木板纹理
    for (let i = 0; i < 5; i++) {
      ctx.fillStyle = i % 2 ? '#201610' : '#2a1e14';
      ctx.fillRect(0, i * (H / 5), W, H / 5);
      ctx.strokeStyle = '#14100a';
      ctx.lineWidth = 0.5;
      ctx.beginPath(); ctx.moveTo(0, i * (H / 5)); ctx.lineTo(W, i * (H / 5)); ctx.stroke();
    }

    // 便签 1：绿色 - 诺亚
    ctx.fillStyle = '#4a6a3a';
    ctx.save(); ctx.translate(20, 18); ctx.rotate(-0.15);
    ctx.fillRect(-14, -10, 28, 20);
    ctx.fillStyle = '#8abf6a';
    ctx.font = '7px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('逃离家人', 0, -1);
    ctx.fillText('做手工！', 0, 6);
    ctx.restore();

    // 便签 2：黄色 - 伊莱亚斯
    ctx.fillStyle = '#8a7020';
    ctx.save(); ctx.translate(50, 16); ctx.rotate(0.1);
    ctx.fillRect(-14, -10, 28, 20);
    ctx.fillStyle = '#f5d87a';
    ctx.font = '7px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('薪资三倍', 0, -1);
    ctx.fillText('走出去！', 0, 6);
    ctx.restore();

    // 便签 3：粉色 - 玛雅
    ctx.fillStyle = '#6a3a4a';
    ctx.save(); ctx.translate(78, 20); ctx.rotate(-0.08);
    ctx.fillRect(-14, -10, 28, 20);
    ctx.fillStyle = '#d890a8';
    ctx.font = '7px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('画极光', 0, -1);
    ctx.fillText('办画展！', 0, 6);
    ctx.restore();

    // 便签 4：蓝色 - 利奥
    ctx.fillStyle = '#2a3a5a';
    ctx.save(); ctx.translate(30, 48); ctx.rotate(0.12);
    ctx.fillRect(-14, -10, 28, 20);
    ctx.fillStyle = '#7a9ac8';
    ctx.font = '7px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('看大海', 0, -1);
    ctx.fillText('闯天下！', 0, 6);
    ctx.restore();

    // 便签 5：白色 - 中央空的（等待玩家）
    ctx.fillStyle = '#c8c0a8';
    ctx.save(); ctx.translate(60, 50); ctx.rotate(-0.05);
    ctx.fillRect(-14, -10, 28, 20);
    ctx.fillStyle = '#4a4030';
    ctx.font = '7px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText('你的愿望', 0, -1);
    ctx.fillText('？', 0, 6);
    ctx.restore();

    // 图钉
    const pins = [[6, 8], [42, 4], [72, 10], [22, 40], [60, 42], [84, 42]];
    for (const [px, py] of pins) {
      ctx.fillStyle = '#8a3030';
      ctx.beginPath();
      ctx.arc(px, py, 2, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = '#c06060';
      ctx.beginPath();
      ctx.arc(px - 0.5, py - 0.5, 0.8, 0, Math.PI * 2);
      ctx.fill();
    }

    tex.refresh();
  }

  placeWishWall(x: number, y: number): Phaser.GameObjects.Image {
    return this.scene.add.image(x, y, 'deco_wishwall').setDepth(3);
  }

  // —— 北方招工小海报（装饰） ——
  private makeNpRecruitPoster(): void {
    const W = 32, H = 44;
    const tex = this.scene.textures.createCanvas('poster_recruit', W, H);
    if (!tex) return;
    const ctx = tex.getContext();
    // 纸
    ctx.fillStyle = '#c8b888';
    ctx.fillRect(0, 0, W, H);
    // 撕裂边缘
    ctx.fillStyle = '#a89868';
    for (let i = 0; i < 8; i++) {
      ctx.fillRect(i * 4, 0, 2, 1 + Math.random() * 2);
      ctx.fillRect(i * 4, H - 1 - Math.random() * 2, 2, 2);
    }
    // 标题
    ctx.fillStyle = '#8a3020';
    ctx.font = 'bold 8px serif';
    ctx.textAlign = 'center';
    ctx.fillText('新港区', W / 2, 12);
    ctx.fillText('招工', W / 2, 22);
    // 薪资标注
    ctx.fillStyle = '#3a5a3a';
    ctx.font = 'bold 9px serif';
    ctx.fillText('×3 薪', W / 2, 34);
    // 底部线
    ctx.strokeStyle = '#6a5030';
    ctx.lineWidth = 0.5;
    ctx.beginPath(); ctx.moveTo(4, 38); ctx.lineTo(W - 4, 38); ctx.stroke();
    ctx.fillStyle = '#6a5030';
    ctx.font = '6px sans-serif';
    ctx.fillText('包食宿', W / 2, 43);
    tex.refresh();
  }

  placeRecruitPoster(x: number, y: number): void {
    const img = this.scene.add.image(x, y, 'poster_recruit').setDepth(3);
    img.setAngle((Math.random() - 0.5) * 5);
  }

  // —— 美术馆征稿小海报（装饰） ——
  private makeGalleryPoster(): void {
    const W = 32, H = 44;
    const tex = this.scene.textures.createCanvas('poster_gallery', W, H);
    if (!tex) return;
    const ctx = tex.getContext();
    // 纸
    ctx.fillStyle = '#b8c8d0';
    ctx.fillRect(0, 0, W, H);
    // 撕裂边缘
    ctx.fillStyle = '#8898a0';
    for (let i = 0; i < 8; i++) {
      ctx.fillRect(i * 4, 0, 2, 1 + Math.random() * 2);
    }
    // 小画框
    ctx.fillStyle = '#2a3850';
    ctx.fillRect(4, 4, W - 8, 18);
    // 画框内：抽象画
    const grad = ctx.createLinearGradient(5, 5, 5, 21);
    grad.addColorStop(0, '#f5c97a');
    grad.addColorStop(0.5, '#d86060');
    grad.addColorStop(1, '#6a8ad8');
    ctx.fillStyle = grad;
    ctx.fillRect(6, 6, W - 12, 14);
    // 画框高光
    ctx.strokeStyle = '#d8c090';
    ctx.lineWidth = 0.5;
    ctx.strokeRect(4.5, 4.5, W - 9, 17);
    // 文字
    ctx.fillStyle = '#2a3850';
    ctx.font = 'bold 7px serif';
    ctx.textAlign = 'center';
    ctx.fillText('新锐征稿', W / 2, 30);
    ctx.font = '6px serif';
    ctx.fillStyle = '#4a5868';
    ctx.fillText('新港区美术馆', W / 2, 38);
    ctx.fillText('有奖金！', W / 2, 43);
    tex.refresh();
  }

  placeGalleryPoster(x: number, y: number): void {
    const img = this.scene.add.image(x, y, 'poster_gallery').setDepth(3);
    img.setAngle((Math.random() - 0.5) * 5);
  }

  // —— 玩家愿望便签（5 种变体，对应不同愿望选择）——
  // 类型：wealth 赚大钱 / freedom 自由 / art 艺术 / friends 朋友 / unknown 未选择（默认空白）
  private makeWishNoteVariants(): void {
    const W = 28, H = 20;
    const variants: Array<{ key: string; bg: string; fg: string; text: string }> = [
      // 赚大钱（金黄）
      { key: 'wish_wealth', bg: '#8a7020', fg: '#f5d87a', text: '赚大钱\n出人头地' },
      // 自由闯天下（蓝）
      { key: 'wish_freedom', bg: '#2a3a5a', fg: '#7a9ac8', text: '看世界\n自由自在' },
      // 追求艺术（粉）
      { key: 'wish_art', bg: '#6a3a4a', fg: '#d890a8', text: '画遍山河\n办画展' },
      // 和朋友在一起（绿）
      { key: 'wish_friends', bg: '#4a6a3a', fg: '#8abf6a', text: '大家一起\n永远不分开' },
      // 找到自己的路（橙）
      { key: 'wish_path', bg: '#8a4a20', fg: '#f5a878', text: '找到属于\n自己的路' }
    ];

    for (const v of variants) {
      const tex = this.scene.textures.createCanvas(v.key, W, H);
      if (!tex) continue;
      const ctx = tex.getContext();
      // 便签底
      ctx.fillStyle = v.bg;
      ctx.fillRect(0, 0, W, H);
      // 便签内文字（两行）
      ctx.fillStyle = v.fg;
      ctx.font = 'bold 6px sans-serif';
      ctx.textAlign = 'center';
      const lines = v.text.split('\n');
      ctx.fillText(lines[0], W / 2, 8);
      ctx.fillText(lines[1], W / 2, 15);
      // 轻微阴影边
      ctx.strokeStyle = 'rgba(0,0,0,0.25)';
      ctx.lineWidth = 0.5;
      ctx.strokeRect(0.5, 0.5, W - 1, H - 1);
      tex.refresh();
    }
  }

  // 放置玩家愿望便签（根据选择类型覆盖愿望墙的中央空白）
  placePlayerWishNote(x: number, y: number, type: 'wealth' | 'freedom' | 'art' | 'friends' | 'path'): Phaser.GameObjects.Image {
    const key = 'wish_' + type;
    const img = this.scene.add.image(x, y, key).setDepth(6);
    img.setAngle((Math.random() - 0.5) * 4);
    // 淡入
    img.setAlpha(0);
    this.scene.tweens.add({ targets: img, alpha: 1, duration: 300 });
    return img;
  }

  // —— 北方灯火小游戏光点 ——
  private makeNBLightDot(): void {
    // 3 个尺寸等级，不同颜色（暖黄/暖橙/淡紫，对应北方城市的不同光晕）
    const sizes: Array<{ key: string; r: number; c: string }> = [
      { key: 'nb_light_s', r: 8,  c: '#f5c97a' },
      { key: 'nb_light_m', r: 12, c: '#f5a878' },
      { key: 'nb_light_l', r: 18, c: '#d890c8' }
    ];
    for (const s of sizes) {
      const tex = this.scene.textures.createCanvas(s.key, s.r * 2, s.r * 2);
      if (!tex) continue;
      const ctx = tex.getContext();
      const cx = s.r, cy = s.r;
      // 径向渐变光晕
      const grad = ctx.createRadialGradient(cx, cy, 1, cx, cy, s.r);
      grad.addColorStop(0, '#ffffff');
      grad.addColorStop(0.25, s.c);
      grad.addColorStop(0.7, s.c + 'cc');
      grad.addColorStop(1, 'rgba(0,0,0,0)');
      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.arc(cx, cy, s.r, 0, Math.PI * 2);
      ctx.fill();
      tex.refresh();
    }
  }

  // 放置北方灯火光点（带呼吸缩放）
  placeNBLight(x: number, y: number, size: 's' | 'm' | 'l' = 'm'): Phaser.GameObjects.Image {
    const key = 'nb_light_' + size;
    const img = this.scene.add.image(x, y, key).setDepth(5);
    img.setBlendMode(Phaser.BlendModes.ADD);
    // 呼吸动画
    this.scene.tweens.add({
      targets: img,
      scale: { from: 0.85, to: 1.15 },
      alpha: { from: 0.7, to: 1 },
      duration: 900 + Math.random() * 500,
      yoyo: true, repeat: -1,
      ease: 'Sine.easeInOut'
    });
    return img;
  }
}
