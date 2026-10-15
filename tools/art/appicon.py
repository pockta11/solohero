"""App icon (D-097, D-168): the ink-cel paladin over the meadow background.

Android adaptive icon = 432 px background + 432 px foreground (the launcher masks to the centre ~288 px), plus a
512 px full-bleed legacy icon that is also the Play Store icon. Reads the current sheets, so run it after
build3d.py / mapgen_hd.py.

Run: python tools/art/appicon.py [--hero PNG]  ->  Assets/SoloHero/Art/Icons/app_icon_{bg,fg,legacy}.png
                                      + _bmad-output/implementation-artifacts/store/icon-512.png
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(__file__)
ART = os.path.join(HERE, "..", "..", "Assets", "SoloHero", "Art")
STORE = os.path.join(HERE, "..", "..", "_bmad-output", "implementation-artifacts", "store", "icon-512.png")


def knight():
    """The paladin's first idle frame (D-168 ink-cel sheets are trimmed per clip: frame width = sheet / frames).
    `--hero PNG` uses a bigger render instead (chibi3d.py knight5 at SOLOHERO_RENDER_SCALE=16, SOLOHERO_CLIPS=idle,
    SOLOHERO_FRAMES=1, then hdize.py's frame) so the icon is not upscaled."""
    if "--hero" in sys.argv:
        big = Image.open(sys.argv[sys.argv.index("--hero") + 1]).convert("RGBA")
        return big.crop(big.getbbox())
    sheet = Image.open(os.path.join(ART, "Hero", "knight5_idle_12.png")).convert("RGBA")
    w = sheet.width // 12
    frame = sheet.crop((0, 0, w, sheet.height))
    return frame.crop(frame.getbbox())


def background(size, scale):
    """The meadow at `scale` screen px per art px (the layers hold K texels per art px)."""
    layers = [Image.open(os.path.join(ART, "Backgrounds", "Ch1_Meadow", "layer_%d.png" % i)).convert("RGBA") for i in range(5)]
    w, h = layers[0].size
    K = w // 320
    view = Image.new("RGBA", (w, h))
    for layer in layers:
        view.alpha_composite(layer)
    # Below the horizon the game draws its floor; paint the meadow floor's grass tones there.
    floor = Image.open(os.path.join(ART, "Tiles", "floor_1.png")).convert("RGBA")
    for x in range(0, w, floor.width):
        view.alpha_composite(floor.crop((0, 0, floor.width, 40 * K)), (x, h - 40 * K))
    side = size // scale
    # Horizon in the lower third so the knight stands on the hills.
    crop = view.crop((40 * K, h - (40 + side * 2 // 3) * K, (40 + side) * K, h - (40 - side // 3) * K))
    return crop.resize((size, size), Image.LANCZOS)


def place(canvas, sprite, height, centre_y):
    big = sprite.resize((round(sprite.width * height / sprite.height), height), Image.LANCZOS)
    x = (canvas.width - big.width) // 2
    y = int(centre_y - big.height / 2)
    canvas.alpha_composite(big, (x, y))


def main():
    hero = knight()
    icons = os.path.join(ART, "Icons")
    bg = background(432, 4)
    bg.save(os.path.join(icons, "app_icon_bg.png"))
    fg = Image.new("RGBA", (432, 432), (0, 0, 0, 0))
    place(fg, hero, 250, 216)
    fg.save(os.path.join(icons, "app_icon_fg.png"))
    legacy = background(512, 4)
    place(legacy, hero, 400, 270)
    legacy.convert("RGB").save(os.path.join(icons, "app_icon_legacy.png"))
    os.makedirs(os.path.dirname(STORE), exist_ok=True)
    legacy.convert("RGB").save(STORE)
    print("app icon", hero.size)


if __name__ == "__main__":
    main()
