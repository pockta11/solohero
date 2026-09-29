"""SoloHero enemies and bosses (D-092) in the hero's cute style (D-090).

Same rules as tools/art/hero.py: a big round head over a small body, one dark outline colour, two-tone cel
shading with a small highlight, big sparkling eyes. Each look picks a head (goblin ears, skull, mushroom cap,
straw hat, hood, wizard hat), a body (tunic, bones, robe), a weapon (club, sword, katana, staff, bow) and colours;
the flying eye has its own builder. Characters are drawn facing right and mirrored to face the hero. Feet sit on
the frame's bottom centre (the sprite pivot). Bosses are drawn at 1x with a larger head instead of a 2x scale.

Run: python tools/art/cast.py [--preview out.png]
Replaces every sheet of these entities under Assets/SoloHero/Art/{Enemies,Bosses}.
"""
import glob
import math
import os
import sys

from PIL import Image, ImageOps

sys.path.insert(0, os.path.dirname(__file__))
from charkit import Canvas, capsule, clean_orphans, ellipse, hexc, polygon, ramp, sheet, superbox, _norm
from hero import (OUT, CEL, EYE, EYE_HI, EYE_LOW, BLUSH, MOUTH, Xf, part, rad, union_shapes, fill_ellipse, set_px,
                  draw_smear, flash)

ART = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Art")
DATA = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Data", "Art")

# ---------------------------------------------------------------- palette

SKIN = ramp("#f2a58a", "#ffd6ba", "#fff1e4")
GOBLIN = ramp("#4f9a3c", "#7cc850", "#bdf08a")
GOBLIN_R = ramp("#b2463c", "#e8705a", "#ffb59a")
BONE = ramp("#b8aa98", "#eee6d6", "#ffffff")
BONE_V = ramp("#8a78b0", "#c6b6e6", "#f0e8ff")
STEM = ramp("#d8b890", "#f6e2c2", "#fff6e8")
CAP_R = ramp("#c2343e", "#ff5a5a", "#ff9a8a")
CAP_B = ramp("#3a5ac8", "#5a8cff", "#a8c8ff")
SHADOW_FACE = ramp("#1e1430", "#2e2046", "#46345e")
LEATHER = ramp("#6b3a28", "#8f5236", "#b8744a")
WOOD = ramp("#7a4a2a", "#a86a3c", "#d69858")
STEEL = ramp("#8aa6cf", "#e2eeff", "#ffffff")
GOLD = ramp("#d68a1c", "#ffc434", "#fff2a4")
BLACK = ramp("#241a30", "#3a2e4a", "#5a4a6a")
RED = ramp("#b8303c", "#ee4a52", "#ff8e86")
STRAW = ramp("#b88a3a", "#e6c068", "#fff0a8")
NAVY = ramp("#2a2e5a", "#3e4a86", "#6a7cc0")
GREEN_CLOTH = ramp("#2e6a3a", "#469a4e", "#7cc878")
PURPLE = ramp("#4a2a78", "#7040b0", "#a878e0")
DARK_PURPLE = ramp("#2a1a44", "#43306a", "#6a50a0")
ORANGE = ramp("#c85a1c", "#ff8a2a", "#ffc070")
BROWN = ramp("#6a4428", "#8e6038", "#b8844e")
GREY = ramp("#4a4a5e", "#6c6c86", "#9a9ab4")
ORB_GREEN = ramp("#2a9a5a", "#5aff9a", "#dcffe8")
ORB_PURPLE = ramp("#7a2ac8", "#c070ff", "#f4dcff")
ORB_FIRE = ramp("#e04a1c", "#ffa030", "#fff0a0")
EYE_WHITE = ramp("#c8c0d8", "#ffffff", "#ffffff")
WING = ramp("#3a2a5a", "#5a4488", "#8a70c0")
WING_R = ramp("#6a2030", "#a83a48", "#e06a70")
IRIS_B = hexc("#3a6ae0")
IRIS_R = hexc("#e03a3a")
GLOW_R = hexc("#ff5a4a")
GLOW_Y = hexc("#ffe066")
GLOW_V = hexc("#e070ff")
GLOW_G = hexc("#6affa0")

ENEMY_FRAME = (80, 64)
BOSS_FRAME = (128, 112)

# ---------------------------------------------------------------- looks

