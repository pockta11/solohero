"""Turns chibi3d.py renders into smooth HD character sheets that match the D-108 UI skin (no pixel art).

The render holds flat palette colours (cel bands) plus an object-id / depth pass. At render scale this script
1. snaps every pixel to its palette colour (shown in punch.py's crisper ramps),
2. draws inner lines where an object sits behind another (the farther side takes its material's darkest tone),
3. draws a thick ink outline around the silhouette (the UI's #2b1d3a ink; swing smears get none),
4. paints round eye highlights from the projected 3D points,
then box-downsamples by 2 with premultiplied alpha, so every edge is anti-aliased. Output scale = render scale / 2
(render 6 -> 3 texels per art pixel; the importer gives these sheets PPU 96 so the world size does not change).

Run: python tools/art/hdize.py RENDER_ROOT ENTITY [--out ART_ROOT]
"""
import glob
import json
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from charkit import hexc
from hero import EYE, OUT
from punch import color_map

ART = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "SoloHero", "Art")
LINE_EPS = 0.3      # art pixels of depth before a neighbour counts as in front
DOWN = 2            # supersampling factor (render pixels per output texel)
INNER = 0.42        # inner line width in art pixels
OUTER = 0.75        # silhouette outline width in art pixels
MAX_SHEET = 4096    # widest sheet; longer clips wrap into rows (low-end GPUs cap textures at 4096)


def shift(a, dy, dx, fill):
    """a moved by (dy, dx) with the uncovered edge filled."""
    out = np.full_like(a, fill)
    h, w = a.shape[:2]
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = a[ys, xs]
    return out


def disk(r):
    r = max(1, int(round(r)))
    return [(dy, dx) for dy in range(-r, r + 1) for dx in range(-r, r + 1) if dy * dy + dx * dx <= r * r + r * 0.6 and (dy or dx)]


# Style knobs (design mockups): outline / inner line widths in art pixels, band softening (gaussian sigma in art
# pixels, applied before the lines), a white sticker border outside the ink, and two tones instead of three.
STYLE = {"outer": OUTER, "inner": INNER, "soft": 0.0, "sticker": 0.0, "twotone": False}


