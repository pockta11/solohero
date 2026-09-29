"""SoloHero hero sprite (D-090): a cute chibi swordsman in the Lucky Defense look, native 1x1 pixels, faces right.

Style rules: a big round head over a tiny round body, one dark outline colour around every part, two-tone cel
shading with a small highlight, sparkling eyes, blush, soft scalloped bangs with one cowlick, a chunky toy sword.

Run: python tools/art/hero.py [--preview out.png]
Writes Assets/SoloHero/Art/Hero/knight_{clip}_{frames}.png and removes older knight sheets with other frame counts.
"""
import glob
import math
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from charkit import Canvas, Part, capsule, clean_orphans, ellipse, hexc, polygon, ramp, sheet, superbox, _norm

HERO_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Art", "Hero")
W, H = 96, 72
FOOT_X = 40

OUT = hexc("#2b1d3a")
SKIN = ramp("#f2a58a", "#ffd6ba", "#fff1e4")
HAIR = ramp("#e0702c", "#ffa33c", "#ffd88a")
TUNIC = ramp("#3a64d0", "#5c98ff", "#aad2ff")
CAPE = ramp("#c2384c", "#ff5a62", "#ff9f8f")
PANTS = ramp("#3d3769", "#5a5394", "#8078bf")
BOOTS = ramp("#8c4f2c", "#bb7342", "#e7a868")
GOLD = ramp("#d68a1c", "#ffc434", "#fff2a4")
BLADE = ramp("#8aa6cf", "#e2eeff", "#ffffff")
GRIP = ramp("#6b3a28", "#8f5236", "#b8744a")
EYE = hexc("#2b1d3a")
EYE_HI = hexc("#ffffff")
EYE_LOW = hexc("#5a4a8a")
BLUSH = hexc("#ff8f9c")
MOUTH = hexc("#b8465a")
SMEAR = (hexc("#ffffff"), hexc("#d8ecff"), hexc("#8cc4ff"))

# Cel shading: most of a part is its base tone, the side away from the light is the shadow tone, a small spot on the
# lit shoulder is the highlight.
CEL = (0.34, 0.9)

# Proportions (art pixels). Head radius R; the body is about half the head.
R = 13.0


def rad(d):
    return math.radians(d)


class Xf:
    """Global transform: rotate by `a` around (px, py), then translate (tx, ty)."""

    def __init__(self, a=0.0, px=0.0, py=0.0, tx=0.0, ty=0.0):
        self.a, self.px, self.py, self.tx, self.ty = a, px, py, tx, ty
        self.c, self.s = math.cos(a), math.sin(a)

    def p(self, x, y):
        dx, dy = x - self.px, y - self.py
        return self.px + dx * self.c - dy * self.s + self.tx, self.py + dx * self.s + dy * self.c + self.ty


def pose(**kw):
    base = dict(
        bx=0.0, by=0.0, lean=0.0, head_tilt=0.0, squash=0.0,
        fa=(-60.0, -20.0), ba=(-110.0, -90.0),        # front / back arm: shoulder, elbow (absolute degrees)
        fl=(-90.0, -90.0), bl=(-90.0, -90.0),          # front / back leg: hip, knee (absolute degrees)
        sword=40.0, cape=0.0, eyes="open", mouth="smile", flash=False, smear=None, fall=0.0,
    )
    base.update(kw)
    return base


def part(name, shape, colors, flat=None, thresholds=CEL):
    return Part(name, shape, colors, thresholds=thresholds, flat=flat, outline=OUT)


