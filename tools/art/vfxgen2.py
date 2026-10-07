"""Bolder pixel VFX (D-108) -> Assets/SoloHero/Art/Vfx/vfx{name}_play_{frames}.png.

Usage: python tools/art/vfxgen2.py [OUT_DIR] [--preview PREVIEW.png]
Redraws the five white "basic" clips that CombatFx tints per use (spark = hit, slash = basic hit, boom = death puff,
ring = level up, whirl = spin) and two weak skill clips (cross, tornado), larger and with a darker rim so they read on
both the snowy and the dark chapter backgrounds. Hard edges (no anti-aliasing), Bayer-dithered fade-outs.
D-109 adds "mark", the target sigil of the mark skills (armor break, hex, hunter's mark), tinted per skill.
D-110 adds "bone", the one-frame projectile the skeleton shooters throw (the view spins it).
Name clips on the command line to redraw only those.
Old strips with another frame count are deleted so ArtBuilder finds exactly one sheet per clip.
"""
import glob
import math
import os
import random
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art', 'Vfx')
CLIP_NAMES = ('spark', 'slash', 'boom', 'ring', 'whirl', 'cross', 'tornado', 'mark', 'bone')
args = [a for i, a in enumerate(sys.argv[1:], 1)
        if not a.startswith('--') and sys.argv[i - 1] != '--preview' and a not in CLIP_NAMES]
if args:
    OUT = args[0]
ONLY = [a for a in sys.argv[1:] if a in CLIP_NAMES]
PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

# White ramp for tinted clips: core, light, mid, rim (the rim reads on snow, the tint colours all of it).
CORE = (255, 255, 255)
LIGHT = (232, 236, 246)
MID = (178, 186, 206)
RIM = (96, 102, 128)
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32) / 16.0

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
        drop = self.bayer >= keep
        self.rgba[drop, 3] = 0

    def image(self):
        return Image.fromarray(self.rgba, 'RGBA')

    def dist(self, cx, cy):
        return np.sqrt((self.x - cx) ** 2 + (self.y - cy) ** 2)

    def ang(self, cx, cy):
        return np.arctan2(self.y - cy, self.x - cx)


def ring_bands(f, d, inner, outer, rim=True):
    """Thick ring between radii inner..outer with a rim, mid, light and core band from outside in."""
    w = outer - inner
    if w <= 0:
        return
    band = (d >= inner) & (d <= outer)
    t = np.clip((d - inner) / w, 0, 1)
    if rim:
        f.paint(band & ((t < 0.18) | (t > 0.82)), RIM)
        f.paint(band & (((t >= 0.18) & (t < 0.36)) | ((t > 0.64) & (t <= 0.82))), MID)
        f.paint(band & (t >= 0.36) & (t <= 0.64), CORE)
    else:
        f.paint(band, LIGHT)


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


def save(frames, name):
    os.makedirs(OUT, exist_ok=True)
    for old in glob.glob(os.path.join(OUT, 'vfx%s_play_*.png' % name)):
        os.remove(old)
        if os.path.exists(old + '.meta'):
            os.remove(old + '.meta')
    w, h = frames[0].w, frames[0].h
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f.image(), (i * w, 0))
    sheet.save(os.path.join(OUT, 'vfx%s_play_%d.png' % (name, len(frames))))
    made.append((name, frames))


