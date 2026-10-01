# Art & font licenses (E8-01)

Every third-party file under `Assets/SoloHero/Art/` is listed here. Only licenses that allow commercial use in a
closed-source mobile game are accepted. Files were renamed / sliced / layer-merged / hue-shifted (the r, v, b, d, n variants) for the project; the licenses
below allow that. Retrieved 2026-09-27.

| Project files | Source | Author | License | Attribution |
|---|---|---|---|---|
| `Hero/knight_*.png`, `Enemies/*`, `Bosses/*` (incl. `golem`) | Modelled and rendered for this project in Blender (`tools/art/build3d.py` = `chibi3d.py` + `pixelize.py`, 2026-10-01, D-096) | SoloHero | Project-owned | - |
| `Tiles/floor_{1..5}.png` | Drawn for this project (`tools/art/floorgen.py`, 2026-09-29, D-091; recoloured 2026-10-01, D-097) | SoloHero | Project-owned | - |
| `Vfx/vfxboom_play_6.png`, `Tiles/ground_beam.png` | Kings and Pigs — https://pixelfrog-assets.itch.io/kings-and-pigs (mirror: https://opengameart.org/content/kings-and-pigs) | Pixel Frog | CC0 1.0 (stated by the author on itch.io; the OGA mirror page lists CC-BY 4.0 — the author's own statement governs) | Not required; credited in-game credits as courtesy |
| `Tiles/shadow_blob.png` | Drawn for this project (2026-09-28, D-082) | SoloHero | Project-owned | - |
| `Backgrounds/Ch{1..5}_*/layer_{0..4}.png` | Drawn for this project (`tools/art/bggen.py`, 2026-10-01, D-097) | SoloHero | Project-owned | - |
| `Vfx/vfxslash_*`, `Vfx/vfxwhirl_*`, `Vfx/vfxring_*`, `Vfx/vfxspark_*`, `UI/ui_card_back.png`, `UI/ui_card_face.png`, `UI/ui_summon_circle.png`, `UI/ui9_*.png` (UI skin), `UI/ui_white.png`, `UI/Icons/icon_*.png`, `Icons/Equipment/*`, `Icons/app_icon_*` (app icon: `tools/art/appicon.py`, 2026-10-01) | Drawn for this project (procedural pixel art, 2026-09-27) | SoloHero | Project-owned | - |
| `Vfx/vfx{fire,bolt,ice,poison,meteor,holy,tornado,swords,heal,shield,aura,vortex,breath,phoenix,spear,judge,cross,clock}_*`, `Icons/Skills/skill_*.png`, `UI/Icons/icon_lock.png`, `../Audio/Sfx/sfx_skill_{fire,thunder,ice,heal,magic}.wav` | Drawn / synthesized for this project (procedural pixel art and synthesized audio, 2026-09-28, D-080) | SoloHero | Project-owned | - |
| `Fonts/Galmuri11.ttf` | Galmuri v2.40.4 — https://github.com/quiple/galmuri | Lee Minseo (quiple) | SIL Open Font License 1.1 (`Fonts/Galmuri-OFL.md`) | Keep the OFL text with the font; the font may be embedded in the app, not sold on its own |

Rules
- Add a row before adding any file. No row, no file.
- CC-BY assets need an in-game credits line (settings panel, E7-09). None are used yet.
- Do not add assets whose license forbids redistribution inside an app bundle.
