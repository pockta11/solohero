---
project_name: 'SoloHero'
user_name: 'Tae-jun'
date: '2026-09-20'
sections_completed: ['technology_stack', 'engine_rules', 'performance_rules', 'organization_rules', 'testing_rules', 'platform_rules', 'anti_patterns']
status: 'complete'
rule_count: 62
optimized_for_llm: true
source: '_bmad-output/planning-artifacts/architecture/arch-solohero-2026-09-20/game-architecture.md'
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing game code in this project. Focus on unobvious details that agents might otherwise miss._

Authoritative detail lives in `game-architecture.md` (decisions D1–D15, ADR-1–7, patterns with code). This file is the short version you read before touching code. When they disagree, the architecture document wins.

---

## Technology Stack & Versions

| Item | Version / Value | Notes |
|---|---|---|
| Unity | **2022.3.62f3 — pinned.** Last 2022.3 patch available to Personal/Pro; 63f1+ is Extended LTS (Industry/Enterprise only). Do not attempt to upgrade within 2022.3 | **Not Unity 6.** No `Awaitable`, no `Unity.Mathematics` assumptions, C# 9 / .NET Standard 2.1 |
| Render | URP 14.0.12, `Renderer2D` | SRP Batcher on. No 3D renderer |
| 2D | `com.unity.feature.2d` 2.0.1 (Pixel Perfect, Sprite Atlas, 2D Animation) | PPU 32, reference 270×480 |
| Async | UniTask **v2.5.11** (git URL pinned) | Coroutines are banned |
| Tween | DOTween (Assets/Plugins/Demigiant) | Needs asmdef via Utility Panel |
| JSON | Newtonsoft `com.unity.nuget.newtonsoft-json` 3.2.1 | Save v2. `JsonUtility` only to read legacy v1 |
| UI | uGUI + TextMeshPro 3.0.7 | UI Toolkit not used |
| Backend | Firebase Auth (anonymous) / Realtime Database / Analytics via EDM4U | Save node `users/{uid}/v2` |
| Ads | Google Mobile Ads Unity 11.5.0 (Android play-services-ads 25.4.0), rewarded only. App ID lives in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` (GMA 11+), not only in the manifest | Test IDs forced in development builds |
| Tests | Unity Test Framework 1.1.33, EditMode only | |
| Build | Gradle (embedded AGP 7.4.2), custom templates in `Assets/Plugins/Android/`, `BuildAutomator.Build`. CI = GitHub Actions only (Jenkins dormant): plain `docker run unityci/editor` + `.github/scripts/unity-build.sh` (Licensing Client Personal activation, secrets `UNITY_EMAIL`/`UNITY_PASSWORD`; `.ulf`/`UNITY_LICENSE` no longer works). `Free disk space` step is required | **JDK 11 confirmed** (E1-09 result A, 2026-09-21): all 18 `.so` 16 KB-aligned with AGP 7.4.2. Re-run `tools/spike/Check16Kb.ps1` whenever a native SDK is added |
| Removed (do not reintroduce) | Addressables, Input System package, `StreamingAssets/JSON`, `Resources/`, `SingletonMB`, `JsonDataManager`, Box-Muller `GachaSystem` | D1, D2, D9 |

## Critical Implementation Rules

### Engine-Specific Rules

- **`SoloHero.Core` has `noEngineReferences: true`.** No `UnityEngine.*` there — not `Mathf`, `Debug`, `Time`, `Random`, `Vector2`, `Color`. Use `System.Math`, `Log`, `Tick(float dt)`, `IRandom`, `IClock`. It will not compile otherwise.
- Four assemblies only: `SoloHero.Core` ← `SoloHero.Game` ← `SoloHero.Editor`; `SoloHero.Tests.EditMode` references **Core only**. Never add a reference from Core to Game.
- C# 9 positional `record` types need `Core/Common/IsExternalInit.cs` polyfill — it exists; do not duplicate it, do not delete it.
- MonoBehaviour only for things that must exist in a scene: views, presenters, boot, infrastructure adapters, audio. Domain logic (`*Service`, `*Brain`, `StageRunner`, `Formulas`) is plain C# in Core.
- Services are registered **once** in `BootSequence` in the documented order (Config → IClock/IRandom/ILogSink → Save → PlayerState → Economy → Progression/HeroLevel → Equipment/Upgrade/Skill → StatAggregator → Gacha → Offline/AdSlot → Ad/Analytics/Audio). Never call `Services.Register` elsewhere. Resolve with `Services.Get<T>()` in `OnEnable`/`Awake`, not in field initializers.
- Presenter lifecycle: `OnEnable` → `Services.Get`, subscribe, immediate refresh; `OnDisable` → unsubscribe. Every subscription has a matching unsubscription.
- Async: `UniTask` only, pass `this.GetCancellationTokenOnDestroy()`, `.Forget()` only on `UniTaskVoid` methods that catch internally. `await` on Firebase/AdMob happens only inside `Game/Infrastructure/`.
- Native SDK callbacks (Firebase, AdMob) may arrive off the main thread — route through `MainThreadDispatcher` before touching state or raising events.
- Animator receives only Triggers `Attack` / `Hit` / `Die` / `Skill1..3` and Bool `Moving`. State lives in Core enums, never in Animator parameters or `StateMachineBehaviour`.
- Hit detection = Animation Event `OnHitFrame` on the 3rd frame (index 2) of Attack clips → `HitFrameRelay` → `HeroBrain.OnHitFrame` (which re-checks range). Not a physics overlap, not a timer.
- Serialization: `SaveDataV2` fields are `double`/`int`/`bool`/`string`/`List<>`; no properties-only members, no dictionaries, no nullable. `MissingMemberHandling.Ignore`. Types preserved in `Assets/link.xml`.
- ScriptableObjects are read once at boot and mirrored into plain C# (`BalanceValues`, `ContentDefs`). Runtime code never touches SO assets.

### Performance Rules

- Targets: 60 FPS mid-range Android (1% low ≥ 50), ≤ 120 draw calls in battle, ≤ 400 MB after 2 h idle, FPS drop ≤ 10% over 30 min.
- **Zero runtime `Instantiate`/`Destroy` in gameplay.** Enemies, damage text, VFX, SFX sources come from `UnityEngine.Pool.ObjectPool<T>` wrappers (`EnemyPool`, `DamageTextPool`, `VfxPool`, `SfxPool`) pre-warmed at boot. `Instantiate` appears only in a pool's `createFunc`.
- `EnemyBrain` objects are reused via `Reset(def, g, x)`; do not allocate a new brain per spawn.
- No allocations in `Tick`/`Update`: no LINQ, no `foreach` over non-`List<T>`, no string interpolation, no closures, no `new` collections. Damage text formatting uses `BigNumberFormat` with a cached `StringBuilder`.
- No `Find*`, `GetComponent` in hot paths; cache in `Awake`.
- Logging in per-frame paths is forbidden. `Log.Info`/`Log.Debug` are stripped in release via `[Conditional]`; `Log.Error`/`Warn` are not — never call them per frame.
- Max 4 enemies alive (`SPAWN_MAX_ALIVE`); spawn queue waits, it never exceeds.
- Sprites: Point filter, no compression, no mipmaps, PPU 32, packed into 4 atlases (`Hero`, `Enemies`, `UI`, `BG_{Theme}`). `SpriteImportPreset` applies this automatically under `Art/` — do not hand-edit importer settings.
- `lowEffectMode` disables particles and camera shake only; damage text stays. `fps30Mode` sets `Application.targetFrameRate = 30`.

### Code Organization Rules

- Everything the project owns lives under `Assets/SoloHero/`. Vendor folders (`Firebase/`, `GoogleMobileAds/`, `ExternalDependencyManager/`, `Plugins/Demigiant/`, `TextMesh Pro/`) are read-only.
- Folder = namespace: `SoloHero.{Assembly}.{Folder}` (`SoloHero.Core.Gacha`, `SoloHero.Game.UI.Panels`).
- Naming: classes/methods/properties `PascalCase`; private and `[SerializeField]` fields `_camelCase`; interfaces `I*`; services `{Domain}Service` (Core); presenters `{View}Presenter` (Game); events `{Noun}{PastParticiple}` (`GoldChanged`); fallible operations `Try*` returning `Result`; async `*Async`.
- `BalanceConfig` fields are `UPPER_SNAKE_CASE` matching GDD constant names exactly (`ENEMY_HP_GROWTH`). Only this class may break C# casing. No magic numbers anywhere else — read `BalanceValues`.
- All formulas live in `Formulas`, `StatAggregator`, `DamageCalc`, `BigNumberFormat`. Arithmetic on game values outside these four files is a violation.
- Asset names: SO `{Type}_{Name}[_{Variant}]` (`Equipment_Sword_Rare`), prefab `{Category}_{Name}` (`Popup_OfflineReward`), sprite sheets `{entity}_{clip}_{frames}.png` (`slime_run_6.png` — last number drives auto-slicing), audio `bgm_{theme}.ogg` / `sfx_{event}.wav`.
- Equipment/enemy/skill `id` strings equal the SO asset name. Saved data stores these strings.
- Save-state field names come verbatim from the GDD table (`highestStage`, `farmingStage`, `retreatMode`, `pityCount`, `upgradeHp` …). Do not invent synonyms.
- **All text inside code is English** — identifiers, comments, log messages, string keys. Korean appears only in the `Strings` table values. Non-ASCII in a `.cs` file is a review failure.
- Player-facing text: `Strings.Get("toast.not_enough_gold")`. No literal Korean in `.cs` or prefabs.

### Testing Rules

- EditMode tests only, in `Assets/SoloHero/Scripts/Tests/EditMode/`, referencing `SoloHero.Core` only. If a test needs `UnityEngine`, the code under test is in the wrong assembly.
- Required suites (GDD E9-06): `GachaTests` (table sums to 1.0, pity at 100 guarantees Legendary), `FormulasTests` (enemy scaling, stage gold, upgrade cost, ratio defense min 1), `UpgradeServiceTests` (cost, unbounded HP/ATK/DEF, SPD cap 100 → `MaxLevel`), `OfflineRewardTests` (600 s → exact gold, cap 21 600, negative and >2×cap → 0 and reset, <60 s no popup, first run none), `MigrationV1ToV2Tests` (refund math, slot renames, g preserved), `BigNumberFormatTests` (999 → "999", 12 400 → "12.4K", T → "aa").
- Inject `IClock` and `IRandom` fakes; never rely on wall clock or unseeded randomness in tests.
- Test names: `Method_Condition_Expected` (`TryUpgrade_SpdAtMax_ReturnsMaxLevel`).
- `GachaVerifier` (editor menu) is the Monte Carlo check; it is not a substitute for `GachaTests`.

### Platform & Build Rules

- Android only. Portrait locked. Reference 1080×1920; Pixel Perfect Camera reference 270×480 with integer upscale — taller phones show more vertical world, never stretch.
- Target API **36** (Google Play requirement for new apps since 2026-08-31). minSdk 24. 16 KB page alignment must pass Play Console — verified by the E1-09 build spike before any other E1 work.
- Build environment is JDK **11** (decided by the E1-09 spike, result A). Do not bump AGP/Gradle/JDK without re-running the 16 KB check.
- `BuildConfig.useTestAdIds` is forced `true` in development builds. Release checklist (E6-14) swaps to real unit IDs; never hardcode an ad unit ID in code.
- `Assets/google-services.json` is not committed. If missing, boot enters `local` mode (PlayerPrefs only) and the game must still run.
- Input: legacy Input Manager only (`activeInputHandler: 0` since 1-09; the Input System package itself is removed in E1-03). UI via `StandaloneInputModule`; Android back = `Input.GetKeyDown(KeyCode.Escape)` handled solely by `BackKeyRouter` (popup → panel → quit confirm).
- Debug tooling (`DebugPanel`, `PerfOverlay`, cheats) is wrapped in `#if DEVELOPMENT_BUILD || UNITY_EDITOR`. Release builds must not contain it — compile-time, not a runtime flag.
- Save on `OnApplicationPause(true)` and `OnApplicationQuit` via `SaveService.FlushAsync()`; write `lastQuitTimeUtc` there and nowhere else.

