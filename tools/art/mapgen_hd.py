"""Ink-cel chapter maps (D-168): the bggen.py backgrounds and floorgen.py floors redrawn smooth.

Same themes, colours and layout as the pixel versions (bggen.THEMES, floorgen.THEMES: five parallax layers 320 x 320
art px bottom-anchored on the ground line, and a 64 x 160 floor tile), drawn as flat cel shapes with anti-aliased
edges at K texels per art pixel (importer PPU 32 * K = 96, so world sizes do not change):
- sky: a smooth gradient (no dithering), two-tone clouds, twinkle stars, a ringed sun or moon;
- ridges: smooth silhouettes with a lit top rim and soft snow caps;
- trees and near foliage: shapes with a shadow crescent away from the light, a highlight toward it and a
  darker-hue outline (only on the near layers, so the far ones stay soft);
- floor: the perspective bands with soft edges, rounded pebbles, grass blades and a tufted far edge.
Every shape wraps horizontally, so each layer still tiles.

Run: python tools/art/mapgen_hd.py [--preview out.png]  ->  Art/Backgrounds/{theme}/layer_{0..4}.png, Art/Tiles/floor_{n}.png
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import bggen  # noqa: E402
import floorgen  # noqa: E402

K = 3           # output texels per art pixel
AA = 2          # supersampling
S = K * AA
W, H, HORIZON = bggen.W, bggen.H, bggen.HORIZON
LIGHT_DIR = (-0.6, 0.8)     # toward the light (left and up), art px per unit


def rgb(c):
    c = bggen.hexc(c) if isinstance(c, str) else c
    return np.array(c[:3], dtype=np.float64)


def mix(a, b, t):
    return rgb(a) * (1 - t) + rgb(b) * t


def darker(c, k):
    """A darker, slightly more saturated version of c (outline ink taking the hue of what it outlines)."""
    v = rgb(c)
    grey = v.mean()
    v = grey + (v - grey) * 1.25
    return np.clip(v * k, 0, 255)


class Canvas:
    """Supersampled RGBA layer in art coordinates: x to the right (wraps at w), y up from the bottom edge."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rgb = np.zeros((h * S, w * S, 3))
        self.a = np.zeros((h * S, w * S))
        j = (np.arange(w * S) + 0.5) / S
        i = h - (np.arange(h * S) + 0.5) / S
        self.x = j[None, :]
        self.y = i[:, None]

    def dx(self, cx):
        """Wrapped x distance to cx."""
        return (self.x - cx + self.w / 2.0) % self.w - self.w / 2.0

    def paint(self, mask, color, alpha=1.0):
        mask = np.broadcast_to(mask, self.a.shape)
        c = rgb(color) if not isinstance(color, np.ndarray) or color.ndim == 1 else color
        if isinstance(alpha, np.ndarray):
            a = np.where(mask, alpha, 0.0)
        else:
            a = mask * float(alpha)
        if c.ndim == 1:
            self.rgb = self.rgb * (1 - a[..., None]) + c[None, None, :] * a[..., None]
        else:
            self.rgb = self.rgb * (1 - a[..., None]) + c * a[..., None]
        self.a = self.a + a * (1 - self.a)

    def image(self):
        hh, ww = self.h * K, self.w * K
        pre = np.dstack([self.rgb * self.a[..., None], self.a])
        pre = pre.reshape(hh, AA, ww, AA, 4).mean(axis=(1, 3))
        out = np.zeros((hh, ww, 4))
        nz = pre[..., 3] > 1e-6
        out[nz, :3] = pre[nz, :3] / pre[nz, 3:4]
        out[..., 3] = pre[..., 3] * 255
        return Image.fromarray(np.clip(out + 0.5, 0, 255).astype(np.uint8), "RGBA")


def shift(mask, dx, dy):
    """mask moved by (dx, dy) art px (x wraps, y fills empty)."""
    px, py = int(round(dx * S)), int(round(-dy * S))
    out = np.roll(mask, px, axis=1)
    if py > 0:
        out = np.vstack([np.zeros((py, out.shape[1]), bool), out[:-py]])
    elif py < 0:
        out = np.vstack([out[-py:], np.zeros((-py, out.shape[1]), bool)])
    return out


def grow(mask, r):
    """Mask dilated by r art px (a disk)."""
    out = mask.copy()
    n = max(1, int(round(r * S)))
    for dy in range(-n, n + 1):
        for dx in range(-n, n + 1):
            if dx * dx + dy * dy <= n * n and (dx or dy):
                out |= shift(mask, dx / S, dy / S)
    return out


