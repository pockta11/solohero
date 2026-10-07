"""UI skin v3 (D-108): smooth, rounded, casual-RPG sprites -> Assets/SoloHero/Art/UI/Hd.

Usage: python tools/art/uigen3.py [OUT_DIR] [--preview PREVIEW.png]
Files named hd9_{name}_{border}.png are 9-slice sprites (border in px, all four sides; {sides}x{caps} when the
left/right and top/bottom borders differ); SpriteImportPreset imports
everything under UI/Hd at 100 px per unit with bilinear filtering, so 1 px = 1 canvas unit on the 1080 layout.
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from uikit import Layer, band, cov, hexc, mix, radial, shade, vgrad, vgrad3  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art', 'UI', 'Hd')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
if args:
    OUT = args[0]
PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

# Palette -------------------------------------------------------------------------------------------------------------
INK = hexc('#2A1F3D')          # outline, same plum as the character outlines
WHITE = hexc('#FFFFFF')
CREAM_HI = hexc('#FFFCF4')
CREAM = hexc('#FFF4DE')
CREAM_LO = hexc('#F5E3C1')
TAN = hexc('#E5CB9C')
TAN_DK = hexc('#C9A26C')
BROWN = hexc('#9A6334')
BROWN_HI = hexc('#E8A65A')
BROWN_DK = hexc('#5E3A1E')
NAVY_HI = hexc('#4D5598')
NAVY = hexc('#2F3468')
NAVY_DK = hexc('#1C1E42')
GOLD_HI = hexc('#FFF0A8')
GOLD = hexc('#FFC43D')
GOLD_DK = hexc('#C97A12')

# (top, bottom, lip) per button tone; the outline is INK for all.
TONES = {
    'green': (hexc('#9BF27A'), hexc('#38B94C'), hexc('#227F36')),
    'gold': (hexc('#FFEA80'), hexc('#FFB323'), hexc('#C8740E')),
    'blue': (hexc('#86D8FF'), hexc('#3090F0'), hexc('#1D5DB6')),
    'red': (hexc('#FFA294'), hexc('#EE4F4F'), hexc('#A42E3C')),
    'purple': (hexc('#DDB0FF'), hexc('#9A5CF2'), hexc('#6538B6')),
    'gray': (hexc('#E8E5EE'), hexc('#AFA9BE'), hexc('#7C7692')),
    'orange': (hexc('#FFC27A'), hexc('#FF8A2A'), hexc('#C2561A')),
}

# Grade frames (light, mid, dark, inner centre, inner edge). D-113 gear ladder: c u r e l m a
# (common, uncommon, rare, epic, legendary, mythic, ancient).
GRADES = {
    'c': (hexc('#F2F4F8'), hexc('#AEB6C4'), hexc('#6F7788'), hexc('#F4F1EC'), hexc('#D5D0C8')),
    'u': (hexc('#DDF8D2'), hexc('#5CC75A'), hexc('#2C8A3A'), hexc('#EEFBE8'), hexc('#B5E6A6')),
    'r': (hexc('#CDE7FF'), hexc('#4C9DFF'), hexc('#2556B8'), hexc('#E6F3FF'), hexc('#A9CFFA')),
    'e': (hexc('#F0D6FF'), hexc('#B067FF'), hexc('#6A2FC0'), hexc('#F5E8FF'), hexc('#D4AFFA')),
    'l': (hexc('#FFF6C2'), hexc('#FFB72E'), hexc('#C2650E'), hexc('#FFF6D8'), hexc('#FFD27A')),
    'm': (hexc('#FFD8D8'), hexc('#FF4D5E'), hexc('#A81F35'), hexc('#FFEDED'), hexc('#FFB2B8')),
    'a': (hexc('#D8FFF8'), hexc('#2FD9C6'), hexc('#127A86'), hexc('#EAFFFB'), hexc('#9EEFE4')),
}
# Sparkle stars on the top grades: legendary two, mythic three, ancient four.
GRADE_STARS = {'l': 2, 'm': 3, 'a': 4}

made = []


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name + '.png'))
    made.append((name, img))


# Buttons -------------------------------------------------------------------------------------------------------------
def button(tone, pressed=False, w=88, h=88, r=22, lip=8):
    """Candy button: ink outline, darker lip under the body, gradient body, gloss on the top half, specular dot."""
    top, bottom, lip_c = TONES[tone]
    L = Layer(w, h)
    o = 3
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    if pressed:
        body_top, body_bot = o + lip - 2, h - o
        top, bottom = shade(top, -0.06), shade(bottom, -0.08)
    else:
        L.over(cov(L.rrect(o, o, w - o, h - o, r - o)), lip_c)
        body_top, body_bot = o, h - o - lip
    body = L.rrect(o, body_top, w - o, body_bot, r - o)
    L.over(cov(body), vgrad(L, body_top, body_bot, top, bottom))
    # Rim light along the top inner edge, soft dark along the bottom inner edge.
    L.over(band(body, -3.5, -1.0) * (L.y < body_top + (body_bot - body_top) * 0.45), shade(top, 0.55)[:3] + (200,))
    L.over(band(body, -3.0, -0.5) * (L.y > body_top + (body_bot - body_top) * 0.7), shade(bottom, -0.18)[:3] + (150,))
    # Gloss: a rounded band over the top 42% of the body.
    gy1 = body_top + (body_bot - body_top) * 0.46
    gloss = L.rrect(o + 7, body_top + 4, w - o - 7, gy1, (r - o - 6))
    L.over(cov(gloss), vgrad(L, body_top + 4, gy1, (255, 255, 255, 120), (255, 255, 255, 18)))
    # Specular dots.
    L.over(cov(L.circle(o + 11, body_top + 9, 3.2)), (255, 255, 255, 230))
    L.over(cov(L.circle(o + 18, body_top + 7, 1.8)), (255, 255, 255, 170))
    return L.image()


# Panels and cards ------------------------------------------------------------------------------------------------------
def panel(w=112, h=112, r=30):
    """Bottom growth panel: ink outline, warm tan rim, cream body with a soft top light."""
    L = Layer(w, h)
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    rim = L.rrect(4, 4, w - 4, h - 4, r - 4)
    L.over(cov(rim), vgrad(L, 4, h - 4, hexc('#F8DFAE'), hexc('#D9AE6E')))
    body = L.rrect(10, 10, w - 10, h - 10, r - 10)
    L.over(cov(body), vgrad(L, 10, h - 10, CREAM_HI, CREAM_LO))
    L.over(band(body, -2.0, 0.0), hexc('#B98B52', 200))          # crisp inner edge
    L.over(band(body, -5.0, -2.0) * (L.y < h * 0.5), (255, 255, 255, 140))
    return L.image()


def window(w=132, h=132, r=34):
    """Popup window: ink outline, glossy wood-gold frame, inner ink line, cream body."""
    L = Layer(w, h)
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    frame = L.rrect(4, 4, w - 4, h - 4, r - 4)
    L.over(cov(frame), vgrad3(L, 4, h - 4, hexc('#FFD98A'), hexc('#E39A45'), hexc('#A85F25'), 0.45))
    L.over(band(frame, -2.5, -0.5) * (L.y < h * 0.4), (255, 250, 220, 210))
    inner = L.rrect(14, 14, w - 14, h - 14, r - 14)
    L.over(cov(L.rrect(12, 12, w - 12, h - 12, r - 12)), hexc('#6B3E1C'))
    L.over(cov(inner), vgrad(L, 14, h - 14, CREAM_HI, CREAM_LO))
    L.over(band(inner, -4.0, -1.0) * (L.y < h * 0.5), (255, 255, 255, 150))
    return L.image()


def card(w=72, h=72, r=18, fill=(CREAM_HI, hexc('#FFF1D8')), edge=hexc('#E2C691'), lip=hexc('#D3B27C')):
    """Raised card on the cream panel: soft edge line and a 4 px darker lip at the bottom."""
    L = Layer(w, h)
    L.over(cov(L.rrect(0, 2, w, h, r)), lip)
    body = L.rrect(0, 0, w, h - 4, r)
    L.over(cov(body), edge)
    inner = L.rrect(2, 2, w - 2, h - 6, r - 2)
    L.over(cov(inner), vgrad(L, 2, h - 6, fill[0], fill[1]))
    L.over(band(inner, -3.0, -0.5) * (L.y < h * 0.4), (255, 255, 255, 170))
    return L.image()


def inset(w=64, h=64, r=16, fill=hexc('#EBD9B6'), shadow=hexc('#CDB386')):
    """Recessed well: darker cream with an inner shadow along the top."""
    L = Layer(w, h)
    d = L.rrect(0, 0, w, h, r)
    L.over(cov(d), fill)
    L.over(band(d, -5.0, -0.5) * np.clip((h * 0.55 - L.y) / (h * 0.55), 0, 1), shadow)
    L.over(band(d, -1.5, 0.0), shade(shadow, -0.12))
    L.over(band(d, -2.5, -0.5) * (L.y > h * 0.7), (255, 255, 255, 120))
    return L.image()


def plate(tone, w=72, h=56, r=16):
    """Coloured header plate (title chips, grade name tags)."""
    top, bottom, lip_c = TONES[tone]
    L = Layer(w, h)
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    body = L.rrect(3, 3, w - 3, h - 3, r - 3)
    L.over(cov(body), vgrad(L, 3, h - 3, top, bottom))
    L.over(band(body, -3.0, -0.5) * (L.y < h * 0.45), (255, 255, 255, 150))
    L.over(cov(L.rrect(8, 6, w - 8, h * 0.45, (r - 6))), (255, 255, 255, 40))
    return L.image()


def ribbon(w=160, h=76):
    """Popup title ribbon: orange body with folded dark tails on both ends."""
    L = Layer(w, h)
    tail = 22
    # Tails (behind), slightly lower.
    for flip in (False, True):
        x0, x1 = (0, tail + 14) if not flip else (w - tail - 14, w)
        pts = [(x0, 14), (x1, 14), (x1, h - 4), (x0, h - 4)]
        if not flip:
            pts = [(0, 14), (tail + 14, 14), (tail + 14, h - 4), (0, h - 4), (10, (14 + h - 4) / 2)]
        else:
            pts = [(w - tail - 14, 14), (w, 14), (w - 10, (14 + h - 4) / 2), (w, h - 4), (w - tail - 14, h - 4)]
        d = L.polygon(pts)
        L.over(cov(d), INK)
        L.over(cov(d + 3), hexc('#B4471F'))
    body = L.rrect(tail, 0, w - tail, h - 12, 14)
    L.over(cov(body), INK)
    inner = L.rrect(tail + 3, 3, w - tail - 3, h - 15, 11)
    L.over(cov(inner), vgrad(L, 3, h - 15, hexc('#FFB45A'), hexc('#F2682A')))
    L.over(band(inner, -3, -0.5) * (L.y < h * 0.35), (255, 240, 200, 200))
    L.over(cov(L.rrect(tail + 9, 7, w - tail - 9, (h - 12) * 0.45, 8)), (255, 255, 255, 50))
    return L.image()


# HUD ------------------------------------------------------------------------------------------------------------------
def hudbar(w=64, h=128):
    """Top HUD strip: navy gradient fading out at the bottom edge, gold hairline."""
    L = Layer(w, h)
    L.over(np.ones((h, w), np.float32), vgrad(L, 0, h, hexc('#1C1E42', 245), hexc('#2B2F63', 225)))
    L.over(((L.y > h - 6) & (L.y <= h - 3)).astype(np.float32), hexc('#C99A4A', 255))
    L.over((L.y > h - 3).astype(np.float32), INK)
    L.over(((L.y > h - 9) & (L.y <= h - 6)).astype(np.float32), (255, 220, 150, 60))
    return L.image()


def pill(w=64, h=56):
    """Currency pill on the HUD: dark glassy capsule."""
    L = Layer(w, h)
    r = h / 2
    d = L.rrect(0, 0, w, h, r)
    L.over(cov(d), INK)
    inner = L.rrect(3, 3, w - 3, h - 3, r - 3)
    L.over(cov(inner), vgrad(L, 3, h - 3, hexc('#151732', 235), hexc('#2A2D5A', 235)))
    L.over(band(inner, -2.5, -0.5) * (L.y > h * 0.55), (140, 150, 230, 110))
    return L.image()


def gauge(w=48, h=32, r=None):
    r = h / 2 if r is None else r
    L = Layer(w, h)
    d = L.rrect(0, 0, w, h, r)
    L.over(cov(d), INK)
    inner = L.rrect(3, 3, w - 3, h - 3, r - 3)
    L.over(cov(inner), vgrad(L, 3, h - 3, hexc('#140F22'), hexc('#2E2546')))
    return L.image()


def gauge_light(w=48, h=32, r=None):
    """Gauge well for the cream surfaces: tan, recessed, with a soft outline."""
    r = h / 2 if r is None else r
    L = Layer(w, h)
    d = L.rrect(0, 0, w, h, r)
    L.over(cov(d), hexc('#B99464'))
    inner = L.rrect(2, 2, w - 2, h - 2, r - 2)
    L.over(cov(inner), vgrad(L, 2, h - 2, hexc('#D9C29A'), hexc('#EADCBF')))
    L.over(band(inner, -3, -0.5) * (L.y < h * 0.5), hexc('#B89A6A', 160))
    return L.image()


def fill(top, bottom, w=40, h=26, r=None):
    r = h / 2 if r is None else r
    L = Layer(w, h)
    d = L.rrect(0, 0, w, h, r)
    L.over(cov(d), vgrad(L, 0, h, top, bottom))
    L.over(cov(L.rrect(4, 3, w - 4, h * 0.42, r * 0.6)), (255, 255, 255, 110))
    L.over(band(d, -2, 0) * (L.y > h * 0.6), shade(bottom, -0.25)[:3] + (160,))
    return L.image()


def tabbar(w=64, h=64):
    L = Layer(w, h)
    L.over(np.ones((h, w), np.float32), vgrad(L, 0, h, hexc('#33386F'), hexc('#1B1D40')))
    L.over((L.y <= 4).astype(np.float32), INK)
    L.over(((L.y > 4) & (L.y <= 7)).astype(np.float32), hexc('#6A73C2'))
    return L.image()


def tab_active(w=96, h=96, r=24):
    """Raised highlight behind the active tab icon (sits inside the tab cell)."""
    L = Layer(w, h)
    d = L.rrect(0, 0, w, h, r)
    L.over(cov(d), INK)
    inner = L.rrect(3, 3, w - 3, h - 3, r - 3)
    L.over(cov(inner), vgrad(L, 3, h - 3, hexc('#FFE9A6'), hexc('#FFB43A')))
    L.over(band(inner, -3, -0.5) * (L.y < h * 0.45), (255, 255, 255, 190))
    L.over(cov(L.rrect(9, 7, w - 9, h * 0.42, r - 10)), (255, 255, 255, 60))
    return L.image()


def avatar_ring(size=112):
    """Round portrait frame: gold ring with ink outlines; transparent centre (the face is masked under it)."""
    L = Layer(size, size)
    c = size / 2
    d = L.circle(c, c, c - 0.5)
    L.over(cov(d), INK)
    ring = L.circle(c, c, c - 3.5)
    L.over(cov(ring), vgrad3(L, 3, size - 3, GOLD_HI, GOLD, GOLD_DK, 0.5))
    L.over(band(ring, -2.5, -0.5) * (L.y < c), (255, 255, 255, 200))
    hole = L.circle(c, c, c - 11)
    # Punch the centre: draw ink line then clear inside.
    L.over(cov(L.circle(c, c, c - 9)), INK)
    a = cov(hole)
    L.a = L.a * (1 - a)
    return L.image()


def avatar_disc(size=112):
    """Background disc under the portrait (sky gradient)."""
    L = Layer(size, size)
    c = size / 2
    L.over(cov(L.circle(c, c, c - 9)), vgrad(L, 9, size - 9, hexc('#9FD8FF'), hexc('#4D7FD6')))
    return L.image()


def level_badge(w=64, h=40):
    L = Layer(w, h)
    r = h / 2
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    inner = L.rrect(3, 3, w - 3, h - 3, r - 3)
    L.over(cov(inner), vgrad(L, 3, h - 3, hexc('#7C86E8'), hexc('#3F48B0')))
    L.over(band(inner, -2.5, -0.5) * (L.y < h * 0.45), (255, 255, 255, 160))
    return L.image()


def dot(size=36):
    """Notification dot: red with a white ring."""
    L = Layer(size, size)
    c = size / 2
    L.over(cov(L.circle(c, c, c - 0.5)), INK)
    L.over(cov(L.circle(c, c, c - 2.5)), WHITE)
    L.over(cov(L.circle(c, c, c - 5.5)), vgrad(L, 5, size - 5, hexc('#FF7A6E'), hexc('#E0283A')))
    L.over(cov(L.ellipse(c - 3, c - 4, 4.5, 3)), (255, 255, 255, 170))
    return L.image()


def round_button(tone, size=112):
    """Side rail circle button."""
    top, bottom, lip_c = TONES[tone]
    L = Layer(size, size)
    c = size / 2
    L.over(cov(L.circle(c, c, c - 0.5)), INK)
    L.over(cov(L.circle(c, c + 0, c - 3.5)), lip_c)
    body = L.circle(c, c - 3, c - 6)
    L.over(cov(body), radial(L, c - size * 0.12, c - size * 0.2, size * 0.75, shade(top, 0.15), bottom))
    L.over(band(body, -3, -0.5) * (L.y < c - 4), (255, 255, 255, 170))
    L.over(cov(L.ellipse(c, c - size * 0.24, size * 0.27, size * 0.13)), (255, 255, 255, 60))
    return L.image()


def rail_tile(w=112, h=112, r=28):
    """Navy rounded tile for menu icons (right rail)."""
    L = Layer(w, h)
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    L.over(cov(L.rrect(3, 3, w - 3, h - 3, r - 3)), hexc('#151735'))
    body = L.rrect(3, 3, w - 3, h - 9, r - 3)
    L.over(cov(body), vgrad(L, 3, h - 9, hexc('#5961B0'), hexc('#2D3270')))
    L.over(band(body, -3, -0.5) * (L.y < h * 0.4), (200, 210, 255, 170))
    return L.image()


def caption(w=72, h=36):
    """Dark label chip under rail icons."""
    L = Layer(w, h)
    r = h / 2
    L.over(cov(L.rrect(0, 0, w, h, r)), (20, 16, 36, 200))
    return L.image()


# Slots ---------------------------------------------------------------------------------------------------------------
def slot(key, size=88, r=20):
    """Grade item frame: ink outline, bevelled grade ring, tinted radial centre (legendary and up: sparkles)."""
    light, mid, dark, centre, edge = GRADES[key]
    L = Layer(size, size)
    L.over(cov(L.rrect(0, 0, size, size, r)), INK)
    ring = L.rrect(3, 3, size - 3, size - 3, r - 3)
    L.over(cov(ring), vgrad3(L, 3, size - 3, light, mid, dark, 0.5))
    inner = L.rrect(9, 9, size - 9, size - 9, r - 9)
    L.over(cov(L.rrect(8, 8, size - 8, size - 8, r - 8)), shade(dark, -0.35))
    L.over(cov(inner), radial(L, size / 2, size * 0.42, size * 0.62, centre, edge))
    L.over(band(ring, -2.5, -0.5) * (L.y < size * 0.4), (255, 255, 255, 190))
    stars = [(15, 15, 4.5), (size - 15, size - 15, 3.5), (size - 15, 15, 3.5), (15, size - 15, 3.0)]
    if key in GRADE_STARS:
        for (cx, cy, s) in stars[:GRADE_STARS[key]]:
            star = L.polygon([(cx, cy - s * 2), (cx + s * 0.45, cy - s * 0.45), (cx + s * 2, cy), (cx + s * 0.45, cy + s * 0.45),
                              (cx, cy + s * 2), (cx - s * 0.45, cy + s * 0.45), (cx - s * 2, cy), (cx - s * 0.45, cy - s * 0.45)])
            L.over(cov(star), (255, 255, 255, 235))
    return L.image()


def slot_empty(size=88, r=20):
    L = Layer(size, size)
    d = L.rrect(0, 0, size, size, r)
    L.over(cov(d), hexc('#D8C49C'))
    inner = L.rrect(4, 4, size - 4, size - 4, r - 4)
    L.over(cov(inner), vgrad(L, 4, size - 4, hexc('#E9D7B2'), hexc('#F3E6CA')))
    L.over(band(inner, -5, -0.5) * (L.y < size * 0.5), hexc('#CDB384', 160))
    return L.image()


def slot_dark(size=88, r=20):
    """Empty / locked slot on the dark battle HUD."""
    L = Layer(size, size)
    L.over(cov(L.rrect(0, 0, size, size, r)), INK)
    inner = L.rrect(3, 3, size - 3, size - 3, r - 3)
    L.over(cov(inner), vgrad(L, 3, size - 3, hexc('#3A3F72', 230), hexc('#23264E', 230)))
    L.over(band(inner, -2.5, -0.5) * (L.y < size * 0.4), (160, 170, 240, 120))
    return L.image()


def select_ring(size=104, r=26):
    """Selection highlight: thick gold ring with a soft outer glow, transparent centre."""
    L = Layer(size, size)
    d = L.rrect(6, 6, size - 6, size - 6, r - 6)
    glow = np.clip(1 - np.abs(d) / 7.0, 0, 1) ** 1.6
    L.over(glow, (255, 230, 120, 150))
    L.over(band(d, -2.0, 3.0), INK)
    L.over(band(d, -0.5, 1.5), hexc('#FFE27A'))
    return L.image()


def lock_veil(size=64, r=16):
    L = Layer(size, size)
    L.over(cov(L.rrect(0, 0, size, size, r)), (24, 18, 40, 150))
    return L.image()


def close_button(size=80):
    """Round red close button with a white X (popup corner)."""
    img = round_button('red', size)
    L = Layer(size, size)
    c = size / 2
    arm = size * 0.2
    for sx in (1, -1):
        pts = []
        # A thick rotated bar as a polygon.
        w = size * 0.065
        ax, ay, bx, by = c - arm, c - 3 - arm * sx, c + arm, c - 3 + arm * sx
        dx, dy = bx - ax, by - ay
        n = (dx * dx + dy * dy) ** 0.5
        nx, ny = -dy / n * w, dx / n * w
        pts = [(ax + nx, ay + ny), (bx + nx, by + ny), (bx - nx, by - ny), (ax - nx, ay - ny)]
        d = L.polygon(pts)
        L.over(cov(d - 2.5), INK)
    for sx in (1, -1):
        w = size * 0.065
        ax, ay, bx, by = c - arm, c - 3 - arm * sx, c + arm, c - 3 + arm * sx
        dx, dy = bx - ax, by - ay
        n = (dx * dx + dy * dy) ** 0.5
        nx, ny = -dy / n * w, dx / n * w
        d = L.polygon([(ax + nx, ay + ny), (bx + nx, by + ny), (bx - nx, by - ny), (ax - nx, ay - ny)])
        L.over(cov(d), WHITE)
    img.alpha_composite(L.image())
    return img


def pedestal(w=320, h=96):
    """Round stone stage the equipment-panel hero stands on: lit top, darker rim and a soft shadow under it."""
    L = Layer(w, h)
    cx = w / 2
    L.over(np.clip(1 - np.sqrt(((L.x - cx) / (w * 0.5)) ** 2 + ((L.y - h * 0.62) / (h * 0.38)) ** 2), 0, 1) ** 1.2, (40, 24, 50, 90))
    side = L.ellipse(cx, h * 0.5, w * 0.44, h * 0.3)
    L.over(cov(side), INK)
    L.over(cov(side + 3), vgrad(L, h * 0.3, h * 0.8, hexc('#B9A2D6'), hexc('#6E5A92')))
    top = L.ellipse(cx, h * 0.4, w * 0.42, h * 0.24)
    L.over(cov(top), INK)
    L.over(cov(top + 3), radial(L, cx, h * 0.34, w * 0.42, hexc('#F4ECFF'), hexc('#BFAEE0')))
    L.over(band(top + 3, -3, -0.5) * (L.y < h * 0.36), (255, 255, 255, 200))
    return L.image()


def card_back(w=120, h=160, r=18):
    """Summon reveal card back: purple with a gold rim and a diamond lattice (the star emblem is a separate image)."""
    L = Layer(w, h)
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    L.over(cov(L.rrect(3, 3, w - 3, h - 3, r - 3)), vgrad(L, 3, h - 3, GOLD_HI, GOLD_DK))
    inner = L.rrect(9, 9, w - 9, h - 9, r - 8)
    L.over(cov(inner), vgrad(L, 9, h - 9, hexc('#7B55E0'), hexc('#3B2690')))
    lattice = (np.abs(((L.x + L.y) / 16.0) % 1 - 0.5) < 0.06) | (np.abs(((L.x - L.y) / 16.0) % 1 - 0.5) < 0.06)
    L.over(lattice.astype(np.float32) * cov(inner + 4), (255, 255, 255, 28))
    L.over(band(inner, -2, 0), hexc('#2A1A66'))
    L.over(band(inner, -4, -2) * (L.y < h * 0.4), (200, 180, 255, 120))
    return L.image()


def card_emblem(size=96):
    """Gold star with a glow for the middle of the card back."""
    L = Layer(size, size)
    c = size / 2
    L.over(np.clip(1 - np.sqrt((L.x - c) ** 2 + (L.y - c) ** 2) / c, 0, 1) ** 2, (255, 230, 150, 140))
    star = L.polygon([(c + np.cos(-np.pi / 2 + k * np.pi / 5) * (size * 0.34 if k % 2 == 0 else size * 0.15),
                       c + np.sin(-np.pi / 2 + k * np.pi / 5) * (size * 0.34 if k % 2 == 0 else size * 0.15)) for k in range(10)])
    L.over(cov(star - 3), INK)
    L.over(cov(star), vgrad(L, c - size * 0.34, c + size * 0.3, GOLD_HI, GOLD))
    L.over(cov(L.circle(c - size * 0.06, c - size * 0.1, size * 0.045)), (255, 255, 255, 220))
    return L.image()


def card_face(w=120, h=160, r=18):
    """Summon reveal card face: white card (tinted by grade in game) with a lighter window for the icon."""
    L = Layer(w, h)
    L.over(cov(L.rrect(0, 0, w, h, r)), INK)
    body = L.rrect(3, 3, w - 3, h - 3, r - 3)
    L.over(cov(body), vgrad(L, 3, h - 3, hexc('#FFFFFF'), hexc('#D8D8D8')))
    L.over(band(body, -3, -0.5) * (L.y < h * 0.4), (255, 255, 255, 220))
    win = L.rrect(14, 30, w - 14, h * 0.66, 12)
    L.over(cov(win), hexc('#FFFFFF', 150))
    return L.image()


def boss_band(w=96, h=200):
    """Boss warning band: gold-trimmed crimson strip with faint warning stripes (stretched across the screen)."""
    L = Layer(w, h)
    L.over(np.ones((h, w), np.float32), vgrad3(L, 0, h, hexc('#A0203A'), hexc('#6A0E22'), hexc('#3A0614'), 0.5))
    stripes = (((L.x + L.y) / 18.0) % 1.0) < 0.5
    L.over(stripes.astype(np.float32) * ((L.y > 22) & (L.y < h - 22)), (255, 255, 255, 14))
    for top in (True, False):
        y0 = 0 if top else h - 18
        L.over(((L.y >= y0) & (L.y < y0 + 18)).astype(np.float32), INK)
        L.over(((L.y >= y0 + 4) & (L.y < y0 + 14)).astype(np.float32), vgrad(L, y0 + 4, y0 + 14, GOLD_HI, GOLD_DK))
    L.over(((L.y >= 18) & (L.y < 24)).astype(np.float32), (255, 120, 140, 110))
    return L.image()


def glow(size=256, power=1.8):
    """Smooth radial light (white, tinted by the Image colour)."""
    L = Layer(size, size)
    c = size / 2
    t = np.clip(1 - np.sqrt((L.x - c) ** 2 + (L.y - c) ** 2) / c, 0, 1) ** power
    L.over(t, WHITE)
    return L.image()


def summon_circle(size=512):
    """Magic summoning circle: glowing rings, a hexagram, rune dots and a soft core (white; tinted in game)."""
    L = Layer(size, size)
    c = size / 2
    rr = np.sqrt((L.x - c) ** 2 + (L.y - c) ** 2)
    ang = np.arctan2(L.y - c, L.x - c)
    halo = np.clip(1 - rr / (c * 0.98), 0, 1) ** 2.2
    L.over(halo, (255, 255, 255, 90))
    for (r, w, a) in [(0.94, 5, 255), (0.86, 2.5, 220), (0.62, 3.5, 255), (0.54, 2, 200), (0.2, 3, 230)]:
        d = np.abs(rr - c * r) - w / 2
        L.over(cov(d), (255, 255, 255, a))
        L.over(np.clip(1 - np.abs(rr - c * r) / (w * 3.5), 0, 1) ** 2, (255, 255, 255, 70))
    # Rune ticks between the outer rings.
    ticks = (np.abs(((ang / (2 * np.pi) * 48) % 1) - 0.5) < 0.12) & (rr > c * 0.87) & (rr < c * 0.93)
    L.over(ticks.astype(np.float32), (255, 255, 255, 230))
    # Rune dots between the middle rings.
    for k in range(12):
        a = k * np.pi / 6 + np.pi / 12
        L.over(cov(L.circle(c + np.cos(a) * c * 0.74, c + np.sin(a) * c * 0.74, size * 0.018)), (255, 255, 255, 255))
    # Hexagram from two triangles.
    for rot in (0, np.pi / 3):
        pts = [(c + np.cos(rot + k * 2 * np.pi / 3 - np.pi / 2) * c * 0.6, c + np.sin(rot + k * 2 * np.pi / 3 - np.pi / 2) * c * 0.6) for k in range(3)]
        for i in range(3):
            ax, ay = pts[i]
            bx, by = pts[(i + 1) % 3]
            dx, dy = bx - ax, by - ay
            n = (dx * dx + dy * dy) ** 0.5
            nx, ny = -dy / n * 1.8, dx / n * 1.8
            d = L.polygon([(ax + nx, ay + ny), (bx + nx, by + ny), (bx - nx, by - ny), (ax - nx, ay - ny)])
            L.over(cov(d), (255, 255, 255, 235))
    core = np.clip(1 - rr / (c * 0.35), 0, 1) ** 1.5
    L.over(core, (255, 255, 255, 170))
    return L.image()



def make():
    for tone in TONES:
        save(button(tone), 'hd9_btn%s_30' % tone)
        save(button(tone, pressed=True), 'hd9_btnp%s_30' % tone)
    save(panel(), 'hd9_panel_44')
    save(window(), 'hd9_window_52')
    save(card(), 'hd9_card_26')
    save(card(fill=(hexc('#FFFFFF'), hexc('#F6F0FF')), edge=hexc('#CFC2EE'), lip=hexc('#B8A8DE')), 'hd9_cardlilac_26')
    save(inset(), 'hd9_inset_22')
    save(inset(fill=hexc('#2A2547', 235), shadow=hexc('#15122A')), 'hd9_insetdark_22')
    for tone in ('gold', 'blue', 'purple', 'green', 'red', 'gray', 'orange'):
        save(plate(tone), 'hd9_plate%s_24' % tone)
    save(ribbon(), 'hd9_ribbon_44x30')
    save(hudbar(), 'hd9_hudbar_20')
    save(pill(), 'hd9_pill_27')
    save(gauge(), 'hd9_gauge_15')
    save(gauge_light(), 'hd9_gaugelight_15')
    save(fill(hexc('#FF8A8A'), hexc('#E0303F')), 'hd9_fillred_12')
    save(fill(hexc('#8FE1FF'), hexc('#2F8BEA')), 'hd9_fillblue_12')
    save(fill(hexc('#FFE98A'), hexc('#F5A512')), 'hd9_fillgold_12')
    save(fill(hexc('#A8F58A'), hexc('#35B04A')), 'hd9_fillgreen_12')
    save(fill(hexc('#E2B8FF'), hexc('#8E52EC')), 'hd9_fillpurple_12')
    save(tabbar(), 'hd9_tabbar_10')
    save(tab_active(), 'hd9_tabactive_30')
    save(avatar_ring(), 'hd_avatar_ring')
    save(avatar_disc(), 'hd_avatar_disc')
    save(level_badge(), 'hd9_lvbadge_19')
    save(dot(), 'hd_dot')
    for tone in ('gold', 'purple', 'blue', 'green', 'red'):
        save(round_button(tone), 'hd_round%s' % tone)
    save(rail_tile(), 'hd9_railtile_34')
    save(caption(), 'hd9_caption_17')
    for key in GRADES:
        save(slot(key), 'hd9_slot%s_28' % key)
    save(slot_empty(), 'hd9_slotempty_28')
    save(slot_dark(), 'hd9_slotdark_28')
    save(select_ring(), 'hd9_select_36')
    save(lock_veil(), 'hd9_veil_20')
    save(close_button(), 'hd_close')
    save(boss_band(), 'hd9_bossband_30x26')
    save(pedestal(), 'hd_pedestal')
    save(card_back(), 'hd9_cardback_30')
    save(card_emblem(), 'hd_card_emblem')
    save(card_face(), 'hd9_cardface_30')
    save(glow(), 'hd_glow')
    save(summon_circle(), 'hd_summon_circle')


if __name__ == '__main__':
    make()
    if PREVIEW:
        pad = 16
        cols = 8
        cw = max(i.width for _, i in made) + pad
        ch = max(i.height for _, i in made) + pad + 14
        rows = (len(made) + cols - 1) // cols
        sheet = Image.new('RGBA', (cols * cw, rows * ch), (120, 160, 120, 255))
        for k, (_, im) in enumerate(made):
            sheet.alpha_composite(im, ((k % cols) * cw + 8, (k // cols) * ch + 8))
        sheet.save(PREVIEW)
    print('made', len(made), 'sprites in', os.path.abspath(OUT))
