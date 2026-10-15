"""Smooth (D-168 ink-cel) VFX from the pixel VFX sheets: every flat colour region is redrawn with smooth edges.

Each frame is upsampled per colour label (bilinear indicator + small blur, then argmax), so stair-stepped band edges
become clean curves while the band colours and the dark rims stay as drawn. Bayer-dithered fades become real alpha.
Glow sheets (vfx*glow_*) are soft halos: they are only de-dithered and blurred at their own size (bilinear filtering
in the game keeps them smooth).

Run: python tools/art/vfxhd.py SRC_DIR OUT_DIR [--preview PREVIEW.png] [name ...]
SRC_DIR holds the pixel sheets (vfxgen.py / vfxgen2.py / vfxgen3.py output); OUT_DIR gets the same names at K x.
"""
import glob
import os
import re
import sys

import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter, uniform_filter, zoom

K = 2           # output texels per art pixel (importer PPU 64)
AA = 4          # supersampling before the box downsample
SHEET = re.compile(r"^vfx([a-z0-9]+)_play_(\d+)\.png$")


def dither_mask(alpha):
    """Pixels inside a Bayer-dithered fade: the 4x4 coverage is partial and the pattern flips a lot."""
    on = (alpha > 0).astype(np.float64)
    cover = uniform_filter(on, 4, mode="constant")
    flips = (np.abs(np.diff(on, axis=1, prepend=0)) + np.abs(np.diff(on, axis=0, prepend=0)))
    busy = uniform_filter(flips, 4, mode="constant")
    return (cover > 0.12) & (cover < 0.88) & (busy > 0.45)


def despeckle(lab, passes=2):
    """Majority filter: a pixel with at most one same-label neighbour takes its neighbours' most common label, so
    single-pixel noise and colour dithering become flat cel regions."""
    h, w = lab.shape
    for _ in range(passes):
        pad = np.pad(lab, 1, mode="edge")
        nb = np.stack([pad[1 + dy:1 + dy + h, 1 + dx:1 + dx + w]
                       for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx])
        same = (nb == lab[None]).sum(0)
        counts = (nb[:, None] == nb[None, :]).sum(1)
        mode = np.take_along_axis(nb, counts.argmax(0)[None], 0)[0]
        lab = np.where(same <= 1, mode, lab)
    return lab


def smooth_frame(img):
    a = np.asarray(img.convert("RGBA"), dtype=np.float64)
    h, w = a.shape[:2]
    alpha = a[..., 3]
    soft = dither_mask(alpha)
    # Soft alpha for dithered fades: the coverage over one 4 x 4 Bayer tile, and the tile's mean colour.
    on = (alpha > 0).astype(np.float64)
    cover = uniform_filter(on, 4, mode="constant")
    mean_rgb = np.stack([uniform_filter(a[..., c] * on, 4, mode="constant") for c in range(3)], -1) / np.maximum(cover, 1e-6)[..., None]
    # Fill dither holes with the nearest drawn colour so the soft region has a colour everywhere.
    rgb = a[..., :3].copy()
    filled = alpha > 0
    for _ in range(3):
        if filled.all():
            break
        acc = np.zeros_like(rgb)
        cnt = np.zeros((h, w))
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                sh = np.roll(np.roll(filled, dy, 0), dx, 1)
                acc += np.roll(np.roll(rgb * filled[..., None], dy, 0), dx, 1)
                cnt += sh
        grow = (~filled) & (cnt > 0)
        rgb[grow] = acc[grow] / cnt[grow, None]
        filled |= grow

    # Labels: distinct colours of the drawn pixels (dither holes take their neighbour's label), 0 = empty.
    solid_alpha = np.where(soft, np.maximum(alpha, 255.0 * (filled & soft)), alpha)
    key = (rgb.round().astype(np.int64) * [65536, 256, 1]).sum(-1)
    key = np.where(solid_alpha > 0, key + (solid_alpha.round().astype(np.int64) << 24), -1)
    labels, inv = np.unique(key, return_inverse=True)
    inv = despeckle(inv.reshape(h, w))
    S = K * AA
    best = np.full((h * S, w * S), -1.0)
    pick = np.zeros((h * S, w * S), dtype=np.int64)
    for li in range(len(labels)):
        ind = (inv == li).astype(np.float64)
        up = zoom(np.pad(ind, 1), S, order=1)[S:-S, S:-S]
        up = gaussian_filter(up, 0.34 * S)
        better = up > best
        best[better] = up[better]
        pick[better] = li
    lab = labels[pick]
    empty = lab < 0
    col = np.stack([(lab >> 16) & 255, (lab >> 8) & 255, lab & 255], -1).astype(np.float64)
    al = ((lab >> 24) & 255).astype(np.float64)
    col[empty] = 0
    al[empty] = 0
    # Dithered fades: smooth alpha from the coverage instead of hard pixels.
    soft_up = zoom(np.pad(soft.astype(np.float64), 1), S, order=1)[S:-S, S:-S] > 0.5
    cov_up = gaussian_filter(zoom(np.pad(cover, 1), S, order=1)[S:-S, S:-S], 0.5 * S)
    soft_rgb = np.where((cover > 0)[..., None], mean_rgb, rgb)
    rgb_up = np.stack([gaussian_filter(zoom(np.pad(soft_rgb[..., c], 1, mode="edge"), S, order=1)[S:-S, S:-S], 0.5 * S)
                       for c in range(3)], -1)
    amax = alpha.max() if alpha.max() > 0 else 255.0
    al = np.where(soft_up, np.clip(cov_up, 0, 1) * amax, al)
    col = np.where(soft_up[..., None], rgb_up, col)
    # Premultiplied box downsample.
    pre = np.dstack([col * (al[..., None] / 255.0), al / 255.0])
    H, W = h * K, w * K
    pre = pre.reshape(H, AA, W, AA, 4).mean(axis=(1, 3))
    out = np.zeros((H, W, 4))
    nz = pre[..., 3] > 1e-6
    out[nz, :3] = pre[nz, :3] / pre[nz, 3:4]
    out[..., 3] = pre[..., 3] * 255.0
    return Image.fromarray(np.clip(out + 0.5, 0, 255).astype(np.uint8), "RGBA")