def build(p):
    cv = Canvas(W, H, FOOT_X)
    fall_t = p["fall"] / 90.0
    xf = Xf(rad(p["fall"]), 0.0, 0.0, fall_t * 14.0, fall_t * 11.0)
    T = xf.p
    bx, by = p["bx"], p["by"]
    lean = rad(p["lean"])
    squash = p["squash"]

    hip = (bx, 7.0 + by)

    def up(d):
        return hip[0] + math.sin(-lean) * d, hip[1] + math.cos(lean) * d

    body_c = up(5.0 - squash * 0.5)
    neck = up(9.5 - squash)
    hc = up(9.5 - squash + R - 1.5)
    hc = (hc[0] + 1.0, hc[1])
    sh_front = (body_c[0] + 3.2, body_c[1] + 2.5)
    sh_back = (body_c[0] - 3.4, body_c[1] + 2.8)

    def seg(x, y, angs, l1, l2):
        a1, a2 = rad(angs[0]), rad(angs[1])
        j = (x + math.cos(a1) * l1, y + math.sin(a1) * l1)
        e = (j[0] + math.cos(a2) * l2, j[1] + math.sin(a2) * l2)
        return j, e

    fk, ff = seg(hip[0] + 2.4, hip[1], p["fl"], 3.4, 3.2)
    bk, bf = seg(hip[0] - 2.4, hip[1], p["bl"], 3.4, 3.2)
    fe, fh = seg(*sh_front, p["fa"], 3.2, 3.0)
    be, bh = seg(*sh_back, p["ba"], 3.2, 3.0)

    def cap(a, b, r1, r2=None):
        A, B = T(*a), T(*b)
        return capsule(A[0], A[1], B[0], B[1], r1, r2)

    def ell(c, rx, ry, r=0.0, bulge=1.0):
        C = T(*c)
        return ellipse(C[0], C[1], rx, ry, r + xf.a, bulge)

    def poly(points, **kw):
        return polygon([T(*q) for q in points], **kw)

    # 1. Cape: a short rounded cape behind the body, swinging with p["cape"].
    sway = p["cape"]
    cape = [(sh_back[0] + 1.0, sh_back[1] + 1.8), (sh_front[0] - 1.5, sh_front[1] + 1.6),
            (hip[0] - 2.0 - sway * 0.4, hip[1] - 2.0),
            (hip[0] - 6.0 - sway * 1.2, hip[1] - 1.0 + sway * 1.0),
            (hip[0] - 8.5 - sway * 1.8, hip[1] + 1.5 + sway * 1.8),
            (sh_back[0] - 3.0 - sway * 0.8, sh_back[1] - 1.0 + sway * 0.6)]
    cv.paint(part("cape", poly(cape, curve=lambda x, y: _norm(-0.2, 0.5, 0.8)), CAPE))

    # 2. Back arm and back leg (the far side).
    cv.paint(part("barm", cap(sh_back, be, 2.2, 2.0), TUNIC))
    cv.paint(part("barm2", cap(be, bh, 2.0), TUNIC))
    cv.paint(part("bhand", ell(bh, 2.5, 2.5), SKIN))
    cv.paint(part("bleg", cap((hip[0] - 2.4, hip[1]), bk, 2.6, 2.4), PANTS))
    cv.paint(part("bleg2", cap(bk, bf, 2.4), PANTS))
    cv.paint(part("bboot", ell((bf[0] + 1.0, bf[1] + 1.4), 3.2, 2.2), BOOTS))

    # 3. Body: a round tunic with a gold belt.
    cv.paint(part("body", ell(body_c, 6.4, 5.8 - squash * 0.4, -lean, 1.4), TUNIC))
    belt = superbox(*T(hip[0] + math.sin(-lean) * 2.0, hip[1] + 2.0), 6.0, 1.1, 6.0, -lean + xf.a)
    cv.paint(part("belt", belt, GOLD))

    # 4. Front leg.
    cv.paint(part("fleg", cap((hip[0] + 2.4, hip[1]), fk, 2.7, 2.5), PANTS))
    cv.paint(part("fleg2", cap(fk, ff, 2.5), PANTS))
    cv.paint(part("fboot", ell((ff[0] + 1.2, ff[1] + 1.4), 3.4, 2.3), BOOTS))

    # 5. Head: a big round face, then the hair cap with scalloped bangs and a cowlick.
    tilt = rad(p["head_tilt"]) - lean * 0.4 + xf.a
    HC = T(*hc)

    def local(x, y):
        dx, dy = x - HC[0], y - HC[1]
        c, s = math.cos(-tilt), math.sin(-tilt)
        return dx * c - dy * s, dx * s + dy * c

    def to_world(lx, ly):
        c, s = math.cos(tilt), math.sin(tilt)
        return HC[0] + lx * c - ly * s, HC[1] + lx * s + ly * c

    # The face stays almost one flat tone: only its far rim falls into shadow.
    cv.paint(part("face", ellipse(HC[0], HC[1], R, R - 1.0, tilt, 3.2), SKIN, thresholds=(0.16, 0.97)))

    def fringe(lx):
        # Three soft locks across the forehead; the tips dip toward the eyes.
        phase = (lx - R * 0.1) / (R * 0.36) * math.pi
        return R * 0.36 - 2.6 * max(0.0, math.cos(phase)) ** 0.8

    def hair_shape(x, y):
        lx, ly = local(x, y)
        u = (lx + 0.6) / (R + 1.2)
        v = (ly - 1.2) / (R + 0.8)
        if u * u + v * v > 1.0:
            return None
        # Behind the ear the hair falls lower along a slanted edge, framing the cheek.
        back = lx < -R * 0.34 + ly * 0.4 and ly > -R * 0.55
        if ly < fringe(lx) and not back:
            return None
        z = math.sqrt(max(0.0, 1.0 - u * u - v * v))
        c, s = math.cos(tilt), math.sin(tilt)
        return _norm(u * c - v * s, u * s + v * c, z * 1.6)

    cv.paint(part("hair", hair_shape, HAIR))
    tip = to_world(R * 0.1, R + 3.2)
    root = to_world(-R * 0.1, R + 0.2)
    bend = to_world(R * 0.35, R + 1.2)
    cv.paint(part("cowlick", union_shapes(capsule(root[0], root[1], bend[0], bend[1], 1.3),
                                          capsule(bend[0], bend[1], tip[0], tip[1], 1.2, 0.8)), HAIR))

    # 6. Front arm with the sword.
    sa = rad(p["sword"])
    dx, dy = math.cos(sa), math.sin(sa)
    nx, ny = -dy, dx
    hx, hy = fh
    pommel = (hx - dx * 3.0, hy - dy * 3.0)
    guard = (hx + dx * 2.2, hy + dy * 2.2)
    tip_s = (hx + dx * 17.0, hy + dy * 17.0)
    if p["smear"] is not None:
        draw_smear(cv, T, hx, hy, p["smear"])
    blade = [(guard[0] + nx * 2.3, guard[1] + ny * 2.3), (tip_s[0] - dx * 3.0 + nx * 2.3, tip_s[1] - dy * 3.0 + ny * 2.3),
             tip_s, (tip_s[0] - dx * 3.0 - nx * 2.3, tip_s[1] - dy * 3.0 - ny * 2.3),
             (guard[0] - nx * 2.3, guard[1] - ny * 2.3)]
    G = T(*guard)

    def blade_normal(x, y):
        side = (x - G[0]) * nx + (y - G[1]) * ny
        return _norm(-0.7, 0.7, 0.4) if side > 0.2 else _norm(0.3, -0.2, 1.0)

    cv.paint(part("blade", poly(blade, curve=blade_normal), BLADE))
    cv.paint(part("grip", cap(pommel, guard, 1.3), GRIP))
    cv.paint(part("pommel", ell(pommel, 1.9, 1.9), GOLD))
    cv.paint(part("guard", cap((guard[0] + nx * 3.6, guard[1] + ny * 3.6), (guard[0] - nx * 3.6, guard[1] - ny * 3.6), 1.4), GOLD))
    cv.paint(part("farm", cap(sh_front, fe, 2.3, 2.1), TUNIC))
    cv.paint(part("farm2", cap(fe, fh, 2.1), TUNIC))
    cv.paint(part("fhand", ell(fh, 2.7, 2.7), SKIN))

    cv.finish(OUT, rim=False)
    cv.clean()
    draw_face(cv, to_world, p)
    img = cv.image()
    if p["flash"]:
        img = flash(img)
    return img


