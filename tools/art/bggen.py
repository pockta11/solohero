"""Chapter parallax backgrounds (D-097): five themes drawn for this project at the characters' pixel density.

Replaces the mixed CC0 backgrounds (drawn at 1x, 2x and 3x pixels by five artists). Every theme has five layers,
far to near: sky (gradient bands with ordered dithering, clouds / stars / sun or moon), far mountains, mid hills,
a tree line and near foliage. Layers tile horizontally (all shapes are periodic in x) and are 320 px tall, bottom
anchored on the ground line; the floor plane covers the bottom 40 px. Far layers are lighter and closer to the sky
colour (aerial perspective) and every layer stays lower in contrast than the outlined characters so they read on top.

Run: python tools/art/bggen.py [--preview out.png]  ->  Assets/SoloHero/Art/Backgrounds/{theme}/layer_{0..4}.png
Then Tools > Setup > Build Art (layer height 320 gives pixel scale 1; follow factors come from ArtBuilder).
"""
import math
import os
import random
import sys

from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Art", "Backgrounds")
W, H = 320, 320
HORIZON = 40  # px hidden behind the floor plane's far edge
BAYER = ((0, 8, 2, 10), (12, 4, 14, 6), (3, 11, 1, 9), (15, 7, 13, 5))


def hexc(s):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


THEMES = {
    "Ch1_Meadow": dict(
        sky=["#5ec8ff", "#7ad2ff", "#a4e2ff", "#d2f2ff"], cloud=["#ffffff", "#e2f4ff"], deco="clouds",
        far=["#a6d8d0", "#bce4da"], far_shape="hills", mid=["#7cc06e", "#94d07e"],
        trees=["#3f8a4a", "#5aa652", "#86c86a"], trunk="#7a5236", tree="round", near=["#4e9a4a", "#6cb85a", "#94d26c"]),
    "Ch2_Snowpeak": dict(
        sky=["#86b2ec", "#9cc2f2", "#b8d6f8", "#e0eeff"], cloud=["#ffffff", "#e6f0ff"], deco="clouds",
        far=["#a8b8de", "#f2f6ff"], far_shape="peaks", mid=["#7d92c4", "#e8f0ff"],
        trees=["#2e5a5e", "#3e7468", "#5a9280"], trunk="#4a3a3a", tree="pine", near=["#3e6a66", "#52807a", "#e8f0fa"]),
    "Ch3_Forest": dict(
        sky=["#9ad8bc", "#b2e4c8", "#ccf0d6", "#e8fbec"], cloud=["#f4fff6", "#dcf4e2"], deco="rays",
        far=["#7cb89e", "#92c8ac"], far_shape="hills", mid=["#4e8e6c", "#62a27c"],
        trees=["#1e4a3a", "#2e6a4a", "#4a8a5a"], trunk="#4a3428", tree="pine_big", near=["#2e6a3e", "#448a4a", "#66a85a"]),
    "Ch4_Dusk": dict(
        sky=["#26285a", "#3a3470", "#5a4686", "#8a5e9a"], cloud=["#fff4d0", "#ffe8a0"], deco="night",
        far=["#5c4c8c", "#6c5a9c"], far_shape="peaks_soft", mid=["#46386e", "#56488a"],
        trees=["#2a2244", "#3a2e5a", "#54447a"], trunk="#221a36", tree="pine", near=["#30264c", "#44386a", "#5a4a84"]),
    "Ch5_Sunset": dict(
        sky=["#8a6ab8", "#c47eae", "#f0a0a0", "#ffd2a4"], cloud=["#ffe2c0", "#f6b8a8"], deco="sun",
        far=["#c88aa8", "#d8a0b4"], far_shape="hills", mid=["#a06a8a", "#b47e9a"],
        trees=["#6a3e5e", "#80506e", "#9a6a84"], trunk="#4a2a40", tree="round", near=["#5a3450", "#7a4a68", "#96607e"]),
}


class Layer:
    def __init__(self):
        self.img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        self.px = self.img.load()

    def set(self, x, y, c):
        """x wraps (tiling), y is measured up from the ground line."""
        yy = H - 1 - y
        if 0 <= yy < H:
            self.px[x % W, yy] = c

    def get(self, x, y):
        yy = H - 1 - y
        return self.px[x % W, yy] if 0 <= yy < H else (0, 0, 0, 0)


