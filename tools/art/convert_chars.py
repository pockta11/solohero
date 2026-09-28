"""Converts LuizMelo CC0 packs into SoloHero sheets {entity}_{clip}_{frames}.png (D-082).
Frames are cropped to the union of all clips, centred on the idle body, feet on the bottom row (the importer's
default foot pivot = bottom centre). Enemies and bosses are mirrored to face left. Recolours are hue shifts."""
import colorsys, os, sys
from PIL import Image

SRC = sys.argv[1]
ART = sys.argv[2]

MCF = 'monsters-creatures-fantasy/Monsters_Creatures_Fantasy/'
CHARS = {
    # entity: (folder under Art, mirror, float px above ground, {clip: file})
    'knight': ('Hero', False, 0, {
        'idle': 'hero-knight/Hero Knight/Sprites/Idle.png', 'run': 'hero-knight/Hero Knight/Sprites/Run.png',
        'attack': 'hero-knight/Hero Knight/Sprites/Attack1.png', 'hit': 'hero-knight/Hero Knight/Sprites/Take Hit.png',
        'dead': 'hero-knight/Hero Knight/Sprites/Death.png'}),
    'goblin': ('Enemies', True, 0, {
        'idle': MCF + 'Goblin/Idle.png', 'run': MCF + 'Goblin/Run.png', 'attack': MCF + 'Goblin/Attack.png',
        'hit': MCF + 'Goblin/Take Hit.png', 'dead': MCF + 'Goblin/Death.png'}),
    'skeleton': ('Enemies', True, 0, {
        'idle': MCF + 'Skeleton/Idle.png', 'run': MCF + 'Skeleton/Walk.png', 'attack': MCF + 'Skeleton/Attack.png',
        'hit': MCF + 'Skeleton/Take Hit.png', 'dead': MCF + 'Skeleton/Death.png'}),
    'mushroom': ('Enemies', True, 0, {
        'idle': MCF + 'Mushroom/Idle.png', 'run': MCF + 'Mushroom/Run.png', 'attack': MCF + 'Mushroom/Attack.png',
        'hit': MCF + 'Mushroom/Take Hit.png', 'dead': MCF + 'Mushroom/Death.png'}),
    'flyeye': ('Enemies', True, 12, {
        'idle': MCF + 'Flying eye/Flight.png', 'attack': MCF + 'Flying eye/Attack.png',
        'hit': MCF + 'Flying eye/Take Hit.png', 'dead': MCF + 'Flying eye/Death.png'}),
    'ronin': ('Bosses', True, 0, {
        'idle': 'martial-hero/Martial Hero/Sprites/Idle.png', 'run': 'martial-hero/Martial Hero/Sprites/Run.png',
        'attack': 'martial-hero/Martial Hero/Sprites/Attack1.png', 'hit': 'martial-hero/Martial Hero/Sprites/Take Hit.png',
        'dead': 'martial-hero/Martial Hero/Sprites/Death.png'}),
    'necro': ('Bosses', True, 0, {
        'idle': 'evil-wizard-3/Evil Wizard 3/Sprites/Idle.png', 'run': 'evil-wizard-3/Evil Wizard 3/Sprites/Run.png',
        'attack': 'evil-wizard-3/Evil Wizard 3/Sprites/Attack.png', 'hit': 'evil-wizard-3/Evil Wizard 3/Sprites/Get hit.png',
        'dead': 'evil-wizard-3/Evil Wizard 3/Sprites/Death.png'}),
    'ranger': ('Bosses', True, 0, {
        'idle': 'fantasy-warrior/Fantasy Warrior/Sprites/Idle.png', 'run': 'fantasy-warrior/Fantasy Warrior/Sprites/Run.png',
        'attack': 'fantasy-warrior/Fantasy Warrior/Sprites/Attack1.png', 'hit': 'fantasy-warrior/Fantasy Warrior/Sprites/Take hit.png',
        'dead': 'fantasy-warrior/Fantasy Warrior/Sprites/Death.png'}),
    'shadowmage': ('Bosses', True, 0, {
        'idle': 'evil-wizard-2/EVil Wizard 2/Sprites/Idle.png', 'run': 'evil-wizard-2/EVil Wizard 2/Sprites/Run.png',
        'attack': 'evil-wizard-2/EVil Wizard 2/Sprites/Attack1.png', 'hit': 'evil-wizard-2/EVil Wizard 2/Sprites/Take hit.png',
        'dead': 'evil-wizard-2/EVil Wizard 2/Sprites/Death.png'}),
    'firemage': ('Bosses', True, 0, {
        'idle': 'evil-wizard/Evil Wizard/Sprites/Idle.png', 'run': 'evil-wizard/Evil Wizard/Sprites/Move.png',
        'attack': 'evil-wizard/Evil Wizard/Sprites/Attack.png', 'hit': 'evil-wizard/Evil Wizard/Sprites/Take Hit.png',
        'dead': 'evil-wizard/Evil Wizard/Sprites/Death.png'}),
}