def union_shapes(*shapes):
    def f(x, y):
        for s in shapes:
            n = s(x, y)
            if n is not None:
                return n
        return None

    return f


def fill_ellipse(cv, cx, cy, rx, ry, color, only_on=None):
    """Paints a filled ellipse directly into the finished frame (face features), only over opaque pixels."""
    for py in range(cv.h):
        wy = cv.h - 1 - py + 0.5
        for px in range(cv.w):
            wx = px - cv.fx + 0.5
            if ((wx - cx) / rx) ** 2 + ((wy - cy) / ry) ** 2 <= 1.0 and cv.final[py][px] is not None:
                if only_on is None or cv.final[py][px] in only_on:
                    cv.final[py][px] = color


def set_px(cv, x, y, color):
    px, py = cv.to_px(math.floor(x), math.floor(y))
    px, py = int(px), int(py)
    if 0 <= px < cv.w and 0 <= py < cv.h and cv.final[py][px] is not None:
        cv.final[py][px] = color


def draw_face(cv, to_world, p):
    """Big sparkling eyes set low and wide, blush under them, a tiny mouth."""
    skin = set(SKIN)
    eyes = p["eyes"]
    for ex, rx in ((R * 0.36, 2.1), (-R * 0.2, 1.8)):
        cx, cy = to_world(ex, -R * 0.26)
        if eyes == "open":
            fill_ellipse(cv, cx, cy, rx, 3.4, EYE, skin)
            fill_ellipse(cv, cx + 0.2, cy - 1.6, rx * 0.7, 1.0, EYE_LOW, {EYE})
            set_px(cv, cx - 0.8, cy + 1.2, EYE_HI)
            set_px(cv, cx + 0.2, cy + 1.2, EYE_HI)
            set_px(cv, cx - 0.8, cy + 0.2, EYE_HI)
            set_px(cv, cx + 0.2, cy + 0.2, EYE_HI)
            set_px(cv, cx + 1.0, cy - 1.4, EYE_HI)
        elif eyes == "closed":
            for dx, dy in ((-1.5, 0.0), (-0.5, 0.8), (0.5, 0.8), (1.5, 0.0)):
                set_px(cv, cx + dx, cy + dy, EYE)
        else:  # hurt: > <
            for dx, dy in ((-1.2, 1.2), (0.0, 0.0), (-1.2, -1.2), (1.2, 1.2), (1.2, -1.2)):
                set_px(cv, cx + dx, cy + dy, EYE)
    for ex in (R * 0.66, -R * 0.44):
        bx, by = to_world(ex, -R * 0.52)
        fill_ellipse(cv, bx, by, 1.8, 0.9, BLUSH, skin)
    mx, my = to_world(R * 0.1, -R * 0.58)
    if p["mouth"] == "open":
        fill_ellipse(cv, mx, my - 0.3, 1.3, 1.3, MOUTH, skin)
    elif p["mouth"] == "grit":
        for dx in (-1.0, 0.0, 1.0):
            set_px(cv, mx + dx, my, EYE)
    else:
        for dx, dy in ((-1.0, 0.0), (0.0, -0.9), (1.0, 0.0)):
            set_px(cv, mx + dx, my + dy, EYE)


