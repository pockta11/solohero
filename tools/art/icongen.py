"""24x24 pixel skill icons (D-078): element-tinted rounded tile + outlined symbol. Writes skill_{id}.png."""
import math, os, sys
from PIL import Image

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
N = 24
OUTLINE = (26, 20, 38, 255)


def rgb(h, a=255):
    return ((h >> 16) & 255, (h >> 8) & 255, h & 255, a)


class Icon:
    def __init__(self, bg):
        self.bg = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        self.fg = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        dark, light = bg
        for y in range(N):
            for x in range(N):
                cx, cy = abs(x - 11.5), abs(y - 11.5)
                if cx > 10.5 and cy > 10.5 and (cx - 10.5) + (cy - 10.5) > 1.2:
                    continue
                edge = x in (0, N - 1) or y in (0, N - 1)
                t = (x + y) / (2 * N)
                c = tuple(int(dark[i] * (1 - t * 0.6) + light[i] * t * 0.6) for i in range(3)) + (255,)
                if edge:
                    c = tuple(min(255, v + 40) for v in dark[:3]) + (255,)
                self.bg.putpixel((x, y), c)

    def p(self, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 1 <= x < N - 1 and 1 <= y < N - 1:
            self.fg.putpixel((x, y), c)

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.p(x, y, c)

    def line(self, x0, y0, x1, y1, c, w=1):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(n + 1):
            t = i / n
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            for o in range(w):
                self.p(x + (o if abs(x1 - x0) < abs(y1 - y0) else 0), y + (o if abs(x1 - x0) >= abs(y1 - y0) else 0), c)

    def disc(self, cx, cy, r, c):
        for y in range(N):
            for x in range(N):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    self.p(x, y, c)

    def ring(self, cx, cy, r, c, a0=0, a1=6.283):
        for k in range(120):
            a = a0 + (a1 - a0) * k / 119
            self.p(cx + math.cos(a) * r, cy + math.sin(a) * r, c)

    def save(self, name):
        out = self.bg.copy()
        # outline around the symbol
        ol = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        for y in range(N):
            for x in range(N):
                if self.fg.getpixel((x, y))[3]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < N and 0 <= yy < N and self.fg.getpixel((xx, yy))[3]:
                        if self.bg.getpixel((x, y))[3]:
                            ol.putpixel((x, y), OUTLINE)
                        break
        out.alpha_composite(ol)
        out.alpha_composite(self.fg)
        out.save(os.path.join(OUT, 'skill_' + name + '.png'))


W = rgb(0xFFFFFF)
STEEL = [rgb(0xE8ECF4), rgb(0xB8C0D0), rgb(0x7A8298)]
GOLD = [rgb(0xFFF3A0), rgb(0xFFC531), rgb(0xB07A1A)]
FIRE = [rgb(0xFFF3A0), rgb(0xFFC531), rgb(0xFF7A1A), rgb(0xD93A1A)]
ICE = [rgb(0xFFFFFF), rgb(0x9FE3FF), rgb(0x3D8BFF)]
GREEN = [rgb(0xE8FFD0), rgb(0xA6F07A), rgb(0x5FBF3F), rgb(0x2F7D32)]
PURPLE = [rgb(0xF0D8FF), rgb(0xD9A6FF), rgb(0xA24BFF), rgb(0x5A1FA0)]

BG_RED = ((90, 30, 30), (200, 80, 50))
BG_BLUE = ((24, 40, 90), (70, 130, 220))
BG_ICE = ((20, 60, 100), (90, 180, 230))
BG_GREEN = ((20, 60, 30), (80, 160, 70))
BG_GOLD = ((90, 60, 10), (220, 170, 50))
BG_PURPLE = ((45, 20, 80), (140, 70, 200))
BG_STEEL = ((40, 44, 60), (110, 120, 150))
BG_ORANGE = ((100, 40, 10), (230, 120, 40))


def sword(ic, x0, y0, x1, y1, blade=STEEL, guard=GOLD):
    ic.line(x0, y0, x1, y1, blade[0], 1)
    ic.line(x0 + 1, y0, x1 + 1, y1, blade[1], 1)
    # guard perpendicular near (x1,y1)
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    gx, gy = x1, y1
    ic.line(gx - uy * 3, gy + ux * 3, gx + uy * 3 + 1, gy - ux * 3, guard[1], 1)
    ic.line(gx + ux * 1, gy + uy * 1, gx + ux * 4, gy + uy * 4, rgb(0x7A4A2A), 2)
    ic.p(gx + ux * 5, gy + uy * 5, guard[1])


def flame(ic, cx, cy, s, pal=FIRE):
    for y in range(N):
        for x in range(N):
            dx, dy = (x - cx) / s, (y - cy) / s
            r = math.hypot(dx, dy * (0.8 if dy > 0 else 0.55))
            wob = 0.12 * math.sin(dy * 5 + dx * 3)
            if r > 1 + wob:
                continue
            k = r + (0.25 if dy < -0.3 else 0)
            c = pal[0] if k < 0.35 else pal[1] if k < 0.6 else pal[2] if k < 0.85 else pal[3]
            ic.p(x, y, c)


def bolt(ic, pts, c1, c2):
    for (a, b) in zip(pts, pts[1:]):
        ic.line(a[0], a[1], b[0], b[1], c2, 2)
    for (a, b) in zip(pts, pts[1:]):
        ic.line(a[0], a[1], b[0], b[1], c1, 1)


def snowflake(ic, cx, cy, r, c1, c2):
    for k in range(6):
        a = k * math.pi / 3 - math.pi / 2
        ex, ey = cx + math.cos(a) * r, cy + math.sin(a) * r
        ic.line(cx, cy, ex, ey, c1, 1)
        mx, my = cx + math.cos(a) * r * 0.6, cy + math.sin(a) * r * 0.6
        for s in (-0.6, 0.6):
            ic.line(mx, my, mx + math.cos(a + s) * 2.5, my + math.sin(a + s) * 2.5, c2, 1)
    ic.p(cx, cy, W)


def shield(ic, cx, top, w, h, c1, c2, c3):
    for y in range(h):
        half = w / 2 if y < h * 0.45 else w / 2 * (1 - (y - h * 0.45) / (h * 0.55)) ** 0.8
        for x in range(int(cx - half), int(cx + half) + 1):
            c = c3 if abs(x - cx) >= half - 1 or y == 0 else (c1 if x < cx else c2)
            ic.p(x, top + y, c)


def arrow(ic, x0, y0, x1, y1, shaft=rgb(0xC8A060), head=STEEL, fletch=rgb(0xF0F0F0)):
    """Arrow from tail (x0,y0) to tip (x1,y1)."""
    ic.line(x0, y0, x1, y1, shaft, 1)
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    for k in range(3):
        ic.line(x1 - ux * k - uy * (2 - k), y1 - uy * k + ux * (2 - k), x1 - ux * k + uy * (2 - k), y1 - uy * k - ux * (2 - k), head[1 if k else 0], 1)
    ic.p(x1, y1, head[0])
    for k in range(3):
        ic.p(x0 + ux * k - uy * 1.5, y0 + uy * k + ux * 1.5, fletch)
        ic.p(x0 + ux * k + uy * 1.5, y0 + uy * k - ux * 1.5, fletch)


def crescent(ic, cx, cy, r, c1, c2, a0=-2.4, a1=0.9):
    """Slash arc: a thick crescent swept from a0 to a1."""
    for k in range(90):
        a = a0 + (a1 - a0) * k / 89
        w = math.sin(k / 89 * math.pi) * 2.2
        for t in range(int(w * 2) + 1):
            rr = r - t * 0.5
            ic.p(cx + math.cos(a) * rr, cy + math.sin(a) * rr, c1 if t < w else c2)


def make_jobs(icons):
    """D-104 job main attacks (main_{job}) and second-job ultimates (ult_*)."""
    BG_DARK = ((24, 20, 34), (80, 70, 110))
    BG_BLOOD = ((70, 10, 16), (190, 40, 40))
    BG_FOREST = ((16, 50, 30), (70, 150, 90))

    ic = Icon(BG_BLUE)  # main_warrior: power slash, blue arc + sword
    crescent(ic, 10, 13, 9, W, ICE[1])
    sword(ic, 18, 4, 13, 10)
    icons['main_warrior'] = ic

    ic = Icon(BG_PURPLE)  # main_mage: magic bolt, orb with trail
    for k in range(5):
        ic.disc(8 - k * 0.2 + k * 0, 16, 0, W)
    for k in range(6):
        ic.disc(15 - k * 1.7, 9 + k * 1.7, 3 - k * 0.45, PURPLE[2 if k > 2 else 1])
    ic.disc(15, 9, 3.6, PURPLE[1])
    ic.disc(14, 8, 2, PURPLE[0])
    for (x, y) in ((19, 4), (20, 12), (10, 4)):
        ic.p(x, y, PURPLE[0])
    icons['main_mage'] = ic

    ic = Icon(BG_FOREST)  # main_archer: double shot, two parallel arrows
    arrow(ic, 4, 13, 18, 6)
    arrow(ic, 5, 19, 19, 12)
    icons['main_archer'] = ic

    ic = Icon(BG_GOLD)  # main_knight: holy charge, gold arc + cross shine
    crescent(ic, 10, 13, 9, W, GOLD[0])
    ic.line(17, 3, 17, 11, W, 1)
    ic.line(13, 7, 21, 7, W, 1)
    ic.p(17, 7, GOLD[1])
    icons['main_knight'] = ic

    ic = Icon(BG_BLOOD)  # main_berserker: raging blow, red arc + axe
    crescent(ic, 10, 13, 9, rgb(0xFFB0A0), rgb(0xFF4040))
    ic.line(12, 21, 17, 5, rgb(0x7A4A2A), 1)
    for y in range(2, 13):
        for x in range(15, 23):
            if (x - 17) ** 2 + (y - 7) ** 2 <= 22 and x >= 18:
                ic.p(x, y, STEEL[0] if x > 19 else STEEL[1])
    icons['main_berserker'] = ic

    ic = Icon(BG_RED)  # main_pyro: flame bolt
    for k in range(5):
        ic.disc(15 - k * 2, 9 + k * 2, 2.6 - k * 0.4, FIRE[min(3, k)])
    flame(ic, 15, 9, 5)
    icons['main_pyro'] = ic

    ic = Icon(BG_ICE)  # main_cryo: ice bolt, crystal shard
    for y in range(3, 21):
        half = 4 - abs(y - 11) * 0.45
        for x in range(int(12 - half), int(12 + half) + 1):
            ic.p(x + (y - 11) * -0.35, y, ICE[0] if x < 12 else ICE[1])
    ic.line(12 + 2.8, 3, 12 - 3.2, 20, ICE[2], 1)
    for (x, y) in ((5, 6), (19, 16), (18, 5)):
        ic.p(x, y, W)
    icons['main_cryo'] = ic

    ic = Icon(BG_FOREST)  # main_ranger: arrow blow, three spread arrows
    arrow(ic, 3, 11, 13, 4)
    arrow(ic, 5, 16, 16, 9)
    arrow(ic, 7, 21, 19, 14)
    icons['main_ranger'] = ic

    ic = Icon(BG_DARK)  # main_sniper: one long arrow through a sight
    ic.ring(15, 9, 5, rgb(0xFF6060))
    ic.line(15, 2, 15, 5, rgb(0xFF6060), 1)
    ic.line(21, 9, 21, 9, rgb(0xFF6060), 1)
    arrow(ic, 3, 21, 16, 8, head=[W, STEEL[0], STEEL[1]])
    icons['main_sniper'] = ic

    ic = Icon(BG_GOLD)  # ult_guardian_cross: shield with a holy cross
    for k in range(8):
        a = k * math.pi / 4
        ic.line(12 + math.cos(a) * 9, 12 + math.sin(a) * 9, 12 + math.cos(a) * 11, 12 + math.sin(a) * 11, W, 1)
    shield(ic, 12, 4, 14, 16, rgb(0x5C8CFF), rgb(0x3D63D0), GOLD[1])
    ic.rect(11, 6, 13, 17, W)
    ic.rect(7, 9, 17, 11, W)
    icons['ult_guardian_cross'] = ic

    ic = Icon(BG_BLOOD)  # ult_blood_rage: blood drop with fury marks
    for y in range(4, 21):
        half = 6 * ((y - 4) / 11) ** 0.8 if y <= 15 else math.sqrt(max(0, 36 - (y - 15) ** 2))
        for x in range(int(12 - half), int(12 + half) + 1):
            ic.p(x, y, rgb(0xFF5050) if x < 11 else rgb(0xC81E28))
    ic.disc(10, 13, 1.5, rgb(0xFFB0A0))
    for s in (-1, 1):
        ic.line(12 + s * 4, 14, 12 + s * 2, 16, OUTLINE, 1)
    ic.line(10, 18, 14, 18, OUTLINE, 1)
    icons['ult_blood_rage'] = ic

    ic = Icon(BG_RED)  # ult_inferno: pillar of fire on a ring
    ic.ring(12, 18, 8, FIRE[1], 0, 3.14)
    flame(ic, 12, 11, 8)
    flame(ic, 6, 15, 3.5)
    flame(ic, 18, 15, 3.5)
    icons['ult_inferno'] = ic

    ic = Icon(BG_ICE)  # ult_blizzard: snowflakes in a gale
    for (y, x0, x1) in ((6, 3, 11), (12, 2, 8), (18, 4, 12)):
        ic.line(x0, y, x1, y, ICE[1], 1)
        ic.p(x1 + 1, y - 1, ICE[1])
    snowflake(ic, 15, 12, 6.5, W, ICE[1])
    ic.p(6, 9, W); ic.p(9, 15, W); ic.p(20, 4, W); ic.p(20, 20, W)
    icons['ult_blizzard'] = ic

    ic = Icon(BG_FOREST)  # ult_arrow_rain: arrows falling from the sky
    for (x, top) in ((6, 2), (12, 6), (18, 3)):
        arrow(ic, x, top, x, top + 13, head=[W, GREEN[0], GREEN[1]])
    for x in (4, 9, 15, 20):
        ic.p(x, 21, GREEN[1])
    icons['ult_arrow_rain'] = ic

    ic = Icon(BG_DARK)  # ult_death_shot: skull in a crosshair
    ic.ring(12, 12, 9, rgb(0xFF4040))
    for (x0, y0, x1, y1) in ((12, 1, 12, 5), (12, 19, 12, 22), (1, 12, 5, 12), (19, 12, 22, 12)):
        ic.line(x0, y0, x1, y1, rgb(0xFF4040), 1)
    ic.disc(12, 11, 4.5, rgb(0xF0EAD8))
    ic.rect(10, 14, 14, 16, rgb(0xF0EAD8))
    ic.disc(10, 11, 1.2, OUTLINE)
    ic.disc(14, 11, 1.2, OUTLINE)
    ic.p(12, 13, OUTLINE)
    ic.p(11, 16, OUTLINE); ic.p(13, 16, OUTLINE)
    icons['ult_death_shot'] = ic


def make_line_skills(icons):
    """D-107: 15 more line skills (5 per line)."""
    BG_FOREST = ((16, 50, 30), (70, 150, 90))
    BROWN = [rgb(0xE8C890), rgb(0xB08050), rgb(0x6A4A2A)]

    ic = Icon(BG_STEEL)  # shield_bash: shield with impact lines
    for k in range(5):
        a = -0.9 + k * 0.45
        ic.line(15 + math.cos(a) * 6, 12 + math.sin(a) * 6, 15 + math.cos(a) * 9, 12 + math.sin(a) * 9, FIRE[0], 1)
    shield(ic, 10, 5, 12, 14, rgb(0x5C8CFF), rgb(0x3D63D0), STEEL[0])
    ic.p(8, 8, W)
    icons['shield_bash'] = ic

    ic = Icon(BG_ORANGE)  # ground_slam: hammer on cracked ground
    ic.rect(2, 16, 21, 21, BROWN[1])
    ic.rect(2, 16, 21, 16, BROWN[0])
    for (x0, x1) in ((9, 6), (10, 13), (11, 17)):
        ic.line(x0, 17, x1, 20, BROWN[2], 1)
    for (x, y) in ((4, 13), (18, 12), (16, 14)):
        ic.p(x, y, BROWN[0])
    ic.line(17, 3, 12, 11, rgb(0x7A4A2A), 2)
    ic.rect(6, 9, 13, 14, STEEL[1])
    ic.rect(6, 9, 13, 10, STEEL[0])
    icons['ground_slam'] = ic

    ic = Icon(BG_RED)  # cleave: wide sweeping arc
    crescent(ic, 11, 12, 9, W, rgb(0xFFB0A0), a0=-2.8, a1=1.2)
    sword(ic, 19, 4, 15, 9)
    icons['cleave'] = ic

    ic = Icon(BG_ORANGE)  # earthquake: split ground with flying rocks
    for y in range(13, 21):
        for x in range(2, 22):
            if abs(x - 12 - (y - 13) * 0.3) > 1:
                ic.p(x, y, BROWN[1] if y > 15 else BROWN[0])
    ic.line(12, 13, 14, 21, OUTLINE, 1)
    for (x, y, r) in ((6, 8, 2), (17, 6, 1.6), (11, 4, 1.3)):
        ic.disc(x, y, r, BROWN[2])
    icons['earthquake'] = ic

    ic = Icon(BG_RED)  # dragon_slash: red double arc with flame tips
    crescent(ic, 11, 12, 9, rgb(0xFFD0A0), rgb(0xFF5030))
    crescent(ic, 13, 12, 5, W, rgb(0xFF9050))
    flame(ic, 18, 5, 2.5)
    icons['dragon_slash'] = ic

    ic = Icon(BG_PURPLE)  # magic_missile: three orbs with trails
    for k, (x, y) in enumerate(((15, 6), (17, 12), (15, 18))):
        ic.line(x - 8, y + 2, x - 2, y, PURPLE[2], 1)
        ic.disc(x, y, 2.3, PURPLE[1])
        ic.p(x - 0.5, y - 0.5, PURPLE[0])
    icons['magic_missile'] = ic

    ic = Icon(BG_RED)  # ember: little flames
    flame(ic, 8, 14, 3.5)
    flame(ic, 16, 11, 4.5)
    for (x, y) in ((5, 6), (12, 5), (19, 18)):
        ic.p(x, y, FIRE[1])
    icons['ember'] = ic

    ic = Icon(BG_ICE)  # ice_lance: icicle lance
    for k in range(14):
        w = 0.4 + k * 0.22
        cx, cy = 18 - k * 1.0, 4 + k * 1.1
        ic.disc(cx, cy, w, ICE[1] if k % 3 else ICE[0])
    ic.line(19, 3, 9, 14, W, 1)
    icons['ice_lance'] = ic

    ic = Icon(BG_ORANGE)  # flame_pillar: tall column of fire
    for y in range(3, 21):
        half = 3.5 + 1.5 * math.sin(y * 0.9)
        for x in range(int(12 - half), int(12 + half) + 1):
            d = abs(x - 12) / max(half, 1)
            ic.p(x, y, FIRE[0] if d < 0.3 else FIRE[1] if d < 0.6 else FIRE[2])
    ic.rect(5, 19, 19, 20, FIRE[3])
    icons['flame_pillar'] = ic

    ic = Icon(BG_PURPLE)  # arcane_storm: cloud with bolts
    for (x, y, r) in ((8, 7, 3.5), (13, 6, 4), (17, 8, 3)):
        ic.disc(x, y, r, rgb(0x6A70B0))
    ic.disc(12, 7, 2.5, rgb(0x8890D0))
    bolt(ic, [(9, 10), (7, 15), (10, 15), (7, 21)], rgb(0xE0F0FF), rgb(0x80C0FF))
    bolt(ic, [(15, 10), (14, 14), (17, 14), (15, 19)], rgb(0xE0F0FF), rgb(0x80C0FF))
    icons['arcane_storm'] = ic

    ic = Icon(BG_FOREST)  # arrow_shot: one big arrow
    arrow(ic, 4, 20, 19, 5)
    icons['arrow_shot'] = ic

    ic = Icon(BG_FOREST)  # scatter_shot: fan of short arrows
    arrow(ic, 4, 19, 12, 5)
    arrow(ic, 4, 19, 19, 9)
    arrow(ic, 4, 19, 20, 17)
    icons['scatter_shot'] = ic

    ic = Icon(BG_GOLD)  # piercing_arrow: arrow through two rings
    ic.ring(9, 12, 4, rgb(0xFF6060))
    ic.ring(16, 12, 3, rgb(0xFF6060))
    arrow(ic, 2, 12, 21, 12, head=[W, STEEL[0], STEEL[1]])
    icons['piercing_arrow'] = ic

    ic = Icon(BG_FOREST)  # storm_arrows: arrows riding a gust
    ic.ring(12, 14, 8, GREEN[0], 3.4, 5.9)
    for (x0, y0) in ((3, 9), (6, 15), (9, 5)):
        arrow(ic, x0, y0, x0 + 11, y0 - 3, head=[W, GREEN[0], GREEN[1]])
    icons['storm_arrows'] = ic

    ic = Icon(((24, 20, 50), (90, 80, 160)))  # starfall_arrow: star with falling arrows
    for k in range(5):
        a = -math.pi / 2 + k * 2 * math.pi / 5
        b = a + math.pi / 5
        ic.line(7, 7, 7 + math.cos(a) * 5, 7 + math.sin(a) * 5, GOLD[0], 1)
        ic.line(7, 7, 7 + math.cos(b) * 2.2, 7 + math.sin(b) * 2.2, GOLD[1], 1)
    ic.disc(7, 7, 1.5, W)
    arrow(ic, 12, 4, 19, 19, head=[W, rgb(0xB0FFB0), GREEN[1]])
    arrow(ic, 8, 12, 12, 21, head=[W, rgb(0xB0FFB0), GREEN[1]])
    icons['starfall_arrow'] = ic


def make():
    icons = {}

    ic = Icon(BG_ORANGE)  # power_strike: sword with impact burst
    for k in range(8):
        a = k * math.pi / 4
        ic.line(15 + math.cos(a) * 3, 8 + math.sin(a) * 3, 15 + math.cos(a) * 6, 8 + math.sin(a) * 6, FIRE[0] if k % 2 else FIRE[1], 1)
    sword(ic, 14, 9, 6, 17)
    icons['power_strike'] = ic

    ic = Icon(BG_BLUE)  # whirlwind: spiral arcs
    for r, c in ((8, ICE[1]), (5.5, W), (3, ICE[1])):
        ic.ring(12, 12, r, c, 0.3, 4.9)
    sword(ic, 18, 5, 15, 9)
    icons['whirlwind'] = ic

    ic = Icon(BG_RED)  # battle_cry: horn with sound waves
    for y in range(9, 16):
        w = (y - 8) if y < 13 else 16 - y
    ic.line(5, 15, 12, 10, GOLD[1], 3)
    ic.rect(12, 7, 14, 15, GOLD[0])
    ic.rect(4, 14, 6, 16, GOLD[2])
    for k, r in enumerate((3, 5.5, 8)):
        ic.ring(14, 11, r, W if k % 2 == 0 else FIRE[1], -0.9, 0.9)
    icons['battle_cry'] = ic

    ic = Icon(BG_STEEL)  # quick_slash: three slashes
    for k in range(3):
        o = k * 5 - 5
        ic.line(5 + o, 18, 16 + o, 5, W, 1)
        ic.line(6 + o, 18, 17 + o, 5, STEEL[1], 1)
    icons['quick_slash'] = ic

    ic = Icon(BG_STEEL)  # iron_skin: steel shield
    shield(ic, 12, 4, 14, 16, STEEL[0], STEEL[1], STEEL[2])
    ic.line(12, 6, 12, 17, STEEL[2], 1)
    ic.p(9, 8, W)
    icons['iron_skin'] = ic

    ic = Icon(BG_GREEN)  # first_aid: cross
    ic.rect(10, 5, 14, 19, GREEN[1])
    ic.rect(5, 10, 19, 14, GREEN[1])
    ic.rect(11, 6, 12, 18, GREEN[0])
    ic.rect(6, 11, 18, 12, GREEN[0])
    icons['first_aid'] = ic

    ic = Icon(BG_ORANGE)  # fireball: ball with trail
    for k in range(5):
        ic.disc(7 - k * 0 + k * 0, 17 - 0, 0, W)
    for k in range(6):
        ic.disc(15 - k * 1.6, 9 + k * 1.6, 3.5 - k * 0.5, FIRE[2 if k > 2 else 1])
    ic.disc(15, 9, 4, FIRE[1])
    ic.disc(14, 8, 2.5, FIRE[0])
    icons['fireball'] = ic

    ic = Icon(BG_ICE)  # frost_nova: snowflake
    snowflake(ic, 12, 12, 8, W, ICE[1])
    icons['frost_nova'] = ic

    ic = Icon(BG_GOLD)  # thunder: bolt
    bolt(ic, [(14, 3), (9, 11), (13, 11), (8, 20)], rgb(0xFFFFE0), GOLD[1])
    icons['thunder'] = ic

    ic = Icon(BG_RED)  # berserk: crossed axes
    for sx in (1, -1):
        ic.line(12 - sx * 7, 19, 12 + sx * 5, 6, rgb(0x7A4A2A), 1)
        cx = 12 + sx * 5
        for y in range(4, 11):
            for x in range(cx - 3, cx + 4):
                if (x - cx) * sx >= -1 and (x - cx) ** 2 + (y - 7) ** 2 <= 10:
                    ic.p(x, y, STEEL[0] if (x - cx) * sx > 1 else STEEL[1])
    ic.disc(12, 14, 1.5, rgb(0xFF4040))
    icons['berserk'] = ic

    ic = Icon(BG_GREEN)  # poison_cloud: skull in cloud
    for (x, y, r) in ((8, 13, 4), (13, 11, 5), (17, 14, 4), (12, 16, 4)):
        ic.disc(x, y, r, GREEN[2])
    ic.disc(12, 12, 3.5, GREEN[0])
    ic.rect(10, 15, 14, 16, GREEN[0])
    ic.p(11, 12, GREEN[3]); ic.p(13, 12, GREEN[3]); ic.p(12, 15, GREEN[3])
    for (x, y) in ((6, 6), (18, 5), (20, 9)):
        ic.p(x, y, GREEN[1])
    icons['poison_cloud'] = ic

    ic = Icon(BG_GOLD)  # eagle_eye: eye
    for x in range(4, 21):
        h = 5 * math.sin((x - 4) / 16 * math.pi)
        ic.p(x, 12 - h, GOLD[0]); ic.p(x, 12 + h, GOLD[0])
        for y in range(int(12 - h) + 1, int(12 + h)):
            ic.p(x, y, W)
    ic.disc(12, 12, 3.2, GOLD[1])
    ic.disc(12, 12, 1.5, OUTLINE)
    ic.p(11, 11, W)
    icons['eagle_eye'] = ic

    ic = Icon(BG_ORANGE)  # meteor
    for k in range(7):
        ic.disc(9 + k * 1.8, 15 - k * 1.8, 3 - k * 0.35, FIRE[1 if k < 3 else 2])
    ic.disc(8, 16, 4, rgb(0x6A4A3A))
    ic.disc(7, 15, 2, rgb(0x9A7A60))
    ic.ring(8, 16, 4, FIRE[1], -1.6, 0.3)
    icons['meteor'] = ic

    ic = Icon(BG_PURPLE)  # chain_lightning: branching purple bolts
    bolt(ic, [(5, 4), (10, 10), (8, 12), (14, 19)], PURPLE[0], PURPLE[2])
    bolt(ic, [(10, 10), (17, 8), (15, 12), (20, 14)], PURPLE[0], PURPLE[2])
    icons['chain_lightning'] = ic

    ic = Icon(BG_STEEL)  # blade_storm: funnel with blades
    for lv in range(7):
        y = 18 - lv * 2.2
        ic.ring(12, y, 2 + lv * 1.2, W if lv % 2 else ICE[1], 0, 3.14)
    sword(ic, 18, 4, 15, 8)
    icons['blade_storm'] = ic

    ic = Icon(BG_GOLD)  # holy_light: rays from above
    for k in range(-3, 4):
        ic.line(12, 2, 12 + k * 3, 20, GOLD[0] if k % 2 else W, 1)
    ic.disc(12, 6, 3, W)
    icons['holy_light'] = ic

    ic = Icon(BG_ICE)  # glacier_spear: ice spear diagonal
    ic.line(4, 20, 16, 8, ICE[2], 2)
    for k in range(6):
        ic.p(16 + k * 0.6, 8 - k * 0.6, ICE[0])
    for y in range(3, 12):
        for x in range(14, 22):
            if (x - 14) + (11 - y) <= 8 and (x - 14) >= 0 and (11 - y) >= 0 and abs((x - 14) - (11 - y)) <= 3:
                ic.p(x, y, ICE[1] if x + y > 25 else ICE[0])
    icons['glacier_spear'] = ic

    ic = Icon(BG_GOLD)  # war_god: winged helmet
    ic.disc(12, 12, 5, GOLD[1])
    ic.rect(7, 12, 17, 17, GOLD[1])
    ic.rect(10, 13, 14, 15, OUTLINE)
    ic.rect(11, 13, 13, 18, OUTLINE)
    ic.p(10, 9, GOLD[0]); ic.p(11, 8, GOLD[0])
    for s in (-1, 1):
        for k in range(4):
            ic.line(12 + s * 6, 12 - k, 12 + s * (8 + k), 6 - k, W, 1)
    icons['war_god'] = ic

    ic = Icon(BG_RED)  # dragon_breath: dragon head with fire
    ic.disc(7, 10, 4, rgb(0xA02828))
    ic.rect(7, 9, 12, 13, rgb(0xA02828))
    ic.p(6, 8, GOLD[1]); ic.line(4, 6, 7, 3, rgb(0xE0D0B0), 1)
    for k in range(5):
        ic.disc(14 + k * 1.6, 12 + (k % 2) * 0.6, 1.5 + k * 0.6, FIRE[min(3, k)])
    icons['dragon_breath'] = ic

    ic = Icon(BG_GOLD)  # judgement: glowing sword pointing down with rays
    for k in range(8):
        a = k * math.pi / 4
        ic.line(12 + math.cos(a) * 5, 17 + math.sin(a) * 3, 12 + math.cos(a) * 9, 17 + math.sin(a) * 5, GOLD[0], 1)
    sword(ic, 12, 20, 12, 7, blade=[W, GOLD[0], GOLD[1]])
    icons['judgement'] = ic

    ic = Icon(BG_PURPLE)  # time_stop: clock
    ic.disc(12, 12, 8, PURPLE[1])
    ic.disc(12, 12, 6.5, PURPLE[0])
    for k in range(12):
        a = k * math.pi / 6
        ic.p(12 + math.cos(a) * 5.5, 12 + math.sin(a) * 5.5, PURPLE[3] if k % 3 else PURPLE[2])
    ic.line(12, 12, 12, 7, PURPLE[3], 1)
    ic.line(12, 12, 16, 13, PURPLE[3], 1)
    icons['time_stop'] = ic

    ic = Icon(BG_STEEL)  # sword_rain: swords pointing down
    for (x, t) in ((5, 12), (11, 17), (17, 14)):
        sword(ic, x, t + 3, x, t - 7)
    icons['sword_rain'] = ic

    ic = Icon(BG_ORANGE)  # phoenix: fire bird
    for s in (-1, 1):
        for k in range(7):
            ic.disc(12 + s * (2 + k * 1.3), 12 - k * 1.1 + (k * k) * 0.08, 2.2 - k * 0.2, FIRE[min(3, k // 2)])
    ic.disc(12, 12, 2.5, FIRE[0])
    ic.disc(12, 8, 1.5, FIRE[1])
    for k in range(4):
        ic.p(12 + (k % 2), 15 + k * 1.3, FIRE[2 + (k > 1)])
    icons['phoenix'] = ic

    ic = Icon(BG_PURPLE)  # black_hole: swirl
    for arm in range(3):
        for s in range(40):
            t = s / 40
            r = 9 * (1 - t) + 2
            a = arm * 2.094 + t * 5
            ic.p(12 + math.cos(a) * r, 12 + math.sin(a) * r, PURPLE[1] if t > 0.5 else PURPLE[2])
    ic.disc(12, 12, 2.5, (8, 0, 16, 255))
    icons['black_hole'] = ic

    make_jobs(icons)
    make_line_skills(icons)
    for name, ic in icons.items():
        ic.save(name)
    return list(icons)


names = make()
print(len(names))
# lock icon (16x16, UI style)
lock = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
for y in range(7, 14):
    for x in range(3, 13):
        edge = x in (3, 12) or y in (7, 13)
        lock.putpixel((x, y), (26, 20, 38, 255) if edge else (255, 197, 49, 255) if y < 10 else (176, 122, 26, 255))
for k in range(20):
    a = math.pi + k / 19 * math.pi
    x, y = 8 + math.cos(a) * 3.5, 7 + math.sin(a) * 4
    lock.putpixel((int(round(x - 0.5)), int(round(y))), (200, 205, 220, 255))
lock.putpixel((7, 10), (26, 20, 38, 255)); lock.putpixel((8, 10), (26, 20, 38, 255)); lock.putpixel((7, 11), (26, 20, 38, 255))
lock.save(os.path.join(OUT, 'icon_lock.png'))
