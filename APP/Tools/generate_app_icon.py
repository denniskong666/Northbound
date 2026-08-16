#!/usr/bin/env python3
"""Generate the Northbound macOS application icon from project-owned artwork."""

from __future__ import annotations

import math
import random
import subprocess
import tempfile
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/Northbound/Art/Brand/NorthboundAppIcon.png"
ICNS_OUTPUT = ROOT / "Assets/Northbound/Art/Brand/NorthboundAppIcon.icns"
WAGON = ROOT / "Assets/Northbound/Art/Props/station-wagon-sprite-sheet.png"
SIZE = 2048
SCALE = SIZE / 1024


def point(x: float, y: float) -> tuple[int, int]:
    return round(x * SCALE), round(y * SCALE)


def points(values: list[tuple[float, float]]) -> list[tuple[int, int]]:
    return [point(x, y) for x, y in values]


def rounded_mask() -> Image.Image:
    mask = Image.new("L", (SIZE, SIZE), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle(
        (*point(42, 42), *point(982, 982)),
        radius=round(218 * SCALE),
        fill=255,
    )
    return mask


def vertical_gradient(top: tuple[int, int, int], bottom: tuple[int, int, int]) -> Image.Image:
    image = Image.new("RGB", (SIZE, SIZE))
    draw = ImageDraw.Draw(image)
    for y in range(SIZE):
        amount = y / (SIZE - 1)
        color = tuple(round(a + (b - a) * amount) for a, b in zip(top, bottom))
        draw.line((0, y, SIZE, y), fill=color)
    return image.convert("RGBA")


def add_glow(base: Image.Image, mask: Image.Image) -> None:
    glow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(glow)
    draw.ellipse((*point(268, 72), *point(756, 556)), fill=(232, 169, 78, 180))
    glow = glow.filter(ImageFilter.GaussianBlur(round(110 * SCALE)))
    glow.putalpha(ImageChops.multiply(glow.getchannel("A"), mask))
    base.alpha_composite(glow)


def add_map_routes(base: Image.Image, mask: Image.Image) -> None:
    routes = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(routes)
    random.seed(18)
    for index in range(9):
        y0 = 180 + index * 92
        amplitude = 32 + index * 5
        phase = random.random() * math.pi
        route = []
        for x in range(-60, 1100, 28):
            y = y0 + math.sin(x / 115 + phase) * amplitude + math.sin(x / 47) * 8
            route.append(point(x, y))
        draw.line(route, fill=(138, 196, 190, 24), width=round(4 * SCALE), joint="curve")
    routes.putalpha(ImageChops.multiply(routes.getchannel("A"), mask))
    base.alpha_composite(routes)


def add_skyline(base: Image.Image, mask: Image.Image) -> None:
    skyline = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(skyline)
    buildings = [
        (50, 455, 190, 720),
        (176, 520, 316, 720),
        (270, 430, 412, 720),
        (610, 500, 738, 720),
        (708, 420, 862, 720),
        (842, 480, 984, 720),
    ]
    for x1, y1, x2, y2 in buildings:
        draw.rectangle((*point(x1, y1), *point(x2, y2)), fill=(6, 22, 31, 122))
        for wx in range(x1 + 22, x2 - 12, 38):
            for wy in range(y1 + 30, y2 - 18, 48):
                if (wx + wy) % 3:
                    draw.rounded_rectangle(
                        (*point(wx, wy), *point(wx + 12, wy + 18)),
                        radius=round(2 * SCALE),
                        fill=(235, 172, 77, 112),
                    )
    draw.rectangle((*point(806, 292), *point(834, 540)), fill=(6, 22, 31, 142))
    draw.ellipse((*point(778, 268), *point(862, 326)), fill=(8, 28, 37, 150))
    draw.line((*point(820, 326), *point(774, 580)), fill=(8, 28, 37, 150), width=round(10 * SCALE))
    draw.line((*point(820, 326), *point(866, 580)), fill=(8, 28, 37, 150), width=round(10 * SCALE))
    skyline = skyline.filter(ImageFilter.GaussianBlur(round(1.1 * SCALE)))
    skyline.putalpha(ImageChops.multiply(skyline.getchannel("A"), mask))
    base.alpha_composite(skyline)


def draw_road_arrow(base: Image.Image) -> None:
    outer = points(
        [
            (512, 106),
            (694, 318),
            (590, 292),
            (728, 900),
            (296, 900),
            (434, 292),
            (330, 318),
        ]
    )
    inner = points(
        [
            (512, 160),
            (626, 293),
            (552, 274),
            (650, 875),
            (374, 875),
            (472, 274),
            (398, 293),
        ]
    )

    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.polygon([(x, y + round(18 * SCALE)) for x, y in outer], fill=(0, 8, 13, 170))
    shadow = shadow.filter(ImageFilter.GaussianBlur(round(22 * SCALE)))
    base.alpha_composite(shadow)

    road = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(road)
    draw.polygon(outer, fill=(232, 211, 166, 255))
    draw.line(outer + [outer[0]], fill=(246, 226, 184, 255), width=round(9 * SCALE), joint="curve")
    draw.polygon(inner, fill=(38, 54, 58, 255))
    draw.line(inner + [inner[0]], fill=(170, 132, 73, 195), width=round(6 * SCALE), joint="curve")

    for y in (760, 640, 530, 440, 370):
        taper = max(6, round((y - 250) / 76))
        height = max(28, round((y - 240) / 6))
        draw.rounded_rectangle(
            (*point(512 - taper / 2, y - height / 2), *point(512 + taper / 2, y + height / 2)),
            radius=round(4 * SCALE),
            fill=(222, 169, 80, 230),
        )

    base.alpha_composite(road)


def extract_wagon() -> Image.Image:
    source = Image.open(WAGON).convert("RGBA")
    crop = source.crop((42, 236, 304, 660))
    pixels = np.asarray(crop).astype(np.float32)
    background = np.array([251.0, 4.0, 249.0], dtype=np.float32)
    distance = np.linalg.norm(pixels[:, :, :3] - background, axis=2)
    alpha = np.clip((distance - 18.0) / 92.0, 0.0, 1.0)
    red = pixels[:, :, 0]
    green = pixels[:, :, 1]
    blue = pixels[:, :, 2]
    magenta_spill = (red > green * 1.45 + 24.0) & (blue > green * 1.3 + 24.0)
    alpha = np.where(magenta_spill, 0.0, alpha)

    # Recover antialiased edge colors from the flat chroma background.
    safe_alpha = np.maximum(alpha[:, :, None], 0.08)
    recovered = (pixels[:, :, :3] - background * (1.0 - alpha[:, :, None])) / safe_alpha
    recovered = np.clip(recovered, 0.0, 255.0)
    pixels[:, :, :3] = np.where(alpha[:, :, None] > 0.0, recovered, 0.0)
    pixels[:, :, 3] = alpha * 255.0
    wagon = Image.fromarray(pixels.astype(np.uint8), "RGBA")
    bounds = wagon.getbbox()
    if bounds:
        wagon = wagon.crop(bounds)
    target_height = round(276 * SCALE)
    target_width = round(wagon.width * target_height / wagon.height)
    wagon = wagon.resize((target_width, target_height), Image.Resampling.LANCZOS)
    return wagon.rotate(180, expand=True, resample=Image.Resampling.BICUBIC)


def add_wagon(base: Image.Image) -> None:
    wagon = extract_wagon()
    x = (SIZE - wagon.width) // 2
    y = round(574 * SCALE)

    shadow_alpha = wagon.getchannel("A").filter(ImageFilter.GaussianBlur(round(16 * SCALE)))
    shadow = Image.new("RGBA", wagon.size, (0, 0, 0, 0))
    shadow.putalpha(shadow_alpha.point(lambda value: round(value * 0.48)))
    base.alpha_composite(shadow, (x + round(8 * SCALE), y + round(20 * SCALE)))
    base.alpha_composite(wagon, (x, y))


def add_north_star(base: Image.Image) -> None:
    star = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(star)
    cx, cy = point(512, 112)
    long_radius = round(47 * SCALE)
    short_radius = round(13 * SCALE)
    vertices = []
    for index in range(8):
        radius = long_radius if index % 2 == 0 else short_radius
        angle = -math.pi / 2 + index * math.pi / 4
        vertices.append((round(cx + math.cos(angle) * radius), round(cy + math.sin(angle) * radius)))

    glow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse((*point(440, 40), *point(584, 184)), fill=(245, 183, 75, 175))
    glow = glow.filter(ImageFilter.GaussianBlur(round(34 * SCALE)))
    base.alpha_composite(glow)
    draw.polygon(vertices, fill=(255, 224, 145, 255))
    draw.ellipse((*point(500, 100), *point(524, 124)), fill=(255, 246, 214, 255))
    base.alpha_composite(star)


def add_finish(base: Image.Image, mask: Image.Image) -> None:
    noise = Image.effect_noise((SIZE, SIZE), 11).convert("L")
    noise_alpha = noise.point(lambda value: round(abs(value - 128) * 0.16))
    noise_rgba = Image.new("RGBA", base.size, (255, 236, 205, 0))
    noise_rgba.putalpha(ImageChops.multiply(noise_alpha, mask))
    base.alpha_composite(noise_rgba)


def generate() -> None:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    mask = rounded_mask()

    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    shadow_mask = mask.filter(ImageFilter.GaussianBlur(round(24 * SCALE)))
    shadow.putalpha(shadow_mask.point(lambda value: round(value * 0.58)))
    canvas.alpha_composite(shadow, point(0, 14))

    base = vertical_gradient((18, 60, 66), (6, 22, 34))
    base.putalpha(mask)
    add_glow(base, mask)
    add_map_routes(base, mask)
    add_skyline(base, mask)
    draw_road_arrow(base)
    add_wagon(base)
    add_north_star(base)
    add_finish(base, mask)

    border = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    border_draw = ImageDraw.Draw(border)
    border_draw.rounded_rectangle(
        (*point(44, 44), *point(980, 980)),
        radius=round(216 * SCALE),
        outline=(210, 158, 82, 220),
        width=round(9 * SCALE),
    )
    base.alpha_composite(border)
    base.putalpha(ImageChops.multiply(base.getchannel("A"), mask))
    canvas.alpha_composite(base)

    canvas = canvas.resize((1024, 1024), Image.Resampling.LANCZOS)
    canvas.save(OUTPUT, "PNG", optimize=True)
    generate_icns(canvas)
    print(OUTPUT)
    print(ICNS_OUTPUT)


def generate_icns(master: Image.Image) -> None:
    icon_files = {
        "icon_16x16.png": 16,
        "icon_16x16@2x.png": 32,
        "icon_32x32.png": 32,
        "icon_32x32@2x.png": 64,
        "icon_128x128.png": 128,
        "icon_128x128@2x.png": 256,
        "icon_256x256.png": 256,
        "icon_256x256@2x.png": 512,
        "icon_512x512.png": 512,
        "icon_512x512@2x.png": 1024,
    }

    with tempfile.TemporaryDirectory(prefix="northbound-app-icon-") as temporary:
        iconset = Path(temporary) / "Northbound.iconset"
        iconset.mkdir()
        for filename, size in icon_files.items():
            resized = master.resize((size, size), Image.Resampling.LANCZOS)
            resized.save(iconset / filename, "PNG", optimize=True)

        subprocess.run(
            ["/usr/bin/iconutil", "-c", "icns", "-o", str(ICNS_OUTPUT), str(iconset)],
            check=True,
        )


if __name__ == "__main__":
    generate()
