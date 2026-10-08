"""Flashy skill layers (D-146) -> Assets/SoloHero/Art/Vfx.

Usage: python tools/art/vfxgen3.py [OUT_DIR] [--preview PREVIEW.png] [name ...]
- Glow sheets vfx{name}glow_play_{n}.png for every effect sheet in Art/Vfx (not the bone): the sheet's colours spread
  into stepped pixel halos (quantised alpha with Bayer dither, saturated), each frame padded so the halo is not cut.
  CombatFx draws them additively under the clip, so every skill looks lit.
- vfxcircle1..4_play_12.png: the cast circle under the hero per skill grade (a ground ellipse with baked colours).
- vfxpillar_play_9.png: a column of light rising from the caster (white ramp, tinted per grade).
- vfxflash_play_6.png: an impact starburst (white ramp, tinted per element).
- vfxshock_play_8.png: a ground shockwave ring with thrown dust (white ramp, tinted per element).
Glows are made last, from whatever sheets are in the folder, so a rerun picks up redrawn art too.
Hard edges, no anti-aliasing; fades are Bayer-dithered like vfxgen2.py.
"""
import glob
import math
import os
import random
import re
import sys

import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art', 'Vfx')
DRAWN = ('circle1', 'circle2', 'circle3', 'circle4', 'pillar', 'flash', 'shock')
args = [a for i, a in enumerate(sys.argv[1:], 1)
        if not a.startswith('--') and sys.argv[i - 1] != '--preview' and a not in DRAWN and a != 'glow']
if args:
    OUT = args[0]
ONLY = [a for a in sys.argv[1:] if a in DRAWN or a == 'glow']
PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

CORE = (255, 255, 255)
LIGHT = (232, 236, 246)
MID = (178, 186, 206)
RIM = (96, 102, 128)
WHITE_RAMP = (CORE, LIGHT, MID, RIM)
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32) / 16.0

# Magic circle palettes per grade (common, rare, epic, legendary): core, light, mid, rim.
CIRCLE_RAMPS = [
    ((255, 255, 255), (226, 232, 244), (168, 178, 200), (92, 100, 128)),
    ((236, 250, 255), (140, 214, 255), (64, 150, 240), (28, 70, 168)),
    ((252, 236, 255), (218, 166, 255), (162, 92, 240), (86, 40, 156)),
    ((255, 252, 222), (255, 226, 120), (244, 172, 40), (156, 92, 18)),
]

made = []


class Frame:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rgba = np.zeros((h, w, 4), np.uint8)
        ys, xs = np.mgrid[0:h, 0:w]
        self.x = xs.astype(np.float32) + 0.5
        self.y = ys.astype(np.float32) + 0.5
        self.bayer = BAYER[ys % 4, xs % 4]

    def paint(self, mask, color, alpha=255):
        m = mask.astype(bool)
        self.rgba[m, 0] = color[0]
        self.rgba[m, 1] = color[1]
        self.rgba[m, 2] = color[2]
        self.rgba[m, 3] = alpha

    def fade(self, keep):
        """Dither away pixels: keep is 0..1 (fraction of pixels kept)."""
        self.rgba[self.bayer >= keep, 3] = 0

    def image(self):
        return Image.fromarray(self.rgba, 'RGBA')

    def dist(self, cx, cy):
        return np.sqrt((self.x - cx) ** 2 + (self.y - cy) ** 2)


def seg_dist(f, ax, ay, bx, by):
    """Pixel distance to the segment a-b."""
    px, py = f.x - ax, f.y - ay
    ex, ey = bx - ax, by - ay
    t = np.clip((px * ex + py * ey) / max(ex * ex + ey * ey, 1e-6), 0, 1)
    dx, dy = px - ex * t, py - ey * t
    return np.sqrt(dx * dx + dy * dy)


