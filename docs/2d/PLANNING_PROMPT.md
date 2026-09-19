# SoloHero 2D 리빌드 — 기획서·산출물 작성 프롬프트

> 사용법: 이 파일 전체를 새 Claude Code 세션에 붙여넣거나, `docs/2d/PLANNING_PROMPT.md 를 읽고 그대로 수행해` 라고 지시한다.
> 작업 디렉터리는 레포 루트(`D:\git\solohero`)여야 한다.

---

## 0. 역할과 목표

너는 1인 개발 모바일 방치형 RPG **SoloHero**의 리드 기획자 겸 테크니컬 디렉터다.
이번 작업의 목표는 **코드를 짜는 것이 아니라**, 앞으로 수개월간 개발의 단일 기준(single source of truth)이 될
**2D 리빌드 기획서 + 산출물 패키지**를 `docs/2d/` 아래에 완성하는 것이다.

이 문서들은 다음 조건을 만족해야 한다.
- 개발자가 문서만 보고 바로 태스크를 시작할 수 있을 만큼 구체적이다 (클래스명, 필드명, 수치, 파일 경로까지).
- 문서 간 모순이 없다 (용어·상수·필드명·화면명이 전부 일치).
- 모든 수치는 근거(공식 + 시뮬레이션 결과)가 있다.
- 나중에 Claude Code에게 "06 문서의 P1-03 태스크를 구현해" 라고 지시하면 그대로 구현 가능한 수준이다.

---

## 1. 절대 규칙

1. **코드·에셋·프로젝트 설정을 절대 수정하지 않는다.** `Assets/`, `Packages/`, `ProjectSettings/`, `CLAUDE.md`, `Jenkinsfile`, `.github/` 변경 금지.
   파일 생성은 `docs/2d/` 하위에만 허용한다. (예외: 검증용 스크립트는 `docs/2d/tools/`)
2. 아래 **§3 확정 결정**은 되묻지 않는다. 이미 결정된 사항이다.
3. 모르는 것은 질문 대신 **"가정"으로 명시하고 진행**한다. 각 문서 상단에 `## 가정` 섹션을 둔다.
   질문은 답에 따라 문서가 크게 갈릴 때만, 한 번에 최대 3개까지.
4. 언어: 문서 본문은 **한국어**. 코드 식별자·파일명·필드명·enum 값·씬명은 **영어**.
   (이 프로젝트는 .cs 파일 안의 한국어가 인코딩 깨짐을 일으킨 이력이 있어, 코드 내 텍스트는 English-only가 컨벤션이다.)
5. 수치에 "적절히", "나중에 조정", "TBD" 를 쓰지 않는다. **초기값 + 조정 방법**을 반드시 적는다.
6. 상수의 단일 출처는 `05_balance_data.md` 다. 다른 문서는 값을 복사하지 말고 `→ 05 §x.y` 로 참조한다.
7. 검증하지 않은 외부 사실(에셋 라이선스, 스토어 정책 세부 등)은 **`[확인 필요]`** 태그를 단다. 아는 척하지 않는다.
8. 산문보다 **표·번호 목록·다이어그램(mermaid)·ASCII 와이어프레임**을 우선한다.
9. 파일 하나 = 주제 하나. 한 파일이 600줄을 넘기면 하위 파일로 분리한다.
10. 마지막에 반드시 **교차 검증 패스(§6-4)** 를 수행하고 결과를 보고한다.

---

## 2. 컨텍스트 (As-Is) — 작업 시작 전 반드시 읽을 것

