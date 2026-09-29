"""Character kit for SoloHero sprites (D-086).

Characters are posed skeletons of simple volumes (ellipses, capsules, superellipse boxes, polygons). Each frame is
rasterised at native resolution (1 art pixel = 1 screen-grid pixel) without anti-aliasing:

1. Parts are painted back to front into an id buffer, each pixel keeping the part's surface normal.
2. Normals are lit by one key light (top-left, toward the viewer) and quantised into the part's hue-shifted ramp.
3. A 1 px internal line separates overlapping parts (the back part darkens where the front part covers it).
4. A selective outline wraps the silhouette: the darkest shade of the touching part, a warmer mid shade on the
   lit top-left edges.
5. Hand-placed details (eyes, gems, buckles) are stamped last.
"""
import math

from PIL import Image

LIGHT = (-0.55, 0.62, 0.56)
_ln = math.sqrt(sum(c * c for c in LIGHT))
LIGHT = tuple(c / _ln for c in LIGHT)


def hexc(s):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


def ramp(*hexes):
    return tuple(hexc(h) for h in hexes)


class Part:
    """A lit volume. shape(x, y) returns a surface normal (nx, ny, nz) or None when (x, y) is outside."""

    def __init__(self, name, shape, colors, thresholds=None, flat=None, outline=None):
        self.name = name
        self.shape = shape
        self.colors = colors
        self.thresholds = thresholds or default_thresholds(len(colors))
        self.flat = flat
        self.outline = outline if outline is not None else colors[0]


def default_thresholds(n):
    # Most of a surface sits in the two middle tones; the brightest tone is a small rim of highlight.
    if n == 5:
        return (0.05, 0.32, 0.62, 0.86)
    if n == 4:
        return (0.12, 0.45, 0.82)
    if n == 3:
        return (0.2, 0.62)
    return tuple((i + 1) / n for i in range(n - 1))


def light(n):
    return max(0.0, n[0] * LIGHT[0] + n[1] * LIGHT[1] + n[2] * LIGHT[2])


def _norm(x, y, z):
    d = math.sqrt(x * x + y * y + z * z) or 1.0
    return (x / d, y / d, z / d)


def ellipse(cx, cy, rx, ry, rot=0.0, bulge=1.0):
    c, s = math.cos(rot), math.sin(rot)

    def f(x, y):
        dx, dy = x - cx, y - cy
        u = (dx * c + dy * s) / rx
        v = (-dx * s + dy * c) / ry
        d = u * u + v * v
        if d > 1.0:
            return None
        z = math.sqrt(max(0.0, 1.0 - d)) * bulge
        nx, ny = u * c - v * s, u * s + v * c
        return _norm(nx, ny, z)

    return f


def capsule(x1, y1, x2, y2, r1, r2=None):
    r2 = r1 if r2 is None else r2
    vx, vy = x2 - x1, y2 - y1
    ll = vx * vx + vy * vy or 1e-6

    def f(x, y):
        t = ((x - x1) * vx + (y - y1) * vy) / ll
        t = min(1.0, max(0.0, t))
        px, py = x1 + vx * t, y1 + vy * t
        r = r1 + (r2 - r1) * t
        dx, dy = x - px, y - py
        d = math.sqrt(dx * dx + dy * dy)
        if d > r:
            return None
        z = math.sqrt(max(0.0, r * r - d * d))
        return _norm(dx, dy, z)

    return f


def superbox(cx, cy, hw, hh, power=4.0, rot=0.0, bulge=1.0):
    c, s = math.cos(rot), math.sin(rot)

    def f(x, y):
        dx, dy = x - cx, y - cy
        u = (dx * c + dy * s) / hw
        v = (-dx * s + dy * c) / hh
        d = abs(u) ** power + abs(v) ** power
        if d > 1.0:
            return None
        gu = math.copysign(abs(u) ** (power - 1), u)
        gv = math.copysign(abs(v) ** (power - 1), v)
        z = (1.0 - d) ** 0.5 * bulge
        nx, ny = gu * c - gv * s, gu * s + gv * c
        return _norm(nx, ny, z * 1.3)

    return f


def polygon(points, normal=(0.0, 0.0, 1.0), curve=None):
    """Filled polygon. curve(x, y) may return a normal to shade it like cloth; otherwise the fixed normal."""
    n = _norm(*normal)

    def f(x, y):
        inside = False
        j = len(points) - 1
        for i in range(len(points)):
            xi, yi = points[i]
            xj, yj = points[j]
            if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
                inside = not inside
            j = i
        if not inside:
            return None
        return curve(x, y) if curve else n

    return f


def union(*shapes):
    def f(x, y):
        for s in shapes:
            r = s(x, y)
            if r is not None:
                return r
        return None

    return f


def minus(shape, cut):
    def f(x, y):
        if cut(x, y) is not None:
            return None
        return shape(x, y)

    return f


def clip_y(shape, y_min=None, y_max=None):
    def f(x, y):
        if y_min is not None and y < y_min:
            return None
        if y_max is not None and y > y_max:
            return None
        return shape(x, y)

    return f


