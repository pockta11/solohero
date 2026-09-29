"""Basic skill art (D-093): the sword-wave VFX sheet and its 16 px HUD icon.

vfxwave_play_6.png: a crescent of light that sweeps forward and thins out - white core, sky-blue body, deep blue
rim, a few sparks on the last frames. icon_basic.png: the same crescent with a small sword, in the UI icon style
(dark outline, two light tones). No anti-aliasing.

Run: python tools/art/fxgen.py
"""
import math
import os

from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Art")
CORE = (255, 255, 255, 255)
BODY = (150, 214, 255, 255)
RIM = (66, 128, 230, 255)
SPARK = (255, 244, 170, 255)
OUT = (18, 15, 31, 255)
LIGHT = (238, 242, 248, 255)
MID = (184, 191, 204, 255)

FW, FH = 56, 56


def crescent(img, ox, t, grow):
    """One frame: an arc from -70 to +70 degrees around a centre left of the frame, thick in the middle."""
    px = img.load()
    cx, cy = ox + 6 + grow * 8, FH / 2
    r_out = 20 + grow * 6
    thick = max(1.5, 9 * (1 - t) + 2)
    for y in range(FH):
        for x in range(FW):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            d = math.hypot(dx, dy)
            a = math.degrees(math.atan2(-dy, dx))
            if a < -72 or a > 72:
                continue
            taper = math.cos(math.radians(a) * 1.15)
            inner = r_out - thick * max(0.0, taper)
            if inner <= d <= r_out:
                depth = (r_out - d) / max(0.5, r_out - inner)
                c = RIM if depth < 0.25 else (CORE if depth > 0.55 and t < 0.7 else BODY)
                if t > 0.75 and (x + y) % 2 == 0:
                    continue
                px[ox + (x - ox) if x < FW else x, y] = c


def wave_sheet():
    frames = 6
    img = Image.new("RGBA", (FW * frames, FH), (0, 0, 0, 0))
    params = [(0.0, 0.0), (0.15, 0.3), (0.3, 0.6), (0.5, 0.85), (0.72, 1.0), (0.9, 1.1)]
    for i, (t, grow) in enumerate(params):
        frame = Image.new("RGBA", (FW, FH), (0, 0, 0, 0))
        crescent(frame, 0, t, grow)
        if i >= 3:
            fp = frame.load()
            for k in range(5):
                sx = int(30 + grow * 12 + math.cos(k * 1.7 + i) * 8) % FW
                sy = int(FH / 2 + math.sin(k * 2.3 + i) * 18) % FH
                fp[sx, sy] = SPARK
        img.alpha_composite(frame, (i * FW, 0))
    img.save(os.path.join(ROOT, "Vfx", "vfxwave_play_6.png"))


def icon():
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = img.load()
    # Crescent: outer radius 7, inner 4.5, centre left of the icon.
    cx, cy = 3.5, 8.0
    for y in range(16):
        for x in range(16):
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
            a = math.degrees(math.atan2(-(y + 0.5 - cy), x + 0.5 - cx))
            if -75 <= a <= 75 and 7.5 <= d <= 11.5:
                px[x, y] = LIGHT if d < 9.5 else BODY
    # Sword across the middle: blade light, hilt gold.
    for i in range(9):
        x, y = 2 + i, 12 - i
        if 0 <= x < 16 and 0 <= y < 16:
            px[x, y] = LIGHT if i > 2 else (255, 196, 52, 255)
    # Outline every opaque pixel's empty neighbours.
    src = img.copy().load()
    for y in range(16):
        for x in range(16):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < 16 and 0 <= ny < 16 and src[nx, ny][3] and src[nx, ny] != OUT:
                    px[x, y] = OUT
                    break
    img.save(os.path.join(ROOT, "UI", "Icons", "icon_basic.png"))


if __name__ == "__main__":
    wave_sheet()
    icon()
    print("fx ok")
