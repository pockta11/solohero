# Art tools (D-080, D-082)

Scripts that made the project-owned and converted art. Python 3 + Pillow (+ numpy, requests).

| Script | Output |
|---|---|
| `vfxgen.py OUT` | skill VFX strips `vfx{name}_play_{frames}.png` -> `Assets/SoloHero/Art/Vfx` |
| `icongen.py OUT` | skill icons `skill_{id}.png` (24x24; also D-104 `skill_main_{job}` main attacks and `skill_ult_*` ultimates) + `icon_lock.png` -> `Art/Icons/Skills`, `Art/UI/Icons` |
| `sfxgen.py OUT` | synthesized `sfx_skill_{fire,thunder,ice,heal,magic}.wav` -> `Assets/SoloHero/Audio/Sfx` |
| `itchdl.py user/slug ...` | downloads free (name-your-price) itch.io packs as zips |
| `build3d.py [ENTITY ...]` | **current characters**: Blender renders (`chibi3d.py`) -> `pixelize.py` -> `Art/{Hero,Enemies,Bosses}/{entity}_{clip}_{frames}.png` (hero, 8 enemies, 6 bosses incl. `golem`). Needs Blender 5.2 (`winget install BlenderFoundation.Blender`); `--preview out.png` writes a contact sheet |
| `bggen.py` | chapter parallax backgrounds `Backgrounds/Ch{1..5}_*/layer_{0..4}.png` (320x320, 1x pixels, D-097); `--preview out.png` |
| `floorgen.py` | battle floor tiles `Tiles/floor_{1..5}.png` (palettes match bggen) |
| `appicon.py` | app icon `Icons/app_icon_{bg,fg,legacy}.png` + store `icon-512.png` from the knight sheet and the meadow background |
| `hero.py`, `cast.py` | the previous 2D-painted characters (charkit), kept for reference |
| `convert_chars.py SRC ART` | LuizMelo CC0 packs (unzipped under SRC) -> `Art/{Hero,Enemies,Bosses}/{entity}_{clip}_{frames}.png` |

Character packs used (all CC0, License.txt inside each): luizmelo/hero-knight, monsters-creatures-fantasy, martial-hero,
evil-wizard, evil-wizard-2, evil-wizard-3, fantasy-warrior (no longer shipped). After changing art run `Tools > Setup > Build Art`.

3D pipeline: each character is primitives in Blender (1 unit = 1 art pixel) with unlit emission materials that quantise
charkit's key light into 3-tone ramps. Frames render at 4x with a data pass (object index + depth); `pixelize.py`
downsamples by majority, draws 1 px internal lines on the farther part, charkit's selective silhouette outline, stamps
eye highlights and mirrors enemies (hit flashes are a shader in game). Clips: idle / run / attack / dead 12 frames, hit 6, played at 20 fps (D-096).