def cel(cv, mask, dark, mid, light, outline=None, width=0.8, depth=1.6, shine=0.9):
    """A cel-shaded shape: mid fill, a dark crescent away from the light, a light rim toward it, an outline."""
    if outline is not None:
        cv.paint(grow(mask, width) & ~mask, outline)
    cv.paint(mask, mid)
    lx, ly = LIGHT_DIR
    cv.paint(mask & ~shift(mask, -lx * depth, -ly * depth), dark)
    if light is not None and shine > 0:
        cv.paint(mask & ~shift(mask, lx * shine, ly * shine), light)


def periodic(seed, terms, amp):
    """bggen.periodic, vectorised (the same random draws, so the same silhouettes)."""
    rng = random.Random(seed)
    waves = [(k, rng.uniform(0, math.tau), rng.uniform(0.5, 1.0) / k ** 0.8) for k in terms]
    total = sum(a for _, _, a in waves)
    return lambda x: amp * sum(a * np.sin(math.tau * k * x / W + p) for k, p, a in waves) / total


def peaks(seed, amp):
    rng = random.Random(seed)
    tips = [(rng.randrange(W), rng.uniform(0.5, 1.0)) for _ in range(6)]

    def f(x):
        best = np.zeros_like(x, dtype=np.float64)
        for tx, a in tips:
            d = np.minimum(np.abs(x - tx), W - np.abs(x - tx))
            # Rounded tips: a smooth max of the tent with a small parabola at the top.
            t = np.maximum(0.0, 1.0 - d / 48.0)
            best = np.maximum(best, a * amp * (t - 0.08 * np.exp(-(d / 4.0) ** 2)))
        return best

    return f


# ------------------------------------------------------------------------------------------------- sky
def sky_layer(t, seed):
    cv = Canvas(W, H)
    bands = [rgb(c) for c in t["sky"]]
    u = np.clip((H - 1 - cv.y) / (H - 1 - HORIZON), 0, 1) * (len(bands) - 1)
    i = np.minimum(u.astype(int), len(bands) - 2)
    f = u - i
    f = f * f * (3 - 2 * f)
    grad = np.stack([bands[k] for k in range(len(bands))])
    col = grad[i] * (1 - f[..., None]) + grad[i + 1] * f[..., None]
    cv.paint(np.ones_like(cv.a, bool), np.broadcast_to(col, cv.rgb.shape))
    rng = random.Random(seed)
    deco = t["deco"]
    cloud = [rgb(c) for c in t["cloud"]]
    if deco in ("clouds", "sun", "rays"):
        for _ in range(5 if deco != "rays" else 3):
            cx, cy = rng.randrange(W), rng.randrange(150, 290)
            m = np.zeros_like(cv.a, bool)
            puffs = [(cx + j * rng.randrange(7, 12), cy + rng.randrange(-3, 5), rng.randrange(6, 12))
                     for j in range(rng.randrange(3, 6))]
            for px, py, r in puffs:
                m |= cv.dx(px) ** 2 + (cv.y - py - r * 0.35) ** 2 <= (r * 0.85) ** 2
            # A flat-bottomed base with round ends under the puffs.
            x0, x1 = puffs[0][0], puffs[-1][0]
            mid_x, half = (x0 + x1) / 2.0, (x1 - x0) / 2.0 + 4
            base = (np.abs(cv.dx(mid_x)) <= half) & (np.abs(cv.y - cy) <= 4)
            base |= ((np.abs(cv.dx(mid_x)) - half) ** 2 + (cv.y - cy) ** 2 <= 16) & (np.abs(cv.dx(mid_x)) > half)
            m |= base
            m &= cv.y >= cy - 4
            cv.paint(m, cloud[1])
            cv.paint(m & shift(m, 0, -2.4), cloud[0])
    if deco == "rays":
        light = rgb(t["cloud"][0])
        for k in range(4):
            x0 = 30 + k * 80
            off = cv.dx(x0 + (H - cv.y) / 3.0)
            w = 6 + (H - cv.y) / 30.0
            ray = (off >= 0) & (off <= w) & (cv.y >= HORIZON)
            cv.paint(ray, light, 0.28)
    if deco == "night":
        for _ in range(70):
            sx, sy = rng.randrange(W), rng.randrange(120, H)
            big = rng.random() < 0.18
            c = (255, 255, 255) if rng.random() < 0.6 else cloud[1]
            d = np.abs(cv.dx(sx + 0.5)), np.abs(cv.y - sy - 0.5)
            if big:
                star = (d[0] * d[1] <= 0.35) & (d[0] + d[1] <= 3.2) | (d[0] ** 2 + d[1] ** 2 <= 0.8)
            else:
                star = d[0] ** 2 + d[1] ** 2 <= 0.42
            cv.paint(star, c)
        moon = cv.dx(230) ** 2 + (cv.y - 255) ** 2
        cv.paint(moon <= 20 ** 2, cloud[0], 0.12)
        cv.paint(moon <= 14 ** 2, cloud[1])
        cv.paint((moon <= 14 ** 2) & (cv.dx(226) ** 2 + (cv.y - 259) ** 2 <= 12 ** 2), cloud[0])
    if deco == "sun":
        sun = cv.dx(200) ** 2 + (cv.y - 110) ** 2
        cv.paint(sun <= 36 ** 2, rgb("#fff0c8"), 0.18)
        cv.paint(sun <= 30 ** 2, rgb("#fff0c8"), 0.25)
        cv.paint(sun <= 26 ** 2, cloud[1])
        cv.paint(sun <= 22 ** 2, rgb("#fff0c8"))
    if deco == "embers":
        sun = cv.dx(210) ** 2 + (cv.y - 140) ** 2
        cv.paint(sun <= 28 ** 2, cloud[0], 0.2)
        cv.paint(sun <= 22 ** 2, cloud[1])
        cv.paint(sun <= 18 ** 2, cloud[0])
        for _ in range(90):
            ex, ey = rng.randrange(W), rng.randrange(HORIZON, H)
            r = rng.uniform(0.5, 1.1)
            cv.paint(cv.dx(ex) ** 2 + (cv.y - ey) ** 2 <= r * r, cloud[0] if rng.random() < 0.5 else cloud[1])
    return cv


