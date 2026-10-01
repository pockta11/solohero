"""App icon (D-097): the 3D-rendered knight over the meadow background, at integer pixel scales.

Android adaptive icon = 432 px background + 432 px foreground (the launcher masks to the centre ~288 px), plus a
512 px full-bleed legacy icon that is also the Play Store icon. Reads the current sheets, so run it after
build3d.py / bggen.py.

Run: python tools/art/appicon.py  ->  Assets/SoloHero/Art/Icons/app_icon_{bg,fg,legacy}.png
                                      + _bmad-output/implementation-artifacts/store/icon-512.png
"""
import os

from PIL import Image

HERE = os.path.dirname(__file__)
ART = os.path.join(HERE, "..", "..", "Assets", "SoloHero", "Art")
STORE = os.path.join(HERE, "..", "..", "_bmad-output", "implementation-artifacts", "store", "icon-512.png")


def knight():
    sheet = Image.open(os.path.join(ART, "Hero", "knight_idle_12.png")).convert("RGBA")
    frame = sheet.crop((0, 0, 96, 72))
    return frame.crop(frame.getbbox())


def background(size, scale):
    layers = [Image.open(os.path.join(ART, "Backgrounds", "Ch1_Meadow", "layer_%d.png" % i)).convert("RGBA") for i in range(5)]
    w, h = layers[0].size
    view = Image.new("RGBA", (w, h))
    for layer in layers:
        view.alpha_composite(layer)
    # Below the horizon the game draws its floor; paint the meadow floor's grass tones there.
    floor = Image.open(os.path.join(ART, "Tiles", "floor_1.png")).convert("RGBA")
    for x in range(0, w, floor.width):
        view.alpha_composite(floor.crop((0, 0, floor.width, 40)), (x, h - 40))
    side = size // scale
    # Horizon in the lower third so the knight stands on the hills.
    crop = view.crop((40, h - 40 - side * 2 // 3, 40 + side, h - 40 + side // 3))
    return crop.resize((side * scale, side * scale), Image.NEAREST).crop((0, 0, size, size))


def place(canvas, sprite, scale, centre_y):
    big = sprite.resize((sprite.width * scale, sprite.height * scale), Image.NEAREST)
    x = (canvas.width - big.width) // 2
    y = int(centre_y - big.height / 2)
    canvas.alpha_composite(big, (x, y))


def main():
    hero = knight()
    icons = os.path.join(ART, "Icons")
    bg = background(432, 4)
    bg.save(os.path.join(icons, "app_icon_bg.png"))
    fg = Image.new("RGBA", (432, 432), (0, 0, 0, 0))
    place(fg, hero, max(1, 250 // hero.height), 216)
    fg.save(os.path.join(icons, "app_icon_fg.png"))
    legacy = background(512, 4)
    place(legacy, hero, max(1, 400 // hero.height), 270)
    legacy.convert("RGB").save(os.path.join(icons, "app_icon_legacy.png"))
    os.makedirs(os.path.dirname(STORE), exist_ok=True)
    legacy.convert("RGB").save(STORE)
    print("app icon", hero.size)


if __name__ == "__main__":
    main()