### Critical Don't-Miss Rules

- **Mutation order is always: mutate `PlayerState` → raise event → `RequestSave()`.** Only the seven GDD save triggers call `RequestSave` (stage clear, gacha result, equip change, upgrade success, skill level-up, offline claim, pause/quit flush). UI, views, and event handlers never call it.
- Expected failures (not enough gold, max level, on cooldown, locked, busy) return `Result.Fail(reason)` — never throw, never `Debug.LogError`. Exceptions are for programmer errors only.
- External I/O failure is a fallback, not an error state: remote load fails → local backup; ad fails → normal claim stays enabled; auth fails → `local` mode. Nothing ever calls `Application.Quit` or blocks boot.
- Gacha: **confirm result → save → then animate.** Skipping or quitting mid-animation must not lose or duplicate a pull. 10-pull deducts the full price up front; pity counts per pull.
- Gacha is a **cumulative probability table + 100-pull pity**: Common 55 / Rare 33 / Epic 10 / Legendary 2 % (`GACHA_RATE`), pity resets on any Legendary (`GACHA_PITY_RESET_ON_LEGENDARY = true`, a data field on `GachaTable`, not a code constant). The Gaussian/μ-shift design in old code (`GachaSystem.cs`) is dead — do not port it.
- Stat aggregation order (GDD): `(base + level gain) × 1.16^upgrade × equipmentMult × (1 + Σ buffs)`; attack speed additive with cap 3.0; crit additive. UI stat summary and combat call the same `StatAggregator.Compute`.
- Defense is ratio-based with a self-scaling reference: `DEF_REF = 8 × enemyAtk`, `damage = max(1, enemyAtk × DEF_REF / (DEF_REF + def))`. Never subtractive.
- Hero move speed is a constant (2.0 u/s). Attack speed is the only speed stat. "SPD" in legacy v1 data meant move speed and is refunded, not migrated.
- Stage index is one global `g = (chapter − 1) × 10 + stage`; `(chapter, stage)` is derived for display only. Chapters ≥ 6 reuse themes via `(chapter − 1) % 5`.
- Simultaneous-death resolution inside `StageRunner.Tick`: enemy deaths → clear check → boss timer → hero death. Clear beats hero death; boss kill beats timer expiry.
- Offline reward: `elapsed < 0` or `elapsed > 2 × 21 600` → reward 0 and reset `lastQuitTimeUtc` silently. `elapsed < 60` → grant without popup. Unclaimed popup survives restart without double counting (claim is what moves `lastQuitTimeUtc`).
- Ad daily counters reset at **device-local midnight** (`IClock.LocalNow`), not UTC.
- Migration v1 → v2: upgrade levels are **refunded as gold** using the v1 linear cost formula, not copied (×1.16^level would explode). Slot ids `Weapon→Sword`, `Helmet→Helm`. v1 node `users/{uid}` is read-only; v2 lives at `users/{uid}/v2`.
- No `Collider2D`/`Rigidbody2D`/`Physics2D` in gameplay even though the modules remain installed. Range checks are 1-D distance on X.
- No `Resources.Load`, no Addressables, no `StreamingAssets` reads. Everything is a direct SO/prefab reference.
- One panel open at a time via `PanelHost`; popups stack via `PopupHost`; toasts queue via `ToastQueue` (max 4 pending). Do not open UI by calling `SetActive` on panels directly.
- Every spend button is a `TapGuardButton` — disabled on tap, re-enabled after the `Try*` call returns (or the ad callback fires).

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any game code
- Follow ALL rules exactly as documented
- When in doubt, prefer the more restrictive option
- Update this file if new patterns emerge

**For Humans:**

- Keep this file lean and focused on agent needs
- Update when technology stack changes
- Review quarterly for outdated rules
- Remove rules that become obvious over time

Last Updated: 2026-09-20
