"""Smooth skill icons (D-108) -> Assets/SoloHero/Art/UI/Hd/Skills/skill_{id}.png (128 x 128).

Usage: python tools/art/skillgen3.py [OUT_DIR] [--preview PREVIEW.png]
Every catalog skill, job main attack (main_{job}) and job ultimate (ult_*): a rounded tile in the skill's element
colour with a soft centre glow, and an ink-outlined symbol on top (the grade frame around it comes from the UI).
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from uikit import Layer, band, cov, hexc, radial, shade, vgrad  # noqa: E402
from icongen3 import (BLUE, BROWN, GOLD, GREEN, INK, ORANGE, PURPLE, RED, SILVER, fill, gloss, inter, outline,  # noqa: E402
                      rot, rrect_at, seg, shield_shape, star_pts, sword, union)

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art', 'UI', 'Hd', 'Skills')
args = []
_skip = False
for _a in sys.argv[1:]:
    if _skip:
        _skip = False
    elif _a == '--preview':
        _skip = True
    elif not _a.startswith('--'):
        args.append(_a)
if args:
    OUT = args[0]
PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

S = 128
WHITE = (255, 255, 255, 255)
BG = {
    'steel': ('#9AAAC8', '#3A4468'), 'fire': ('#FFB45A', '#B8301E'), 'ice': ('#B0F0FF', '#2C6CCC'),
    'storm': ('#ACA0FF', '#36268E'), 'nature': ('#A6EC8A', '#24783C'), 'holy': ('#FFEDA8', '#D08A16'),
    'void': ('#CDA0FF', '#3E1680'), 'wind': ('#94F2CC', '#1A8466'), 'blood': ('#FF9A8E', '#8E1426'),
    'earth': ('#E8BC80', '#74441E'), 'night': ('#7C8EE0', '#1A1C4C'), 'sky': ('#A8DCFF', '#3270C8'),
}
FLAME = (hexc('#FFF6B8'), hexc('#FFA22E'), hexc('#D8361A'))
ICE = (hexc('#FFFFFF'), hexc('#9EE6FF'), hexc('#3C8AE0'))
YELLOW = (hexc('#FFFBD0'), hexc('#FFE04A'), hexc('#E09A10'))
WOOD = (hexc('#E8B880'), hexc('#A86A38'), hexc('#5E3818'))
STONE = (hexc('#E0D6E8'), hexc('#9A8EAE'), hexc('#5A4E70'))
VENOM = (hexc('#E6FFB0'), hexc('#7EDC3C'), hexc('#2E8A2A'))
VIOLET = (hexc('#F4DCFF'), hexc('#B98AF0'), hexc('#6A3CB8'))
PINK = (hexc('#FFE0F0'), hexc('#FF8AC0'), hexc('#C83C7A'))

made = []


def tile(element):
    """Background tile: rounded square gradient, centre glow, light top rim and a darker bottom rim."""
    top, bottom = BG[element]
    L = Layer(S, S)
    d = L.rrect(0, 0, S, S, 24)
    L.over(cov(d), vgrad(L, 0, S, hexc(top), hexc(bottom)))
    glow = np.clip(1 - np.sqrt((L.x - 64) ** 2 + (L.y - 60) ** 2) / 70, 0, 1) ** 1.8
    L.over(glow * cov(d), (255, 255, 255, 110))
    L.over(band(d, -4, -1) * (L.y < S * 0.5), (255, 255, 255, 120))
    L.over(band(d, -5, 0) * (L.y > S * 0.55), (0, 0, 0, 50))
    return L


def save(L, name):
    os.makedirs(OUT, exist_ok=True)
    img = L.image()
    img.save(os.path.join(OUT, 'skill_' + name + '.png'))
    made.append((name, img))


# Symbol helpers -------------------------------------------------------------------------------------------------------
def arrow(L, ax, ay, bx, by, shaft=WOOD, head=SILVER, feather=(hexc('#FFFFFF'), hexc('#F0F0F8'), hexc('#B0B4C8')), w=4.0, hs=1.0):
    """Arrow from the nock (a) to the tip (b)."""
    ang = math.atan2(by - ay, bx - ax)
    ln = math.hypot(bx - ax, by - ay)
    x, y = rot(L, ax, ay, ang)
    shaft_d = rrect_at(x, y, ax + 6, ay - w / 2, ax + ln - 16 * hs, ay + w / 2, w / 2)
    tip_x = ax + ln
    head_d = L.polygon([rot_pt((tip_x, ay), ax, ay, ang), rot_pt((tip_x - 20 * hs, ay - 10 * hs), ax, ay, ang),
                        rot_pt((tip_x - 15 * hs, ay), ax, ay, ang), rot_pt((tip_x - 20 * hs, ay + 10 * hs), ax, ay, ang)])
    fe = []
    for sgn in (-1, 1):
        fe.append(L.polygon([rot_pt((ax, ay), ax, ay, ang), rot_pt((ax + 16, ay), ax, ay, ang),
                             rot_pt((ax + 6, ay + sgn * 10), ax, ay, ang), rot_pt((ax - 4, ay + sgn * 10), ax, ay, ang)]))
    every = union(shaft_d, head_d, *fe)
    outline(L, every, 5)
    for f in fe:
        fill(L, f, feather, hi=False)
    fill(L, shaft_d, shaft, hi=False)
    fill(L, head_d, head)
    return every


def rot_pt(p, cx, cy, ang):
    px, py = p[0] - cx, p[1] - cy
    c, s = math.cos(ang), math.sin(ang)
    return (cx + px * c - py * s, cy + px * s + py * c)


def flame_d(L, cx, cy, size, lean=0.0):
    """Teardrop flame: a disc with a pointed top (lean tilts the tip)."""
    body = L.circle(cx, cy, size * 0.62)
    tip = L.polygon([(cx - size * 0.6, cy - size * 0.05), (cx + lean * size, cy - size * 1.35), (cx + size * 0.6, cy - size * 0.05)])
    return union(body, tip)


def flame(L, cx, cy, size, lean=0.0, ramp=FLAME, ow=5):
    d = flame_d(L, cx, cy, size, lean)
    outline(L, d, ow)
    fill(L, d, ramp, hi=False)
    inner = flame_d(L, cx + lean * size * 0.1, cy + size * 0.18, size * 0.55, lean)
    L.over(cov(inner), shade(ramp[0], -0.02))
    return d


def bolt_d(L, pts):
    return L.polygon(pts)


def bolt(L, pts, ramp=YELLOW, ow=5):
    d = bolt_d(L, pts)
    outline(L, d, ow)
    fill(L, d, ramp)
    return d


def snowflake(L, cx, cy, r, ramp=ICE):
    parts = []
    for k in range(6):
        a = k * math.pi / 3 - math.pi / 2
        ex, ey = cx + math.cos(a) * r, cy + math.sin(a) * r
        parts.append(seg(L, cx, cy, ex, ey, r * 0.09))
        for t in (0.55, 0.8):
            mx, my = cx + math.cos(a) * r * t, cy + math.sin(a) * r * t
            for sgn in (-1, 1):
                b = a + sgn * 0.7
                parts.append(seg(L, mx, my, mx + math.cos(b) * r * 0.26, my + math.sin(b) * r * 0.26, r * 0.07))
    parts.append(L.circle(cx, cy, r * 0.2))
    d = union(*parts)
    outline(L, d, 5)
    fill(L, d, ramp)
    return d


def orb(L, cx, cy, r, ramp, ow=5):
    d = L.circle(cx, cy, r)
    outline(L, d, ow)
    L.over(cov(d), radial(L, cx - r * 0.3, cy - r * 0.35, r * 1.6, ramp[0], ramp[2]))
    L.over(cov(L.ellipse(cx - r * 0.3, cy - r * 0.4, r * 0.38, r * 0.24)), (255, 255, 255, 200))
    return d


def trail(L, ax, ay, bx, by, w, color):
    """Comet tail from a (a point, transparent) widening to b (the head), with a bright core streak."""
    ln = math.hypot(bx - ax, by - ay)
    ang = math.atan2(by - ay, bx - ax)
    x, y = rot(L, ax, ay, ang)
    t = np.clip((x - ax) / ln, 0, 1)
    half = w * t ** 0.85
    inside = ((x >= ax) & (x <= ax + ln) & (np.abs(y - ay) <= half)).astype(np.float32)
    L.over(inside * t ** 1.4, color)
    core = ((x >= ax) & (x <= ax + ln) & (np.abs(y - ay) <= half * 0.35)).astype(np.float32)
    L.over(core * t ** 1.2, (255, 255, 255, 210))


def crescent_d(L, cx, cy, r, thick, a0, a1):
    rr = np.sqrt((L.x - cx) ** 2 + (L.y - cy) ** 2)
    th = np.arctan2(L.y - cy, L.x - cx)
    t = np.clip((th - a0) / (a1 - a0), 0, 1)
    w = thick * np.sin(t * math.pi) ** 0.7
    on = (th >= a0) & (th <= a1)
    return np.where(on, np.abs(rr - r) - w / 2, 1e3)


def slash(L, cx, cy, r, thick, a0, a1, ramp=(hexc('#FFFFFF'), hexc('#D8F0FF'), hexc('#7AB8F0')), ow=4):
    d = crescent_d(L, cx, cy, r, thick, a0, a1)
    outline(L, d, ow)
    fill(L, d, ramp, hi=False)
    return d


def cloud(L, cx, cy, w, ramp=(hexc('#F4F2FF'), hexc('#B8B0D8'), hexc('#6A6290'))):
    d = union(L.circle(cx - w * 0.3, cy + w * 0.05, w * 0.24), L.circle(cx, cy - w * 0.08, w * 0.3),
              L.circle(cx + w * 0.3, cy + w * 0.05, w * 0.22), rrect_at(L.x, L.y, cx - w * 0.48, cy, cx + w * 0.48, cy + w * 0.22, w * 0.11))
    outline(L, d, 5)
    fill(L, d, ramp)
    return d


def rock(L, cx, cy, r, ramp=STONE):
    d = L.polygon([(cx - r, cy + r * 0.2), (cx - r * 0.5, cy - r * 0.8), (cx + r * 0.4, cy - r * 0.9), (cx + r, cy - r * 0.1),
                   (cx + r * 0.6, cy + r * 0.8), (cx - r * 0.5, cy + r * 0.8)])
    outline(L, d, 4)
    fill(L, d, ramp)
    return d


def sparkle(L, cx, cy, s, color=WHITE):
    L.over(cov(L.polygon(star_pts(cx, cy, s, s * 0.3, 4, 0)) - 2), INK[:3] + (150,))
    L.over(cov(L.polygon(star_pts(cx, cy, s, s * 0.3, 4, 0))), color)


def drop(L, cx, cy, r, ramp):
    d = union(L.circle(cx, cy + r * 0.3, r * 0.75), L.polygon([(cx - r * 0.7, cy + r * 0.1), (cx, cy - r * 1.1), (cx + r * 0.7, cy + r * 0.1)]))
    outline(L, d, 4)
    fill(L, d, ramp)
    return d


def wing(L, cx, cy, flip, ramp=(hexc('#FFFFFF'), hexc('#E8EEFF'), hexc('#98A8D0')), scale=1.0):
    s = -1 if flip else 1
    f = [(0, 0), (40, -30), (36, -14), (48, -16), (38, -2), (46, 0), (30, 10), (12, 14)]
    pts = [(cx + s * px * scale, cy + py * scale) for (px, py) in f]
    d = L.polygon(pts)
    outline(L, d, 4)
    fill(L, d, ramp)
    return d


def axe(L, ax, ay, bx, by, ramp=SILVER):
    ang = math.atan2(by - ay, bx - ax)
    ln = math.hypot(bx - ax, by - ay)
    x, y = rot(L, ax, ay, ang)
    handle = rrect_at(x, y, ax, ay - 4.5, ax + ln, ay + 4.5, 4)
    hx = ax + ln * 0.78
    blade = L.polygon([rot_pt(p, ax, ay, ang) for p in [(hx - 8, ay - 4), (hx + 10, ay - 4), (hx + 16, ay - 26), (hx - 14, ay - 26)]])
    edge = L.polygon([rot_pt(p, ax, ay, ang) for p in [(hx - 14, ay - 26), (hx + 16, ay - 26), (hx + 20, ay - 32), (hx - 18, ay - 32)]])
    d = union(handle, blade, edge)
    outline(L, d, 5)
    fill(L, handle, WOOD, hi=False)
    fill(L, union(blade, edge), ramp)
    return d


def hammer(L, ax, ay, bx, by, ramp=SILVER):
    ang = math.atan2(by - ay, bx - ax)
    ln = math.hypot(bx - ax, by - ay)
    x, y = rot(L, ax, ay, ang)
    handle = rrect_at(x, y, ax, ay - 5, ax + ln - 10, ay + 5, 4)
    head = rrect_at(x, y, ax + ln - 22, ay - 22, ax + ln + 4, ay + 22, 6)
    d = union(handle, head)
    outline(L, d, 5)
    fill(L, handle, WOOD, hi=False)
    fill(L, head, ramp)
    return d


def eye(L, cx, cy, w, iris=(hexc('#FFE890'), hexc('#F0A020'), hexc('#8A4A08'))):
    d = inter(L.circle(cx, cy - w * 0.55, w * 0.95), L.circle(cx, cy + w * 0.55, w * 0.95))
    outline(L, d, 5)
    L.over(cov(d), vgrad(L, cy - w * 0.5, cy + w * 0.5, hexc('#FFFFFF'), hexc('#E0E4F0')))
    ir = L.circle(cx, cy, w * 0.34)
    L.over(cov(ir), radial(L, cx - 4, cy - 4, w * 0.5, iris[0], iris[2]))
    L.over(cov(L.circle(cx, cy, w * 0.15)), INK)
    L.over(cov(L.circle(cx - w * 0.1, cy - w * 0.12, w * 0.07)), WHITE)
    return d


def ring_band(L, cx, cy, r, w, ramp, ow=4):
    d = np.abs(L.circle(cx, cy, r)) - w / 2
    outline(L, d, ow)
    fill(L, d, ramp, hi=False)
    return d


def swirl(L, cx, cy, r, arms, ramp, width=7.0, turns=0.9):
    rr = np.sqrt((L.x - cx) ** 2 + (L.y - cy) ** 2)
    th = np.arctan2(L.y - cy, L.x - cx)
    best = np.full(rr.shape, 1e3, np.float32)
    for k in range(arms):
        off = k * 2 * math.pi / arms
        for turn in range(-1, 3):
            sr = r * ((th + math.pi + off) % (2 * math.pi) / (2 * math.pi) / turns + turn / turns)
            dd = np.abs(rr - sr)
            best = np.minimum(best, np.where(sr < r, dd, 1e3))
    d = np.maximum(best - width / 2 * np.clip(rr / r, 0.25, 1), rr - r)
    outline(L, d, 4)
    fill(L, d, ramp, hi=False)
    return d


def chain_link(L, cx, cy, ang, ramp=SILVER):
    x, y = rot(L, cx, cy, ang)
    outer = rrect_at(x, y, cx - 14, cy - 8, cx + 14, cy + 8, 8)
    hole = rrect_at(x, y, cx - 8, cy - 3, cx + 8, cy + 3, 3)
    d = np.maximum(outer, -hole)
    outline(L, d, 3)
    fill(L, d, ramp, hi=False)
    return d


def cross_shape(L, cx, cy, arm, w):
    return union(rrect_at(L.x, L.y, cx - w / 2, cy - arm, cx + w / 2, cy + arm, w * 0.3),
                 rrect_at(L.x, L.y, cx - arm, cy - w / 2, cx + arm, cy + w / 2, w * 0.3))


# Icons ----------------------------------------------------------------------------------------------------------------
def i_power_strike():
    L = tile('fire')
    for k in range(8):
        a = k * math.pi / 4 + 0.2
        L.over(cov(seg(L, 88 + math.cos(a) * 18, 38 + math.sin(a) * 18, 88 + math.cos(a) * 34, 38 + math.sin(a) * 34, 3.5)), (255, 246, 200, 230))
    sword(L, 22, 108, 98, 30)
    sparkle(L, 92, 34, 14)
    return L


def i_whirlwind():
    L = tile('sky')
    slash(L, 64, 66, 36, 16, -math.pi * 0.95, math.pi * 0.05)
    slash(L, 64, 66, 36, 16, 0.2, math.pi * 1.05)
    sword(L, 40, 92, 92, 40, scale=0.8)
    return L


def i_battle_cry():
    L = tile('fire')
    bell = L.polygon([(36, 50), (92, 24), (92, 104), (36, 78)])
    mouth = rrect_at(L.x, L.y, 18, 46, 42, 82, 8)
    d = union(bell, mouth)
    outline(L, d)
    fill(L, bell, GOLD)
    fill(L, mouth, ORANGE)
    for k, rr in enumerate((16, 28)):
        arc = np.maximum(np.abs(L.circle(96, 64, rr)) - 3, 96 - L.x)
        L.over(cov(arc - 3), INK[:3] + (180,))
        L.over(cov(arc), (255, 248, 220, 255))
    return L


def i_quick_slash():
    L = tile('steel')
    for k, off in enumerate((-22, 0, 22)):
        d = slash(L, 30 + off, 70 + off * 0.2, 54, 13, -1.25, 0.25)
    sparkle(L, 98, 30, 10)
    return L


def i_iron_skin():  # 회피 기동: a hexagon barrier with dash streaks
    L = tile('wind')
    for (y, ln) in ((40, 40), (64, 52), (88, 36)):
        L.over(cov(rrect_at(L.x, L.y, 10, y - 3, 10 + ln, y + 3, 3)), (255, 255, 255, 170))
    hexd = L.polygon([(64 + math.cos(k * math.pi / 3) * 40 + 10, 64 + math.sin(k * math.pi / 3) * 40) for k in range(6)])
    outline(L, hexd)
    L.over(cov(hexd), radial(L, 66, 54, 60, hexc('#E8FFF8'), hexc('#3AB894')))
    inner = L.polygon([(64 + math.cos(k * math.pi / 3) * 26 + 10, 64 + math.sin(k * math.pi / 3) * 26) for k in range(6)])
    L.over(np.clip(1 - np.abs(inner) / 2.5, 0, 1), (255, 255, 255, 200))
    sparkle(L, 92, 36, 9)
    return L


def i_first_aid():
    L = tile('nature')
    d = cross_shape(L, 64, 64, 40, 28)
    outline(L, d)
    fill(L, d, (hexc('#FFFFFF'), hexc('#E8FFE8'), hexc('#8ACB8A')))
    for (x, y, s) in ((26, 30, 8), (100, 96, 10), (98, 30, 6)):
        sparkle(L, x, y, s)
    return L


def i_fireball():
    L = tile('fire')
    trail(L, 8, 120, 68, 60, 26, (255, 230, 150, 220))
    orb(L, 74, 54, 28, FLAME)
    flame(L, 78, 46, 18, lean=0.4, ramp=(hexc('#FFFFFF'), hexc('#FFF0A0'), hexc('#FFB040')), ow=3)
    return L


def i_frost_nova():
    L = tile('ice')
    L.over(np.clip(1 - np.abs(L.circle(64, 64, 46)) / 4, 0, 1), (255, 255, 255, 160))
    snowflake(L, 64, 64, 40)
    return L


def i_thunder():
    L = tile('storm')
    cloud(L, 64, 30, 80)
    bolt(L, [(60, 44), (40, 80), (58, 80), (46, 118), (88, 66), (68, 66), (80, 44)])
    return L


def i_berserk():
    L = tile('blood')
    L.over(np.clip(1 - L.circle(64, 66, 0) / 60, 0, 1) ** 2, (255, 80, 60, 120))
    axe(L, 24, 108, 92, 26)
    axe(L, 104, 108, 36, 26)
    for (x, y) in ((22, 30), (106, 32)):
        drop(L, x, y, 8, RED)
    return L


def i_poison_cloud():  # 독침 화살
    L = tile('nature')
    for (x, y, r) in ((38, 88, 16), (58, 96, 12), (30, 104, 9)):
        d = L.circle(x, y, r)
        L.over(cov(d), (150, 230, 90, 170))
    arrow(L, 18, 108, 104, 22, head=VENOM, shaft=WOOD)
    drop(L, 102, 46, 9, VENOM)
    drop(L, 86, 22, 6, VENOM)
    return L


def i_eagle_eye():
    L = tile('holy')
    ring_band(L, 64, 64, 46, 6, RED)
    for k in range(4):
        a = k * math.pi / 2
        L.over(cov(seg(L, 64 + math.cos(a) * 40, 64 + math.sin(a) * 40, 64 + math.cos(a) * 54, 64 + math.sin(a) * 54, 4)), INK)
    eye(L, 64, 64, 30)
    return L


def i_meteor():
    L = tile('night')
    for (x, y) in ((24, 30), (40, 18), (100, 96)):
        sparkle(L, x, y, 6)
    trail(L, 120, 8, 58, 70, 28, (255, 200, 110, 230))
    flame(L, 56, 76, 30, lean=-0.7)
    rock(L, 50, 82, 18, STONE)
    return L


def i_chain_lightning():
    L = tile('storm')
    pts = [(18, 30), (52, 54), (40, 72), (86, 98), (74, 64), (110, 40)]
    for a, b in zip(pts, pts[1:]):
        d = seg(L, a[0], a[1], b[0], b[1], 4.5)
        L.over(cov(d - 4), INK)
    for a, b in zip(pts, pts[1:]):
        L.over(cov(seg(L, a[0], a[1], b[0], b[1], 4.5)), hexc('#FFF6A0'))
        L.over(cov(seg(L, a[0], a[1], b[0], b[1], 1.8)), WHITE)
    for (x, y) in (pts[0], pts[2], pts[4], pts[5]):
        orb(L, x, y, 8, YELLOW, 4)
    return L


def i_blade_storm():
    L = tile('steel')
    L.over(np.clip(1 - np.abs(L.circle(64, 64, 34)) / 10, 0, 1), (220, 236, 255, 160))
    for k in range(3):
        a = k * 2 * math.pi / 3 - math.pi / 2
        cx, cy = 64 + math.cos(a) * 30, 64 + math.sin(a) * 30
        tx, ty = cx + math.cos(a + math.pi / 2) * 30, cy + math.sin(a + math.pi / 2) * 30
        sword(L, cx - math.cos(a + math.pi / 2) * 18, cy - math.sin(a + math.pi / 2) * 18, tx, ty, scale=0.6)
    return L


def i_holy_light():  # 축복의 화살
    L = tile('holy')
    ring_band(L, 64, 34, 22, 6, GOLD)
    arrow(L, 22, 106, 100, 28, head=GOLD, shaft=(hexc('#FFFFFF'), hexc('#F0E8D0'), hexc('#C0A870')))
    for (x, y, s) in ((30, 60, 9), (96, 92, 11), (100, 60, 6)):
        sparkle(L, x, y, s)
    return L


def i_glacier_spear():  # 빙결 화살
    L = tile('ice')
    arrow(L, 18, 108, 100, 26, head=ICE, shaft=(hexc('#E8FBFF'), hexc('#A8E0F8'), hexc('#4A90C8')))
    for (x, y, r) in ((92, 30, 14), (104, 50, 9)):
        d = L.polygon([(x, y - r), (x + r * 0.5, y), (x, y + r), (x - r * 0.5, y)])
        outline(L, d, 4)
        fill(L, d, ICE)
    return L


def i_war_god():  # 군신의 가호: winged helm in a holy glow
    L = tile('holy')
    wing(L, 40, 66, True, scale=1.05)
    wing(L, 88, 66, False, scale=1.05)
    dome = union(L.circle(64, 58, 28), rrect_at(L.x, L.y, 36, 56, 92, 94, 12))
    outline(L, dome)
    fill(L, dome, GOLD)
    slit = union(rrect_at(L.x, L.y, 44, 62, 84, 70, 4), rrect_at(L.x, L.y, 60, 62, 68, 88, 4))
    L.over(cov(slit), hexc('#3A2412'))
    sparkle(L, 64, 26, 10)
    return L


def i_dragon_breath():
    L = tile('fire')
    cone = L.polygon([(30, 40), (116, 18), (122, 92), (30, 64)])
    L.over(cov(cone), (255, 220, 120, 160))
    flame(L, 96, 62, 26, lean=0.9)
    flame(L, 72, 58, 20, lean=0.9)
    head = union(L.ellipse(36, 56, 22, 16), L.polygon([(22, 48), (12, 28), (32, 42)]), L.polygon([(38, 44), (34, 24), (48, 42)]))
    outline(L, head)
    fill(L, head, RED)
    L.over(cov(L.circle(38, 50, 3.5)), hexc('#FFE24A'))
    L.over(cov(L.polygon([(48, 62), (60, 58), (52, 68)])), WHITE)
    return L


def i_judgement():
    L = tile('holy')
    L.over(cov(rrect_at(L.x, L.y, 44, 0, 84, 128, 18)), (255, 250, 220, 120))
    sword(L, 64, 6, 64, 116, blade=GOLD, guard=(hexc('#FFFFFF'), hexc('#E8ECF8'), hexc('#9AA4C0')))
    for (x, y, s) in ((30, 30, 9), (98, 40, 11), (36, 96, 7)):
        sparkle(L, x, y, s)
    return L


def i_time_stop():  # 속박의 화살비: arrows raining into a chain ring
    L = tile('void')
    for x in (34, 64, 94):
        arrow(L, x - 6, 8, x, 74, head=VIOLET, w=3.5, hs=0.8)
    for k in range(6):
        a = k * math.pi / 3 + math.pi / 6
        chain_link(L, 64 + math.cos(a) * 34, 92 + math.sin(a) * 14, a + math.pi / 2)
    return L


def i_sword_rain():
    L = tile('night')
    for (x, y, sc) in ((34, 16, 0.55), (64, 30, 0.7), (94, 12, 0.55)):
        sword(L, x, y, x, y + 86 * sc, scale=sc)
    for (x, y) in ((20, 112), (64, 114), (108, 112)):
        L.over(cov(L.ellipse(x, y, 12, 4)), (255, 255, 255, 140))
    return L


def i_phoenix():
    L = tile('fire')
    for sgn in (False, True):
        wing(L, 64 + (-14 if not sgn else 14), 62, sgn, ramp=FLAME, scale=1.25)
    body = union(L.ellipse(64, 70, 14, 24), L.circle(64, 42, 12))
    outline(L, body)
    fill(L, body, (hexc('#FFF2A0'), hexc('#FFB43A'), hexc('#D85A1A')))
    tail = L.polygon([(56, 88), (72, 88), (82, 120), (64, 106), (46, 120)])
    outline(L, tail, 4)
    fill(L, tail, FLAME)
    L.over(cov(L.polygon([(70, 40), (82, 44), (70, 48)])), hexc('#FFE24A'))
    L.over(cov(L.circle(66, 40, 2.5)), INK)
    return L


def i_black_hole():
    L = tile('void')
    swirl(L, 64, 64, 50, 3, VIOLET, width=9)
    core = L.circle(64, 64, 15)
    L.over(cov(core - 4), (230, 180, 255, 255))
    L.over(cov(core), hexc('#0C0418'))
    return L


def i_shield_bash():
    L = tile('steel')
    for k in range(5):
        a = -0.9 + k * 0.45
        L.over(cov(seg(L, 92 + math.cos(a) * 20, 60 + math.sin(a) * 20, 92 + math.cos(a) * 34, 60 + math.sin(a) * 34, 3.5)), (255, 236, 160, 240))
    d = shield_shape(L, 56, 20, 70, 88)
    outline(L, d)
    fill(L, d, GOLD)
    inner = shield_shape(L, 56, 28, 54, 70)
    fill(L, inner, BLUE)
    L.over(cov(L.circle(56, 58, 8)), GOLD[1])
    return L


def i_ground_slam():
    L = tile('earth')
    ground = rrect_at(L.x, L.y, 6, 92, 122, 124, 8)
    outline(L, ground, 4)
    fill(L, ground, BROWN, hi=False)
    for (x0, x1) in ((60, 44), (64, 70), (68, 92)):
        L.over(cov(seg(L, x0, 94, x1, 120, 2.2)), INK)
    for (x, y, r) in ((26, 80, 8), (102, 76, 7)):
        rock(L, x, y, r)
    hammer(L, 96, 20, 54, 70)
    return L


def i_cleave():
    L = tile('blood')
    slash(L, 54, 70, 44, 22, -2.6, 0.9, ramp=(hexc('#FFFFFF'), hexc('#FFE0D8'), hexc('#F07A6A')))
    sword(L, 70, 100, 112, 30, scale=0.75)
    return L


def i_earthquake():
    L = tile('earth')
    left = L.polygon([(4, 70), (58, 64), (50, 124), (4, 124)])
    right = L.polygon([(70, 62), (124, 68), (124, 124), (62, 124)])
    for d in (left, right):
        outline(L, d, 4)
        fill(L, d, BROWN)
    L.over(cov(L.polygon([(58, 64), (70, 62), (62, 124), (50, 124)])), hexc('#2A1A10'))
    for (x, y, r) in ((30, 40, 11), (86, 30, 9), (60, 22, 7)):
        rock(L, x, y, r)
    return L


def i_dragon_slash():
    L = tile('blood')
    for k in range(3):
        slash(L, 22 + k * 22, 78 - k * 8, 48, 16, -1.35, 0.35, ramp=(hexc('#FFFFFF'), hexc('#FFC0A0'), hexc('#E0402A')), ow=5)
    flame(L, 100, 36, 18, lean=0.2)
    return L


def i_magic_missile():
    L = tile('void')
    for (x, y) in ((90, 30), (98, 64), (88, 98)):
        trail(L, x - 64, y + 20, x, y, 12, (220, 170, 255, 200))
        orb(L, x, y, 13, VIOLET, 4)
    return L


def i_ember():
    L = tile('fire')
    flame(L, 46, 86, 26, lean=-0.15)
    flame(L, 86, 76, 32, lean=0.15)
    for (x, y) in ((28, 38), (64, 28), (104, 30)):
        L.over(cov(L.circle(x, y, 4)), hexc('#FFE070'))
    return L


def i_ice_lance():
    L = tile('ice')
    x, y = rot(L, 64, 64, -math.pi / 4)
    lance = L.polygon([rot_pt(p, 64, 64, -math.pi / 4) for p in [(4, 64), (84, 52), (124, 64), (84, 76)]])
    outline(L, lance)
    fill(L, lance, ICE)
    L.over(cov(L.polygon([rot_pt(p, 64, 64, -math.pi / 4) for p in [(20, 63), (84, 56), (110, 64), (84, 62)]])), (255, 255, 255, 230))
    for (cx, cy) in ((34, 34), (100, 98)):
        sparkle(L, cx, cy, 8)
    return L


def i_flame_pillar():
    L = tile('fire')
    pillar = union(rrect_at(L.x, L.y, 40, 30, 88, 118, 18), flame_d(L, 64, 34, 30))
    outline(L, pillar)
    fill(L, pillar, FLAME)
    core = union(rrect_at(L.x, L.y, 54, 44, 74, 116, 10), flame_d(L, 64, 48, 16))
    L.over(cov(core), hexc('#FFF4B0'))
    base = L.ellipse(64, 116, 44, 9)
    L.over(cov(base), (255, 210, 120, 200))
    return L


def i_arcane_storm():
    L = tile('void')
    cloud(L, 64, 34, 92, ramp=(hexc('#E8DCFF'), hexc('#9A86D8'), hexc('#4E3C8E')))
    bolt(L, [(48, 48), (34, 80), (48, 80), (38, 116), (68, 72), (54, 72), (64, 48)], ramp=(hexc('#FFFFFF'), hexc('#D8B0FF'), hexc('#8A4AE0')))
    bolt(L, [(84, 50), (74, 76), (86, 76), (78, 104), (102, 70), (90, 70), (98, 50)], ramp=(hexc('#FFFFFF'), hexc('#D8B0FF'), hexc('#8A4AE0')))
    return L


def i_arrow_shot():
    L = tile('wind')
    arrow(L, 14, 112, 112, 16, w=5.5, hs=1.3)
    for k in range(3):
        L.over(cov(seg(L, 20 + k * 10, 80 - k * 10, 34 + k * 10, 66 - k * 10, 2)), (255, 255, 255, 160))
    return L


def i_scatter_shot():
    L = tile('wind')
    for (bx, by) in ((70, 14), (108, 32), (114, 74)):
        arrow(L, 18, 108, bx, by, w=3.5, hs=0.85)
    return L


def i_piercing_arrow():
    L = tile('holy')
    for cx in (52, 86):
        ring_band(L, cx, 64, 18 if cx == 52 else 14, 6, RED)
    arrow(L, 6, 64, 122, 64, head=(hexc('#FFFFFF'), hexc('#E8F0FF'), hexc('#8AA0D0')), w=4.5)
    return L


def i_storm_arrows():
    L = tile('wind')
    L.over(np.clip(1 - np.abs(L.circle(64, 70, 40)) / 7, 0, 1) * ((L.y > 50) | (L.x < 64)), (230, 255, 245, 170))
    for (ax, ay, bx, by) in ((14, 64, 76, 30), (24, 96, 92, 70), (40, 118, 108, 104)):
        arrow(L, ax, ay, bx, by, w=3.5, hs=0.8)
    return L


def i_starfall_arrow():
    L = tile('night')
    st = L.polygon(star_pts(34, 34, 22, 9))
    outline(L, st, 4)
    fill(L, st, GOLD)
    for (ax, ay, bx, by) in ((60, 6, 96, 94), (88, 4, 116, 76), (40, 56, 66, 118)):
        arrow(L, ax, ay, bx, by, head=(hexc('#FFFFFF'), hexc('#D0FFD0'), hexc('#4AB04A')), w=3.2, hs=0.75)
    return L


# Job main attacks and ultimates ---------------------------------------------------------------------------------------
def i_main_beginner():
    L = tile('sky')
    slash(L, 34, 92, 66, 20, -1.45, 0.05)
    sparkle(L, 98, 30, 10)
    return L


def i_main_warrior():  # 파워 슬래시
    L = tile('steel')
    slash(L, 40, 86, 56, 18, -1.5, 0.2, ramp=(hexc('#FFFFFF'), hexc('#FFE8B0'), hexc('#E0A030')))
    sword(L, 30, 104, 100, 34, scale=0.85)
    return L


def i_main_knight():  # 홀리 차지
    L = tile('holy')
    L.over(cov(rrect_at(L.x, L.y, 50, 4, 78, 124, 14)), (255, 255, 230, 140))
    d = shield_shape(L, 64, 26, 64, 80)
    outline(L, d)
    fill(L, d, (hexc('#FFFFFF'), hexc('#E8ECF8'), hexc('#9AA4C0')))
    c = cross_shape(L, 64, 62, 22, 12)
    fill(L, c, GOLD)
    return L


def i_main_berserker():  # 레이징 블로우
    L = tile('blood')
    axe(L, 22, 110, 100, 26, ramp=(hexc('#FFE0D8'), hexc('#E86A5A'), hexc('#8E1A20')))
    for (x, y) in ((96, 92), (30, 36)):
        sparkle(L, x, y, 9, (255, 220, 200, 255))
    return L


def i_main_mage():  # 매직 볼트
    L = tile('void')
    trail(L, 6, 122, 70, 52, 24, (230, 190, 255, 230))
    orb(L, 76, 46, 28, VIOLET)
    sparkle(L, 100, 92, 8)
    return L


def i_main_pyro():  # 플레임 볼트
    L = tile('fire')
    trail(L, 6, 122, 70, 52, 24, (255, 220, 130, 230))
    orb(L, 76, 46, 28, FLAME)
    flame(L, 78, 40, 16, lean=0.3, ramp=(hexc('#FFFFFF'), hexc('#FFF0A0'), hexc('#FFB040')), ow=3)
    return L


def i_main_cryo():  # 아이스 볼트
    L = tile('ice')
    trail(L, 6, 122, 70, 52, 24, (230, 250, 255, 230))
    orb(L, 76, 46, 28, ICE)
    snowflake(L, 76, 46, 18, ramp=(hexc('#FFFFFF'), hexc('#FFFFFF'), hexc('#A8D8F8')))
    return L


def i_main_archer():  # 더블 샷
    L = tile('wind')
    arrow(L, 10, 82, 106, 34, w=4)
    arrow(L, 22, 108, 118, 60, w=4)
    return L


def i_main_ranger():  # 애로우 블로우
    L = tile('nature')
    for (ax, ay, bx, by) in ((8, 50, 110, 30), (8, 76, 116, 64), (8, 102, 110, 98)):
        arrow(L, ax, ay, bx, by, w=3.5, hs=0.85)
    return L


def i_main_sniper():  # 저격
    L = tile('night')
    ring_band(L, 64, 64, 40, 6, RED)
    for k in range(4):
        a = k * math.pi / 2
        L.over(cov(seg(L, 64 + math.cos(a) * 18, 64 + math.sin(a) * 18, 64 + math.cos(a) * 54, 64 + math.sin(a) * 54, 3.5) - 3), INK)
        L.over(cov(seg(L, 64 + math.cos(a) * 18, 64 + math.sin(a) * 18, 64 + math.cos(a) * 54, 64 + math.sin(a) * 54, 3.5)), RED[1])
    arrow(L, 12, 116, 66, 62, w=3.5, hs=0.8)
    return L


def i_ult_guardian_cross():
    L = tile('holy')
    L.over(np.clip(1 - L.circle(64, 64, 0) / 64, 0, 1) ** 1.5, (255, 255, 230, 160))
    c = cross_shape(L, 64, 64, 50, 26)
    outline(L, c)
    fill(L, c, (hexc('#FFFFFF'), hexc('#FFF0B8'), hexc('#D89A20')))
    inner = cross_shape(L, 64, 64, 36, 10)
    L.over(cov(inner), (255, 255, 255, 220))
    gem_d = L.circle(64, 64, 9)
    outline(L, gem_d, 3)
    fill(L, gem_d, BLUE)
    return L


def i_ult_blood_rage():
    L = tile('blood')
    L.over(np.clip(1 - L.circle(64, 64, 0) / 64, 0, 1) ** 1.2, (255, 60, 60, 150))
    axe(L, 18, 112, 86, 22, ramp=(hexc('#FFE0D8'), hexc('#E86A5A'), hexc('#8E1A20')))
    axe(L, 110, 112, 42, 22, ramp=(hexc('#FFE0D8'), hexc('#E86A5A'), hexc('#8E1A20')))
    drop(L, 64, 96, 14, RED)
    return L


def i_ult_inferno():
    L = tile('fire')
    flame(L, 40, 96, 26, lean=-0.3)
    flame(L, 88, 96, 26, lean=0.3)
    flame(L, 64, 84, 40)
    L.over(cov(L.circle(64, 90, 10)), (255, 255, 230, 230))
    return L


def i_ult_blizzard():
    L = tile('ice')
    for k in range(4):
        L.over(cov(rrect_at(L.x, L.y, 8 + k * 6, 20 + k * 26, 120, 26 + k * 26, 3)), (255, 255, 255, 130))
    snowflake(L, 46, 54, 30)
    snowflake(L, 92, 86, 22)
    return L


def i_ult_arrow_rain():
    L = tile('nature')
    cloud(L, 64, 22, 96, ramp=(hexc('#F0FFF0'), hexc('#A8D8A8'), hexc('#4A8A5A')))
    for (x, y) in ((28, 44), (52, 56), (76, 44), (100, 56)):
        arrow(L, x - 6, y, x + 4, y + 62, w=3.2, hs=0.75)
    return L


def i_ult_death_shot():
    L = tile('night')
    ring_band(L, 64, 64, 44, 6, RED)
    head = L.circle(64, 58, 24)
    jaw = rrect_at(L.x, L.y, 50, 70, 78, 90, 7)
    sk = union(head, jaw)
    outline(L, sk)
    fill(L, sk, (hexc('#FFFFFF'), hexc('#ECE6F2'), hexc('#A89CB8')))
    for cx in (54, 74):
        L.over(cov(L.ellipse(cx, 60, 7, 8)), INK)
        L.over(cov(L.circle(cx + 2, 58, 2.5)), (255, 80, 80, 255))
    return L


ICONS = {
    'power_strike': i_power_strike, 'whirlwind': i_whirlwind, 'battle_cry': i_battle_cry,
    'quick_slash': i_quick_slash, 'iron_skin': i_iron_skin, 'first_aid': i_first_aid, 'fireball': i_fireball,
    'frost_nova': i_frost_nova, 'thunder': i_thunder, 'berserk': i_berserk, 'poison_cloud': i_poison_cloud,
    'eagle_eye': i_eagle_eye, 'meteor': i_meteor, 'chain_lightning': i_chain_lightning, 'blade_storm': i_blade_storm,
    'holy_light': i_holy_light, 'glacier_spear': i_glacier_spear, 'war_god': i_war_god, 'dragon_breath': i_dragon_breath,
    'judgement': i_judgement, 'time_stop': i_time_stop, 'sword_rain': i_sword_rain, 'phoenix': i_phoenix,
    'black_hole': i_black_hole, 'shield_bash': i_shield_bash, 'ground_slam': i_ground_slam, 'cleave': i_cleave,
    'earthquake': i_earthquake, 'dragon_slash': i_dragon_slash, 'magic_missile': i_magic_missile, 'ember': i_ember,
    'ice_lance': i_ice_lance, 'flame_pillar': i_flame_pillar, 'arcane_storm': i_arcane_storm,
    'arrow_shot': i_arrow_shot, 'scatter_shot': i_scatter_shot, 'piercing_arrow': i_piercing_arrow,
    'storm_arrows': i_storm_arrows, 'starfall_arrow': i_starfall_arrow,
    'main_beginner': i_main_beginner, 'main_warrior': i_main_warrior, 'main_knight': i_main_knight,
    'main_berserker': i_main_berserker, 'main_mage': i_main_mage, 'main_pyro': i_main_pyro, 'main_cryo': i_main_cryo,
    'main_archer': i_main_archer, 'main_ranger': i_main_ranger, 'main_sniper': i_main_sniper,
    'ult_guardian_cross': i_ult_guardian_cross, 'ult_blood_rage': i_ult_blood_rage, 'ult_inferno': i_ult_inferno,
    'ult_blizzard': i_ult_blizzard, 'ult_arrow_rain': i_ult_arrow_rain, 'ult_death_shot': i_ult_death_shot,
}

if __name__ == '__main__':
    only = [a for a in args[1:]] if len(args) > 1 else None
    for name, fn in ICONS.items():
        if only and name not in only:
            continue
        save(fn(), name)
    if PREVIEW:
        cols = 9
        cell = S + 12
        rows = (len(made) + cols - 1) // cols
        sheet = Image.new('RGBA', (cols * cell, rows * cell), (232, 220, 196, 255))
        for k, (_, im) in enumerate(made):
            sheet.alpha_composite(im, ((k % cols) * cell + 6, (k // cols) * cell + 6))
        sheet.save(PREVIEW)
    print('made', len(made), 'skill icons in', os.path.abspath(OUT))