# Clips -------------------------------------------------------------------------------------------------------------
def spark():
    """Hit impact (tinted per hit): flash disc, 8-ray burst, ring with flying shards, fading shards."""
    S = 40
    c = S / 2
    frames = []
    rnd = random.Random(7)
    shards = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(0.8, 1.2)) for _ in range(7)]
    for i in range(6):
        f = Frame(S, S)
        d = f.dist(c, c)
        if i == 0:
            f.paint(d <= 7, RIM)
            f.paint(d <= 5.5, CORE)
        elif i in (1, 2):
            grow = 1.0 if i == 1 else 1.25
            big = star_mask(f, c, c, 17 * grow, 10 * grow, 3.6 if i == 1 else 2.6, 8)
            outline = star_mask(f, c, c, 18.5 * grow, 11.5 * grow, 4.8 if i == 1 else 3.8, 8)
            f.paint(outline, RIM)
            f.paint(big, LIGHT)
            f.paint(star_mask(f, c, c, 11 * grow, 6 * grow, 2.0, 8), CORE)
            f.paint(d <= (6 if i == 1 else 4), CORE)
            if i == 2:
                ring_bands(f, d, 9, 12)
        else:
            k = i - 2
            if i == 3:
                ring_bands(f, d, 10 + k * 2.2, 12 + k * 2.2)
            for (a, sp) in shards:
                rr = (9 + k * 4.5) * sp
                x, y = c + math.cos(a) * rr, c + math.sin(a) * rr
                size = 2.2 if i < 5 else 1.3
                m = (np.abs(f.x - x) + np.abs(f.y - y)) <= size + 1
                f.paint(m, RIM)
                f.paint((np.abs(f.x - x) + np.abs(f.y - y)) <= size, CORE)
            if i == 5:
                f.fade(0.6)
        frames.append(f)
    save(frames, 'spark')


def slash():
    """Basic hit: a thick crescent swing that sweeps through, thins, and breaks into sparkles."""
    S = 56
    frames = []
    cx, cy = 18, 28
    for i in range(6):
        f = Frame(S, S)
        d = f.dist(cx, cy)
        a = f.ang(cx, cy)
        sweep = [0.35, 0.75, 1.0, 1.0, 1.0, 1.0][i]
        thick = [4, 8, 9, 6, 3.5, 2][i]
        r_out = [20, 22, 24, 26, 27, 28][i]
        a0, a1 = -1.25, -1.25 + 2.5 * sweep
        inside = (a >= a0) & (a <= a1)
        t = np.clip((a - a0) / max(a1 - a0, 1e-6), 0, 1)
        taper = np.sin(np.clip(t, 0, 1) * math.pi) ** 0.6
        inner = r_out - thick * taper - 0.5
        body = inside & (d <= r_out) & (d >= inner)
        rim = inside & (d <= r_out + 1.4) & (d >= inner - 1.2) & ~body
        f.paint(rim, RIM)
        f.paint(body, MID)
        mid = inside & (d <= r_out - 0.8) & (d >= inner + thick * taper * 0.25)
        f.paint(mid, LIGHT)
        core = inside & (d <= r_out - 1.6) & (d >= r_out - 1.6 - thick * taper * 0.45)
        f.paint(core, CORE)
        if i >= 3:
            rnd = random.Random(20 + i)
            for _ in range(5):
                aa = rnd.uniform(a0, a1)
                rr = r_out + rnd.uniform(1, 5) + (i - 3) * 2
                x, y = cx + math.cos(aa) * rr, cy + math.sin(aa) * rr
                m = (np.abs(f.x - x) + np.abs(f.y - y)) <= 1.6
                f.paint(m, CORE)
        if i == 5:
            f.fade(0.55)
        frames.append(f)
    save(frames, 'slash')


def boom():
    """Death puff (tinted white): a cartoon cloud of puffs that swells, sparkles, and dissolves."""
    S = 64
    c = S / 2
    frames = []
    rnd = random.Random(11)
    puffs = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(0.55, 1.0), rnd.uniform(0.75, 1.15)) for _ in range(7)]
    for i in range(7):
        f = Frame(S, S)
        k = [0.35, 0.65, 0.9, 1.0, 1.05, 1.1, 1.12][i]
        mask_out = np.zeros((S, S), bool)
        mask_in = np.zeros((S, S), bool)
        shade = np.zeros((S, S), bool)
        for (a, dist, size) in puffs:
            px = c + math.cos(a) * 11 * dist * k
            py = c + 4 + math.sin(a) * 8 * dist * k - i * 0.8
            r = (7 + 3 * size) * k * (1 - 0.12 * max(0, i - 3))
            d = f.dist(px, py)
            mask_out |= d <= r + 1.5
            mask_in |= d <= r
            shade |= (d <= r) & (f.y > py + r * 0.25)
        core_r = 9 * k * (1 - 0.15 * max(0, i - 3))
        dcore = f.dist(c, c + 2 - i * 0.6)
        mask_out |= dcore <= core_r + 1.5
        mask_in |= dcore <= core_r
        f.paint(mask_out, RIM)
        f.paint(mask_in, LIGHT)
        f.paint(shade & mask_in, MID)
        f.paint(mask_in & (f.y < c - 2 + i * 0.5) & (dcore <= core_r * 0.7), CORE)
        if 1 <= i <= 4:
            for kk in range(4):
                a = kk * math.pi / 2 + math.pi / 4 + i * 0.3
                rr = 20 + i * 3
                x, y = c + math.cos(a) * rr, c + math.sin(a) * rr
                f.paint(star_mask(f, x, y, 4.5, 4.5, 1.5, 4), CORE)
        if i >= 4:
            f.fade([1.0, 1.0, 1.0, 1.0, 0.8, 0.55, 0.3][i])
        frames.append(f)
    save(frames, 'boom')


