"""UI skin v2 (grade frames, selection, badges, cards, portrait props, icons) -> Assets/SoloHero/Art/UI.

Usage: python uigen.py OUT [PREVIEW.png]
Same palette as the v1 skin (outline #120F1F, navy panels, gold trim). 9-slice files are named ui9_{name}_{border}.png
so SpriteImportPreset sets the border; 1 art pixel = 4 canvas units.
"""
import os
import sys

from PIL import Image

OUT = sys.argv[1]
PREVIEW = sys.argv[2] if len(sys.argv) > 2 else None
os.makedirs(os.path.join(OUT, 'Icons'), exist_ok=True)

T = (0, 0, 0, 0)
OUTLINE = (18, 15, 31, 255)
DEEP = (11, 10, 20, 255)
NAVY = (26, 24, 41, 240)
NAVY_HI = (70, 65, 95, 255)
PANEL = (35, 32, 54, 245)
PANEL_HI = (58, 53, 82, 255)
GOLD_HI = (246, 215, 122, 255)
GOLD = (209, 154, 58, 255)
GOLD_LO = (125, 84, 24, 255)
WHITE = (255, 255, 255, 255)

# Grade palettes: (light, mid, dark, inner background). GDD grade colours are the mids.
GRADES = {
    'c': ((200, 200, 208, 255), (158, 158, 158, 255), (92, 92, 104, 255), (38, 37, 48, 255)),
    'r': ((150, 200, 255, 255), (61, 139, 255, 255), (30, 74, 168, 255), (22, 34, 66, 255)),
    'e': ((214, 160, 255, 255), (162, 75, 255, 255), (92, 36, 168, 255), (40, 22, 66, 255)),
    'l': ((255, 240, 160, 255), (255, 197, 49, 255), (170, 104, 20, 255), (66, 44, 20, 255)),
}

made = []


def new(w, h, fill=T):
    return Image.new('RGBA', (w, h), fill)


def save(im, name, sub=''):
    path = os.path.join(OUT, sub, name + '.png')
    im.save(path)
    made.append((name, im))


def rect(im, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            im.putpixel((x, y), c)


def ring(im, i, c_top, c_bottom=None, c_left=None, c_right=None):
    """Draws the i-th ring from the edge: top / bottom rows and left / right columns."""
    w, h = im.size
    c_bottom = c_bottom or c_top
    c_left = c_left or c_top
    c_right = c_right or c_bottom
    for x in range(i, w - i):
        im.putpixel((x, i), c_top)
        im.putpixel((x, h - 1 - i), c_bottom)
    for y in range(i + 1, h - 1 - i):
        im.putpixel((i, y), c_left)
        im.putpixel((w - 1 - i, y), c_right)


def round_corners(im, r=1):
    w, h = im.size
    for (x, y) in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]:
        im.putpixel((x, y), T)
    if r >= 2:
        for (x, y) in [(1, 0), (0, 1), (w - 2, 0), (w - 1, 1), (0, h - 2), (1, h - 1), (w - 1, h - 2), (w - 2, h - 1)]:
            im.putpixel((x, y), T)


def mirror_corners(im, size):
    """Copies the top-left size x size block to the other three corners (mirrored)."""
    w, h = im.size
    block = im.crop((0, 0, size, size))
    im.paste(block.transpose(Image.FLIP_LEFT_RIGHT), (w - size, 0))
    im.paste(block.transpose(Image.FLIP_TOP_BOTTOM), (0, h - size))
    im.paste(block.transpose(Image.ROTATE_180), (w - size, h - size))


