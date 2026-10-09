"""D-150: recolours existing 3D character sheets with punch.py's crisper ramps, without rendering again.

The sheets hold only chibi3d.PAL colours (plus the outline and eye colour), so swapping every palette colour for its
punched version gives the same result as pixelizing fresh renders with the punched palette. PAL is read from
chibi3d.py's source (that script needs Blender's bpy to import).

Run: python tools/art/repalette.py [--check] [--preview out.png] [SHEET.png ...]
Without sheets it recolours every sheet under Art/Hero, Art/Enemies, Art/Bosses and Art/Pets. Sheets already
recoloured are skipped (a marker in the PNG text chunk). --check lists what would change; --preview writes a
before/after board of a few looks instead.
"""
import ast
import glob
import os
import sys

from PIL import Image, PngImagePlugin

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from punch import color_map

ART = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "SoloHero", "Art"))
FOLDERS = ("Hero", "Enemies", "Bosses", "Pets")
MARK_KEY = "solohero"
MARK = "punch-d150"


def load_pal():
    tree = ast.parse(open(os.path.join(HERE, "chibi3d.py"), encoding="utf-8").read())
    for node in tree.body:
        if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id == "PAL" for t in node.targets):
            return ast.literal_eval(node.value)
    raise SystemExit("PAL not found in chibi3d.py")


def recolour(img, mapping):
    img = img.convert("RGBA")
    px = img.load()
    changed = 0
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            new = mapping.get((r, g, b))
            if new is not None and new != (r, g, b):
                px[x, y] = (new[0], new[1], new[2], a)
                changed += 1
    return img, changed


def is_marked(path):
    try:
        return Image.open(path).text.get(MARK_KEY) == MARK
    except Exception:
        return False


def sheets(args):
    if args:
        return args
    found = []
    for folder in FOLDERS:
        found += sorted(glob.glob(os.path.join(ART, folder, "*.png")))
    return found


def preview(mapping, out):
    picks = [("Hero", "jobpyro_idle_12"), ("Hero", "knight_idle_12"), ("Hero", "jobarcher_idle_12"),
             ("Enemies", "goblin_run_12"), ("Enemies", "skeleton_run_12"), ("Enemies", "mushroom_run_12"),
             ("Enemies", "flyeye_run_12"), ("Pets", "petslime_idle_12"), ("Pets", "petphoenix_idle_12"),
             ("Pets", "petbunny_idle_12"), ("Bosses", "golem_idle_12")]
    scale = 4
    tiles = []
    for folder, stem in picks:
        path = os.path.join(ART, folder, stem + ".png")
        if not os.path.exists(path):
            continue
        sheet = Image.open(path).convert("RGBA")
        n = int(stem.rsplit("_", 1)[1])
        fw = sheet.width // n
        frame = sheet.crop((0, 0, fw, sheet.height))
        after, _ = recolour(frame, mapping)
        tiles.append((frame, after))
    bg_path = os.path.join(os.environ.get("PREVIEW_BG", ""))
    cell_w = max(f.width for f, _ in tiles) * scale
    cell_h = max(f.height for f, _ in tiles) * scale
    board = Image.new("RGBA", (cell_w * len(tiles), cell_h * 2), (74, 96, 64, 255))
    if bg_path and os.path.exists(bg_path):
        bg = Image.open(bg_path).convert("RGBA").resize(board.size)
        board.alpha_composite(bg)
    for i, (before, after) in enumerate(tiles):
        for row, frame in enumerate((before, after)):
            big = frame.resize((frame.width * scale, frame.height * scale), Image.NEAREST)
            board.alpha_composite(big, (i * cell_w + (cell_w - big.width) // 2, row * cell_h + cell_h - big.height))
    board.save(out)
    print("preview", out, board.size)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if "--preview" in sys.argv:
        out = sys.argv[sys.argv.index("--preview") + 1]
        args = [a for a in args if a != out]
    mapping = color_map(load_pal())
    if "--preview" in sys.argv:
        preview(mapping, out)
        return
    total = 0
    for path in sheets(args):
        if is_marked(path):
            continue
        img, changed = recolour(Image.open(path), mapping)
        total += 1
        if "--check" in sys.argv:
            print("would recolour", os.path.relpath(path, ART), changed, "px")
            continue
        info = PngImagePlugin.PngInfo()
        info.add_text(MARK_KEY, MARK)
        img.save(path, pnginfo=info)
        print("recoloured", os.path.relpath(path, ART), changed, "px")
    print("sheets", total)


if __name__ == "__main__":
    main()
