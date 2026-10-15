# Art tools (D-080, D-082, D-108, D-168)

Scripts that made the project-owned and converted art. Python 3 + Pillow (+ numpy, requests).

| Script | Output |
|---|---|
| `uikit.py` | D-108 drawing kit (signed-distance anti-aliased shapes, gradients, 9-slice preview) used by the scripts below |
| `uigen3.py [OUT] [--preview P]` | D-108 smooth UI skin `Art/UI/Hd/hd9_*` (9-slice, 1 px = 1 canvas unit) and `hd_*` sprites: candy buttons, cream panel/window/cards, HUD pills and gauges, grade slots, tab plate, ribbon, close button, summon circle, boss band, card faces |
| `icongen3.py [OUT] [--preview P]` | D-108 smooth UI icons `Art/UI/Hd/Icons/hdicon_{name}.png` (128 px; `UiSkin.Icon` prefers them over `Art/UI/Icons`) |
| `equipgen3.py [OUT] [SLOT ...] [--preview P]` | D-108 equipment icons `Art/UI/Hd/Equipment/equip_{slot}_{grade}.png` (grade = material + ornament); D-109 adds gloves, necklace, ring, earring |
| `skillgen3.py [OUT] [ID ...] [--preview P]` | D-108 skill icons `Art/UI/Hd/Skills/skill_{id}.png`: element tile + outlined symbol for every skill, job main attack and ultimate |
| `vfxgen2.py [OUT] [CLIP ...] [--preview P]` | D-108 bolder basic VFX (spark, slash, boom, ring, whirl) and the cross / tornado skill clips, D-109 `mark` (target sigil of the mark skills); deletes the old strips |
| `loadinggen.py [--preview P]` | D-108 / D-168 loading screen backdrop `Art/UI/Hd/Screens/hd_loading_bg.png` (from the ink-cel maps; run after `mapgen_hd.py`) and title logo `Art/UI/Hd/hd_logo.png` |
| `uimock3.py BATTLE.png OUT.png [SKIN]` | layout preview of the v3 skin over a battle screenshot (design check before touching the scene) |
| `vfxgen.py OUT` | skill VFX strips `vfx{name}_play_{frames}.png` -> `Assets/SoloHero/Art/Vfx` |
| `icongen.py OUT` | skill icons `skill_{id}.png` (24x24; also D-104 `skill_main_{job}` main attacks and `skill_ult_*` ultimates) + `icon_lock.png` -> `Art/Icons/Skills`, `Art/UI/Icons` |
| `sfxgen.py OUT` | synthesized `sfx_skill_{fire,thunder,ice,heal,magic}.wav` -> `Assets/SoloHero/Audio/Sfx` |
| `itchdl.py user/slug ...` | downloads free (name-your-price) itch.io packs as zips |
| `build3d.py [ENTITY ...] [--pixel]` | **current characters (D-168 ink cel)**: Blender renders (`chibi3d.py`, x6) -> `hdize.py` -> `Art/{Hero,Enemies,Bosses,Pets}/{entity}_{clip}_{frames}[r{rows}].png` at 3 texels per art pixel (importer PPU 96, bilinear, ASTC). Frames trimmed per clip about the foot pivot; sheets wider than 4096 wrap into rows. `--pixel` = the old x4 render + `pixelize.py`. Full run about 1.5-2 h. Needs Blender 5.2 |
| `hdize.py RENDER_ROOT ENTITY` | D-168 converter: palette snap + punch colours, inner lines, ink outline (#2b1d3a), two eye highlights, x2 box downsample |
| `vfxhd.py SRC OUT [NAME ...]` | D-168 smooth VFX from the pixel sheets (vfxgen / vfxgen2 / vfxgen3 output in SRC): per-colour smooth edges, despeckle, dithered fades -> real alpha; clips x2 (PPU 64), glow sheets de-dithered at 1x |
| `mapgen_hd.py [--preview P]` | D-168 ink-cel maps: the bggen / floorgen themes redrawn smooth at 3 texels per art pixel (PPU 96) - sky gradient, cel clouds, ridges with lit rims, outlined trees and foliage, floor tiles |
| `bggen.py` | (pixel, superseded by `mapgen_hd.py`; keeps the theme table) chapter parallax backgrounds `Backgrounds/Ch{1..5}_*/layer_{0..4}.png` (320x320, 1x pixels, D-097); `--preview out.png` |
| `floorgen.py` | (pixel, superseded by `mapgen_hd.py`) battle floor tiles `Tiles/floor_{1..5}.png` (palettes match bggen) |
| `appicon.py` | app icon `Icons/app_icon_{bg,fg,legacy}.png` + store `icon-512.png` from the knight sheet and the meadow background |
| `hero.py`, `cast.py` | the previous 2D-painted characters (charkit), kept for reference |
| `convert_chars.py SRC ART` | LuizMelo CC0 packs (unzipped under SRC) -> `Art/{Hero,Enemies,Bosses}/{entity}_{clip}_{frames}.png` |

Character packs used (all CC0, License.txt inside each): luizmelo/hero-knight, monsters-creatures-fantasy, martial-hero,
evil-wizard, evil-wizard-2, evil-wizard-3, fantasy-warrior (no longer shipped). After changing art run `Tools > Setup > Build Art`.

3D pipeline: each character is primitives in Blender (1 unit = 1 art pixel) with unlit emission materials that quantise
charkit's key light into 3-tone ramps. Frames render at 4x with a data pass (object index + depth); `pixelize.py`
downsamples by majority, draws 1 px internal lines on the farther part, charkit's selective silhouette outline, stamps
eye highlights and mirrors enemies (hit flashes are a shader in game). Clips: idle / run / attack / dead 12 frames, hit 6, played at 20 fps (D-096).

D-168 ink cel: humanoid heads are 0.75 of the D-090 size with legs 1.3 longer (`SOLOHERO_HEAD_SCALE`, `SOLOHERO_LEG_SCALE`;
mushrooms keep 0.95). Heroes and job looks have a `skill` clip (12 frames: sword dash and rising slash, staff charge and
thrust, bow triple shot) that the game plays on every skill cast. VFX pipeline: run vfxgen.py / vfxgen2.py / vfxgen3.py
into a scratch folder, then `vfxhd.py SCRATCH Assets/SoloHero/Art/Vfx`.
