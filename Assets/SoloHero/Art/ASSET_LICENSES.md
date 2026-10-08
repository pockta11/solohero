# Art & font licenses (E8-01)

Every file under `Assets/SoloHero/Art/` is listed here. Only licenses that allow commercial use in a closed-source mobile
game are accepted. Re-checked against the folder on 2026-10-08: everything the game ships is project-made except the
font, whose license text travels with the game (settings > credits).

| Project files | Source | Author | License | Attribution |
|---|---|---|---|---|
| `Hero/*` (knight, knight1/2/4 and the job looks jobmage, jobpyro, jobcryo, jobarcher, jobranger, jobsniper), `Enemies/*`, `Bosses/*` | Modelled and rendered for this project in Blender (`tools/art/build3d.py` = `chibi3d.py` + `pixelize.py`; D-096, D-104, D-110, D-131) | SoloHero | Project-owned | - |
| `Pets/pet*` (16 pets) | Same pipeline (`chibi3d.py` `build_pet`, D-114) | SoloHero | Project-owned | - |
| `Backgrounds/Ch{1..10}_*/layer_{0..4}.png` | Drawn for this project (`tools/art/bggen.py`; D-097, D-131) | SoloHero | Project-owned | - |
| `Tiles/floor_{1..10}.png`, `Tiles/shadow_blob.png` | Drawn for this project (`tools/art/floorgen.py`; D-091, D-097, D-131; shadow D-082) | SoloHero | Project-owned | - |
| `UI/Hd/**` (smooth UI skin, UI icons, equipment icons for the seven grades, skill icons) | Drawn for this project (`tools/art/uigen3.py`, `icongen3.py`, `equipgen3.py`, `skillgen3.py`; D-108, D-113) | SoloHero | Project-owned | - |
| `UI/ui9_*.png`, `UI/ui_*.png` (earlier pixel skin, summon circle, cards, loading background), `UI/Icons/icon_*.png` | Drawn for this project (`tools/art/uigen.py`, `icongen.py`, `loadinggen.py`; 2026-09-27..10-06) | SoloHero | Project-owned | - |
| `Icons/Equipment/*` (first pixel set), `Icons/Skills/*` (pixel skill icons), `Icons/app_icon_{fg,bg,legacy}.png` | Drawn for this project (procedural pixel art 2026-09-28: E8-07, D-080, D-104; app icon `tools/art/appicon.py`) | SoloHero | Project-owned | - |
| `Vfx/vfx*_play_*.png` (all 26 clips) | Drawn for this project (`tools/art/vfxgen.py`, `vfxgen2.py`, `fxgen.py`; D-080, D-098, D-108) | SoloHero | Project-owned | - |
| `Materials/*.mat`, `Placeholder/Unit.png` | Made in this project | SoloHero | Project-owned | - |
| `Tiles/ground_beam.png` | Kings and Pigs - https://pixelfrog-assets.itch.io/kings-and-pigs | Pixel Frog | CC0 1.0 (the author's statement on itch.io) | Not required. Only the scene builder's fallback floor (ArtBuilder); no scene references it, so it is not in the app |
| `Fonts/SoloHeroJua.ttf` | Jua (https://github.com/google/fonts/tree/main/ofl/jua) with the glyphs × → ▶ from Do Hyeon (https://github.com/google/fonts/tree/main/ofl/dohyeon), merged and renamed for this project (D-106) | The Jua Project Authors, The Do Hyeon Project Authors | SIL Open Font License 1.1 (`Fonts/SoloHeroJua-OFL.md`) | Copyright lines and the OFL text are in the in-game credits (`Data/Strings/credits_ko.txt`); the font may be embedded in the app, never sold on its own, and keeps no Reserved Font Name |

Audio has its own sheet: `Assets/SoloHero/Audio/ASSET_LICENSES.md`.

Rules
- Add a row before adding any file. No row, no file.
- CC-BY assets need an in-game credits line (settings > credits). None are used.
- Do not add assets whose license forbids redistribution inside an app bundle.
- Removed 2026-10-08: `Fonts/Galmuri11.ttf` (OFL; unused since the D-106 font change).
