# Art tools (D-080, D-082)

Scripts that made the project-owned and converted art. Python 3 + Pillow (+ numpy, requests).

| Script | Output |
|---|---|
| `vfxgen.py OUT` | skill VFX strips `vfx{name}_play_{frames}.png` -> `Assets/SoloHero/Art/Vfx` |
| `icongen.py OUT` | skill icons `skill_{id}.png` (24x24) + `icon_lock.png` -> `Art/Icons/Skills`, `Art/UI/Icons` |
| `sfxgen.py OUT` | synthesized `sfx_skill_{fire,thunder,ice,heal,magic}.wav` -> `Assets/SoloHero/Audio/Sfx` |
| `itchdl.py user/slug ...` | downloads free (name-your-price) itch.io packs as zips |
| `convert_chars.py SRC ART` | LuizMelo CC0 packs (unzipped under SRC) -> `Art/{Hero,Enemies,Bosses}/{entity}_{clip}_{frames}.png` |

Character packs used (all CC0, License.txt inside each): luizmelo/hero-knight, monsters-creatures-fantasy, martial-hero,
evil-wizard, evil-wizard-2, evil-wizard-3, fantasy-warrior. After changing art run `Tools > Setup > Build Art`.
