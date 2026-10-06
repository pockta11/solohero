"""Layout preview of the v3 skin (D-108) on a battle screenshot: python tools/art/uimock3.py BATTLE.png OUT.png [SKIN_DIR]

Composes the HUD, skill bar, character panel and tab bar from the generated hd sprites with the same unit sizes as
GameUiBuilder (1080 x 2400 canvas), so layout and colour decisions can be judged before touching the scene.
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from uikit import nine, text  # noqa: E402

ART = os.path.join(HERE, '..', '..', 'Assets', 'SoloHero', 'Art')
FONT = os.path.join(ART, 'Fonts', 'SoloHeroJua.ttf')
SKIN = sys.argv[3] if len(sys.argv) > 3 else os.path.join(ART, 'UI', 'Hd')
W, H = 1080, 2400

INK = (42, 31, 61, 255)
BROWN = (92, 58, 40, 255)
MUTED = (150, 118, 90, 255)
GREEN_TXT = (40, 160, 70, 255)
WHITE = (255, 255, 255, 255)
GOLD_TXT = (255, 214, 92, 255)


def sk(name):
    for f in os.listdir(SKIN):
        if f.startswith(name + '_') or f == name + '.png':
            if f.endswith('.png'):
                im = Image.open(os.path.join(SKIN, f)).convert('RGBA')
                border = 0
                if f.startswith('hd9_'):
                    spec = f[:-4].split('_')[-1].split('x')
                    side = int(spec[0])
                    cap = int(spec[1]) if len(spec) > 1 else side
                    border = (side, cap, side, cap)
                return im, border
    raise KeyError(name)


def put9(img, name, x, y, w, h, scale=1.0):
    src, b = sk(name)
    img.alpha_composite(nine(src, b, int(w), int(h), scale), (int(x), int(y)))


def put(img, name, x, y, w=None, h=None):
    src, _ = sk(name)
    if w:
        src = src.resize((int(w), int(h or w)), Image.LANCZOS)
    img.alpha_composite(src, (int(x), int(y)))


def icon(img, name, x, y, size, sub='UI/Icons/icon_'):
    src = Image.open(os.path.join(ART, sub + name + '.png')).convert('RGBA')
    src = src.resize((int(size), int(size * src.height / src.width)), Image.NEAREST)
    img.alpha_composite(src, (int(x), int(y)))


def label(img, x, y, s, size, fill=WHITE, anchor='mm', outline=True):
    if outline:
        text(img, (x, y), s, FONT, size, fill, anchor, stroke=4, stroke_fill=INK, shadow=(0, 4, (20, 12, 30, 170)))
    else:
        text(img, (x, y), s, FONT, size, fill, anchor)


def button(img, tone, x, y, w, h, s, size=40, ic=None):
    put9(img, 'hd9_btn' + tone, x, y, w, h)
    cx = x + w / 2
    if ic:
        icon(img, ic, x + 18, y + (h - 8) / 2 - 20, 40)
        cx += 22
    label(img, cx, y + (h - 8) / 2, s, size)


def main():
    battle = Image.open(sys.argv[1]).convert('RGBA')
    img = Image.new('RGBA', (W, H), (0, 0, 0, 255))
    img.alpha_composite(battle.resize((W, H)), (0, 0))

    # Top HUD --------------------------------------------------------------------------------------------------
    put9(img, 'hd9_hudbar', 0, 0, W, 132)
    put(img, 'hd_avatar_disc', 14, 10)
    face = Image.open(os.path.join(ART, 'Hero', 'jobarcher_idle_12.png')).convert('RGBA').crop((0, 0, 96, 72))
    face = face.resize((96 * 4, 72 * 4), Image.NEAREST).crop((96, 40, 96 + 200, 40 + 200)).resize((104, 104), Image.NEAREST)
    mask = Image.new('L', (112, 112), 0)
    from PIL import ImageDraw
    ImageDraw.Draw(mask).ellipse((10, 10, 102, 102), fill=255)
    tile = Image.new('RGBA', (112, 112), (0, 0, 0, 0))
    tile.alpha_composite(face, (4, 10))
    tile.putalpha(Image.composite(tile.split()[3], Image.new('L', (112, 112), 0), mask))
    img.alpha_composite(tile, (14, 10))
    put(img, 'hd_avatar_ring', 14, 10)
    put9(img, 'hd9_lvbadge', 22, 98, 96, 38)
    label(img, 70, 116, 'Lv 24', 26)
    label(img, 140, 34, '궁수', 30, anchor='lm')
    put9(img, 'hd9_gauge', 138, 54, 300, 30)
    put9(img, 'hd9_fillred', 141, 57, 230, 24)
    label(img, 428, 69, '249', 24, anchor='rm')
    put9(img, 'hd9_gauge', 138, 90, 300, 24)
    put9(img, 'hd9_fillblue', 141, 93, 200, 18)
    label(img, 150, 102, 'EXP', 20, (170, 230, 255, 255), anchor='lm')
    label(img, 428, 102, '73.7%', 20, anchor='rm')
    put9(img, 'hd9_pill', 700, 22, 196, 58)
    icon(img, 'coin', 694, 17, 64)
    label(img, 880, 51, '18.6K', 34, GOLD_TXT, anchor='rm')
    put9(img, 'hd9_pill', 908, 22, 160, 58)
    icon(img, 'gem', 900, 17, 64)
    label(img, 1052, 51, '60', 34, (160, 225, 255, 255), anchor='rm')

    # Stage plate under the bar.
    put9(img, 'hd9_pill', 400, 140, 280, 92)
    label(img, 540, 176, '2-7', 46)
    put9(img, 'hd9_gauge', 430, 202, 220, 20)
    put9(img, 'hd9_fillgold', 432, 204, 70, 16)

    # Rails.
    for k, (tone, ic, cap) in enumerate([('gold', 'coin', '골드 2배'), ('purple', 'gem', '젬 +5')]):
        y = 160 + k * 170
        put(img, 'hd_round' + tone, 20, y, 112, 112)
        icon(img, ic, 44, y + 20, 64)
        icon(img, 'tv', 96, y - 6, 40)
        put9(img, 'hd9_caption', 14, y + 112, 124, 38)
        label(img, 76, y + 131, cap, 24)
    put9(img, 'hd9_railtile', 948, 160, 112, 112)
    icon(img, 'menu', 972, 182, 64)
    put(img, 'hd_dot', 1036, 152)

    # Challenge pill next to the stage plate.
    button(img, 'orange', 690, 150, 190, 84, '도전', 38, ic='skull')

    # Skill bar ---------------------------------------------------------------------------------------------------
    top = 1250
    put9(img, 'hd9_slotr', 24, top - 120, 104, 104)
    icon(img, 'main_archer', 40, top - 104, 72, sub='Icons/Skills/skill_')
    label(img, 76, top - 6, '더블 샷', 24)
    put9(img, 'hd9_btngreen', 840, top - 112, 216, 84)
    label(img, 948, top - 74, 'AUTO', 36)
    slots = [('r', 'iron_skin', '14'), ('c', 'arrow_shot', '2'), ('r', 'scatter_shot', '7'), (None, None, ''), ('lock', None, 'Lv 30'), ('lock', None, 'Lv 45')]
    for i, (g, sid, t) in enumerate(slots):
        x = 30 + i * 172
        y = top
        if g in ('c', 'r', 'e', 'l'):
            put9(img, 'hd9_slot' + g, x, y, 128, 128)
            icon(img, sid, x + 16, y + 16, 96, sub='Icons/Skills/skill_')
            label(img, x + 116, y + 108, t, 34, anchor='rm')
        else:
            put9(img, 'hd9_slotdark', x, y, 128, 128)
            if g == 'lock':
                icon(img, 'lock', x + 40, y + 22, 48)
                label(img, x + 64, y + 96, t, 26)

    # Panel ----------------------------------------------------------------------------------------------------------
    ptop = H - 960
    pbot = H - 168
    put9(img, 'hd9_panel', 0, ptop, W, pbot - ptop + 30)
    # Header.
    put9(img, 'hd9_plateblue', 30, ptop + 26, 420, 76)
    icon(img, 'crown', 44, ptop + 36, 52)
    label(img, 110, ptop + 62, '궁수', 40, anchor='lm')
    label(img, 430, ptop + 62, 'Lv 24', 36, GOLD_TXT, anchor='rm')
    button(img, 'orange', 760, ptop + 22, 290, 88, '전직 Lv 30', 36)
    # Stats: three columns, two rows on an inset.
    put9(img, 'hd9_inset', 30, ptop + 122, W - 60, 128)
    stats = [('heart', '체력', '248'), ('atk', '공격력', '101'), ('def', '방어력', '21'), ('spd', '공격속도', '1.20/초'), ('crit', '치명타', '10%'), ('burst', '치명 피해', '150%')]
    for i, (ic, name, val) in enumerate(stats):
        col, row = i % 3, i // 3
        x = 46 + col * 336
        y = ptop + 134 + row * 58
        icon(img, ic, x, y + 4, 44)
        text(img, (x + 54, y + 27), name, FONT, 28, MUTED, 'lm')
        text(img, (x + 316, y + 27), val, FONT, 30, BROWN, 'rm')
    # Upgrade cards 2x2.
    lanes = [('heart', '체력', 'Lv 0', '248', '288', '300'), ('atk', '공격력', 'Lv 10', '101', '117', '1.4K'),
             ('def', '방어력', 'Lv 0', '21', '24', '450'), ('spd', '공격속도', 'Lv 7', '1.20', '1.22', '1.3K')]
    gx, gy, gw, gh = 30, ptop + 270, W - 60, pbot - ptop - 270 - 26
    cw, chh = (gw - 16) / 2, (gh - 16) / 2
    for i, (ic, name, lv, a, b, cost) in enumerate(lanes):
        col, row = i % 2, i // 2
        x = gx + col * (cw + 16)
        y = gy + row * (chh + 16)
        put9(img, 'hd9_card', x, y, cw, chh)
        put9(img, 'hd9_slotr' if i % 2 else 'hd9_slotc', x + 16, y + 16, 84, 84)
        icon(img, ic, x + 26, y + 26, 64)
        text(img, (x + 116, y + 44), name, FONT, 38, BROWN, 'lm')
        put9(img, 'hd9_lvbadge', x + cw - 132, y + 22, 112, 44)
        label(img, x + cw - 76, y + 43, lv, 28)
        text(img, (x + 116, y + 92), a, FONT, 34, MUTED, 'lm')
        text(img, (x + 116 + 20 * len(a) + 16, y + 92), '▶', FONT, 28, GREEN_TXT, 'lm')
        text(img, (x + 116 + 20 * len(a) + 52, y + 92), b, FONT, 38, GREEN_TXT, 'lm')
        button(img, 'green', x + 16, y + chh - 104, cw - 32, 92, cost, 40, ic='coin')

    # Tab bar ---------------------------------------------------------------------------------------------------------
    put9(img, 'hd9_tabbar', 0, H - 168, W, 168)
    tabs = [('crown', '캐릭터'), ('helm', '장비'), ('star', '소환'), ('book', '스킬'), ('burst', '특성')]
    for i, (ic, name) in enumerate(tabs):
        x0 = i * W / 5
        if i == 0:
            put9(img, 'hd9_tabactive', x0 + 14, H - 160, W / 5 - 28, 150)
            icon(img, ic, x0 + W / 10 - 40, H - 150, 80)
            label(img, x0 + W / 10, H - 46, name, 32)
        else:
            icon(img, ic, x0 + W / 10 - 32, H - 140, 64)
            label(img, x0 + W / 10, H - 50, name, 30, (210, 214, 245, 255))
        if i == 4:
            put(img, 'hd_dot', x0 + W / 5 - 54, H - 158)

    img.convert('RGB').save(sys.argv[2])
    print('wrote', sys.argv[2])


if __name__ == '__main__':
    main()