def ring():
    """Level up: a thick ring bursts out with a soft inner flash and rising sparkles."""
    S = 72
    c = S / 2
    frames = []
    for i in range(7):
        f = Frame(S, S)
        d = f.dist(c, c)
        r = [8, 15, 21, 26, 30, 33, 35][i]
        w = [6, 7, 6, 5, 4, 3, 2][i]
        if i < 3:
            f.paint(d <= r - w - 1, LIGHT)
            f.fade([0.5, 0.35, 0.2][i] + 0.0)
        ring_bands(f, d, r - w, r)
        for kk in range(6):
            a = kk * math.pi / 3 + i * 0.15
            rr = r + 3
            x, y = c + math.cos(a) * rr, c + math.sin(a) * rr - i * 1.5
            if i >= 2:
                f.paint(star_mask(f, x, y, 3.5, 3.5, 1.2, 4), CORE)
        if i >= 5:
            f.fade([1, 1, 1, 1, 1, 0.65, 0.35][i])
        frames.append(f)
    save(frames, 'ring')


def whirl():
    """Spin attack: two thick blades of wind circling and widening."""
    S = 64
    c = S / 2
    frames = []
    for i in range(6):
        f = Frame(S, S)
        d = f.dist(c, c)
        a = f.ang(c, c)
        r = 14 + i * 2.6
        thick = [5, 6, 6, 5, 4, 3][i]
        for arm in range(2):
            start = i * 0.9 + arm * math.pi
            rel = np.mod(a - start, 2 * math.pi)
            length = 2.2
            on = rel <= length
            t = np.clip(rel / length, 0, 1)
            w = thick * np.sin(t * math.pi) ** 0.7
            band = on & (d <= r + w * 0.5) & (d >= r - w * 0.5)
            edge = on & (d <= r + w * 0.5 + 1.2) & (d >= r - w * 0.5 - 1.2) & ~band
            f.paint(edge & (w > 0.6), RIM)
            f.paint(band, LIGHT)
            f.paint(on & (np.abs(d - r) <= w * 0.2), CORE)
        if i >= 4:
            f.fade([1, 1, 1, 1, 0.75, 0.45][i])
        frames.append(f)
    save(frames, 'whirl')


# Coloured skill clips ----------------------------------------------------------------------------------------------
STEEL = [(255, 255, 255), (206, 230, 255), (120, 170, 236), (44, 64, 128)]