def star_mask(f, cx, cy, r_long, r_short, width, n=4, rot=0.0):
    """n-pointed spiky star: diamonds along n directions."""
    m = np.zeros((f.h, f.w), bool)
    for k in range(n):
        a = rot + k * 2 * math.pi / n
        ca, sa = math.cos(a), math.sin(a)
        u = (f.x - cx) * ca + (f.y - cy) * sa
        v = -(f.x - cx) * sa + (f.y - cy) * ca
        r = r_long if k % 2 == 0 else r_short
        m |= (u >= 0) & (u <= r) & (np.abs(v) <= width * np.clip(1 - u / max(r, 1e-6), 0, 1))
    return m


def banded(f, dist_px, half, ramp):
    """A line of half-width `half` px (a number or a per-pixel array) around dist_px == 0: rim outside, then mid,
    light and a core centre (the inner bands only where the line is wide enough to show them)."""
    core, light, mid, rim = ramp
    half = np.asarray(half, np.float32)
    f.paint(dist_px <= half + 1.2, rim)
    f.paint(dist_px <= half, mid)
    f.paint((dist_px <= half * 0.62) & (half >= 1.2), light)
    f.paint((dist_px <= half * 0.28) & (half >= 2.0), core)


def save(frames, name):
    os.makedirs(OUT, exist_ok=True)
    for old in glob.glob(os.path.join(OUT, 'vfx%s_play_*.png' % name)):
        os.remove(old)
        if os.path.exists(old + '.meta'):
            os.remove(old + '.meta')
    w, h = frames[0].shape[1], frames[0].shape[0]
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, arr in enumerate(frames):
        sheet.paste(Image.fromarray(arr, 'RGBA'), (i * w, 0))
    sheet.save(os.path.join(OUT, 'vfx%s_play_%d.png' % (name, len(frames))))
    made.append((name, frames))


# Magic circles -------------------------------------------------------------------------------------------------------
def ellipse_point(cx, cy, rx, ry, r, a):
    return cx + r * rx * math.cos(a), cy + r * ry * math.sin(a)