### 2-1. 프로젝트 개요
- 레포: `D:\git\solohero` (git, main 브랜치)
- 엔진: **Unity 2022.3.62f3 LTS**, C#, **URP 14.0.12** (현재 3D Forward Renderer)
- 현재 게임: 쿼터뷰 **3D** 방치형 액션 RPG (Landscape 1920×1080), 캡슐 플레이스홀더 → 3D 모델 교체 단계에서 **중단하고 2D로 전환하기로 결정**
- 플랫폼: Android (AAB), 향후 iOS 미정
- 백엔드: Firebase Realtime Database / Authentication(익명) / Analytics — EDM4U 관리
- 광고: Google Mobile Ads v25.0.0 (보상형 광고), App ID는 `Assets/Plugins/Android/AndroidManifest.xml`
- 에셋 배포: Addressables 1.22.3 (리모트 = Firebase Hosting) — 실사용 여부 재검토 대상
- 라이브러리: UniTask(비동기), DOTween(트위닝), Newtonsoft.Json, TextMeshPro, Input System 1.14.2 (Both 모드)
- 빌드: Jenkins(로컬 Windows batchmode) + GitHub Actions(GameCI) 이중 파이프라인, 진입점 `Assets/Editor/BuildAutomator.cs` → `Builds/game.aab`
- 환경 제약: **JDK 11 고정**(21은 Firebase 호환 문제), Gradle Java 11, `google-services.json`은 `Assets/`에 있어야 함(미커밋)
- API 제약: Unity 2022.3에 없는 API 사용 금지 (예: `AssetDatabase.AssetPathExists`는 2023.1+ → `File.Exists` 사용)

### 2-2. 현재 구현된 시스템 (3D 기준, 총 ~5,900줄 / 49개 .cs)
| 영역 | 파일 | 재활용 판단 힌트 |
|---|---|---|
| 저장 | `Assets/Scripts/Managers/SaveManager.cs` | RTDB + PlayerPrefs 백업 + 250ms 디바운스 큐. **개념 재활용 가치 높음** |
| DTO | `Assets/Scripts/Data/PlayerData.cs` | v1 스키마(gold long, chapter, stageNumber, equipped×4, ownedEquipmentCsv, upgrade×4, gachaPullCount, lastQuitTimeUtc, dataVersion=1). **v2 설계 + 마이그레이션 기준점** |
| 가챠 | `Assets/Scripts/Systems/GachaSystem.cs` | **Box-Muller 정규분포 + 100회 천장(Legendary 보장) + pity 진행도에 따라 μ 0.3→0.8 이동.** 이 프로젝트의 차별점이므로 **반드시 계승** |
| 강화 | `Assets/Scripts/Systems/UpgradeService.cs` | HP/ATK/DEF/SPD, 비용 `baseCost×(level+1)`, 최대 50 — 방치형 곡선으로 **재설계 대상** |
| 장비 | `Assets/Scripts/Systems/PlayerEquipmentService.cs`, `Data/EquipmentData.cs` | 4슬롯(Sword/Helm/Armor/Boots) × 4등급 SO 16종 |
| 오프라인 | `Assets/Scripts/Systems/OfflineRewardSystem.cs`, `UI/OfflineRewardPopup.cs` | 최대 21,600초, 초당 골드 고정값, 광고 2배 연결됨 |
| 광고 | `Assets/Scripts/Managers/AdMobService.cs`, `MainThreadDispatcher.cs` | 테스트 유닛 ID 사용 중 |
| 진입 | `Assets/Scripts/Managers/GameManager.cs` | 익명 로그인 → 로드 → 오프라인 계산 → GameScene |
| 스테이지 | `Assets/Scripts/Managers/StageManager.cs` | 공식 기반: kills=`5+(ch−1)×10+(stage−1)×2`, gold=`50+(ch−1)×100+(stage−1)×20` |
| 전투 | `Controllers/Characters/*.cs` (Player/Enemy/Auto/Spawn) | 3D NavMesh + OverlapSphere. **2D에서는 전부 폐기** |
| UI | `Assets/Scripts/UI/*.cs` (HUD, MainBottomNav, TopRightMenu, 패널 5종) | 가로 레이아웃 기준. 구조(탭→패널 슬라이드, 하나만 열림)는 계승, 구현은 폐기 |
| 에디터 | `Assets/Editor/MenuSetupWizard.cs`, `SoulStrikeSetupWizard.cs` 등 | 씬/캔버스 자동 생성 위저드 패턴은 계승 |
| 데이터 | `Assets/StreamingAssets/JSON/*.json`, `Models/JsonDataManager.cs` | JSON vs ScriptableObject 단일화 결정 필요 |

