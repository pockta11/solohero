---
title: PRD 입력 문서 추출본 (extract-inputs)
project: SoloHero 2D 리빌드
created: 2026-09-19
sources:
  - "[PP]  docs/2d/PLANNING_PROMPT.md (2D 리빌드 기획 프롬프트, §3 = 확정 결정)"
  - "[MVP] docs/SoloHero_MVP_개발계획서.txt (3D 시대 MVP 계획서)"
  - "[SS]  docs/soulstrikeSummary.md (레퍼런스 게임 Soul Strike 분석)"
  - "[CL]  CLAUDE.md (3D As-Is 구현 상태)"
  - "[code] Assets/Scripts/... (문서에 없는 수치의 코드 확인용 — 보조 출처, 명시적으로 표기)"
---

# PRD 입력 문서 추출본

> 목적: 4개 입력 문서의 사실만 구조화. 제품 아이디어 추가 없음. 각 항목 끝의 `[출처 §]`로 원문 위치 표시.
> 표기: [PP]=PLANNING_PROMPT, [MVP]=MVP 개발계획서, [SS]=soulstrikeSummary, [CL]=CLAUDE.md, [code]=소스코드 확인.

---

## 1. 확정 결정 (To-Be, 변경 불가)

### 1-1. §3 확정 결정 D1~D8 — 되묻지 않음 [PP §1-2, §3]

| # | 항목 | 결정 (원문 기준) | 출처 |
|---|---|---|---|
| D1 | 전투 뷰 | **2D 횡스크롤 사이드뷰.** 히어로가 오른쪽으로 자동 전진, 적 웨이브가 오른쪽에서 등장. 배경 패럴랙스 스크롤 | [PP §3 D1] |
| D2 | 화면 방향 | **Portrait 1080×1920** (CanvasScaler 기준 해상도). 현재 Landscape는 폐기 | [PP §3 D2] |
| D3 | 코드 | **같은 레포에서 `Assets/Scripts` 전면 재설계.** 기존 코드는 참고용(패턴·공식)으로만. Firebase/AdMob/EDM4U/Addressables/빌드 파이프라인 등 **인프라는 유지** | [PP §3 D3] |
| D4 | 아트 | **픽셀아트**, 무료 에셋(itch.io / Asset Store / Kenney 등) 기반 | [PP §3 D4] |
| D5 | 장르 | 자동 전투 **방치형 RPG** (버섯커 키우기·세븐나이츠 키우기·소울스트라이크 계열). 조작은 최소, 스킬 자동 발동 | [PP §3 D5] |
| D6 | 엔진 | Unity 2022.3.62f3 유지, URP는 **2D Renderer**로 전환, UniTask·DOTween·Newtonsoft 유지 | [PP §3 D6] |
| D7 | 차별점 | **정규분포(Box-Muller) + 천장 가챠** 계승, 오프라인 보상 + 광고 2배 계승 | [PP §3 D7] |
| D8 | 지금 단계 | **문서만 작성한다.** 코드 전환은 문서 완성 후 별도 세션에서 `08_migration_plan.md`를 따라 진행 | [PP §3 D8] |

### 1-2. 절대 규칙 (문서 작성 방식에 대한 고정 제약) [PP §1]

| # | 규칙 | 출처 |
|---|---|---|
| R1 | 코드·에셋·프로젝트 설정 수정 금지 (`Assets/`, `Packages/`, `ProjectSettings/`, `CLAUDE.md`, `Jenkinsfile`, `.github/`). 파일 생성은 `docs/2d/` 하위만 (검증 스크립트는 `docs/2d/tools/`) | [PP §1-1] |
| R2 | §3 확정 결정은 되묻지 않는다 | [PP §1-2] |
| R3 | 모르는 것은 질문 대신 **"가정"으로 명시하고 진행**. 각 문서 상단 `## 가정` 섹션. 질문은 답에 따라 문서가 크게 갈릴 때만, 한 번에 최대 3개 | [PP §1-3] |
| R4 | 문서 본문 **한국어**. 코드 식별자·파일명·필드명·enum 값·씬명은 **영어** (.cs 내 한국어 인코딩 깨짐 이력 → English-only 컨벤션) | [PP §1-4] |
| R5 | 수치에 "적절히 / 나중에 조정 / TBD" 금지. **초기값 + 조정 방법** 필수 | [PP §1-5] |
| R6 | 상수의 단일 출처는 `05_balance_data.md`. 다른 문서는 `→ 05 §x.y` 참조만 (값 복사 금지) | [PP §1-6] |
| R7 | 검증하지 않은 외부 사실(에셋 라이선스, 스토어 정책 세부)은 **`[확인 필요]`** 태그 | [PP §1-7] |
| R8 | 산문보다 표·번호 목록·mermaid·ASCII 와이어프레임 우선 | [PP §1-8] |
| R9 | 파일 하나 = 주제 하나. 600줄 초과 시 하위 파일 분리 | [PP §1-9] |
| R10 | 마지막에 교차 검증 패스(§6-4) 수행·보고 | [PP §1-10] |

### 1-3. 계승이 확정된 기존 원칙 [PP §2-3, §7; MVP §0]

- MVP 개발계획서의 **형식과 원칙은 그대로 계승** — 루프 우선 / 스키마 고정 / 저장 안전성 우선 / 범위 통제 [PP §2-3, §7; MVP §0]
- MVP 문서의 **DoD, 저장 정책(4.3 충돌 정책·4.4 저장 트리거), QA 시나리오 S1~S10, 리스크** 계승 [PP §2-3]
- 산출물은 "개발자가 문서만 보고 태스크를 시작할 수 있을 만큼 구체적(클래스명·필드명·수치·경로)", "문서 간 모순 없음", "모든 수치에 근거(공식+시뮬레이션)" [PP §0]
- 역할 정의: "1인 개발 모바일 방치형 RPG SoloHero의 리드 기획자 겸 테크니컬 디렉터" [PP §0]

**확정 결정 수: D1~D8 = 8건 (+ 절대 규칙 10건, 계승 원칙 4건)**

---

## 2. As-Is 시스템 인벤토리 (3D 기준, 총 ~5,900줄 / 49개 .cs) [PP §2-2]

