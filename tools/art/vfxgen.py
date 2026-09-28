"""Procedural pixel-art skill VFX for SoloHero (D-078). Writes Art/Vfx/vfx{name}_play_{frames}.png strips."""
import math, random, sys, os
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out'
os.makedirs(OUT, exist_ok=True)


def rgba(h, a=255):
    return ((h >> 16) & 255, (h >> 8) & 255, h & 255, a)


def new(w, h):
    return Image.new('RGBA', (w, h), (0, 0, 0, 0))


def put(img, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < img.width and 0 <= y < img.height and c is not None:
        if c[3] >= img.getpixel((x, y))[3] or img.getpixel((x, y))[3] == 0:
            img.putpixel((x, y), c)


def over(img, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < img.width and 0 <= y < img.height and c is not None:
        img.putpixel((x, y), c)


def blob(img, cx, cy, r, layers, seed=0, bumps=5, amp=0.25, squash=1.0, up=0.0, holes=0.0, dither=0.0):
    """Noisy disc. layers: list of (fraction_of_radius, color) inner->outer."""
    rnd = random.Random(seed)
    ph = [rnd.uniform(0, 6.28) for _ in range(3)]
    for y in range(int(cy - r * 1.6) - 1, int(cy + r * 1.6) + 2):
        for x in range(int(cx - r * 1.6) - 1, int(cx + r * 1.6) + 2):
            dx, dy = x - cx, (y - cy) / squash
            if up:
                dy += up * max(0, -dy) * 0  # placeholder
            a = math.atan2(dy, dx)
            rr = r * (1 + amp * (math.sin(a * bumps + ph[0]) * 0.6 + math.sin(a * (bumps + 3) + ph[1]) * 0.4))
            if up and dy < 0:
                rr *= 1 + up
            d = math.hypot(dx, dy)
            if d > rr:
                continue
            t = d / rr if rr > 0 else 0
            if holes and rnd.random() < holes * t:
                continue
            if dither and ((x + y) % 2 == 0) and rnd.random() < dither:
                continue
            for frac, col in layers:
                if t <= frac:
                    over(img, x, y, col)
                    break


def line(img, x0, y0, x1, y1, c, w=1):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / max(1, n)
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        for ox in range(-(w // 2), w - w // 2):
            for oy in range(-(w // 2), w - w // 2):
                over(img, x + ox, y + oy, c)


def spark(img, x, y, c, s=1):
    over(img, x, y, c)
    for k in range(1, s + 1):
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
            over(img, x + dx, y + dy, c)


def strip(frames, name):
    w, h = frames[0].size
    out = new(w * len(frames), h)
    for i, f in enumerate(frames):
        out.paste(f, (i * w, 0))
    path = os.path.join(OUT, 'vfx%s_play_%d.png' % (name, len(frames)))
    out.save(path)
    return path


# ---------------------------------------------------------------- palettes
WHITE = rgba(0xFFFFFF)
FIRE = [rgba(0xFFFFFF), rgba(0xFFF3A0), rgba(0xFFC531), rgba(0xFF7A1A), rgba(0xD93A1A), rgba(0x7A1F14)]
SMOKE = [rgba(0x8A8494, 200), rgba(0x5C5666, 180), rgba(0x3A3544, 150)]


def fire_layers(k):
    # k: 0 young (hot) .. 1 old (cool)
    if k < 0.3:
        return [(0.35, FIRE[0]), (0.55, FIRE[1]), (0.75, FIRE[2]), (1.0, FIRE[3])]
    if k < 0.6:
        return [(0.25, FIRE[1]), (0.5, FIRE[2]), (0.78, FIRE[3]), (1.0, FIRE[4])]
    return [(0.3, FIRE[3]), (0.65, FIRE[4]), (1.0, FIRE[5])]


def make_fire():
    frames = []
    W = H = 48
    rnd = random.Random(3)
    embers = [(rnd.uniform(0, 6.28), rnd.uniform(0.6, 1.2)) for _ in range(12)]
    radii = [5, 10, 14, 17, 18, 17, 15, 12]
    for i, r in enumerate(radii):
        img = new(W, H)
        k = i / (len(radii) - 1)
        if i >= 4:
            for j in range(4):
                blob(img, 24 + (j - 1.5) * 6, 24 - (i - 3) * 3 - j % 2 * 3, 4 + i - 3, [(0.6, SMOKE[1]), (1.0, SMOKE[2])], seed=i * 10 + j, dither=0.3 * (i - 3) / 4)
        if i < 7:
            blob(img, 24, 25 - i * 0.5, r * (1 - 0.35 * max(0, k - 0.5)), fire_layers(k), seed=i, bumps=6, amp=0.22, up=0.3, holes=0.25 * max(0, k - 0.4))
        for a, sp in embers:
            d = (i + 1) * 3.2 * sp
            x, y = 24 + math.cos(a) * d, 24 + math.sin(a) * d - i * 0.8
            if d < 23 and i > 0:
                over(img, x, y, FIRE[1] if i < 4 else FIRE[3])
        frames.append(img)
    return strip(frames, 'fire')


def zigzag(rnd, x0, y0, y1, amp):
    pts = [(x0, y0)]
    y = y0
    x = x0
    while y < y1:
        y += rnd.randint(6, 11)
        x = x0 + rnd.uniform(-amp, amp)
        pts.append((x, min(y, y1)))
    return pts


def make_bolt():
    W, H = 40, 120
    frames = []
    glow = rgba(0x7FD8FF, 170)
    core = rgba(0xFFFFFF)
    mid = rgba(0xC8F0FF)
    for i in range(7):
        img = new(W, H)
        rnd = random.Random(40 + (i if i < 3 else 2))
        pts = zigzag(rnd, 20, 0, H - 8, 7)
        if i == 0:
            for (a, b) in zip(pts[:3], pts[1:4]):
                line(img, a[0], a[1], b[0], b[1], glow, 1)
        elif i <= 4:
            w = 5 if i <= 2 else 3
            for (a, b) in zip(pts, pts[1:]):
                line(img, a[0], a[1], b[0], b[1], glow if i <= 3 else rgba(0x7FD8FF, 110), w)
            if i <= 3:
                for (a, b) in zip(pts, pts[1:]):
                    line(img, a[0], a[1], b[0], b[1], mid, 3 if i <= 2 else 1)
                    line(img, a[0], a[1], b[0], b[1], core, 1)
                # branches
                for j in range(1, len(pts) - 2, 2):
                    bx, by = pts[j]
                    ex = bx + rnd.choice([-1, 1]) * rnd.randint(6, 12)
                    ey = by + rnd.randint(5, 12)
                    line(img, bx, by, ex, ey, glow, 1)
            # ground flash
            fr = [0, 9, 12, 10, 7][i]
            blob(img, 20, H - 6, fr, [(0.4, core), (0.7, mid), (1.0, glow)], seed=i, squash=0.45, amp=0.1)
        else:
            blob(img, 20, H - 6, 6 - (i - 5) * 2, [(0.6, rgba(0x7FD8FF, 140)), (1.0, rgba(0x3D8BFF, 90))], seed=i, squash=0.45, dither=0.4)
        if i >= 2:
            for s in range(8):
                a = random.Random(s).uniform(3.3, 6.1)
                d = (i - 1) * 4 + s % 3
                x, y = 20 + math.cos(a) * d, H - 6 + math.sin(a) * d * 0.8 + (i - 2) * 1.5
                over(img, x, y, core if i < 4 else mid)
        frames.append(img)
    return strip(frames, 'bolt')


def tri(img, cx, base, w, h, fill, edge, hi):
    for yy in range(int(h)):
        y = base - yy
        half = w / 2 * (1 - yy / h)
        for x in range(int(cx - half), int(cx + half) + 1):
            c = edge if abs(x - cx) >= half - 1 else fill
            if x < cx - half * 0.2 and x > cx - half * 0.7:
                c = hi if c is fill else c
            over(img, x, y, c)


def make_ice():
    W, H = 56, 56
    frames = []
    edge = rgba(0x2B6CB0)
    fill = rgba(0x9FE3FF)
    hi = rgba(0xFFFFFF)
    spikes = [(28, 1.0, 9), (18, 0.7, 7), (38, 0.75, 7), (11, 0.45, 5), (45, 0.5, 5), (24, 0.55, 5), (33, 0.6, 5)]
    grow = [0.25, 0.6, 1.0, 1.0, 1.0, 0.85, 0.5]
    for i, g in enumerate(grow):
        img = new(W, H)
        blob(img, 28, H - 3, 18 + i, [(0.5, rgba(0xC8F0FF, 150)), (1.0, rgba(0x7FD8FF, 90))], seed=i, squash=0.3, amp=0.1, dither=0.3 if i > 4 else 0)
        if i < 5:
            for (x, s, w) in spikes:
                tri(img, x, H - 3, w, 46 * s * g, fill, edge, hi)
        if i == 3:
            for s in range(6):
                spark(img, 10 + s * 7, 12 + (s * 13) % 25, hi, 1)
        if i >= 4:
            rnd = random.Random(7)
            for s in range(14):
                a = rnd.uniform(3.4, 6.0)
                d = (i - 3) * 7 + rnd.uniform(0, 5)
                x, y = 28 + math.cos(a) * d, 40 + math.sin(a) * d * 0.9 + (i - 4) * 4
                over(img, x, y, fill)
                over(img, x + 1, y, edge)
        frames.append(img)
    return strip(frames, 'ice')


def make_poison():
    W, H = 72, 52
    frames = []
    dark, mid, light = rgba(0x2F7D32, 220), rgba(0x5FBF3F, 220), rgba(0xA6F07A, 230)
    rnd = random.Random(11)
    puffs = [(rnd.uniform(-22, 22), rnd.uniform(-6, 4), rnd.uniform(7, 12)) for _ in range(7)]
    for i in range(8):
        img = new(W, H)
        g = min(1.0, 0.35 + i * 0.22)
        fade = max(0, (i - 4) / 4)
        for j, (px_, py_, pr) in enumerate(puffs):
            blob(img, 36 + px_ * g, H - 14 + py_ - i * 0.6, pr * g, [(0.45, light), (0.75, mid), (1.0, dark)], seed=j + i * 3, amp=0.2, dither=fade * 0.8)
        for b in range(9):
            bx = 36 + (b * 17 % 44) - 22
            by = H - 10 - ((i * 5 + b * 7) % 34)
            if (b + i) % 3 != 0:
                over(img, bx, by, light)
                over(img, bx + 1, by, rgba(0xE8FFD0))
                over(img, bx, by - 1, light)
                over(img, bx + 1, by - 1, light)
        frames.append(img)
    return strip(frames, 'poison')


def make_meteor():
    W, H = 72, 136
    frames = []
    rock = [rgba(0x5C4033), rgba(0x8A6A50), rgba(0xFFC531)]
    for i in range(11):
        img = new(W, H)
        if i < 5:
            t = i / 4
            mx, my = 62 - 28 * t, 10 + 100 * t
            for k in range(1, 9):
                tx, ty = mx + k * 2.8, my - k * 7
                blob(img, tx, ty, 6 - k * 0.55, fire_layers(k / 9), seed=k + i * 9, amp=0.25)
            blob(img, mx, my, 7, [(0.45, rock[1]), (0.8, rock[0]), (1.0, rock[2])], seed=i, amp=0.15)
            over(img, mx - 2, my - 2, rgba(0xC8A080))
        else:
            k = i - 5
            r = [10, 20, 27, 30, 26, 18][k]
            if k >= 2:
                for j in range(5):
                    blob(img, 36 + (j - 2) * 9, H - 20 - (k - 1) * 6 - j % 2 * 4, 6 + k, [(0.6, SMOKE[1]), (1.0, SMOKE[2])], seed=j + k * 7, dither=0.15 * k)
            if k < 5:
                blob(img, 36, H - 14, r, fire_layers(k / 5), seed=k, bumps=7, amp=0.2, squash=0.8, up=0.4, holes=0.15 * max(0, k - 2))
            rnd = random.Random(99)
            for s in range(16):
                a = rnd.uniform(3.3, 6.1)
                d = k * 6 + rnd.uniform(2, 8)
                over(img, 36 + math.cos(a) * d, H - 12 + math.sin(a) * d, rock[1] if s % 2 else FIRE[2])
        frames.append(img)
    return strip(frames, 'meteor')


def make_holy():
    W, H = 48, 136
    frames = []
    widths = [3, 9, 16, 20, 18, 12, 6, 2]
    for i, w in enumerate(widths):
        img = new(W, H)
        cx = 24
        for y in range(H):
            wob = math.sin(y * 0.3 + i) * 0.8
            half = w / 2 + wob
            for x in range(int(cx - half - 3), int(cx + half + 4)):
                d = abs(x - cx)
                if d <= half * 0.35:
                    c = WHITE
                elif d <= half * 0.7:
                    c = rgba(0xFFF3A0)
                elif d <= half:
                    c = rgba(0xFFC531, 220)
                elif d <= half + 2 and (x + y) % 2 == 0:
                    c = rgba(0xFFC531, 110)
                else:
                    continue
                over(img, x, y, c)
        blob(img, cx, H - 4, 10 + w * 0.4, [(0.4, WHITE), (0.7, rgba(0xFFF3A0)), (1.0, rgba(0xFFC531, 160))], seed=i, squash=0.3, amp=0.1)
        rnd = random.Random(i)
        for s in range(6 + i):
            x = cx + rnd.uniform(-20, 20)
            y = rnd.uniform(10, H - 10) - i * 3
            spark(img, x, y, WHITE if s % 2 else rgba(0xFFF3A0), 1 if s % 3 else 2)
        frames.append(img)
    return strip(frames, 'holy')


def make_tornado():
    W, H = 64, 84
    frames = []
    for i in range(8):
        img = new(W, H)
        a0 = i * 0.9
        for lvl in range(14):
            y = H - 6 - lvl * 5.5
            rx = 6 + lvl * 1.7
            ry = 2 + lvl * 0.25
            for s in range(90):
                a = s / 90 * 6.283
                if math.sin(a + a0 + lvl * 0.7) < -0.2:
                    continue
                x = 32 + math.cos(a + a0 + lvl * 0.7) * rx + math.sin(lvl + i) * 1.5
                yy = y + math.sin(a + a0 + lvl * 0.7) * ry
                front = math.sin(a + a0 + lvl * 0.7) > 0.4
                over(img, x, yy, WHITE if front else rgba(0x9EC9FF, 200))
                over(img, x, yy + 1, rgba(0xC8E0FF, 230) if front else rgba(0x6A8FC0, 170))
        for b in range(6):
            a = a0 * 1.5 + b * 1.05
            y = H - 16 - b * 10
            rx = 10 + b * 3.5
            x = 32 + math.cos(a) * rx
            line(img, x - 2, y + 1, x + 2, y - 1, rgba(0xE8F4FF), 1)
            over(img, x, y, WHITE)
        if i >= 6:
            for y in range(H):
                for x in range(W):
                    if (x + y + i) % 2 == 0 and img.getpixel((x, y))[3] > 0:
                        img.putpixel((x, y), (0, 0, 0, 0))
        frames.append(img)
    return strip(frames, 'tornado')


def sword(img, x, tip, length=20):
    blade = rgba(0xC8D0E0)
    edge = rgba(0x6A7288)
    hi = WHITE
    for k in range(length):
        y = tip - k
        over(img, x, y, blade if k > 1 else hi)
        over(img, x + 1, y, hi if k > 1 else blade)
        over(img, x - 1, y, edge if k > 2 else None)
        over(img, x + 2, y, edge if k > 2 else None)
    y = tip - length
    for dx in range(-3, 5):
        over(img, x + dx, y, rgba(0xFFC531))
        over(img, x + dx, y - 1, rgba(0xB07A1A))
    for k in range(2, 7):
        over(img, x, y - k, rgba(0x7A4A2A))
        over(img, x + 1, y - k, rgba(0x5A341E))
    over(img, x, y - 7, rgba(0xFFC531))
    over(img, x + 1, y - 7, rgba(0xFFC531))


def make_swords():
    W, H = 104, 120
    frames = []
    rnd = random.Random(5)
    swords = [(10 + j * 12 + rnd.randint(-2, 2), rnd.randint(0, 4)) for j in range(8)]
    ground = H - 6
    for i in range(11):
        img = new(W, H)
        for (x, start) in swords:
            t = i - start
            if t < 0:
                continue
            if t < 3:
                tip = -10 + (ground + 10) * (t + 1) / 3
                sword(img, x, tip)
                for k in range(1, 4):
                    over(img, x, tip - 20 - k * 6, rgba(0xE8F4FF, 160))
            else:
                if i >= 9 and (x + i) % 2 == 0:
                    continue
                sword(img, x, ground + 3)
                if t == 3:
                    spark(img, x, ground, WHITE, 3)
                    blob(img, x, ground + 1, 5, [(1.0, rgba(0xB09A80, 180))], seed=x, squash=0.4)
                elif t == 4:
                    spark(img, x - 3, ground - 2, rgba(0xFFF3A0), 1)
                    spark(img, x + 4, ground - 1, rgba(0xFFF3A0), 1)
        frames.append(img)
    return strip(frames, 'swords')


def plus(img, x, y, c, edge):
    for d in range(-2, 3):
        over(img, x + d, y, c)
        over(img, x, y + d, c)
    for (dx, dy) in ((-3, 0), (3, 0), (0, -3), (0, 3)):
        over(img, x + dx, y + dy, edge)


def make_heal():
    W, H = 48, 64
    frames = []
    rnd = random.Random(2)
    items = [(rnd.uniform(6, 42), rnd.uniform(0, 8), rnd.random() < 0.5) for _ in range(10)]
    for i in range(8):
        img = new(W, H)
        r = 12 + i * 1.5
        if i < 6:
            blob(img, 24, H - 5, r, [(0.55, rgba(0xA6F07A, 120)), (0.85, rgba(0x5FBF3F, 170)), (1.0, rgba(0x2F7D32, 200))], seed=i, squash=0.3, amp=0.05, holes=0.4)
        for j, (x, delay, big) in enumerate(items):
            t = i - delay * 0.4
            if t < 0:
                continue
            y = H - 8 - t * 7
            if y < 2 or (i >= 6 and j % 2 == 0):
                continue
            if big:
                plus(img, x, y, rgba(0xA6F07A), rgba(0x2F7D32))
            else:
                spark(img, x, y, rgba(0xE8FFD0), 1)
        frames.append(img)
    return strip(frames, 'heal')


def hexagon(img, cx, cy, r, edge, fill, hi_angle=None, dither=0.0):
    pts = [(cx + r * math.cos(math.pi / 6 + k * math.pi / 3), cy + r * math.sin(math.pi / 6 + k * math.pi / 3)) for k in range(6)]
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            # inside test via polygon winding
            inside = True
            for k in range(6):
                (x0, y0), (x1, y1) = pts[k], pts[(k + 1) % 6]
                if (x1 - x0) * (y - y0) - (y1 - y0) * (x - x0) < 0:
                    inside = False
                    break
            if not inside:
                continue
            if fill[3] == 0 or (dither and (x + y) % 2 == 0 and random.random() < dither):
                continue
            over(img, x, y, fill)
    for k in range(6):
        (x0, y0), (x1, y1) = pts[k], pts[(k + 1) % 6]
        line(img, x0, y0, x1, y1, edge, 1)
    if hi_angle is not None:
        a = hi_angle
        line(img, cx + math.cos(a) * r * 0.2, cy + math.sin(a) * r * 0.2, cx + math.cos(a) * r * 0.75, cy + math.sin(a) * r * 0.75, WHITE, 1)


def make_shield():
    W, H = 52, 52
    frames = []
    radii = [8, 20, 23, 22, 22, 21]
    for i, r in enumerate(radii):
        img = new(W, H)
        dither = 0.0 if i < 4 else 0.5 * (i - 3)
        hexagon(img, 26, 26, r, rgba(0x7FD8FF), rgba(0x5FA8FF, 90 if i else 150), hi_angle=-2.2 + i * 0.5 if i else None, dither=dither)
        hexagon(img, 26, 26, r * 0.55, rgba(0x9EC9FF, 150), (0, 0, 0, 0))
        if i in (1, 2):
            for k in range(6):
                a = k * 1.047 + i
                spark(img, 26 + math.cos(a) * (r + 2), 26 + math.sin(a) * (r + 2), WHITE, 1)
        frames.append(img)
    return strip(frames, 'shield')


def make_aura():
    W, H = 52, 72
    frames = []
    for i in range(8):
        img = new(W, H)
        g = min(1.0, 0.3 + i * 0.25) * (1 - max(0, i - 5) * 0.25)
        base = H - 4
        blob(img, 26, base, 18 * g + 4, [(0.6, rgba(0xFFFFFF, 90)), (1.0, rgba(0xFFFFFF, 160))], seed=i, squash=0.3, amp=0.05, holes=0.5)
        for j in range(7):
            x = 26 + (j - 3) * 5.5
            hgt = (26 + (j * 7 % 13) * 2) * g * (0.8 + 0.2 * math.sin(i * 1.3 + j))
            for k in range(int(hgt)):
                y = base - k
                wv = 2.2 * (1 - k / max(1, hgt)) + 0.6
                sway = math.sin(k * 0.25 + i * 0.9 + j) * 1.5
                a = 255 if k < hgt * 0.6 else 170
                for dx in range(-int(wv), int(wv) + 1):
                    if i >= 6 and (dx + k + i) % 2 == 0:
                        continue
                    over(img, x + dx + sway, y, (255, 255, 255, a) if abs(dx) < wv - 0.5 else (200, 200, 210, a))
        rnd = random.Random(i)
        for s in range(5):
            spark(img, rnd.uniform(6, 46), rnd.uniform(4, base - 20), WHITE, 1)
        frames.append(img)
    return strip(frames, 'aura')


def make_vortex():
    W = H = 84
    frames = []
    for i in range(10):
        img = new(W, H)
        g = min(1.0, 0.3 + i * 0.2) * (1 - max(0, i - 7) * 0.3)
        rot = i * 0.55
        for arm in range(4):
            for s in range(90):
                t = s / 90
                r = 38 * g * (1 - t) + 3
                a = rot + arm * 1.571 + t * 5.5
                x, y = 42 + math.cos(a) * r, 42 + math.sin(a) * r * 0.85
                c = rgba(0xD9A6FF) if t > 0.6 else rgba(0xA24BFF) if t > 0.3 else rgba(0x6A2BB0)
                over(img, x, y, c)
                if t > 0.4:
                    over(img, x + 1, y, rgba(0xA24BFF, 200))
        blob(img, 42, 42, 9 * g + 2, [(0.6, rgba(0x05000A)), (0.85, rgba(0x1A0833)), (1.0, rgba(0xD9A6FF))], seed=i, amp=0.08)
        rnd = random.Random(i + 50)
        for s in range(10):
            a = rnd.uniform(0, 6.28)
            d = rnd.uniform(16, 40) * g
            spark(img, 42 + math.cos(a) * d, 42 + math.sin(a) * d * 0.85, WHITE if s % 3 == 0 else rgba(0xD9A6FF), 0)
        frames.append(img)
    return strip(frames, 'vortex')


def make_breath():
    W, H = 136, 60
    frames = []
    for i in range(8):
        img = new(W, H)
        reach = min(1.0, (i + 1) / 4) * (W - 8)
        fade = max(0, (i - 5) / 3)
        rnd = random.Random(i)
        n = 34
        for j in range(n):
            t = j / n
            x = 4 + t * reach
            spread = 5 + t * 20
            y = H - 22 + math.sin(j * 1.7 + i) * spread * 0.35
            r = 4 + t * 9
            age = t * 0.8 + fade * 0.5
            blob(img, x, y, r, fire_layers(min(0.95, age)), seed=j + i * 31, amp=0.3, up=0.3, holes=fade * 0.6, dither=fade * 0.5)
        for s in range(12):
            x = 4 + rnd.uniform(0.2, 1.0) * reach
            spark(img, x, rnd.uniform(6, H - 8), FIRE[1], 0)
        frames.append(img)
    return strip(frames, 'breath')


def make_phoenix():
    W, H = 72, 72
    frames = []
    for i in range(9):
        img = new(W, H)
        open_ = min(1.0, i / 4)
        lift = min(i, 5) * 2
        fade = max(0, (i - 6) / 3)
        cx, cy = 36, 44 - lift
        for side in (-1, 1):
            for k in range(12):
                t = k / 11
                ang = (1.9 - open_ * 1.0) + t * 0.9
                x = cx + side * math.cos(ang - 0.6) * (6 + t * 26 * (0.4 + 0.6 * open_))
                y = cy - math.sin(ang) * (4 + t * 20) + t * t * 6
                blob(img, x, y, 3.2 + (1 - t) * 3, fire_layers(0.2 + t * 0.6), seed=k + side * 20 + i, amp=0.3, up=0.4, dither=fade)
        blob(img, cx, cy, 5, fire_layers(0.1), seed=i, amp=0.1, dither=fade)
        blob(img, cx, cy - 6, 2.5, [(0.6, WHITE), (1.0, FIRE[2])], seed=i + 5)
        for k in range(6):
            blob(img, cx + math.sin(k + i) * 2, cy + 6 + k * 3, 3 - k * 0.4, fire_layers(0.4 + k * 0.1), seed=k + 70 + i, dither=fade)
        blob(img, 36, H - 4, 14 * open_ + 2, [(0.5, rgba(0xFFF3A0, 140)), (1.0, rgba(0xFF7A1A, 120))], seed=i, squash=0.3, dither=fade)
        frames.append(img)
    return strip(frames, 'phoenix')


makers = [make_fire, make_bolt, make_ice, make_poison, make_meteor, make_holy, make_tornado, make_swords,
          make_heal, make_shield, make_aura, make_vortex, make_breath, make_phoenix]
for m in makers:
    print(m())