LOOKS = {
    # Enemies (R = head radius in art pixels).
    "goblin": dict(R=10.0, skin=GOBLIN, head="goblin", body="tunic", cloth=BROWN, legs=LEATHER, weapon="club", eyes="cute"),
    "goblinr": dict(R=10.0, skin=GOBLIN_R, head="goblin", body="tunic", cloth=BLACK, legs=LEATHER, weapon="club", eyes="cute"),
    "skeleton": dict(R=10.0, skin=BONE, head="skull", body="bones", cloth=BONE, legs=BONE, weapon="sword", eyes="socket", glow=GLOW_Y),
    "skeletonv": dict(R=10.0, skin=BONE_V, head="skull", body="bones", cloth=BONE_V, legs=BONE_V, weapon="sword", eyes="socket", glow=GLOW_V),
    "mushroom": dict(R=8.5, skin=STEM, head="mushroom", cap=CAP_R, body="tunic", cloth=STEM, legs=STEM, weapon="none", eyes="cute"),
    "mushroomb": dict(R=8.5, skin=STEM, head="mushroom", cap=CAP_B, body="tunic", cloth=STEM, legs=STEM, weapon="none", eyes="cute"),
    "flyeye": dict(R=9.0, kind="flyeye", iris=IRIS_B, wing=WING),
    "flyeyer": dict(R=9.0, kind="flyeye", iris=IRIS_R, wing=WING_R),
    # Bosses.
    "ronin": dict(R=16.0, skin=SKIN, head="straw_hat", body="tunic", cloth=NAVY, legs=BLACK, weapon="katana", eyes="cute", brows=True, boss=True),
    "necro": dict(R=16.0, skin=BONE, head="hood", hood=BLACK, body="robe", cloth=BLACK, legs=BLACK, weapon="staff", orb=ORB_GREEN, eyes="socket", glow=GLOW_G, boss=True),
    "ranger": dict(R=16.0, skin=SKIN, head="hood", hood=GREEN_CLOTH, body="tunic", cloth=GREEN_CLOTH, legs=BROWN, weapon="bow", eyes="cute", brows=True, boss=True),
    "shadowmage": dict(R=16.0, skin=SHADOW_FACE, head="wizard", hat=DARK_PURPLE, body="robe", cloth=DARK_PURPLE, legs=DARK_PURPLE, weapon="staff", orb=ORB_PURPLE, eyes="glow", glow=GLOW_V, boss=True),
    "firemage": dict(R=16.0, skin=SKIN, head="wizard", hat=RED, body="robe", cloth=RED, legs=RED, weapon="staff", orb=ORB_FIRE, eyes="cute", boss=True),
}


def pose(**kw):
    base = dict(bx=0.0, by=0.0, lean=0.0, squash=0.0, fa=(-60.0, -20.0), ba=(-110.0, -90.0), fl=(-90.0, -90.0),
                bl=(-90.0, -90.0), weapon=40.0, eyes="open", mouth="smile", flash=False, smear=None, fall=0.0,
                glow=0.0, wing=0.0, draw=0.0)
    base.update(kw)
    return base


# ---------------------------------------------------------------- humanoid builder

