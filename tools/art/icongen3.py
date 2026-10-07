"""Smooth UI icon set (D-108) -> Assets/SoloHero/Art/UI/Hd/Icons/hdicon_{name}.png (128 x 128).

Usage: python tools/art/icongen3.py [OUT_DIR] [--preview PREVIEW.png]
Same names as the 16 px pixel icons in Art/UI/Icons (UiSkin.Icon prefers these). Shapes are signed-distance fields
with a plum ink outline, a vertical shading gradient and a soft top highlight, so they stay crisp at 40-112 units.
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from uikit import Layer, band, cov, hexc, radial, shade, vgrad  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art', 'UI', 'Hd', 'Icons')
# OUT_DIR is the argument that looks like a path; bare names select icons (D-111).
args = [a for i, a in enumerate(sys.argv[1:], 1)
        if not a.startswith('--') and sys.argv[i - 1] != '--preview' and ('/' in a or os.sep in a)]
if args:
    OUT = args[0]
PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

S = 128
INK = hexc('#2A1F3D')
OW = 6.0  # outline width
WHITE = (255, 255, 255, 255)

GOLD = (hexc('#FFF1A0'), hexc('#FFC234'), hexc('#D98A10'))
SILVER = (hexc('#FFFFFF'), hexc('#C9D2E6'), hexc('#7E8AA8'))
RED = (hexc('#FF9C94'), hexc('#F04848'), hexc('#B0263A'))
BLUE = (hexc('#9EE0FF'), hexc('#3C9BF2'), hexc('#2059B8'))
PURPLE = (hexc('#E4BCFF'), hexc('#9D5CF0'), hexc('#5C2DB0'))
GREEN = (hexc('#B4F590'), hexc('#4CC24A'), hexc('#227A36'))
ORANGE = (hexc('#FFD08A'), hexc('#FF8C2A'), hexc('#C2501A'))
BROWN = (hexc('#E0A870'), hexc('#A8683A'), hexc('#6A3E1E'))

made = []


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, 'hdicon_' + name + '.png'))
    made.append((name, img))


def union(*ds):
    out = ds[0]
    for d in ds[1:]:
        out = np.minimum(out, d)
    return out


def inter(a, b):
    return np.maximum(a, b)


def seg(L, ax, ay, bx, by, w):
    """Distance to a capsule (thick line) from a to b with half width w."""
    px, py = L.x - ax, L.y - ay
    ex, ey = bx - ax, by - ay
    t = np.clip((px * ex + py * ey) / (ex * ex + ey * ey), 0, 1)
    dx, dy = px - ex * t, py - ey * t
    return np.sqrt(dx * dx + dy * dy) - w


def rot(L, cx, cy, ang):
    """Returns (x, y) arrays rotated by -ang around (cx, cy), for drawing rotated shapes."""
    c, s = math.cos(ang), math.sin(ang)
    x, y = L.x - cx, L.y - cy
    return cx + x * c + y * s, cy - x * s + y * c


def rrect_at(x, y, x0, y0, x1, y1, r):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2, (y1 - y0) / 2
    r = min(r, hx, hy)
    qx = np.abs(x - cx) - (hx - r)
    qy = np.abs(y - cy) - (hy - r)
    return np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2) + np.minimum(np.maximum(qx, qy), 0) - r


def fill(L, d, ramp, y0=None, y1=None, hi=True):
    """Gradient fill of shape d with a light top rim and a darker bottom rim."""
    light, mid, dark = ramp
    ys = np.where(d < 0, L.y, np.nan)
    if y0 is None:
        y0 = np.nanmin(ys) if np.isfinite(np.nanmin(ys)) else 0
    if y1 is None:
        y1 = np.nanmax(ys) if np.isfinite(np.nanmax(ys)) else S
    L.over(cov(d), vgrad(L, y0, y1, shade(light, -0.05), mid))
    L.over(cov(d) * np.clip((L.y - (y0 + (y1 - y0) * 0.55)) / ((y1 - y0) * 0.45 + 1e-6), 0, 1), dark[:3] + (170,))
    if hi:
        L.over(band(d, -4.5, -1.2) * np.clip(1 - (L.y - y0) / ((y1 - y0) * 0.55 + 1e-6), 0, 1), (255, 255, 255, 190))


def outline(L, d, w=OW):
    L.over(cov(d - w), INK)


def gloss(L, d, cx, cy, rx, ry, alpha=90):
    L.over(cov(inter(d + 3, L.ellipse(cx, cy, rx, ry))), (255, 255, 255, alpha))


def new():
    return Layer(S, S)


def star_pts(cx, cy, r_out, r_in, n=5, rot0=-math.pi / 2):
    pts = []
    for k in range(n * 2):
        r = r_out if k % 2 == 0 else r_in
        a = rot0 + k * math.pi / n
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


# Icons ---------------------------------------------------------------------------------------------------------------
def coin():
    L = new()
    d = L.circle(64, 64, 52)
    outline(L, d)
    fill(L, d, GOLD)
    inner = L.circle(64, 64, 38)
    L.over(band(inner, -3, 0), shade(GOLD[2], -0.05))
    L.over(cov(inner + 3), vgrad(L, 26, 102, hexc('#FFE27A'), hexc('#F2A51C')))
    star = L.polygon(star_pts(64, 66, 22, 9.5))
    L.over(cov(star - 2), shade(GOLD[2], -0.1))
    L.over(cov(star), vgrad(L, 44, 88, hexc('#FFF8C8'), hexc('#FFD050')))
    gloss(L, d, 48, 36, 22, 12, 110)
    return L.image()


def gem():
    L = new()
    top = [(26, 46), (44, 24), (84, 24), (102, 46)]
    body = [(26, 46), (102, 46), (64, 110)]
    d = union(L.polygon(top + [(102, 46), (26, 46)][:0]), L.polygon(body))
    d = union(L.polygon([(26, 46), (44, 24), (84, 24), (102, 46)]), L.polygon(body))
    outline(L, d)
    fill(L, d, BLUE, hi=False)
    # Facets.
    L.over(cov(L.polygon([(26, 46), (44, 24), (52, 46)])), hexc('#C6F0FF'))
    L.over(cov(L.polygon([(52, 46), (64, 24), (76, 46)])), hexc('#E8FAFF'))
    L.over(cov(L.polygon([(76, 46), (84, 24), (102, 46)])), hexc('#8FD4FF'))
    L.over(cov(L.polygon([(44, 24), (64, 24), (52, 46)])), hexc('#A6E4FF'))
    L.over(cov(L.polygon([(64, 24), (84, 24), (76, 46)])), hexc('#7CC8FF'))
    L.over(cov(L.polygon([(26, 46), (52, 46), (64, 110)])), hexc('#5DB4FF'))
    L.over(cov(L.polygon([(52, 46), (76, 46), (64, 110)])), hexc('#8AD0FF'))
    L.over(cov(L.polygon([(76, 46), (102, 46), (64, 110)])), hexc('#2E7FE0'))
    for (ax, ay, bx, by) in [(26, 46, 102, 46), (52, 46, 64, 110), (76, 46, 64, 110), (52, 46, 44, 24), (76, 46, 84, 24), (52, 46, 64, 24), (76, 46, 64, 24)]:
        L.over(cov(seg(L, ax, ay, bx, by, 1.0)), hexc('#1E4E9C', 150))
    L.over(cov(L.polygon(star_pts(46, 36, 9, 2.5, 4, 0))), WHITE)
    return L.image()


def heart():
    L = new()
    d = union(L.circle(44, 50, 26), L.circle(84, 50, 26),
              L.polygon([(20, 58), (108, 58), (64, 110)]))
    outline(L, d)
    fill(L, d, RED)
    gloss(L, d, 42, 42, 12, 9, 170)
    L.over(cov(L.circle(58, 38, 3.5)), (255, 255, 255, 200))
    return L.image()


def sword(L, ax, ay, bx, by, blade=SILVER, grip=BROWN, guard=GOLD, scale=1.0):
    """Diagonal sword from the pommel (a) to the tip (b)."""
    ang = math.atan2(by - ay, bx - ax)
    ln = math.hypot(bx - ax, by - ay)
    x, y = rot(L, ax, ay, ang)
    # In rotated space the sword lies along +x from (ax, ay).
    bx0 = ax + ln * 0.36
    blade_d = rrect_at(x, y, bx0, ay - 9 * scale, ax + ln - 14 * scale, ay + 9 * scale, 2)
    # Tip: triangle approximated with the max of two half-planes.
    tx = ax + ln
    k = 9 * scale / (14 * scale)
    tip_d = np.maximum(np.maximum((y - ay) - (tx - x) * k, -(y - ay) - (tx - x) * k), (ax + ln - 14 * scale) - x - 0.5)
    blade_d = np.minimum(blade_d, tip_d)
    guard_d = rrect_at(x, y, ax + ln * 0.3, ay - 22 * scale, ax + ln * 0.38, ay + 22 * scale, 4 * scale)
    grip_d = rrect_at(x, y, ax + 8 * scale, ay - 6 * scale, ax + ln * 0.31, ay + 6 * scale, 3 * scale)
    pommel_d = np.sqrt((x - (ax + 8 * scale)) ** 2 + (y - ay) ** 2) - 9 * scale
    all_d = union(blade_d, guard_d, grip_d, pommel_d)
    outline(L, all_d)
    fill(L, blade_d, blade, hi=False)
    # Central ridge.
    ridge = np.maximum(np.abs(y - ay) - 2 * scale, np.maximum(bx0 + 4 - x, x - (ax + ln - 10 * scale)))
    L.over(cov(ridge), (255, 255, 255, 200))
    L.over(cov(np.maximum(blade_d + 1, (y - ay) - 0)), (120, 130, 160, 70))
    fill(L, grip_d, grip, hi=False)
    fill(L, guard_d, guard)
    fill(L, pommel_d, guard)
    return all_d


def atk():
    L = new()
    sword(L, 24, 104, 108, 20)
    return L.image()


def shield_shape(L, cx, top, w, h):
    """Heater shield: rounded top, pointed bottom."""
    x0, x1 = cx - w / 2, cx + w / 2
    upper = rrect_at(L.x, L.y, x0, top, x1, top + h * 0.55, 14)
    lower = L.polygon([(x0, top + h * 0.4), (x1, top + h * 0.4), (cx + w * 0.18, top + h * 0.86), (cx, top + h), (cx - w * 0.18, top + h * 0.86)])
    return union(upper, lower)


def defence():
    L = new()
    d = shield_shape(L, 64, 14, 92, 102)
    outline(L, d)
    fill(L, d, GOLD)
    inner = shield_shape(L, 64, 24, 72, 80)
    fill(L, inner, BLUE)
    cross = union(rrect_at(L.x, L.y, 58, 36, 70, 86, 4), rrect_at(L.x, L.y, 42, 48, 86, 60, 4))
    L.over(cov(cross + 1), shade(BLUE[2], -0.1))
    L.over(cov(cross), vgrad(L, 36, 86, hexc('#FFFFFF'), hexc('#D8E6FF')))
    gloss(L, inner, 48, 34, 16, 9, 110)
    return L.image()


def spd():
    L = new()
    d = L.polygon([(74, 8), (30, 70), (60, 70), (48, 120), (98, 52), (68, 52), (84, 8)])
    outline(L, d)
    fill(L, d, (hexc('#FFF6B0'), hexc('#FFD23A'), hexc('#E08A10')))
    L.over(cov(L.polygon([(74, 14), (40, 64), (52, 64), (79, 18)])), (255, 255, 255, 150))
    return L.image()


def crit():
    L = new()
    ring = np.abs(L.circle(64, 64, 38)) - 9
    cross = union(rrect_at(L.x, L.y, 58, 6, 70, 40, 4), rrect_at(L.x, L.y, 58, 88, 70, 122, 4),
                  rrect_at(L.x, L.y, 6, 58, 40, 70, 4), rrect_at(L.x, L.y, 88, 58, 122, 70, 4))
    dot = L.circle(64, 64, 11)
    d = union(ring, cross, dot)
    outline(L, d)
    fill(L, union(ring, cross), RED)
    fill(L, dot, (hexc('#FFFFFF'), hexc('#FFE0E0'), hexc('#F08080')))
    return L.image()


def burst():
    L = new()
    pts = []
    n = 9
    for k in range(n * 2):
        r = 56 if k % 2 == 0 else 30
        r += (5 if k % 4 == 0 else 0)
        a = -math.pi / 2 + k * math.pi / n
        pts.append((64 + math.cos(a) * r, 66 + math.sin(a) * r))
    d = L.polygon(pts)
    outline(L, d)
    fill(L, d, ORANGE)
    core = L.polygon(star_pts(64, 66, 30, 17, 7))
    L.over(cov(core), vgrad(L, 36, 96, hexc('#FFF7C0'), hexc('#FFC43A')))
    L.over(cov(L.circle(64, 66, 11)), (255, 255, 255, 230))
    return L.image()


def crown():
    L = new()
    body = L.polygon([(16, 42), (40, 66), (64, 30), (88, 66), (112, 42), (102, 98), (26, 98)])
    base = rrect_at(L.x, L.y, 24, 90, 104, 110, 6)
    d = union(body, base)
    outline(L, d)
    fill(L, body, GOLD)
    fill(L, base, GOLD)
    for (cx, cy, r) in [(16, 40, 9), (64, 28, 10), (112, 40, 9)]:
        b = L.circle(cx, cy, r)
        outline(L, b, 5)
        fill(L, b, GOLD)
    for (cx, cy, ramp) in [(44, 100, RED), (64, 100, BLUE), (84, 100, RED)]:
        g = L.circle(cx, cy, 6.5)
        L.over(cov(g + 2), INK)
        fill(L, g, ramp, hi=False)
        L.over(cov(L.circle(cx - 2, cy - 2, 2)), WHITE)
    return L.image()


def helm():
    """Knight helmet: rounded dome, cheek guards, a T-shaped eye slit and a red plume."""
    L = new()
    plume = union(L.ellipse(64, 20, 14, 16), L.ellipse(78, 26, 14, 9))
    dome = union(L.circle(64, 62, 44), rrect_at(L.x, L.y, 20, 60, 108, 112, 18))
    outline(L, union(dome, plume))
    fill(L, plume, RED)
    fill(L, dome, SILVER)
    ridge = rrect_at(L.x, L.y, 59, 20, 69, 60, 5)
    L.over(cov(ridge), (255, 255, 255, 150))
    slit = union(rrect_at(L.x, L.y, 30, 64, 98, 76, 6), rrect_at(L.x, L.y, 57, 64, 71, 104, 6))
    L.over(cov(slit + 2), shade(SILVER[2], -0.3))
    L.over(cov(slit), hexc('#241B38'))
    for cx in (38, 90):
        L.over(cov(L.circle(cx, 96, 4)), hexc('#9AA4C0'))
    gloss(L, dome, 44, 38, 15, 10, 130)
    return L.image()


def auto():
    """AUTO: two curved arrows chasing each other around a circle."""
    L = new()
    r = np.sqrt((L.x - 64) ** 2 + (L.y - 64) ** 2)
    th = np.arctan2(L.y - 64, L.x - 64)
    ring = np.abs(r - 38) - 9
    gap = (np.abs(np.sin(th)) < 0.32) & (np.cos(th) > 0)
    gap2 = (np.abs(np.sin(th)) < 0.32) & (np.cos(th) < 0)
    arcs = np.where(gap | gap2, 1e3, ring)
    heads = union(L.polygon([(84, 70), (122, 70), (103, 96)]), L.polygon([(44, 58), (6, 58), (25, 32)]))
    d = union(arcs, heads)
    outline(L, d, 5)
    fill(L, d, (hexc('#FFFFFF'), hexc('#E8F8FF'), hexc('#9CC8E8')))
    return L.image()


def gift():
    L = new()
    box = rrect_at(L.x, L.y, 20, 54, 108, 114, 8)
    lid = rrect_at(L.x, L.y, 14, 38, 114, 60, 8)
    bow = union(L.ellipse(48, 30, 18, 12), L.ellipse(80, 30, 18, 12))
    d = union(box, lid, bow)
    outline(L, d)
    fill(L, box, RED)
    fill(L, lid, RED)
    fill(L, bow, GOLD)
    ribbon = union(rrect_at(L.x, L.y, 56, 38, 72, 114, 2))
    L.over(cov(ribbon), vgrad(L, 38, 114, hexc('#FFF0A0'), hexc('#E8A21C')))
    L.over(cov(L.circle(64, 34, 8)), GOLD[1])
    return L.image()


def gate():
    """Dungeon: a stone arch with a dark doorway and a torch glow."""
    L = new()
    arch = union(L.circle(64, 56, 46), rrect_at(L.x, L.y, 18, 56, 110, 116, 6))
    door = union(L.circle(64, 62, 26), rrect_at(L.x, L.y, 38, 62, 90, 116, 2))
    outline(L, arch)
    fill(L, arch, (hexc('#E4DCF0'), hexc('#A79BBE'), hexc('#655A80')))
    for (x0, y0, x1, y1) in [(22, 70, 34, 84), (94, 70, 106, 84), (22, 92, 34, 106), (94, 92, 106, 106)]:
        L.over(cov(rrect_at(L.x, L.y, x0, y0, x1, y1, 3)), (255, 255, 255, 60))
    L.over(cov(door + 2), INK)
    L.over(cov(door), vgrad(L, 36, 116, hexc('#5A3AA0'), hexc('#1A1234')))
    L.over(cov(L.ellipse(64, 92, 16, 22)) * 0.6, (255, 180, 90, 120))
    return L.image()


def paw():
    L = new()
    pad = L.ellipse(64, 84, 30, 25)
    toes = [L.ellipse(30, 54, 11, 14), L.ellipse(52, 34, 11, 14), L.ellipse(76, 34, 11, 14), L.ellipse(98, 54, 11, 14)]
    d = union(pad, *toes)
    outline(L, d)
    for t in [pad] + toes:
        fill(L, t, (hexc('#FFD0E0'), hexc('#FF7FA8'), hexc('#C8406E')))
    return L.image()


def flag():
    """Stage select: a red pennant on a pole over a small hill."""
    L = new()
    pole = rrect_at(L.x, L.y, 30, 12, 40, 112, 4)
    cloth = L.polygon([(38, 16), (110, 36), (38, 62)])
    hill = inter(L.ellipse(64, 124, 60, 22), L.y - 118.0)
    d = union(pole, cloth, hill)
    outline(L, d)
    fill(L, hill, GREEN)
    fill(L, pole, BROWN, hi=False)
    fill(L, cloth, RED)
    L.over(cov(L.circle(35, 12, 7)), GOLD[1])
    return L.image()


def scroll():
    """Credits: a parchment scroll with rolled ends."""
    L = new()
    paper = rrect_at(L.x, L.y, 26, 20, 102, 108, 6)
    top = rrect_at(L.x, L.y, 16, 12, 112, 30, 9)
    bot = rrect_at(L.x, L.y, 16, 98, 112, 116, 9)
    d = union(paper, top, bot)
    outline(L, d)
    fill(L, paper, (hexc('#FFF8E6'), hexc('#F4E2BC'), hexc('#C9A870')))
    for y in (44, 58, 72, 86):
        L.over(cov(seg(L, 40, y, 88 - (y % 3) * 6, y, 2.2)), hexc('#B89870'))
    fill(L, top, BROWN)
    fill(L, bot, BROWN)
    return L.image()


def star():
    L = new()
    d = L.polygon(star_pts(64, 68, 58, 25))
    d = d - 3  # slightly rounded
    outline(L, d, OW - 3)
    fill(L, d, GOLD)
    L.over(cov(L.polygon(star_pts(64, 68, 34, 15)) + 1), (255, 250, 200, 120))
    gloss(L, d, 52, 44, 14, 9, 140)
    return L.image()


def book():
    L = new()
    cover = rrect_at(L.x, L.y, 20, 16, 104, 112, 10)
    pages = rrect_at(L.x, L.y, 28, 98, 106, 116, 4)
    d = union(cover, pages)
    outline(L, d)
    L.over(cov(pages), vgrad(L, 98, 116, hexc('#FFFFFF'), hexc('#E0D8C8')))
    for y in (104, 109):
        L.over(cov(seg(L, 34, y, 102, y, 0.8)), hexc('#B8A890'))
    fill(L, cover, PURPLE)
    spine = rrect_at(L.x, L.y, 20, 16, 34, 112, 6)
    L.over(cov(spine), shade(PURPLE[2], -0.1)[:3] + (220,))
    emblem = L.polygon(star_pts(70, 60, 22, 10))
    L.over(cov(emblem - 3), shade(PURPLE[2], -0.2))
    fill(L, emblem, GOLD)
    for (cx, cy) in [(98, 22), (98, 92)]:
        c = L.polygon([(cx - 10, cy), (cx + 4, cy - 12 if cy < 50 else cy + 12), (cx + 4, cy)])
        L.over(cov(c), GOLD[1])
    return L.image()


def cog():
    L = new()
    teeth = []
    for k in range(8):
        a = k * math.pi / 4
        cx, cy = 64 + math.cos(a) * 44, 64 + math.sin(a) * 44
        x, y = rot(L, cx, cy, a)
        teeth.append(rrect_at(x, y, cx - 12, cy - 11, cx + 12, cy + 11, 4))
    body = L.circle(64, 64, 40)
    d = union(body, *teeth)
    hole = L.circle(64, 64, 16)
    d = np.maximum(d, -hole)
    outline(L, d)
    fill(L, d, SILVER)
    L.over(band(hole, 0, 3), shade(SILVER[2], -0.2))
    return L.image()


def skull():
    L = new()
    head = L.circle(64, 56, 44)
    jaw = rrect_at(L.x, L.y, 40, 76, 88, 112, 12)
    d = union(head, jaw)
    outline(L, d)
    fill(L, d, (hexc('#FFFFFF'), hexc('#ECE6F2'), hexc('#A89CB8')))
    for cx in (46, 82):
        e = L.ellipse(cx, 60, 13, 15)
        L.over(cov(e), hexc('#2A1F3D'))
        L.over(cov(L.circle(cx + 4, 56, 3.5)), (255, 120, 120, 230))
    nose = L.polygon([(64, 70), (58, 84), (70, 84)])
    L.over(cov(nose), hexc('#2A1F3D'))
    for x0 in (50, 62, 74):
        L.over(cov(rrect_at(L.x, L.y, x0 - 3, 92, x0 + 3, 108, 2)), hexc('#2A1F3D', 200))
    gloss(L, head, 44, 30, 16, 9, 140)
    return L.image()


def menu():
    L = new()
    bars = [rrect_at(L.x, L.y, 18, y, 110, y + 18, 9) for y in (22, 55, 88)]
    d = union(*bars)
    outline(L, d)
    for b in bars:
        fill(L, b, (hexc('#FFFFFF'), hexc('#EDEAF6'), hexc('#B6B0CC')))
    return L.image()


def lock():
    L = new()
    shackle = np.abs(inter(L.circle(64, 50, 28), L.y - 58.0)) - 0
    shackle = np.maximum(np.abs(L.circle(64, 50, 26)) - 8, L.y - 62.0)
    bodyd = rrect_at(L.x, L.y, 26, 54, 102, 116, 14)
    d = union(shackle, bodyd)
    outline(L, d)
    fill(L, shackle, SILVER)
    fill(L, bodyd, GOLD)
    hole = union(L.circle(64, 80, 9), rrect_at(L.x, L.y, 60, 80, 68, 100, 3))
    L.over(cov(hole), hexc('#5A3412'))
    return L.image()


def tv():
    L = new()
    box = rrect_at(L.x, L.y, 10, 30, 118, 112, 16)
    ant = union(seg(L, 64, 30, 42, 8, 4), seg(L, 64, 30, 86, 8, 4))
    d = union(box, ant)
    outline(L, d)
    L.over(cov(ant), hexc('#C9D2E6'))
    fill(L, box, BLUE)
    screen = rrect_at(L.x, L.y, 22, 42, 106, 100, 10)
    L.over(cov(screen), vgrad(L, 42, 100, hexc('#FFFFFF'), hexc('#D8ECFF')))
    play = L.polygon([(54, 54), (54, 88), (82, 71)])
    L.over(cov(play), hexc('#2F8BEA'))
    return L.image()


def clock():
    L = new()
    d = L.circle(64, 66, 52)
    outline(L, d)
    fill(L, d, PURPLE)
    face = L.circle(64, 66, 40)
    L.over(cov(face), vgrad(L, 26, 106, hexc('#FFFFFF'), hexc('#E6E0F4')))
    for k in range(12):
        a = k * math.pi / 6
        L.over(cov(seg(L, 64 + math.cos(a) * 32, 66 + math.sin(a) * 32, 64 + math.cos(a) * 36, 66 + math.sin(a) * 36, 2.2)), hexc('#6A5A8A'))
    L.over(cov(seg(L, 64, 66, 64, 38, 4)), INK)
    L.over(cov(seg(L, 64, 66, 84, 74, 4)), INK)
    L.over(cov(L.circle(64, 66, 6)), hexc('#F04848'))
    return L.image()


def cry():
    L = new()
    bell = L.polygon([(34, 46), (96, 18), (96, 110), (34, 82)])
    mouth = rrect_at(L.x, L.y, 16, 44, 40, 84, 8)
    d = union(bell, mouth)
    outline(L, d)
    fill(L, bell, GOLD)
    fill(L, mouth, ORANGE)
    rim = rrect_at(L.x, L.y, 90, 14, 104, 114, 6)
    outline(L, rim, 4)
    fill(L, rim, GOLD)
    for k, r in enumerate((14, 24)):
        arc = np.maximum(np.abs(L.circle(104, 64, r + 10)) - 2.5, 104 - L.x)
        L.over(cov(arc + 0), (255, 240, 180, 230 - k * 60))
    return L.image()


def strike():
    L = new()
    x, y = rot(L, 64, 64, -math.pi / 4)
    head = rrect_at(x, y, 30, 22, 98, 54, 8)
    handle = rrect_at(x, y, 58, 50, 70, 116, 5)
    d = union(head, handle)
    outline(L, d)
    fill(L, handle, BROWN, hi=False)
    fill(L, head, SILVER)
    for k in range(3):
        a = math.pi * (0.95 + 0.18 * k)
        L.over(cov(seg(L, 28 + math.cos(a) * 6, 98 + math.sin(a) * 6, 28 + math.cos(a) * 22, 98 + math.sin(a) * 22, 3)), ORANGE[1])
    return L.image()


def whirl():
    L = new()
    th = np.arctan2(L.y - 64, L.x - 64)
    r = np.sqrt((L.x - 64) ** 2 + (L.y - 64) ** 2)
    # Archimedean spiral distance: compare r with the spiral radius for the nearest turn.
    best = np.full(r.shape, 1e9, np.float32)
    for turn in range(-1, 4):
        sr = 8 + 13 * ((th + math.pi) / (2 * math.pi) + turn)
        best = np.minimum(best, np.abs(r - sr))
    d = np.where(r < 56, best - 5.5, 1e3)
    d = np.maximum(d, r - 54)
    outline(L, d, 5)
    L.over(cov(d), radial(L, 64, 64, 56, hexc('#E8FBFF'), hexc('#3FB0F0')))
    return L.image()


def basic():
    L = new()
    r = np.sqrt((L.x - 40) ** 2 + (L.y - 88) ** 2)
    th = np.arctan2(L.y - 88, L.x - 40)
    t = np.clip((th + math.pi * 0.62) / (math.pi * 0.62), 0, 1)
    width = 4 + 13 * np.sin(np.clip(t, 0, 1) * math.pi) ** 0.8
    arc = np.where((th < 0) & (th > -math.pi * 0.62), np.abs(r - 66) - width, 1e3)
    outline(L, arc, 5)
    L.over(cov(arc), vgrad(L, 20, 100, hexc('#FFFFFF'), hexc('#8ED8FF')))
    L.over(cov(np.where((th < 0) & (th > -math.pi * 0.62), np.abs(r - 70) - width * 0.35, 1e3)), (255, 255, 255, 220))
    for (cx, cy, s) in [(98, 30, 7), (86, 18, 4)]:
        L.over(cov(L.polygon(star_pts(cx, cy, s, s * 0.35, 4, 0))), WHITE)
    return L.image()


def portal():
    """Summon tab: a glowing swirl gate (purple)."""
    L = new()
    d = L.circle(64, 64, 54)
    outline(L, d)
    L.over(cov(d), radial(L, 64, 64, 54, hexc('#FFF4FF'), hexc('#7A3FE0')))
    th = np.arctan2(L.y - 64, L.x - 64)
    r = np.sqrt((L.x - 64) ** 2 + (L.y - 64) ** 2)
    arms = np.cos(3 * th + r * 0.12)
    L.over(np.clip(arms * 2 - 0.6, 0, 1) * np.clip(1 - r / 52, 0, 1) * cov(d), (255, 255, 255, 150))
    L.over(cov(L.polygon(star_pts(64, 64, 18, 7, 4, 0))), WHITE)
    L.over(band(d, -4, -1) * (L.y < 64), (255, 255, 255, 170))
    return L.image()


def next_arrow():
    """Stage select chevron (pointing right; the builder mirrors it for 'previous')."""
    L = new()
    d = L.polygon([(36, 18), (104, 64), (36, 110), (36, 82), (64, 64), (36, 46)])
    d = d - 4
    outline(L, d, 5)
    fill(L, d, (hexc('#FFFFFF'), hexc('#EEF4FF'), hexc('#A8B8D8')))
    return L.image()


def plus():
    """Empty-slot hint: a rounded white plus."""
    L = new()
    d = union(rrect_at(L.x, L.y, 52, 18, 76, 110, 12), rrect_at(L.x, L.y, 18, 52, 110, 76, 12))
    outline(L, d, 5)
    fill(L, d, (hexc('#FFFFFF'), hexc('#F0EEF8'), hexc('#B8B2CC')))
    return L.image()


def hand():
    """D-111 tutorial pointer: a white glove pointing up-left with its index finger."""
    L = new()
    palm = L.ellipse(76, 82, 26, 24)
    finger = seg(L, 66, 70, 30, 28, 10)
    knuckles = union(seg(L, 80, 64, 90, 52, 9.5), seg(L, 92, 70, 102, 60, 9), seg(L, 98, 82, 108, 74, 8.5))
    thumb = seg(L, 56, 88, 42, 74, 9)
    cuff = rrect_at(L.x, L.y, 70, 100, 112, 122, 7)
    glove = union(palm, finger, knuckles, thumb)
    d = union(glove, cuff)
    outline(L, d)
    fill(L, glove, (hexc('#FFFFFF'), hexc('#F2F2FA'), hexc('#B8BCD0')))
    for a, b in (((70, 70), (84, 58)), ((84, 76), (96, 66))):
        L.over(cov(seg(L, a[0], a[1], b[0], b[1], 1.4)), hexc('#B8BCD0'))
    fill(L, cuff, GOLD, hi=False)
    L.over(cov(L.circle(34, 30, 4)), (255, 255, 255, 230))
    return L.image()


def quest():
    """D-111 guide quest: a parchment scroll with a gold exclamation badge."""
    L = new()
    paper = rrect_at(L.x, L.y, 22, 26, 92, 110, 6)
    top = rrect_at(L.x, L.y, 12, 18, 102, 36, 9)
    bot = rrect_at(L.x, L.y, 12, 100, 102, 118, 9)
    d = union(paper, top, bot)
    outline(L, d)
    fill(L, paper, (hexc('#FFF8E6'), hexc('#F4E2BC'), hexc('#C9A870')))
    for y in (50, 64, 78, 90):
        L.over(cov(seg(L, 34, y, 74 - (y % 3) * 5, y, 2.2)), hexc('#B89870'))
    fill(L, top, BROWN)
    fill(L, bot, BROWN)
    badge = L.circle(96, 32, 22)
    outline(L, badge, 5)
    fill(L, badge, GOLD)
    L.over(cov(rrect_at(L.x, L.y, 92, 18, 100, 38, 3)), hexc('#7A3A10'))
    L.over(cov(L.circle(96, 45, 4)), hexc('#7A3A10'))
    return L.image()


def make():
    save(coin(), 'coin')
    save(gem(), 'gem')
    save(heart(), 'heart')
    save(atk(), 'atk')
    save(defence(), 'def')
    save(spd(), 'spd')
    save(crit(), 'crit')
    save(burst(), 'burst')
    save(crown(), 'crown')
    save(helm(), 'helm')
    save(star(), 'star')
    save(book(), 'book')
    save(cog(), 'cog')
    save(skull(), 'skull')
    save(menu(), 'menu')
    save(lock(), 'lock')
    save(tv(), 'tv')
    save(clock(), 'clock')
    save(cry(), 'cry')
    save(strike(), 'strike')
    save(whirl(), 'whirl')
    save(basic(), 'basic')
    save(portal(), 'portal')
    save(auto(), 'auto')
    save(gift(), 'gift')
    save(gate(), 'gate')
    save(paw(), 'paw')
    save(flag(), 'flag')
    save(scroll(), 'scroll')
    save(plus(), 'plus')
    save(next_arrow(), 'next')
    save(hand(), 'hand')
    save(quest(), 'quest')


if __name__ == '__main__':
    ONLY = [a for i, a in enumerate(sys.argv[1:], 1)
            if not a.startswith('--') and sys.argv[i - 1] != '--preview' and '/' not in a and os.sep not in a]
    if ONLY:
        _save = save
        def save(img, name):  # noqa: F811 - restrict the run to the named icons
            if name in ONLY:
                _save(img, name)
    make()
    if PREVIEW:
        cols = 8
        cell = S + 24
        rows = (len(made) + cols - 1) // cols
        sheet = Image.new('RGBA', (cols * cell * 2, rows * cell), (238, 226, 200, 255))
        dark = Image.new('RGBA', (cols * cell, rows * cell), (40, 44, 90, 255))
        sheet.alpha_composite(dark, (cols * cell, 0))
        for k, (_, im) in enumerate(made):
            x, y = (k % cols) * cell + 12, (k // cols) * cell + 12
            sheet.alpha_composite(im, (x, y))
            small = im.resize((48, 48), Image.LANCZOS)
            sheet.alpha_composite(small, (cols * cell + x + 40, y + 40))
        sheet.save(PREVIEW)
    print('made', len(made), 'icons in', os.path.abspath(OUT))