def draw_smear(cv, T, hx, hy, arc):
    """A crescent swipe from angle a0 to a1 (degrees) around the hand: the fast-swing frame."""
    a0, a1 = arc
    r_out, r_in = 22.0, 15.0
    for i in range(81):
        t = i / 80
        a = rad(a0 + (a1 - a0) * t)
        rin = r_out - (r_out - r_in) * math.sin(t * math.pi)
        r = rin
        while r <= r_out:
            X, Y = T(hx + math.cos(a) * r, hy + math.sin(a) * r)
            px, py = cv.to_px(X, Y)
            px, py = int(math.floor(px)), int(math.floor(py))
            depth = (r_out - r) / max(0.01, r_out - rin)
            c = SMEAR[0] if depth < 0.35 else SMEAR[1] if depth < 0.7 else SMEAR[2]
            if 0 <= px < cv.w and 0 <= py < cv.h and cv.id[py][px] < 0:
                cv.col[py][px] = c
                cv.id[py][px] = 10_000
            r += 0.5


def flash(img):
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (min(255, r + 150), min(255, g + 150), min(255, b + 150), a)
    return out


# ---------------------------------------------------------------- clips

def idle():
    frames = []
    n = 8
    for i in range(n):
        t = i / n * math.tau
        k = (math.sin(t) + 1.0) / 2.0
        frames.append(pose(
            by=0.0, squash=round(k), lean=-2.0,
            fa=(-55.0 + k * 6, -15.0 + k * 6), ba=(-115.0, -95.0 + k * 6),
            fl=(-82.0, -92.0), bl=(-98.0, -88.0),
            sword=40.0 + k * 4, cape=math.sin(t) * 1.0,
            eyes="closed" if i == 5 else "open"))
    return frames