| 영역 | As-Is 파일 | As-Is 내용 | PLANNING_PROMPT 판단 | 출처 |
|---|---|---|---|---|
| 저장 | `Assets/Scripts/Managers/SaveManager.cs` | Firebase RTDB 읽기/쓰기 + PlayerPrefs 로컬 백업(강제 종료 대응) + **250ms 디바운스 큐** | **개념 재활용 가치 높음** → 08에서 "SaveManager 개념 이식 방법" 기술 | [PP §2-2, §4-08-6; CL 인벤토리] |
| DTO | `Assets/Scripts/Data/PlayerData.cs` | v1 스키마: gold(long), chapter, stageNumber, equipped×4, ownedEquipmentCsv, upgrade×4, gachaPullCount, lastQuitTimeUtc, dataVersion=1 | **v2 설계 + 마이그레이션 기준점** (v1→v2 매핑표 필수) | [PP §2-2, §4-02-7] |
| 가챠 | `Assets/Scripts/Systems/GachaSystem.cs` | Box-Muller 정규분포 + 100회 천장(Legendary 보장) + pity 진행도에 따라 μ 0.3→0.8 이동 | **이 프로젝트의 차별점이므로 반드시 계승** (+ 등급 확장, 1회/10회, 연출, 확률 공시표) | [PP §2-2, §3 D7, §4-01-6] |
| 강화 | `Assets/Scripts/Systems/UpgradeService.cs` | HP/ATK/DEF/SPD, 비용 `baseCost×(level+1)`, 최대 50 | **방치형 곡선으로 재설계 대상** | [PP §2-2] |
| 장비 | `Assets/Scripts/Systems/PlayerEquipmentService.cs`, `Data/EquipmentData.cs` | 4슬롯(Sword/Helm/Armor/Boots) × 4등급 SO 16종; 가챠 결과 자동 장착/인벤 처리; `PlayerEquipmentApplier`가 장비+강화 보너스 합산 → 스탯 적용 | 판단 명시 없음 (GDD에서 "슬롯·등급·스탯·중복 처리 규칙" 재정의 요구) | [PP §2-2, §4-01-5; CL 인벤토리] |
| 오프라인 | `Assets/Scripts/Systems/OfflineRewardSystem.cs`, `UI/OfflineRewardPopup.cs` | 최대 21,600초, 초당 골드 고정값, 광고 2배 연결됨; DOTween 팝업 | **계승** (D7) — 상한·계산 기준·광고 2배·수령 플로우 재정의 | [PP §2-2, §3 D7, §4-01-7; CL 오프라인] |
| 광고 | `Assets/Scripts/Managers/AdMobService.cs`, `MainThreadDispatcher.cs` | Google Mobile Ads v25.0.0 보상형; **테스트 유닛 ID 사용 중** | 인프라 유지(D3); 슬롯 목록·일일 제한·실패 처리 재정의 | [PP §2-1, §2-2, §4-01-8] |
| 진입 | `Assets/Scripts/Managers/GameManager.cs` | 익명 로그인 → SaveManager 로드 → 오프라인 계산 → GameScene | 부트스트랩 순서로 계승 (Firebase init → 익명 auth → 로드 → **마이그레이션** → 오프라인 계산 → Main) | [PP §2-2, §4-02-3; CL] |
| 스테이지 | `Assets/Scripts/Managers/StageManager.cs` | 공식 기반 kills/gold (→§5); OnEnemyKilled → StageClear 코루틴 → 3초 후 자동 진행 | 공식 패턴은 참고; 챕터 구조·보스·후퇴·최고/파밍 분리 등 재설계 | [PP §2-2, §4-01-4; CL] |
| 전투 | `Controllers/Characters/*.cs` (Player/Enemy/Auto/Spawn) | 3D NavMesh + Physics.OverlapSphere; PlayerController(Space 근접, J 스킬), EnemyController(NavMeshAgent 추적), PlayerAutoController(가장 가까운 적 자동 추적), SpawnManager(코루틴 순차 스폰) | **2D에서는 전부 폐기** | [PP §2-2; CL 전투] |
| UI | `Assets/Scripts/UI/*.cs` (HUD, MainBottomNav, TopRightMenu, 패널 5종) | 가로 레이아웃; Canvas HUD(sortOrder 10)·Menu(15)·Joystick(20); 조이스틱 alpha 0.15/0.4; SafeAreaAdjuster | **구조(탭→패널 슬라이드, 하나만 열림)는 계승, 구현은 폐기** | [PP §2-2; CL 모바일 빌드 세팅] |
| 에디터 | `Assets/Editor/MenuSetupWizard.cs`, `SoulStrikeSetupWizard.cs` 등 | `Tools > Setup` 메뉴로 SO·Canvas·프리팹 자동 생성 | **씬/캔버스 자동 생성 위저드 패턴은 계승** → 2D 셋업 위저드 | [PP §2-2, §4-02-11; CL] |
| 데이터 | `Assets/StreamingAssets/JSON/*.json`, `Models/JsonDataManager.cs` | Newtonsoft.Json 로컬 로드 (PlayerData·EnemyData·SpawnData·StageData) + ScriptableObject 병용 | **JSON vs ScriptableObject 단일화 결정 필요** (ADR) | [PP §2-2, §4-00; CL] |
| 빌드 | `Assets/Editor/BuildAutomator.cs`, Jenkinsfile, `.github/` | Jenkins(로컬 Windows batchmode) + GitHub Actions(GameCI unity-builder@v4) 이중 파이프라인 → `Builds/game.aab`; LoginScene+GameScene 빌드 | **인프라 유지**(D3); Portrait 설정·BuildAutomator 변경점만 | [PP §2-1, §3 D3, §4-02-13; CL Build] |
| Firebase | EDM4U 관리; RTDB / Auth(익명) / Analytics | `google-services.json`은 `Assets/`에 있어야 함(미커밋) | **인프라 유지**; 저장 노드 `users` → `users_v2` 전환 결정 | [PP §2-1, §3 D3, §4-08-6] |
| Addressables | 1.22.3, 리모트 = Firebase Hosting | CL에서는 "Planned: patchSize==0 즉시 전환 / >0 다운로드 UI" | D3는 "유지"라 하나 §2-1은 **"실사용 여부 재검토 대상"** → ADR 필요 | [PP §2-1, §3 D3, §4-00; CL Planned] |
| 에셋 문서 | `docs/무료에셋추천.txt` | 3D 기준 에셋 목록 | **2D용으로 전면 교체** | [PP §2-3] |
| 3D 에셋 | RPGHero, Pure Poly, Models, Animations, Tiles, NavMesh 데이터, 3D 머티리얼, SafeArea.unity, 3D 전용 에디터 위저드 | MVP §10에 임포트 목록 (RPG Hero PBR Polyart, Monster Buddy, Mixamo FBX, Low Poly Nature Forest, Slash Effects 등) | 08에서 **삭제/보존/이동 목록** 작성 대상 | [PP §4-08-2; MVP §10.1] |

CL 마일스톤 상태(As-Is): 핵심 시스템·환경 ✅ / 전투+스테이지+가챠 ✅ / UI·인벤·Firebase 저장 ✅ / 오프라인 보상 ✅ / AdMob 2배 📋 Planned / 3D 모델 교체 🔄 In progress(→ 여기서 중단, 2D 전환 결정) / QA·밸런싱·Google Play 출시 📋 Planned [CL Development Milestones; PP §2-1]

---

## 3. 제품 포지셔닝 신호

| 항목 | 내용 | 출처 |
|---|---|---|
| 한 줄 정의(As-Is) | "오프라인 보상과 장비 가챠 기반의 1인 개발 모바일 육성 RPG. 앱을 종료한 시간에 비례해 골드를 획득하고, 그 재화로 정규분포 기반 장비 가챠를 즐기는 쿼터뷰 3D 방치형 액션 RPG" | [CL Project Overview] |
| MVP 한 줄 정의 | "오프라인으로 골드를 얻고(수령), 전투로 스테이지가 진행되며, 골드로 장비를 뽑아 장착/인벤/강화로 성장하고, 재실행해도 모든 상태가 유지되는 게임" | [MVP §1.1] |
| 장르 | 자동 전투 방치형 RPG, 조작 최소, 스킬 자동 발동 | [PP §3 D5] |
| 비교 대상(명시) | **버섯커 키우기 · 세븐나이츠 키우기 · 소울스트라이크** 계열 | [PP §3 D5] |
| 레퍼런스 게임 | Soul Strike (Tikitaka Studio / Comtus, 2024-01-17, Idle Action RPG, Unity, Android/iOS) — "north star for SoloHero's design decisions" | [SS 헤더] |
| 개발 형태 | **1인 개발** | [PP §0; CL] |
| 플랫폼 | Android (AAB). **iOS 미정**. 출시 목표 Google Play | [PP §2-1; CL Milestones] |
| 차별점 | (1) 정규분포(Box-Muller) + 천장 가챠, (2) 오프라인 보상 + 광고 2배 | [PP §3 D7, §7] |
| 수익화(현재) | Google AdMob 보상형 광고 (Rewarded Interstitial) — 오프라인 보상 2배 | [CL Overview; PP §2-1] |
| 수익화(계획) | 보상형 광고 슬롯 목록(용도·보상·일일 제한·실패 처리) + **IAP 로드맵(v1.1)** | [PP §4-01-8] |
| 수익화(레퍼런스) | Soul Strike: 가챠(소환 티켓), 코스메틱(~999종), 배틀패스 | [SS Core Identity] |
| 재화 | 현재 골드 1종(오프라인+스테이지 클리어); 로드맵 "x10 뽑기용 프리미엄 재화 추가"; ADR "재화 1종 vs 2종" 결정 필요 | [SS Economy; PP §4-00] |
| 타깃 유저 | **어느 문서에도 정의 없음.** PP 01_gdd가 "타깃 유저, 세션 설계(3분/15분/하루)" 작성을 요구 | [PP §4-01-1] |
| 세션 설계 | 3분 / 15분 / 하루 단위로 유저가 하는 일 (작성 요구, 내용 미정) | [PP §4-01-1] |
| 디자인 필러 | 3개 정의 요구 (내용 미정) | [PP §4-01-1] |

