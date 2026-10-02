"""Builds every 3D character: Blender render (chibi3d.py) then pixel conversion (pixelize.py), plus a preview sheet.

Run: python tools/art/build3d.py [ENTITY ...] [--blender PATH] [--renders DIR] [--out ART_ROOT] [--preview out.png]
Default entities: all looks in chibi3d.LOOKS. Default Blender: C:/Program Files/Blender Foundation/Blender 5.2.
After writing into Assets run `Tools > Setup > Build Art` in Unity.
"""
import os
import subprocess
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from charkit import hexc
from pixelize import ART, Entity

ENTITIES = ["knight", "knight1", "knight2", "knight4", "jobmage", "jobpyro", "jobcryo", "jobarcher", "jobranger", "jobsniper", "petslime", "petwisp", "petowl", "petdragon", "goblin", "goblinr", "skeleton", "skeletonv", "mushroom", "mushroomb", "flyeye", "flyeyer",
            "ronin", "necro", "ranger", "shadowmage", "firemage", "golem"]


def opt(name, default):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


def main():
    flags = {"--blender", "--renders", "--out", "--preview"}
    args, skip = [], False
    for a in sys.argv[1:]:
        if skip:
            skip = False
        elif a in flags:
            skip = True
        else:
            args.append(a)
    entities = args or ENTITIES
    blender = opt("--blender", r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe")
    renders = opt("--renders", os.path.join(tempfile.gettempdir(), "solohero_renders"))
    art_root = opt("--out", ART)

    def run(entity):
        r = subprocess.run([blender, "-b", "--factory-startup", "-P", os.path.join(HERE, "chibi3d.py"), "--",
                            renders, entity], capture_output=True, text=True)
        if "chibi3d done" not in r.stdout:
            raise SystemExit("blender failed for %s:\n%s\n%s" % (entity, r.stdout[-3000:], r.stderr[-3000:]))
        e = Entity(os.path.join(renders, entity))
        sheets = e.sheets()
        e.write(sheets, art_root)
        print("built", entity, {c: len(f) for c, f in sheets.items()}, flush=True)
        return entity, sheets

    with ThreadPoolExecutor(max_workers=4) as pool:
        results = dict(pool.map(run, entities))

    if "--preview" in sys.argv:
        scale = 3
        rows = []
        for entity in entities:
            s = results[entity]
            pick = [s["idle"][0], s["attack"][min(2, len(s["attack"]) - 1)], s["hit"][0], s["dead"][-1]]
            if "run" in s:
                pick.insert(1, s["run"][2])
            rows.append(pick)
        cw = max(f.width for r in rows for f in r)
        img = Image.new("RGBA", (5 * cw * scale, sum(r[0].height for r in rows) * scale), hexc("#3a3f58"))
        y = 0
        for frames in rows:
            for i, f in enumerate(frames):
                img.alpha_composite(f.resize((f.width * scale, f.height * scale), Image.NEAREST), (i * cw * scale, y))
            y += frames[0].height * scale
        img.save(opt("--preview", "preview.png"))


if __name__ == "__main__":
    main()