def grade_frame(key):
    """10x10, border 3: outline, bevelled grade ring, inner shadow line, tinted centre."""
    light, mid, dark, inner = GRADES[key]
    im = new(10, 10, inner)
    ring(im, 0, OUTLINE)
    ring(im, 1, light, dark, light, dark)
    ring(im, 2, mid, dark, mid, dark)
    # Inner top-left shadow so the centre reads as recessed.
    for x in range(3, 7):
        im.putpixel((x, 3), tuple(int(v * 0.7) for v in inner[:3]) + (255,))
    for y in range(4, 7):
        im.putpixel((3, y), tuple(int(v * 0.7) for v in inner[:3]) + (255,))
    round_corners(im)
    if key == 'e':
        im.putpixel((1, 1), WHITE)
        im.putpixel((8, 8), light)
    if key == 'l':
        # Gold studs and a white glint on the corners.
        for (x, y) in [(1, 1), (8, 1), (1, 8), (8, 8)]:
            im.putpixel((x, y), WHITE)
        im.putpixel((2, 1), light)
        im.putpixel((1, 2), light)
    return im


def empty_frame():
    im = new(10, 10, (14, 12, 24, 215))
    ring(im, 0, OUTLINE)
    ring(im, 1, (46, 42, 70, 255), (30, 27, 46, 255))
    ring(im, 2, (22, 20, 36, 235))
    round_corners(im)
    return im


def selection():
    """14x14, border 6: thick gold corner brackets, transparent between them."""
    im = new(14, 14)
    c = [
        "OOOOOO",
        "OWYYYO",
        "OYYOOO",
        "OYO...",
        "OYO...",
        "OOO...",
    ]
    for y, row in enumerate(c):
        for x, ch in enumerate(row):
            col = {'O': OUTLINE, 'W': WHITE, 'Y': GOLD_HI, '.': T}[ch]
            im.putpixel((x, y), col)
    mirror_corners(im, 6)
    return im


def badge():
    """7x7, border 3: dark rounded pill for level numbers."""
    im = new(7, 7, (14, 12, 24, 240))
    ring(im, 0, OUTLINE)
    for x in range(1, 6):
        im.putpixel((x, 1), (52, 47, 76, 255))
    round_corners(im)
    return im


def tag(light, mid, dark):
    im = new(7, 7, mid)
    ring(im, 0, OUTLINE)
    for x in range(1, 6):
        im.putpixel((x, 1), light)
        im.putpixel((x, 5), dark)
    round_corners(im)
    return im


def inset():
    """10x10, border 4: recessed well (portrait window, grid backdrop)."""
    im = new(10, 10, (16, 14, 27, 235))
    ring(im, 0, OUTLINE)
    ring(im, 1, (8, 7, 14, 255), (50, 46, 74, 255), (10, 9, 17, 255), (30, 27, 46, 255))
    for x in range(2, 8):
        im.putpixel((x, 2), (12, 11, 20, 240))
    round_corners(im)
    return im


def card():
    """12x12, border 4: raised card for upgrade tiles and the skill detail."""
    im = new(12, 12, (38, 34, 58, 255))
    ring(im, 0, OUTLINE)
    ring(im, 1, (78, 72, 108, 255), (22, 20, 35, 255), (60, 55, 86, 255), (28, 25, 42, 255))
    for x in range(2, 10):
        im.putpixel((x, 2), (48, 44, 70, 245))
    round_corners(im)
    # Small gold rivets in the top corners.
    im.putpixel((2, 2), GOLD)
    im.putpixel((9, 2), GOLD)
    return im


def portrait():
    """12x12, border 4: gold window the hero stands in."""
    im = new(12, 12, (16, 14, 27, 235))
    ring(im, 0, OUTLINE)
    ring(im, 1, GOLD_HI, GOLD_LO, GOLD_HI, GOLD_LO)
    ring(im, 2, GOLD, GOLD_LO, GOLD, GOLD_LO)
    ring(im, 3, (48, 40, 24, 255), (22, 18, 12, 255), (36, 30, 18, 255), (28, 22, 14, 255))
    for x in range(4, 8):
        im.putpixel((x, 4), (12, 11, 20, 240))
    round_corners(im)
    im.putpixel((2, 2), WHITE)
    im.putpixel((9, 2), GOLD_HI)
    im.putpixel((2, 9), GOLD)
    im.putpixel((9, 9), GOLD_LO)
    return im


