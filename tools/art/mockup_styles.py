"""Design mockups for the HD character look: the same battle scene in several hdize.py styles plus today's pixel art.

Run: python tools/art/mockup_styles.py RENDER_ROOT OUT_DIR
RENDER_ROOT holds chibi3d.py renders at SOLOHERO_RENDER_SCALE=6 of knight2, goblin, petfox and ronin.
Writes OUT_DIR/style_{key}.png (1080 x 640, the in-game scale: 4 screen pixels per art pixel).
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hdize import HdEntity  # noqa: E402

ART = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "SoloHero", "Art")
STYLES = {
    "a_ink": {"outer": 0.75, "inner": 0.42},
    "b_soft": {"outer": 0.6, "inner": 0.3, "soft": 0.9},
    "c_sticker": {"outer": 0.55, "inner": 0.35, "sticker": 0.75, "twotone": True},
    "d_thin": {"outer": 0.45, "inner": 0.22},
}
W, H, K = 1080, 640, 4          # canvas, screen pixels per art pixel
GROUND = 560
# (entity, clip, frame, screen x of the frame's left edge, lift in art px)
CAST = [("petfox", "idle", 0, 40, 26), ("knight2", "attack", 5, 150, 0), ("goblin", "attack", 4, 470, 0),
        ("goblin", "idle", 0, 600, 0), ("ronin", "idle", 0, 690, 0)]


def background():
    bg = Image.new("RGBA", (W, H), (120, 180, 230, 255))
    layers = os.path.join(ART, "Backgrounds", "Ch1_Meadow")
    for i in range(5):
        p = os.path.join(layers, "layer_%d.png" % i)
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGBA")
        im = im.resize((im.width * K, im.height * K), Image.NEAREST)
        for x in range(0, W, im.width):
            bg.alpha_composite(im, (x, GROUND - im.height + 60))
    floor = Image.open(os.path.join(ART, "Tiles", "floor_1.png")).convert("RGBA")
    floor = floor.resize((floor.width * K, floor.height * K), Image.NEAREST)
    for x in range(0, W, floor.width):
        bg.alpha_composite(floor.crop((0, 0, floor.width, H - GROUND + 20)), (x, GROUND - 20))
    return bg


def pixel_frame(entity, clip, index):
    folder = "Pets" if entity.startswith("pet") else "Bosses" if entity == "ronin" else "Hero" if entity.startswith("knight") else "Enemies"
    path = [os.path.join(ART, folder, f) for f in os.listdir(os.path.join(ART, folder))
            if f.startswith(entity + "_" + clip + "_") and f.endswith(".png")][0]
    sheet = Image.open(path).convert("RGBA")
    n = int(path.rsplit("_", 1)[1].split(".")[0])
    w = sheet.width // n
    f = sheet.crop((index * w, 0, (index + 1) * w, sheet.height))
    return f.resize((f.width * K, f.height * K), Image.NEAREST)


def hd_frame(root, entity, clip, index, style, cache):
    key = (entity, tuple(sorted(style.items())))
    if key not in cache:
        cache[key] = HdEntity(os.path.join(root, entity), style)
    e = cache[key]
    stamps = e.meta["clips"][clip][index]["stamps"]
    f = e.frame(os.path.join(root, entity, "%s_%d" % (clip, index)), stamps)
    if e.meta["mirror"]:
        f = f.transpose(Image.FLIP_LEFT_RIGHT)
    scale = K / (e.S / 2.0)
    return f.resize((round(f.width * scale), round(f.height * scale)), Image.LANCZOS)


def scene(frames):
    img = background()
    for (entity, clip, index, x, lift), f in zip(CAST, frames):
        img.alpha_composite(f, (x, GROUND - f.height - lift * K))
    return img


def main():
    root, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    scene([pixel_frame(e, c, i) for e, c, i, _, _ in CAST]).save(os.path.join(out, "style_0_pixel.png"))
    cache = {}
    for key, style in STYLES.items():
        frames = [hd_frame(root, e, c, i, style, cache) for e, c, i, _, _ in CAST]
        scene(frames).save(os.path.join(out, "style_%s.png" % key))
        print("mockup", key, flush=True)


if __name__ == "__main__":
    main()