### 2-3. 기존 문서 (반드시 읽고 계승/폐기 판단)
- `docs/soulstrikeSummary.md` — 레퍼런스 게임(소울스트라이크) 분석. UI 구조·소환풀·성장 로드맵의 원본
- `docs/SoloHero_MVP_개발계획서.txt` — 기존 MVP 정의, DoD, 저장 정책(4.3~4.4), QA 시나리오 S1~S10, 리스크. **형식과 원칙은 그대로 계승**
- `docs/무료에셋추천.txt` — 3D 기준 에셋 목록. 2D용으로 전면 교체

### 2-4. 작업 시작 시 읽을 파일 (순서대로)
1. `CLAUDE.md`
2. `docs/SoloHero_MVP_개발계획서.txt`, `docs/soulstrikeSummary.md`
3. `Assets/Scripts/Data/PlayerData.cs`, `Assets/Scripts/Managers/SaveManager.cs`, `Assets/Scripts/Systems/GachaSystem.cs`
4. `Assets/Scripts/Managers/GameManager.cs`, `Assets/Scripts/Managers/StageManager.cs`, `Assets/Scripts/Systems/OfflineRewardSystem.cs`
5. `Packages/manifest.json`, `Assets/Editor/BuildAutomator.cs`
6. `Assets/` 최상위 폴더 목록 (마이그레이션 문서의 삭제/보존 목록 작성용)

---

## 3. 확정 결정 (To-Be) — 변경 불가

| # | 항목 | 결정 |
|---|---|---|
| D1 | 전투 뷰 | **2D 횡스크롤 사이드뷰.** 히어로가 오른쪽으로 자동 전진, 적 웨이브가 오른쪽에서 등장. 배경 패럴랙스 스크롤 |
| D2 | 화면 방향 | **Portrait 1080×1920** (CanvasScaler 기준 해상도). 현재 Landscape는 폐기 |
| D3 | 코드 | **같은 레포에서 `Assets/Scripts` 전면 재설계.** 기존 코드는 참고용(패턴·공식)으로만. Firebase/AdMob/EDM4U/Addressables/빌드 파이프라인 등 **인프라는 유지** |
| D4 | 아트 | **픽셀아트**, 무료 에셋(itch.io / Asset Store / Kenney 등) 기반 |
| D5 | 장르 | 자동 전투 **방치형 RPG** (버섯커 키우기·세븐나이츠 키우기·소울스트라이크 계열). 조작은 최소, 스킬 자동 발동 |
| D6 | 엔진 | Unity 2022.3.62f3 유지, URP는 **2D Renderer**로 전환, UniTask·DOTween·Newtonsoft 유지 |
| D7 | 차별점 | **정규분포(Box-Muller) + 천장 가챠** 계승, 오프라인 보상 + 광고 2배 계승 |
| D8 | 지금 단계 | **문서만 작성한다.** 코드 전환은 문서 완성 후 별도 세션에서 `08_migration_plan.md`를 따라 진행 |

---

## 4. 산출물 목록 — 파일별 필수 섹션

모든 파일은 `docs/2d/` 아래. 각 파일 최상단에 `# 제목`, `> 버전 / 작성일 / 상태(초안·검토·확정)`, `## 가정` 을 둔다.

### `README.md` — 인덱스
- 한 줄 정의, 확정 결정 표(§3 그대로), 문서 목록 + 읽는 순서, 용어집(한↔영 대응: 히어로/Hero, 강화/Upgrade, 스테이지/Stage …), 문서 갱신 규칙

### `00_decisions_adr.md` — 결정 기록 (ADR)
- ADR-001 ~ : 각각 `배경 / 선택지(최소 2개, 장단점) / 결정 / 결과·영향`
- 최소 포함: 2D 전환 이유, 세로 화면, 같은 레포 재설계, 픽셀아트, JSON vs ScriptableObject, 서비스 로케이터 vs DI, Physics2D vs 거리 판정, 큰 수 표현(double vs long vs BigInteger), Addressables 유지 여부, 재화 1종 vs 2종, 저장 노드 `users` vs `users_v2`

