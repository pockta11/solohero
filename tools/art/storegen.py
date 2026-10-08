"""Google Play feature graphic (1024 x 500, no alpha) from the game's own art (launch plan P0-6).

Meadow parallax layers and floor, the knight hero with a pet facing a goblin wave and the demon knight boss, the
title logo and a tagline in the game font. Pixel art is scaled by whole numbers with nearest sampling.

    python tools/art/storegen.py            -> _bmad-output/implementation-artifacts/store/feature-graphic-1024x500.png
    python tools/art/storegen.py out.png    -> a preview elsewhere
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..'))
ART = os.path.join(ROOT, 'Assets', 'SoloHero', 'Art')
OUT = os.path.join(ROOT, '_bmad-output', 'implementation-artifacts', 'store', 'feature-graphic-1024x500.png')
FONT = os.path.join(ART, 'Fonts', 'SoloHeroJua.ttf')
W, H = 1024, 500
GROUND = 430  # y of the characters' feet


def frame(sheet, index, count):
    """Frame <index> of a horizontal strip with <count> frames."""
    img = Image.open(os.path.join(ART, sheet)).convert('RGBA')
    w = img.width // count
    return img.crop((index * w, 0, (index + 1) * w, img.height))


def trim(img):
    box = img.getbbox()
    return img.crop(box) if box else img


def scaled(img, k):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)


def mirrored(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def place(canvas, sprite, cx, feet):
    """Paste <sprite> centred on x = <cx> with its bottom edge on y = <feet>."""
    canvas.alpha_composite(sprite, (int(cx - sprite.width / 2), int(feet - sprite.height)))


def shadow(canvas, cx, feet, width):
    blob = Image.new('RGBA', (width, max(6, width // 5)), (0, 0, 0, 0))
    ImageDraw.Draw(blob).ellipse((0, 0, blob.width - 1, blob.height - 1), fill=(20, 30, 20, 90))
    canvas.alpha_composite(blob, (int(cx - width / 2), int(feet - blob.height / 2)))


def background():
    canvas = Image.new('RGBA', (W, H), (0, 0, 0, 255))
    theme = os.path.join(ART, 'Backgrounds', 'Ch1_Meadow')
    for i in range(5):
        layer = scaled(Image.open(os.path.join(theme, 'layer_%d.png' % i)).convert('RGBA'), 2)
        # The layers are 320 px tall art drawn for the top of the battle view; lift them so the horizon meets the floor.
        top = GROUND - layer.height + 70
        for x in range(0, W, layer.width):
            canvas.alpha_composite(layer, (x, top))
    floor = scaled(Image.open(os.path.join(ART, 'Tiles', 'floor_1.png')).convert('RGBA'), 2)
    for x in range(0, W, floor.width):
        canvas.alpha_composite(floor, (x, GROUND - 46))
    return canvas


def outlined_text(draw, xy, text, font, fill, outline, width):
    x, y = xy
    for dx in range(-width, width + 1):
        for dy in range(-width, width + 1):
            if dx * dx + dy * dy <= width * width:
                draw.text((x + dx, y + dy), text, font=font, fill=outline)
    draw.text((x, y), text, font=font, fill=fill)


def main(out):
    canvas = background()

    # A soft light fading to the right keeps the logo readable over the sky.
    veil = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    veil_draw = ImageDraw.Draw(veil)
    for x in range(0, 640):
        veil_draw.line((x, 0, x, H), fill=(255, 255, 255, int(70 * (1 - x / 640.0) ** 1.5)))
    canvas.alpha_composite(veil)

    boss = trim(frame('Bosses/demonknight_idle_12.png', 2, 12))
    goblin = trim(frame('Enemies/goblin_run_12.png', 3, 12))
    goblin_b = trim(frame('Enemies/goblin_run_12.png', 8, 12))
    hero = trim(frame('Hero/knight2_idle_12.png', 0, 12))
    pet = trim(frame('Pets/petdragon_idle_12.png', 0, 12))

    shadow(canvas, 905, GROUND, 190)
    place(canvas, scaled(boss, 3), 905, GROUND + 4)
    for cx, sprite in ((760, goblin), (820, goblin_b), (700, goblin_b)):
        shadow(canvas, cx, GROUND, 70)
        place(canvas, scaled(sprite, 3), cx, GROUND + 2)
    shadow(canvas, 590, GROUND, 120)
    place(canvas, scaled(hero, 4), 590, GROUND + 4)
    place(canvas, scaled(pet, 3), 470, GROUND - 120)

    logo = Image.open(os.path.join(ART, 'UI', 'Hd', 'hd_logo.png')).convert('RGBA')
    logo = logo.resize((int(logo.width * 0.62), int(logo.height * 0.62)), Image.LANCZOS)
    canvas.alpha_composite(logo, (28, 46))

    draw = ImageDraw.Draw(canvas)
    font = ImageFont.truetype(FONT, 40)
    outlined_text(draw, (44, 46 + logo.height + 10), '손 놓아도 강해지는 픽셀 영웅', font, (255, 255, 255, 255), (40, 26, 60, 255), 3)

    canvas.convert('RGB').save(out)
    print('feature graphic ->', out)


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else OUT)
