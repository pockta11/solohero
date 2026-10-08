"""Smooth equipment icons (D-108) -> Assets/SoloHero/Art/UI/Hd/Equipment/equip_{slot}_{grade}.png (128 x 128).

Usage: python tools/art/equipgen3.py [OUT_DIR] [SLOT ...] [--preview PREVIEW.png]
Eight slots (D-109: gloves, necklace, ring and earring after the gear) x seven grades (D-113), plus
the staff and the bow that replace the sword picture for mages and archers (D-140). Each grade changes
material and ornament, not just the hue: common = iron and leather, uncommon = the same shapes in green-trimmed steel,
rare = polished steel with blue trim, epic = violet steel with gems, legendary = gold with wings, a ruby and a glint,
mythic = the legendary shapes in crimson with gold gems, ancient = the legendary shapes in jade with golden wings.
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from uikit import Layer, band, cov, hexc, shade, vgrad  # noqa: E402
from icongen3 import (BLUE, BROWN, GOLD, INK, OW, PURPLE, RED, SILVER, fill, gloss, inter, outline, rot, rrect_at,  # noqa: E402
                      seg, star_pts, union)

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art', 'UI', 'Hd', 'Equipment')
SLOT_NAMES = ('sword', 'helm', 'armor', 'boots', 'gloves', 'necklace', 'ring', 'earring', 'staff', 'bow')
args = [a for i, a in enumerate(sys.argv[1:], 1)
        if not a.startswith('--') and sys.argv[i - 1] != '--preview' and a not in SLOT_NAMES]
if args:
    OUT = args[0]
PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

S = 128
IRON = (hexc('#E4E8EE'), hexc('#A4ACB8'), hexc('#5E6676'))
STEEL = (hexc('#FFFFFF'), hexc('#C8DCF6'), hexc('#6E86B4'))
VIOLET = (hexc('#F4DCFF'), hexc('#B98AF0'), hexc('#6A3CB8'))
GOLDEN = (hexc('#FFF8C8'), hexc('#FFCF4A'), hexc('#C47A10'))
LEATHER = (hexc('#E8B888'), hexc('#A8703E'), hexc('#5E3A1E'))
PAD_LEATHER = (hexc('#D8A878'), hexc('#8E5A30'), hexc('#4E2E14'))
GRADES = ['common', 'uncommon', 'rare', 'epic', 'legendary', 'mythic', 'ancient']
AURA_L = (255, 220, 120, 150)

made = []


def new():
    return Layer(S, S)


def save(img, slot, grade):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, 'equip_%s_%s.png' % (slot, grade)))
    made.append(('%s_%s' % (slot, grade), img))


def gem(L, cx, cy, r, ramp):
    d = L.polygon([(cx, cy - r), (cx + r * 0.85, cy), (cx, cy + r), (cx - r * 0.85, cy)])
    L.over(cov(d - 3), INK)
    fill(L, d, ramp, hi=False)
    L.over(cov(L.circle(cx - r * 0.25, cy - r * 0.3, r * 0.22)), (255, 255, 255, 230))


def sparkle(L, cx, cy, s):
    L.over(cov(L.polygon(star_pts(cx, cy, s, s * 0.28, 4, 0)) - 1.5), INK[:3] + (120,))
    L.over(cov(L.polygon(star_pts(cx, cy, s, s * 0.28, 4, 0))), (255, 255, 255, 255))


# Sword --------------------------------------------------------------------------------------------------------------
def sword_icon(grade):
    L = new()
    blade_ramp = {'common': IRON, 'rare': STEEL, 'epic': VIOLET, 'legendary': GOLDEN}[grade]
    guard_ramp = {'common': (hexc('#9AA0AC'), hexc('#6A707C'), hexc('#3E4250')), 'rare': GOLD, 'epic': PURPLE, 'legendary': GOLD}[grade]
    grip_ramp = {'common': LEATHER, 'rare': BLUE, 'epic': (hexc('#6A4AA0'), hexc('#3E2A70'), hexc('#24163E')), 'legendary': RED}[grade]
    ax, ay, bx, by = 26, 104, 108, 22
    ang = math.atan2(by - ay, bx - ax)
    ln = math.hypot(bx - ax, by - ay)
    x, y = rot(L, ax, ay, ang)
    wide = 1.15 if grade == 'legendary' else 1.0
    b0 = ax + ln * 0.34
    blade = rrect_at(x, y, b0, ay - 9.5 * wide, ax + ln - 16, ay + 9.5 * wide, 2)
    k = 9.5 * wide / 16.0
    tip = np.maximum(np.maximum((y - ay) - (ax + ln - x) * k, -(y - ay) - (ax + ln - x) * k), (ax + ln - 16) - x - 0.5)
    blade = np.minimum(blade, tip)
    if grade in ('common', 'rare'):
        guard = rrect_at(x, y, ax + ln * 0.28, ay - 21, ax + ln * 0.36, ay + 21, 4)
    else:
        # Swept guard: two curved horns.
        guard = rrect_at(x, y, ax + ln * 0.27, ay - 12, ax + ln * 0.36, ay + 12, 5)
        for sgn in (-1, 1):
            hx0, hy0 = ax + ln * 0.315, ay + sgn * 10
            pts = [(ax + ln * 0.27, ay + sgn * 8), (ax + ln * 0.36, ay + sgn * 8), (ax + ln * 0.43, ay + sgn * 26), (ax + ln * 0.38, ay + sgn * 27)]
            guard = np.minimum(guard, L.polygon([rot_pt(p, ax, ay, ang) for p in pts]))
    grip = rrect_at(x, y, ax + 9, ay - 6.5, ax + ln * 0.29, ay + 6.5, 3)
    pommel = np.sqrt((x - (ax + 8)) ** 2 + (y - ay) ** 2) - (10 if grade == 'legendary' else 8.5)
    every = union(blade, guard, grip, pommel)
    if grade == 'legendary':
        aura = np.clip(1 - np.maximum(every, 0) / 16.0, 0, 1) ** 2 * (every > 0)
        L.over(aura, AURA_L)
    if grade == 'epic':
        aura = np.clip(1 - np.maximum(every, 0) / 12.0, 0, 1) ** 2 * (every > 0)
        L.over(aura, (200, 140, 255, 120))
    outline(L, every)
    fill(L, blade, blade_ramp, hi=False)
    ridge = np.maximum(np.abs(y - ay) - 2.2, np.maximum(b0 + 6 - x, x - (ax + ln - 14)))
    L.over(cov(ridge), (255, 255, 255, 210))
    L.over(cov(np.maximum(blade + 1, -(y - ay))), shade(blade_ramp[2], -0.1)[:3] + (70,))
    fill(L, grip, grip_ramp, hi=False)
    for t in (0.45, 0.62, 0.79):
        gx = ax + 9 + (ln * 0.29 - 9) * t
        L.over(cov(rrect_at(x, y, gx - 1, ay - 6.5, gx + 1, ay + 6.5, 1)), shade(grip_ramp[2], -0.2)[:3] + (200,))
    fill(L, guard, guard_ramp)
    fill(L, pommel, guard_ramp)
    if grade == 'epic':
        gem(L, *rot_pt((ax + ln * 0.315, ay), ax, ay, ang), 7, (hexc('#FFD0F8'), hexc('#F050C0'), hexc('#901870')))
    if grade == 'legendary':
        gem(L, *rot_pt((ax + ln * 0.315, ay), ax, ay, ang), 8, RED)
        sparkle(L, 92, 26, 11)
        sparkle(L, 108, 52, 6)
    if grade == 'rare':
        gem(L, *rot_pt((ax + 8, ay), ax, ay, ang), 5, BLUE)
    return L.image()


def rot_pt(p, cx, cy, ang):
    """Point in the rotated frame (x along the blade) back to image space."""
    px, py = p[0] - cx, p[1] - cy
    c, s = math.cos(ang), math.sin(ang)
    return (cx + px * c - py * s, cy + px * s + py * c)


# Helm ---------------------------------------------------------------------------------------------------------------
def helm_icon(grade):
    L = new()
    metal = {'common': IRON, 'rare': STEEL, 'epic': VIOLET, 'legendary': GOLDEN}[grade]
    extras = []
    if grade == 'rare':
        plume = union(L.ellipse(64, 20, 13, 15), L.ellipse(78, 26, 15, 9))
        extras.append((plume, BLUE))
    if grade == 'epic':
        for sgn in (-1, 1):
            horn = L.polygon([(64 + sgn * 24, 52), (64 + sgn * 36, 34), (64 + sgn * 50, 22), (64 + sgn * 60, 4),
                              (64 + sgn * 58, 26), (64 + sgn * 50, 44), (64 + sgn * 36, 62)])
            extras.append((horn, (hexc('#FFF4E0'), hexc('#E8D8B8'), hexc('#A08A68'))))
    if grade == 'legendary':
        for sgn in (-1, 1):
            wing = L.polygon([(64 + sgn * 34, 58), (64 + sgn * 62, 22), (64 + sgn * 58, 40), (64 + sgn * 64, 44), (64 + sgn * 54, 56),
                              (64 + sgn * 60, 62), (64 + sgn * 44, 70)])
            extras.append((wing, WING))
    if grade == 'common':
        dome = union(inter(L.circle(64, 72, 46), L.y - 86.0), rrect_at(L.x, L.y, 16, 76, 112, 96, 10))
    else:
        dome = union(L.circle(64, 64, 42), rrect_at(L.x, L.y, 22, 62, 106, 112, 18))
    every = dome
    for d, _ in extras:
        every = union(every, d)
    outline(L, every)
    for d, ramp in extras:
        fill(L, d, ramp)
    fill(L, dome, metal)
    if grade == 'common':
        rim = rrect_at(L.x, L.y, 16, 80, 112, 96, 8)
        fill(L, rim, (shade(metal[0], -0.05), shade(metal[1], -0.12), shade(metal[2], -0.1)))
        for cx in (30, 52, 76, 98):
            L.over(cov(L.circle(cx, 88, 3.2)), hexc('#E8ECF2'))
    else:
        slit = union(rrect_at(L.x, L.y, 32, 64, 96, 76, 6), rrect_at(L.x, L.y, 57, 64, 71, 104, 6))
        L.over(cov(slit + 2), shade(metal[2], -0.25))
        L.over(cov(slit), hexc('#241B38'))
        if grade == 'legendary':
            gem(L, 64, 40, 9, RED)
            sparkle(L, 100, 24, 9)
        if grade == 'epic':
            gem(L, 64, 42, 8, (hexc('#FFD0F8'), hexc('#F050C0'), hexc('#901870')))
    gloss(L, dome, 44, 44, 14, 9, 130)
    return L.image()


# Armor --------------------------------------------------------------------------------------------------------------
def armor_icon(grade):
    L = new()
    metal = {'common': LEATHER, 'rare': STEEL, 'epic': VIOLET, 'legendary': GOLDEN}[grade]
    trim = {'common': (hexc('#C8A070'), hexc('#8A5A30'), hexc('#4A2E16')), 'rare': GOLD, 'epic': GOLD, 'legendary': RED}[grade]
    body = union(L.polygon([(34, 30), (94, 30), (100, 70), (88, 112), (40, 112), (28, 70)]),
                 rrect_at(L.x, L.y, 40, 22, 88, 40, 8))
    neck = L.ellipse(64, 26, 15, 9)
    pads = [L.ellipse(26, 40, 19, 16), L.ellipse(102, 40, 19, 16)]
    every = union(body, *pads)
    if grade == 'legendary':
        for sgn in (-1, 1):
            wing = L.polygon([(64 + sgn * 46, 34), (64 + sgn * 64, 6), (64 + sgn * 60, 26), (64 + sgn * 64, 30), (64 + sgn * 52, 46)])
            every = union(every, wing)
    outline(L, every)
    if grade == 'legendary':
        for sgn in (-1, 1):
            wing = L.polygon([(64 + sgn * 46, 34), (64 + sgn * 64, 6), (64 + sgn * 60, 26), (64 + sgn * 64, 30), (64 + sgn * 52, 46)])
            fill(L, wing, WING)
    fill(L, body, metal)
    L.over(cov(neck), shade(metal[2], -0.35))
    belt = rrect_at(L.x, L.y, 34, 84, 94, 96, 3)
    fill(L, belt, trim, hi=False)
    L.over(cov(rrect_at(L.x, L.y, 58, 82, 70, 98, 2)), shade(trim[0], 0.2))
    for p in pads:
        fill(L, p, metal if grade != 'common' else PAD_LEATHER)
    if grade == 'common':
        for (x0, y0, x1, y1) in [(46, 46, 49, 76), (79, 46, 82, 76)]:
            L.over(cov(rrect_at(L.x, L.y, x0, y0, x1, y1, 1)), shade(LEATHER[2], -0.1))
    else:
        L.over(cov(seg(L, 64, 40, 64, 80, 1.6)), shade(metal[2], -0.15)[:3] + (200,))
        emblem_ramp = {'rare': BLUE, 'epic': (hexc('#FFD0F8'), hexc('#F050C0'), hexc('#901870')), 'legendary': RED}[grade]
        gem(L, 64, 58, 9 if grade == 'legendary' else 7, emblem_ramp)
        if grade == 'legendary':
            sparkle(L, 100, 76, 9)
    gloss(L, body, 50, 46, 12, 8, 110)
    return L.image()


# Boots --------------------------------------------------------------------------------------------------------------
def boot_shape(L, x0, y0):
    leg = rrect_at(L.x, L.y, x0, y0, x0 + 26, y0 + 58, 8)
    foot = rrect_at(L.x, L.y, x0, y0 + 40, x0 + 44, y0 + 66, 12)
    return union(leg, foot)


def boots_icon(grade):
    L = new()
    metal = {'common': LEATHER, 'rare': STEEL, 'epic': VIOLET, 'legendary': GOLDEN}[grade]
    trim = {'common': (hexc('#C8A070'), hexc('#7A4A24'), hexc('#3E2410')), 'rare': GOLD, 'epic': GOLD, 'legendary': WING}[grade]
    back = boot_shape(L, 54, 30)
    front = boot_shape(L, 24, 44)
    every = union(back, front)
    wings = []
    if grade in ('epic', 'legendary'):
        for (wx, wy) in ((50, 54), (80, 40)):
            w = L.polygon([(wx, wy), (wx + 26, wy - 22), (wx + 22, wy - 8), (wx + 30, wy - 8), (wx + 18, wy + 6)])
            wings.append(w)
            every = union(every, w)
    outline(L, every)
    for w in wings:
        fill(L, w, WING)
    for d, dim in ((back, -0.18), (front, 0.0)):
        ramp = tuple(shade(c, dim) for c in metal)
        fill(L, d, ramp)
    for (x0, y0) in ((54, 30), (24, 44)):
        cuff = rrect_at(L.x, L.y, x0 - 3, y0, x0 + 29, y0 + 12, 5)
        fill(L, cuff, trim, hi=False)
        sole = rrect_at(L.x, L.y, x0, y0 + 60, x0 + 44, y0 + 66, 3)
        L.over(cov(sole), shade(metal[2], -0.35))
    if grade == 'legendary':
        sparkle(L, 104, 22, 9)
        gem(L, 37, 50, 6, RED)
    if grade == 'rare':
        gem(L, 37, 50, 5, BLUE)
    return L.image()


# D-109 accessories ---------------------------------------------------------------------------------------------------
PINK = (hexc('#FFD0F8'), hexc('#F050C0'), hexc('#901870'))
WING = (hexc('#FFFFFF'), hexc('#E4ECFF'), hexc('#98A8D0'))
BRONZE = (hexc('#F4CC90'), hexc('#C08A4A'), hexc('#6E4622'))


def metal_of(grade, common=None):
    # Looked up at call time (not as a default argument) so the D-113 palette swaps reach it.
    return {'common': common if common is not None else IRON, 'rare': STEEL, 'epic': VIOLET, 'legendary': GOLDEN}[grade]


def gem_of(grade):
    return {'common': None, 'rare': BLUE, 'epic': PINK, 'legendary': RED}[grade]


def wing_pair(L, cx, cy, span, up=True):
    """Two small feathered wings either side of (cx, cy); returns their union."""
    out = None
    for sgn in (-1, 1):
        sy = -1 if up else 1
        w = L.polygon([(cx + sgn * span * 0.25, cy), (cx + sgn * span, cy + sy * span * 0.75), (cx + sgn * span * 0.82, cy + sy * span * 0.3),
                       (cx + sgn * span * 0.98, cy + sy * span * 0.22), (cx + sgn * span * 0.6, cy - sy * span * 0.1)])
        out = w if out is None else union(out, w)
    return out


# Gloves -------------------------------------------------------------------------------------------------------------
def gloves_icon(grade):
    L = new()
    metal = metal_of(grade, LEATHER)
    trim = {'common': (hexc('#C8A070'), hexc('#7A4A24'), hexc('#3E2410')), 'rare': GOLD, 'epic': GOLD, 'legendary': RED}[grade]
    palm = rrect_at(L.x, L.y, 36, 52, 92, 98, 16)
    fingers = [seg(L, 45, 56, 43, 26, 7.5), seg(L, 58, 54, 58, 18, 7.5), seg(L, 71, 54, 73, 21, 7.5), seg(L, 83, 58, 88, 32, 7)]
    thumb = seg(L, 40, 84, 22, 60, 8)
    cuff = rrect_at(L.x, L.y, 32, 94, 96, 118, 7)
    hand = union(palm, thumb, *fingers)
    every = union(hand, cuff)
    wings = None
    if grade == 'legendary':
        wings = wing_pair(L, 64, 104, 50, up=True)
        every = union(every, wings)
    outline(L, every)
    if wings is not None:
        fill(L, wings, WING)
    fill(L, hand, metal)
    # Finger creases / plate joints.
    joint = shade(metal[2], -0.2)[:3] + (190,)
    for fx, fy0, fy1 in ((44, 40, 41), (58, 34, 35), (72, 36, 37), (86, 44, 45)):
        L.over(cov(seg(L, fx - 6, fy0, fx + 6, fy1, 1.2)), joint)
    if grade == 'common':
        for t in range(5):
            sx = 44 + t * 10
            L.over(cov(seg(L, sx, 88, sx + 5, 88, 1.0)), hexc('#F2D2A6'))
    else:
        knuckles = rrect_at(L.x, L.y, 40, 52, 90, 64, 6)
        fill(L, knuckles, tuple(shade(c, 0.08) for c in metal), hi=False)
        L.over(band(knuckles, -1.5, 0), shade(metal[2], -0.25)[:3] + (200,))
    fill(L, cuff, trim, hi=False)
    L.over(cov(rrect_at(L.x, L.y, 32, 94, 96, 99, 3)), shade(trim[0], 0.25)[:3] + (200,))
    g = gem_of(grade)
    if g is not None:
        gem(L, 64, 76, 9 if grade == 'legendary' else 7, g)
    if grade == 'legendary':
        sparkle(L, 104, 26, 10)
        sparkle(L, 22, 34, 6)
    gloss(L, palm, 52, 64, 10, 7, 110)
    return L.image()


# Necklace -----------------------------------------------------------------------------------------------------------
def necklace_icon(grade):
    L = new()
    chain_ramp = {'common': IRON, 'rare': STEEL, 'epic': GOLD, 'legendary': GOLDEN}[grade]
    setting = {'common': BRONZE, 'rare': STEEL, 'epic': GOLD, 'legendary': GOLDEN}[grade]
    cx, cy, rx, ry = 64, 46, 42, 38
    # The loop: the back half (top) is a thin dark line, the front half carries the beads.
    back_chain = inter(np.abs(L.ellipse(cx, cy, rx, ry * 0.55)) - 2.0, L.y - cy)
    chain = inter(np.abs(L.ellipse(cx, cy, rx, ry)) - 3.2, cy - L.y)
    beads = None
    for k in range(9):
        a = math.pi * (0.04 + 0.92 * k / 8.0)
        bx, by = cx - math.cos(a) * rx, cy + math.sin(a) * ry
        b = L.circle(bx, by, 5.4)
        beads = b if beads is None else union(beads, b)
    L.over(cov(back_chain - 3), INK[:3] + (150,))
    L.over(cov(back_chain), shade(chain_ramp[2], -0.25))
    big = grade in ('epic', 'legendary')
    pend_y = 98
    if grade == 'common':
        pendant = L.circle(64, pend_y, 15)
    elif grade == 'rare':
        pendant = union(L.circle(64, pend_y + 4, 14), L.polygon([(64, pend_y - 22), (78, pend_y), (50, pend_y)]))
    else:
        pendant = L.polygon(star_pts(64, pend_y, 26 if grade == 'legendary' else 22, 13 if grade == 'legendary' else 11, 8, -math.pi / 2))
        pendant = union(pendant, L.circle(64, pend_y, 15))
    bail = rrect_at(L.x, L.y, 58, pend_y - 30, 70, pend_y - 18, 4)
    every = union(chain, beads, pendant, bail)
    outline(L, every, 5.0)
    L.over(cov(chain), chain_ramp[2])
    fill(L, beads, chain_ramp, hi=False)
    fill(L, bail, setting, hi=False)
    fill(L, pendant, setting)
    g = gem_of(grade)
    if g is None:
        L.over(cov(L.circle(64, pend_y, 9)), shade(BRONZE[2], -0.1))
        L.over(cov(L.polygon(star_pts(64, pend_y, 7, 3, 5))), BRONZE[0])
    else:
        gem(L, 64, pend_y + (2 if grade == 'rare' else 0), 12 if big else 9, g)
    if grade == 'legendary':
        sparkle(L, 102, 80, 10)
        sparkle(L, 26, 98, 6)
    return L.image()


# Ring ---------------------------------------------------------------------------------------------------------------
def ring_icon(grade):
    L = new()
    metal = metal_of(grade, BRONZE)
    cx, cy = 64, 80
    outer = L.ellipse(cx, cy, 36, 32)
    hole = L.ellipse(cx, cy + 3, 25, 21)
    band_d = inter(outer, -hole)
    head = None
    g = gem_of(grade)
    if g is not None:
        r = {'rare': 13, 'epic': 15, 'legendary': 18}[grade]
        head = union(L.circle(64, 44, r + 5), rrect_at(L.x, L.y, 52, 44, 76, 60, 6))
    wings = None
    if grade == 'legendary':
        wings = wing_pair(L, 64, 50, 46)
    every = band_d if head is None else union(band_d, head)
    if wings is not None:
        every = union(every, wings)
    outline(L, every)
    if wings is not None:
        fill(L, wings, WING)
    fill(L, band_d, metal)
    # Inner shadow on the far side of the band.
    L.over(cov(inter(band_d, L.y - (cy - 4))) * 0.35, shade(metal[2], -0.3)[:3] + (255,))
    if head is not None:
        fill(L, head, tuple(shade(c, 0.05) for c in metal), hi=False)
        for sgn in (-1, 1):
            L.over(cov(seg(L, 64 + sgn * 12, 34, 64 + sgn * 8, 52, 2.4)), shade(metal[0], 0.1))
        gem(L, 64, 44, {'rare': 12, 'epic': 14, 'legendary': 16}[grade], g)
    else:
        for t in (-14, 0, 14):
            L.over(cov(L.circle(cx + t, cy + 26, 2.6)), shade(metal[0], 0.15))
    gloss(L, band_d, 44, 66, 9, 6, 120)
    if grade == 'legendary':
        sparkle(L, 104, 22, 10)
        sparkle(L, 24, 30, 6)
    return L.image()


# Earring ------------------------------------------------------------------------------------------------------------
EAR_SIZE = {'common': 9, 'rare': 12, 'epic': 13, 'legendary': 15}


def one_earring(L, x, y, grade, dim):
    """A hoop with a clasp on top and a bead or teardrop gem hanging from its bottom."""
    metal = tuple(shade(c, dim) for c in metal_of(grade))
    r = 17
    hoop = np.abs(L.circle(x, y + r + 4, r)) - 3.4
    clasp = rrect_at(L.x, L.y, x - 4, y - 4, x + 4, y + 8, 3)
    size = EAR_SIZE[grade]
    top = y + 2 * r + 6
    if grade == 'common':
        drop = L.circle(x, top + size - 2, size)
    else:
        drop = union(L.circle(x, top + size * 1.2, size),
                     L.polygon([(x, top - 5), (x + size * 0.95, top + size * 1.1), (x - size * 0.95, top + size * 1.1)]))
    link = seg(L, x, top - 8, x, top, 2.6)
    cap = L.circle(x, top - 1, 5) if grade != 'common' else None
    parts = [hoop, clasp, link, drop] + ([cap] if cap is not None else [])
    return union(*parts), (hoop, clasp, link, drop, cap, metal, (x, top, size))


def earring_icon(grade):
    L = new()
    back, back_parts = one_earring(L, 86, 18, grade, -0.18)
    front, front_parts = one_earring(L, 46, 28, grade, 0.0)
    wings = wing_pair(L, 46, 70, 40, up=True) if grade == "legendary" else None
    every = union(back, front) if wings is None else union(back, front, wings)
    outline(L, every, 5.0)
    if wings is not None:
        fill(L, wings, WING)
    g = gem_of(grade)
    for parts, dim in ((back_parts, -0.18), (front_parts, 0.0)):
        hoop, clasp, link, drop, cap, metal, (x, top, size) = parts
        fill(L, hoop, metal, hi=False)
        L.over(band(hoop, -3.4, -1.6) * (L.y < top - 2 * 17), (255, 255, 255, 120))
        fill(L, clasp, metal, hi=False)
        L.over(cov(link), metal[2])
        fill(L, drop, metal if g is None else tuple(shade(c, dim) for c in g))
        if cap is not None:
            fill(L, cap, metal, hi=False)
        oy = top + (size - 2 if grade == 'common' else size * 1.2)
        L.over(cov(L.circle(x - size * 0.35, oy - size * 0.35, size * 0.26)), (255, 255, 255, 220))
    if grade == 'legendary':
        sparkle(L, 106, 96, 10)
        sparkle(L, 20, 106, 6)
    return L.image()


# Staff and bow (D-140: the weapon slot of mages and archers) ---------------------------------------------------------
def circ(x, y, cx, cy, r):
    return np.sqrt((x - cx) ** 2 + (y - cy) ** 2) - r


def seg_xy(x, y, ax, ay, bx, by, w):
    """Capsule distance in an arbitrary (rotated) frame."""
    px, py = x - ax, y - ay
    ex, ey = bx - ax, by - ay
    t = np.clip((px * ex + py * ey) / (ex * ex + ey * ey), 0, 1)
    dx, dy = px - ex * t, py - ey * t
    return np.sqrt(dx * dx + dy * dy) - w


def rot_pt_frame(px, py, ang, cx=64, cy=64):
    """A point of the frame rot(L, cx, cy, ang) draws in, back to image space."""
    c, s_ = math.cos(ang), math.sin(ang)
    dx, dy = px - cx, py - cy
    return (cx + dx * c - dy * s_, cy + dx * s_ + dy * c)


PINK_ORB = (hexc('#FFD0F8'), hexc('#F050C0'), hexc('#901870'))


def staff_icon(grade):
    L = new()
    shaft_ramp = {'common': LEATHER, 'rare': (hexc('#C89870'), hexc('#7A4E2E'), hexc('#43281A')), 'epic': VIOLET,
                  'legendary': GOLDEN}[grade]
    holder_ramp = {'common': IRON, 'rare': STEEL, 'epic': GOLD, 'legendary': GOLDEN}[grade]
    orb_ramp = {'common': (hexc('#E8F4FF'), hexc('#9CC8F0'), hexc('#4A78B0')), 'rare': BLUE, 'epic': PINK_ORB,
                'legendary': RED}[grade]
    ox, oy = 88, 38
    orr = 17 if grade == 'legendary' else 15
    shaft = seg(L, 22, 110, ox - 10, oy + 12, 5.5)
    orb = L.circle(ox, oy, orr)
    collar = seg(L, ox - 18, oy + 20, ox - 9, oy + 11, 7.5)
    holder = collar if grade == 'common' else union(np.abs(L.circle(ox, oy, orr + 3)) - 3.2, collar)
    every = union(shaft, orb, holder)
    wings = None
    if grade == 'legendary':
        wings = wing_pair(L, ox, oy + 8, 34, up=True)
        every = union(every, wings)
    if grade in ('epic', 'legendary'):
        reach = 16.0 if grade == 'legendary' else 12.0
        aura = np.clip(1 - np.maximum(every, 0) / reach, 0, 1) ** 2 * (every > 0)
        L.over(aura, AURA_L if grade == 'legendary' else (200, 140, 255, 120))
    outline(L, every)
    if wings is not None:
        fill(L, wings, WING)
    fill(L, shaft, shaft_ramp, hi=False)
    for t in (0.16, 0.27):
        gx, gy = 22 + (ox - 10 - 22) * t, 110 + (oy + 12 - 110) * t
        L.over(cov(seg(L, gx - 5, gy - 5, gx + 5, gy + 5, 2.0) - 0.5), shade(shaft_ramp[2], -0.2)[:3] + (220,))
    fill(L, holder, holder_ramp)
    fill(L, orb, orb_ramp, hi=False)
    L.over(cov(L.circle(ox - orr * 0.35, oy - orr * 0.35, orr * 0.3)), (255, 255, 255, 220))
    if grade == 'rare':
        gem(L, 34, 98, 5, BLUE)
    if grade == 'epic':
        sparkle(L, 110, 18, 8)
    if grade == 'legendary':
        sparkle(L, 112, 16, 11)
        sparkle(L, 22, 44, 7)
    return L.image()


def bow_icon(grade):
    L = new()
    limb_ramp = {'common': LEATHER, 'rare': STEEL, 'epic': VIOLET, 'legendary': GOLDEN}[grade]
    grip_ramp = {'common': PAD_LEATHER, 'rare': BLUE, 'epic': (hexc('#6A4AA0'), hexc('#3E2A70'), hexc('#24163E')),
                 'legendary': RED}[grade]
    tip_ramp = {'common': BRONZE, 'rare': GOLD, 'epic': GOLD, 'legendary': GOLDEN}[grade]
    ang = math.radians(-40)
    x, y = rot(L, 64, 64, ang)
    cx, cy, R = 92, 64, 56
    limb = np.maximum(np.abs(circ(x, y, cx, cy, R)) - 5.5, x - 66)
    tip_y = math.sqrt(R * R - (cx - 66) ** 2)
    tips = union(circ(x, y, 66, cy - tip_y, 6.5), circ(x, y, 66, cy + tip_y, 6.5))
    grip = rrect_at(x, y, cx - R - 8, cy - 13, cx - R + 8, cy + 13, 5)
    string = seg_xy(x, y, 66, cy - tip_y, 66, cy + tip_y, 1.6)
    parts = [limb, tips, grip]
    arrow = None
    if grade != 'common':
        shaft = seg_xy(x, y, 24, cy, 100, cy, 2.6)
        head = np.maximum(np.maximum((y - cy) - (x - 12) * 0.62, -(y - cy) - (x - 12) * 0.62), x - 26)
        fletch = union(seg_xy(x, y, 92, cy, 104, cy - 9, 2.4), seg_xy(x, y, 92, cy, 104, cy + 9, 2.4))
        arrow = (shaft, head, fletch)
        parts += [shaft, head, fletch]
    wings = None
    if grade == 'legendary':
        halves = []
        for sgn in (-1, 1):
            outer = circ(x, y, 78, cy + sgn * (tip_y + 2), 15)
            bite = circ(x, y, 92, cy + sgn * (tip_y - 6), 14)
            halves.append(np.maximum(outer, -bite))
        wings = union(*halves)
        parts.append(wings)
    every = union(*parts)
    if grade in ('epic', 'legendary'):
        reach = 16.0 if grade == 'legendary' else 12.0
        aura = np.clip(1 - np.maximum(every, 0) / reach, 0, 1) ** 2 * (every > 0)
        L.over(aura, AURA_L if grade == 'legendary' else (200, 140, 255, 120))
    outline(L, every)
    L.over(cov(string), (255, 250, 236, 235))
    if wings is not None:
        fill(L, wings, WING)
    fill(L, limb, limb_ramp, hi=False)
    L.over(cov(np.maximum(np.abs(circ(x, y, cx, cy, R + 2.2)) - 1.2, x - 64)), (255, 255, 255, 120))
    fill(L, tips, tip_ramp)
    fill(L, grip, grip_ramp, hi=False)
    if arrow is not None:
        shaft, head, fletch = arrow
        fill(L, shaft, (hexc('#F4E2C4'), hexc('#C89A64'), hexc('#7A5432')), hi=False)
        fill(L, head, GOLDEN if grade == 'legendary' else STEEL, hi=False)
        fill(L, fletch, {'rare': BLUE, 'epic': PINK_ORB, 'legendary': RED}[grade], hi=False)
    gx, gy = rot_pt_frame(cx - R, cy, ang)
    if grade == 'rare':
        gem(L, gx, gy, 5, BLUE)
    if grade == 'epic':
        gem(L, gx, gy, 6, PINK_ORB)
        sparkle(L, 104, 22, 8)
    if grade == 'legendary':
        gem(L, gx, gy, 7, RED)
        sparkle(L, 106, 20, 11)
        sparkle(L, 22, 104, 7)
    return L.image()


ORDER = ['sword', 'helm', 'armor', 'boots', 'gloves', 'necklace', 'ring', 'earring', 'staff', 'bow']
MAKERS = {'sword': sword_icon, 'helm': helm_icon, 'armor': armor_icon, 'boots': boots_icon,
          'gloves': gloves_icon, 'necklace': necklace_icon, 'ring': ring_icon, 'earring': earring_icon,
          'staff': staff_icon, 'bow': bow_icon}

# D-113: the grades between and above the four drawn ones reuse a drawn shape in their own materials. The swap
# rebinds this module's palette names while one icon is drawn: (shape grade, palette swaps, extra sparkles).
VARIANTS = {
    'uncommon': ('common', {
        'IRON': (hexc('#F4FBF0'), hexc('#B2D8A8'), hexc('#4F7C4E')),
        'LEATHER': (hexc('#C4E8A8'), hexc('#62A44C'), hexc('#2C5C26')),
        'BRONZE': (hexc('#E4F4C8'), hexc('#8EBE62'), hexc('#44702E')),
        'PAD_LEATHER': (hexc('#B4DC98'), hexc('#548E40'), hexc('#264E20')),
    }, 0),
    'mythic': ('legendary', {
        'GOLDEN': (hexc('#FFE6DE'), hexc('#F25A5E'), hexc('#981A36')),
        'RED': (hexc('#FFF8B8'), hexc('#FFC83A'), hexc('#B86E0A')),
        'WING': (hexc('#FFF6F6'), hexc('#FFC4CA'), hexc('#B0506A')),
        'AURA_L': (255, 110, 120, 165),
    }, 1),
    'ancient': ('legendary', {
        'GOLDEN': (hexc('#E8FFFA'), hexc('#5CE2CE'), hexc('#127A88')),
        'RED': (hexc('#FFFFFF'), hexc('#FFE98A'), hexc('#C88A10')),
        'WING': (hexc('#FFFDF0'), hexc('#FFDF76'), hexc('#BE8E1C')),
        'AURA_L': (110, 255, 225, 175),
    }, 2),
}
EXTRA_SPARKLES = [(20, 22, 7), (108, 108, 6)]


def render(slot, grade):
    shape, swaps, extra = VARIANTS.get(grade, (grade, {}, 0))
    names = globals()
    saved = {k: names[k] for k in swaps}
    names.update(swaps)
    try:
        img = MAKERS[slot](shape)
    finally:
        names.update(saved)
    if extra:
        L = new()
        for (cx, cy, size) in EXTRA_SPARKLES[:extra]:
            sparkle(L, cx, cy, size)
        img.alpha_composite(L.image())
    return img


if __name__ == '__main__':
    only = [a for a in sys.argv[1:] if a in MAKERS]
    for slot in ORDER:
        if only and slot not in only:
            continue
        for g in GRADES:
            save(render(slot, g), slot, g)
    if PREVIEW:
        cell = S + 16
        order = [s for s in ORDER if not only or s in only]
        sheet = Image.new('RGBA', (len(GRADES) * cell, len(order) * cell), (232, 220, 196, 255))
        lookup = dict(made)
        for r, slot in enumerate(order):
            for c, g in enumerate(GRADES):
                sheet.alpha_composite(lookup['%s_%s' % (slot, g)], (c * cell + 8, r * cell + 8))
        sheet.save(PREVIEW)
    print('made', len(made), 'equipment icons in', os.path.abspath(OUT))