### `01_gdd.md` — 게임 기획서
1. **비전**: 한 줄 정의, 타깃 유저, 세션 설계(3분 / 15분 / 하루 단위로 유저가 하는 일), 디자인 필러 3개, 레퍼런스 게임에서 **가져올 것 / 안 가져올 것** 표
2. **핵심 루프** 다이어그램(mermaid) + 각 단계의 체감 목표
3. **전투 (사이드뷰)**: 히어로 화면 내 고정 X 비율, 전진/정지 조건, 적 스폰 규칙(위치·간격·동시 최대 수), 타겟팅, 사거리, 공격 판정 방식, 데미지 공식(→05), 크리티컬, 스킬 자동 발동 규칙, 피격/사망/부활, 보스 규칙(등장 연출·시간 제한 여부), **보스 실패 시 후퇴·파밍 모드**, 자동전투 토글 의미(있다면)
4. **스테이지/진행**: 챕터-스테이지 구조(N스테이지/챕터, 보스 위치), 킬 요구, 챕터 테마 순환, 무한 확장 방식, `최고 도달` vs `현재 파밍` 스테이지 분리, 스테이지 선택 UI 유무
5. **성장**: 히어로 레벨(EXP), 스탯 강화(종류·골드), 장비(슬롯·등급·스탯·중복 처리 규칙), 스킬(슬롯 수·해금·레벨업), **배율 합산 순서(가산/승산)를 수식으로 명시**
6. **가챠**: 정규분포+천장 규칙 계승 및 등급 확장, 비용 재화, 1회/10회, 연출 단계, 확률 공시표(→05), 자동 장착 규칙
7. **경제**: 재화별 소스/싱크 표, 골드 인플레이션 통제 장치, 오프라인 보상(상한·계산 기준·광고 2배·수령 플로우), 일일 콘텐츠(일일 퀘스트/출석) MVP 포함 여부
8. **광고/수익화**: 보상형 광고 슬롯 목록(용도·보상·일일 제한·실패 처리), IAP 로드맵(v1.1)
9. **범위**: MVP In / Out / MVP+ 표 (기존 MVP 문서 §2 형식 계승), 각 항목의 DoD
10. **유저 플로우**: 첫 실행 → (최소 튜토리얼) → 루프 → 종료 → 재실행, 상태 다이어그램
11. **v1.1+ 로드맵**: 던전, 스킬 가챠, 동료, 컬렉션 등 후보와 우선순위 근거

각 시스템은 아래 **시스템 스펙 템플릿(§5)** 형식을 따른다.