def build_humanoid(look, p, frame):
    W, H = frame
    R = look["R"]
    k = R / 13.0  # body scale relative to the hero
    cv = Canvas(W, H, W // 2)
    fall_t = p["fall"] / 90.0
    # Falling backward pivots on the feet; lift the body so the head lands on the ground, not under it.
    xf = Xf(rad(p["fall"]), 0.0, 0.0, fall_t * (R + 4.0 * k), fall_t * (R * 0.95 + 2.0))
    T = xf.p
    lean = rad(p["lean"])
    squash = p["squash"]
    hip = (p["bx"] * k, 7.0 * k + p["by"])

    def up(d):
        return hip[0] + math.sin(-lean) * d, hip[1] + math.cos(lean) * d

    body_c = up(5.0 * k - squash * 0.5)
    hc = up(9.5 * k - squash + R - 1.5 * k)
    hc = (hc[0] + 1.0 * k, hc[1])
    sh_front = (body_c[0] + 3.2 * k, body_c[1] + 2.5 * k)
    sh_back = (body_c[0] - 3.4 * k, body_c[1] + 2.8 * k)

    def seg(x, y, angs, l1, l2):
        a1, a2 = rad(angs[0]), rad(angs[1])
        j = (x + math.cos(a1) * l1, y + math.sin(a1) * l1)
        return j, (j[0] + math.cos(a2) * l2, j[1] + math.sin(a2) * l2)

    fk, ff = seg(hip[0] + 2.4 * k, hip[1], p["fl"], 3.4 * k, 3.2 * k)
    bk, bf = seg(hip[0] - 2.4 * k, hip[1], p["bl"], 3.4 * k, 3.2 * k)
    fe, fh = seg(*sh_front, p["fa"], 3.2 * k, 3.0 * k)
    be, bh = seg(*sh_back, p["ba"], 3.2 * k, 3.0 * k)

    def cap(a, b, r1, r2=None):
        A, B = T(*a), T(*b)
        return capsule(A[0], A[1], B[0], B[1], r1, r2)

    def ell(c, rx, ry, r=0.0, bulge=1.0):
        C = T(*c)
        return ellipse(C[0], C[1], rx, ry, r + xf.a, bulge)

    def poly(points, **kw):
        return polygon([T(*q) for q in points], **kw)

    skin, cloth, legs = look["skin"], look["cloth"], look["legs"]
    thin = 0.7 if look["body"] == "bones" else 1.0
    tilt = -lean * 0.4 + xf.a
    HC = T(*hc)

    def to_world(lx, ly):
        c, s = math.cos(tilt), math.sin(tilt)
        return HC[0] + lx * c - ly * s, HC[1] + lx * s + ly * c

    def local(x, y):
        dx, dy = x - HC[0], y - HC[1]
        c, s = math.cos(-tilt), math.sin(-tilt)
        return dx * c - dy * s, dx * s + dy * c

    # Behind everything: the hood's back, the wizard hat's brim back half, the ronin's hair bun.
    if look["head"] == "hood":
        cv.paint(part("hood_back", ellipse(HC[0], HC[1] + 0.5, R + 2.2, R + 1.4, tilt, 1.6), look["hood"]))
    if look["head"] == "straw_hat":
        bun = to_world(-R * 0.55, R * 0.7)
        cv.paint(part("bun", ellipse(bun[0], bun[1], R * 0.32, R * 0.3, tilt), BLACK))

    # Back arm and back leg.
    cv.paint(part("barm", cap(sh_back, be, 2.2 * k * thin, 2.0 * k * thin), cloth if look["body"] != "bones" else skin))
    cv.paint(part("barm2", cap(be, bh, 2.0 * k * thin), cloth if look["body"] == "robe" else skin))
    cv.paint(part("bhand", ell(bh, 2.4 * k, 2.4 * k), skin))
    if look["weapon"] == "bow":
        draw_bow(cv, T, bh, p, k)
    if look["body"] != "robe":
        cv.paint(part("bleg", cap((hip[0] - 2.4 * k, hip[1]), bk, 2.6 * k * thin, 2.4 * k * thin), legs))
        cv.paint(part("bleg2", cap(bk, bf, 2.4 * k * thin), legs))
    cv.paint(part("bboot", ell((bf[0] + 1.0 * k, bf[1] + 1.4 * k), 3.2 * k, 2.2 * k), BLACK if look["body"] == "robe" else legs if look["body"] == "bones" else LEATHER))

    # Body.
    if look["body"] == "robe":
        skirt = [(body_c[0] - 5.0 * k, body_c[1] + 1.0 * k), (body_c[0] + 5.0 * k, body_c[1] + 1.0 * k),
                 (hip[0] + 7.5 * k, 0.8), (hip[0] - 7.5 * k, 0.8)]
        cv.paint(part("skirt", poly(skirt, curve=lambda x, y: _norm(math.sin(x * 0.7) * 0.5 - 0.2, 0.3, 0.9)), cloth))
    cv.paint(part("body", ell(body_c, 6.4 * k, 5.8 * k - squash * 0.4, -lean, 1.4), cloth))
    ribs = []
    if look["body"] == "bones":
        for i in range(3):
            y = body_c[1] + (1.6 - i * 1.6) * k
            ribs.append((body_c[0] - 3.5 * k, y, body_c[0] + 3.5 * k, y))
    elif look["body"] == "tunic" and look["weapon"] != "none":
        belt = superbox(*T(hip[0] + math.sin(-lean) * 2.0 * k, hip[1] + 2.0 * k), 6.0 * k, 1.1 * k, 6.0, -lean + xf.a)
        cv.paint(part("belt", belt, GOLD if look.get("boss") else LEATHER))

    # Front leg.
    if look["body"] != "robe":
        cv.paint(part("fleg", cap((hip[0] + 2.4 * k, hip[1]), fk, 2.7 * k * thin, 2.5 * k * thin), legs))
        cv.paint(part("fleg2", cap(fk, ff, 2.5 * k * thin), legs))
    cv.paint(part("fboot", ell((ff[0] + 1.2 * k, ff[1] + 1.4 * k), 3.4 * k, 2.3 * k), BLACK if look["body"] == "robe" else legs if look["body"] == "bones" else LEATHER))

    # Head.
    head = look["head"]
    if head == "goblin":
        for side in (-1, 1):
            base = to_world(side * R * 0.75, R * 0.15)
            tip = to_world(side * R * 1.55, R * 0.55)
            low = to_world(side * R * 0.7, -R * 0.2)
            cv.paint(part("ear", polygon([base, tip, low]), skin))
    face_ramp = skin
    thresholds = (0.16, 0.97)
    cv.paint(Part_face(HC, R, tilt, face_ramp, thresholds))
    if head == "goblin":
        tuft = to_world(0.0, R + 2.0)
        root = to_world(-R * 0.1, R - 0.5)
        cv.paint(part("tuft", capsule(root[0], root[1], tuft[0], tuft[1], 1.4 * k, 0.8), BLACK))
    elif head == "mushroom":
        cap_c = to_world(0.0, R * 0.55)

        def cap_shape(x, y):
            lx, ly = local(x, y)
            u, v = lx / (R * 1.45), (ly - R * 0.35) / (R * 0.95)
            if u * u + v * v > 1.0 or ly < R * 0.05 + abs(lx) * 0.08:
                return None
            return _norm(u, v, math.sqrt(max(0.0, 1.0 - u * u - v * v)) * 1.5)

        cv.paint(part("cap", cap_shape, look["cap"]))
        for sx, sy, sr in ((-0.55, 0.75, 0.22), (0.35, 0.95, 0.2), (0.85, 0.45, 0.16), (-0.1, 1.15, 0.14)):
            c = to_world(sx * R, sy * R)
            cv.paint(part("spot", ellipse(c[0], c[1], sr * R, sr * R * 0.8, tilt), STEM, flat=hexc("#fff6e8")))
    elif head == "straw_hat":
        brim = [to_world(-R * 1.6, R * 0.35), to_world(R * 1.6, R * 0.35), to_world(R * 1.3, R * 0.6), to_world(0.0, R * 1.25),
                to_world(-R * 1.3, R * 0.6)]
        cv.paint(part("hat", polygon(brim, curve=lambda x, y: _norm(-0.3, 0.8, 0.5)), STRAW))
        band = [to_world(-R * 0.95, R * 0.72), to_world(R * 0.95, R * 0.72), to_world(R * 0.8, R * 0.86), to_world(-R * 0.8, R * 0.86)]
        cv.paint(part("band", polygon(band), RED))
    elif head == "hood":
        def hood_front(x, y):
            lx, ly = local(x, y)
            u, v = (lx + 0.5) / (R + 2.0), (ly - 0.5) / (R + 1.2)
            if u * u + v * v > 1.0:
                return None
            if ly < R * 0.3 and lx > -R * 0.55:
                return None
            return _norm(u, v, math.sqrt(max(0.0, 1.0 - u * u - v * v)) * 1.6)

        cv.paint(part("hood", hood_front, look["hood"]))
    elif head == "wizard":
        brim = to_world(0.0, R * 0.45)
        cv.paint(part("brim", ellipse(brim[0], brim[1], R * 1.55, R * 0.32, tilt, 1.2), look["hat"]))
        cone = [to_world(-R * 0.95, R * 0.5), to_world(R * 0.95, R * 0.5), to_world(R * 0.4, R * 1.3),
                to_world(-R * 0.35, R * 2.05), to_world(-R * 0.75, R * 1.75), to_world(-R * 0.45, R * 1.25)]
        cv.paint(part("cone", polygon(cone, curve=lambda x, y: _norm(-0.4, 0.5, 0.8)), look["hat"]))
        star = to_world(R * 0.1, R * 1.0)
        cv.paint(part("hatstar", ellipse(star[0], star[1], R * 0.14, R * 0.14, tilt), GOLD, flat=GOLD[1]))

    # Front arm with the weapon.
    weapon = look["weapon"]
    if weapon != "bow":
        draw_weapon(cv, T, weapon, fh, p, k, look)
    cv.paint(part("farm", cap(sh_front, fe, 2.3 * k * thin, 2.1 * k * thin), cloth if look["body"] != "bones" else skin))
    cv.paint(part("farm2", cap(fe, fh, 2.1 * k * thin), cloth if look["body"] == "robe" else skin))
    cv.paint(part("fhand", ell(fh, 2.6 * k, 2.6 * k), skin))
    if weapon == "staff":
        draw_orb(cv, T, fh, p, k, look)

    cv.finish(OUT, rim=False)
    cv.clean()
    for rib in ribs:
        set_line(cv, T, *rib)
    draw_monster_face(cv, to_world, p, look)
    img = cv.image()
    if p["flash"]:
        img = flash(img)
    return img


def Part_face(HC, R, tilt, face_ramp, thresholds):
    return part("face", ellipse(HC[0], HC[1], R, R - 1.0, tilt, 3.2), face_ramp, thresholds=thresholds)


def set_line(cv, T, x0, y0, x1, y1):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / max(1, n)
        X, Y = T(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t)
        set_px(cv, X, Y, OUT)


def draw_weapon(cv, T, weapon, hand, p, k, look):
    sa = rad(p["weapon"])
    dx, dy = math.cos(sa), math.sin(sa)
    nx, ny = -dy, dx
    hx, hy = hand

    def cap(a, b, r1, r2=None):
        A, B = T(*a), T(*b)
        return capsule(A[0], A[1], B[0], B[1], r1, r2)

    if p["smear"] is not None:
        draw_smear(cv, T, hx, hy, p["smear"])
    if weapon == "club":
        end = (hx + dx * 11.0 * k, hy + dy * 11.0 * k)
        cv.paint(part("club", cap((hx - dx * 2.0 * k, hy - dy * 2.0 * k), end, 1.3 * k, 3.0 * k), WOOD))
    elif weapon in ("sword", "katana"):
        length = (17.0 if weapon == "sword" else 22.0) * k
        half = (2.0 if weapon == "sword" else 1.3) * k
        guard = (hx + dx * 2.2 * k, hy + dy * 2.2 * k)
        tip = (hx + dx * length, hy + dy * length)
        blade = [(guard[0] + nx * half, guard[1] + ny * half), (tip[0] - dx * 3.0 * k + nx * half, tip[1] - dy * 3.0 * k + ny * half),
                 tip, (tip[0] - dx * 3.0 * k - nx * half, tip[1] - dy * 3.0 * k - ny * half),
                 (guard[0] - nx * half, guard[1] - ny * half)]
        G = T(*guard)

        def bn(x, y):
            side = (x - G[0]) * nx + (y - G[1]) * ny
            return _norm(-0.7, 0.7, 0.4) if side > 0.2 else _norm(0.3, -0.2, 1.0)

        cv.paint(part("blade", polygon([T(*q) for q in blade], curve=bn), STEEL))
        cv.paint(part("grip", cap((hx - dx * 3.0 * k, hy - dy * 3.0 * k), guard, 1.2 * k), BLACK if weapon == "katana" else LEATHER))
        if weapon == "katana":
            g = T(*guard)
            cv.paint(part("tsuba", ellipse(g[0], g[1], 1.8 * k, 1.8 * k), GOLD))
        else:
            a = (guard[0] + nx * 3.2 * k, guard[1] + ny * 3.2 * k)
            b = (guard[0] - nx * 3.2 * k, guard[1] - ny * 3.2 * k)
            cv.paint(part("guard", cap(a, b, 1.3 * k), GREY))
    elif weapon == "staff":
        top = (hx + dx * 13.0 * k, hy + dy * 13.0 * k)
        bottom = (hx - dx * 9.0 * k, hy - dy * 9.0 * k)
        cv.paint(part("staff", cap(bottom, top, 1.2 * k), WOOD))


def draw_orb(cv, T, hand, p, k, look):
    sa = rad(p["weapon"])
    top = (hand[0] + math.cos(sa) * 14.5 * k, hand[1] + math.sin(sa) * 14.5 * k)
    C = T(*top)
    r = (2.8 + p["glow"] * 1.2) * k
    cv.paint(part("orb", ellipse(C[0], C[1], r, r, 0.0, 1.2), look["orb"]))


def draw_bow(cv, T, hand, p, k):
    hx, hy = hand
    draw = p["draw"]
    pts_out, pts_in = [], []
    for i in range(13):
        a = rad(-70 + i * (140 / 12))
        pts_out.append((hx + math.cos(a) * 9.0 * k, hy + math.sin(a) * 9.0 * k))
        pts_in.append((hx + math.cos(a) * 7.2 * k, hy + math.sin(a) * 7.2 * k))
    cv.paint(part("bow", polygon([T(*q) for q in pts_out + pts_in[::-1]], curve=lambda x, y: _norm(-0.3, 0.6, 0.8)), WOOD))
    top, bottom = pts_in[-1], pts_in[0]
    pull = (hx - (2.0 + draw * 5.0) * k, hy)
    for a, b in ((top, pull), (pull, bottom)):
        n = 24
        for i in range(n + 1):
            t = i / n
            X, Y = T(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
            set_px_any(cv, X, Y, hexc("#f4ecd8"))
    if draw > 0.2:
        n = int(14 * k)
        for i in range(n):
            X, Y = T(pull[0] + i, pull[1])
            set_px_any(cv, X, Y, hexc("#d8c8a8") if i < n - 3 else STEEL[1])


def set_px_any(cv, x, y, color):
    """Like set_px but also paints transparent pixels (thin bow strings and arrows over the background)."""
    px, py = cv.to_px(math.floor(x), math.floor(y))
    px, py = int(px), int(py)
    if 0 <= px < cv.w and 0 <= py < cv.h:
        cv.col[py][px] = color
        if cv.id[py][px] < 0:
            cv.id[py][px] = 20_000


def draw_monster_face(cv, to_world, p, look):
    R = look["R"]
    face = set(look["skin"])
    eyes = p["eyes"]
    style = look["eyes"]
    ry = 3.4 * R / 13.0 + 0.4
    for ex, rx in ((R * 0.36, 2.1 * R / 13.0 + 0.3), (-R * 0.2, 1.8 * R / 13.0 + 0.3)):
        cx, cy = to_world(ex, -R * 0.26)
        if eyes == "closed" or eyes == "dead":
            marks = ((-1.2, 1.2), (0.0, 0.0), (-1.2, -1.2), (1.2, 1.2), (1.2, -1.2)) if eyes == "dead" else \
                ((-1.5, 0.0), (-0.5, 0.8), (0.5, 0.8), (1.5, 0.0))
            for dx, dy in marks:
                set_px(cv, cx + dx, cy + dy, EYE)
            continue
        if eyes == "hurt":
            for dx, dy in ((-1.2, 1.2), (0.0, 0.0), (-1.2, -1.2)):
                set_px(cv, cx + dx, cy + dy, EYE)
            continue
        if style == "socket":
            fill_ellipse(cv, cx, cy, rx + 0.4, ry, EYE, face)
            set_px(cv, cx, cy, look["glow"])
            set_px(cv, cx - 0.9, cy, look["glow"])
        elif style == "glow":
            fill_ellipse(cv, cx, cy, rx, ry * 0.55, look["glow"], face)
        else:
            fill_ellipse(cv, cx, cy, rx, ry, EYE, face)
            fill_ellipse(cv, cx + 0.2, cy - ry * 0.47, rx * 0.7, ry * 0.3, EYE_LOW, {EYE})
            set_px(cv, cx - 0.8, cy + 1.2, EYE_HI)
            set_px(cv, cx + 0.2, cy + 1.2, EYE_HI)
            set_px(cv, cx - 0.8, cy + 0.2, EYE_HI)
            set_px(cv, cx + 0.2, cy + 0.2, EYE_HI)
            set_px(cv, cx + 1.0, cy - ry * 0.4, EYE_HI)
        if look.get("brows"):
            for dx, dy in ((-1.5, ry + 1.6), (-0.5, ry + 1.2), (0.5, ry + 0.9), (1.5, ry + 0.6)):
                set_px(cv, cx + dx, cy + dy, EYE)
    if style == "cute" and look["skin"] is not SHADOW_FACE:
        for ex in (R * 0.66, -R * 0.44):
            bx, by = to_world(ex, -R * 0.52)
            fill_ellipse(cv, bx, by, 1.8 * R / 13.0 + 0.2, 0.9, BLUSH, face)
    mx, my = to_world(R * 0.1, -R * 0.6)
    if style == "socket":
        for dx in (-1.5, -0.5, 0.5, 1.5):
            set_px(cv, mx + dx, my, EYE)
            set_px(cv, mx + dx, my - 1.0, EYE if dx in (-1.5, 1.5) else look["skin"][1])
    elif style == "glow":
        return
    elif p["mouth"] == "open":
        fill_ellipse(cv, mx, my - 0.3, 1.3, 1.3, MOUTH, face)
    elif p["mouth"] == "grit":
        for dx in (-1.0, 0.0, 1.0):
            set_px(cv, mx + dx, my, EYE)
    else:
        for dx, dy in ((-1.0, 0.0), (0.0, -0.9), (1.0, 0.0)):
            set_px(cv, mx + dx, my + dy, EYE)
    if look["head"] == "goblin" and style == "cute":
        set_px(cv, mx + 1.2, my + 0.1, hexc("#ffffff"))


# ---------------------------------------------------------------- flying eye

def build_flyeye(look, p, frame):
    W, H = frame
    R = look["R"]
    cv = Canvas(W, H, W // 2)
    fall_t = p["fall"] / 90.0
    cy = max(R + 1.5, 22.0 + p["by"] - fall_t * 18.0)
    cx = p["bx"]
    flap = p["wing"]
    for side in (-1, 1):
        root = (cx + side * R * 0.6, cy + R * 0.2)
        tip = (cx + side * (R * 1.9), cy + R * (0.9 + flap * 0.8))
        mid = (cx + side * (R * 1.6), cy - R * (0.1 + flap * 0.3))
        low = (cx + side * (R * 1.1), cy - R * 0.25)
        cv.paint(part("wing", polygon([root, tip, mid, low], curve=lambda x, y: _norm(-0.2, 0.6, 0.8)), look["wing"]))
    tail = [(cx - R * 0.5, cy - R * 0.5), (cx - R * 1.4, cy - R * 1.1), (cx - R * 0.9, cy - R * 0.4)]
    cv.paint(part("tail", polygon(tail), look["wing"]))
    cv.paint(part("ball", ellipse(cx, cy, R, R, 0.0, 2.4), EYE_WHITE, thresholds=(0.16, 0.97)))
    cv.finish(OUT, rim=False)
    cv.clean()
    # One big iris looking at the hero (right before mirroring).
    ix, iy = cx + R * 0.25, cy - R * 0.05
    if p["eyes"] in ("dead", "hurt"):
        # A shut, squeezed eye: an X across the ball.
        for d in range(-3, 4):
            set_px(cv, ix + d, iy + d, EYE)
            set_px(cv, ix + d, iy - d, EYE)
    else:
        fill_ellipse(cv, ix, iy, R * 0.52, R * 0.58, look["iris"], None)
        fill_ellipse(cv, ix + 0.4, iy, R * 0.24, R * 0.32, EYE, None)
        fill_ellipse(cv, ix - R * 0.18, iy + R * 0.22, R * 0.14, R * 0.14, EYE_HI, None)
        set_px(cv, ix + R * 0.28, iy - R * 0.25, EYE_HI)
    if p["mouth"] == "open":
        fill_ellipse(cv, cx + R * 0.2, cy - R * 0.72, R * 0.28, R * 0.16, MOUTH, None)
    img = cv.image()
    if p["flash"]:
        img = flash(img)
    return img


# ---------------------------------------------------------------- clips

def clips_for(look):
    if look.get("kind") == "flyeye":
        idle = [pose(by=math.sin(i / 6 * math.tau) * 1.5, wing=math.sin(i / 6 * math.tau)) for i in range(6)]
        attack = [pose(bx=-2, by=1, wing=1.0), pose(bx=-3, by=2, wing=0.6), pose(bx=5, by=-1, wing=-0.6, mouth="open"),
                  pose(bx=6, by=-1, wing=-1.0, mouth="open"), pose(bx=3, wing=0.0), pose(bx=0, wing=0.6)]
        hit = [pose(bx=-3, eyes="hurt", flash=True, wing=0.8), pose(bx=-2, eyes="hurt", wing=0.2), pose(bx=-1, wing=-0.4)]
        dead = [pose(eyes="dead", flash=True), pose(eyes="dead", fall=20, wing=-0.5), pose(eyes="dead", fall=45, wing=-1.0),
                pose(eyes="dead", fall=70, wing=-1.0), pose(eyes="dead", fall=90, wing=-1.0), pose(eyes="dead", fall=90, wing=-1.0)]
        return {"idle": idle, "attack": attack, "hit": hit, "dead": dead}

    weapon = look["weapon"]
    stand = dict(fa=(-55.0, -15.0), ba=(-115.0, -95.0), fl=(-82.0, -92.0), bl=(-98.0, -88.0))
    rest = {"club": 55.0, "sword": 40.0, "katana": 35.0, "staff": 80.0, "bow": 0.0, "none": 0.0}[weapon]
    idle = []
    for i in range(6):
        t = i / 6 * math.tau
        s = (math.sin(t) + 1.0) / 2.0
        idle.append(pose(squash=round(s), lean=-2.0, weapon=rest + s * 4, glow=s, eyes="closed" if i == 4 else "open", **stand))
    if weapon in ("club", "sword", "katana"):
        attack = [pose(lean=-6, bx=-1, squash=1, fa=(-20.0, 50.0), ba=(-125.0, -100.0), weapon=125.0),
                  pose(lean=-12, bx=-2, squash=1, fa=(30.0, 100.0), ba=(-135.0, -110.0), weapon=155.0, mouth="grit"),
                  pose(lean=12, bx=3, fa=(-5.0, -10.0), ba=(-150.0, -140.0), weapon=-15.0, mouth="open", smear=(155.0, -20.0)),
                  pose(lean=15, bx=4, fa=(-35.0, -45.0), ba=(-155.0, -140.0), weapon=-50.0, mouth="open"),
                  pose(lean=6, bx=2, fa=(-50.0, -30.0), weapon=5.0),
                  pose(lean=-2, weapon=rest, **stand)]
    elif weapon == "staff":
        attack = [pose(lean=-6, fa=(-10.0, 40.0), weapon=95.0, glow=0.5),
                  pose(lean=-10, fa=(10.0, 60.0), weapon=100.0, glow=1.0, mouth="grit"),
                  pose(lean=10, bx=2, fa=(-20.0, 0.0), weapon=40.0, glow=1.6, mouth="open"),
                  pose(lean=12, bx=3, fa=(-25.0, -5.0), weapon=35.0, glow=1.2, mouth="open"),
                  pose(lean=4, fa=(-40.0, -10.0), weapon=60.0, glow=0.5),
                  pose(lean=-2, weapon=rest, **stand)]
    elif weapon == "bow":
        bowarm = dict(ba=(-10.0, -5.0), fa=(-160.0, -170.0))
        attack = [pose(lean=-4, draw=0.3, **bowarm), pose(lean=-8, draw=0.8, mouth="grit", **bowarm),
                  pose(lean=-10, draw=1.0, mouth="grit", **bowarm), pose(lean=6, bx=1, draw=0.0, mouth="open", **bowarm),
                  pose(lean=2, draw=0.0, **bowarm), pose(lean=-2, **stand)]
        idle = [dict(q, ba=(-40.0, -20.0)) for q in idle]
    else:  # mushroom headbutt
        attack = [pose(lean=-12, bx=-1, squash=1, **stand), pose(lean=-18, bx=-2, squash=2, mouth="grit", **stand),
                  pose(lean=22, bx=4, by=1, mouth="open", **stand), pose(lean=18, bx=4, mouth="open", **stand),
                  pose(lean=6, bx=2, **stand), pose(lean=-2, **stand)]
    hit = [pose(lean=-14, bx=-2, squash=1, weapon=rest - 20, eyes="hurt", mouth="grit", flash=True, **stand),
           pose(lean=-12, bx=-2, weapon=rest - 20, eyes="hurt", mouth="grit", **stand),
           pose(lean=-5, bx=-1, weapon=rest - 10, **stand)]
    dead = [pose(lean=-14, bx=-2, weapon=rest, eyes="dead", mouth="open", flash=True, **stand),
            pose(lean=-18, bx=-3, squash=1, fa=(-10.0, 30.0), ba=(-150.0, -130.0), fl=(-60.0, -110.0), bl=(-115.0, -120.0),
                 weapon=rest - 30, eyes="dead", mouth="open"),
            pose(lean=-10, bx=-4, by=-1, fa=(-80.0, -100.0), ba=(-130.0, -130.0), fl=(-20.0, -100.0), bl=(-140.0, -160.0),
                 weapon=-30.0, eyes="dead", fall=30.0),
            pose(lean=-10, bx=-5, by=-1, fa=(-90.0, -110.0), ba=(-140.0, -140.0), fl=(-10.0, -60.0), bl=(-150.0, -170.0),
                 weapon=-70.0, eyes="dead", fall=60.0),
            pose(lean=-10, bx=-6, by=-1, fa=(-100.0, -120.0), ba=(-150.0, -150.0), fl=(0.0, -20.0), bl=(-160.0, -175.0),
                 weapon=-95.0, eyes="dead", fall=85.0),
            pose(lean=-8, bx=-6, by=-1, fa=(-105.0, -125.0), ba=(-150.0, -150.0), fl=(5.0, -10.0), bl=(-165.0, -178.0),
                 weapon=-100.0, eyes="dead", fall=88.0)]
    return {"idle": idle, "attack": attack, "hit": hit, "dead": dead}


def render(entity):
    look = LOOKS[entity]
    frame = BOSS_FRAME if look.get("boss") else ENEMY_FRAME
    builder = build_flyeye if look.get("kind") == "flyeye" else build_humanoid
    out = {}
    for clip, poses in clips_for(look).items():
        out[clip] = [ImageOps.mirror(builder(look, q, frame)) for q in poses]
    return out


def write(entity, sheets):
    folder = "Bosses" if LOOKS[entity].get("boss") else "Enemies"
    directory = os.path.join(ART, folder)
    for old in glob.glob(os.path.join(directory, entity + "_*.png")):
        name = os.path.basename(old)
        clip = name[len(entity) + 1:].split("_")[0]
        if clip in sheets and name == "%s_%s_%d.png" % (entity, clip, len(sheets[clip])):
            continue
        os.remove(old)
        if os.path.exists(old + ".meta"):
            os.remove(old + ".meta")
    for clip, frames in sheets.items():
        sheet(frames).save(os.path.join(directory, "%s_%s_%d.png" % (entity, clip, len(frames))))


def preview(path, scale=3):
    rows = []
    for entity in LOOKS:
        s = render(entity)
        rows.append(s["idle"][:1] + s["attack"][2:3] + s["hit"][:1] + s["dead"][-1:])
    cell = BOSS_FRAME
    img = Image.new("RGBA", (4 * cell[0] * scale, sum(r[0].height for r in rows) * scale), hexc("#3a3f58"))
    y = 0
    for frames in rows:
        h = frames[0].height
        for i, f in enumerate(frames):
            img.alpha_composite(f.resize((f.width * scale, f.height * scale), Image.NEAREST), (i * cell[0] * scale, y))
        y += h * scale
    img = img.crop((0, 0, img.width, y))
    img.save(path)


if __name__ == "__main__":
    if "--preview" in sys.argv:
        preview(sys.argv[sys.argv.index("--preview") + 1])
    else:
        for name in LOOKS:
            write(name, render(name))
        print("cast", len(LOOKS))