def circle(grade):
    """Cast circle under the hero (ground ellipse). Grade 1 one ring and ticks ... grade 4 three rings, an eight-point
    star, rune glyphs turning the other way and spikes. Grows in, turns and pulses, then expands and dissolves."""
    W, H = 136, 52
    cx, cy = W / 2, H / 2
    RX, RY = 62.0, 22.5
    ramp = CIRCLE_RAMPS[grade - 1]
    core, light, mid, rim = ramp
    frames = []
    n = 12
    for i in range(n):
        f = Frame(W, H)
        grow = [0.32, 0.62, 0.88, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.04, 1.08, 1.12][i]
        rx, ry = RX * grow, RY * grow
        u = (f.x - cx) / rx
        v = (f.y - cy) / ry
        d = np.sqrt(u * u + v * v)
        a = np.arctan2(v, u)
        # Pixels per normalised radius unit at this angle: turns ring widths into pixels on the squashed ellipse.
        scale_px = np.sqrt((rx * np.cos(a)) ** 2 + (ry * np.sin(a)) ** 2)
        spin = i * 0.21
        pulse = 1.0 if i % 2 == 0 else 0.0

        def ring(r, half):
            banded(f, np.abs(d - r) * scale_px, half, ramp)

        if i <= 1:
            # Opening flash: a dithered disc of light.
            disc = d <= 1.0
            f.paint(disc & (f.bayer < 0.45), light)
            f.paint(d <= 0.5, core)

        rings = {1: [(0.94, 1.6)], 2: [(0.95, 1.8), (0.72, 1.1)], 3: [(0.96, 1.9), (0.76, 1.2), (0.42, 1.0)],
                 4: [(0.97, 2.1), (0.86, 1.0), (0.72, 1.3), (0.34, 1.0)]}[grade]
        for (r, half) in rings:
            ring(r, half + (0.4 if pulse and r > 0.9 else 0.0))

        # Ticks / runes between the outer rings, turning.
        marks = {1: 8, 2: 12, 3: 16, 4: 16}[grade]
        mark_r = {1: 0.82, 2: 0.835, 3: 0.86, 4: 0.79}[grade]
        for k in range(marks):
            ang = spin * (1 if grade < 4 else -1) + k * 2 * math.pi / marks
            x, y = ellipse_point(cx, cy, rx, ry, mark_r, ang)
            if grade == 1:
                x2, y2 = ellipse_point(cx, cy, rx, ry, mark_r + 0.07, ang)
                banded(f, seg_dist(f, x, y, x2, y2), 0.8, ramp)
            elif k % 2 == 0:
                f.paint((np.abs(f.x - x) + np.abs(f.y - y) * 1.6) <= 2.6, rim)
                f.paint((np.abs(f.x - x) + np.abs(f.y - y) * 1.6) <= 1.6, core if pulse else light)
            else:
                f.paint((np.abs(f.x - x) <= 1.0) & (np.abs(f.y - y) <= 1.0), mid)

        # Inner figure: a hexagram (epic) or an eight-point star (legendary), turning slowly.
        if grade >= 3:
            pts = 6 if grade == 3 else 8
            fig_r = 0.76 if grade == 3 else 0.72
            step = 2 if grade == 3 else 3
            for k in range(pts):
                a0 = spin * 0.5 + k * 2 * math.pi / pts
                a1 = spin * 0.5 + ((k + step) % pts) * 2 * math.pi / pts
                x0, y0 = ellipse_point(cx, cy, rx, ry, fig_r, a0)
                x1, y1 = ellipse_point(cx, cy, rx, ry, fig_r, a1)
                banded(f, seg_dist(f, x0, y0, x1, y1), 0.9, ramp)
        if grade == 2:
            # Four little petals inside the inner ring.
            for k in range(4):
                ang = -spin + k * math.pi / 2
                x, y = ellipse_point(cx, cy, rx, ry, 0.45, ang)
                f.paint((np.abs(f.x - x) * 0.7 + np.abs(f.y - y) * 1.3) <= 3.0, mid)
                f.paint((np.abs(f.x - x) * 0.7 + np.abs(f.y - y) * 1.3) <= 1.8, light)
        if grade == 4:
            # Spikes outside the outer ring and a bright core gem.
            for k in range(8):
                ang = spin * 0.8 + k * math.pi / 4
                x0, y0 = ellipse_point(cx, cy, rx, ry, 1.0, ang - 0.07)
                x1, y1 = ellipse_point(cx, cy, rx, ry, 1.0, ang + 0.07)
                xt, yt = ellipse_point(cx, cy, rx, ry, 1.13, ang)
                tri = np.minimum(seg_dist(f, x0, y0, xt, yt), seg_dist(f, x1, y1, xt, yt))
                f.paint(tri <= 1.4, mid)
                f.paint(tri <= 0.7, core)
            gem = (np.abs(f.x - cx) / 6 + np.abs(f.y - cy) / 3) <= 1.0
            f.paint(gem, rim)
            f.paint((np.abs(f.x - cx) / 4.5 + np.abs(f.y - cy) / 2.2) <= 1.0, light)
            f.paint((np.abs(f.x - cx) / 2 + np.abs(f.y - cy) / 1) <= 1.0, core)

        if i >= 9:
            f.fade([0.75, 0.5, 0.25][i - 9])
        frames.append(f.rgba.copy())
    save(frames, 'circle%d' % grade)


# Light pillar --------------------------------------------------------------------------------------------------------
def pillar():
    """A column of light rising from the caster's feet: shoots up, holds with rising sparkles, narrows and fades."""
    W, H = 48, 176
    cx = W / 2
    frames = []
    rnd = random.Random(31)
    motes = [(rnd.uniform(-14, 14), rnd.uniform(0, H), rnd.uniform(5, 9)) for _ in range(14)]
    for i in range(9):
        f = Frame(W, H)
        top = [H - 46, H - 120, 6, 0, 0, 0, 0, 0, 0][i]
        width = [5, 10, 14, 15, 14, 12, 9, 6, 3][i]
        dx = np.abs(f.x - cx)
        inside_h = f.y >= top
        # The beam narrows a little toward the top.
        taper = width * (0.75 + 0.25 * np.clip((f.y - top) / max(H - top, 1), 0, 1))
        banded(f, np.where(inside_h, np.maximum(dx - taper * 0.55, 0), 99), taper * 0.45 + 1.0, WHITE_RAMP)
        # Ground splash.
        if 1 <= i <= 6:
            rx = [0, 16, 21, 23, 23, 21, 17][i]
            e = np.sqrt(((f.x - cx) / rx) ** 2 + ((f.y - (H - 4)) / (rx * 0.28)) ** 2)
            f.paint((e <= 1.0) & (f.bayer < 0.7), LIGHT)
            f.paint(np.abs(e - 1.0) * rx <= 1.0, CORE)
        # Rising sparkles.
        if i >= 2:
            for (ox, oy, speed) in motes:
                y = (oy - (i - 2) * speed * 3) % H
                if y < top:
                    continue
                f.paint(star_mask(f, cx + ox, y, 3.2, 3.2, 1.1, 4), CORE)
        if i >= 6:
            f.fade([0.8, 0.55, 0.3][i - 6])
        frames.append(f.rgba.copy())
    save(frames, 'pillar')