def plate():
    """10x8, border 3: gold-trimmed nameplate for the portrait caption."""
    im = new(10, 8, (28, 24, 44, 245))
    ring(im, 0, OUTLINE)
    ring(im, 1, GOLD_HI, GOLD_LO, GOLD, GOLD_LO)
    for x in range(2, 8):
        im.putpixel((x, 2), (52, 46, 76, 255))
    round_corners(im)
    im.putpixel((2, 2), GOLD)
    im.putpixel((7, 2), GOLD)
    return im


def chip():
    """8x8, border 3: flat stat chip."""
    im = new(8, 8, (20, 18, 33, 225))
    ring(im, 0, (10, 9, 17, 255))
    for x in range(1, 7):
        im.putpixel((x, 6), (44, 40, 64, 255))
    round_corners(im)
    return im


def chip_white():
    """7x7, border 3: light grey pill meant to be tinted (grade name chip)."""
    im = new(7, 7, (205, 205, 212, 255))
    ring(im, 0, OUTLINE)
    for x in range(1, 6):
        im.putpixel((x, 1), WHITE)
        im.putpixel((x, 5), (140, 140, 150, 255))
    round_corners(im)
    return im


def gauge_fill(light, mid, dark):
    im = new(6, 6, mid)
    for x in range(6):
        im.putpixel((x, 0), light)
        im.putpixel((x, 1), light)
        im.putpixel((x, 5), dark)
    return im


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def glow(size=48):
    """Dithered radial light, white (tinted by the Image colour)."""
    im = new(size, size)
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = (((x - c) ** 2 + (y - c) ** 2) ** 0.5) / c
            if d >= 1:
                continue
            level = (1 - d) ** 1.4
            steps = [(0.55, 150), (0.3, 95), (0.12, 50), (0.0, 22)]
            for threshold, alpha in steps:
                if level > threshold:
                    # Dither the band edge.
                    edge = (level - threshold) * 16 * 4
                    if edge < 16 and BAYER[y % 4][x % 4] > edge and alpha > 22:
                        alpha = steps[min(steps.index((threshold, alpha)) + 1, len(steps) - 1)][1]
                    im.putpixel((x, y), (255, 255, 255, alpha))
                    break
    return im


def pedestal():
    """40x12 stone disc the portrait hero stands on."""
    w, h = 40, 12
    im = new(w, h)
    cx, cy, rx, ry = (w - 1) / 2, 4.5, 19.5, 4.5
    top_hi, top, rim, front, front_lo = (96, 88, 130, 255), (70, 65, 95, 255), (120, 112, 156, 255), (40, 36, 60, 255), (26, 23, 40, 255)
    for y in range(h):
        for x in range(w):
            inside_top = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1
            inside_low = ((x - cx) / rx) ** 2 + ((y - cy - 3) / ry) ** 2 <= 1
            if inside_top:
                im.putpixel((x, y), top_hi if y < cy - 1 else top)
            elif inside_low and y > cy:
                im.putpixel((x, y), front if y < h - 3 else front_lo)
    # Outline pass.
    base = im.copy()
    for y in range(h):
        for x in range(w):
            if base.getpixel((x, y))[3] == 0:
                continue
            for dx, dy in [(1, 0), (-1, 0), (0, 1), (0, -1)]:
                nx, ny = x + dx, y + dy
                if nx < 0 or ny < 0 or nx >= w or ny >= h or base.getpixel((nx, ny))[3] == 0:
                    im.putpixel((x, y), OUTLINE)
                    break
    # Rim highlight along the top edge of the disc.
    for x in range(w):
        for y in range(h):
            if im.getpixel((x, y)) != OUTLINE and im.getpixel((x, y))[3] > 0:
                if y <= 2:
                    im.putpixel((x, y), rim)
                break
    return im


