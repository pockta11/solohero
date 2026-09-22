---
baseline_commit: 0651876044ba1c8dc39faa3c4c4f7d0e7f0c2fb7
---
# Story 1.11: 어셈블리 골격 (asmdef 4 + Core/Common + Formulas + BalanceConfig)

Status: review

<!-- Epic E1-11 · Must · 선행: 1-03 done. 후행: 1-04 부트는 이 asmdef와 Services/BalanceValues 위에 올라간다 -->
<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a 1인 개발자,
I want UnityEngine을 모르는 Core 어셈블리와 그 위의 Game/Editor/EditMode 테스트 경계를 먼저 고정하고, GDD 상수와 공식의 첫 구현을 그 안에 두고,
so that 이후 스토리(부트, 저장, 전투, 성장)가 어셈블리를 다시 쪼개지 않고 같은 공식·같은 상수를 호출한다.

**범위는 골격이다.** 서비스·상태 머신·부트·UI·StatAggregator·DamageCalc·BigNumberFormat·가챠는 만들지 않는다. 완료 기준은 EditMode 테스트 1개 파일(`FormulasTests`)이 녹색인 것이다.

## Acceptance Criteria

1. **asmdef 4개만** 존재하고 참조 방향이 고정된다. `SoloHero.Tests.EditMode → SoloHero.Core`, `SoloHero.Editor → SoloHero.Game → SoloHero.Core`. Core의 `noEngineReferences`는 true. 테스트 asmdef의 references에 Game/Editor/UnityEngine 모듈을 넣지 않는다.
2. **Core/Common**에 `Result`, `FailReason`, `Log`/`LogTag`/`LogLevel`/`ILogSink`/`NullSink`, `Services`, `IClock`/`SystemClock`, `IRandom`/`SystemRandom`, `IsExternalInit` 폴리필이 있다. Core의 `.cs`에 `UnityEngine` 문자열이 0건이다.
3. **`Formulas`**가 `BalanceValues`만 받아 `EnemyHp`, `EnemyAtk`, `StageGold`, `UpgradeCost`, `HitDamage`를 계산한다. 산술 공식은 이 클래스 안에만 있다.
4. **`BalanceConfig` SO**가 `Assets/SoloHero/Data/Config/BalanceConfig.asset`에 있고, 필드명·기본값이 GDD 상수표와 같다. `ToValues()`가 같은 값의 `BalanceValues`를 만든다. 게임 코드에 매직 넘버를 새로 넣지 않는다.
5. **`FormulasTests` 1개**가 Test Runner EditMode에서 녹색이다. 테스트는 `new BalanceValues()`만 쓰고 SO·씬·`UnityEngine`을 로드하지 않는다.
6. **기존 컴파일이 유지된다.** `BuildSpikeProbe`(Game), `BuildAutomator`/`SpikeSceneSetup`/`FontSetupWizard`(Editor), `Scripts/Legacy/`(Assembly-CSharp)가 오류 0으로 컴파일된다. Legacy 파일을 수정하거나 Core/Game에서 Legacy 타입을 참조하지 않는다.

## Tasks / Subtasks