# Impact flash --------------------------------------------------------------------------------------------------------
def flash():
    """Impact starburst: a white disc, an eight-ray star that stretches, a ring, then flying sparkles."""
    S = 96
    c = S / 2
    frames = []
    rnd = random.Random(13)
    shards = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(0.75, 1.25)) for _ in range(10)]
    for i in range(6):
        f = Frame(S, S)
        d = f.dist(c, c)
        if i == 0:
            f.paint(d <= 13, RIM)
            f.paint(d <= 11, LIGHT)
            f.paint(d <= 8, CORE)
        elif i in (1, 2):
            g = 1.0 if i == 1 else 1.3
            f.paint(star_mask(f, c, c, 44 * g, 26 * g, 6.5 if i == 1 else 4.5, 8, 0.2), RIM)
            f.paint(star_mask(f, c, c, 41 * g, 23 * g, 5.0 if i == 1 else 3.2, 8, 0.2), LIGHT)
            f.paint(star_mask(f, c, c, 30 * g, 14 * g, 2.6, 8, 0.2), CORE)
            f.paint(d <= (12 if i == 1 else 8), CORE)
            if i == 2:
                banded(f, np.abs(d - 22), 2.2, WHITE_RAMP)
        else:
            k = i - 2
            banded(f, np.abs(d - (24 + k * 6)), [2.0, 1.4, 0.9][k - 1], WHITE_RAMP)
            for (a, sp) in shards:
                rr = (20 + k * 9) * sp
                x, y = c + math.cos(a) * rr, c + math.sin(a) * rr
                f.paint(star_mask(f, x, y, 4.5 - k, 4.5 - k, 1.3, 4), CORE)
            if i == 5:
                f.fade(0.55)
        frames.append(f.rgba.copy())
    save(frames, 'flash')


# Ground shockwave ----------------------------------------------------------------------------------------------------
def shock():
    """A ground ring that races outward (squashed for the floor plane) and throws dust chunks up at its rim."""
    W, H = 168, 52
    cx, cy = W / 2, H - 16
    frames = []
    rnd = random.Random(17)
    chunks = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(0.85, 1.1), rnd.uniform(5, 12)) for _ in range(16)]
    for i in range(8):
        f = Frame(W, H)
        rx = [16, 34, 50, 62, 71, 77, 80, 82][i]
        ry = rx * 0.26
        half = [3.2, 3.0, 2.6, 2.2, 1.8, 1.4, 1.0, 0.8][i]
        u = (f.x - cx) / rx
        v = (f.y - cy) / ry
        d = np.sqrt(u * u + v * v)
        a = np.arctan2(v, u)
        scale_px = np.sqrt((rx * np.cos(a)) ** 2 + (ry * np.sin(a)) ** 2)
        banded(f, np.abs(d - 1.0) * scale_px, half, WHITE_RAMP)
        if i <= 2:
            f.paint((d <= 0.9) & (f.bayer < [0.55, 0.35, 0.18][i]), LIGHT)
        for (ang, rr, lift) in chunks:
            if i == 0:
                continue
            t = (i - 1) / 6.0
            x = cx + math.cos(ang) * rx * rr
            y = cy + math.sin(ang) * ry * rr - lift * math.sin(min(t, 1.0) * math.pi) * 1.4
            size = 2.6 - t * 1.6
            m = (np.abs(f.x - x) + np.abs(f.y - y)) <= size + 1
            f.paint(m, RIM)
            f.paint((np.abs(f.x - x) + np.abs(f.y - y)) <= size, LIGHT)
        if i >= 5:
            f.fade([0.75, 0.5, 0.28][i - 5])
        frames.append(f.rgba.copy())
    save(frames, 'shock')