def periodic(seed, terms, amp):
    """A smooth function of x that repeats every W pixels (integer frequencies only)."""
    rng = random.Random(seed)
    waves = [(k, rng.uniform(0, math.tau), rng.uniform(0.5, 1.0) / k ** 0.8) for k in terms]
    total = sum(a for _, _, a in waves)

    def f(x):
        return amp * sum(a * math.sin(math.tau * k * x / W + p) for k, p, a in waves) / total

    return f


def blob(layer, cx, cy, r, colors, light=True):
    """A soft round shape (cloud puff, tree crown) with a lit top-left and shaded bottom-right edge."""
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d = dx * dx + dy * dy
            if d > r * r:
                continue
            c = colors[1] if len(colors) > 1 else colors[0]
            if light and dx + dy * -1 < -r * 0.6 and len(colors) > 2:
                c = colors[2]
            elif dx - dy * -1 > r * 0.55:
                c = colors[0]
            layer.set(cx + dx, cy + dy, c)


def sky_layer(t, seed):
    L = Layer()
    bands = [hexc(c) for c in t["sky"]]
    top, bottom = H - 1, HORIZON
    for y in range(H):
        u = (top - y) / (top - bottom)  # 0 at the top, 1 at the horizon
        u = min(max(u, 0.0), 1.0) * (len(bands) - 1)
        i = min(int(u), len(bands) - 2)
        f = u - i
        for x in range(W):
            threshold = (BAYER[y % 4][x % 4] + 0.5) / 16.0
            L.set(x, y, bands[i + 1] if f > threshold else bands[i])
    rng = random.Random(seed)
    deco = t["deco"]
    cloud = [hexc(c) for c in t["cloud"]]
    if deco in ("clouds", "sun", "rays"):
        for _ in range(5 if deco != "rays" else 3):
            cx, cy = rng.randrange(W), rng.randrange(150, 290)
            for j in range(rng.randrange(3, 6)):
                blob(L, cx + j * rng.randrange(7, 12), cy + rng.randrange(-3, 5), rng.randrange(6, 12),
                     [cloud[1], cloud[0], cloud[0]], light=False)
    if deco == "rays":
        light = hexc(t["cloud"][0])
        for k in range(4):
            x0 = 30 + k * 80
            for y in range(HORIZON, H):
                w = 6 + (H - y) // 30
                for dx in range(w):
                    if (dx + y) % 3 == 0:
                        L.set(x0 + dx + (H - y) // 3, y, light)
    if deco == "night":
        star = hexc("#ffffff")
        for _ in range(70):
            L.set(rng.randrange(W), rng.randrange(120, H), star if rng.random() < 0.6 else cloud[1])
        blob(L, 230, 255, 14, [cloud[1], cloud[0], cloud[0]], light=False)
    if deco == "sun":
        blob(L, 200, 110, 26, [cloud[1], hexc("#fff0c8"), hexc("#fff0c8")], light=False)
    return L


def ridge_layer(fn, base, colors, seed, snow=None, dots=None):
    L = Layer()
    rng = random.Random(seed)
    for x in range(W):
        h = int(base + fn(x))
        for y in range(HORIZON - 10, h + 1):
            c = colors[0]
            if snow is not None and y > snow(x):
                c = colors[1]
            L.set(x, y, c)
        # Lit rim: the top pixel of each column is the light tone where the slope faces the light (left).
        if fn(x - 1) < fn(x + 1) and snow is None and len(colors) > 1:
            L.set(x, h, colors[1])
    if dots:
        for _ in range(dots[1]):
            x = rng.randrange(W)
            top = int(base + fn(x))
            y = rng.randrange(HORIZON, max(HORIZON + 1, top - 2))
            L.set(x, y, dots[0])
    return L


def peaks(seed, amp):
    rng = random.Random(seed)
    tips = [(rng.randrange(W), rng.uniform(0.5, 1.0)) for _ in range(6)]

    def f(x):
        best = 0.0
        for tx, a in tips:
            d = min(abs(x - tx), W - abs(x - tx))
            best = max(best, a * amp * max(0.0, 1.0 - d / 48.0))
        return best

    return f


def tree(L, x, base, kind, colors, trunk, rng, scale=1.0):
    dark, mid, light = colors
    if kind == "round":
        r = int(rng.randrange(9, 14) * scale)
        for y in range(base, base + r):
            L.set(x, y, trunk)
            L.set(x + 1, y, trunk)
        blob(L, x, base + r + r // 2, r, [dark, mid, light])
        return
    height = int(rng.randrange(34, 50) * scale * (1.6 if kind == "pine_big" else 1.0))
    width = int(height * 0.36)
    for y in range(base, base + height // 6):
        L.set(x, y, trunk)
    for i in range(height):
        y = base + height // 6 + i
        half = int(width * (1.0 - i / height)) + (2 if (i % 9) < 3 else 0)
        for dx in range(-half, half + 1):
            c = mid
            if dx < -half * 0.3:
                c = light if (i % 9) > 4 else mid
            elif dx > half * 0.4:
                c = dark
            L.set(x + dx, y, c)


def tree_layer(t, seed):
    L = Layer()
    rng = random.Random(seed)
    colors = [hexc(c) for c in t["trees"]]
    trunk = hexc(t["trunk"])
    kind = t["tree"]
    # Back row smaller and darker, front row larger: one layer, two depths.
    for row, (base, count, scale) in enumerate(((HORIZON + 6, 9, 0.75), (HORIZON - 2, 7, 1.0))):
        xs = sorted(rng.randrange(W) for _ in range(count))
        for x in xs:
            cols = colors if row else [colors[0], colors[0], colors[1]]
            tree(L, x, base, kind, cols, trunk, rng, scale)
    return L


def near_layer(t, seed):
    L = Layer()
    rng = random.Random(seed)
    dark, mid, light = [hexc(c) for c in t["near"]]
    fn = periodic(seed, (3, 5, 9, 14), 8)
    for x in range(W):
        h = int(HORIZON + 6 + fn(x))
        for y in range(HORIZON - 10, h + 1):
            L.set(x, y, mid if y > h - 3 else dark)
    for _ in range(26):
        blob(L, rng.randrange(W), HORIZON + rng.randrange(6, 14), rng.randrange(5, 9), [dark, mid, light])
    for _ in range(40):
        x = rng.randrange(W)
        top = HORIZON + 8 + int(fn(x)) + rng.randrange(2, 6)
        for y in range(HORIZON + 4, top):
            L.set(x, y, light if y == top - 1 else mid)
    return L


def build(theme, t, seed):
    layers = [sky_layer(t, seed)]
    far = [hexc(c) for c in t["far"]]
    mid = [hexc(c) for c in t["mid"]]
    shape = t["far_shape"]
    if shape == "peaks":
        f = peaks(seed + 1, 150)
        g = periodic(seed + 2, (2, 5, 11), 10)
        # Snow caps above a wavy snow line, so only the peaks are white.
        layers.append(ridge_layer(lambda x: f(x) + g(x), 70, far, seed, snow=lambda x: 150 + int(6 * math.sin(x / 5.0))))
        m = peaks(seed + 3, 80)
        layers.append(ridge_layer(lambda x: m(x) + g(x) * 0.6, 62, mid, seed + 3, snow=lambda x: 112 + int(4 * math.sin(x / 4.0))))
    elif shape == "peaks_soft":
        f = peaks(seed + 1, 120)
        g = periodic(seed + 2, (2, 4, 9), 12)
        layers.append(ridge_layer(lambda x: f(x) * 0.8 + g(x), 80, far, seed))
        layers.append(ridge_layer(periodic(seed + 3, (2, 3, 7), 22), 78, mid, seed + 3, dots=(far[1], 50)))
    else:
        layers.append(ridge_layer(periodic(seed + 1, (1, 2, 3, 5), 34), 120, far, seed))
        layers.append(ridge_layer(periodic(seed + 3, (2, 3, 4, 7), 18), 84, mid, seed + 3, dots=(mid[1], 60)))
    layers.append(tree_layer(t, seed + 5))
    layers.append(near_layer(t, seed + 7))
    return layers


def main():
    sheets = {}
    for n, (theme, t) in enumerate(THEMES.items()):
        layers = build(theme, t, 11 + n * 17)
        directory = os.path.join(ROOT, theme)
        os.makedirs(directory, exist_ok=True)
        for old in os.listdir(directory):
            if old.startswith("layer_") and (old.endswith(".png") or old.endswith(".png.meta")):
                index = int(old.split("_")[1].split(".")[0])
                if index >= len(layers):
                    os.remove(os.path.join(directory, old))
        for i, layer in enumerate(layers):
            layer.img.save(os.path.join(directory, "layer_%d.png" % i))
        sheets[theme] = layers
    if "--preview" in sys.argv:
        out = Image.new("RGBA", (W * 2, H * len(sheets)), (0, 0, 0, 255))
        for r, layers in enumerate(sheets.values()):
            for k in range(2):
                for layer in layers:
                    out.alpha_composite(layer.img, (k * W, r * H))
        out.save(sys.argv[sys.argv.index("--preview") + 1])
    print("backgrounds", len(sheets))


if __name__ == "__main__":
    main()