def cross():
    """Cross slash (dragon slash etc., tinted): two thick blade arcs crossing with a bright X flash."""
    S = 72
    c = S / 2
    frames = []
    for i in range(7):
        f = Frame(S, S)
        for sgn in (1, -1):
            ang = math.pi / 4 * sgn
            ca, sa = math.cos(ang), math.sin(ang)
            u = (f.x - c) * ca + (f.y - c) * sa
            v = -(f.x - c) * sa + (f.y - c) * ca
            reach = [10, 26, 30, 30, 30, 30, 30][i]
            if sgn == -1 and i == 0:
                continue
            width = [2.5, 4.5, 5.5, 4.5, 3.2, 2.2, 1.4][i]
            t = np.clip(1 - np.abs(u) / reach, 0, 1)
            w = width * t ** 0.5
            body = (np.abs(u) <= reach) & (np.abs(v) <= w)
            edge = (np.abs(u) <= reach + 1) & (np.abs(v) <= w + 1.3) & ~body
            f.paint(edge, STEEL[3])
            f.paint(body, STEEL[2])
            f.paint((np.abs(u) <= reach * 0.9) & (np.abs(v) <= w * 0.55), STEEL[1])
            f.paint((np.abs(u) <= reach * 0.75) & (np.abs(v) <= w * 0.22), STEEL[0])
        if 1 <= i <= 3:
            d = f.dist(c, c)
            f.paint(d <= [0, 6, 8, 5][i], STEEL[0])
            f.paint(star_mask(f, c, c, [0, 14, 18, 12][i], [0, 14, 18, 12][i], 2.0, 4), STEEL[0])
        if i >= 5:
            f.fade([1, 1, 1, 1, 1, 0.6, 0.3][i])
        frames.append(f)
    save(frames, 'cross')


WIND = [(255, 255, 255), (220, 246, 255), (150, 210, 240), (60, 110, 160)]


def tornado():
    """Tornado (rises from the ground): a twisting funnel of wind bands with debris."""
    W, H = 64, 96
    frames = []
    rnd = random.Random(5)
    debris = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(0.1, 0.9)) for _ in range(10)]
    for i in range(8):
        f = Frame(W, H)
        grow = min(1.0, (i + 1) / 3)
        top = H - 6 - (H - 14) * grow
        for b in range(9):
            y = H - 6 - b * 9.5
            if y < top:
                break
            k = (H - 6 - y) / (H - 14)
            half = 5 + 22 * k
            phase = i * 0.9 + b * 0.7
            cx = W / 2 + math.sin(phase) * (2 + 4 * k)
            ell = ((f.x - cx) / half) ** 2 + ((f.y - y) / 3.6) ** 2
            back = (ell <= 1.0) & (f.y < y)
            front = (ell <= 1.0) & (f.y >= y)
            ring = (ell <= 1.0) & (ell >= 0.45)
            f.paint(back & ring, WIND[3])
            f.paint(front & ring, WIND[2])
            f.paint(front & (ell >= 0.62) & (ell <= 0.85), WIND[1])
            hi = (np.abs(f.x - (cx - half * 0.55)) <= 1.6) & (np.abs(f.y - y - 1) <= 1.2)
            f.paint(hi, WIND[0])
        for (a, k) in debris:
            y = H - 8 - k * (H - 20) * grow
            x = W / 2 + math.cos(a + i * 1.1) * (6 + 22 * k)
            m = (np.abs(f.x - x) <= 1.5) & (np.abs(f.y - y) <= 1.5)
            f.paint(m, (150, 120, 90))
        if i >= 6:
            f.fade([1, 1, 1, 1, 1, 1, 0.7, 0.4][i])
        frames.append(f)
    save(frames, 'tornado')


