"""Smooth UI drawing kit (D-108): anti-aliased rounded shapes, gradients, gloss and shadows on numpy RGBA layers.

The v3 UI skin is drawn at 1 sprite pixel = 1 canvas unit (1080 reference width), so edges are signed-distance
anti-aliased instead of pixel stepped. Used by uigen3.py (sprites) and uimock3.py (layout previews).
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def mix(c0, c1, t):
    return tuple(int(round(c0[i] + (c1[i] - c0[i]) * t)) for i in range(4))


def shade(c, k):
    """k > 0 lightens toward white, k < 0 darkens toward black."""
    if k >= 0:
        return tuple(int(round(c[i] + (255 - c[i]) * k)) if i < 3 else c[3] for i in range(4))
    return tuple(int(round(c[i] * (1 + k))) if i < 3 else c[3] for i in range(4))


class Layer:
    """Straight-alpha float RGBA image with 'over' compositing of masks."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), np.float32)
        ys, xs = np.mgrid[0:h, 0:w]
        self.x = xs.astype(np.float32) + 0.5
        self.y = ys.astype(np.float32) + 0.5

    def over(self, cov, color):
        """Composite `color` (RGBA tuple, or (h, w, 4) array) with coverage `cov` (h, w) over the layer."""
        if isinstance(color, np.ndarray):
            src_rgb = color[..., :3].astype(np.float32)
            src_a = cov * (color[..., 3].astype(np.float32) / 255.0)
        else:
            src_rgb = np.array(color[:3], np.float32)
            src_a = cov * (color[3] / 255.0)
        out_a = src_a + self.a * (1 - src_a)
        safe = np.where(out_a > 1e-6, out_a, 1)
        self.rgb = (src_rgb * src_a[..., None] + self.rgb * (self.a * (1 - src_a))[..., None]) / safe[..., None]
        self.a = out_a

    def image(self):
        arr = np.dstack([np.clip(self.rgb, 0, 255), np.clip(self.a * 255, 0, 255)]).astype(np.uint8)
        return Image.fromarray(arr, 'RGBA')

    # Distance fields -------------------------------------------------------------------------------------------
    def rrect(self, x0, y0, x1, y1, r):
        """Signed distance to a rounded rectangle (negative inside)."""
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        hx, hy = (x1 - x0) / 2, (y1 - y0) / 2
        r = min(r, hx, hy)
        qx = np.abs(self.x - cx) - (hx - r)
        qy = np.abs(self.y - cy) - (hy - r)
        outside = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2)
        inside = np.minimum(np.maximum(qx, qy), 0)
        return outside + inside - r

    def circle(self, cx, cy, r):
        return np.sqrt((self.x - cx) ** 2 + (self.y - cy) ** 2) - r

    def ellipse(self, cx, cy, rx, ry):
        # Approximate distance (good enough for AA edges of soft shapes).
        k = np.sqrt(((self.x - cx) / rx) ** 2 + ((self.y - cy) / ry) ** 2)
        return (k - 1) * min(rx, ry)

    def polygon(self, pts):
        """Signed distance to a simple polygon."""
        x, y = self.x, self.y
        n = len(pts)
        d = np.full(x.shape, 1e9, np.float32)
        s = np.ones(x.shape, np.float32)
        for i in range(n):
            ax, ay = pts[i]
            bx, by = pts[(i + 1) % n]
            ex, ey = bx - ax, by - ay
            wx, wy = x - ax, y - ay
            t = np.clip((wx * ex + wy * ey) / (ex * ex + ey * ey), 0, 1)
            px, py = wx - ex * t, wy - ey * t
            d = np.minimum(d, px * px + py * py)
            c1 = y >= ay
            c2 = y < by
            c3 = ex * wy > ey * wx
            flip = (c1 & c2 & c3) | (~c1 & ~c2 & ~c3)
            s = np.where(flip, -s, s)
        return s * np.sqrt(d)