### `02_tdd.md` — 기술 설계서
1. 아키텍처 개요: 레이어 다이어그램(mermaid), 의존 방향 규칙, **Assembly Definition 분리안**(예: Core / Data / Gameplay / Systems / UI / Services / Editor / Tests)
2. 폴더 구조 (`Assets/_Project/...` 전체 트리)
3. 씬 구성(Boot / Title / Main 등)과 **부트스트랩 순서** (Firebase init → 익명 auth → 로드 → 마이그레이션 → 오프라인 계산 → Main 진입), 실패 분기
4. 핵심 패턴: 서비스 로케이터 vs DI 선택(→ADR), 이벤트 버스, UI MVP(Presenter 순수 C# / View MonoBehaviour), 순수 C# 시스템으로 테스트 가능하게, MonoBehaviour 최소화 원칙
5. **전투 런타임**: `BattleDirector` 상태머신 다이어그램(Spawning / Fighting / BossIntro / Clear / Retreat / Dead …), Unit/Hero/Enemy 클래스 다이어그램, 스폰·풀링, 판정 방식(→ADR), 투사체, 데미지 숫자 풀, 카메라 + 패럴랙스 구현 방식, **Pixel Perfect Camera 설정값**(PPU, 기준 해상도), Sorting Layer 표, Animator 파라미터 표, 스프라이트시트 규약, 애니메이션 이벤트(히트 프레임)
6. **데이터**: ScriptableObject 정의 목록(각 SO의 필드 표: 이름/타입/설명/예시값), 밸런스 상수 SO, JSON 사용 여부(→ADR)
7. **저장**: `PlayerSave` v2 전체 필드 표(이름/타입/기본값/용도/저장 트리거), **v1→v2 마이그레이션 매핑표**, Firebase 노드 구조, 저장 트리거 목록 + 디바운스 정책(기존 4.4 계승), 로컬 백업, 원격/로컬 충돌 정책(기존 4.3 계승), 오프라인 시간 검증(기기 시간 조작 방어)
8. 큰 수 처리(타입 결정→ADR, 포맷터 규격 K/M/B/T/aa…), 난수(시드·재현성)
9. 외부 SDK: Firebase Analytics 이벤트 목록(이름/파라미터/발화 시점), AdMob 로드/표시/실패/재시도 시퀀스, Addressables 사용 범위 결정(→ADR)
10. 성능 예산: 목표 FPS, 드로우콜 상한, 메모리 상한, 동시 적 수, 풀 크기, **30분·2시간 방치 안정성 대책**(누수 지점 목록)
11. 에디터 툴: 2D 셋업 위저드(씬·캔버스·정렬 레이어 자동 생성), SO 생성기, 밸런스 시트 임포터(있다면)
12. 테스트 전략: EditMode 테스트 대상(가챠 분포, 강화 비용, 오프라인 계산, 마이그레이션, 큰 수 포맷), PlayMode 스모크
13. 빌드/CI: Portrait 설정, `BuildAutomator` 변경점, Jenkins/GHA 유지 사항

### `03_ui_ux.md` — UI/UX 설계서
1. 화면 목록 + 네비게이션 맵(mermaid)
2. **세로 레이아웃 그리드**: 전투 뷰 영역 %, 스킬바, 슬라이드 패널 높이, 탭바, SafeArea 처리 (ASCII 전체 레이아웃)
3. 화면별 **ASCII 와이어프레임 + 요소 표(요소명 / 바인딩 데이터 / 이벤트 / 상태)**: HUD, 스테이지 바, 캐릭터(스탯 강화), 장비/인벤, 가챠, 스킬, 설정, 오프라인 보상 팝업, 보스 등장/실패 팝업, 광고 확인 팝업, 로딩/패치, 토스트
4. 인터랙션 규칙: 패널 하나만 열림, 슬라이드 방향·시간, 탭 강조, Android 뒤로가기, 연타 방어
5. 연출 리스트(DOTween 표준값: 골드 카운트업, 강화 성공 펀치, 가챠 카드 뒤집기, 데미지 텍스트 궤적, 히트 플래시, 카메라 셰이크)
6. 텍스트/폰트/색: 픽셀 폰트(한글 지원) 선정, 등급 색상 코드 표, 숫자 포맷 규칙, 문자열 리소스 관리(현지화 대비 최소 구조)
7. 저사양/접근성: 이펙트 축소 옵션, 30fps 모드

### `04_art_assets.md` — 아트/에셋 계획
1. 아트 디렉션: PPU, 캐릭터 기준 px 크기, 팔레트 정책, 배경 레이어 수, 픽셀 퍼펙트 기준 해상도(→02 §5와 일치)
2. **필요 에셋 목록** (히어로 / 적 N종 / 보스 N종 / 챕터 배경 테마 N종 / VFX / UI / 아이콘 / 폰트 / SFX·BGM): 각 항목마다 필요한 애니메이션 클립·권장 프레임 수·**후보 무료 에셋(이름·URL·라이선스 `[확인 필요]`)**·대안
3. 임포트 규약: Texture 설정(Filter Point, Compression None, PPU), Sprite Atlas 구성, 네이밍·폴더 규칙
4. 애니메이션 규약: 클립명 표준, 필수 클립(Idle/Run/Attack/Hit/Die + 스킬), 히트 프레임 이벤트 규칙
5. 플레이스홀더 전략: 에셋 없어도 개발 진행 가능한 색상 박스 규격
6. 라이선스 관리표(출시 전 체크리스트, 크레딧 표기 필요 항목)

### `05_balance_data.md` + `tools/balance_sim.py` — 밸런스·데이터 설계
1. **설계 목표 곡선을 먼저 정의**: 예) 1일차 1-10 보스 도달, 3일차 챕터 3, 7일차 챕터 5, 첫 정체 구간 위치, 하루 골드 순환 시간
2. **상수표 전체** (이름/값/단위/설명) — 다른 문서가 참조하는 유일한 출처
3. 공식: 플레이어 스탯(레벨·강화·장비·스킬 합산), 적 HP/ATK/골드/EXP 스케일링(스테이지 전역 인덱스 기준), 보스 배율, 강화 비용, 장비 등급 배율, 스킬 데미지/쿨다운, 오프라인 골드/초, 가챠 μ·σ·등급 임계값·천장
4. **시뮬레이션**: `tools/balance_sim.py` 를 실제로 작성·실행(Python 3)하고 결과표를 문서에 첨부.
   - 입력: 상수표 값 / 출력: 스테이지별 도달 누적 시간, 정체 구간, 강화 레벨 추이, 골드 잔고
   - 가챠 Monte Carlo: pity 0 / 25 / 50 / 75 / 99 시점의 등급별 확률표 (10만 회 이상)
   - 목표 곡선(§1)과 비교해 **조정 이력**을 남긴다 (초기값 → 조정 이유 → 최종값)