def mark():
    """D-109 mark (tinted): a crosshair ring closes on the target with four arrowheads, flashes, then a rune ring with
    eight ticks and an X turns and fades while the mark lasts its first moment."""
    S = 64
    c = S / 2
    frames = []
    for i in range(8):
        f = Frame(S, S)
        d = f.dist(c, c)
        a = f.ang(c, c)
        if i <= 2:
            r = [27, 22, 18][i]
            ring_bands(f, d, r - [1.5, 2.2, 3.0][i], r + [1.5, 2.2, 3.0][i])
            reach = r + 4
            for k in range(4):
                ang = k * math.pi / 2 + math.pi / 4
                ca, sa = math.cos(ang), math.sin(ang)
                u = (f.x - c) * ca + (f.y - c) * sa
                v = -(f.x - c) * sa + (f.y - c) * ca
                tri = (u >= reach - 2) & (u <= reach + 7) & (np.abs(v) <= (u - (reach - 2)) * 0.6)
                f.paint(tri & ((u > reach + 5) | (np.abs(v) > (u - (reach - 2)) * 0.6 - 1.2)), RIM)
                f.paint(tri & (u <= reach + 5) & (np.abs(v) <= (u - (reach - 2)) * 0.6 - 1.2), CORE)
            if i == 2:
                f.paint(d <= 3.5, CORE)
            if i == 0:
                f.fade(0.7)
        elif i == 3:
            ring_bands(f, d, 13, 19)
            f.paint(star_mask(f, c, c, 16, 16, 3.0, 4, math.pi / 4), CORE)
            f.paint(d <= 6, CORE)
        else:
            spin = (i - 4) * 0.25
            ring_bands(f, d, 14, 18.5)
            for k in range(8):
                ang = spin + k * math.pi / 4
                ca, sa = math.cos(ang), math.sin(ang)
                u = (f.x - c) * ca + (f.y - c) * sa
                v = -(f.x - c) * sa + (f.y - c) * ca
                tick = (u >= 20) & (u <= 24 + (k % 2) * 2) & (np.abs(v) <= 1.6)
                f.paint(tick, RIM if k % 2 else LIGHT)
            for sgn in (1, -1):
                ang = math.pi / 4 * sgn + spin
                ca, sa = math.cos(ang), math.sin(ang)
                u = (f.x - c) * ca + (f.y - c) * sa
                v = -(f.x - c) * sa + (f.y - c) * ca
                f.paint((np.abs(u) <= 9) & (np.abs(v) <= 2.6), MID)
                f.paint((np.abs(u) <= 8) & (np.abs(v) <= 1.2), CORE)
            f.fade([1, 1, 1, 1, 1.0, 0.8, 0.55, 0.3][i])
        frames.append(f)
    save(frames, 'mark')


def bone():
    """D-110 thrown bone (one frame, untinted): a cream shaft with round knobs at both ends and a dark rim."""
    W, H = 18, 10
    f = Frame(W, H)
    ink = (52, 40, 64)
    cream = (240, 232, 208)
    light = (255, 255, 248)
    shade = (196, 182, 152)
    shaft = (f.x >= 5) & (f.x <= 13) & (np.abs(f.y - 5) <= 1.6)
    knobs = np.zeros((H, W), bool)
    for cx in (3.5, 14.5):
        for cy in (3.4, 6.6):
            knobs |= f.dist(cx, cy) <= 2.3
    body = shaft | knobs
    rim = np.zeros((H, W), bool)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        rim |= np.roll(np.roll(body, dy, axis=0), dx, axis=1)
    f.paint(rim & ~body, ink)
    f.paint(body, cream)
    f.paint(body & (f.y > 6.2), shade)
    f.paint(shaft & (np.abs(f.y - 4.4) <= 0.5), light)
    save([f], 'bone')


if __name__ == '__main__':
    for name in CLIP_NAMES:
        if ONLY and name not in ONLY:
            continue
        globals()[name]()
    if PREVIEW:
        scale = 4
        rows = []
        for name, frames in made:
            w, h = frames[0].w, frames[0].h
            row = Image.new('RGBA', (len(frames) * w * scale + 8 * len(frames), h * scale), (0, 0, 0, 0))
            for i, fr in enumerate(frames):
                row.alpha_composite(fr.image().resize((w * scale, h * scale), Image.NEAREST), (i * (w * scale + 8), 0))
            rows.append(row)
        width = max(r.width for r in rows) * 2 + 40
        height = sum(r.height + 16 for r in rows)
        sheet = Image.new('RGBA', (width, height), (0, 0, 0, 0))
        left = Image.new('RGBA', (width // 2, height), (40, 44, 80, 255))
        right = Image.new('RGBA', (width - width // 2, height), (220, 232, 244, 255))
        sheet.alpha_composite(left, (0, 0))
        sheet.alpha_composite(right, (width // 2, 0))
        y = 0
        for r in rows:
            sheet.alpha_composite(r, (8, y))
            sheet.alpha_composite(r, (width // 2 + 8, y))
            y += r.height + 16
        sheet.save(PREVIEW)
    print('made', [n for n, _ in made], 'in', os.path.abspath(OUT))