def cov(d, soft=1.0):
    """Coverage from a signed distance: 1 inside, 0 outside, a `soft` px wide anti-aliased edge."""
    return np.clip(0.5 - d / soft, 0, 1)


def band(d, inner, outer):
    """Coverage of the ring between distance `inner` and `outer` (both measured like d, e.g. -6 .. -3)."""
    return np.clip(np.minimum(0.5 - (d - outer), 0.5 + (d - inner)), 0, 1)


def vgrad(layer, y0, y1, c0, c1):
    """(h, w, 4) array: vertical gradient c0 at y0 to c1 at y1."""
    t = np.clip((layer.y - y0) / max(1e-6, (y1 - y0)), 0, 1)[..., None]
    a = np.array(c0, np.float32)
    b = np.array(c1, np.float32)
    return a + (b - a) * t


def vgrad3(layer, y0, y1, c0, cm, c1, mid=0.5):
    t = np.clip((layer.y - y0) / max(1e-6, (y1 - y0)), 0, 1)
    a, m, b = (np.array(c, np.float32) for c in (c0, cm, c1))
    lo = (t / mid)[..., None]
    hi = ((t - mid) / (1 - mid))[..., None]
    return np.where((t < mid)[..., None], a + (m - a) * lo, m + (b - m) * hi)


def radial(layer, cx, cy, r, c0, c1):
    t = np.clip(np.sqrt((layer.x - cx) ** 2 + (layer.y - cy) ** 2) / r, 0, 1)[..., None]
    a = np.array(c0, np.float32)
    b = np.array(c1, np.float32)
    return a + (b - a) * t


def blur_alpha(img, radius):
    """Soft shadow helper: blurred copy of an image's alpha as a black RGBA image."""
    a = img.split()[3].filter(ImageFilter.GaussianBlur(radius))
    out = Image.new('RGBA', img.size, (0, 0, 0, 0))
    out.putalpha(a)
    return out


# Text -----------------------------------------------------------------------------------------------------------
_fonts = {}


def font(path, size):
    key = (path, size)
    if key not in _fonts:
        _fonts[key] = ImageFont.truetype(path, size)
    return _fonts[key]


def text(img, xy, s, path, size, fill, anchor='mm', stroke=0, stroke_fill=None, shadow=None):
    """Draws text; `shadow` = (dx, dy, rgba) drop shadow under the stroked text."""
    d = ImageDraw.Draw(img)
    f = font(path, size)
    if shadow:
        dx, dy, sc = shadow
        d.text((xy[0] + dx, xy[1] + dy), s, font=f, fill=sc, anchor=anchor, stroke_width=stroke, stroke_fill=sc)
    d.text(xy, s, font=f, fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=stroke_fill)


def nine(src, border, w, h, scale=1.0):
    """Unity-style 9-slice of `src` to w x h; `border` in source px, drawn at `scale` (pixelsPerUnitMultiplier^-1)."""
    if isinstance(border, int):
        border = (border, border, border, border)
    bl, bt, br, bb = border
    sw, sh = src.size
    dl, dt, dr, db = (int(round(v * scale)) for v in (bl, bt, br, bb))
    # Unity shrinks borders that do not fit.
    if dl + dr > w:
        k = w / max(1, dl + dr)
        dl, dr = int(dl * k), w - int(dl * k)
    if dt + db > h:
        k = h / max(1, dt + db)
        dt, db = int(dt * k), h - int(dt * k)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    xs = [(0, bl, 0, dl), (bl, sw - br, dl, w - dr), (sw - br, sw, w - dr, w)]
    ys = [(0, bt, 0, dt), (bt, sh - bb, dt, h - db), (sh - bb, sh, h - db, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 <= dx0 or dy1 <= dy0 or sx1 <= sx0 or sy1 <= sy0:
                continue
            part = src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.BILINEAR)
            out.alpha_composite(part, (dx0, dy0))
    return out