# ------------------------------------------------------------------------------------------------- ridges
def ridge_layer(fn, base, colors, seed, snow=None, dots=None, rim=True):
    cv = Canvas(W, H)
    rng = random.Random(seed)
    top = base + fn(cv.x)
    m = (cv.y <= top) & (cv.y >= HORIZON - 10)
    cv.paint(m, colors[0])
    if snow is not None:
        cv.paint(m & (cv.y > snow(cv.x)), colors[1])
        # Snow edge: a soft shade under the cap reads as volume.
        cv.paint(m & (cv.y > snow(cv.x)) & (cv.y < snow(cv.x) + 1.5), mix(colors[1], colors[0], 0.35))
    elif rim and len(colors) > 1:
        slope = fn(cv.x + 1) - fn(cv.x - 1)
        cv.paint(m & (cv.y > top - 1.6) & (slope > 0), colors[1])
    if dots:
        for _ in range(dots[1]):
            x = rng.randrange(W)
            t = base + float(fn(np.array([float(x)]))[0])
            y = rng.randrange(HORIZON, max(HORIZON + 1, int(t) - 2))
            cv.paint(cv.dx(x + 0.5) ** 2 + (cv.y - y - 0.5) ** 2 <= 0.55, dots[0])
    return cv


# ------------------------------------------------------------------------------------------------- trees
def tri(cv, cx, y0, half, h):
    """Upright triangle (pine tier) with a slightly rounded tip."""
    u = (cv.y - y0) / h
    return (u >= 0) & (u <= 1) & (np.abs(cv.dx(cx)) <= half * (1 - u) + 0.6 * (u > 0.85))