- [x] **T1. asmdef 4개** (AC 1, 6)
  - [x] `Assets/SoloHero/Scripts/Core/SoloHero.Core.asmdef` — `noEngineReferences: true`, `references: []`, `autoReferenced: true`
  - [x] `Assets/SoloHero/Scripts/Game/SoloHero.Game.asmdef` — `references: ["SoloHero.Core"]`만. `overrideReferences: false` (Firebase·GMA DLL 자동 참조를 끄지 말 것). DOTween·UniTask·TMP는 **넣지 않는다**
  - [x] `Assets/SoloHero/Scripts/Editor/SoloHero.Editor.asmdef` — `includePlatforms: ["Editor"]`, `references: ["SoloHero.Core", "SoloHero.Game", "Unity.TextMeshPro"]`. `FontSetupWizard`가 `TMPro`를 쓴다
  - [x] `Assets/SoloHero/Scripts/Tests/EditMode/SoloHero.Tests.EditMode.asmdef` — 아래 JSON 그대로. `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `autoReferenced: false`
  - [x] asmdef는 `Scripts/` 루트나 `Legacy/`에 두지 않는다. 두면 Legacy가 Core/Game으로 끌려 들어간다
- [x] **T2. Core/Common** (AC 2)
  - [x] 파일당 public 타입 1개. 네임스페이스 `SoloHero.Core.Common`. 주석·식별자·로그 문자열은 영문
  - [x] `IsExternalInit`만 예외: 네임스페이스 `System.Runtime.CompilerServices`, `internal static class`. 두 번째 사본을 만들지 않는다
  - [x] `SystemClock`만 `DateTime`을 호출한다. `SystemRandom`만 `System.Random`을 호출한다. 다른 Core 파일에서 `DateTime.` / `new Random` 금지
- [x] **T3. BalanceValues + UpgradeLane + Formulas** (AC 3, 4)
  - [x] `SoloHero.Core.Config.BalanceValues` — 아래 필드 표와 동일한 이름·타입·기본값. 메서드 없음
  - [x] `SoloHero.Core.Growth.UpgradeLane` — `Hp, Atk, Def, Spd`
  - [x] `SoloHero.Core.Formulas` (`Scripts/Core/Formulas.cs`) — 시그니처는 Dev Notes의 코드 블록 그대로
- [x] **T4. BalanceConfig SO** (AC 4)
  - [x] `SoloHero.Game.Config.BalanceConfig : ScriptableObject`, `[CreateAssetMenu(menuName = "SoloHero/Config/Balance")]`
  - [x] 필드 이니셜라이저는 `BalanceValues`와 같은 값. `ToValues()`는 필드별 복사
  - [x] 에셋 경로 `Assets/SoloHero/Data/Config/BalanceConfig.asset`. 스크립트 `.meta`의 guid가 생긴 뒤에 에셋을 만든다 (guid가 어긋나면 Missing Script)
  - [x] **[사용자]** Unity 2022.3.62f3로 프로젝트를 한 번 열어 새 스크립트·asmdef의 `.meta`를 생성하고, 그 guid로 에셋을 만든 뒤 meta와 에셋을 함께 커밋
- [x] **T5. FormulasTests** (AC 5)
  - [x] `Assets/SoloHero/Scripts/Tests/EditMode/FormulasTests.cs`, 네임스페이스 `SoloHero.Tests.EditMode`
  - [x] `new BalanceValues()`만 사용. 아래 케이스 전부 녹색
  - [x] **[사용자]** `Window > General > Test Runner > EditMode > Run All` 녹색. CI는 EditMode를 돌리지 않는다
- [x] **T6. 컴파일 회귀** (AC 6)
  - [x] **[사용자]** 콘솔 오류 0. `BuildSpikeProbe`가 Firebase·`GoogleMobileAds`를 못 찾으면 `overrideReferences`를 true로 바꾸지 말고, DLL이 Auto Reference인지 확인한다
  - [x] **[사용자]** `Tools > Build > Android AAB`가 여전히 성공한다. asmdef는 플레이어 컴파일 경계를 바꾸므로 EditMode 녹색만으로 AC 6을 통과시키지 않는다

## Dev Notes

### 이 스토리가 만드는 것과 만들지 않는 것

아키텍처 First Steps 3번이 이 스토리 전체다. [Source: game-architecture.md#Development Environment › First Steps]

| 만든다 | 만들지 않는다 (담당 스토리) |
|---|---|
| asmdef 4개, Common 타입, `Formulas`, `BalanceValues`, `BalanceConfig` + 에셋, `UpgradeLane`, `FormulasTests` | `BootSequence`(1-04), `UnityLogSink`, `SaveDataV2`(1-05), `StatAggregator`/`DamageCalc`(E2/E4), `BigNumberFormat`(E7-16), 가챠·경제 서비스, 빈 도메인 폴더, DOTween asmdef |

`BigNumberFormat`은 폴더 주석에만 있다. 이 스토리의 Common 목록에 없다. 만들지 않는다.

### asmdef JSON

Core:

```json
{
    "name": "SoloHero.Core",
    "rootNamespace": "SoloHero.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

Game: 위와 같되 `name`/`rootNamespace`는 `SoloHero.Game`, `references`는 `["SoloHero.Core"]`, `noEngineReferences`는 false.

Editor: `includePlatforms: ["Editor"]`, `references: ["SoloHero.Core", "SoloHero.Game", "Unity.TextMeshPro"]`.

Tests (Unity Test Framework 1.1.33. `optionalUnityReferences`는 쓰지 않는다):

```json
{
    "name": "SoloHero.Tests.EditMode",
    "rootNamespace": "SoloHero.Tests.EditMode",
    "references": ["SoloHero.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`rootNamespace`는 새 스크립트 기본값일 뿐이다. 기존 `BuildAutomator`/`SpikeSceneSetup`/`FontSetupWizard`/`BuildSpikeProbe`는 전역 네임스페이스 그대로 둔다. 옮기거나 네임스페이스를 붙이지 않는다.

### 컴파일이 깨지는 지점 (미리 읽기)

지금 `Assets/SoloHero/Scripts/` 아래 스크립트는 전부 `Assembly-CSharp` / `Assembly-CSharp-Editor`다. asmdef를 폴더에 넣는 순간 그 폴더 트리가 그 어셈블리로 이동한다.

| 파일 | 이동 후 | 필요한 참조 |
|---|---|---|
| `Scripts/Editor/FontSetupWizard.cs` | `SoloHero.Editor` | `TMPro` → asmdef 이름 `Unity.TextMeshPro`. 빠지면 Editor 컴파일 실패 |
| `Scripts/Editor/SpikeSceneSetup.cs` | `SoloHero.Editor` | `AddComponent<BuildSpikeProbe>()` → Editor가 Game을 참조해야 한다 |
| `Scripts/Editor/BuildAutomator.cs` | `SoloHero.Editor` | `UnityEditor`만. 추가 참조 없음 |
| `Scripts/Game/Boot/BuildSpikeProbe.cs` | `SoloHero.Game` | `Firebase.*`, `GoogleMobileAds.Api`. 둘 다 **소스 asmdef가 없는 DLL** (`Assets/Firebase/Plugins/*.dll`, `Assets/GoogleMobileAds/*.dll`). `overrideReferences: false`이면 Auto Reference DLL이 보인다 |
| `Scripts/Legacy/*.cs` | 그대로 `Assembly-CSharp` | 이 폴더에는 asmdef를 두지 않는다. Core/Game에서 Legacy 타입을 참조하면 컴파일이 거절되는 것이 맞다 |

DOTween은 `Assets/Plugins/Demigiant`의 **소스**라 기본 어셈블리에 있다. Game asmdef는 그 소스를 보지 못한다. 이 스토리의 Game 스크립트는 DOTween을 쓰지 않으므로 참조를 넣지 않는다. 유틸리티 패널의 Create ASMDEF는 DOTween을 처음 쓰는 스토리(UI 트윈)에서 한다. [Source: project-context.md#Technology Stack — DOTween needs asmdef]

UniTask 패키지(`com.cysharp.unitask`, 태그 v2.5.11)도 이 스토리에서 참조하지 않는다. `BuildSpikeProbe`는 `async void` + Firebase `Task`만 쓴다.

### Common 코드 (이 모양으로 구현)

`Result` / `FailReason` / `Log` / `Services` / `IClock` / `IRandom`은 아키텍처 예시를 그대로 따른다. [Source: game-architecture.md#Implementation Patterns › Error Handling, Logging, Communication Patterns, Time & Random]

```csharp
public readonly struct Result
{
    public readonly bool Ok;
    public readonly FailReason Reason;
    private Result(bool ok, FailReason reason) { Ok = ok; Reason = reason; }
    public static readonly Result Success = new Result(true, FailReason.None);
    public static Result Fail(FailReason reason) => new Result(false, reason);
}

public enum FailReason { None, NotEnoughGold, NotEnoughGem, MaxLevel, OnCooldown, Locked, Busy }

public enum LogLevel { Error, Warn, Info, Debug }
public enum LogTag { Boot, Save, Migrate, Combat, Stage, Growth, Gacha, Economy, Offline, Ad, UI, Audio, Pool }

public interface ILogSink
{
    void Write(LogLevel level, LogTag tag, string message);
}

public sealed class NullSink : ILogSink
{
    public static readonly NullSink Instance = new NullSink();
    private NullSink() { }
    public void Write(LogLevel level, LogTag tag, string message) { }
}

public static class Log
{
    public static ILogSink Sink = NullSink.Instance;
    public static void Error(LogTag tag, string message) => Sink.Write(LogLevel.Error, tag, message);
    public static void Warn(LogTag tag, string message) => Sink.Write(LogLevel.Warn, tag, message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Info(LogTag tag, string message) => Sink.Write(LogLevel.Info, tag, message);
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Debug(LogTag tag, string message) => Sink.Write(LogLevel.Debug, tag, message);
}
```

`[Conditional]`은 `System.Diagnostics`. `noEngineReferences` 어셈블리도 Unity가 `UNITY_EDITOR` / `DEVELOPMENT_BUILD`를 넘기므로 동작한다. `UnityEngine.Debug.Log`는 쓰지 않는다.

```csharp
public static class Services
{
    private static readonly Dictionary<Type, object> Map = new Dictionary<Type, object>();
    public static void Register<T>(T instance) where T : class => Map[typeof(T)] = instance;
    public static T Get<T>() where T : class =>
        Map.TryGetValue(typeof(T), out var found) ? (T)found : throw new InvalidOperationException("service not registered: " + typeof(T).Name);
    public static void Clear() => Map.Clear();
}

public interface IClock { long UtcNowSeconds { get; } DateTime LocalNow { get; } }
public interface IRandom { double NextDouble(); int Next(int maxExclusive); }
```

`SystemClock.UtcNowSeconds`는 `DateTimeOffset.UtcNow.ToUnixTimeSeconds()`. `LocalNow`는 `DateTime.Now` (광고 일일 리셋은 기기 로컬 자정 — 이 스토리에서 리셋 로직은 구현하지 않는다). `SystemRandom`은 생성자로 `System.Random`을 받고, 인자 없는 생성자는 `new System.Random()`이다. 테스트는 이 클래스를 쓰지 않고 페이크를 만든다. 이 스토리의 테스트는 난수·시계가 필요 없다.

`IsExternalInit`:

```csharp
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
```

### 공식

```csharp
public static double EnemyHp(BalanceValues c, int g) =>
    c.ENEMY_HP_BASE * Math.Pow(c.ENEMY_HP_GROWTH, g - 1);

public static double EnemyAtk(BalanceValues c, int g) =>
    c.ENEMY_ATK_BASE * Math.Pow(c.ENEMY_ATK_GROWTH, g - 1);

public static double StageGold(BalanceValues c, int g) =>
    c.STAGE_GOLD_BASE * Math.Pow(c.STAGE_GOLD_GROWTH, g - 1);

public static double UpgradeCost(BalanceValues c, UpgradeLane lane, int level)
{
    double baseCost;
    switch (lane)
    {
        case UpgradeLane.Hp: baseCost = c.UPG_BASE_HP; break;
        case UpgradeLane.Atk: baseCost = c.UPG_BASE_ATK; break;
        case UpgradeLane.Def: baseCost = c.UPG_BASE_DEF; break;
        case UpgradeLane.Spd: baseCost = c.UPG_BASE_SPD; break;
        default: throw new ArgumentOutOfRangeException(nameof(lane));
    }
    return baseCost * Math.Pow(c.UPG_COST_GROWTH, level);
}

public static double HitDamage(BalanceValues c, double enemyAtk, double def)
{
    double defRef = c.DEF_REF_MULT * enemyAtk;
    double denom = defRef + def;
    if (denom == 0d) return 1d;
    return Math.Max(1d, enemyAtk * defRef / denom);
}
```

`g`는 1부터다. `g < 1` 가드는 넣지 않는다. `level`은 강화 **전** 레벨이고, 레벨 0의 비용이 baseCost다. [Source: gdd.md#Number Balancing; game-architecture.md#Configuration Example, Stat & Formula Patterns]

### BalanceValues / BalanceConfig 필드

두 클래스의 필드명·타입·초기값이 같다. `UPPER_SNAKE_CASE`는 이 두 클래스에서만 허용된다. 표에 없는 상수를 지어 내지 않는다.

퍼센트 표기(크리 5%, 가챠 55%)는 **분수로 바꾸지 않고** 표의 수 그대로 둔다 (5, 55). 나누기 100은 그 값을 쓰는 나중 스토리의 일이다. 스킬 3의 피해 배율은 표가 "—"이므로 필드를 만들지 않는다. `UPG_MAX_LEVEL_OTHER`는 "없음"이므로 필드를 만들지 않는다 (HP/ATK/DEF 상한 없음).

| 필드 | 타입 | 기본값 |
|---|---|---|
| HP_BASE, ATK_BASE, DEF_BASE | double | 100, 10, 10 |
| ATKSPD_BASE | double | 1.0 |
| MOVE_SPEED | double | 2.0 |
| ATTACK_RANGE | double | 1.6 |
| CRIT_RATE_BASE | double | 5 |
| CRIT_MULT | double | 1.5 |
| DEF_REF_MULT | double | 8.0 |
| LEVEL_HP_GAIN, LEVEL_ATK_GAIN | double | 20, 2 |
| EXP_REQ_BASE | double | 50 |
| EXP_REQ_GROWTH | double | 1.12 |
| UPG_COST_GROWTH | double | 1.12 |
| UPG_BASE_HP, UPG_BASE_ATK, UPG_BASE_DEF, UPG_BASE_SPD | double | 100, 150, 150, 200 |
| UPG_STAT_MULT | double | 1.16 |
| UPG_GAIN_SPD | double | 0.02 |
| UPG_MAX_LEVEL_SPD | int | 100 |
| UPG_FARM_EXPONENT | double | 1.31 |
| ENEMY_HP_BASE, ENEMY_HP_GROWTH | double | 30, 1.10 |
| ENEMY_ATK_BASE, ENEMY_ATK_GROWTH | double | 5, 1.10 |
| ENEMY_DEF | double | 0 |
| ENEMY_EXP_BASE, ENEMY_EXP_GROWTH | double | 5, 1.10 |
| ENEMY_ATK_INTERVAL | float | 1.2f |
| SPAWN_INTERVAL | float | 0.8f |
| SPAWN_MAX_ALIVE | int | 4 |
| SPAWN_OFFSET_X | double | 1.5 |
| KILL_TARGET_NORMAL | int | 8 |
| BOSS_HP_MULT, BOSS_ATK_MULT | double | 15.0, 1.5 |
| BOSS_ATK_INTERVAL | float | 1.8f |
| BOSS_GOLD_MULT, BOSS_EXP_MULT | double | 5.0, 5.0 |
| BOSS_TIME_LIMIT | int | 30 |
| BOSS_INTRO_TIME | float | 1.5f |
| STAGE_GOLD_BASE, STAGE_GOLD_GROWTH | double | 50, 1.08 |
| CHAPTER_CLEAR_GEM | int | 60 |
| OFFLINE_CAP | int | 21600 |
| OFFLINE_DIVISOR | double | 400 |
| OFFLINE_MIN_SECONDS | int | 60 |
| OFFLINE_AD_MULT | double | 2.0 |
| GACHA_COST_SINGLE, GACHA_COST_TEN | double | 500, 4500 |
| GACHA_COST_TEN_GEM | int | 200 |
| GACHA_RATE_C, GACHA_RATE_R, GACHA_RATE_E, GACHA_RATE_L | double | 55, 33, 10, 2 |
| GACHA_PITY | int | 100 |
| GACHA_PITY_RESET_ON_LEGENDARY | bool | true |
| REFUND_C, REFUND_R, REFUND_E, REFUND_L | double | 50, 200, 800, 3000 |
| SKILL_MULT_1, SKILL_MULT_2 | double | 3.0, 2.0 |
| SKILL_CD_1, SKILL_CD_2, SKILL_CD_3 | float | 6f, 12f, 20f |
| SKILL_UNLOCK_LV_1, SKILL_UNLOCK_LV_2, SKILL_UNLOCK_LV_3 | int | 1, 5, 12 |
| SKILL_MAX_LEVEL | int | 10 |
| SKILL_LEVEL_GAIN | double | 10 |
| SKILL_UPG_BASE_1, SKILL_UPG_BASE_2, SKILL_UPG_BASE_3 | double | 300, 500, 800 |
| SKILL_UPG_COST_GROWTH | double | 1.15 |
| SKILL_SEQUENCE_GAP | float | 0.4f |
| WHIRLWIND_RANGE | double | 3.0 |
| WHIRLWIND_MIN_TARGETS | int | 2 |
| BATTLECRY_ATK_BUFF | double | 30 |
| BATTLECRY_DURATION | float | 8f |
| BATTLECRY_HEAL, BATTLECRY_HP_THRESHOLD | double | 15, 60 |
| STAGES_PER_CHAPTER | int | 10 |
| MVP_CHAPTERS | int | 5 |
| STAGE_CLEAR_DELAY, STAGE_RETRY_DELAY, DEATH_ANIM_TIME | float | 2f, 3f, 1f |
| FAIL_STREAK_PROMPT | int | 3 |
| PPU | int | 32 |
| PIXEL_REF_WIDTH, PIXEL_REF_HEIGHT | int | 270, 480 |
| BATTLE_VIEW_RATIO | int | 55 |
| HERO_SCREEN_X | int | 30 |
| SAVE_DEBOUNCE | int | 250 |

`ToValues()`는 위 필드를 한 줄에 하나씩 복사한다. 리플렉션 복사는 쓰지 않는다 (필드가 빠지면 테스트가 아니라 런타임에서야 드러난다).

### 테스트 케이스

`FormulasTests`. NUnit `[Test]`. double 비교는 `Assert.AreEqual(expected, actual, 1e-9)`.

| 메서드 | 기대 |
|---|---|
| `EnemyHp_StageOne_EqualsBase` | `EnemyHp(c, 1)` == 30 (리터럴) |
| `EnemyHp_StageTwo_AppliesGrowth` | == `30 * Math.Pow(1.10, 1)` |
| `EnemyAtk_StageTwo_AppliesGrowth` | `EnemyAtk(c, 1)` == 5, `EnemyAtk(c, 2)` == `5 * Math.Pow(1.10, 1)` |
| `StageGold_StageOne_EqualsBase` | == 50 |
| `StageGold_StageTwo_AppliesGrowth` | == `50 * Math.Pow(1.08, 1)` |
| `UpgradeCost_HpLevelZero_EqualsBase` | `UpgradeCost(c, Hp, 0)` == 100 |
| `UpgradeCost_EachLaneLevelOne_AppliesGrowth` | Hp 112, Atk 168, Def 168, Spd 224. 기대값은 `base * Math.Pow(1.12, 1)` |
| `HitDamage_DefEqualsRef_IsHalf` | enemyAtk 8, def 64 → 4 |
| `HitDamage_ZeroDef_EqualsAttack` | enemyAtk 5, def 0 → 5 |
| `HitDamage_HugeDef_IsOne` | enemyAtk 1, def 1e9 → 1 |

`c`는 매번 `new BalanceValues()`. 상수를 테스트 안에 또 적어 공식과 따로 놀게 하지 말고, 성장 케이스는 위 식과 같은 `Math.Pow`로 기대값을 만든다. g=1과 레인 base는 리터럴로 고정해 기본값이 표와 같은지 잠근다.

### 이전 스토리에서 가져올 것

- Unity가 만든 `.meta`를 커밋하지 않으면 CI가 다른 guid를 발급한다. 에셋의 `m_Script` guid는 스크립트 meta와 같아야 한다. [Source: 1-03 Dev Notes › 이전 스토리(1-09)]
- `Scripts/Legacy`는 격리 상태다. 이 스토리에서 고치지 않는다. 영문 규칙 유예는 그 폴더 README에만 있다. [Source: 1-03 Review Findings]
- CI는 `Assets/**` 변경으로 Android AAB를 돌리고, EditMode는 돌리지 않는다. 테스트 녹색은 로컬 Test Runner 기록으로 남긴다. 푸시하면 빌드가 약 9–22분 돈다. [Source: .github/workflows/build.yml]
- `google-services.json` / `google-services.xml`은 gitignore다. 빌드 검증 중에 커밋하지 않는다.

### Project Structure Notes

```
Assets/SoloHero/Scripts/
├── Core/SoloHero.Core.asmdef
│   ├── Common/   Result, FailReason, Log, LogTag, LogLevel, ILogSink, NullSink, Services, IClock, SystemClock, IRandom, SystemRandom, IsExternalInit
│   ├── Config/   BalanceValues.cs
│   ├── Growth/   UpgradeLane.cs
│   └── Formulas.cs
├── Game/SoloHero.Game.asmdef
│   ├── Boot/BuildSpikeProbe.cs          (기존, 수정 금지)
│   └── Config/BalanceConfig.cs
├── Editor/SoloHero.Editor.asmdef         (기존 스크립트 3개 + asmdef)
├── Tests/EditMode/SoloHero.Tests.EditMode.asmdef
│   └── FormulasTests.cs
└── Legacy/                               (asmdef 없음)
Assets/SoloHero/Data/Config/BalanceConfig.asset
```

### Project Context Rules

- Core는 `UnityEngine`을 모른다. `Mathf`/`Debug.Log`/`Time`/`Random`/`Vector2` 금지. `System.Math`만 쓴다.
- 어셈블리는 이 4개뿐이다. Core → Game 참조 금지. 테스트는 Core만.
- 코드 안 텍스트는 영문. 한국어는 `Strings` 값에만 두며, 이 스토리에서 Strings는 만들지 않는다.
- 예상 실패는 `Result.Fail`. 이 스토리에서 `Result`를 던지는 코드는 없다. `Services.Get`의 미등록과 `UpgradeCost`의 잘못된 lane만 예외 (프로그래머 오류).
- 벤더 폴더(`Firebase/`, `GoogleMobileAds/`, `Plugins/Demigiant/`, `TextMesh Pro/`)는 수정하지 않는다.
- 매직 넘버 금지. 공식 입력은 `BalanceValues` 필드다.
- 파일명 = 클래스명, 파일당 public 타입 1개. private 필드 `_camelCase`.

### References

- [Source: game-architecture.md#Architectural Decisions D8, ADR-5]
- [Source: game-architecture.md#Project Structure › Directory Structure, Architectural Boundaries]
- [Source: game-architecture.md#Configuration, Logging, Communication Patterns, Time & Random Injection]
- [Source: game-architecture.md#Development Environment › First Steps 3]
- [Source: gdd.md#Number Balancing 상수표]
- [Source: project-context.md#Engine-Specific Rules, Testing Rules, Code Organization Rules]
- [Source: epics.md#E1 — E1-11]
- [Source: 1-03-cleanup-3d-assets.md#Dev Notes, Review Findings]

## Dev Agent Record

### Agent Model Used

Grok 4.7

### Debug Log References

- Unity 2022.3.62f3 `-runTests -testPlatform EditMode`: `Temp/story-1-11-editmode.xml` result=Passed total=10 passed=10 failed=0. Assembly `SoloHero.Tests.EditMode.dll`.
- Unity 2022.3.62f3 `-executeMethod BuildAutomator.Build`: `[Build] result=Succeeded size=511275810 errors=0 warnings=0 time=00:01:48.3626353`. Log has no `error CS`.

### Completion Notes List

- asmdef 4개, Core/Common, `Formulas`, `BalanceValues`, `BalanceConfig` 에셋, `FormulasTests`를 넣었다. `BalanceConfig.cs.meta` guid는 `c4e8a1b27d6f4e0a9c3b5d7e1f2a4b60`.
- Core `.cs`에 `UnityEngine` 문자열 0건.
- 에디터를 닫은 뒤 batchmode로 EditMode 10/10과 Android AAB 성공을 확인했다. `overrideReferences`는 false로 두었고 Firebase·GMA 참조 오류는 없었다.

### File List

- `Assets/SoloHero/Scripts/Core/SoloHero.Core.asmdef`
- `Assets/SoloHero/Scripts/Core/Common/*.cs` (Result, FailReason, Log, LogTag, LogLevel, ILogSink, NullSink, Services, IClock, SystemClock, IRandom, SystemRandom, IsExternalInit)
- `Assets/SoloHero/Scripts/Core/Config/BalanceValues.cs`
- `Assets/SoloHero/Scripts/Core/Growth/UpgradeLane.cs`
- `Assets/SoloHero/Scripts/Core/Formulas.cs`
- `Assets/SoloHero/Scripts/Game/SoloHero.Game.asmdef`
- `Assets/SoloHero/Scripts/Game/Config/BalanceConfig.cs`
- `Assets/SoloHero/Scripts/Editor/SoloHero.Editor.asmdef`
- `Assets/SoloHero/Scripts/Tests/EditMode/SoloHero.Tests.EditMode.asmdef`
- `Assets/SoloHero/Scripts/Tests/EditMode/FormulasTests.cs`
- `Assets/SoloHero/Data/Config/BalanceConfig.asset`
- matching `.meta` files for the new folders, scripts, asmdefs, and the asset

### Change Log

- 2026-09-22: Assembly skeleton implemented. EditMode 10/10 and Android AAB succeeded in batchmode.