def run():
    frames = []
    n = 8
    for i in range(n):
        t = i / n * math.tau
        s, c = math.sin(t), math.cos(t)
        frames.append(pose(
            by=round(abs(s) * 1.5), lean=10.0,
            fl=(-90.0 + s * 40.0, -90.0 + s * 40.0 - max(0.0, -c) * 50.0),
            bl=(-90.0 - s * 40.0, -90.0 - s * 40.0 - max(0.0, c) * 50.0),
            fa=(-70.0 - s * 30.0, -25.0 - s * 20.0), ba=(-110.0 + s * 35.0, -80.0 + s * 30.0),
            sword=18.0 - s * 10.0, cape=3.0 + math.sin(t * 2.0) * 1.2, mouth="open" if i in (2, 6) else "smile"))
    return frames


def attack():
    return [
        pose(lean=-6.0, bx=-1.0, squash=1, fa=(-20.0, 50.0), ba=(-125.0, -100.0), fl=(-75.0, -95.0), bl=(-105.0, -92.0),
             sword=125.0, cape=1.0, mouth="smile"),
        pose(lean=-12.0, bx=-2.0, squash=1, fa=(30.0, 100.0), ba=(-135.0, -110.0), fl=(-72.0, -95.0), bl=(-108.0, -92.0),
             sword=155.0, cape=0.5, mouth="grit"),
        pose(lean=12.0, bx=3.0, fa=(-5.0, -10.0), ba=(-150.0, -140.0), fl=(-60.0, -92.0), bl=(-118.0, -98.0),
             sword=-15.0, cape=3.0, mouth="open", smear=(155.0, -20.0)),
        pose(lean=15.0, bx=4.0, fa=(-35.0, -45.0), ba=(-155.0, -140.0), fl=(-58.0, -92.0), bl=(-120.0, -98.0),
             sword=-50.0, cape=3.5, mouth="open"),
        pose(lean=12.0, bx=4.0, fa=(-45.0, -55.0), ba=(-150.0, -130.0), fl=(-60.0, -92.0), bl=(-118.0, -98.0),
             sword=-60.0, cape=2.5, mouth="grit"),
        pose(lean=5.0, bx=2.0, fa=(-55.0, -30.0), ba=(-125.0, -105.0), fl=(-70.0, -92.0), bl=(-110.0, -90.0),
             sword=10.0, cape=1.5),
        pose(lean=-2.0, fa=(-55.0, -15.0), ba=(-115.0, -95.0), fl=(-82.0, -92.0), bl=(-98.0, -88.0),
             sword=40.0, cape=1.0),
    ]


def hit():
    return [
        pose(lean=-14.0, bx=-2.0, squash=1, fa=(-30.0, 10.0), ba=(-140.0, -120.0), fl=(-72.0, -90.0), bl=(-108.0, -92.0),
             sword=25.0, cape=-1.0, eyes="hurt", mouth="grit", flash=True),
        pose(lean=-12.0, bx=-2.0, fa=(-35.0, 5.0), ba=(-135.0, -115.0), fl=(-72.0, -90.0), bl=(-108.0, -92.0),
             sword=25.0, cape=-0.5, eyes="hurt", mouth="grit"),
        pose(lean=-6.0, bx=-1.0, fa=(-45.0, -5.0), ba=(-120.0, -100.0), fl=(-78.0, -92.0), bl=(-102.0, -90.0),
             sword=30.0, cape=0.5, eyes="hurt"),
        pose(lean=-2.0, fa=(-55.0, -15.0), ba=(-115.0, -95.0), fl=(-82.0, -92.0), bl=(-98.0, -88.0),
             sword=40.0, cape=1.0),
    ]


