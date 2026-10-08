"""Battle floor tiles (D-091): a ground plane seen from slightly above, one per chapter theme.

Each tile repeats horizontally. Rows are spaced by perspective (a 1/z mapping), so bands grow taller toward the
viewer; speckles (grass blades, pebbles, flowers) shrink and thin out with distance; a soft dark lip and a row of
tufts along the far edge blend the floor into the background. No anti-aliasing.

Run: python tools/art/floorgen.py  ->  Assets/SoloHero/Art/Tiles/floor_{1..10}.png
"""
import os
import random

from PIL import Image

OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Art", "Tiles")
W, H = 64, 160
ROWS = 9


def hexc(s):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


# (far, dark band, light band, near highlight, speckle colours, tuft colour, lip)
THEMES = [
    # D-097: floors re-coloured to sit under the generated backgrounds (bggen.py).
    ("meadow", "#2f5a36", "#4f8a3f", "#62a04a", "#86c35e", ["#a6d86a", "#ffe066", "#ff9fb0", "#3b6e34"], "#3f7a38", "#1f3a2a"),
    ("snowpeak", "#8a9cbc", "#bccce2", "#ccdaec", "#eef4fc", ["#ffffff", "#a8bcd8", "#7a90b0", "#4a6e66"], "#52807a", "#5a6a8a"),
    ("forest", "#24402c", "#38603c", "#447046", "#5e8c52", ["#86b866", "#c8a050", "#ff9fb0", "#2a4428"], "#3a6a3e", "#16261a"),
    ("dusk", "#2a1a2e", "#46304e", "#523a5c", "#6e5078", ["#8a6a96", "#b08ac0", "#2e1e34", "#ffcf7a"], "#3a2a44", "#140c18"),
    ("sunset", "#5a3036", "#8a5048", "#9a5e50", "#b87a62", ["#d8a07a", "#ffd090", "#5a3036", "#f0b8a0"], "#6a4048", "#24121a"),
    # D-131 chapters 6-10.
    ("desert", "#7a5230", "#b98450", "#c8945a", "#e0b070", ["#f0d090", "#8a6038", "#d8a868", "#6a8a40"], "#9a7040", "#4a2e1a"),
    ("volcano", "#1e1216", "#3a2226", "#46282c", "#5e3434", ["#ff7a3a", "#2a1a1e", "#ffb060", "#5e3434"], "#2e1a1e", "#100a0c"),
    ("swamp", "#24301e", "#3a4a2e", "#465834", "#5e7044", ["#7a9a5a", "#4a5a3a", "#a8b870", "#2e3a24"], "#3a5030", "#141c10"),
    ("crystal", "#1a1438", "#2e2656", "#382e66", "#4e4282", ["#8ad0ff", "#c890ff", "#ffffff", "#2a2050"], "#3a3070", "#0e0a20"),
    ("castle", "#1a1014", "#2e1e24", "#38242c", "#4e3240", ["#6a4a58", "#2a1a20", "#a8706a", "#ff6a5a"], "#2e1e26", "#0c0608"),
]


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3)) + (255,)


def row_edges():
    """Row boundaries from the far edge (y=0) to the near edge (y=H): equal steps in depth z, projected as 1/z."""
    near, far = 1.0, 5.0
    edges = []
    for k in range(ROWS + 1):
        z = far - (far - near) * k / ROWS
        v = (1.0 / z - 1.0 / far) / (1.0 / near - 1.0 / far)
        edges.append(int(round(v * (H - 6))) + 6)
    edges[0] = 6
    edges[-1] = H
    return edges


def make(theme, seed):
    name, far_c, dark, light, near_hi, specks, tuft, lip = theme
    far_c, dark, light, near_hi, tuft, lip = map(hexc, (far_c, dark, light, near_hi, tuft, lip))
    specks = [hexc(s) for s in specks]
    rng = random.Random(seed)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()
    edges = row_edges()

    for r in range(ROWS):
        y0, y1 = edges[r], edges[r + 1]
        depth = r / (ROWS - 1)  # 0 far, 1 near
        # Far rows sink into the dark horizon colour; near rows get the full band colour and more contrast.
        band = dark if r % 2 == 0 else lerp(light, near_hi, 0.18 * depth)
        base = lerp(far_c, band, 0.12 + 0.88 * depth ** 0.85)
        for y in range(y0, y1):
            # Inside a band the top is a touch lighter than the bottom: each strip reads as a tilted plane.
            t = (y - y0) / max(1, y1 - y0 - 1)
            shade = lerp(base, lerp(base, far_c, 0.35), t * 0.5)
            for x in range(W):
                px[x, y] = shade
        # A one-pixel lighter lip on top of each near band reads as the rows turning toward the light.
        if depth > 0.2:
            hi = lerp(base, near_hi, 0.45)
            for x in range(W):
                px[x, y0] = hi

    # Speckles: bigger and denser near the viewer; wrap horizontally so the tile repeats cleanly.
    for r in range(ROWS):
        y0, y1 = edges[r], edges[r + 1]
        depth = r / (ROWS - 1)
        count = int(3 + depth * 10)
        size = 1 if depth < 0.45 else 2
        for _ in range(count):
            c = specks[rng.randrange(len(specks))]
            x = rng.randrange(W)
            y = rng.randrange(y0, max(y0 + 1, y1 - size))
            for dx in range(size):
                for dy in range(size if c != specks[0] else 1):
                    px[(x + dx) % W, min(H - 1, y + dy)] = c
            if c == specks[0] and depth > 0.4:
                # A grass blade: a short vertical stroke.
                for dy in range(1, 2 + int(depth * 2)):
                    px[x % W, max(y0, y - dy)] = c

    # Far edge: a dark lip and a row of rounded tufts that overlap the background above the floor.
    for x in range(W):
        for y in range(0, 6):
            px[x, y] = (0, 0, 0, 0)
        px[x, 6] = lip
        px[x, 7] = lerp(lip, far_c, 0.5)
    x = 0
    while x < W:
        w = rng.randrange(4, 8)
        h = rng.randrange(2, 5)
        for dx in range(w):
            k = abs(dx - (w - 1) / 2.0) / (w / 2.0)
            top = 6 - int(round(h * (1.0 - k * k)))
            for y in range(top, 7):
                px[(x + dx) % W, y] = tuft if y > top else lerp(tuft, near_hi, 0.3)
        x += w + rng.randrange(0, 3)
    return img


def main():
    for i, theme in enumerate(THEMES):
        make(theme, 91 + i).save(os.path.join(OUT_DIR, "floor_%d.png" % (i + 1)))
    print("floors", len(THEMES))


if __name__ == "__main__":
    main()