5. 데이터 시트: SO 필드 = 표 열로 **초기 데이터를 실제 값으로** 채운다 — 적 최소 8종, 보스 최소 3종, 장비 최소 20종, 스킬 최소 6종, 챕터 테마 최소 5종
6. 확률 공시표(스토어 정책용 최종 포맷)

### `06_milestones_wbs.md` — 마일스톤·일정·WBS
1. 페이즈 정의(예: P0 전환 → P1 전투 코어 → P2 성장 루프 → P3 UI → P4 메타(오프라인·광고·일일) → P5 QA·출시) 각 **DoD**와 데모 조건
2. **WBS 표**: 태스크 ID(`P1-03` 형식) / 설명 / 산출물(파일·프리팹·클래스) / 선행 태스크 / 예상 시간(h) / 우선순위(M·S·C) / 검증 방법
3. 1인 개발 기준 주간 계획 예시(가정: 주당 투입 시간을 명시), 버퍼 비율
4. 리스크 등록부(리스크 / 확률 / 영향 / 대응 / 트리거 신호)

### `07_qa_plan.md` — QA 계획
1. 테스트 레벨(EditMode 유닛 / 스모크 / 시나리오 / 방치 / 기기)
2. 시나리오 표(ID / 전제 / 절차 / 기대 / 우선순위) — 기존 S1~S10 계승 + 2D·세로·보스·후퇴·마이그레이션·광고 실패 시나리오 추가
3. 방치 테스트 프로토콜(30분 / 2시간, FPS·메모리 로그 수집 방법)
4. 기기 매트릭스(저·중·고사양, 노치·펀치홀)
5. 버그 심각도 정의, 출시 게이트(Critical 0 등 정량 기준)

### `08_migration_plan.md` — 전환 절차 (실제 실행 매뉴얼)
1. 브랜치/태그 전략(현재 상태 아카이브 태그, 작업 브랜치)
2. **삭제 / 보존 / 이동 파일 목록 — 실제 레포 경로 기준으로 작성** (`Assets/` 를 읽고 하나씩 판단: RPGHero, Pure Poly, Models, Animations, Tiles, NavMesh 데이터, 3D 머티리얼, SafeArea.unity, 3D 전용 에디터 위저드 …)
3. 패키지 변경(add / remove): `com.unity.ai.navigation` 제거 여부, `com.unity.feature.2d` 2.0.1에 Pixel Perfect·Aseprite Importer 포함 여부 `[확인 필요]`
4. 프로젝트 설정 변경: Orientation Portrait, URP 2D Renderer 생성·연결, Physics2D, Quality, Input
5. **단계별 절차** + 각 단계의 검증(컴파일 통과 / 에디터 플레이 / AAB 빌드 통과)
6. Firebase 노드 전환(`users` → `users_v2`), 기존 개발 데이터 처리, `SaveManager` 개념 이식 방법
7. `CLAUDE.md` 갱신 초안 전문(2D 기준으로 다시 쓴 내용 — 적용은 하지 말고 문서 안에 초안만)

