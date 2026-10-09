"""D-150 crisper character colours.

The 3D characters (chibi3d.py) shade every material with a 3-tone ramp (shadow, base, light). Many ramps were close
together and pale, which read as soft and washed out on the green battlefields. punch_ramp() spreads each ramp in
OKLab: the shadow drops further below the base and turns slightly cooler while keeping its colour, the base gains a
little chroma, and a near-white highlight on a coloured material keeps some of its hue and turns slightly warmer
(classic pixel-art hue shifting). One-colour entries (eyes, blush, glows, smears) are left alone.

pixelize.py maps renders through it, and repalette.py recolours existing sheets with color_map(), which picks the
same ramp per colour as pixelize (the first material that lists it).
"""
import math

SHADOW_GAP = 1.45  # shadow-to-base lightness gap grows by this factor ...
SHADOW_MIN_GAP = 0.13  # ... and is at least this much OKLab lightness (less on dark materials, see below)
SHADOW_CHROMA = 1.05
SHADOW_GAMUT_MARGIN = 0.88  # a shadow pushed past the gamut stays this far inside it (edge colours are harsh primaries)
SHADOW_HUE = 8.0  # degrees toward blue-violet
SHADOW_HUE_TARGET = 290.0
BASE_CHROMA = 1.14
LIGHT_KEEP = 0.38  # a near-white highlight keeps this share of the base chroma
LIGHT_HUE = 8.0  # degrees toward yellow
LIGHT_HUE_TARGET = 100.0
MAX_CHROMA = 0.32


def _lin(c):
    c /= 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _srgb(c):
    c = c * 12.92 if c <= 0.0031308 else 1.055 * (c ** (1 / 2.4)) - 0.055
    return c * 255.0


def to_oklab(rgb):
    r, g, b = (_lin(float(v)) for v in rgb[:3])
    l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b
    m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b
    s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b
    l, m, s = (math.copysign(abs(v) ** (1 / 3), v) for v in (l, m, s))
    return (0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s)


def from_oklab(lab):
    L, a, b = lab
    l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3
    m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3
    s = (L - 0.0894841775 * a - 1.2914855480 * b) ** 3
    r = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s
    g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s
    bb = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s
    return tuple(_srgb(v) for v in (r, g, bb))


def to_lch(rgb):
    L, a, b = to_oklab(rgb)
    return L, math.hypot(a, b), math.degrees(math.atan2(b, a)) % 360.0


def from_lch(L, C, h, margin=1.0):
    """sRGB (0-255 ints) for OKLCh, with the chroma reduced to the largest that fits the gamut (times margin)."""
    def rgb_at(c):
        return from_oklab((L, c * math.cos(math.radians(h)), c * math.sin(math.radians(h))))

    def inside(rgb):
        return all(-0.5 <= v <= 255.5 for v in rgb)

    rgb = rgb_at(C)
    if not inside(rgb):
        lo, hi = 0.0, C
        for _ in range(30):
            mid = (lo + hi) / 2
            if inside(rgb_at(mid)):
                lo = mid
            else:
                hi = mid
        rgb = rgb_at(lo * margin)
    return tuple(int(round(min(255.0, max(0.0, v)))) for v in rgb)


def _toward(h, target, step):
    d = (target - h + 540.0) % 360.0 - 180.0
    return (h + max(-step, min(step, d))) % 360.0


def hexc(s):
    s = s.lstrip("#")
    return int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16)


def punch_ramp(hexes):
    """The punched colours (sRGB tuples) of one darkest-first ramp; one-colour entries come back unchanged."""
    rgbs = [hexc(h) for h in hexes]
    if len(rgbs) < 3:
        return rgbs
    (Ls, Cs, hs), (Lb, Cb, hb), (Ll, Cl, hl) = (to_lch(c) for c in rgbs[:3])
    base_c = min(MAX_CHROMA, Cb * BASE_CHROMA)
    # Dark materials (black cloth, basalt) cannot drop much further without turning into the outline.
    min_gap = SHADOW_MIN_GAP * min(1.0, max(0.35, (Lb - 0.25) / 0.45))
    gap = max(min_gap, (Lb - Ls) * SHADOW_GAP)
    shadow_l = max(0.18, Lb - gap)
    shadow_c = min(MAX_CHROMA, max(Cs * SHADOW_CHROMA, base_c * 0.75))
    shadow_h = _toward(hs if Cs > 0.02 else hb, SHADOW_HUE_TARGET, SHADOW_HUE) if Cs > 0.02 or Cb > 0.02 else hs
    if Cb > 0.06 and Cl < Cb * LIGHT_KEEP:
        light_c = Cb * LIGHT_KEEP
        light_h = _toward(hb, LIGHT_HUE_TARGET, LIGHT_HUE)
        light_l = min(Ll, 0.965)
    else:
        light_c, light_h, light_l = Cl, (_toward(hl, LIGHT_HUE_TARGET, LIGHT_HUE) if Cl > 0.02 else hl), Ll
    out = [from_lch(shadow_l, shadow_c, shadow_h, SHADOW_GAMUT_MARGIN), from_lch(Lb, base_c, hb),
           from_lch(light_l, light_c, light_h)]
    return out + rgbs[3:]


def color_map(palette):
    """{old sRGB: new sRGB} over a {material: darkest-first hexes} palette; the first material listing a colour wins."""
    mapping = {}
    for hexes in palette.values():
        new = punch_ramp(hexes)
        for old_hex, rgb in zip(hexes, new):
            old = hexc(old_hex)
            if old not in mapping:
                mapping[old] = tuple(rgb)
    return mapping
