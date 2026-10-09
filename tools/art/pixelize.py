"""Turns chibi3d.py renders into SoloHero character sheets with charkit's pixel rules.

1. Each 4x4 block of the render becomes one pixel: opaque when half the block is, coloured by the block's main
   object and that object's most common palette colour (the render only holds palette colours, so no blending).
2. A 1 px internal line separates different objects: the farther pixel takes its material's darkest tone.
3. The silhouette outline is charkit's: dark (#2b1d3a) by default, the part's own dark tone on the lit top-left.
   Swing smears get neither.
4. Orphan pixels are merged, eye highlights are stamped from the projected 3D points and enemy frames are mirrored
   to face the hero. Hit flashes are not baked in: the game flashes sprites white with a shader (D-096).

Run: python tools/art/pixelize.py RENDER_ROOT ENTITY [--out ART_ROOT]
Reads RENDER_ROOT/ENTITY, writes ART_ROOT/{folder}/{name}_{clip}_{frames}.png (default Assets/SoloHero/Art) and
removes that entity's sheets with other frame counts.
"""
import glob
import json
import os
import sys
from collections import Counter

import numpy as np
from PIL import Image, ImageOps

sys.path.insert(0, os.path.dirname(__file__))
from charkit import clean_orphans, hexc, sheet
from hero import EYE, OUT
from punch import color_map

ART = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Art")
LINE_EPS = 0.3  # art pixels of depth before a neighbour counts as in front


class Entity:
    def __init__(self, src):
        self.src = src
        self.meta = json.load(open(os.path.join(src, "meta.json")))
        m = self.meta
        self.W, self.H = m["frame"]
        self.S = m["scale"]
        self.near, self.far = m["depth"]
        self.colors = []  # palette index -> (rgba, material)
        seen = set()
        for mat, hexes in m["palette"].items():
            for h in hexes:
                if h not in seen:
                    seen.add(h)
                    self.colors.append((hexc(h), mat))
        self.pal = np.array([c[0][:3] for c in self.colors], dtype=np.int32)
        self.line_of = {mat: hexc(hexes[0]) for mat, hexes in m["palette"].items()}
        self.ramped = {mat for mat, hexes in m["palette"].items() if len(hexes) > 1}
        self.nolines = set(m["nolines"])
        # D-150: renders are matched against the palette as rendered, then shown in punch.py's crisper ramps.
        self.punched = color_map(m["palette"]) if os.environ.get("SOLOHERO_PUNCH", "1") != "0" else {}

    def pixelize(self, stem):
        W, H, S = self.W, self.H, self.S
        rgba = np.asarray(Image.open(stem + "_color.png").convert("RGBA"), dtype=np.int32)
        data = np.asarray(Image.open(stem + "_data.png").convert("RGB"), dtype=np.float64)
        obj = data[..., 0].astype(np.int32)
        depth = self.near + (data[..., 1] + data[..., 2] / 255.0) / 255.0 * (self.far - self.near)
        d = ((rgba[..., None, :3] - self.pal[None, None, :, :]) ** 2).sum(-1)
        idx = d.argmin(-1)
        solid = (rgba[..., 3] > 127) & (obj > 0)

        ids = np.full((H, W), -1, dtype=np.int32)
        col = np.full((H, W), -1, dtype=np.int32)
        dep = np.zeros((H, W))
        for y in range(H):
            for x in range(W):
                bs = (slice(y * S, (y + 1) * S), slice(x * S, (x + 1) * S))
                m = solid[bs]
                if m.sum() * 2 < S * S:
                    continue
                o = Counter(obj[bs][m].tolist()).most_common(1)[0][0]
                mo = m & (obj[bs] == o)
                col[y, x] = Counter(idx[bs][mo].tolist()).most_common(1)[0][0]
                ids[y, x] = o
                dep[y, x] = depth[bs][mo].mean()

        def mat_at(y, x):
            return self.colors[col[y, x]][1]

        final = [[None] * W for _ in range(H)]
        for y in range(H):
            for x in range(W):
                if ids[y, x] < 0:
                    continue
                c, mat = self.colors[col[y, x]]
                final[y][x] = c
                if mat in self.nolines:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H and ids[ny, nx] >= 0 and ids[ny, nx] != ids[y, x] \
                            and mat_at(ny, nx) not in self.nolines and dep[y, x] > dep[ny, nx] + LINE_EPS:
                        final[y][x] = self.line_of[mat]
                        break
        out = [row[:] for row in final]
        for y in range(H):
            for x in range(W):
                if ids[y, x] >= 0:
                    continue
                touch, lit = None, False
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H and ids[ny, nx] >= 0 and mat_at(ny, nx) not in self.nolines:
                        touch = (ny, nx)
                        lit = lit or dx == 1 or dy == 1
                if touch is None:
                    continue
                mat = mat_at(*touch)
                out[y][x] = self.line_of[mat] if lit and mat in self.ramped else OUT
        img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        px = img.load()
        for y in range(H):
            for x in range(W):
                if out[y][x] is not None:
                    c = out[y][x]
                    p = self.punched.get(tuple(c[:3]))
                    px[x, y] = (p[0], p[1], p[2], c[3]) if p is not None else c
        return clean_orphans(img)

    def stamp(self, img, stamps):
        px = img.load()
        for sx, sy, color, size in stamps:
            x, y = int(sx), int(sy)
            c = hexc(color)
            for dy in range(size):
                if 0 <= x < self.W and 0 <= y + dy < self.H and px[x, y + dy][:3] == EYE[:3]:
                    px[x, y + dy] = c

    def sheets(self):
        out = {}
        for clip, entries in self.meta["clips"].items():
            frames = []
            for i, e in enumerate(entries):
                img = self.pixelize(os.path.join(self.src, "%s_%d" % (clip, i)))
                self.stamp(img, e["stamps"])
                frames.append(ImageOps.mirror(img) if self.meta["mirror"] else img)
            out[clip] = frames
        return out

    def write(self, sheets, art_root):
        directory = os.path.join(art_root, self.meta["folder"])
        os.makedirs(directory, exist_ok=True)
        name = self.meta["name"]
        for clip, frames in sheets.items():
            keep = "%s_%s_%d.png" % (name, clip, len(frames))
            for old in glob.glob(os.path.join(directory, "%s_%s_*.png" % (name, clip))):
                if os.path.basename(old) != keep:
                    os.remove(old)
                    if os.path.exists(old + ".meta"):
                        os.remove(old + ".meta")
            sheet(frames).save(os.path.join(directory, keep))


if __name__ == "__main__":
    root, entity = sys.argv[1], sys.argv[2]
    art_root = sys.argv[sys.argv.index("--out") + 1] if "--out" in sys.argv else ART
    e = Entity(os.path.join(root, entity))
    sh = e.sheets()
    e.write(sh, art_root)
    print("pixelize", entity, {c: len(f) for c, f in sh.items()})