# Glow sheets ---------------------------------------------------------------------------------------------------------
SHEET = re.compile(r'^vfx([a-z0-9]+)_play_(\d+)\.png$')
LEVELS = np.array([0.0, 0.2, 0.42, 0.66, 0.9], np.float32)


def glow_frame(rgba, pad):
    """One halo frame: blur colour and alpha, push the colour to full saturation and step the alpha (dithered)."""
    h, w = rgba.shape[:2]
    arr = np.zeros((h + 2 * pad, w + 2 * pad, 4), np.float32)
    arr[pad:pad + h, pad:pad + w] = rgba.astype(np.float32) / 255.0
    a = arr[..., 3]
    sigma = max(2.0, pad * 0.42)
    ba = gaussian_filter(a, sigma)
    rgb = np.stack([gaussian_filter(arr[..., k] * a, sigma) for k in range(3)], -1)
    col = rgb / np.maximum(ba[..., None], 1e-4)
    peak = col.max(-1, keepdims=True)
    col = col / np.maximum(peak, 0.35)
    col = col * 0.82 + 0.18
    alpha = np.clip(ba * 2.6, 0, 1)
    ys, xs = np.mgrid[0:alpha.shape[0], 0:alpha.shape[1]]
    dither = BAYER[ys % 4, xs % 4]
    steps = alpha * (len(LEVELS) - 1)
    lo = np.floor(steps).astype(int)
    frac = steps - lo
    idx = np.clip(lo + (frac > dither).astype(int), 0, len(LEVELS) - 1)
    out = np.zeros_like(arr)
    out[..., :3] = np.clip(col, 0, 1)
    out[..., 3] = LEVELS[idx]
    out[out[..., 3] <= 0.0] = 0
    return (out * 255).astype(np.uint8)


def glows():
    for path in sorted(glob.glob(os.path.join(OUT, 'vfx*_play_*.png'))):
        m = SHEET.match(os.path.basename(path))
        if not m:
            continue
        name, n = m.group(1), int(m.group(2))
        if name.endswith('glow') or name == 'bone':
            continue
        sheet = np.array(Image.open(path).convert('RGBA'))
        fw = sheet.shape[1] // n
        pad = int(round(max(6, min(fw, sheet.shape[0]) * 0.2)))
        frames = [glow_frame(sheet[:, k * fw:(k + 1) * fw], pad) for k in range(n)]
        save(frames, name + 'glow')


MAKERS = {'circle1': lambda: circle(1), 'circle2': lambda: circle(2), 'circle3': lambda: circle(3),
          'circle4': lambda: circle(4), 'pillar': pillar, 'flash': flash, 'shock': shock}

if __name__ == '__main__':
    for key, make in MAKERS.items():
        if not ONLY or key in ONLY:
            make()
    if not ONLY or 'glow' in ONLY:
        glows()
    if PREVIEW:
        shown = [(n, fr) for (n, fr) in made if not n.endswith('glow') or n in ('fireglow', 'meteorglow', 'boltglow', 'circle4glow')]
        cell_h = [max(fr[0].shape[0], 40) for _, fr in shown]
        width = max(len(fr) * (fr[0].shape[1] + 4) for _, fr in shown) + 8
        sheet = Image.new('RGBA', (width, sum(h + 8 for h in cell_h)), (40, 34, 60, 255))
        y = 4
        for (name, frames), h in zip(shown, cell_h):
            x = 4
            for arr in frames:
                sheet.alpha_composite(Image.fromarray(arr, 'RGBA'), (x, y))
                x += arr.shape[1] + 4
            y += h + 8
        sheet = sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST)
        sheet.save(PREVIEW)
    print('made', len(made), 'sheets in', os.path.abspath(OUT))