def outline_icon(im):
    w, h = im.size
    base = im.copy()
    for y in range(h):
        for x in range(w):
            if base.getpixel((x, y))[3] != 0:
                continue
            for dx, dy in [(1, 0), (-1, 0), (0, 1), (0, -1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and base.getpixel((nx, ny))[3] > 0 and base.getpixel((nx, ny)) != OUTLINE:
                    im.putpixel((x, y), OUTLINE)
                    break
    return im


def icon_crit():
    """Red crosshair: critical rate."""
    im = new(16, 16)
    red, red_hi, red_lo = (236, 72, 72, 255), (255, 150, 140, 255), (150, 30, 44, 255)
    c = 7.5
    for y in range(16):
        for x in range(16):
            d = ((x - c) ** 2 + (y - c) ** 2) ** 0.5
            if 3.8 <= d <= 5.4:
                im.putpixel((x, y), red_hi if y < c - 1 else red if y < c + 2 else red_lo)
    # Ticks from the edge to the ring, and a white centre dot.
    for i in range(1, 4):
        for (x, y) in [(7, i), (8, i), (7, 15 - i), (8, 15 - i), (i, 7), (i, 8), (15 - i, 7), (15 - i, 8)]:
            im.putpixel((x, y), red)
    for (x, y) in [(7, 7), (8, 7), (7, 8), (8, 8)]:
        im.putpixel((x, y), WHITE)
    return outline_icon(im)


def icon_burst():
    """Orange star burst: critical damage."""
    im = new(16, 16)
    pts = ["......O.O.......",
           "......OYO.......",
           ".O....OYO....O..",
           "..OO.OYYYO.OO...",
           "...OYYYYYYYO....",
           "....OYWWWYYO....",
           "OOOOYYWWWYYYOOO.",
           ".OYYYWWWWWYYYO..",
           "..OOYYWWWYYYOO..",
           "....OYYWYYYO....",
           "...OYYYYYYYYO...",
           "..OOYOOYOOYYOO..",
           ".O..OO.O..OO..O.",
           ".......O........",
           "................",
           "................"]
    col = {'O': (170, 60, 20, 255), 'Y': (255, 170, 50, 255), 'W': (255, 238, 170, 255), '.': T}
    for y, row in enumerate(pts):
        for x, ch in enumerate(row):
            im.putpixel((x, y), col[ch])
    im = im.transform(im.size, Image.AFFINE, (1, 0, 0, 0, 1, -1))
    return outline_icon(im)


save(grade_frame('c'), 'ui9_gradec_3')
save(grade_frame('r'), 'ui9_grader_3')
save(grade_frame('e'), 'ui9_gradee_3')
save(grade_frame('l'), 'ui9_gradel_3')
save(empty_frame(), 'ui9_gradenone_3')
save(selection(), 'ui9_select_6')
save(badge(), 'ui9_badge_3')
save(tag((143, 224, 122, 255), (76, 176, 74, 255), (43, 122, 52, 255)), 'ui9_tag_3')
save(inset(), 'ui9_inset_4')
save(card(), 'ui9_card_4')
save(portrait(), 'ui9_portrait_4')
save(plate(), 'ui9_plate_3')
save(chip(), 'ui9_chip_3')
save(chip_white(), 'ui9_chipw_3')
save(gauge_fill((160, 226, 255, 255), (64, 156, 236, 255), (30, 84, 164, 255)), 'ui9_gaugeblue_2')
save(glow(), 'ui_glow')
save(pedestal(), 'ui_pedestal')
save(icon_crit(), 'icon_crit', 'Icons')
save(icon_burst(), 'icon_burst', 'Icons')

if PREVIEW:
    S, pad = 8, 12
    cw = max(i.width for _, i in made) * S + pad
    ch = max(i.height for _, i in made) * S + pad
    cols = 4
    rows = (len(made) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cw, rows * ch), (52, 50, 64, 255))
    for k, (_, im) in enumerate(made):
        big = im.resize((im.width * S, im.height * S), Image.NEAREST)
        sheet.alpha_composite(big, ((k % cols) * cw, (k // cols) * ch))
    sheet.save(PREVIEW)

print('made', len(made), 'sprites in', OUT)