---

## 4. Soul Strike 참고 요소

### 4-1. 가져오도록 명시된 것

| 요소 | Soul Strike 원본 | SoloHero 적용 지시 | 출처 |
|---|---|---|---|
| 하단 탭 → 패널 슬라이드 구조 | 하단 5탭, 패널이 아래에서 슬라이드 업(~50% 높이), 우상단 × 닫기, DOTween 슬라이드, **하나 열면 다른 것 자동 닫힘** | "구조(탭→패널 슬라이드, 하나만 열림)는 계승, 구현은 폐기" / MVP "한 패널 열면 다른 패널 자동 닫힘" | [SS UI Layout; PP §2-2; MVP §3.7] |
| 자동전투 토글 위치 | 중앙 탭, 강조색으로 두드러지게 | "Auto-battle toggle must be prominent" (교훈 #6); PP는 "자동전투 토글 의미(있다면)" — 존재 여부 결정 필요 | [SS Bottom Tab; SS Lessons #6; PP §4-01-3] |
| 5탭 구성 | Character / Growth / [Auto] / Summon / Equipment | As-Is 패널 5종(HUD, MainBottomNav, TopRightMenu 외) 대응; 03_ui_ux 화면 목록에 캐릭터(스탯 강화)·장비/인벤·가챠·스킬·설정 | [SS Bottom Tab; PP §4-03-3] |
| 스테이지 포맷 | Chapter-Stage (1-1 ~ 6-100), N마리 스폰 → 전멸 → 골드 → 자동 진행 | As-Is 동일 패턴 (공식 기반), "solid progression arc" (교훈 #7) | [SS Stage; SS Lessons #7] |
| 천장 시스템 | "guaranteed Mythic after N pulls" | SoloHero: 100회 Legendary 보장 (계승) | [SS Summon Mechanics; SS Equivalent] |
| 오프라인 보상 + 광고 2배 | 골드 오프라인 누적, AdMob 보상형 → 2배 ("same as SoloHero plan") | 계승 (D7) | [SS Offline Reward; PP §3 D7] |
| 성능 교훈 | "Optimization issues were the main complaint — keep draw calls and particle effects controlled" | PP 02 성능 예산(FPS·드로우콜·메모리·동시 적 수·풀 크기·30분/2시간 방치) | [SS Lessons #8; PP §4-02-10] |
| 자동 시스템 품질 | "Repetitive clicking fatigue — auto systems must be robust and satisfying" | MVP S10 30분 방치 / PP 방치 테스트 프로토콜 | [SS Lessons #9; MVP §6.1; PP §4-07-3] |
| 소환풀·성장 로드맵 원본 | 소환풀 5종(Job/Skill/Companion/Artifact/Pet), 성장(Training/Traits/Awakening/Constellation) | "UI 구조·소환풀·성장 로드맵의 원본" → v1.1+ 로드맵 후보로 | [PP §2-3; PP §4-01-11] |

### 4-2. SS 문서 안의 SoloHero 로드맵 체크박스 (미구현, v1.0 이후 후보)

- 가챠: 스킬 소환풀, 동료 소환풀, 일일 무료 뽑기(5/day) [SS Summon Roadmap]
- 장비: Soul orb 전투 중 패시브 충전 → 장비 드롭 팝업(3택1), Legendary/Mythic 5라인 스탯, 장비 승급(tier promotion) [SS Equipment Roadmap]
- 성장: 크리티컬 확률/피해, 공격 속도, **스킬 시스템(쿨다운 있는 액티브 스킬)** [SS Growth Roadmap]
- 진행: 정복자(Conqueror) 오픈맵(전 챕터 클리어 후), 필드 보스, 시간제 상자/상인 이벤트 [SS Stage Roadmap]
- 던전: 출시 후 2~3종, 보스 레이드(주간), 시련의 탑(무한 층) [SS Dungeon Roadmap]
- 동료: 1~2 슬롯(패시브 스탯만, **v1.5**), 동료 소환풀 [SS Companion Roadmap]
- 경제: x10 뽑기용 프리미엄 재화 [SS Economy]

### 4-3. 건너뛰도록 명시된 것

| 요소 | 판단 | 출처 |
|---|---|---|
| 정복자(오픈맵), 길드, 다수 던전, 쉘터/기지 | MVP 제외 (v1.0 이후) | [MVP §2.2] |
| 동료 시스템, 스킬 소환풀(별도 가챠), 분리된 다중 소환풀 | MVP 제외 | [MVP §2.2] |
| 장비 고급화(5라인/소켓/승급/재감정) | MVP 제외 | [MVP §2.2] |
| 이벤트/패스/상점 고도화 | MVP 제외 | [MVP §2.2] |
| Shelter(기지 건설) | "Not planned for initial release. Consider as mid-term content (post-launch)" | [SS Shelter Note] |
| Guild | "Post-launch feature. Skip for v1.0" | [SS Guild Note] |
| Companion | v1.5 | [SS Companion Roadmap] |
| 광고 2배 보상 | MVP 문서는 "MVP+로 분리 권장" — 단, PP D7은 차별점으로 계승 (→ §9 모순) | [MVP §2.2, §2.3; PP §3 D7] |

### 4-4. SS 교훈 중 To-Be 확정 결정과 충돌하는 것 (→ §9)

- 교훈 #1 "Horizontal layout is the differentiator — lean into it fully" ↔ **D2 Portrait 1080×1920** [SS Lessons #1; PP §3 D2]
- 교훈 #2 "Active skills matter — pure idle is boring; give players things to press" ↔ **D5 조작 최소, 스킬 자동 발동** [SS Lessons #2; PP §3 D5]
- SS HUD 배치(좌상 플레이어 카드 / 중상 스테이지 / 우상 세로 아이콘 / 3D 필드)는 가로 전용 → 세로 그리드로 재해석 필요 [SS HUD; PP §4-03-2]
- SS "Screen: Horizontal landscape (same as SoloHero 1920x1080)" — 문서 자체가 As-Is 가로 기준으로 작성됨 [SS Core Identity]

---

## 5. 수치·공식·제약 (전수)

### 5-1. 가챠

| 항목 | 값 | 출처 |
|---|---|---|
| 분포 생성 | Box-Muller 변환 정규분포 샘플 | [CL 가챠; PP §2-2] |
| 등급 가중치(weight) | Common=0.20, Rare=0.45, Epic=0.68, Legendary=0.90 | [CL 가챠] |
| 천장(Pity) | **100회** 누적 시 Legendary 보장, 달성 후 카운터 리셋 | [CL; PP §2-2; SS Equivalent] |
| μ 이동 | pity 진행도에 따라 μ 0.3 → 0.8 (초반 → 천장 직전), `Lerp(0.3, 0.8, pullCount/100)` | [CL; PP §2-2; code GachaSystem.cs:114-115] |
| σ | **0.2** (문서 미기재, 코드만) | [code GachaSystem.cs:116] |
| 1회 비용 | **100 골드** `_costPerPull` (문서 미기재, 코드만) | [code GachaSystem.cs:16] |
| 등급 선택 방식 | 코드는 `score = 1 − |eq.weight − sample|` 최대인 장비 선택(최근접), 임계값 방식 아님 (문서 미기재) | [code GachaSystem.cs:121-131] |
| 풀 구성 | 4슬롯(Sword/Helm/Armor/Boots) × 4등급 = SO 16종 | [CL; PP §2-2] |
| 시뮬레이션 요구 | Monte Carlo: pity 0/25/50/75/99 시점 등급별 확률표, **10만 회 이상** | [PP §4-05-4] |
| 확률 공시표 | 스토어 정책용 최종 포맷 필수 | [PP §4-05-6] |
| SS 비교 | 일일 무료 뽑기 5회 초기 → 카테고리별 최대 30/day; 픽업 소환은 카테고리 Lv6 해금 | [SS Summon Mechanics] |

### 5-2. 오프라인 보상

| 항목 | 값 | 출처 |
|---|---|---|
| 상한 | **21,600초 = 6시간** | [CL; PP §2-2; MVP §2.1, §3.3] |
| 기본 속도 | 초당 1골드 (`BaseGoldPerSecond = 1f`) | [CL 오프라인; code] |
| 공식 | `reward = seconds × rate` (MVP에서는 rate 고정 가능) | [MVP §3.3] |
| 기준 시각 | `lastQuitTimeUtc` (Unix seconds, UTC), `OnApplicationPause`/`OnApplicationQuit`에 저장, `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` | [CL; MVP §3.3, §4.1] |
| 광고 2배 | AdMob 보상형 시청 시 2배 | [CL; PP D7; SS] |
| 방어 | 시간 조작/음수/비정상 값에서 크래시 없이 안전 처리(최소 방어); PP는 "기기 시간 조작 방어" 명시 요구 | [MVP §3.3; PP §4-02-7] |
| QA | S3 종료 10분 후 수령; S4 6시간 상한 확인 | [MVP §6.1] |

### 5-3. 스테이지

| 항목 | 값 | 출처 |
|---|---|---|
| 필요 킬수 | `kills = 5 + (ch−1)×10 + (stage−1)×2` | [CL; PP §2-2] |
| 골드 보상 | `gold = 50 + (ch−1)×100 + (stage−1)×20` | [CL; PP §2-2] |
| 클리어 후 대기 | 3초 후 다음 스테이지 자동 진행 | [CL 진행 시스템] |
| 챕터당 스테이지 수 | **미정의** (SS 레퍼런스는 1-1 ~ 6-100) | [SS Stage] |
| 목표 곡선 예시 | "1일차 1-10 보스 도달, 3일차 챕터 3, 7일차 챕터 5, 첫 정체 구간 위치, 하루 골드 순환 시간" (예시 문구) | [PP §4-05-1] |
| 데이터 최소량 | 적 최소 8종, 보스 최소 3종, 장비 최소 20종, 스킬 최소 6종, 챕터 테마 최소 5종 | [PP §4-05-5] |

### 5-4. 강화(Upgrade)

| 항목 | 값 | 출처 |
|---|---|---|
| 스탯 | HP / ATK / DEF / SPD 4종 | [CL; PP; MVP §2.1; SS] |
| 비용 공식 | `nextCost = baseCost × (level + 1)` | [CL; PP; SS Growth Equivalent] |
| 최대 레벨 | **50** | [CL; PP; SS] |
| baseCost (코드) | HP 100 / ATK 150 / DEF 150 / SPD 200 골드 | [code UpgradeService.cs:13-16] |
| 레벨당 보너스 (코드) | HP +50 / ATK +5 / DEF +5 / SPD +0.2 | [code UpgradeService.cs:8-11] |
| 재설계 지시 | "방치형 곡선으로 재설계 대상" | [PP §2-2] |
| QA | S9 강화 10회 연타 → 골드 음수/중복 적용 없음 | [MVP §3.6, §6.1] |

### 5-5. 전투 (As-Is 3D — 전부 폐기, 참고 수치)

| 항목 | 값 | 출처 |
|---|---|---|
| 근접 공격 | Space, OverlapSphere r=1.8 | [CL 전투] |
| 스킬 | J, SP 50 소모, r=3.5, 데미지 ×3 | [CL 전투] |
| 카메라 | 쿼터뷰 Rot X:60, Y:7, Z:−5 | [CL Overview] |
| Animator(계획) | MoveSpeed(float), Attack/Skill1/Hit/Die(trigger); Idle/Walk/Run/Attack/Skill/Hit/Die | [MVP §10.2] |

### 5-6. 화면·UI

| 항목 | 값 | 출처 |
|---|---|---|
| As-Is 해상도 | Landscape 1920×1080, CanvasScaler Scale With Screen Size | [CL] |
| **To-Be 해상도** | **Portrait 1080×1920** | [PP §3 D2] |
| Canvas sortOrder | HUD 10 / Menu 15 / Joystick 20 | [CL] |
| 조이스틱 alpha | 0.15 / 0.4 (2D에서는 조이스틱 자체가 폐기 대상 — D5 조작 최소) | [CL] |
| SS 패널 높이 | 화면의 ~50% 슬라이드 업 | [SS Panels] |
| 저사양 옵션 | 이펙트 축소, **30fps 모드** | [PP §4-03-7] |
| 방치 안정성 | **30분 · 2시간** 방치 테스트, FPS·메모리 로그 | [PP §4-02-10, §4-07-3; MVP §1.3, §3.4] |

### 5-7. 저장·데이터

| 항목 | 값 | 출처 |
|---|---|---|
| 디바운스 | 250ms 큐 | [PP §2-2] |
| PlayerData v1 필드 | gold(long), chapter, stageNumber, stageKillsCurrent, equippedWeapon/Helmet/Armor/Boots(string id), ownedEquipmentCsv, upgradeHp/Atk/Def/SpdLevel, gachaPullCount, lastQuitTimeUtc, dataVersion=1 | [PP §2-2; MVP §4.1; code PlayerData.cs] |
| 저장 트리거(항상) | OnApplicationPause(true) / OnApplicationQuit | [MVP §4.4] |
| 저장 트리거(행동) | 스테이지 클리어 / 가챠 결과 확정 / 장착 변경 / 강화 성공 / 오프라인 보상 수령 직후 | [MVP §4.4] |
| 충돌 정책 | 원격 로드 성공 시 원격 우선; 원격 실패 시 로컬 백업으로 시작; 원격 복구 시 로컬→원격 자동 덮어쓰기는 하지 않거나 매우 보수적으로 | [MVP §4.3] |
| Firebase 노드 | `users` → `users_v2` 전환 (ADR) | [PP §4-00, §4-08-6] |
| 인벤 상한 | MVP에서는 무제한 가능; 상한 두면 초과 처리 규칙 필요 | [MVP §4.2] |
| 자동 장착 규칙 예시 | 해당 슬롯 장착 장비보다 등급/스탯이 좋으면 자동 장착, 아니면 인벤; UI 피드백("자동 장착됨/인벤으로 들어감") 일치 | [MVP §4.2] |

### 5-8. 일정·QA 정량 기준

| 항목 | 값 | 출처 |
|---|---|---|
| MVP 마일스톤 기간 | A 기준선 0.5~1일 / B 데이터 안정화 2~3일 / C 성장 루프 2~4일 / D QA·릴리즈 2~3일 | [MVP §5] |
| A 완료 | 신규/기존 두 케이스 "게임 진입" 10회 중 10회 성공 | [MVP §5.1] |
| B 완료 | 강제 종료 후 재실행 20회 시나리오에서 핵심 데이터 유실 0 | [MVP §5.2] |
| C 완료 | 30분 플레이로 성장 체감 + 루프 반복 가능 | [MVP §5.3] |
| D 완료 | QA 필수 10개 시나리오 All Pass + AAB 실행 성공 | [MVP §5.4] |
| 출시 게이트 | Critical 버그 0 (데이터 유실/초기화처럼 보임, 진행 불가, 무한 로딩/저장); Major 최소화 (골드 음수, 장비 중복/소실, 강화 수치 불일치) | [MVP §6.2; PP §4-07-5] |
| S7 | 20회 이상 연속 뽑기 | [MVP §6.1] |
| WBS 형식 | 태스크 ID `P1-03` 형식 / 예상 시간(h) / 우선순위 M·S·C / 검증 방법; 주당 투입 시간 명시(가정), 버퍼 비율 | [PP §4-06-2,3] |
| 문서 제약 | 파일 600줄 상한; 질문 최대 3개 | [PP §1-9, §1-3] |

### 5-9. 버전·환경 상수

| 항목 | 값 | 출처 |
|---|---|---|
| Unity | 2022.3.62f3 LTS | [CL; PP §2-1, D6] |
| URP | 14.0.12 (As-Is 3D Forward → To-Be 2D Renderer) | [CL; PP D6] |
| Addressables | 1.22.3 | [CL; PP §2-1] |
| Google Mobile Ads | v25.0.0 | [CL; PP §2-1] |
| Input System | 1.14.2, Both 모드 | [CL; PP §2-1] |
| DOTween | HOTween v2 v1.2.825 | [MVP §10.1] |
| JDK | **11 고정** (21은 Firebase 호환 문제), Gradle Java 11 | [CL; PP §2-1] |
| AdMob App ID | `ca-app-pub-1435934257467286~9895276357` (`Assets/Plugins/Android/AndroidManifest.xml`) | [CL] |
| 2D 패키지 | `com.unity.feature.2d` 2.0.1 — Pixel Perfect·Aseprite Importer 포함 여부 `[확인 필요]`; `com.unity.ai.navigation` 제거 여부 | [PP §4-08-3] |
| 폰트(As-Is) | NotoSansKR 9 굵기 + TMP SDF (`Assets/Fonts/`) — To-Be는 "픽셀 폰트(한글 지원) 선정" | [MVP §10.1; PP §4-03-6] |

---

## 6. MVP 범위 힌트

### 6-1. MVP 정의·핵심 루프·DoD [MVP §1]

- 핵심 루프: 앱 실행 → 로그인/로드 → 오프라인 보상 팝업(조건부) → 전투/스테이지 진행 → 골드 획득 → (가챠/인벤/강화) 성장 → 자동 저장 → 종료/재실행 → 동일 상태 복원 [MVP §1.2]
- 기능 DoD: 신규 유저 첫 실행~진입 막힘 없음 / 기존 유저 동일 상태 복원 / 오프라인 보상 계산·상한·수령·저장 / 장비 뽑기→장착 또는 인벤→스탯 반영→저장 / 강화 비용 차감→스탯→저장 / 스테이지 킬 목표→클리어→보상→자동 다음 진행 [MVP §1.3]
- 품질 DoD: 강제 종료 후 마지막 저장 지점 복구; 30분 연속 플레이/방치에서 진행 멈춤·메모리 폭증·치명 오류 없음 [MVP §1.3]
- 릴리즈 DoD: Android AAB 1회 이상 빌드/설치/실행 성공(개발 기기) [MVP §1.3]

### 6-2. In / Out / MVP+ (3D 시대 기준, "형식 계승" 지시) [MVP §2; PP §4-01-9]

| 구분 | 항목 |
|---|---|
| **In** | 익명 로그인, 첫 실행/재실행, 로딩 UI; RTDB+로컬 백업, 자동 저장 트리거, 중복 저장/경합 방지; 오프라인 보상(UTC, 6h 상한, 팝업 일반 수령); 자동 전투(기본 공격/간단 스킬), 킬 목표·클리어·골드·자동 진행; 장비 뽑기 1회, 자동 장착/인벤 규칙, 인벤 UI, 장착·합산 스탯 요약; HP/ATK/DEF/SPD 강화, 비용 공식·최대 레벨; 하단 탭/패널(열면 닫힘), HUD(골드·킬/목표·HP/SP) |
| **Out (v1.0 이후)** | 정복자(오픈맵), 길드, 다수 던전, 쉘터/기지; 동료, 스킬 소환풀, 다중 소환풀; 장비 고급화(5라인/소켓/승급/재감정); 이벤트/패스/상점 고도화; 광고 2배 보상(MVP+로 분리 권장) |
| **MVP+ (시간 남으면)** | 오프라인 보상 보상형 광고 2배; 장비 뽑기 x10; 간단한 설정 메뉴(사운드/진동/계정/데이터 초기화는 주의) |

### 6-3. 2D 리빌드에서 GDD가 새로 다뤄야 하는 범위 항목 (PP가 명시 요구) [PP §4-01]

- 전투: 히어로 화면 내 고정 X 비율, 전진/정지 조건, 적 스폰 규칙(위치·간격·동시 최대), 타겟팅, 사거리, 판정, 데미지 공식, 크리티컬, 스킬 자동 발동, 피격/사망/부활, 보스 규칙(등장 연출·시간 제한 여부), **보스 실패 시 후퇴·파밍 모드**, 자동전투 토글 의미(있다면) [PP §4-01-3]
- 진행: 챕터-스테이지 구조(N스테이지/챕터, 보스 위치), 챕터 테마 순환, 무한 확장, **최고 도달 vs 현재 파밍 스테이지 분리**, 스테이지 선택 UI 유무 [PP §4-01-4]
- 성장: **히어로 레벨(EXP)**, 스탯 강화, 장비(중복 처리 규칙), **스킬(슬롯 수·해금·레벨업)**, 배율 합산 순서(가산/승산) 수식 [PP §4-01-5]
- 가챠: 등급 확장, 비용 재화, 1회/10회, 연출 단계, 확률 공시표, 자동 장착 규칙 [PP §4-01-6]
- 경제: 재화별 소스/싱크, 골드 인플레이션 통제, **일일 콘텐츠(일일 퀘스트/출석) MVP 포함 여부** [PP §4-01-7]
- 광고: 보상형 광고 슬롯 목록(용도·보상·일일 제한·실패 처리), IAP 로드맵(v1.1) [PP §4-01-8]
- 유저 플로우: 첫 실행 → **(최소 튜토리얼)** → 루프 → 종료 → 재실행 [PP §4-01-10]
- v1.1+ 로드맵: 던전, 스킬 가챠, 동료, 컬렉션 등 후보와 우선순위 근거 [PP §4-01-11]

### 6-4. 페이즈·출시 [PP §4-06; CL; PP §4-09]

- 페이즈 예시: P0 전환 → P1 전투 코어 → P2 성장 루프 → P3 UI → P4 메타(오프라인·광고·일일) → P5 QA·출시, 각 DoD와 데모 조건 [PP §4-06-1]
- 출시 채널: Google Play (CL 마일스톤 "QA · 밸런싱 · Google Play 출시") [CL]
- 스토어 자산: 아이콘·스크린샷·개인정보처리방침·광고 정책 항목 [PP §4-09-1]
- 테스트 레벨: EditMode 유닛(가챠 분포, 강화 비용, 오프라인 계산, 마이그레이션, 큰 수 포맷) / 스모크 / 시나리오 / 방치 / 기기(저·중·고사양, 노치·펀치홀) [PP §4-02-12, §4-07]
- QA 시나리오: S1~S10 계승 + 2D·세로·보스·후퇴·마이그레이션·광고 실패 시나리오 추가 [PP §4-07-2]
- 리스크(계승): 경합 저장 → 단일 파이프라인(큐/락); 원격/로컬 불일치 → 원격 우선; 씬 변경 잦음 → MVP 중 씬 구조 변경 금지; 파티클/풀링/스폰 누수 → 30분 방치 테스트를 개발 중간부터 반복 [MVP §7]

---

## 7. 정성적 톤·느낌

| 진술 | 출처 |
|---|---|
| 방치형이되 "Cool idle game" — 순수 방치가 아니라 능동 스킬 입력으로 손맛; "pure idle is boring; give players things to press" (레퍼런스 정체성 — D5 "조작 최소"와 조율 필요) | [SS Core Identity; SS Lessons #2] |
| "Repetitive clicking fatigue — auto systems must be robust and satisfying" (자동 시스템의 만족감) | [SS Lessons #9] |
| "Companion diversity beats pure damage optimization — reward experimentation" (실험 보상; v1.5 이후 참고) | [SS Lessons #10] |
| 히어로가 **오른쪽으로 자동 전진**, 적 웨이브가 오른쪽에서 등장, 배경 **패럴랙스** — 횡스크롤 진행감 | [PP §3 D1] |
| 픽셀아트, 무료 에셋 기반; 팔레트 정책·배경 레이어 수·PPU·캐릭터 기준 px 정의 요구 | [PP §3 D4, §4-04-1] |
| 세션 설계: **3분 / 15분 / 하루** 단위로 유저가 하는 일 | [PP §4-01-1] |
| 핵심 루프 각 단계의 **체감 목표** 명시 요구 | [PP §4-01-2] |
| "뽑기→장착→**강해짐 체감**"이 전투/스테이지에서 확인되어야 함 | [MVP §3.5] |
| "30분 플레이로 **성장 체감** + 루프 반복" | [MVP §5.3] |
| MVP = "가능한 최소 기능" + "**사용자가 체감하는 완성도**" | [MVP §0] |
| "사용자가 **길을 잃지 않고** 전투 ↔ 성장(가챠/인벤/강화)을 반복 가능" | [MVP §3.7] |
| "어떤 경우에도 데이터가 **초기화처럼 보이지 않게**(최소 방어 UI 포함)" — 신뢰감 | [MVP §4.3] |
| 보스: 등장 연출, 시간 제한 여부, **실패 시 후퇴·파밍 모드** (좌절 대신 파밍 루프로 전환) | [PP §4-01-3] |
| 첫 **정체 구간 위치**, 하루 골드 순환 시간 — 페이싱 설계 대상 | [PP §4-05-1] |
| 연출 표준(DOTween): 골드 카운트업, 강화 성공 펀치, 가챠 카드 뒤집기, 데미지 텍스트 궤적, 히트 플래시, 카메라 셰이크 | [PP §4-03-5] |
| As-Is 연출: 피격 플래시, 콤보 텍스트, 사망/스테이지 클리어 패널; 조이스틱 반투명 | [CL 전투, 모바일 세팅] |
| 가챠 연출 단계 (1회/10회) | [PP §4-01-6] |
| 오프라인 보상 팝업: DOTween 팝업 애니메이션, 일반 수령 / 2배 수령 버튼 | [CL 오프라인] |
| 저사양/접근성: 이펙트 축소 옵션, 30fps 모드 | [PP §4-03-7] |
| 자동전투 토글은 눈에 띄게(중앙, 강조색) | [SS Lessons #6] |
| Soul Strike 사용자 불만 1위 = 최적화 → 가볍게 | [SS Lessons #8] |

---

## 8. 기술적 How (Addendum 후보 — PRD 본문이 아닌 아키텍처 문서로)

| 영역 | 내용 | 출처 |
|---|---|---|
| 엔진/렌더 | Unity 2022.3.62f3 LTS, URP 14.0.12 → **2D Renderer** 전환; Pixel Perfect Camera 설정값(PPU, 기준 해상도); Sorting Layer 표 | [PP D6, §4-02-5, §4-04-1] |
| API 제약 | Unity 2022.3에 없는 API 금지 (예: `AssetDatabase.AssetPathExists` 2023.1+ → `File.Exists`) | [PP §2-1] |
| 비동기/트위닝 | UniTask(코루틴 대신), DOTween(모든 애니메이션; 셋업 위저드 필요), Newtonsoft.Json, TextMeshPro | [CL; PP D6] |
| 입력 | Input System 1.14.2 Both 모드 (As-Is 가상 조이스틱/WASD) — 2D에서 재검토 | [CL; PP §2-1] |
| 아키텍처 | 레이어 다이어그램, 의존 방향, **Assembly Definition 분리안**(Core/Data/Gameplay/Systems/UI/Services/Editor/Tests), 폴더 `Assets/_Project/...` | [PP §4-02-1,2] |
| 패턴 | UI MVP(Presenter 순수 C# / View MonoBehaviour), ScriptableObject Model; 서비스 로케이터 vs DI(ADR); 이벤트 버스; MonoBehaviour 최소화; 순수 C# 시스템으로 테스트 가능 | [CL Architecture; PP §4-02-4] |
| 전투 런타임 | `BattleDirector` 상태머신(Spawning/Fighting/BossIntro/Clear/Retreat/Dead); Unit/Hero/Enemy 클래스; 스폰·풀링; Physics2D vs 거리 판정(ADR); 투사체; 데미지 숫자 풀; 카메라+패럴랙스; Animator 파라미터 표; 스프라이트시트 규약; 히트 프레임 애니메이션 이벤트 | [PP §4-02-5] |
| As-Is 전투(폐기) | StateMachineBehaviour FSM, IDamageable, NavMeshAgent + Physics.OverlapSphere, 오브젝트 풀링 | [CL Architecture] |
| 데이터 | SO 정의 목록(필드 표), 밸런스 상수 SO, JSON vs SO 단일화(ADR); As-Is StreamingAssets/JSON + JsonDataManager | [PP §4-02-6; CL] |
| 저장 | DTO → JSON → Firebase RTDB; PlayerPrefs 로컬 백업; 250ms 디바운스 큐; `PlayerSave` v2 필드 표; v1→v2 마이그레이션 매핑; 노드 `users`→`users_v2`; 충돌 정책; 기기 시간 조작 방어 | [CL; PP §2-2, §4-02-7, §4-08-6] |
| 큰 수 | double vs long vs BigInteger(ADR); 포맷터 K/M/B/T/aa…; 난수 시드·재현성 | [PP §4-00, §4-02-8] |
| 부트스트랩 | Boot/Title/Main 씬; Firebase init → 익명 auth → 로드 → 마이그레이션 → 오프라인 계산 → Main; 실패 분기 | [PP §4-02-3] |
| 외부 SDK | Firebase Analytics 이벤트 목록; AdMob 로드/표시/실패/재시도 시퀀스(현재 테스트 유닛 ID); Addressables 사용 범위(ADR; 리모트 Firebase Hosting, patchSize 분기) | [PP §4-02-9; CL Planned] |
| 성능 예산 | 목표 FPS, 드로우콜 상한, 메모리 상한, 동시 적 수, 풀 크기, 30분·2시간 방치 누수 지점 | [PP §4-02-10] |
| 에디터 툴 | 2D 셋업 위저드(씬·캔버스·정렬 레이어), SO 생성기, 밸런스 시트 임포터 | [PP §4-02-11] |
| 테스트 | EditMode(가챠 분포, 강화 비용, 오프라인 계산, 마이그레이션, 큰 수 포맷), PlayMode 스모크 | [PP §4-02-12] |
| 빌드/CI | `BuildAutomator.Build` (Jenkins batchmode + GHA GameCI unity-builder@v4, ubuntu-latest, 시크릿 UNITY_LICENSE/EMAIL/PASSWORD, 아티팩트 `android-aab` 7일); Portrait 설정 변경; `Builds/game.aab` | [CL Build; PP §4-02-13] |
| Android | 커스텀 Gradle 템플릿(EDM4U 주입), AndroidManifest AdMob App ID, `FirebaseApp.androidlib`/`GoogleMobileAdsPlugin.androidlib`; **Java 11 고정**(17/21 금지); `google-services.json` 미커밋 | [CL Android Build Config] |
| 아트 임포트 | Texture Filter Point / Compression None / PPU; Sprite Atlas; 클립명 표준(Idle/Run/Attack/Hit/Die + 스킬); 플레이스홀더 색상 박스 규격; 라이선스 관리표 | [PP §4-04-3,4,5,6] |
| 패키지 변경 | `com.unity.ai.navigation` 제거 여부; `com.unity.feature.2d` 2.0.1 구성 `[확인 필요]`; Physics2D·Quality·Input 설정 | [PP §4-08-3,4] |
| 밸런스 도구 | `docs/2d/tools/balance_sim.py` (Python 3) 실제 실행·결과 첨부·조정 이력 | [PP §4-05-4] |

---

## 9. 공백·모순·미결 질문

### 9-1. 문서 간 모순

| # | 모순 | 관련 출처 | 상태 |
|---|---|---|---|
| C1 | **가로 vs 세로**: CL·MVP·SS 전부 Landscape 1920×1080 기준(SS 교훈 #1 "가로가 차별점, 완전히 기대라") ↔ D2 Portrait 1080×1920 | [CL; SS Lessons #1; PP D2] | D2로 확정. SS의 HUD 배치·교훈 #1은 재해석 대상 |
| C2 | **능동 스킬 vs 자동 발동**: SS 교훈 #2 "누를 것을 줘라" ↔ D5 "조작 최소, 스킬 자동 발동" | [SS Lessons #2; PP D5] | D5 확정. 수동 입력이 0인지, 스킬 탭/자동전투 토글 정도는 허용인지 미결 |
| C3 | **광고 2배 범위**: MVP 문서 Out/MVP+ ↔ CL "Planned" ↔ PP §2-2 "광고 2배 연결됨"(구현됨) ↔ D7 차별점 계승 | [MVP §2.2-2.3; CL; PP §2-2, D7] | D7 우선. 2D MVP In으로 볼 근거 있으나 명시 결정 필요 |
| C4 | **Addressables**: D3 "인프라 유지" ↔ PP §2-1 "실사용 여부 재검토 대상" ↔ CL "Planned"(미구현) | [PP D3, §2-1, §4-00; CL] | ADR로 결정 예정 |
| C5 | **장비 슬롯 명칭**: CL/PP "Sword/Helm/Armor/Boots" ↔ MVP §4.1 "equippedWeaponId/HelmId" ↔ SS "Weapon/Helmet" ↔ code `equippedWeapon/equippedHelmet` | [CL; PP §2-2; MVP §4.1; SS; code] | 용어집에서 단일화 필요 |
| C6 | **가챠 등급 결정 방식**: CL은 등급별 weight(0.20/0.45/0.68/0.90)만 기재, PP 05는 "등급 임계값" 요구 ↔ 코드는 샘플과 weight 최근접 장비 선택 | [CL; PP §4-05-3; code] | 임계값 vs 최근접 방식 확정 후 확률 공시표 산출 |
| C7 | **JSON vs ScriptableObject**: CL은 SO Model + StreamingAssets JSON 병용 ↔ PP "단일화 결정 필요" | [CL; PP §2-2] | ADR |
| C8 | MVP §10(3D 에셋 도입·비주얼 교체 절차)은 2D 전환으로 **전면 무효** | [MVP §10; PP §2-1] | 08 삭제 목록으로 흡수 |

### 9-2. 정의되지 않은 것 (PRD가 채워야 할 공백)

| # | 공백 | 요구 출처 |
|---|---|---|
| G1 | **타깃 유저 페르소나** — 어느 문서에도 없음 | [PP §4-01-1] |
| G2 | **세션 설계**(3분/15분/하루)와 **디자인 필러 3개** | [PP §4-01-1] |
| G3 | **챕터 구조**: 챕터당 스테이지 수, 보스 위치, 챕터 테마 순환, 무한 확장 방식 | [PP §4-01-4] |
| G4 | **보스 규칙**: 시간 제한 여부, 실패 시 후퇴·파밍 모드, 최고 도달 vs 현재 파밍 분리, 스테이지 선택 UI 유무 | [PP §4-01-3,4] |
| G5 | **히어로 레벨/EXP**, **스킬 시스템**(슬롯 수·해금·레벨업·자동 발동 규칙), **크리티컬** — As-Is에 없음 | [PP §4-01-3,5; SS Growth Roadmap] |
| G6 | **가챠 등급 확장** 여부·등급 수(현재 4; SS는 7) 및 신규 등급 가중치·비용 재화·10회 뽑기 | [PP §4-01-6] |
| G7 | **재화 1종 vs 2종** (프리미엄 재화 도입 여부) | [PP §4-00; SS Economy] |
| G8 | **강화 곡선 재설계** 공식 (방치형), 큰 수 타입 | [PP §2-2, §4-00] |
| G9 | **오프라인 골드/초 계산 기준** (고정 1 vs 스테이지 연동) | [PP §4-01-7, §4-05-3; MVP §3.3] |
| G10 | **광고 슬롯 목록**·일일 제한·실패 처리; IAP v1.1 내용 | [PP §4-01-8] |
| G11 | **일일 콘텐츠(일일 퀘스트/출석) MVP 포함 여부** | [PP §4-01-7] |
| G12 | **자동전투 토글 존재 여부** 및 의미 | [PP §4-01-3; SS Lessons #6] |
| G13 | **튜토리얼** 범위("최소 튜토리얼") | [PP §4-01-10] |
| G14 | **장비 중복 처리 규칙**, 인벤 상한 | [PP §4-01-5; MVP §4.2] |
| G15 | **현지화** 범위(한국어 단독? 문자열 리소스 최소 구조) | [PP §4-03-6] |
| G16 | **주당 투입 시간**, 출시 목표 시점, 버퍼 비율 | [PP §4-06-3] |
| G17 | **iOS** 여부 | [PP §2-1] |
| G18 | 가챠 σ=0.2, 1회 100골드 — 코드에만 존재, 문서 미기재 → 05 상수표에 편입 필요 | [code] |
| G19 | 무료 에셋 라이선스, `com.unity.feature.2d` 구성 — `[확인 필요]` | [PP §1-7, §4-08-3] |
| G20 | 기존 개발 데이터(`users` 노드) 처리 방침 | [PP §4-08-6] |

### 9-3. PM이 사용자에게 확인해야 할 상위 질문 (답에 따라 문서가 크게 갈리는 것)

1. **재화 구조**: 골드 1종으로 MVP를 가고 10회 뽑기도 골드로 하는가, 아니면 프리미엄 재화(광고/IAP 소스)를 MVP에 넣는가? (G6·G7·G10 연동)
2. **조작의 최소선**: D5 "조작 최소"에서 허용되는 수동 입력은 무엇인가 — 자동전투 토글 / 스킬 수동 탭 / 완전 무조작? (C2·G12)
3. **광고 2배와 일일 콘텐츠**: 2D MVP In에 광고 2배(D7)를 포함하는가; 일일 퀘스트/출석은 MVP인가 v1.1인가? (C3·G11)
4. **챕터 규모와 보스**: 챕터당 스테이지 수(예: 10 vs 100), 보스 시간 제한·실패 후퇴 모드 채택 여부 (G3·G4)
5. **성장 축 확장**: 히어로 레벨(EXP)·스킬·크리티컬을 MVP에 넣는가, 강화 4종+장비만으로 MVP를 닫는가? (G5·G8)

---

## 10. PLANNING_PROMPT 산출물 체계 [PP §4, §5, §6]

### 10-1. 계획된 산출물 10건 (+ 도구 1) — 모두 `docs/2d/` 하위

| 파일 | 역할 | 필수 내용 요약 |
|---|---|---|
| `README.md` | 인덱스 | 한 줄 정의, 확정 결정 표(§3 그대로), 문서 목록+읽는 순서, 용어집(한↔영: 히어로/Hero, 강화/Upgrade, 스테이지/Stage …), 문서 갱신 규칙 |
| `00_decisions_adr.md` | ADR | ADR-001~: 배경/선택지(≥2, 장단점)/결정/결과·영향. 최소: 2D 전환 이유, 세로 화면, 같은 레포 재설계, 픽셀아트, JSON vs SO, 서비스 로케이터 vs DI, Physics2D vs 거리 판정, 큰 수(double/long/BigInteger), Addressables 유지, 재화 1종 vs 2종, 저장 노드 `users` vs `users_v2` |
| `01_gdd.md` | 게임 기획서 | 11개 섹션 (→ 10-2) |
| `02_tdd.md` | 기술 설계서 | 13개 섹션 (아키텍처, 폴더, 씬/부트스트랩, 패턴, 전투 런타임, 데이터, 저장, 큰 수/난수, 외부 SDK, 성능 예산, 에디터 툴, 테스트, 빌드/CI) |
| `03_ui_ux.md` | UI/UX 설계서 | 7개 섹션 (→ 10-3) |
| `04_art_assets.md` | 아트/에셋 | 아트 디렉션(PPU·px·팔레트·레이어), 필요 에셋 목록(히어로/적 N/보스 N/배경 N/VFX/UI/아이콘/폰트/SFX·BGM + 후보 무료 에셋·URL·라이선스 `[확인 필요]`), 임포트 규약, 애니메이션 규약, 플레이스홀더, 라이선스 관리표 |
| `05_balance_data.md` + `tools/balance_sim.py` | 밸런스·데이터 (**상수 단일 출처**) | 목표 곡선 → 상수표 → 공식(플레이어 스탯, 적 HP/ATK/골드/EXP, 보스 배율, 강화 비용, 장비 등급 배율, 스킬, 오프라인 골드/초, 가챠 μ·σ·임계값·천장) → 시뮬레이션(Python 실행, 가챠 Monte Carlo ≥10만) → 데이터 시트(적≥8, 보스≥3, 장비≥20, 스킬≥6, 챕터 테마≥5) → 확률 공시표 |
| `06_milestones_wbs.md` | 마일스톤·WBS | 4개 섹션 (→ 10-4) |
| `07_qa_plan.md` | QA | 테스트 레벨, 시나리오 표(S1~S10 계승 + 2D·세로·보스·후퇴·마이그레이션·광고 실패), 방치 프로토콜(30분/2시간), 기기 매트릭스, 심각도·출시 게이트 |
| `08_migration_plan.md` | 전환 절차 | 7개 섹션 (→ 10-5) |
| `09_deliverables_checklist.md` | 체크리스트·추적표 | 산출물 전체 목록(문서/씬/프리팹/SO/클래스/에디터 툴/테스트/빌드/스토어 자산: 아이콘·스크린샷·개인정보처리방침·광고 정책) `☐`; **Traceability Matrix**: GDD 시스템 ↔ TDD 클래스 ↔ PlayerSave 필드 ↔ UI 화면 ↔ WBS 태스크 ↔ QA 시나리오 (빈 칸 = 누락) |

공통 헤더: `# 제목`, `> 버전 / 작성일 / 상태(초안·검토·확정)`, `## 가정` [PP §4 서두]

### 10-2. `01_gdd.md` 필수 섹션 [PP §4-01]

1. 비전: 한 줄 정의, 타깃 유저, 세션 설계(3분/15분/하루), 디자인 필러 3개, 레퍼런스 게임에서 가져올 것/안 가져올 것 표
2. 핵심 루프 다이어그램(mermaid) + 각 단계 체감 목표
3. 전투(사이드뷰): 히어로 고정 X 비율, 전진/정지 조건, 스폰 규칙(위치·간격·동시 최대), 타겟팅, 사거리, 판정, 데미지 공식(→05), 크리티컬, 스킬 자동 발동, 피격/사망/부활, 보스 규칙(연출·시간 제한), 보스 실패 시 후퇴·파밍 모드, 자동전투 토글 의미
4. 스테이지/진행: 챕터-스테이지 구조(N/챕터, 보스 위치), 킬 요구, 테마 순환, 무한 확장, 최고 도달 vs 현재 파밍, 스테이지 선택 UI 유무
5. 성장: 히어로 레벨(EXP), 스탯 강화, 장비(슬롯·등급·스탯·중복 규칙), 스킬(슬롯·해금·레벨업), 배율 합산 순서 수식
6. 가챠: 정규분포+천장 계승·등급 확장, 비용 재화, 1회/10회, 연출, 확률 공시표(→05), 자동 장착 규칙
7. 경제: 재화별 소스/싱크, 인플레이션 통제, 오프라인 보상(상한·계산·광고 2배·수령 플로우), 일일 콘텐츠 MVP 포함 여부
8. 광고/수익화: 보상형 광고 슬롯(용도·보상·일일 제한·실패 처리), IAP 로드맵(v1.1)
9. 범위: MVP In / Out / MVP+ 표(MVP 문서 §2 형식), 각 항목 DoD
10. 유저 플로우: 첫 실행 → (최소 튜토리얼) → 루프 → 종료 → 재실행, 상태 다이어그램
11. v1.1+ 로드맵: 던전, 스킬 가챠, 동료, 컬렉션 등 + 우선순위 근거

모든 시스템은 **시스템 스펙 템플릿** 적용 [PP §5]: 목적/플레이어 체감(한 문장) · 규칙(번호 목록) · 공식·수치(→05 참조) · 데이터(SO/PlayerSave 필드명) · UI 연결(→03) · 예외·엣지(골드 부족, 연타, 앱 종료 중, 네트워크 끊김, 시간 조작) · DoD(체크 가능) · 확장 여지(v1.1+)

### 10-3. `03_ui_ux.md` 필수 섹션 [PP §4-03]

1. 화면 목록 + 네비게이션 맵(mermaid)
2. 세로 레이아웃 그리드: 전투 뷰 영역 %, 스킬바, 슬라이드 패널 높이, 탭바, SafeArea (ASCII 전체 레이아웃)
3. 화면별 ASCII 와이어프레임 + 요소 표(요소명/바인딩 데이터/이벤트/상태): HUD, 스테이지 바, 캐릭터(스탯 강화), 장비/인벤, 가챠, 스킬, 설정, 오프라인 보상 팝업, 보스 등장/실패 팝업, 광고 확인 팝업, 로딩/패치, 토스트
4. 인터랙션 규칙: 패널 하나만 열림, 슬라이드 방향·시간, 탭 강조, Android 뒤로가기, 연타 방어
5. 연출 리스트(DOTween 표준값): 골드 카운트업, 강화 성공 펀치, 가챠 카드 뒤집기, 데미지 텍스트 궤적, 히트 플래시, 카메라 셰이크
6. 텍스트/폰트/색: 픽셀 폰트(한글 지원) 선정, 등급 색상 코드 표, 숫자 포맷 규칙, 문자열 리소스(현지화 대비 최소 구조)
7. 저사양/접근성: 이펙트 축소 옵션, 30fps 모드

### 10-4. `06_milestones_wbs.md` 필수 섹션 [PP §4-06]

1. 페이즈 정의(예: P0 전환 → P1 전투 코어 → P2 성장 루프 → P3 UI → P4 메타(오프라인·광고·일일) → P5 QA·출시), 각 DoD와 데모 조건
2. WBS 표: 태스크 ID(`P1-03`) / 설명 / 산출물(파일·프리팹·클래스) / 선행 / 예상 시간(h) / 우선순위(M·S·C) / 검증 방법
3. 1인 개발 주간 계획 예시(주당 투입 시간 명시 가정), 버퍼 비율
4. 리스크 등록부(리스크/확률/영향/대응/트리거 신호)

### 10-5. `08_migration_plan.md` 필수 섹션 [PP §4-08]

1. 브랜치/태그 전략(현재 상태 아카이브 태그, 작업 브랜치)
2. 삭제/보존/이동 파일 목록 — 실제 레포 경로 기준 (RPGHero, Pure Poly, Models, Animations, Tiles, NavMesh 데이터, 3D 머티리얼, SafeArea.unity, 3D 전용 에디터 위저드 …)
3. 패키지 변경(add/remove): `com.unity.ai.navigation` 제거 여부, `com.unity.feature.2d` 2.0.1 Pixel Perfect·Aseprite Importer 포함 여부 `[확인 필요]`
4. 프로젝트 설정: Orientation Portrait, URP 2D Renderer 생성·연결, Physics2D, Quality, Input
5. 단계별 절차 + 각 단계 검증(컴파일 통과 / 에디터 플레이 / AAB 빌드 통과)
6. Firebase 노드 전환(`users` → `users_v2`), 기존 개발 데이터 처리, `SaveManager` 개념 이식
7. `CLAUDE.md` 갱신 초안 전문(2D 기준; 적용은 하지 않고 문서 안에 초안만)

### 10-6. 작업 순서·검증 [PP §6, §7]

- 작성 순서: 읽기 → README + 00 ADR → **01 GDD → 05 밸런스(시뮬레이션) → 02 TDD → 03 UI → 04 아트 → 08 마이그레이션 → 06 일정 → 07 QA → 09 체크리스트** (05가 02보다 앞: 데이터 타입·필드가 밸런스 수식에서 나옴)
- 교차 검증 패스: 용어집 한↔영 일치, 05 상수 참조형(`→ 05 §`)만 사용, PlayerSave 필드명 01/02/09 일치, 화면명 03/09 일치, 09 추적표 빈 칸 없음, `[확인 필요]` 총 목록
- 최종 보고: 생성 파일 목록(줄 수), 가정 통합본, `[확인 필요]` 목록, 미결 사항, 다음 세션 착수 지시문 예시("08 문서의 Step 1부터 실행해")
- 자기 검토 체크리스트(§7): 클래스명·필드명·타입 유무 / 모든 수치 근거 / "적절히·나중에·TBD" 없음 / 모든 시스템에 예외·DoD / MVP 원칙 4종 계승 / 정규분포 가챠·오프라인·광고 2배 차별점 유지 / 식별자 영어 / 상호 참조 유효 / `docs/2d/` 밖 미변경