def tree(cv, x, base, kind, colors, trunk, rng, scale, outline):
    dark, mid, light = colors
    ink = darker(dark, 0.62) if outline else None
    if kind == "cactus":
        h = int(rng.randrange(22, 34) * scale)
        w = max(2, int(3 * scale))
        m = (np.abs(cv.dx(x)) <= w) & (cv.y >= base) & (cv.y <= base + h - w)
        m |= cv.dx(x) ** 2 + (cv.y - (base + h - w)) ** 2 <= w * w
        for side in (-1, 1):
            if rng.random() < 0.8:
                ay = base + int(h * rng.uniform(0.35, 0.6))
                ah = int(h * 0.35)
                ax = x + side * (w + 3)
                m |= (np.abs(cv.dx(x + side * (w + 1.5))) <= 2.5) & (np.abs(cv.y - ay) <= 1.4)
                m |= (np.abs(cv.dx(ax)) <= 1.4) & (cv.y >= ay) & (cv.y <= ay + ah)
                m |= cv.dx(ax) ** 2 + (cv.y - ay - ah) ** 2 <= 1.4 ** 2
        cel(cv, m, dark, mid, light, ink, depth=1.4, shine=0.8)
        return
    if kind == "dead":
        h = int(rng.randrange(26, 40) * scale)
        m = (cv.dx(x + 0.5) ** 2 <= 1.0) & (cv.y >= base) & (cv.y <= base + h)
        m &= np.abs(cv.dx(x + 0.5)) <= 1.0 + 0.6 * (1 - (cv.y - base) / h)
        for _ in range(3):
            by = base + int(h * rng.uniform(0.45, 0.95))
            side = rng.choice((-1, 1))
            length = int(rng.randrange(6, 12) * scale)
            d = cv.dx(x)
            along = d * side
            m |= (along >= 0) & (along <= length) & (np.abs(cv.y - (by + along / 2.0)) <= 0.9 - 0.4 * along / length)
        cel(cv, m, dark, trunk, mid, ink, depth=1.0, shine=0.0)
        return
    if kind == "crystal":
        h = int(rng.randrange(18, 32) * scale)
        for cx, hh in ((x - 6, int(h * 0.6)), (x + 6, int(h * 0.7)), (x, h)):
            w = max(2, hh // 5)
            u = (cv.y - base) / hh
            half = np.where(u > 0.3, w * (1 - (u - 0.3) / 0.7), w)
            m = (u >= 0) & (u <= 1) & (np.abs(cv.dx(cx)) <= half)
            if ink is not None:
                cv.paint(grow(m, 0.7) & ~m, ink)
            cv.paint(m, mid)
            cv.paint(m & (cv.dx(cx) < -half * 0.15), light)
            cv.paint(m & (cv.dx(cx) > half * 0.35), dark)
        return
    if kind == "spire":
        h = int(rng.randrange(30, 48) * scale)
        w = max(3, int(5 * scale))
        m = (np.abs(cv.dx(x)) <= w) & (cv.y >= base) & (cv.y <= base + h)
        roof = w * 2.2
        u = (cv.y - base - h) / roof
        m |= (u >= 0) & (u <= 1) & (np.abs(cv.dx(x)) <= (w + 1) * (1 - u))
        if ink is not None:
            cv.paint(grow(m, 0.7) & ~m, ink)
        cv.paint(m, dark)
        cv.paint(m & (cv.y <= base + h) & (cv.dx(x) < w * 0.4), mid)
        win = (np.abs(cv.dx(x)) <= 0.7) & (np.abs(cv.y - (base + int(h * 0.7)) - 0.6) <= 1.2)
        cv.paint(win, light)
        return
    if kind == "round":
        r = int(rng.randrange(9, 14) * scale)
        tm = (np.abs(cv.dx(x + 0.5)) <= 1.1) & (cv.y >= base) & (cv.y <= base + r + 1)
        if ink is not None:
            cv.paint(grow(tm, 0.7) & ~tm, ink)
        cv.paint(tm, trunk)
        cy = base + r + r // 2
        crown = cv.dx(x) ** 2 + (cv.y - cy) ** 2 <= r * r
        # Two side puffs make the crown read as foliage, not a ball.
        crown |= cv.dx(x - r * 0.62) ** 2 + (cv.y - cy + r * 0.3) ** 2 <= (r * 0.62) ** 2
        crown |= cv.dx(x + r * 0.62) ** 2 + (cv.y - cy + r * 0.25) ** 2 <= (r * 0.6) ** 2
        cel(cv, crown, dark, mid, light, ink, depth=r * 0.22, shine=r * 0.12)
        return
    height = int(rng.randrange(34, 50) * scale * (1.6 if kind == "pine_big" else 1.0))
    width = int(height * 0.36)
    tm = (np.abs(cv.dx(x + 0.5)) <= 1.0) & (cv.y >= base) & (cv.y <= base + height // 6 + 2)
    cv.paint(tm, trunk)
    body = np.zeros_like(cv.a, bool)
    y0 = base + height // 6
    tiers = 4
    for k in range(tiers):
        ty = y0 + height * k / (tiers + 0.6)
        th = height - (ty - y0)
        body |= tri(cv, x, ty, width * (1 - k / (tiers + 1.5)) + 2, th * 0.55 if k < tiers - 1 else th)
    cel(cv, body, dark, mid, light, ink, depth=width * 0.18, shine=width * 0.1)


def tree_layer(t, seed):
    cv = Canvas(W, H)
    rng = random.Random(seed)
    colors = [rgb(c) for c in t["trees"]]
    trunk = rgb(t["trunk"])
    kind = t["tree"]
    for row, (base, count, scale) in enumerate(((HORIZON + 6, 9, 0.75), (HORIZON - 2, 7, 1.0))):
        xs = sorted(rng.randrange(W) for _ in range(count))
        for x in xs:
            cols = colors if row else [colors[0], colors[0], mix(colors[0], colors[1], 0.5)]
            tree(cv, x, base, kind, cols, trunk, rng, scale, outline=row == 1)
    return cv


def near_layer(t, seed):
    cv = Canvas(W, H)
    rng = random.Random(seed)
    dark, mid, light = [rgb(c) for c in t["near"]]
    ink = darker(dark, 0.6)
    fn = periodic(seed, (3, 5, 9, 14), 8)
    top = HORIZON + 6 + fn(cv.x)
    ground = (cv.y <= top) & (cv.y >= HORIZON - 10)
    bushes = np.zeros_like(ground)
    for _ in range(26):
        bx, by, r = rng.randrange(W), HORIZON + rng.randrange(6, 14), rng.randrange(5, 9)
        bushes |= cv.dx(bx) ** 2 + (cv.y - by) ** 2 <= r * r
    blades = np.zeros_like(ground)
    for _ in range(40):
        x = rng.randrange(W)
        t_ = HORIZON + 8 + float(fn(np.array([float(x)]))[0]) + rng.randrange(2, 6)
        lean = rng.uniform(-0.25, 0.25)
        u = (cv.y - (HORIZON + 4)) / max(1.0, t_ - HORIZON - 4)
        blades |= (u >= 0) & (u <= 1) & (np.abs(cv.dx(x + 0.5 + lean * (cv.y - HORIZON - 4))) <= 0.8 * (1 - u) + 0.15)
    whole = ground | bushes
    cv.paint(grow(whole | blades, 0.8) & ~(whole | blades), ink)
    cv.paint(ground, dark)
    cv.paint(ground & (cv.y > top - 2.5), mid)
    cel(cv, bushes, dark, mid, mix(mid, light, 0.55), None, depth=1.8, shine=0.6)
    cv.paint(blades, mid)
    cv.paint(blades & ~shift(blades, 0, 1.2), light)
    return cv


def build(t, seed):
    layers = [sky_layer(t, seed)]
    far = [rgb(c) for c in t["far"]]
    mid = [rgb(c) for c in t["mid"]]
    shape = t["far_shape"]
    if shape == "peaks":
        f = peaks(seed + 1, 150)
        g = periodic(seed + 2, (2, 5, 11), 10)
        layers.append(ridge_layer(lambda x: f(x) + g(x), 70, far, seed, snow=lambda x: 150 + 6 * np.sin(x / 5.0)))
        m = peaks(seed + 3, 80)
        layers.append(ridge_layer(lambda x: m(x) + g(x) * 0.6, 62, mid, seed + 3, snow=lambda x: 112 + 4 * np.sin(x / 4.0)))
    elif shape == "peaks_soft":
        f = peaks(seed + 1, 120)
        g = periodic(seed + 2, (2, 4, 9), 12)
        layers.append(ridge_layer(lambda x: f(x) * 0.8 + g(x), 80, far, seed))
        layers.append(ridge_layer(periodic(seed + 3, (2, 3, 7), 22), 78, mid, seed + 3, dots=(far[1], 50)))
    else:
        layers.append(ridge_layer(periodic(seed + 1, (1, 2, 3, 5), 34), 120, far, seed))
        layers.append(ridge_layer(periodic(seed + 3, (2, 3, 4, 7), 18), 84, mid, seed + 3, dots=(mid[1], 60)))
    layers.append(tree_layer(t, seed + 5))
    layers.append(near_layer(t, seed + 7))
    return [cv.image() for cv in layers]


# ------------------------------------------------------------------------------------------------- floor
def floor(theme, seed):
    name, far_c, dark, light, near_hi, specks, tuft, lip = theme
    far_c, dark, light, near_hi, tuft, lip = map(rgb, (far_c, dark, light, near_hi, tuft, lip))
    specks = [rgb(s) for s in specks]
    rng = random.Random(seed)
    FW, FH = floorgen.W, floorgen.H
    cv = Canvas(FW, FH)
    yd = FH - cv.y     # floorgen rows count down from the top
    edges = floorgen.row_edges()
    rows = floorgen.ROWS
    for r in range(rows):
        y0, y1 = edges[r], edges[r + 1]
        depth = r / (rows - 1)
        band = dark if r % 2 == 0 else mix(light, near_hi, 0.18 * depth)
        base = mix(far_c, band, 0.12 + 0.88 * depth ** 0.85)
        m = (yd >= y0) & (yd < y1)
        t = np.clip((yd - y0) / max(1, y1 - y0 - 1), 0, 1)
        shade = base[None, None, :] * (1 - 0.175 * t[..., None]) + far_c[None, None, :] * 0.175 * t[..., None]
        cv.paint(m, np.broadcast_to(shade, cv.rgb.shape))
        if depth > 0.2:
            cv.paint((yd >= y0) & (yd < y0 + 0.9), mix(base, near_hi, 0.45))
    for r in range(rows):
        y0, y1 = edges[r], edges[r + 1]
        depth = r / (rows - 1)
        count = int(3 + depth * 10)
        size = 0.6 if depth < 0.45 else 1.1
        for _ in range(count):
            c = specks[rng.randrange(len(specks))]
            x = rng.randrange(FW) + 0.5
            y = rng.randrange(y0, max(y0 + 1, int(y1 - size))) + 0.5
            if c is specks[0] and depth > 0.4:
                hgt = 1.5 + depth * 2.5
                u = (y - yd) / hgt
                cv.paint((u >= -0.2) & (u <= 1) & (np.abs(cv.dx(x)) <= 0.55 * (1 - u) + 0.1), c)
            else:
                pebble = (cv.dx(x) / (size * 1.25)) ** 2 + ((yd - y) / size) ** 2 <= 1
                cv.paint(pebble, darker(c, 0.7))
                cv.paint(pebble & ((cv.dx(x) / (size * 1.25)) ** 2 + ((yd - y + 0.25) / size) ** 2 <= 0.7), c)
    # Far edge: transparent above, a dark lip, then rounded tufts overlapping the background.
    cut = np.broadcast_to(yd < 6, cv.a.shape)
    cv.rgb[cut] = 0
    cv.a[cut] = 0
    cv.paint((yd >= 6) & (yd < 7.2), lip)
    cv.paint((yd >= 7.2) & (yd < 8), mix(lip, far_c, 0.5))
    tufts = np.zeros_like(cv.a, bool)
    x = 0
    while x < FW:
        w = rng.randrange(4, 8)
        h = rng.randrange(2, 5)
        cx = x + w / 2.0
        k = np.abs(cv.dx(cx)) / (w / 2.0)
        tufts |= (k <= 1) & (yd <= 6.6) & (yd >= 6.6 - h * (1 - k * k))
        x += w + rng.randrange(0, 3)
    cv.paint(grow(tufts, 0.6) & ~tufts & (yd < 6.6), lip)
    cv.paint(tufts, tuft)
    cv.paint(tufts & ~shift(tufts, 0, -0.8), mix(tuft, near_hi, 0.35))
    return cv.image()


def main():
    shots = []
    for n, (theme, t) in enumerate(bggen.THEMES.items()):
        layers = build(t, 11 + n * 17)
        directory = os.path.join(bggen.ROOT, theme)
        os.makedirs(directory, exist_ok=True)
        for i, img in enumerate(layers):
            img.save(os.path.join(directory, "layer_%d.png" % i), optimize=True)
        shots.append(layers)
        print("background", theme, flush=True)
    floors = []
    for i, theme in enumerate(floorgen.THEMES):
        img = floor(theme, 91 + i)
        img.save(os.path.join(floorgen.OUT_DIR, "floor_%d.png" % (i + 1)), optimize=True)
        floors.append(img)
    print("floors", len(floors))
    if "--preview" in sys.argv:
        tile_w, tile_h = W * K, H * K
        fh = floors[0].height
        out = Image.new("RGBA", (tile_w * 2, (tile_h + fh - 8 * K) * len(shots)), (0, 0, 0, 255))
        for r, layers in enumerate(shots):
            y = r * (tile_h + fh - 8 * K)
            for k in range(2):
                for img in layers:
                    out.alpha_composite(img, (k * tile_w, y))
            for x in range(0, tile_w * 2, floors[r].width):
                out.alpha_composite(floors[r], (x, y + tile_h - 6 * K - HORIZON * K + 6 * K))
        out.save(sys.argv[sys.argv.index("--preview") + 1])


if __name__ == "__main__":
    main()