def smooth_glow(img):
    """De-dither a glow halo at its own size: blur alpha and colour (premultiplied)."""
    a = np.asarray(img.convert("RGBA"), dtype=np.float64) / 255.0
    pre = np.dstack([a[..., :3] * a[..., 3:4], a[..., 3:4]])
    pre = np.stack([gaussian_filter(pre[..., c], 1.1) for c in range(4)], -1)
    out = np.zeros_like(pre)
    nz = pre[..., 3] > 1e-4
    out[nz, :3] = pre[nz, :3] / pre[nz, 3:4]
    out[..., 3] = pre[..., 3]
    return Image.fromarray(np.clip(out * 255.0 + 0.5, 0, 255).astype(np.uint8), "RGBA")


def convert(path, out_dir):
    name = os.path.basename(path)
    m = SHEET.match(name)
    sheet = Image.open(path).convert("RGBA")
    n = int(m.group(2))
    fw = sheet.width // n
    glow = m.group(1).endswith("glow")
    frames = [sheet.crop((i * fw, 0, (i + 1) * fw, sheet.height)) for i in range(n)]
    frames = [smooth_glow(f) if glow else smooth_frame(f) for f in frames]
    w, h = frames[0].size
    out = Image.new("RGBA", (w * n, h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        out.paste(f, (i * w, 0))
    out.save(os.path.join(out_dir, name), optimize=True)
    return name, out


def main():
    src, dst = sys.argv[1], sys.argv[2]
    only = [a for i, a in enumerate(sys.argv[3:], 3) if not a.startswith("--") and sys.argv[i - 1] != "--preview"]
    os.makedirs(dst, exist_ok=True)
    done = []
    for path in sorted(glob.glob(os.path.join(src, "vfx*_play_*.png"))):
        m = SHEET.match(os.path.basename(path))
        if not m or (only and m.group(1) not in only):
            continue
        done.append(convert(path, dst))
        print("vfxhd", done[-1][0], flush=True)
    if "--preview" in sys.argv:
        rows = [o for _, o in done]
        W = max(o.width for o in rows)
        img = Image.new("RGBA", (min(W, 3000), sum(o.height + 8 for o in rows)), (58, 63, 88, 255))
        y = 0
        for o in rows:
            img.alpha_composite(o.crop((0, 0, min(o.width, 3000), o.height)), (0, y))
            y += o.height + 8
        img.save(sys.argv[sys.argv.index("--preview") + 1])


if __name__ == "__main__":
    main()