# Recolours: new entity <- (base entity, hue shift in turns, saturation multiplier, min saturation to touch)
RECOLOURS = {
    'goblinr': ('goblin', -0.30, 1.1, 0.25),
    'skeletonv': ('skeleton', 0.72, 1.6, 0.05),
    'mushroomb': ('mushroom', 0.55, 1.0, 0.2),
    'flyeyer': ('flyeye', 0.9, 1.3, 0.15),
}


def frames_of(path):
    im = Image.open(os.path.join(SRC, path)).convert('RGBA')
    n = max(1, round(im.width / im.height))
    w = im.width // n
    return [im.crop((i * w, 0, (i + 1) * w, im.height)) for i in range(n)]


def convert(entity, folder, mirror, lift, clips):
    loaded = {c: frames_of(p) for c, p in clips.items()}
    idle0 = loaded['idle'][0]
    ib = idle0.getbbox()
    cx = (ib[0] + ib[2]) / 2
    top, bottom, half = 10 ** 9, 0, 0
    for frames in loaded.values():
        for f in frames:
            b = f.getbbox()
            if not b:
                continue
            top = min(top, b[1])
            bottom = max(bottom, b[3])
            half = max(half, cx - b[0], b[2] - cx)
    # Feet: the idle frame's lowest row is the ground line (death clips may lie a bit lower; they get clipped).
    ground = ib[3] + lift
    half = int(half) + 2
    box = (int(round(cx)) - half, top - 2, int(round(cx)) + half, ground)
    out = {}
    for c, frames in loaded.items():
        cropped = []
        for f in frames:
            g = Image.new('RGBA', (box[2] - box[0], box[3] - box[1]), (0, 0, 0, 0))
            g.paste(f.crop((max(0, box[0]), max(0, box[1]), min(f.width, box[2]), min(f.height, box[3]))),
                    (max(0, -box[0]), max(0, -box[1])))
            if mirror:
                g = g.transpose(Image.FLIP_LEFT_RIGHT)
            cropped.append(g)
        out[c] = cropped
    return out


def save(entity, folder, clips):
    os.makedirs(os.path.join(ART, folder), exist_ok=True)
    for c, frames in clips.items():
        w, h = frames[0].size
        sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
        for i, f in enumerate(frames):
            sheet.paste(f, (i * w, 0))
        sheet.save(os.path.join(ART, folder, '%s_%s_%d.png' % (entity, c, len(frames))))
    print(entity, {c: len(f) for c, f in clips.items()}, 'frame', clips['idle'][0].size)


def recolour(img, shift, sat_mul, min_sat):
    px = img.load()
    out = img.copy()
    po = out.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if s < min_sat:
                continue
            h = (h + shift) % 1.0
            s = min(1.0, s * sat_mul)
            r2, g2, b2 = colorsys.hsv_to_rgb(h, s, v)
            po[x, y] = (int(r2 * 255), int(g2 * 255), int(b2 * 255), a)
    return out


built = {}
for entity, (folder, mirror, lift, clips) in CHARS.items():
    built[entity] = (folder, convert(entity, folder, mirror, lift, clips))
    save(entity, folder, built[entity][1])
for entity, (base, shift, sat, min_sat) in RECOLOURS.items():
    folder, clips = built[base]
    save(entity, folder, {c: [recolour(f, shift, sat, min_sat) for f in fr] for c, fr in clips.items()})