def dead():
    return [
        pose(lean=-14.0, bx=-2.0, fa=(-30.0, 10.0), ba=(-140.0, -120.0), fl=(-72.0, -90.0), bl=(-108.0, -92.0),
             sword=60.0, eyes="hurt", mouth="grit", flash=True),
        pose(lean=-18.0, bx=-3.0, squash=1, fa=(-10.0, 30.0), ba=(-150.0, -130.0), fl=(-60.0, -110.0),
             bl=(-115.0, -120.0), sword=50.0, eyes="hurt", mouth="open"),
        pose(lean=-8.0, bx=-3.0, by=-2.0, squash=2, fa=(-60.0, -80.0), ba=(-130.0, -120.0), fl=(-30.0, -110.0),
             bl=(-130.0, -150.0), sword=-40.0, eyes="closed", mouth="open", cape=-1.0),
        pose(lean=-10.0, bx=-4.0, by=-2.0, fa=(-80.0, -100.0), ba=(-130.0, -130.0), fl=(-20.0, -100.0),
             bl=(-140.0, -160.0), sword=-30.0, eyes="closed", fall=25.0, cape=-2.0),
        pose(lean=-10.0, bx=-5.0, by=-2.0, fa=(-90.0, -110.0), ba=(-140.0, -140.0), fl=(-10.0, -60.0),
             bl=(-150.0, -170.0), sword=-70.0, eyes="closed", fall=55.0, cape=-3.0),
        pose(lean=-10.0, bx=-6.0, by=-2.0, fa=(-100.0, -120.0), ba=(-150.0, -150.0), fl=(0.0, -20.0),
             bl=(-160.0, -175.0), sword=-95.0, eyes="closed", fall=80.0, cape=-3.0),
        pose(lean=-8.0, bx=-6.0, by=-2.0, fa=(-105.0, -125.0), ba=(-150.0, -150.0), fl=(5.0, -10.0),
             bl=(-165.0, -178.0), sword=-100.0, eyes="closed", fall=88.0, cape=-2.0),
        pose(lean=-8.0, bx=-6.0, by=-2.0, fa=(-105.0, -125.0), ba=(-150.0, -150.0), fl=(5.0, -10.0),
             bl=(-165.0, -178.0), sword=-100.0, eyes="closed", fall=88.0, cape=-2.0),
    ]


CLIPS = {"idle": idle, "run": run, "attack": attack, "hit": hit, "dead": dead}


def render_all():
    return {name: [build(p) for p in fn()] for name, fn in CLIPS.items()}


def write(sheets):
    for name, frames in sheets.items():
        for old in glob.glob(os.path.join(HERO_DIR, "knight_%s_*.png" % name)):
            if not old.endswith("_%d.png" % len(frames)):
                os.remove(old)
                meta = old + ".meta"
                if os.path.exists(meta):
                    os.remove(meta)
        sheet(frames).save(os.path.join(HERO_DIR, "knight_%s_%d.png" % (name, len(frames))))


def preview(sheets, path, scale=4):
    rows = list(sheets.values())
    cols = max(len(r) for r in rows)
    img = Image.new("RGBA", (cols * W * scale, len(rows) * H * scale), hexc("#3a3f58"))
    for r, frames in enumerate(rows):
        for c, f in enumerate(frames):
            img.alpha_composite(f.resize((W * scale, H * scale), Image.NEAREST), (c * W * scale, r * H * scale))
    img.save(path)


if __name__ == "__main__":
    sheets = render_all()
    if "--preview" in sys.argv:
        preview(sheets, sys.argv[sys.argv.index("--preview") + 1])
    else:
        write(sheets)