class Canvas:
    """One frame. World coordinates: x right, y up, origin at the feet (bottom centre of the frame)."""

    def __init__(self, w, h, foot_x=None):
        self.w, self.h = w, h
        self.fx = w // 2 if foot_x is None else foot_x
        self.id = [[-1] * w for _ in range(h)]
        self.col = [[None] * w for _ in range(h)]
        self.parts = []

    def to_px(self, x, y):
        return self.fx + x, self.h - 1 - y

    def paint(self, part):
        pid = len(self.parts)
        self.parts.append(part)
        for py in range(self.h):
            wy = self.h - 1 - py + 0.5
            for px in range(self.w):
                wx = px - self.fx + 0.5
                n = part.shape(wx, wy)
                if n is None:
                    continue
                prev = self.id[py][px]
                self.id[py][px] = pid
                self.col[py][px] = self._shade(part, n)
                # Internal line: the covered part darkens along the new part's edge.
                if prev >= 0:
                    pass
        return pid

    def _shade(self, part, n):
        if part.flat is not None:
            return part.flat
        v = light(n)
        idx = 0
        for t in part.thresholds:
            if v >= t:
                idx += 1
        return part.colors[min(idx, len(part.colors) - 1)]

    def set(self, x, y, color, pid=None):
        px, py = self.to_px(x, y)
        if 0 <= px < self.w and 0 <= py < self.h:
            self.col[py][px] = color
            if pid is not None:
                self.id[py][px] = pid
            elif self.id[py][px] < 0:
                self.id[py][px] = len(self.parts) + 100

    def stamp(self, x0, y0, rows, palette, flip=False):
        """rows: strings top to bottom; (x0, y0) is the top-left in world units; '.' is skipped."""
        for r, line in enumerate(rows):
            for c, ch in enumerate(line):
                if ch == "." or ch == " ":
                    continue
                cx = x0 - c if flip else x0 + c
                self.set(cx, y0 - r, palette[ch])

    def finish(self, outline_dark, rim=None, inner_lines=True):
        h, w = self.h, self.w
        ids, col = self.id, self.col
        out = [row[:] for row in col]
        if inner_lines:
            # A pixel whose right/below/left neighbour belongs to a part painted later (in front) takes its own
            # part's darkest tone: a 1 px separation line on the back part only.
            for y in range(h):
                for x in range(w):
                    a = ids[y][x]
                    if a < 0 or a >= len(self.parts):
                        continue
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h:
                            b = ids[ny][nx]
                            if b > a and b < len(self.parts) and self._separate(self.parts[a], self.parts[b]):
                                out[y][x] = self.parts[a].outline
                                break
        # Silhouette outline: dark by default, the part's own dark tone where the light hits (top-left).
        final = [row[:] for row in out]
        for y in range(h):
            for x in range(w):
                if ids[y][x] >= 0:
                    continue
                touch = None
                lit = False
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and ids[ny][nx] >= 0:
                        touch = ids[ny][nx]
                        if dx == 1 or dy == 1:
                            lit = True
                if touch is None:
                    continue
                part = self.parts[touch] if touch < len(self.parts) else None
                if part is not None and lit and rim:
                    final[y][x] = part.outline
                else:
                    final[y][x] = outline_dark
        self.final = final

    @staticmethod
    def _separate(back, front):
        """Parts get a line between them when they are different materials (a different ramp)."""
        if back.outline != front.outline:
            return True
        return back.colors is not front.colors

    def clean(self):
        """Orphan cleanup on the finished frame, before hand-drawn details (eyes, marks) are stamped on top."""
        w, h = self.w, self.h
        src = [row[:] for row in self.final]
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                c = src[y][x]
                if c is None:
                    continue
                ns = [src[y][x + 1], src[y][x - 1], src[y + 1][x], src[y - 1][x]]
                if any(n is None for n in ns) or any(n == c for n in ns):
                    continue
                counts = {}
                for n in ns:
                    counts[n] = counts.get(n, 0) + 1
                best = max(counts.items(), key=lambda kv: kv[1])
                if best[1] >= 3:
                    self.final[y][x] = best[0]

    def image(self):
        img = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        px = img.load()
        for y in range(self.h):
            for x in range(self.w):
                c = self.final[y][x]
                if c is not None:
                    px[x, y] = c
        return img


def clean_orphans(img):
    """Single pixels of a tone surrounded by one other tone are merged (shading noise from quantisation)."""
    w, h = img.size
    src = img.load()
    out = img.copy()
    dst = out.load()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            c = src[x, y]
            if c[3] == 0:
                continue
            ns = [src[x + 1, y], src[x - 1, y], src[x, y + 1], src[x, y - 1]]
            if any(n[3] == 0 for n in ns):
                continue
            if all(n != c for n in ns):
                counts = {}
                for n in ns:
                    counts[n] = counts.get(n, 0) + 1
                best = max(counts.items(), key=lambda kv: kv[1])
                if best[1] >= 3:
                    dst[x, y] = best[0]
    return out


def sheet(frames):
    w, h = frames[0].size
    img = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        img.alpha_composite(f, (i * w, 0))
    return img


def rot(px, py, ox, oy, a):
    c, s = math.cos(a), math.sin(a)
    dx, dy = px - ox, py - oy
    return ox + dx * c - dy * s, oy + dx * s + dy * c


def limb(x, y, a1, l1, a2, l2):
    """Two-segment limb from (x, y): absolute angles in radians (0 = right, -pi/2 = down). Returns joint, end."""
    jx, jy = x + math.cos(a1) * l1, y + math.sin(a1) * l1
    ex, ey = jx + math.cos(a2) * l2, jy + math.sin(a2) * l2
    return (jx, jy), (ex, ey)