### `09_deliverables_checklist.md` — 산출물 체크리스트 + 추적표
1. 산출물 전체 목록(문서 / 씬 / 프리팹 / SO / 스크립트 클래스 / 에디터 툴 / 테스트 / 빌드 / 스토어 자산: 아이콘·스크린샷·개인정보처리방침·광고 정책 항목) 각 `☐` 상태
2. **추적표(Traceability Matrix)**: GDD 시스템 ↔ TDD 클래스 ↔ PlayerSave 필드 ↔ UI 화면 ↔ WBS 태스크 ↔ QA 시나리오. 빈 칸이 있으면 누락이다.

---

## 5. 시스템 스펙 템플릿 (GDD의 모든 시스템에 적용)

```
### N. <시스템명> (<EnglishName>)
- 목적 / 플레이어 체감: 한 문장
- 규칙: 번호 목록 (모호한 표현 금지)
- 공식·수치: → 05 §x.y 참조 (값 복사 금지)
- 데이터: 관련 SO / PlayerSave 필드명
- UI 연결: 어느 화면(→03 §x), 어떤 피드백
- 예외·엣지 케이스: 골드 부족, 연타, 앱 종료 중, 네트워크 끊김, 시간 조작 …
- 완료 기준(DoD): 체크 가능한 문장
- 확장 여지(v1.1+): 한 줄
```

---

## 6. 작업 순서

1. **읽기**: §2-4 파일을 순서대로 읽는다. 읽은 뒤 "계승 / 폐기 / 재설계" 판단을 메모한다.
2. **README + 00 ADR 초안** 작성 (결정을 먼저 고정해야 뒤 문서가 흔들리지 않는다).
3. 작성 순서: **01 GDD → 05 밸런스(시뮬레이션 실행 포함) → 02 TDD → 03 UI → 04 아트 → 08 마이그레이션 → 06 일정 → 07 QA → 09 체크리스트·추적표**
   - 05를 02보다 먼저 쓰는 이유: 데이터 타입·필드가 밸런스 수식에서 나오기 때문.
4. **교차 검증 패스**: 아래를 실제로 grep/대조하여 결과를 표로 보고한다.
   - 용어집의 한↔영 대응이 모든 문서에서 동일한가
   - 05의 상수명이 다른 문서에서 참조 형태(`→ 05 §`)로만 쓰였는가 (값 복붙 없음)
   - PlayerSave 필드명이 01 / 02 / 09 에서 일치하는가
   - 화면명이 03 / 09 에서 일치하는가
   - 09 추적표에 빈 칸이 없는가
   - `[확인 필요]` 태그 총 목록
5. **최종 보고**: 생성 파일 목록(줄 수 포함), 가정 목록 통합본, `[확인 필요]` 목록, 미결 사항(사용자 결정 필요), 다음 세션 착수 지시문 예시("08 문서의 Step 1부터 실행해").

---

## 7. 품질 기준 (자기 검토 체크리스트)

- [ ] 개발자가 문서만 보고 클래스를 만들 수 있는가 (클래스명·필드명·타입이 있는가)
- [ ] 모든 수치에 공식·근거·시뮬레이션 결과가 있는가
- [ ] "적절히 / 나중에 / TBD" 가 없는가
- [ ] 모든 시스템에 예외 케이스와 DoD가 있는가
- [ ] 기존 MVP 문서의 원칙(루프 우선 / 스키마 고정 / 저장 안전성 / 범위 통제)이 계승되었는가
- [ ] 정규분포 가챠·오프라인 보상·광고 2배가 차별점으로 살아 있는가
- [ ] 코드 식별자가 전부 영어인가
- [ ] 문서 간 상호 참조 링크가 실제 섹션을 가리키는가
- [ ] `docs/2d/` 밖의 파일을 하나도 건드리지 않았는가