class HdEntity:
    def __init__(self, src, style=None):
        self.style = dict(STYLE, **(style or {}))
        self.src = src
        self.meta = json.load(open(os.path.join(src, "meta.json")))
        m = self.meta
        self.W, self.H = m["frame"]
        self.S = m["scale"]
        self.near, self.far = m["depth"]
        punched = color_map(m["palette"]) if os.environ.get("SOLOHERO_PUNCH", "1") != "0" else {}
        cols, mats = [], []
        for mat, hexes in m["palette"].items():
            for h in hexes:
                c = hexc(h)
                if any(tuple(c[:3]) == tuple(x[:3]) for x in cols):
                    continue
                cols.append(c)
                mats.append(mat)
        self.pal = np.array([c[:3] for c in cols], dtype=np.int32)
        shown = [punched.get(tuple(c[:3]), tuple(c[:3])) for c in cols]
        self.shown = np.array([s[:3] for s in shown], dtype=np.float64)
        line = {mat: hexc(hexes[0]) for mat, hexes in m["palette"].items()}
        self.line_rgb = np.array([punched.get(tuple(line[mt][:3]), tuple(line[mt][:3]))[:3] for mt in mats], dtype=np.float64)
        self.noline = np.array([mt in set(m["nolines"]) for mt in mats])
        self.eye_index = [i for i, c in enumerate(cols) if tuple(c[:3]) == tuple(EYE[:3])]
        # Two-tone map: palette index -> the same ramp's middle tone when it is the light one.
        key = [tuple(c[:3]) for c in cols]
        self.two = np.arange(len(cols))
        for mat, hexes in m["palette"].items():
            if len(hexes) == 3:
                lo, mid, hi = (key.index(tuple(hexc(h)[:3])) for h in hexes)
                self.two[hi] = mid

    def frame(self, stem, stamps):
        S = self.S
        rgba = np.asarray(Image.open(stem + "_color.png").convert("RGBA"), dtype=np.int32)
        data = np.asarray(Image.open(stem + "_data.png").convert("RGB"), dtype=np.float64)
        obj = data[..., 0].astype(np.int32)
        depth = self.near + (data[..., 1] + data[..., 2] / 255.0) / 255.0 * (self.far - self.near)
        solid = (rgba[..., 3] > 127) & (obj > 0)
        d = ((rgba[..., None, :3] - self.pal[None, None, :, :]) ** 2).sum(-1)
        idx = d.argmin(-1)
        noline = self.noline[idx] & solid
        st = self.style
        if st["twotone"]:
            # Fold each ramp's light tone into its middle tone (cleaner flat look).
            idx = self.two[idx]
        rgb = self.shown[idx].copy()
        if st["soft"] > 0:
            from PIL import ImageFilter
            m = solid.astype(np.float64)
            pre = Image.fromarray(np.clip(np.dstack([rgb * m[..., None], m * 255]), 0, 255).astype(np.uint8), "RGBA")
            blur = np.asarray(pre.filter(ImageFilter.GaussianBlur(st["soft"] * S)), dtype=np.float64)
            a = blur[..., 3:4] / 255.0
            soft = np.where(a > 1e-3, blur[..., :3] / np.maximum(a, 1e-3), rgb)
            rgb = np.where(solid[..., None], soft, rgb)

        # Inner lines: this pixel is behind a different object within INNER art pixels.
        inner = np.zeros(solid.shape, dtype=bool)
        far = np.where(solid, depth, np.inf)
        for dy, dx in disk(st["inner"] * S):
            o2 = shift(obj, dy, dx, 0)
            s2 = shift(solid & ~noline, dy, dx, False)
            d2 = shift(far, dy, dx, np.inf)
            inner |= solid & ~noline & s2 & (o2 != obj) & (depth > d2 + LINE_EPS)
        rgb[inner] = self.line_rgb[idx[inner]]

        # Eye highlights: round dots on the eye's dark colour.
        if self.eye_index:
            yy, xx = np.mgrid[0:solid.shape[0], 0:solid.shape[1]]
            on_eye = np.isin(idx, self.eye_index) & solid
            for sx, sy, color, size in stamps:
                # A big catch light up front and a small one lower right, like the UI's cute faces.
                cx, cy = (sx + 0.5) * S, (sy + 0.5 * size) * S
                r = 0.5 * S * (size ** 0.8)
                dot = on_eye & ((xx - cx) ** 2 + (yy - cy) ** 2 <= r * r)
                cx2, cy2 = cx + 0.9 * S, cy + 1.7 * S
                r2 = 0.45 * r
                dot |= on_eye & ((xx - cx2) ** 2 + (yy - cy2) ** 2 <= r2 * r2)
                rgb[dot] = hexc(color)[:3]

        # Silhouette outline in ink around everything but the swing smears.
        body = solid & ~noline
        ring = np.zeros(solid.shape, dtype=bool)
        for dy, dx in disk(st["outer"] * S):
            ring |= shift(body, dy, dx, False)
        ring &= ~solid
        sticker = np.zeros(solid.shape, dtype=bool)
        if st["sticker"] > 0:
            for dy, dx in disk((st["outer"] + st["sticker"]) * S):
                sticker |= shift(body, dy, dx, False)
            sticker &= ~solid & ~ring
        alpha = np.where(solid | ring | sticker, 1.0, 0.0)
        rgb[ring] = OUT[:3]
        rgb[sticker] = (255, 255, 255)
        # Smears keep their soft look: partly transparent.
        alpha[noline] = 0.85

        # Premultiplied box downsample -> anti-aliased edges.
        h, w = alpha.shape
        h2, w2 = h // DOWN, w // DOWN
        pre = np.dstack([rgb * alpha[..., None], alpha])[:h2 * DOWN, :w2 * DOWN]
        pre = pre.reshape(h2, DOWN, w2, DOWN, 4).mean(axis=(1, 3))
        a = pre[..., 3]
        out = np.zeros((h2, w2, 4), dtype=np.float64)
        nz = a > 1e-6
        out[nz, :3] = pre[nz, :3] / a[nz, None]
        out[..., 3] = a * 255.0
        return Image.fromarray(np.clip(out + 0.5, 0, 255).astype(np.uint8), "RGBA")

    def sheets(self):
        out = {}
        for clip, entries in self.meta["clips"].items():
            frames = []
            for i, e in enumerate(entries):
                img = self.frame(os.path.join(self.src, "%s_%d" % (clip, i)), e["stamps"])
                frames.append(img.transpose(Image.FLIP_LEFT_RIGHT) if self.meta["mirror"] else img)
            out[clip] = frames
        return out

    def write(self, sheets, art_root):
        """Each clip -> one sheet. Frames are trimmed to the clip's opaque union, symmetric about the frame centre
        (the importer's foot pivot) with the bottom row kept, so the pivot does not move; widths stay a multiple
        of 6 (ASTC blocks never straddle two frames). Sheets wider than MAX_SHEET wrap into rows:
        {name}_{clip}_{frames}r{rows}.png, read left to right, top to bottom."""
        directory = os.path.join(art_root, self.meta["folder"])
        os.makedirs(directory, exist_ok=True)
        name = self.meta["name"]
        for clip, frames in sheets.items():
            w, h = frames[0].size
            alpha = np.zeros((h, w), dtype=bool)
            for f in frames:
                alpha |= np.asarray(f)[..., 3] > 0
            ys, xs = np.nonzero(alpha)
            half = max(w / 2.0 - xs.min(), xs.max() + 1 - w / 2.0) if xs.size else w / 2.0
            half = min(w // 2, int(math.ceil((half + 1) / 3.0)) * 3)
            top = max(0, ys.min() - 2) if ys.size else 0
            top -= (h - top) % 6 and (6 - (h - top) % 6)
            top = max(0, top)
            box = (w // 2 - half, top, w // 2 + half, h)
            frames = [f.crop(box) for f in frames]
            fw, fh = frames[0].size
            cols = len(frames)
            while cols * fw > MAX_SHEET:
                cols = (cols + 1) // 2
            rows = (len(frames) + cols - 1) // cols
            keep = "%s_%s_%d%s.png" % (name, clip, len(frames), "r%d" % rows if rows > 1 else "")
            for old in glob.glob(os.path.join(directory, "%s_%s_*.png" % (name, clip))):
                if os.path.basename(old) != keep:
                    os.remove(old)
                    if os.path.exists(old + ".meta"):
                        os.remove(old + ".meta")
            img = Image.new("RGBA", (fw * cols, fh * rows), (0, 0, 0, 0))
            for i, f in enumerate(frames):
                img.paste(f, ((i % cols) * fw, (i // cols) * fh))
            img.save(os.path.join(directory, keep), optimize=True)

if __name__ == "__main__":
    root, entity = sys.argv[1], sys.argv[2]
    art_root = sys.argv[sys.argv.index("--out") + 1] if "--out" in sys.argv else ART
    e = HdEntity(os.path.join(root, entity))
    sh = e.sheets()
    e.write(sh, art_root)
    print("hdize", entity, {c: len(f) for c, f in sh.items()})
