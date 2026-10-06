"""Loading screen art (D-108): the meadow backdrop (pixel art) and the smooth title logo.

Usage: python tools/art/loadinggen.py [--preview PREVIEW.png]
Writes Assets/SoloHero/Art/UI/ui_loading_bg.png (270 x 600, 1 art px = 4 canvas units like the battle) built from the
chapter 1 background layers and floor tile, and Assets/SoloHero/Art/UI/Hd/hd_logo.png (the "솔로히어로" title in
the UI font: gold gradient, plum outline, white rim and a soft shadow, over a "픽셀 방치 RPG" ribbon).
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art')
FONT = os.path.join(ART, 'Fonts', 'SoloHeroJua.ttf')
sys.path.insert(0, HERE)
from uikit import Layer, band, cov, hexc, vgrad  # noqa: E402

PREVIEW = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None


def backdrop(w=270, h=600):
    layers = [Image.open(os.path.join(ART, 'Backgrounds', 'Ch1_Meadow', 'layer_%d.png' % i)).convert('RGBA') for i in range(5)]
    comp = Image.new('RGBA', (320, 320), (0, 0, 0, 0))
    for im in layers:
        comp.alpha_composite(im)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 255))
    # Sky: extend the top colour of the sky layer upward.
    sky_top = comp.getpixel((10, 2))
    out.paste(Image.new('RGBA', (w, h), sky_top), (0, 0))
    ground_y = 430  # where the tree line ends in the output
    # The tree band ends at y 290 in the layer; align it with ground_y.
    oy = ground_y - 290
    out.alpha_composite(comp.crop((25, 0, 25 + w, 290)), (0, oy))
    floor = Image.open(os.path.join(ART, 'Tiles', 'floor_1.png')).convert('RGBA')
    # Fill under the floor with its own top and bottom colours so no sky shows through gaps.
    top_c = floor.getpixel((floor.width // 2, min(6, floor.height - 1)))
    bot_c = floor.getpixel((floor.width // 2, floor.height - 1))
    ImageDraw.Draw(out).rectangle([0, ground_y - 3, w, ground_y + floor.height // 2], fill=top_c[:3] + (255,))
    ImageDraw.Draw(out).rectangle([0, ground_y + floor.height // 2, w, h], fill=bot_c[:3] + (255,))
    out.alpha_composite(comp.crop((25, 270, 25 + w, 290)), (0, ground_y - 20))
    for x in range(0, w, floor.width):
        out.alpha_composite(floor, (x, ground_y))
    # Darken the very bottom a little so the loading bar reads.
    shade = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(shade)
    for y in range(h - 90, h):
        a = int(120 * (y - (h - 90)) / 90)
        d.line([(0, y), (w, y)], fill=(20, 14, 34, a))
    out.alpha_composite(shade)
    return out


def text_mask(s, size, pad=40):
    f = ImageFont.truetype(FONT, size)
    l, t, r, b = f.getbbox(s)
    w, h = r - l + pad * 2, b - t + pad * 2
    m = Image.new('L', (w, h), 0)
    ImageDraw.Draw(m).text((pad - l, pad - t), s, font=f, fill=255)
    return m


def logo():
    title = text_mask('솔로히어로', 190, pad=48)
    w, h = title.size
    W, H = max(w, 760), h + 110
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ox = (W - w) // 2
    full = Image.new('L', (W, H), 0)
    full.paste(title, (ox, 0))
    # Shadow, ink outline, white rim, gradient fill, top gloss.
    ink = full.filter(ImageFilter.MaxFilter(25))
    shadow = ink.filter(ImageFilter.GaussianBlur(6))
    sh = Image.new('RGBA', (W, H), (40, 20, 60, 0))
    sh.putalpha(shadow.point(lambda v: int(v * 0.55)))
    out.alpha_composite(sh, (0, 10))
    ink_img = Image.new('RGBA', (W, H), hexc('#2A1F3D'))
    ink_img.putalpha(ink)
    out.alpha_composite(ink_img)
    rim = full.filter(ImageFilter.MaxFilter(11))
    rim_img = Image.new('RGBA', (W, H), (255, 255, 255, 255))
    rim_img.putalpha(rim)
    out.alpha_composite(rim_img)
    L = Layer(W, H)
    grad = vgrad(L, 40, h - 40, hexc('#FFF6B8'), hexc('#FF9A1E'))
    arr = np.array(full, np.float32) / 255.0
    L.over(arr, grad)
    out.alpha_composite(L.image())
    gloss = Layer(W, H)
    top = (gloss.y < h * 0.5).astype(np.float32) * arr * 0.35
    gloss.over(top, (255, 255, 255, 255))
    out.alpha_composite(gloss.image())
    # Ribbon with the subtitle.
    rib = Layer(W, H)
    rw, rh = 470, 74
    x0, y0 = (W - rw) / 2, h - 34
    body = rib.rrect(x0, y0, x0 + rw, y0 + rh, 18)
    rib.over(cov(body + 4), hexc('#2A1F3D'))
    rib.over(cov(body), vgrad(rib, y0, y0 + rh, hexc('#7ED0FF'), hexc('#2F7FE0')))
    rib.over(band(body, -3, -0.5) * (rib.y < y0 + rh * 0.4), (255, 255, 255, 180))
    out.alpha_composite(rib.image())
    sub = text_mask('픽셀 방치 RPG', 44, pad=8)
    sub_img = Image.new('RGBA', sub.size, (255, 255, 255, 255))
    sub_img.putalpha(sub)
    sub_ink = Image.new('RGBA', sub.size, hexc('#173E86'))
    sub_ink.putalpha(sub.filter(ImageFilter.MaxFilter(5)))
    sx, sy = int((W - sub.width) / 2), int(y0 + (rh - sub.height) / 2 + 2)
    out.alpha_composite(sub_ink, (sx, sy))
    out.alpha_composite(sub_img, (sx, sy))
    return out.crop(out.getbbox())


if __name__ == '__main__':
    bg = backdrop()
    bg.save(os.path.join(ART, 'UI', 'ui_loading_bg.png'))
    lg = logo()
    os.makedirs(os.path.join(ART, 'UI', 'Hd'), exist_ok=True)
    lg.save(os.path.join(ART, 'UI', 'Hd', 'hd_logo.png'))
    if PREVIEW:
        big = bg.resize((bg.width * 4, bg.height * 4), Image.NEAREST)
        big.alpha_composite(lg, ((big.width - lg.width) // 2, 330))
        big.save(PREVIEW)
    print('loading art written', bg.size, lg.size)
