"""Smooth equipment icons (D-108) -> Assets/SoloHero/Art/UI/Hd/Equipment/equip_{slot}_{grade}.png (128 x 128).

Usage: python tools/art/equipgen3.py [OUT_DIR] [--preview PREVIEW.png]
Four slots x four grades. Each grade changes material and ornament, not just the hue: common = iron and leather,
rare = polished steel with blue trim, epic = violet steel with gems, legendary = gold with wings, a ruby and a glint.
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
args = [a for a in sys.argv[1:] if not a.startswith('--')]
if args:
    OUT = args[0]
PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None

S = 128
IRON = (hexc('#E4E8EE'), hexc('#A4ACB8'), hexc('#5E6676'))
STEEL = (hexc('#FFFFFF'), hexc('#C8DCF6'), hexc('#6E86B4'))
VIOLET = (hexc('#F4DCFF'), hexc('#B98AF0'), hexc('#6A3CB8'))
GOLDEN = (hexc('#FFF8C8'), hexc('#FFCF4A'), hexc('#C47A10'))
LEATHER = (hexc('#E8B888'), hexc('#A8703E'), hexc('#5E3A1E'))
GRADES = ['common', 'rare', 'epic', 'legendary']

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
        L.over(aura, (255, 220, 120, 150))
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
            extras.append((wing, (hexc('#FFFFFF'), hexc('#E4ECFF'), hexc('#98A8D0'))))
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
            fill(L, wing, (hexc('#FFFFFF'), hexc('#E4ECFF'), hexc('#98A8D0')))
    fill(L, body, metal)
    L.over(cov(neck), shade(metal[2], -0.35))
    belt = rrect_at(L.x, L.y, 34, 84, 94, 96, 3)
    fill(L, belt, trim, hi=False)
    L.over(cov(rrect_at(L.x, L.y, 58, 82, 70, 98, 2)), shade(trim[0], 0.2))
    for p in pads:
        fill(L, p, metal if grade != 'common' else (hexc('#D8A878'), hexc('#8E5A30'), hexc('#4E2E14')))
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
    trim = {'common': (hexc('#C8A070'), hexc('#7A4A24'), hexc('#3E2410')), 'rare': GOLD, 'epic': GOLD, 'legendary': (hexc('#FFFFFF'), hexc('#E4ECFF'), hexc('#98A8D0'))}[grade]
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
        fill(L, w, (hexc('#FFFFFF'), hexc('#E4ECFF'), hexc('#98A8D0')))
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


if __name__ == '__main__':
    for g in GRADES:
        save(sword_icon(g), 'sword', g)
        save(helm_icon(g), 'helm', g)
        save(armor_icon(g), 'armor', g)
        save(boots_icon(g), 'boots', g)
    if PREVIEW:
        cell = S + 16
        sheet = Image.new('RGBA', (4 * cell, 4 * cell), (232, 220, 196, 255))
        order = ['sword', 'helm', 'armor', 'boots']
        lookup = dict(made)
        for r, slot in enumerate(order):
            for c, g in enumerate(GRADES):
                sheet.alpha_composite(lookup['%s_%s' % (slot, g)], (c * cell + 8, r * cell + 8))
        sheet.save(PREVIEW)
    print('made', len(made), 'equipment icons in', os.path.abspath(OUT))
