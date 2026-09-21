---
title: 'Game Architecture'
project: 'SoloHero'
date: '2026-09-20'
author: 'Tae-jun'
version: '1.0'
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8, 9]
status: 'complete'
engine: 'Unity 2022.3.62f3 (last Personal/Pro patch of 2022.3)'
platform: 'Android (Google Play, AAB) — Portrait 1080×1920'

# Source Documents
gdd: '_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/gdd.md'
epics: '_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/epics.md'
decision_log: '_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/decision-log.md'
brief: null
---

# SoloHero — Game Architecture

## Executive Summary

**SoloHero**는 Unity 2022.3.62f3 위에서 Android 단독·세로 1080×1920으로 만드는 픽셀아트 2D 횡스크롤 방치형 RPG다. 이 아키텍처의 기조는 **장르 표준 모방**이다 — 버섯커 키우기·세븐나이츠 키우기·Soul Strike가 검증한 관습을 그대로 따르고, 고유 설계는 두지 않는다.

**핵심 결정**

- **순수 C# `SoloHero.Core`** (asmdef `noEngineReferences`)가 전투 상태 머신·성장·가챠·경제·저장 DTO·이관·공식을 소유한다. MonoBehaviour는 씬에 존재해야 하는 뷰·UI·인프라에만 쓴다. 단위 테스트 5영역과 밸런스 시뮬레이터가 이 어셈블리만으로 돈다.
- **저장은 `users/{uid}/v2` 새 노드 + Newtonsoft + 로컬 백업 + 250 ms 디바운스.** v1 강화 레벨은 의미가 바뀌었으므로(가산 → 승산) 누적 지출 골드로 환급한다. 수치는 `double`(RTDB가 double로 저장하므로).
- **데이터는 ScriptableObject 단일 출처.** `BalanceConfig` 필드명 = GDD 상수명. Addressables·StreamingAssets JSON·Input System 패키지·Resources 폴더는 제거한다.
- **가챠는 등급별 확률표(55/33/10/2 %) + 100회 천장 + Legendary 획득 시 리셋** (정규분포 방식 폐기. GDD 역반영 완료 — decision-log D-049~D-052).
- **런타임 생성 0** — 적·데미지 텍스트·VFX·SFX는 `UnityEngine.Pool` 풀에서만 공급.
- **Android 타깃 API 36 + 16 KB 정렬**은 E1-09 실빌드 스파이크로 먼저 확인하고, 필요하면 JDK 17 전환을 허용한다.

**프로젝트 구조:** 하이브리드(종류별 → 어셈블리 → 도메인), `Assets/SoloHero/` 아래에 프로젝트 소유 파일 전부. 핵심 시스템 9개, 어셈블리 4개, 씬 2개(Boot·Game).

**구현 패턴:** 표준 패턴 12개(통신·엔티티·상태·데이터·스탯/수식·변경-저장·시간/난수 주입·가챠·UI·비동기·에러·로깅) 전부 코드 예시 포함, 일관성 규칙 11항목. 고유 패턴 0개.

**준비 상태:** 에픽·스토리 정제 및 E1 착수 가능. E5(가챠)의 GDD 역반영(O1·O2)은 2026-09-20 완료되어 착수 제한이 없다.

## Document Status

**상태:** 완료 (9 / 9). 2026-09-20.

GDD가 아키텍처로 이월한 4건은 모두 닫혔다.

| ID | 이월 항목 | 결정 |
|---|---|---|
| C-004 | 에셋 전달 방식 | **D1** — 직접 참조, Addressables 제거 |
| C-007 | 데이터 정의 방식 | **D2** — ScriptableObject 전면 + `BalanceConfig` |
| — | 큰 수 표현 타입 | **D3** — `double` + 접미어 포맷터 |
| — | 저장 노드 전환 및 v1→v2 이관 | **D4 / ADR-4** — `users/{uid}/v2` 노드, 값 변환 규칙, 강화 골드 환급 |

이 문서가 GDD에 되돌린 것: **R9**(가챠 방식 → D-049~D-051), **ADR-4**(E1-06 문구 → D-052). 2026-09-20 완료.

---

## As-Is 코드베이스 (커밋 `b87bb78` 기준)

3D 잔재를 제거한 뒤 확인된 사실이다. 이후 단계는 이 값을 기준점으로 삼는다.

| 항목 | 확인된 상태 | 아키텍처 함의 |
|---|---|---|
| 렌더 파이프라인 | `Settings/UniversalRP.asset` → `Renderer2D.asset` | URP 2D Renderer 전환 완료. 별도 결정 불필요 |
| 화면 방향 | Portrait 고정 (autorotate 세로만 허용) | GDD 1080×1920 전제와 일치 |
| Android minSdk | 24 (Android 7.0) | GDD 목표 사양과 일치. Q-6 해소 |
| 생존 런타임 스크립트 | 15개 | 계승 대상은 가챠·저장·오프라인·강화·장비·인프라에 한정 |
| 삭제된 3D 런타임 | 29개 (NavMesh·OverlapSphere·Rigidbody3D·가로 UI) | 전투·스폰·UI는 전면 신규 설계 |
| 씬 | 없음 (`Assets/Scenes` 비어 있음), `EditorBuildSettings` 목록 초기화 | 부트 씬 구성은 아키텍처가 정의해야 함 |
| Addressables | 그룹에 애셋 엔트리 0건 (기본 그룹만 존재) | C-004를 백지에서 결정 가능 |
| 유지된 3D 의존 | `com.unity.modules.physics` | DOTween `DOTweenModulePhysics.cs`가 `Rigidbody` 참조. 제거 시 컴파일 실패 |
| 데이터 애셋 | 장비 SO 16종 + `StreamingAssets/JSON` 5종 병존 | C-007(JSON vs SO) 결정 대상이 실물로 둘 다 존재 |

미검증 항목: Unity 에디터 컴파일. 정적 참조 검사만 통과한 상태이며 에디터 실행 확인이 필요하다.

---

## Project Context

### Game Overview

**SoloHero** — 세로 화면 픽셀아트 2D 횡스크롤 방치형 RPG. 히어로가 오른쪽으로 자동 전진하며 적 웨이브를 스스로 처치하고, 플레이어의 입력은 전투가 아니라 **성장 결정**(강화·가챠·스킬 레벨업·파밍 스테이지 선택·광고 시청)에만 쓰인다. 두 축이 게임의 심장이다 — 앱을 끈 시간이 골드가 되는 **오프라인 축적**(최대 6시간)과, 등급별 확률표에 100회 천장을 더한 **장르 표준 장비 가챠**(Legendary 보장).

기존 3D 쿼터뷰 구현(약 5,900줄 / 49개 스크립트)에서 전면 피벗했다. 오프라인·저장·광고·빌드 인프라는 계승하고, 전투·스폰·UI·스테이지 공식·강화 공식·가챠 등급 결정은 전부 신규 설계다.

### Technical Scope

| 항목 | 값 | 출처 |
|---|---|---|
| **플랫폼** | Android 단독 (Google Play, AAB). iOS는 v1.0 Out of Scope | GDD Platform-Specific, D-024 |
| **최소 지원** | Android 7.0 (API 24) — As-Is 프로젝트 설정과 일치 | GDD Q-6, As-Is 표 |
| **화면** | Portrait 고정 1080×1920. 픽셀 퍼펙트 기준 270×480 정수 4배, PPU 32 | GDD Art Style, D-031 |
| **장르** | Idle/Incremental 주축 + RPG 병합 | D-002 |
| **설계 기조** | **장르 표준 모방.** 버섯커 키우기 · 세븐나이츠 키우기 · Soul Strike의 검증된 방치형 RPG 관습을 그대로 따른다. 가챠를 포함해 **고유 설계는 두지 않는다.** 관습이 있는 곳에서 새로 발명하지 않는다 | 사용자 방향 (아키텍처 세션) |
| **엔진 (고정)** | Unity 2022.3.62f3 LTS, URP 14.0.12 (Renderer2D) | GDD Dependencies, As-Is 표 |
| **백엔드** | Firebase RTDB(저장) / 익명 인증 / Analytics — 실시간 동기화 용도 아님 | GDD Platform-Specific |
| **광고** | Google AdMob 보상형 v25.0.0, 보상형만 사용 | GDD Economy 광고 슬롯 |
| **네트워킹** | 멀티플레이 없음. 단일 플레이어. 오프라인(네트워크 없음) 상태에서도 전투·성장·로컬 저장이 동작해야 함 | GDD Platform-Specific |
| **빌드 제약** | JDK 11 고정, Gradle Java 11, GitHub Actions 단일 CI (Jenkins 휴면 — 2026-09-21) | GDD Dependencies, CLAUDE.md |
| **프로젝트 규모** | 1인 개발. 9 에픽 / 132 스토리 (Must 122 · Should 10) | epics.md |
| **복잡도 수준** | **중간** — 시스템은 표준 방치형이나 저장 신뢰성·2시간 안정성 두 게이트가 엄격 | 본 문서 분석 |

### Core Systems

| # | 시스템 | 복잡도 | 핵심 요구 | GDD 절 | 에픽 |
|---|---|---|---|---|---|
| S1 | **부트 · 저장 파이프라인** | **높음** | 초기화 → 익명 인증 → 로드 → v1→v2 이관 → 오프라인 계산 → 전투 진입. 원격 우선 + 로컬 백업 폴백. 저장 트리거 7종, 250 ms 디바운스 큐, 강제 종료 20회 유실 0, 로드 실패 방어 UI | Technical Specifications › 저장 신뢰성 요구 | E1 |
| S2 | **자동 전투** | 중간 | 히어로 5상태(전진/교전/피격/스킬/사망), 스폰 큐(간격 0.8 s · 동시 4), 최근접 타겟팅(같은 프레임 재탐색), 히트 프레임 판정, 데미지·크리·비율 감쇄 공식, 동시 판정 우선순위 규칙 | Idle RPG › Auto-Combat System | E2 |
| S3 | **스테이지 · 챕터 진행** | 중간 | 전역 인덱스 `g` 단일 스케일링, 킬 8 고정, 보스(15배 HP · 30 s 타이머 · 등장 연출), 후퇴 파밍, highest/farming 분리, 챕터 테마·적 구성 데이터, 6+ 순환 | Level Design Framework | E3 |
| S4 | **성장** | 중간 | 히어로 레벨(EXP 자동), 강화 4레인(HP·ATK·DEF 승산·무상한 / 공격속도 가산·100), 장비 16종 등급 배율, 스탯 합산 순서(가산 → 승산 → 버프 합산 후 1회 승산), 스킬 3종(자동 조건 + 수동 탭 + 0.4 s 순차) | Character System, Upgrade Trees | E4 |
| S5 | **가챠** | **낮음** | 등급별 고정 확률표(C/R/E/L, 합 100%), 슬롯 균등 25%, 100회 천장 Legendary 보장, 10연 선차감, 중복 자동 환급, 확률표 그대로 게임 내 공시. **확률값 4개와 천장 리셋 규칙은 GDD 역반영 시 확정** | Inventory, Equipment and Gacha (개정 예정) | E5 |
| S6 | **경제 · 오프라인 · 광고** | 중간 | 골드/젬 소스·싱크, 오프라인 `파밍스테이지_골드/400` × 최대 21,600 s, 시간 조작 방어(음수·상한 2배 초과 → 0), 미수령 상태 유지, 광고 슬롯 3종 + 로컬 자정 리셋, 광고 실패 시 일반 경로 항상 유지 | Automation and Offline, Economy and Resources | E6 |
| S7 | **UI / UX 세로 레이아웃** | 중간 | 전투 뷰 55%, 하단 탭 5 + 패널 5(하나만 열림), 팝업 6종, 토스트 큐, 튜토리얼 3단계, Android 뒤로가기 정책, 전역 연타 방어, 큰 수 표기(K/M/B/T → 알파벳), SafeArea | Controls and Input, 저장 상태와 화면 대응 | E7 |
| S8 | **아트 · 연출 · 오디오** | 중간 | 픽셀 퍼펙트 임포트 규약, 적·데미지 텍스트·VFX 재사용 풀(런타임 생성 0), 이펙트 축소·30 fps 모드, 플레이스홀더 단색 박스로 전 기능 검증 가능 | Art and Audio Direction | E8 |
| S9 | **밸런스 시뮬레이터** | 낮음 | 상수표를 입력으로 스테이지별 누적 시간·골드·강화 추이 출력. **E4 착수 전 1차 수행.** 게임 코드와 같은 상수를 읽어야 함 | Number Balancing › 검증 필요 항목 | E9 |

### Technical Requirements

**성능 (출시 게이트)**

| 항목 | 목표 |
|---|---|
| 프레임레이트 | 중급 기기 60 FPS 지속, 하위 1% 프레임 50 이상. 30 FPS 모드 별도 |
| 드로우콜 | 전투 화면 120 이하 |
| 메모리 | 2시간 방치 후 400 MB 이하 |
| 30분 방치 | FPS 저하 10% 이내, 메모리 증가 10% 이내 |
| 2시간 방치 | 크래시 없음, 메모리 증가 20% 이내 |
| 콜드 스타트 → 전투 진입 | 8초 이내 (10회 평균) |
| 크래시 프리 세션 | 99.5% 이상 |
| 동시 화면 적 | 4마리 (스폰 상한) |

**저장 신뢰성**
- 원격(Firebase RTDB) 우선, 실패 시 로컬 백업으로 시작. 어떤 경우에도 "데이터가 초기화된 것처럼" 보이지 않아야 한다
- 저장 트리거 7종: 일시정지·종료(항상) + 스테이지 클리어 / 가챠 결과 확정 / 장착 변경 / 강화 성공 / 스킬 레벨업 / 오프라인 보상 수령
- 250 ms 디바운스 큐로 직렬 처리. 같은 프레임 다중 트리거에서 중복·유실 없음
- v1 → v2 이관 경로 제공. 기존 개발 계정 진입 실패 0
- 저장 상태 항목 명칭은 GDD `저장 상태와 화면 대응` 표를 단일 출처로 한다 (영문 식별자, 프레스티지 예약 필드 5개 포함)

**검증 가능성**
- 단위 테스트 5영역: 가챠(확률표 합 100% · 100회 천장 보장) / 강화 비용 / 오프라인 계산 / 저장 이관 / 큰 수 포맷
- 밸런스 시뮬레이터가 게임과 **같은 상수 출처**를 읽어야 한다 (상수 복사 금지 — GDD "핵심 수치 규약")

**오프라인 플레이**
- 네트워크 없이 전투·성장·로컬 저장 전부 동작. 광고만 불가
- 광고 없이도 챕터 5 보스까지 완주 가능 (E9-21)

**입력**
- 터치 단일. 조이스틱·가속도계·제스처 없음. Android 뒤로가기 처리 필요
- Input System은 As-Is Both 모드로 남아 있으나 요구는 UI 터치뿐 → 정리 대상 (Step 4)

### Complexity Drivers

**높은 복잡도**

| # | 동인 | 장르 표준 해법 | SoloHero에 남는 결정 |
|---|---|---|---|
| 1 | 저장 파이프라인 + v1→v2 이관 | 원격 + 로컬 백업 + 디바운스는 As-Is에 이미 있음. 이관은 `dataVersion` 스위치 + 단계별 변환 함수가 관습 | **의미가 바뀐 필드 3종의 값 변환 규칙** — 강화 레벨(가산·최대 50 → 승산·무상한: 레벨 30 단순 복사 시 ×86), SPD(이동 속도 → 공격 속도), chapter/stage(챕터당 5 → 10). 노드 명칭 변환(`equippedWeapon` → `equippedSword`, `equippedHelmet` → `equippedHelm`, `gachaPullCount` → `pityCount`)도 포함. GDD A-9(v1 계정 소수, 손실 감수 가능)가 보수적 규칙을 허용 |
| 2 | 무상한 수치 | `double` + K/M/B/T/aa/ab 접미어 포맷터. 방치형 사실상 표준 (BreakInfinity 계열은 1e308 초과 시에만 필요) | double 채택 확인만 (Step 4). `long`은 9.2×10¹⁸에서 넘치므로 부적합 |
| 3 | 2시간 무생성 전투 루프 | 오브젝트 풀 + 고정 상한 스폰 | 없음 |

**장르 표준을 그대로 쓰는 개념** — 관습적 구현이 있으므로 아키텍처는 관습을 따른다.

| 개념 | 방치형 RPG 관습 | SoloHero 차이 |
|---|---|---|
| 장비 가챠 | 등급별 확률표 + N회 천장 + 슬롯 균등 + 중복 환급 | 없음 |
| 최고 도달 / 파밍 스테이지 분리 + 보스 재도전 | `highestStage`·`currentStage` 2필드 + "보스 도전" 버튼 | `retreatMode` 플래그 1개 |
| 오프라인 시간 조작 방어 | 로컬 UTC + 음수·상한 초과 클램프 | 없음 |
| 스킬 자동 발동 + 수동 탭 | 자동 ON 기본 + 버튼 탭 즉시 발동 | 스킬별 조건 테이블 + 0.4 s 순차 간격 |
| 비율 감쇄 방어력 | `DEF / (DEF + K)` | K = `8 × 적 공격력` (공식 1줄) |
| 재사용 풀 전투 | 적·텍스트·VFX 풀링 | 없음 |

**고유 개념: 없음.** 전 시스템이 장르 관습 위에 있다.

### Technical Risks

| # | 리스크 | 출처 | 아키텍처 함의 |
|---|---|---|---|
| R1 | 밸런스 상수 조합 미검증 (Q-2, V-1~V-7) | GDD 미해결 질문 | 상수를 데이터로 분리해 시뮬레이터와 게임이 공유. 상수 변경이 코드 변경이 되면 안 됨 |
| R2 | 중급 기기 60 FPS (A-6) | GDD Assumptions | 파티클 예산·드로우콜 예산을 초기 구조에서 강제. 이펙트 축소 모드가 후처리가 아니라 1급 경로 |
| R3 | 무료 에셋·한글 픽셀 폰트 미확보 (A-4, A-5, Q-3, Q-4) | GDD Assumptions | 플레이스홀더 파이프라인이 실제 에셋과 같은 인터페이스를 써야 E8 교체가 코드 무변경 |
| R4 | Firebase 네트워크 실패 시 진입 차단 | GDD Dependencies | 로컬 백업 폴백이 부트 시퀀스의 정식 분기 |
| R5 | AdMob 로드 실패·재고 없음 | GDD Dependencies | 모든 광고 경로에 일반 대안. 광고 서비스는 실패를 정상 상태로 반환 |
| R6 | 이월 결정 4건 미해결 (C-004 Addressables, C-007 JSON vs SO, 큰 수 타입, 이관) | decision-log | Step 4에서 결정. 복잡도 동인 1·2와 직접 연결 |
| R7 | 상수 단일 출처가 `gdd.md` Number Balancing 절 (`05_balance_data.md` 아님) | decision-log 구조 편차 | 코드가 읽는 상수 파일과 GDD 상수표의 동기화 방식을 아키텍처가 정의 |
| R8 | 생존 스크립트 15개의 에디터 컴파일 미검증 | As-Is 표 | E1-03 착수 시 최초 확인. 아키텍처 결정에 영향 없음 |
| R9 | **가챠 방식 변경이 GDD와 불일치** — 정규분포 → 확률표 + 천장. As-Is `GachaSystem.cs`(Box-Muller)는 계승 대상에서 폐기로 전환 | 본 세션 사용자 결정 | **해소 (2026-09-20).** GDD·epics·decision-log 역반영 완료 — D-049~D-052. 확률표 55/33/10/2, Legendary 획득 시 리셋 |

---

## Engine & Framework

### Selected Engine

**Unity 2022.3.62f3** — 고정. **2022.3.63f1 이후 패치는 Extended LTS(xLTS)로 Unity Industry/Enterprise 라이선스 전용**이라 Personal/Pro에서는 62f3이 받을 수 있는 마지막 버전이다 (2026-09-20 Unity Hub 설치 시도에서 라이선스 오류로 확인). 초안의 "76f1 상향" 계획은 이 게이트를 놓친 것으로, 철회한다.

**Rationale:** 엔진은 선택이 아니라 제약이다. GDD Dependencies가 Unity 2022.3 고정을 명시하고, Firebase·AdMob·EDM4U·Gradle 템플릿·JDK 11 빌드 파이프라인이 전부 이 버전에서 검증되어 있다. 16 KB 페이지 크기 엔진 지원은 2022.3.56f1부터 포함되어 62f3에 이미 있다. 타깃 API 36은 에디터 버전이 아니라 **Android SDK Platform 36 설치**로 충족한다.

**검토 후 기각한 대안**

| 대안 | 기각 이유 |
|---|---|
| 2022.3.63f1 ~ 76f1 패치 상향 | **불가.** Extended LTS — Industry/Enterprise 라이선스 전용. Hub 설치 시 "License error: This build of Unity 2022 is part of an Extended LTS release" |
| Unity 6.0 LTS | 보안 지원 2026-10-16 종료. 상향 직후 EOL |
| Unity 6.3 LTS | URP 14→17, JDK 11→17, Gradle 7→8 동시 전환. Firebase/AdMob/EDM4U/커스텀 Gradle 템플릿 전부 재검증. 1인 개발에서 v1.0 출시 전 감당할 이유가 없음. **v1.0 출시 후 재검토** — 2022.3에 더 이상 패치가 없으므로 그때는 유일한 상향 경로 |

**외부 제약 (2026-09-20 확인)**

| 제약 | 내용 | 대응 |
|---|---|---|
| Google Play 타깃 API | 2026-08-31부터 신규 앱은 API 36(Android 16) 필수. 연장 신청 시 2026-11-01 | SoloHero는 신규 앱 → 출시 시 타깃 API 36 |
| 16 KB 페이지 크기 | API 35+ 타깃 앱은 16 KB 정렬 필수. 2022.3.56f1+ 엔진 지원. 단, 2022.3 내장 AGP 7.4.2 / Gradle 7.5.1로 서드파티 `.so` 정렬 실패 사례 보고됨 | E1-09 실빌드 검증. AGP 8.5+ 상향이 필요하면 Gradle 8.7+ → JDK 17 → CLAUDE.md의 "JDK 11 고정" 제약 재검토 (D13) |

### Project Initialization

스타터 템플릿 없음. 기존 레포에서 계속한다 — 씬 0개·Addressables 엔트리 0건·생존 스크립트 15개 상태가 사실상 백지다. 신규 프로젝트 생성은 Firebase/AdMob/Gradle 설정을 다시 하는 비용만 만든다.

### Engine-Provided Architecture

엔진과 이미 설치된 패키지가 결정해 주는 것. 아키텍처는 이를 다시 결정하지 않는다.

| Component | Solution | Notes |
|---|---|---|
| 렌더링 | URP 14.0.12 + `Renderer2D` | 전환 완료 (As-Is 표). SRP Batcher 기본 활성 |
| 픽셀 퍼펙트 | `com.unity.feature.2d` 2.0.1 → 2D Pixel Perfect 패키지 | 기준 270×480, PPU 32 (GDD D-031). 설정값은 Step 4 |
| 2D 파이프라인 | 2D Feature Set: Sprite Atlas, 2D Animation, Tilemap, PSD Importer (Aseprite Importer `com.unity.2d.aseprite`는 필요 시 별도 추가) | 무료 픽셀아트 에셋 임포트 경로 확보 |
| UI | uGUI 1.0.0 + TextMeshPro 3.0.7 + CanvasScaler | 세로 1080×1920 기준. SafeAreaAdjuster 계승 |
| 오디오 | Unity 네이티브 AudioSource + AudioMixer | BGM/SFX 개별 음소거는 Mixer 그룹으로 |
| 비동기 | UniTask (Git URL) | 2022.3에는 `Awaitable`이 없음. 코루틴 대신 UniTask (CLAUDE.md 규약) |
| 트위닝 | DOTween | 계승. `modules.physics` 의존으로 물리 모듈 제거 불가 |
| 테스트 | Unity Test Framework 1.1.33 (NUnit) | EditMode 테스트 5영역의 실행 기반 |
| 빌드 | Gradle (내장 AGP 7.4.2 / Gradle 7.5.1) + 커스텀 템플릿 + `BuildAutomator.Build` | GitHub Actions(`.github/scripts/unity-build.sh`)가 호출. Jenkins 휴면 |
| Android SDK/JDK | Unity Hub 번들 OpenJDK 11, minSdk 24 | 타깃 API 36 + 16 KB 정렬은 **E1-09 실빌드로 검증** |
| 백엔드 SDK | Firebase (Auth/RTDB/Analytics) via EDM4U, Google Mobile Ads 25.0.0 | 계승. 16 KB 정렬된 최신 `.so`인지 확인 대상 |
| 직렬화 | JsonUtility (As-Is `PlayerData` DTO), Newtonsoft.Json 3.2.1 설치됨 | 어느 쪽을 저장 스키마 v2에 쓸지는 Step 4 |
| 씬 관리 | `SceneManager` | 씬 구성(부트 + 게임 vs 단일)은 Step 4 |
| 입력 | Input System 1.14.2 (Both 모드) + EventSystem | 요구는 UI 터치뿐. 정리 방향은 Step 4 |

### AI Development Tools

| MCP | Repo / Endpoint | 버전·상태 (2026-09-20) | 요구 | 용도 | 채택 |
|---|---|---|---|---|---|
| MCP for Unity | `CoplayDev/unity-mcp` | v10.0.0 (2026-06-30), 14.3k★, MIT, **2021.3 LTS ~ 6.x** | Python 3.10+ (`uv`), Unity Package Manager git URL | 씬·GameObject·컴포넌트·프리팹·스크립트를 AI가 직접 조작. `MenuSetupWizard`류 에디터 스크립트 의존을 줄임 | **채택** |
| Context7 | `upstash/context7` | 활성 | MCP 클라이언트 설정 | Unity 2022.3 API 문서를 학습 데이터 대신 현재 문서에서 조회 — 2022.3 API 제약(`Awaitable` 없음 등) 확인용 | **채택** |
| Higgsfield MCP | `https://mcp.higgsfield.ai/mcp` (호스팅) | 활성. 이미지 모델 Nano Banana Pro · GPT Image 2 · Seedream 5.0 · Flux · Soul 2.0(캐릭터 일관성), 4K까지 | Higgsfield 계정 + 크레딧. **MCP 경유 생성은 플랜과 무관하게 항상 크레딧 차감** | 자체 에셋 생성 — **배경 패럴랙스·장비 아이콘·UI 프레임·스토어 자산에 적합.** 캐릭터 애니메이션 시트(프레임 일관성·32 px 그리드·투명 배경·제한 팔레트)는 부적합 → 무료 에셋팩 유지(GDD A-5). 생성물 상업적 사용권은 Higgsfield ToS + 하위 모델 약관 확인 후 GDD Q-7 라이선스 관리표에 등재 | **선택 (실험)** |

`CoderGamester/mcp-unity`는 Unity 6+ 전용이라 제외. Unity 공식 MCP는 Unity AI 유료 구독 + Unity 6 필요라 제외.

**에셋 계약 원칙.** 에셋의 출처(무료팩 / 생성 AI / 수작업)와 무관하게 임포트 경로는 하나다 — 투명 PNG, 정확한 프레임 크기, PPU 32, 시트 배열·명명 규칙 고정. 계약은 D12에서 정의한다.

설치 절차는 Development Environment 절(Step 9)에 기록한다.

### Remaining Architectural Decisions

엔진이 정해 주지 않아 Step 4에서 명시적으로 결정할 것. 이월 항목 4건을 포함한다.

| # | 결정 | 연결 |
|---|---|---|
| D1 | 에셋 전달: Addressables + Firebase Hosting 유지 vs 로컬 번들 | C-004 |
| D2 | 데이터 정의: ScriptableObject vs JSON — 상수 단일 출처를 시뮬레이터와 어떻게 공유하는가 | C-007, R1, R7 |
| D3 | 큰 수 타입: `double` + 접미어 포맷터 | 이월 항목, 복잡도 동인 2 |
| D4 | 저장 스키마 v2 노드 구조 + v1→v2 값 변환 규칙 + 직렬화기(JsonUtility vs Newtonsoft) | 이월 항목, 복잡도 동인 1 |
| D5 | 씬 구성: Boot + Game 2씬 vs 단일 씬 | 부트 시퀀스 E1-04 |
| D6 | 서비스 접근: `SingletonMB` 계승 vs 서비스 로케이터 | As-Is `Core/SingletonMB.cs` |
| D7 | 시스템 간 통신: C# event vs ScriptableObject 이벤트 채널 | UI MVP 계승 여부 |
| D8 | 어셈블리 정의 분할 여부 | EditMode 테스트가 참조할 어셈블리 필요 |
| D9 | 입력 정리: Input System 패키지 유지(UI 모듈만) vs 레거시 전용 | Both 모드 정리 |
| D10 | 물리 모듈: `physics2d` 미사용 시 제거 여부 (`physics`는 DOTween 때문에 유지) | 전투는 1D 거리 비교, 충돌 판정 없음 |
| D11 | 오브젝트 풀 구현: 자체 `ObjectPool<T>` vs `UnityEngine.Pool` (2021.1+) | 복잡도 동인 3 |
| D12 | 에셋 계약 + 픽셀 퍼펙트 카메라 설정 + 스프라이트 임포트 프리셋 | E8-16, 에셋 출처 무관 단일 임포트 경로 |
| D13 | Android | **타깃 API 36, 에디터 62f3 고정, IL2CPP ARMv7+ARM64. 결정 A — JDK 11 · AGP 7.4.2 · Gradle 7.5.1 유지** | 2022.3.62f3 | 2026-09-21 E1-09 스파이크: 로컬 AAB의 `.so` 18개 전부 LOAD align 0x4000, zipalign -P 16 통과 (`tools/spike/Check16Kb.ps1` exit 0). Play Console·실기기 검증은 스토리 1-09 T9·T10에서 계속 |

---

## Architectural Decisions

기조는 **장르 표준 모방**이다. 각 결정은 방치형 RPG 관습을 기본값으로 삼고, SoloHero 고유 사정(As-Is 계승, 1인 개발, 2022.3 제약)이 있을 때만 그 이유를 적는다. 검증 일자 2026-09-20.

### Decision Summary

| # | 분류 | 결정 | 버전 | 근거 (한 줄) |
|---|---|---|---|---|
| D1 | 에셋 전달 | **직접 참조 (프리팹·SO). Addressables 패키지 제거** | — | 엔트리 0건, 콘텐츠 규모가 AAB 한도에 한참 미달, 콜드 스타트 8초 예산. C-004 해소 |
| D2 | 데이터 정의 | **ScriptableObject 전면 + `BalanceConfig` SO 1개**. `StreamingAssets/JSON`·`JsonDataManager` 삭제 | — | 상수 단일 출처. 시뮬레이터가 같은 SO를 읽어 동기화 문제 원천 제거. C-007 해소 |
| D3 | 큰 수 타입 | **`double`** (골드·젬·HP·ATK·DEF·EXP), `int` (레벨·카운트) | — | Firebase RTDB가 숫자를 IEEE 754 double로 저장 — `long`도 2⁵³ 이상은 정밀도를 잃는다 |
| D4 | 저장 | **새 노드 `users/{uid}/v2` + Newtonsoft 직렬화 + v1 값 변환 규칙** | Newtonsoft 3.2.1 | 롤백 안전(v1 원본 보존), 컬렉션 직렬화, 누락 필드 기본값 |
| D5 | 씬 | **Boot + Game 2씬** | — | 초기화 순서를 게임 오브젝트와 분리. 장르 표준 |
| D6 | 서비스 접근 | **명시 등록 서비스 로케이터.** `SingletonMB` 삭제. 서비스는 순수 C# 클래스 | — | 자동 생성 싱글턴은 초기화 순서 버그를 숨김. 순수 클래스가 EditMode 테스트 전제 |
| D7 | 통신 | **서비스의 C# `event Action<T>` + UI Presenter 구독** | — | As-Is `GoldChanged` 방식 계승. SO 이벤트 채널은 에셋만 늘림 |
| D8 | 어셈블리 | **4개: `SoloHero.Core`(Unity-free) / `SoloHero.Game` / `SoloHero.Editor` / `SoloHero.Tests.EditMode`** | — | Core가 UnityEngine을 참조하지 않아야 테스트·시뮬레이터가 빠르다 |
| D9 | 입력 | **Input System 패키지 제거, 레거시 전용** (`StandaloneInputModule`, `KeyCode.Escape`) | — | 요구는 UI 터치 + 뒤로가기뿐. Both 모드는 EventSystem 2벌 |
| D10 | 물리 | **`physics`·`physics2d` 모듈 유지, 게임플레이에서 Collider/Rigidbody 사용 금지** | — | `physics2d`는 Tilemap 의존, `physics`는 DOTween 의존. 미사용 시 런타임 비용 없음. 판정은 X축 거리 비교 |
| D11 | 풀 | **`UnityEngine.Pool.ObjectPool<T>` 내장 + 프리팹 래퍼.** 적·데미지 텍스트·VFX·AudioSource 4종, Boot 프리웜 | 2022.3 내장 | 자체 구현 이유 없음 |
| D12 | 에셋 계약 | **투명 PNG / 32×32·24×32·64×64 / 가로 스트립 / `{entity}_{clip}_{frames}.png` / 히트 프레임 3번째.** 임포트: Point·무압축·PPU 32·밉맵 없음. Sprite Atlas 4종. Pixel Perfect Camera 270×480 | 2D Pixel Perfect (2D Feature 2.0.1) | 출처(무료팩·생성 AI·수작업) 무관 단일 임포트 경로 |
| D13 | Android | **타깃 API 36, 에디터 62f3 고정(63f1+는 xLTS), IL2CPP ARMv7+ARM64. 결정 A — JDK 11 · AGP 7.4.2 · Gradle 7.5.1 유지** | 2022.3.62f3 | E1-09 스파이크(2026-09-21): `.so` 18개 LOAD align 0x4000, zipalign -P 16 통과, 16 KB 에뮬레이터 런타임 통과, GitHub Actions 성공. Play Console 검증은 E9-18 |
| D14 | 상태 관리 | **순수 C# 상태 머신** (히어로 5상태, 스테이지 러너). Animator는 시각 재생만 | — | `StateMachineBehaviour`(3D 시절) 폐기. 상태를 테스트 가능한 코드가 소유 |
| D15 | 오디오 | **AudioMixer BGM/SFX 2그룹 + `AudioService`**, SFX AudioSource 풀 4개, 음소거 = −80 dB | 내장 | 엔진 제공. 설정 저장 항목 `bgmMuted`·`sfxMuted`와 1:1 |

**확인된 라이브러리 버전:** UniTask v2.5.11 (2025-05-19, 2022.3 지원 명시) — Git URL을 이 태그로 고정한다. Newtonsoft `com.unity.nuget.newtonsoft-json` 3.2.1 (설치됨). DOTween은 계승 (Utility Panel에서 asmdef 생성 필요).

### State Management

**Approach:** 순수 C# 상태 머신 (D14) + 서비스 로케이터 (D6) + C# 이벤트 (D7)

- **게임 흐름:** `Boot` 씬의 `BootSequence`가 순차 실행 — Firebase 초기화 → 익명 인증(실패 시 `local`) → 로드(원격 실패 시 로컬 백업) → 이관(`dataVersion < 2`) → 오프라인 계산 → `Game` 씬 로드. 어떤 단계 실패도 앱을 멈추지 않는다 (E1-04).
- **런타임 상태의 소유자:** `PlayerState`(저장 DTO의 런타임 사본) 하나. 서비스는 `PlayerState`를 변경하고 이벤트를 발화한다. UI는 절대 `PlayerState`를 직접 쓰지 않는다.
- **전투 상태:** `StageRunner`(Running / Clearing / BossIntro / BossTimer / Failed / Retreat)와 `HeroBrain`(Advance / Engage / Hit / Skill / Dead)은 `SoloHero.Core`의 순수 클래스가 소유하고, `Game` 어셈블리의 MonoBehaviour가 매 프레임 `Tick(dt)`를 호출한다. Animator 파라미터는 상태 변화 이벤트를 받아 갱신될 뿐 상태를 결정하지 않는다.
- **동시 판정 규칙(GDD):** 히어로 사망 vs 마지막 적 사망 → 클리어 우선 / 타이머 종료 vs 보스 사망 → 보스 사망 우선. `StageRunner.Tick` 안에서 적 사망 처리를 히어로 사망 처리보다 먼저 실행하는 순서로 보장한다.

### Data Persistence

**Save System:** Firebase RTDB 원격 + PlayerPrefs 로컬 백업 + 250 ms 디바운스 큐 (As-Is `SaveManager` 계승) — 노드·직렬화·이관은 D4

**저장 노드**

```
users/{uid}            ← v1 (As-Is). 읽기 전용, 이관 원본. 삭제하지 않는다
users/{uid}/v2         ← v2. 이후 모든 읽기/쓰기
```

**DTO 규약**
- `SaveDataV2` — 평면 필드 + `List<string> ownedEquipment` + `List<bool> chapterFirstClearFlags`. 필드명은 GDD `저장 상태와 화면 대응` 표의 영문 식별자를 **그대로** 쓴다 (`gold`, `gem`, `highestStage`, `farmingStage`, `retreatMode`, `heroLevel`, `heroExp`, `upgradeHp/Atk/Def/Spd`, `equippedSword/Helm/Armor/Boots`, `pityCount`, `totalPullCount`, `skillLevel1~3`, `lastQuitTimeUtc`, `adCountA1~A3`, `adCountResetDate`, `goldBoosterEndUtc`, `tutorialStep`, `bgmMuted`, `sfxMuted`, `lowEffectMode`, `fps30Mode`, `rebirthCount`, `soul`, `permGoldLevel`, `permAtkLevel`, `permOfflineLevel`, `dataVersion = 2`).
- 수치형은 D3에 따라 `double`/`int`. 스테이지는 전역 인덱스 `g`(int) 하나로 저장하고 (챕터, 스테이지)는 표시 시 계산한다.
- `PlayerDataV1`(As-Is `PlayerData.cs` 개명)은 JsonUtility로만 읽고 쓰지 않는다.
- Newtonsoft: `MissingMemberHandling.Ignore`, 누락 필드는 C# 기본값. IL2CPP 스트리핑 대비 `link.xml`에 `SaveDataV2` 보존.

**v1 → v2 값 변환 규칙** (GDD A-9: v1 계정은 소수, 손실 감수 가능 → 보수적 규칙)

| v1 | v2 | 규칙 |
|---|---|---|
| `gold` | `gold` | 그대로 + 아래 환급액 |
| `upgradeHp/Atk/Def/SpdLevel` (가산·최대 50) | `upgradeHp/Atk/Def/Spd = 0` | **v1 비용 공식 `Σ baseCost × (i+1), i=0..level−1`로 누적 지출을 역산해 `gold`에 환급.** 승산 레벨로 직접 복사하면 ×86배 같은 붕괴가 생긴다 |
| `chapter`, `stageNumber` (챕터당 5) | `highestStage = farmingStage = (chapter−1)×5 + stageNumber` | g 인덱스 보존, 최소 1. `retreatMode = false` |
| `equippedWeapon/Helmet/Armor/Boots` | `equippedSword/Helm/Armor/Boots` | 명칭 매핑. id는 `EquipmentData.id` 기준으로 재조회, 없으면 빈 문자열 |
| `ownedEquipmentCsv` (파이프 구분) | `ownedEquipment` | 분할 → 유효 id만 보존 |
| `gachaPullCount` | `pityCount`, `totalPullCount` | 둘 다 같은 값 |
| `lastQuitTimeUtc` | `lastQuitTimeUtc` | 그대로 (이관 직후 오프라인 계산은 정상 수행) |
| `stageKillsCurrent` | — | 폐기 (전투 상태는 저장하지 않는다) |
| (없음) | 나머지 전 필드 | 기본값 |

이관은 `SoloHero.Core`의 순수 함수 `MigrationV1ToV2.Convert(PlayerDataV1) → SaveDataV2`이며 단위 테스트 대상(E9-06 "저장 이관").

**저장 트리거 7종·디바운스**는 As-Is 구조를 계승한다: `SaveService.RequestSave()`(250 ms 디바운스, 마지막 상태만) / `FlushAsync()`(일시정지·종료 시 즉시). 로컬 백업은 항상 먼저 쓰고 원격은 실패해도 삼킨다.

### Asset Management

**Loading Strategy:** 직접 참조 (D1). 모든 콘텐츠는 SO·프리팹 참조로 빌드에 포함되고 씬 로드 시 함께 올라온다.

- **콘텐츠 SO:** `EquipmentData`×16 (As-Is 계승, `weight` 필드 삭제 — 가챠 확률은 확률표 SO로 이동), `EnemyData`×8, `BossData`×3, `ChapterTheme`×5 (배경 4레이어 + 적 구성 + BGM), `SkillData`×3, `GachaTable`×1 (등급 4행 확률 + 천장), `BalanceConfig`×1 (GDD 상수표 전체, 필드명 = 상수명).
- **런타임 생성 0:** 적·데미지 텍스트·VFX·AudioSource는 D11 풀에서만 공급. 풀 크기는 `BalanceConfig`(`SPAWN_MAX_ALIVE` 등)에서 계산해 Boot에서 프리웜.
- **에셋 계약(D12)**을 지키는 파일은 출처와 무관하게 같은 임포트 프리셋과 아틀라스 규칙을 탄다. 플레이스홀더(단색 박스)도 같은 계약을 따르므로 E8 교체는 에셋 파일 교체만으로 끝난다.
- **Addressables 제거 절차:** 패키지 제거 → `AddressableAssetsData` 폴더 삭제 → `BuildAutomator`에서 관련 호출 제거. 원격 패치가 필요해지는 시점(v1.1+)에 재도입한다.

### Data Definition & Balance Source

- **단일 출처:** `Assets/Data/Config/BalanceConfig.asset`. GDD `Number Balancing` 상수표의 각 행이 같은 이름의 필드가 된다 (`ENEMY_HP_GROWTH`, `UPG_STAT_MULT`, …). 코드는 매직 넘버를 쓰지 않고 이 SO만 읽는다.
- **공식은 코드:** `Formulas` 정적 클래스(`SoloHero.Core`)가 `EnemyHp(g)`, `StageGold(g)`, `UpgradeCost(lane, level)`, `HitDamage(enemyAtk, def)` 등을 제공하며 `BalanceConfig` 값을 인자로 받는다. 시뮬레이터·테스트·게임이 같은 함수를 호출한다.
- **시뮬레이터:** `SoloHero.Editor`의 메뉴 `Tools > Balance > Run Simulation` — `BalanceConfig`를 읽어 `Formulas`로 스테이지별 누적 시간·골드·강화 추이를 CSV로 출력. Python 별도 스크립트는 두지 않는다 (R7 해소).
- **GDD ↔ SO 동기화:** 상수 변경은 GDD 표를 먼저 고치고 SO를 따라 고친다. 이름이 같으므로 diff로 대조한다.

### Architecture Decision Records

**ADR-1 Addressables 제거 (D1).** 3D 시절 계획은 Firebase Hosting 원격 패치였다. 2D 리빌드에서 콘텐츠 규모(픽셀아트 수십 개 파일)와 콜드 스타트 8초 예산을 고려하면 카탈로그 페치·패치 UI·호스팅 운영은 비용만 있고 이득이 없다. 엔트리 0건이므로 제거 비용도 0. 재도입 조건: 스토어 릴리스 없이 콘텐츠를 갱신해야 하는 운영 요구가 실제로 생길 때.

**ADR-2 ScriptableObject 단일 출처 + C# 시뮬레이터 (D2).** 대안 "SO + JSON 상수 공유 + Python 시뮬레이터"는 상수를 두 벌 유지하게 만들고, decision-log가 이미 "상수 출처가 어디인지" 편차를 기록한 상태였다. 시뮬레이터를 Unity 에디터 안 C#로 두면 게임과 **같은 함수·같은 SO**를 쓰므로 편차가 구조적으로 불가능해진다. 대가는 Python 대비 반복 속도 저하이며, EditMode에서 도는 순수 C#이라 실질 차이는 작다.

**ADR-3 double (D3).** `long` 유지 논거는 "정수 재화의 정밀성"이었으나 RTDB가 double로 저장하는 이상 허구다. 강화 승산·무상한 구조에서 값이 1e18을 넘는 시점은 MVP 범위(g ≤ 50) 밖이지만, 타입을 나중에 바꾸면 저장 스키마 이관이 한 번 더 필요하다. 지금 double로 확정한다. 표시 정밀도는 포맷터가 소수 1자리로 잘라 흡수한다.

**ADR-4 v2 노드 분리 + 강화 골드 환급 (D4).** 같은 노드 덮어쓰기는 이관 버그 시 원본을 잃는다. 강화 레벨 직접 복사는 의미가 바뀐 필드(가산→승산)라 불가능하고, "레벨 유지"라는 E1-06 문구는 값 의미가 같다는 전제였다. 환급은 플레이어가 쓴 골드를 돌려주므로 A-9의 "손실 감수" 범위 안에서 가장 보수적이다. epics의 E1-06 완료 기준은 "진행도·장비·**강화 지출 골드**를 잃지 않고 진입"으로 갱신 대상 (R9와 함께 GDD 역반영).

**ADR-5 순수 C# Core 어셈블리 (D6·D8·D14).** 저장 신뢰성(G-3)·가챠 검증·이관·큰 수 포맷의 단위 테스트 5영역은 전부 UnityEngine 없이 돌 수 있는 로직이다. Core를 Unity-free로 두면 EditMode 테스트가 도메인 리로드 없이 돌고, 시뮬레이터도 같은 코드를 쓴다. MonoBehaviour는 "씬에 존재해야 하는 것"(전투 뷰, UI, 오디오, 부트)에만 쓴다.

**ADR-6 Input System 제거 (D9).** GDD 입력 요구는 UI 탭·뒤로가기뿐이다. Both 모드는 두 EventSystem 모듈과 경고를 남기고, 새 Input System의 이점(액션 맵·리바인딩)은 하나도 쓰이지 않는다. 제거하면 `Active Input Handling = Input Manager (Old)`로 되돌리고 `StandaloneInputModule`을 쓴다.

**ADR-7 물리 모듈 유지 (D10).** 제거 시도는 Tilemap(`physics2d`)·DOTween(`physics`) 의존으로 컴파일 실패 위험이 있고, 미사용 모듈은 런타임 비용이 없다. 대신 규약으로 막는다 — 게임플레이 어셈블리에서 `Collider2D`/`Rigidbody2D` 사용 금지, 판정은 `HeroBrain`/`EnemyBrain`의 X축 거리 비교.

---

## Cross-cutting Concerns

전 시스템이 따르는 규칙이다. 어느 에이전트가 어느 스토리를 구현하든 이 절과 다르게 하면 안 된다. 코드 안의 텍스트(식별자·로그·주석·UI 문자열 키)는 **영문만** 쓴다 — 한글은 일부 환경에서 인코딩이 깨진다 (프로젝트 규약). 플레이어에게 보이는 한국어 문자열은 `Strings` 테이블(E7-17)에만 둔다.

### Error Handling

**Strategy:** 도메인 실패는 **Result 객체**, 외부 I/O 실패는 **경계에서 try-catch + 폴백**, 게임을 멈추는 에러는 없다.

| 층 | 규칙 | 예 |
|---|---|---|
| `SoloHero.Core` 서비스 | 예상되는 실패(골드 부족, 최대 레벨, 쿨다운 중)는 **예외를 던지지 않고** `Result`로 반환한다. 예외는 프로그래밍 오류(null, 잘못된 인덱스)에만 쓴다 | `UpgradeService.TryUpgrade(lane)` → `Result.Fail(FailReason.NotEnoughGold)` |
| 외부 경계 (Firebase·AdMob·PlayerPrefs·파일) | 호출 지점에서 try-catch. 실패는 `Log.Warn`으로 남기고 **항상 폴백 경로**로 진행한다 (원격 실패 → 로컬, 광고 실패 → 일반 수령). 재시도는 부트 방어 UI의 사용자 버튼으로만 | As-Is `SaveManager.LoadAsync` 방식 계승 |
| UI Presenter | `Result`의 `FailReason`을 토스트 문자열 키로 매핑한다. UI는 예외를 잡지 않는다 | `NotEnoughGold` → `toast.not_enough_gold` |
| 부트 | 단계별 실패 분기가 정식 경로다 (E1-04). 인증 실패 → `local` 모드, 로드 실패 → 로컬 백업, 로컬도 없음 → 신규 생성 + 방어 UI로 상황 설명 | `BootSequence` |
| 네이티브 콜백 | Firebase·AdMob 콜백은 스레드가 다를 수 있다. **`MainThreadDispatcher`(As-Is 계승)를 거친 뒤** 이벤트를 발화한다 | `AdService` |

**에러 등급**

- **Recoverable** — 폴백이 있는 실패. `Log.Warn`. 플레이어에게는 필요할 때만 토스트.
- **Degraded** — 기능 하나가 세션 동안 꺼짐 (광고 SDK 초기화 실패 등). `Log.Error` 1회 + 해당 버튼 비활성. 게임은 계속.
- **Fatal** — 발생하면 안 되는 상태 (저장 DTO null). `Log.Error` + 방어 UI. **그래도 `Application.Quit`이나 `throw`로 게임을 끝내지 않는다.** 로컬 백업이나 신규 데이터로 계속한다.

**Example**

```csharp
// SoloHero.Core — no UnityEngine
public readonly struct Result
{
    public readonly bool Ok;
    public readonly FailReason Reason;
    private Result(bool ok, FailReason reason) { Ok = ok; Reason = reason; }
    public static readonly Result Success = new Result(true, FailReason.None);
    public static Result Fail(FailReason reason) => new Result(false, reason);
}

public enum FailReason { None, NotEnoughGold, NotEnoughGem, MaxLevel, OnCooldown, Locked, Busy }

public sealed class UpgradeService
{
    public event Action<UpgradeLane, int> LaneUpgraded;

    public Result TryUpgrade(UpgradeLane lane)
    {
        int level = _state.GetUpgradeLevel(lane);
        if (lane == UpgradeLane.Spd && level >= _cfg.UPG_MAX_LEVEL_SPD) return Result.Fail(FailReason.MaxLevel);
        double cost = Formulas.UpgradeCost(_cfg, lane, level);
        if (_state.Gold < cost) return Result.Fail(FailReason.NotEnoughGold);

        _state.Gold -= cost;
        _state.SetUpgradeLevel(lane, level + 1);
        LaneUpgraded?.Invoke(lane, level + 1);
        _save.RequestSave();
        return Result.Success;
    }
}
```

```csharp
// SoloHero.Game — I/O boundary
public async UniTask<SaveDataV2> LoadAsync()
{
    if (_isLocalMode) return LoadLocalBackup();
    try
    {
        var snapshot = await _userRef.Child("v2").GetValueAsync();
        if (!snapshot.Exists) return await TryMigrateFromV1Async();
        var data = JsonConvert.DeserializeObject<SaveDataV2>(snapshot.GetRawJsonValue(), _jsonSettings);
        SaveLocalBackup(data);
        return data ?? new SaveDataV2();
    }
    catch (Exception e)
    {
        Log.Warn(LogTag.Save, $"remote load failed, using local backup: {e.Message}");
        return LoadLocalBackup();
    }
}
```

### Logging

**Format:** `[Tag] message key=value key=value` — 태그는 `LogTag` enum, 메시지는 영문 소문자 문장, 값은 `key=value`
**Destination:** Unity Console (에디터·개발 빌드). 릴리스 빌드는 `Error`·`Warn`만 남고 `Info`·`Debug`는 컴파일 제거

| 레벨 | 언제 | 릴리스 빌드 |
|---|---|---|
| `Error` | Degraded·Fatal. 세션당 반복 금지(같은 원인은 1회) | 남김 |
| `Warn` | Recoverable 폴백 발동 | 남김 |
| `Info` | 부트 단계 완료, 씬 전환, 저장 완료, 이관 수행, 스테이지 클리어/보스 결과 | 제거 |
| `Debug` | 가챠 샘플·데미지 계산·스폰 등 진단 | 제거 |

**금지:** `Tick`/`Update`/풀 루프 안에서의 로깅(프레임당 호출 경로), 문자열 보간을 `Debug` 레벨에서 무조건 수행하는 것(`[Conditional]`로 호출 자체를 제거한다), `Debug.Log` 직접 호출(반드시 `Log` 래퍼).

**Example**

```csharp
// SoloHero.Core — Unity-free facade; Game assembly installs the sink at boot
public enum LogTag { Boot, Save, Migrate, Combat, Stage, Growth, Gacha, Economy, Offline, Ad, UI, Audio, Pool }

public static class Log
{
    public static ILogSink Sink = NullSink.Instance;

    public static void Error(LogTag tag, string msg) => Sink.Write(LogLevel.Error, tag, msg);
    public static void Warn (LogTag tag, string msg) => Sink.Write(LogLevel.Warn,  tag, msg);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Info (LogTag tag, string msg) => Sink.Write(LogLevel.Info,  tag, msg);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Debug(LogTag tag, string msg) => Sink.Write(LogLevel.Debug, tag, msg);
}

// usage
Log.Info(LogTag.Stage, $"stage cleared g={g} gold={reward:F0} elapsed={elapsed:F1}s");
```

### Configuration

**Approach:** 종류별로 저장 위치가 하나씩 정해져 있다. 코드에 매직 넘버를 두지 않는다.

| 종류 | 위치 | 접근 | 변경 주체 |
|---|---|---|---|
| **밸런스 상수** (GDD 상수표 전체) | `Assets/Data/Config/BalanceConfig.asset` (SO, D2) | 부트에서 로드 → `Services.Register<BalanceConfig>` → `Formulas`가 인자로 받음 | 디자이너 (GDD 표 → SO 순서) |
| **콘텐츠 데이터** (장비·적·보스·챕터·스킬·가챠표) | `Assets/Data/{Equipment,Enemies,Bosses,Chapters,Skills,Gacha}/` SO | `ContentCatalog` SO 하나가 전부 참조. 부트에서 등록 | 디자이너 |
| **플레이어 설정** (`bgmMuted`, `sfxMuted`, `lowEffectMode`, `fps30Mode`) | `SaveDataV2` 필드 — 별도 저장소 없음 | `SettingsService` | 플레이어 |
| **플랫폼·빌드 설정** (AdMob 유닛 ID, Firebase 노드 루트, 빌드 버전 라벨) | `Assets/Data/Config/BuildConfig.asset` (SO). `useTestAdIds`는 `DEVELOPMENT_BUILD`에서 강제 true | `Services.Get<BuildConfig>()` | 개발자. 릴리스 체크리스트 항목(E6-14) |
| **화면 상수** (PPU 32, 기준 해상도 270×480, 전투 뷰 55%) | `BalanceConfig`의 `Display` 그룹 | Pixel Perfect Camera·레이아웃 스크립트가 읽음 | 디자이너 |
| **원격 설정** | **없음 (v1.0).** Firebase Remote Config 미사용 | — | — |

**SO 필드명 규칙:** GDD 상수명과 동일한 `UPPER_SNAKE_CASE` (`ENEMY_HP_GROWTH`). C# 관례와 다르지만 GDD 대조가 목적이므로 이 SO에서만 허용한다. `Formulas`는 `_cfg.ENEMY_HP_GROWTH`처럼 읽는다.

**Example**

```csharp
[CreateAssetMenu(menuName = "SoloHero/Config/Balance")]
public sealed class BalanceConfig : ScriptableObject
{
    [Header("Enemy")]
    public double ENEMY_HP_BASE = 30;
    public double ENEMY_HP_GROWTH = 1.10;
    public double ENEMY_ATK_BASE = 5;
    public double ENEMY_ATK_GROWTH = 1.10;
    public float  ENEMY_ATK_INTERVAL = 1.2f;
    [Header("Spawn")]
    public float  SPAWN_INTERVAL = 0.8f;
    public int    SPAWN_MAX_ALIVE = 4;
    public int    KILL_TARGET_NORMAL = 8;
    // ... one field per GDD constant, same name
}

// SoloHero.Core
public static class Formulas
{
    public static double EnemyHp(BalanceValues c, int g) => c.ENEMY_HP_BASE * Math.Pow(c.ENEMY_HP_GROWTH, g - 1);
    public static double HitDamage(BalanceValues c, double enemyAtk, double def)
    {
        double defRef = c.DEF_REF_MULT * enemyAtk;
        return Math.Max(1, enemyAtk * defRef / (defRef + def));
    }
}
```

`BalanceValues`는 `BalanceConfig`의 값을 복사한 순수 C# 클래스다 (Core가 UnityEngine을 참조하지 않기 위해). 부트에서 `config.ToValues()`로 한 번 변환한다.

### Event System

**Pattern:** Observer — 서비스가 소유하는 타입 안전 C# `event` (D7). 이벤트 버스·문자열 키·비동기 큐는 쓰지 않는다.

**Event Naming:** `{명사}{과거분사}` — `GoldChanged`, `StageCleared`, `BossFailed`, `EquipmentEquipped`, `PullCompleted`, `LaneUpgraded`, `HeroLeveledUp`, `OfflineRewardClaimed`

**규칙**

1. **발화자는 서비스 하나.** 같은 사실을 두 곳에서 발화하지 않는다 (`GoldChanged`는 `EconomyService`만).
2. **동기·메인 스레드.** 네이티브 콜백은 `MainThreadDispatcher`를 거친 뒤 발화.
3. **인자는 값 또는 읽기 전용 DTO.** `PlayerState` 자체를 넘기지 않는다.
4. **구독 해제 필수.** MonoBehaviour는 `OnEnable`에서 구독, `OnDisable`에서 해제. 서비스끼리는 생성자에서 구독하고 `Dispose`에서 해제.
5. **이벤트 핸들러 안에서 저장을 요청하지 않는다.** 저장 요청은 상태를 바꾼 서비스가 `RequestSave()`로 한 번만.
6. **순서 의존 금지.** 핸들러 실행 순서에 기대지 않는다. 순서가 필요하면 (레벨업 → 클리어 토스트) 이벤트가 아니라 `StageRunner` 안의 명시 큐로 처리한다.

**Example**

```csharp
// service
public sealed class EconomyService
{
    public event Action<double> GoldChanged;   // new balance
    public event Action<double> GemChanged;

    public void AddGold(double amount)
    {
        _state.Gold += amount;
        GoldChanged?.Invoke(_state.Gold);
    }
}

// presenter (Game assembly)
public sealed class HudGoldPresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    private EconomyService _economy;

    private void OnEnable()
    {
        _economy = Services.Get<EconomyService>();
        _economy.GoldChanged += OnGoldChanged;
        OnGoldChanged(_economy.Gold);
    }
    private void OnDisable() => _economy.GoldChanged -= OnGoldChanged;
    private void OnGoldChanged(double gold) => _label.text = BigNumberFormat.Format(gold);
}
```

### Debug Tools

**Available Tools**

| 도구 | 위치 | 기능 |
|---|---|---|
| **Debug Window** | `SoloHero.Editor` — `Tools > SoloHero > Debug` (에디터 전용) | 골드·젬 지급, 스테이지 지정, 강화·스킬 레벨 설정, pity 설정, 저장 초기화, `lastQuitTimeUtc`를 N초 전으로 설정(오프라인 테스트), 튜토리얼 스킵, v1 샘플 데이터 주입(이관 테스트) |
| **In-game Debug Panel** | `SoloHero.Game` — `#if DEVELOPMENT_BUILD \|\| UNITY_EDITOR`로만 컴파일. 설정 패널의 버전 라벨 5회 탭으로 열림 | 위 기능 중 기기에서 필요한 것: 골드 지급, 스테이지 이동, 저장 초기화, 오프라인 시각 조작, 광고 테스트 ID 표시 |
| **Perf Overlay** | 개발 빌드 전용 | FPS(하위 1% 포함), `Profiler.GetTotalAllocatedMemoryLong()`, 풀 사용량, 활성 적 수. 30분·2시간 방치 프로토콜(E9-09·10)의 계측 수단 |
| **Balance Simulator** | `Tools > Balance > Run Simulation` | `BalanceConfig` → CSV (D2) |
| **Gacha Verifier** | `Tools > Balance > Verify Gacha Table` | 확률표 합 100% 검사 + N회 시뮬레이션으로 실측 분포 출력 (E5-14 대체) |
| **Log Filter** | Unity Console 검색 `[Save]` 등 태그 | 로그 태그 규칙에 의존 |

**Activation:** 릴리스 빌드에는 디버그 코드가 **존재하지 않는다** — `#if` 컴파일 제거이지 런타임 플래그가 아니다. 개발 빌드 여부는 `BuildAutomator`의 `EditorUserBuildSettings.development`로 결정하고, 개발 빌드는 `useTestAdIds = true`를 강제한다.

---

## Project Structure

### Organization Pattern

**Pattern:** 하이브리드 — 최상위는 Unity 관례대로 **종류별**(Scripts / Data / Art / Prefabs / Scenes), `Scripts` 안은 **어셈블리 → 도메인** 순.

**Rationale:** Unity는 종류별 폴더(에셋 임포터·아틀라스·프리셋이 폴더 단위로 걸린다)를 전제로 한다. 코드만 어셈블리 경계(D8)를 폴더로 드러내면 "어디에 두는가"가 곧 "어느 어셈블리에 속하는가"가 되어 의존 방향이 폴더 구조로 강제된다. 프로젝트 소유 파일은 전부 `Assets/SoloHero/` 아래에 두어 벤더 SDK 폴더(Firebase·GoogleMobileAds·EDM4U·TextMesh Pro·Plugins)와 섞이지 않게 한다.

### Directory Structure

```
solohero/
├── Assets/
│   ├── SoloHero/                                  # 프로젝트 소유 전부. 벤더와 분리
│   │   ├── Scripts/
│   │   │   ├── Core/                              # SoloHero.Core.asmdef — noEngineReferences: true
│   │   │   │   ├── Common/                        # Result, FailReason, Log, LogTag, ILogSink, Services, BigNumberFormat, IClock, IRandom, IsExternalInit(polyfill)
│   │   │   │   ├── Config/                        # BalanceValues, ContentDefs (SO의 순수 C# 미러)
│   │   │   │   ├── Save/                          # SaveDataV2, PlayerDataV1, MigrationV1ToV2, PlayerState, ISaveStore
│   │   │   │   ├── Economy/                       # EconomyService, OfflineRewardService, AdSlotPolicy
│   │   │   │   ├── Growth/                        # UpgradeService, HeroLevelService, EquipmentService, SkillService, StatAggregator
│   │   │   │   ├── Gacha/                         # GachaService, GachaTableValues, IRandom, SystemRandom
│   │   │   │   ├── Combat/                        # HeroBrain, EnemyBrain, StageRunner, SpawnScheduler, Targeting, DamageCalc, SkillAutoCaster
│   │   │   │   ├── Progression/                   # StageIndex (g ↔ chapter/stage), ProgressionService
│   │   │   │   └── Formulas.cs
│   │   │   ├── Game/                              # SoloHero.Game.asmdef — refs: Core, UniTask, DOTween, Firebase, GoogleMobileAds, TMP
│   │   │   │   ├── Boot/                          # BootSequence, BootFailView
│   │   │   │   ├── Infrastructure/                # FirebaseSaveStore, LocalBackupStore, SaveService, AuthService, AdService, AnalyticsService, MainThreadDispatcher, UnityLogSink
│   │   │   │   ├── Config/                        # BalanceConfig, BuildConfig, ContentCatalog, EquipmentData, EnemyData, BossData, ChapterTheme, SkillData, GachaTable (SO 클래스)
│   │   │   │   ├── Combat/                        # BattleView, HeroView, EnemyView, EnemyPool, ParallaxScroller, DamageTextPool, VfxPool, HitFrameRelay
│   │   │   │   ├── UI/
│   │   │   │   │   ├── Hud/                       # HudGoldPresenter, HudStageBar, HudHpBar, HudKillGauge, SkillButtonBar
│   │   │   │   │   ├── Panels/                    # CharacterPanel, EquipmentPanel, GachaPanel, SkillPanel, SettingsPanel (+Presenter 각각)
│   │   │   │   │   ├── Popups/                    # OfflineRewardPopup, BossIntroPopup, BossFailPopup, AdConfirmPopup, QuitConfirmPopup, StageSelectSheet
│   │   │   │   │   ├── Common/                    # TabBar, PanelHost, ToastQueue, SafeAreaAdjuster, TapGuardButton, BackKeyRouter, Strings
│   │   │   │   │   └── Tutorial/                  # TutorialHints
│   │   │   │   ├── Audio/                         # AudioService, SfxPool
│   │   │   │   └── Debug/                         # DebugPanel, PerfOverlay  (#if DEVELOPMENT_BUILD || UNITY_EDITOR)
│   │   │   ├── Editor/                            # SoloHero.Editor.asmdef — refs: Core, Game
│   │   │   │   ├── BuildAutomator.cs              # Assets/Editor에서 이동
│   │   │   │   ├── DebugWindow.cs
│   │   │   │   ├── BalanceSimulator.cs
│   │   │   │   ├── GachaVerifier.cs
│   │   │   │   ├── SpriteImportPreset.cs          # AssetPostprocessor: Art/ 아래 자동 적용
│   │   │   │   └── ContentSetupWizard.cs          # MenuSetupWizard 후속
│   │   │   └── Tests/
│   │   │       └── EditMode/                      # SoloHero.Tests.EditMode.asmdef — refs: Core만
│   │   │           ├── GachaTests.cs
│   │   │           ├── FormulasTests.cs
│   │   │           ├── UpgradeServiceTests.cs
│   │   │           ├── OfflineRewardTests.cs
│   │   │           ├── MigrationV1ToV2Tests.cs
│   │   │           └── BigNumberFormatTests.cs
│   │   ├── Data/                                  # SO 인스턴스. 코드 아님
│   │   │   ├── Config/                            # BalanceConfig.asset, BuildConfig.asset, ContentCatalog.asset
│   │   │   ├── Equipment/                         # Equipment_Sword_Common.asset … 16개
│   │   │   ├── Enemies/                           # Enemy_Slime.asset … 8개
│   │   │   ├── Bosses/                            # Boss_SlimeKing.asset … 3개
│   │   │   ├── Chapters/                          # Chapter_01_Meadow.asset … 5개
│   │   │   ├── Skills/                            # Skill_PowerStrike.asset … 3개
│   │   │   └── Gacha/                             # GachaTable.asset
│   │   ├── Art/                                   # 임포트 프리셋 자동 적용 범위 (D12)
│   │   │   ├── Hero/                              # hero_idle_4.png, hero_run_6.png, hero_attack_6.png, hero_hit_2.png, hero_die_6.png, hero_skill1_6.png …
│   │   │   ├── Enemies/{slime,rat,goblin,spider,bat,skeleton,zombie,imp}/
│   │   │   ├── Bosses/{slime_king,goblin_chief,lava_giant}/
│   │   │   ├── Backgrounds/{meadow,forest,cave,ruins,volcano}/   # far.png, mid.png, near.png, ground.png
│   │   │   ├── Icons/Equipment/                   # icon_sword_common.png … 16개
│   │   │   ├── UI/                                # 프레임·버튼·게이지·탭 (9-slice)
│   │   │   ├── Vfx/                               # hit_flash_4.png, crit_6.png, levelup_ring_8.png …
│   │   │   ├── Placeholder/                       # 단색 박스. 계약 동일, E8에서 교체
│   │   │   ├── Fonts/                             # Assets/Fonts에서 이동. TMP 폰트 에셋 포함
│   │   │   └── Atlases/                           # Hero.spriteatlas, Enemies.spriteatlas, UI.spriteatlas, BG_Meadow.spriteatlas …
│   │   ├── Animation/
│   │   │   ├── Hero/                              # Hero.controller + Hero_Idle.anim …
│   │   │   ├── Enemies/                           # Enemy_Base.controller (Override Controller per enemy)
│   │   │   └── Bosses/
│   │   ├── Audio/
│   │   │   ├── Music/                             # bgm_meadow.ogg … 6곡
│   │   │   ├── Sfx/                               # sfx_hit.wav, sfx_hit_crit.wav … 17종
│   │   │   └── Mixer/                             # Main.mixer (BGM / SFX 그룹)
│   │   ├── Prefabs/
│   │   │   ├── Combat/                            # Hero, Enemy_Base, Boss_Base, DamageText, Vfx_*
│   │   │   ├── UI/                                # Panel_*, Popup_*, Toast, TabBar
│   │   │   └── Systems/                           # Services (Boot 씬 루트), AudioSources
│   │   └── Scenes/
│   │       ├── Boot.unity
│   │       └── Game.unity
│   ├── Settings/                                  # URP: UniversalRP.asset, Renderer2D.asset (현 위치 유지)
│   ├── Plugins/                                   # 벤더: Android 템플릿·매니페스트, Demigiant(DOTween)
│   ├── Firebase/  GoogleMobileAds/  ExternalDependencyManager/  GeneratedLocalRepo/   # 벤더. 손대지 않음
│   ├── TextMesh Pro/                              # 벤더
│   ├── Editor Default Resources/                  # 벤더(Firebase)
│   ├── link.xml                                   # SaveDataV2·PlayerDataV1 스트리핑 보존
│   └── google-services.json                       # 커밋 금지 (기존 규약)
├── Packages/manifest.json
├── ProjectSettings/
├── Builds/                                        # game.aab, build.log (gitignore)
├── _bmad-output/                                  # 계획 산출물 (GDD·아키텍처·에픽·스토리)
├── docs/                                          # 참고 문서
├── Jenkinsfile
└── .github/workflows/
```

**정리 대상 (E1-03에서 수행)**

| 현재 | 처리 |
|---|---|
| `Assets/Scripts/*` (15개) | `Assets/SoloHero/Scripts/{Core,Game}/…`로 이동하며 D6·D8 규약에 맞게 분리. `SingletonMB`·`JsonDataManager`·`GachaSystem`(Box-Muller)은 삭제 |
| `Assets/Editor/*` | `Assets/SoloHero/Scripts/Editor/` |
| `Assets/ScriptableObjects/Equipment` | `Assets/SoloHero/Data/Equipment/` (이름 규칙 적용) |
| `Assets/Fonts`, `Assets/Sprites`, `Assets/Tiles` | `Assets/SoloHero/Art/…`로 이동. 3D 잔재는 삭제 |
| `Assets/AddressableAssetsData` | 삭제 (D1) |
| `Assets/StreamingAssets/JSON` | 삭제 (D2) |
| `Assets/Resources` | 삭제. Resources 폴더는 두지 않는다 (직접 참조만) |
| `Assets/Prefabs` | `Assets/SoloHero/Prefabs/` |

### System Location Mapping

| 시스템 (Step 2) | Core (순수 로직) | Game (Unity) | 데이터 | 테스트 |
|---|---|---|---|---|
| S1 부트·저장 | `Save/` — DTO, 이관, `PlayerState`, `ISaveStore` | `Boot/`, `Infrastructure/` — Firebase·PlayerPrefs 스토어, 디바운스, 인증 | `Data/Config/BuildConfig` | `MigrationV1ToV2Tests` |
| S2 자동 전투 | `Combat/` — `HeroBrain`, `EnemyBrain`, `SpawnScheduler`, `Targeting`, `DamageCalc`, `SkillAutoCaster` | `Combat/` — 뷰, 풀, 패럴랙스, 히트 프레임 릴레이 | `Data/Enemies`, `Data/Bosses` | `FormulasTests` (데미지·감쇄) |
| S3 스테이지·챕터 | `Combat/StageRunner`, `Progression/` | `Combat/BattleView`가 `StageRunner.Tick` 호출, `UI/Popups/StageSelectSheet` | `Data/Chapters` | `FormulasTests` (g 스케일링) |
| S4 성장 | `Growth/` — 강화·레벨·장비·스킬·합산 | `UI/Panels/Character·Equipment·Skill` | `Data/Equipment`, `Data/Skills` | `UpgradeServiceTests` |
| S5 가챠 | `Gacha/` — 확률표·천장·`IRandom` 주입 | `UI/Panels/GachaPanel` (연출) | `Data/Gacha/GachaTable` | `GachaTests` |
| S6 경제·오프라인·광고 | `Economy/` — 골드·젬, 오프라인 계산, 광고 슬롯 정책(횟수·리셋) | `Infrastructure/AdService` (AdMob), `UI/Popups/OfflineRewardPopup` | `Data/Config/BalanceConfig` | `OfflineRewardTests` |
| S7 UI | `Common/BigNumberFormat` | `UI/` 전체 | — | `BigNumberFormatTests` |
| S8 아트·연출·오디오 | — | `Combat/` 풀·VFX, `Audio/` | `Art/`, `Animation/`, `Audio/` | — |
| S9 시뮬레이터 | `Formulas.cs` | — | `Data/Config/BalanceConfig` | `Editor/BalanceSimulator` |
| 횡단 (Step 5) | `Common/` — `Result`, `Log`, `Services` | `Infrastructure/UnityLogSink`, `Debug/` | — | — |

### Naming Conventions

#### Files

| 종류 | 규칙 | 예 |
|---|---|---|
| C# 스크립트 | `PascalCase.cs`, 파일명 = 클래스명, 파일당 public 타입 1개 | `StageRunner.cs`, `GachaPanelPresenter.cs` |
| asmdef | `SoloHero.{Assembly}.asmdef` | `SoloHero.Core.asmdef` |
| 씬 | `PascalCase.unity` | `Boot.unity`, `Game.unity` |
| SO 에셋 | `{Type}_{Name}[_{Variant}].asset` | `Equipment_Sword_Rare.asset`, `Enemy_Slime.asset`, `Chapter_01_Meadow.asset` |
| 프리팹 | `{Category}_{Name}.prefab` | `Enemy_Base.prefab`, `Popup_OfflineReward.prefab`, `Panel_Gacha.prefab` |
| 스프라이트 시트 | `{entity}_{clip}_{frames}.png` (snake_case) | `slime_run_6.png`, `hero_skill2_6.png` |
| 단일 스프라이트 | `{category}_{name}.png` | `icon_sword_rare.png`, `ui_frame_panel.png` |
| 배경 | `Backgrounds/{theme}/{layer}.png` | `meadow/far.png`, `meadow/ground.png` |
| 오디오 | `bgm_{theme}.ogg`, `sfx_{event}.wav` | `bgm_volcano.ogg`, `sfx_gacha_flip.wav` |
| 애니메이션 | 컨트롤러 `{Entity}.controller`, 클립 `{Entity}_{Clip}.anim` | `Hero.controller`, `Hero_Attack.anim` |
| 아틀라스 | `{Group}.spriteatlas` | `Enemies.spriteatlas`, `BG_Cave.spriteatlas` |

#### Code Elements

| 요소 | 규칙 | 예 |
|---|---|---|
| 네임스페이스 | `SoloHero.{Assembly}.{Folder}` | `SoloHero.Core.Gacha`, `SoloHero.Game.UI.Panels` |
| 클래스·구조체·enum | `PascalCase` | `UpgradeService`, `FailReason` |
| 인터페이스 | `I` + `PascalCase` | `ISaveStore`, `IRandom` |
| 메서드 | `PascalCase`, 실패 가능 동작은 `Try` 접두 + `Result` 반환 | `TryUpgrade`, `TryPull` |
| public 프로퍼티 | `PascalCase` | `Gold`, `PityCount` |
| private 필드 | `_camelCase` | `_state`, `_cfg` |
| `[SerializeField]` 필드 | `_camelCase` (private) | `[SerializeField] private TMP_Text _label;` |
| 지역 변수·매개변수 | `camelCase` | `stageIndex`, `dt` |
| 상수 (C#) | `PascalCase` | `const int MaxToastQueue = 4;` |
| `BalanceConfig` 필드 | `UPPER_SNAKE_CASE` = GDD 상수명 (이 클래스에서만 허용) | `ENEMY_HP_GROWTH` |
| 이벤트 | `{명사}{과거분사}` | `GoldChanged`, `StageCleared` |
| 비동기 메서드 | `Async` 접미, `UniTask` 반환 | `LoadAsync`, `ShowRewardedAsync` |
| 서비스 클래스 | `{Domain}Service` (Core, 순수 C#) | `EconomyService` |
| Presenter | `{View}Presenter` (Game, MonoBehaviour) | `GachaPanelPresenter` |
| 저장 DTO 필드 | GDD `저장 상태와 화면 대응` 표의 식별자 그대로 (`camelCase`) | `highestStage`, `pityCount` |
| 문자열 키 | `{area}.{snake_case}` | `toast.not_enough_gold`, `popup.boss_fail.title` |
| Animator 파라미터 | Trigger `Attack`/`Hit`/`Die`/`Skill1~3`, Bool `Moving` | — |
| 로그 태그 | `LogTag` enum 값 | `LogTag.Gacha` |

#### Game Assets

- **스프라이트 시트**는 프레임을 가로 한 줄로 배열하고 파일명의 마지막 숫자가 프레임 수다. 임포터가 이 숫자로 자동 슬라이스한다(`SpriteImportPreset`).
- **히트 프레임**은 Attack 클립의 3번째 프레임(인덱스 2)에 Animation Event `OnHitFrame`을 둔다. 다른 인덱스를 쓰려면 `EnemyData.hitFrameIndex`로 덮어쓴다.
- **플레이스홀더**는 `Art/Placeholder/`에 같은 파일명 규칙으로 두고, 실제 에셋이 같은 이름으로 `Art/{Hero,Enemies,…}/`에 들어오면 프리팹 참조만 바꾼다.
- **등급 색상**은 `BalanceConfig.Display`가 아니라 `ContentCatalog.gradeColors[4]`에 둔다 (GDD 단일 출처: `#9E9E9E`, `#3D8BFF`, `#A24BFF`, `#FFC531`).

### Architectural Boundaries

```
SoloHero.Tests.EditMode ──▶ SoloHero.Core
SoloHero.Editor ──▶ SoloHero.Game ──▶ SoloHero.Core
                         │                 ▲
                         ▼                 │ (참조 금지 — asmdef noEngineReferences)
                 UniTask · DOTween · Firebase · GoogleMobileAds · TMP · UnityEngine
```

| 경계 | 규칙 | 강제 수단 |
|---|---|---|
| **Core는 UnityEngine을 모른다** | `Mathf`·`Debug`·`Time`·`Random` 사용 금지. `System.Math`, `Log`, `Tick(dt)` 인자, `IRandom` 주입 | `SoloHero.Core.asmdef`의 `noEngineReferences: true` — 위반 시 컴파일 실패 |
| **Game → Core 단방향** | Core는 Game 타입을 참조하지 않는다. Game은 Core 서비스를 `Services.Get<T>()`로 얻고 이벤트를 구독한다 | asmdef 참조 방향 |
| **UI는 상태를 쓰지 않는다** | Presenter는 서비스의 `Try*` 메서드를 호출하고 이벤트로 갱신한다. `PlayerState`에 직접 접근·변경 금지 | 코드 리뷰 규칙. `PlayerState` setter는 `internal` (Core 어셈블리 안에서만) |
| **저장은 서비스만 요청** | `SaveService.RequestSave()` 호출은 Core 서비스의 상태 변경 직후에만. UI·뷰·핸들러에서 호출 금지 | 코드 리뷰 규칙 |
| **Tick은 뷰가 호출** | `StageRunner.Tick(dt)`·`HeroBrain.Tick(dt)`는 `BattleView.Update`에서만 호출. 다른 곳에서 호출 금지 | 단일 호출 지점 |
| **물리 미사용** | Game 어셈블리에서 `Collider2D`·`Rigidbody2D`·`Physics2D` 사용 금지 (D10) | 코드 리뷰 규칙 |
| **벤더 폴더 불변** | `Firebase/`, `GoogleMobileAds/`, `ExternalDependencyManager/`, `Plugins/Demigiant/`, `TextMesh Pro/`는 수정하지 않는다. 설정은 `Plugins/Android/` 템플릿과 `BuildConfig`로만 | 코드 리뷰 규칙 |
| **테스트는 Core만** | EditMode 테스트가 Game을 참조하면 Unity 의존이 새어 들어온 것이다 | `SoloHero.Tests.EditMode.asmdef` 참조 목록 |
| **에디터 코드 격리** | `UnityEditor` 사용은 `Scripts/Editor/`에서만. 런타임 코드의 `#if UNITY_EDITOR`는 `Debug/`에서만 허용 | asmdef `Editor` 플랫폼 제한 |

---

## Implementation Patterns

에이전트마다 다르게 결정할 수 있는 지점을 고정한다. 기준은 "구현자가 추측해야 하는가?" — 추측해야 하면 여기 있다.

### Novel Patterns

**없음.** Step 2의 결정(장르 표준 모방)에 따라 고유 패턴을 두지 않는다. 가챠는 확률표 + 천장, 전투는 상태 머신 + 풀, 저장은 원격 + 로컬 + 디바운스다. 아래는 전부 표준 패턴의 **SoloHero 적용 규약**이다.

### Communication Patterns

**Pattern:** 서비스 로케이터(D6) + 서비스 소유 C# 이벤트(D7). 등록은 `Boot`에서 한 번, 순서 고정.

**등록 순서 (의존 방향)**

```
BalanceValues, ContentDefs, BuildConfig          ← 설정 (의존 없음)
IClock, IRandom, ILogSink                         ← 인프라 추상
SaveService (ISaveStore: Firebase | Local)        ← 저장
PlayerState                                       ← 로드·이관 결과
EconomyService                                    ← 상태만 의존
ProgressionService, HeroLevelService              ← 상태 + 경제
EquipmentService, UpgradeService, SkillService    ← 상태 + 경제
StatAggregator                                    ← 위 성장 서비스 전부
GachaService                                      ← 경제 + 장비
OfflineRewardService, AdSlotPolicy                ← 경제 + 진행
AdService, AnalyticsService, AudioService         ← Unity 측 서비스
```

**Example**

```csharp
// SoloHero.Core/Common/Services.cs
public static class Services
{
    private static readonly Dictionary<Type, object> _map = new();
    public static void Register<T>(T instance) where T : class => _map[typeof(T)] = instance;
    public static T Get<T>() where T : class =>
        _map.TryGetValue(typeof(T), out var o) ? (T)o : throw new InvalidOperationException($"service not registered: {typeof(T).Name}");
    public static void Clear() => _map.Clear();   // tests only
}

// SoloHero.Game/Boot/BootSequence.cs (excerpt)
Services.Register(balance.ToValues());
Services.Register(catalog.ToDefs());
Services.Register<IClock>(new SystemClock());
Services.Register<IRandom>(new SystemRandom());
var save = new SaveService(store, localBackup, Services.Get<IClock>());
Services.Register(save);
var state = await save.LoadAndMigrateAsync();
Services.Register(state);
var economy = new EconomyService(state, save);
Services.Register(economy);
// ... same order as the table above
```

**Presenter 템플릿** — 모든 UI 클래스가 이 형태를 따른다.

```csharp
public sealed class CharacterPanelPresenter : MonoBehaviour, IPanel
{
    [SerializeField] private UpgradeRowView[] _rows;      // 4 lanes
    private UpgradeService _upgrade;
    private EconomyService _economy;

    private void OnEnable()
    {
        _upgrade = Services.Get<UpgradeService>();
        _economy = Services.Get<EconomyService>();
        _upgrade.LaneUpgraded += OnLaneUpgraded;
        _economy.GoldChanged  += OnGoldChanged;
        RefreshAll();
    }
    private void OnDisable()
    {
        _upgrade.LaneUpgraded -= OnLaneUpgraded;
        _economy.GoldChanged  -= OnGoldChanged;
    }

    public void OnUpgradeTapped(int laneIndex)          // wired from TapGuardButton
    {
        var result = _upgrade.TryUpgrade((UpgradeLane)laneIndex);
        if (!result.Ok) Services.Get<ToastQueue>().Show(StringKeys.For(result.Reason));
    }

    private void OnLaneUpgraded(UpgradeLane lane, int level) => _rows[(int)lane].Refresh();
    private void OnGoldChanged(double gold) => RefreshAffordability();
}
```

### Entity Patterns

**Creation:** 프리팹 + `UnityEngine.Pool.ObjectPool<T>` + `Bind(brain, def)`. 뷰(MonoBehaviour)는 껍데기이고 로직은 Core의 `EnemyBrain` 인스턴스가 가진다. 풀에서 꺼낼 때 Brain을 새로 만들지 않고 `Reset(def, g)`로 재사용한다.

**Example**

```csharp
// SoloHero.Game/Combat/EnemyPool.cs
public sealed class EnemyPool : MonoBehaviour
{
    [SerializeField] private EnemyView _prefab;
    [SerializeField] private Transform _root;
    private ObjectPool<EnemyView> _pool;

    private void Awake()
    {
        int cap = Services.Get<BalanceValues>().SPAWN_MAX_ALIVE + 1;   // +1 for boss overlap safety
        _pool = new ObjectPool<EnemyView>(
            createFunc:      () => Instantiate(_prefab, _root),
            actionOnGet:     v => v.gameObject.SetActive(true),
            actionOnRelease: v => v.gameObject.SetActive(false),
            actionOnDestroy: v => Destroy(v.gameObject),
            collectionCheck: true, defaultCapacity: cap, maxSize: cap);
        for (int i = 0; i < cap; i++) _pool.Release(_pool.Get());   // pre-warm
    }

    public EnemyView Spawn(EnemyDef def, int g, float x)
    {
        var view = _pool.Get();
        view.Bind(def, g, x, onDead: Release);
        return view;
    }
    private void Release(EnemyView v) => _pool.Release(v);
}

// SoloHero.Game/Combat/EnemyView.cs (excerpt)
public void Bind(EnemyDef def, int g, float x, Action<EnemyView> onDead)
{
    Brain.Reset(def, g, x);            // Core object reused, no allocation
    _sprite.sprite = def.IdleFrame;    // placeholder or real, same path
    _animator.runtimeAnimatorController = def.Animator;
    _onDead = onDead;
}
```

**규칙**
- `Instantiate`는 풀의 `createFunc` 안에서만. 게임플레이 코드에 `Instantiate`/`Destroy`가 보이면 규약 위반.
- 풀 4종: `EnemyPool`, `DamageTextPool`, `VfxPool`, `SfxPool`. 크기는 `BalanceValues`에서 계산 (`SPAWN_MAX_ALIVE`, 데미지 텍스트 = 적 수 × 공격속도 상한 × 표시 시간 ≈ 16, VFX 8, SFX 4).
- 보스는 `EnemyView`를 그대로 쓰고 `BossDef`(= `EnemyDef` + 배율)로 바인딩한다. 별도 클래스 없음.

### State Patterns

**Pattern:** 순수 C# 상태 머신 (D14). `enum` 상태 + `Tick(dt)` 안의 `switch`. 상태 클래스 계층·`StateMachineBehaviour`·코루틴은 쓰지 않는다. 전이는 `Tick` 안에서만 일어나고, 전이 시 `StateChanged` 이벤트를 발화한다 (뷰가 Animator를 갱신).

**Example — `HeroBrain`**

```csharp
public enum HeroState { Advance, Engage, Hit, Skill, Dead }

public sealed class HeroBrain
{
    public HeroState State { get; private set; }
    public event Action<HeroState> StateChanged;
    public event Action<double, bool> DealtDamage;    // amount, isCrit

    public void Tick(float dt, ICombatWorld world)
    {
        switch (State)
        {
            case HeroState.Advance:
                if (world.NearestEnemyDistance() <= _cfg.ATTACK_RANGE) { Set(HeroState.Engage); break; }
                _x += _cfg.MOVE_SPEED * dt;                       // constant, never a stat
                break;
            case HeroState.Engage:
                if (!world.HasEnemyInRange(_cfg.ATTACK_RANGE)) { Set(HeroState.Advance); break; }
                _attackTimer -= dt;
                if (_attackTimer <= 0f) { _attackTimer = 1f / _stats.AttackSpeed; RequestAttackAnim(); }
                break;
            case HeroState.Hit:  if ((_stun -= dt) <= 0f) Set(HeroState.Engage); break;
            case HeroState.Skill: /* skill windup owned by SkillAutoCaster */ break;
            case HeroState.Dead: break;
        }
    }

    // Called by the view on the Attack clip's hit frame (Animation Event → HitFrameRelay → here)
    public void OnHitFrame(ICombatWorld world)
    {
        var target = world.NearestEnemyInRange(_cfg.ATTACK_RANGE);
        if (target == null) return;                                // re-check range at hit frame (GDD rule 7)
        bool crit = _rng.NextDouble() < _stats.CritRate;
        double dmg = DamageCalc.HeroHit(_stats, crit, _cfg);
        target.TakeDamage(dmg);
        DealtDamage?.Invoke(dmg, crit);
    }

    private void Set(HeroState s) { if (State == s) return; State = s; StateChanged?.Invoke(s); }
}
```

**Example — `StageRunner` 동시 판정 순서**

```csharp
public void Tick(float dt)
{
    _spawner.Tick(dt);                    // may spawn (respects SPAWN_MAX_ALIVE)
    foreach (var e in _enemies) e.Tick(dt, _world);
    _hero.Tick(dt, _world);

    // Order matters (GDD edge cases): resolve enemy deaths BEFORE hero death, boss death BEFORE timer.
    ResolveEnemyDeaths();                 // kills++ ; may set _cleared
    if (_cleared) { Transition(StageState.Clearing); return; }
    if (_isBoss && (_bossTimer -= dt) <= 0f) { Transition(StageState.Failed); return; }
    if (_hero.Hp <= 0) { Transition(StageState.Failed); return; }
}
```

**규칙**
- 상태 소유자는 하나. `HeroState`는 `HeroBrain`, `StageState`는 `StageRunner`, `EnemyState`는 `EnemyBrain`. 뷰는 상태를 읽기만 한다.
- 시간 상수(`STAGE_CLEAR_DELAY` 2 s, `STAGE_RETRY_DELAY` 3 s, `BOSS_INTRO_TIME` 1.5 s)는 `Tick` 안의 타이머로 처리한다. `UniTask.Delay`는 UI 연출에만 쓴다.
- Animator는 Trigger `Attack`/`Hit`/`Die`/`Skill{n}`, Bool `Moving`만 받는다. 상태 enum을 Animator 정수 파라미터로 넘기지 않는다.

### Data Patterns

**Access:** SO(Game) → 부트에서 순수 C# 미러(Core)로 1회 변환 → 서비스는 미러만 읽는다. 런타임 중 SO를 다시 읽지 않는다.

**Example**

```csharp
// SoloHero.Game/Config/ContentCatalog.cs
[CreateAssetMenu(menuName = "SoloHero/Config/Content Catalog")]
public sealed class ContentCatalog : ScriptableObject
{
    public EquipmentData[] equipment;   // 16
    public EnemyData[] enemies;         // 8
    public BossData[] bosses;           // 3
    public ChapterTheme[] chapters;     // 5
    public SkillData[] skills;          // 3
    public GachaTable gacha;
    public Color[] gradeColors = new Color[4];

    public ContentDefs ToDefs() => new ContentDefs(
        equipment.Select(e => e.ToDef()).ToArray(),
        enemies.Select(e => e.ToDef()).ToArray(), /* ... */);
}

// SoloHero.Core/Config/ContentDefs.cs — plain C#, immutable
public sealed record EquipmentDef(string Id, EquipmentSlot Slot, Grade Grade, double Multiplier, double SpeedBonus, double CritBonus, double RefundGold);
// Unity 2022.3 = C# 9 on .NET Standard 2.1: positional records need the IsExternalInit polyfill (Core/Common/IsExternalInit.cs)
```

**규칙**
- `id`는 SO 에셋 이름과 동일한 문자열 (`Equipment_Sword_Rare`). 저장 DTO에는 이 문자열이 들어간다.
- 챕터 6+는 `ChapterTheme[(chapter − 1) % 5]`로 순환 (`StageIndex.ThemeIndex(chapter)`).
- 콘텐츠 조회는 `ContentDefs.Equipment(id)`처럼 O(1) 딕셔너리. 매 프레임 `Find`·LINQ 금지.

### Stat & Formula Patterns

**Pattern:** 합산은 `StatAggregator` 한 곳. GDD 순서 — 기본 + 레벨 가산 → 강화 승산 → 장비 승산 → 버프 **합산 후 1회** 승산. 공격속도 상한, 크리확률 가산.

```csharp
public static HeroStats Compute(PlayerState s, ContentDefs d, BalanceValues c, BuffSet buffs)
{
    double lvl = s.HeroLevel - 1;
    double atk = (c.ATK_BASE + c.LEVEL_ATK_GAIN * lvl) * Math.Pow(c.UPG_STAT_MULT, s.UpgradeAtk) * d.Mult(s.EquippedSword) * (1 + buffs.SumAtk());
    double hp  = (c.HP_BASE  + c.LEVEL_HP_GAIN  * lvl) * Math.Pow(c.UPG_STAT_MULT, s.UpgradeHp)  * d.Mult(s.EquippedArmor);
    double def =  c.DEF_BASE                            * Math.Pow(c.UPG_STAT_MULT, s.UpgradeDef) * d.Mult(s.EquippedHelm);
    double spd = Math.Min(c.ATKSPD_MAX, (c.ATKSPD_BASE + c.UPG_GAIN_SPD * s.UpgradeSpd) * (1 + d.SpeedBonus(s.EquippedBoots)));
    double crit = c.CRIT_RATE_BASE + d.CritBonus(s.EquippedBoots);
    return new HeroStats(hp, atk, def, spd, crit);
}
```

**규칙:** UI 스탯 요약과 전투가 같은 `Compute`를 호출한다 (GDD DoD "합산 순서 수식이 UI와 일치"). 공식은 `Formulas`/`StatAggregator`/`DamageCalc`에만 있고 다른 클래스에 수식이 나타나면 위반.

### Mutation & Save Pattern

**Pattern:** 상태 변경은 항상 같은 3단계 — **변경 → 이벤트 → 저장 요청**. 저장 요청은 GDD 트리거 7종에 해당하는 서비스 메서드에서만.

```csharp
public Result TryEquip(string equipmentId)
{
    // 1. validate
    if (!_state.Owned.Contains(equipmentId)) return Result.Fail(FailReason.Locked);
    var def = _defs.Equipment(equipmentId);
    // 2. mutate
    _state.SetEquipped(def.Slot, equipmentId);
    // 3. notify
    EquipmentEquipped?.Invoke(def.Slot, equipmentId);
    // 4. persist (this method IS a save trigger per GDD)
    _save.RequestSave();
    return Result.Success;
}
```

| 저장 트리거 (GDD) | 호출 위치 |
|---|---|
| 스테이지 클리어 | `ProgressionService.OnStageCleared` |
| 가챠 결과 확정 | `GachaService.TryPull` / `TryPullTen` — 연출 전 |
| 장착 변경 | `EquipmentService.TryEquip` (자동 장착 포함) |
| 강화 성공 | `UpgradeService.TryUpgrade` |
| 스킬 레벨업 | `SkillService.TryLevelUp` |
| 오프라인 보상 수령 | `OfflineRewardService.Claim` |
| 일시정지·종료 | `GameLifecycle.OnApplicationPause/Quit` → `SaveService.FlushAsync()` |

### Time & Random Injection Pattern

**Pattern:** Core는 `DateTime.UtcNow`·`UnityEngine.Random`·`System.Random`을 직접 쓰지 않는다. `IClock`·`IRandom`을 생성자로 받는다. 테스트가 시간 조작·고정 시드를 주입한다.

```csharp
public interface IClock  { long UtcNowSeconds { get; } DateTime LocalNow { get; } }   // LocalNow: ad daily reset at device-local midnight
public interface IRandom { double NextDouble(); int Next(int maxExclusive); }

public sealed class OfflineRewardService
{
    public OfflineReward Calculate(PlayerState s, BalanceValues c)
    {
        long elapsed = _clock.UtcNowSeconds - s.LastQuitTimeUtc;
        if (s.LastQuitTimeUtc == 0) return OfflineReward.None;                       // first run
        if (elapsed < 0 || elapsed > c.OFFLINE_CAP * 2) { s.LastQuitTimeUtc = _clock.UtcNowSeconds; return OfflineReward.None; } // tamper
        long capped = Math.Min(elapsed, c.OFFLINE_CAP);
        double gold = Formulas.StageGold(c, s.FarmingStage) / c.OFFLINE_DIVISOR * capped;
        return new OfflineReward(capped, gold, showPopup: capped >= c.OFFLINE_MIN_SECONDS);
    }
}
```

### Gacha Pattern (장르 표준)

**Pattern:** 확률표 누적 합 + 천장 카운터. 결과 확정 → 저장 → 연출 순서. 10연은 선차감 후 10회 반복.

```csharp
public PullResult Pull()
{
    _state.PityCount++; _state.TotalPullCount++;
    Grade grade;
    if (_state.PityCount >= _table.PityCeiling) { grade = Grade.Legendary; _state.PityCount = 0; }
    else
    {
        double r = _rng.NextDouble();               // [0,1)
        grade = _table.PickGrade(r);                // cumulative table, sums to 1.0 (verified by GachaVerifier)
        if (grade == Grade.Legendary && _table.ResetOnLegendary) _state.PityCount = 0;
    }
    var slot = (EquipmentSlot)_rng.Next(4);
    var def  = _defs.Equipment(slot, grade);
    return _equipment.Acquire(def);                 // AutoEquipped | Stored | Refunded(gold)
}
```

`ResetOnLegendary`는 `GachaTable` 필드로 두어 GDD 역반영 시 확정값을 데이터로 받는다 (R9).

### UI Patterns

| 패턴 | 규약 |
|---|---|
| **패널 (하나만 열림)** | `PanelHost.Toggle(PanelId)` — 열린 패널이 같으면 닫고, 다르면 닫은 뒤 연다. 슬라이드는 `DOAnchorPosY`, 0.25 s, `Ease.OutCubic`. 패널은 `IPanel { Show(); Hide(); }` |
| **팝업 (스택)** | `PopupHost.Push(popup)` / `Pop()`. 뒤로가기는 `BackKeyRouter`: 팝업 → 패널 → 종료 확인 순. 팝업 중첩 표시 금지 (스택 최상단만 보임) |
| **토스트** | `ToastQueue.Show(stringKey)` — 큐, 동시 1개, 1.5 s 표시, 최대 대기 4개(초과 시 가장 오래된 것 드롭) |
| **연타 방어** | 소비 버튼은 전부 `TapGuardButton` — 탭 즉시 `interactable = false`, 서비스 `Try*` 반환 후(동기) 또는 `UniTask` 완료 후 복구. 광고 버튼은 콜백까지 |
| **큰 수** | 표시 문자열은 `BigNumberFormat.Format(double)`만. 1,000 미만 정수, 이상 K/M/B/T 소수 1자리, T 초과 aa·ab… |
| **문자열** | `Strings.Get(key)` — 하드코딩 한국어 금지. 키는 `{area}.{snake_case}` |
| **레이아웃** | 전투 뷰 55% / 스킬바 / 패널 영역 / 탭바. 앵커 기반, `SafeAreaAdjuster`가 최상위 `SafeArea` RectTransform 하나만 보정 |

### Async Patterns

- `UniTask`만 사용. 코루틴(`StartCoroutine`) 금지.
- MonoBehaviour에서 시작하는 비동기는 `this.GetCancellationTokenOnDestroy()`를 넘긴다.
- `.Forget()`은 내부에 try-catch가 있는 `UniTaskVoid` 메서드에만.
- 외부 SDK 호출은 `Infrastructure/`에서만 `await`하고, 결과를 `Result` 또는 이벤트로 Core에 넘긴다.
- 프레임 단위 로직(전투)은 비동기가 아니라 `Tick(dt)`.

### Consistency Rules

| 패턴 | 규약 | 강제 수단 |
|---|---|---|
| 서비스 등록 | Boot에서 위 표의 순서로 1회. 다른 곳에서 `Register` 금지 | 코드 리뷰 |
| Presenter | `OnEnable` 구독 + 즉시 Refresh / `OnDisable` 해제 / `Try*` 호출 / `Result` → 토스트 | 템플릿 |
| 엔티티 생성 | 풀 `createFunc` 밖 `Instantiate` 금지 | grep `Instantiate(` |
| 상태 머신 | enum + `Tick` switch. 전이는 `Tick` 안에서만 | 코드 리뷰 |
| 수식 위치 | `Formulas`·`StatAggregator`·`DamageCalc`·`BigNumberFormat` 외부에 산술 공식 금지 | 코드 리뷰 |
| 변경 3단계 | 변경 → 이벤트 → `RequestSave` 순서. 트리거 7종 표의 메서드에서만 저장 요청 | 코드 리뷰 |
| 시간·난수 | Core에서 `DateTime`·`Random` 직접 사용 금지 | `noEngineReferences` + grep `DateTime.` |
| 가챠 | 확정 → 저장 → 연출. 연출 중 이탈해도 결과 보존 | 순서 규약 |
| UI 호스트 | 패널은 `PanelHost`, 팝업은 `PopupHost`, 토스트는 `ToastQueue`를 통해서만 | 코드 리뷰 |
| 비동기 | UniTask + 취소 토큰, 코루틴 금지 | grep `StartCoroutine` |
| 문자열 | `Strings.Get(key)` 외 한국어 리터럴 금지 | grep 비ASCII in `.cs` |

---

## Architecture Validation

### Validation Summary

| Check | Result | Notes |
|---|---|---|
| Decision Compatibility | **PASS** | 결정 15건 상호 충돌 없음. 2022.3 API 가용성 확인 — `UnityEngine.Pool`(2021.1+), asmdef `noEngineReferences`, UniTask 2.5.11, Newtonsoft 3.2.1. C# 9 positional record는 `IsExternalInit` 폴리필 필요 → 구조에 추가 (이슈 1) |
| GDD Coverage | **PASS (조건부)** | S1~S9 전부 위치·패턴·데이터·테스트 매핑. 단 **R9(가챠 방식 변경)와 ADR-4(E1-06 문구)는 GDD 역반영 전까지 문서 간 불일치 상태** — Step 9 후속 작업으로 고정 |
| Pattern Completeness | **PASS** | 엔티티 생성·통신·상태·에러·데이터·이벤트 6개 시나리오 + 스탯/수식·변경-저장·시간/난수 주입·가챠·UI·비동기 6개 추가. 전부 코드 예시 포함 |
| Epic Mapping | **PASS** | E1~E9 전부 위치 매핑 (아래 표). E9의 기기 매트릭스·스토어 자산은 수작업 항목이라 아키텍처 대상 아님 |
| Document Completeness | **PASS** | 필수 섹션 전부 존재, `{{}}`·TODO·TBD 없음. Executive Summary는 Step 9에서 상단에 추가 |

### Decision Compatibility Detail

| 검사 | 상태 | 비고 |
|---|---|---|
| 엔진 + 패턴 | PASS | 풀·asmdef·UniTask·Newtonsoft 전부 2022.3에서 가용. `Awaitable` 미사용 확인 |
| 횡단 관심사 + 엔진 | PASS | `Log` 파사드는 Core(Unity-free), 싱크는 Game이 설치. `[Conditional]` 두 심볼 병기 정상 |
| 구조가 전 시스템 수용 | PASS | S1~S9 + 횡단 관심사 모두 폴더·어셈블리 지정 |
| 결정 간 충돌 | PASS | D1(Addressables 제거) ↔ `BuildAutomator`: Addressables 호출 없음 확인 / D9(Input System 제거) ↔ TMP·Firebase·AdMob: 의존 없음 / D10(물리 유지) ↔ D14(거리 판정): 규약으로 분리 |
| GDD 규약 ↔ 패턴 | PASS | 합산 순서·동시 판정·저장 트리거 7종·오프라인 규칙·연타 방어가 코드 예시로 고정됨 |

### GDD Coverage Detail

**핵심 시스템**

| 시스템 | 아키텍처 지원 | 상태 |
|---|---|---|
| S1 부트·저장 | D4·D5·D6, `Save/`·`Boot/`·`Infrastructure/`, 변경-저장 패턴, `MigrationV1ToV2Tests` | ✅ |
| S2 자동 전투 | D10·D11·D14, `HeroBrain`·`EnemyBrain`·`SpawnScheduler`·`DamageCalc`, 풀 4종, 히트 프레임 릴레이 | ✅ |
| S3 스테이지·챕터 | `StageRunner`·`ProgressionService`·`StageIndex`, 동시 판정 순서, 챕터 순환 | ✅ |
| S4 성장 | `Growth/` 4서비스 + `StatAggregator`(GDD 합산 순서 코드화) | ✅ |
| S5 가챠 | `GachaService` 확률표 + 천장, `IRandom` 주입, `GachaVerifier` | ✅ (확률값·리셋 규칙은 GDD 역반영 시 데이터로 확정) |
| S6 경제·오프라인·광고 | `EconomyService`·`OfflineRewardService`(조작 방어 코드화)·`AdSlotPolicy`(`IClock.LocalNow` 자정 리셋)·`AdService` 실패 → 일반 경로 | ✅ |
| S7 UI | `PanelHost`·`PopupHost`·`BackKeyRouter`·`ToastQueue`·`TapGuardButton`·`BigNumberFormat`·`Strings`·`SafeAreaAdjuster`·`TutorialHints` | ✅ |
| S8 아트·연출·오디오 | D12 에셋 계약 + `SpriteImportPreset`, 풀 VFX, `AudioService`, `lowEffectMode`/`fps30Mode`는 `SettingsService`가 VFX 풀·`targetFrameRate`에 적용 | ✅ |
| S9 시뮬레이터 | D2, `BalanceSimulator` 에디터 툴이 `Formulas` + `BalanceConfig` 공유 | ✅ |

**기술 요구**

| 요구 | 대응 | 상태 |
|---|---|---|
| 60 FPS / 드로우콜 120 / 메모리 400 MB | 풀링(런타임 생성 0), Sprite Atlas 4종, SRP Batcher, `PerfOverlay` 계측 | ✅ 설계 / 실측은 E9 |
| 30분·2시간 방치 안정성 | `Instantiate` 금지 규약 + 풀 크기 상한 + `PerfOverlay` | ✅ |
| 콜드 스타트 8초 | Addressables 제거(카탈로그 페치 없음), Boot 단계 로그로 계측 | ✅ |
| 저장 신뢰성 (강제 종료 20회) | 로컬 백업 선기록 + 디바운스 + Flush, 변경-저장 3단계 | ✅ |
| v1→v2 이관 | ADR-4 값 변환 규칙 + 노드 분리 + 순수 함수 테스트 | ✅ |
| 오프라인 플레이 (네트워크 없음) | `local` 모드 부트 분기, 광고만 Degraded | ✅ |
| 단위 테스트 5영역 | `Tests/EditMode/` 6파일, Core Unity-free | ✅ |
| 시간 조작 방어 | `OfflineRewardService` 예시 코드 + `IClock` 주입 테스트 | ✅ |
| Android API 36 / 16 KB | D13 스파이크 E1-09 선행 | ✅ 결정 / 결과는 실빌드 |
| 한국어 단독 + 문자열 분리 | `Strings.Get(key)` 규약, 코드 내 영문만 | ✅ |
| 확률 공시 (Google Play) | 확률표 = 공시표, `GachaVerifier`로 검증 | ✅ |
| Analytics 이벤트 (E6-15, Should) | `Infrastructure/AnalyticsService` 자리만 확보. 이벤트 집합은 스토리에서 정의 | ⚠️ Should — 의도적 최소 |

### Epic Mapping

| 에픽 | 주 위치 | 적용 패턴 | 상태 |
|---|---|---|---|
| E1 2D 전환 기반 | `Boot/`, `Infrastructure/`, `Save/`, 프로젝트 정리(구조 절 "정리 대상"), D13 스파이크 | 서비스 등록 순서, 변경-저장, 이관 | ✅ |
| E2 전투 코어 | `Core/Combat/`, `Game/Combat/` | 상태 머신, 엔티티 풀, 히트 프레임 | ✅ |
| E3 스테이지·챕터 | `StageRunner`, `Progression/`, `Data/Chapters`, `StageSelectSheet` | 상태 머신, 데이터 미러 | ✅ |
| E4 성장 | `Growth/`, `UI/Panels/{Character,Equipment,Skill}` | 스탯 합산, 변경-저장, Presenter | ✅ |
| E5 가챠 | `Gacha/`, `UI/Panels/GachaPanel`, `Data/Gacha`, `GachaVerifier` | 가챠 패턴, 난수 주입 | ✅ (GDD 역반영 완료) |
| E6 경제·메타 | `Economy/`, `Infrastructure/AdService`, `Popups/OfflineRewardPopup` | 시간 주입, 에러 등급(광고 Degraded) | ✅ |
| E7 UI/UX | `UI/` 전체 | UI 패턴 7종, 연타 방어, 문자열 | ✅ |
| E8 아트·연출 | `Art/`, `Animation/`, `Audio/`, `SpriteImportPreset`, 풀 VFX | 에셋 계약 | ✅ |
| E9 밸런싱·QA·출시 | `Tests/EditMode/`, `BalanceSimulator`, `PerfOverlay`, `BuildConfig`(광고 ID) | 테스트는 Core만 | ✅ (기기 매트릭스·스토어 자산은 수작업) |

### Pattern Completeness

| 시나리오 | 정의 | 예시 |
|---|---|---|
| 엔티티 생성 | ✅ 풀 + `Bind` | `EnemyPool` |
| 컴포넌트 통신 | ✅ 로케이터 + 이벤트 | `Services`, Presenter 템플릿 |
| 상태 관리 | ✅ enum + `Tick` | `HeroBrain`, `StageRunner` |
| 에러 처리 | ✅ `Result` + 경계 try-catch | `UpgradeService`, `LoadAsync` |
| 데이터 접근 | ✅ SO → 미러 | `ContentCatalog.ToDefs` |
| 이벤트 처리 | ✅ 명명·구독·해제 규칙 | `EconomyService` |
| 로깅 | ✅ 파사드 + 태그 | `Log` |
| 설정 | ✅ 종류별 위치 | `BalanceConfig` |
| 비동기 | ✅ UniTask 규약 | — |
| UI 호스트 | ✅ 패널·팝업·토스트 | 표 |

### Coverage Report

**Systems Covered:** 9/9
**Patterns Defined:** 12 (표준) + 0 (고유)
**Decisions Made:** 15 + ADR 7
**Tests Specified:** 6 파일 / GDD 5영역

### Issues Resolved

| # | 이슈 | 해결 |
|---|---|---|
| 1 | C# 9 positional record(`EquipmentDef`)는 .NET Standard 2.1에 없는 `IsExternalInit`이 필요 | `Core/Common/IsExternalInit.cs` 폴리필을 구조에 추가하고 코드 예시에 주석 |
| 2 | 광고 일일 리셋은 **기기 로컬 자정** 기준(GDD)인데 `IClock`이 UTC만 노출 | `IClock.LocalNow` 추가 |
| 3 | 2D Feature Set 2.0.1에 Aseprite Importer 포함 여부 불확실 | "PSD Importer (+ Aseprite Importer 별도 추가 가능)"로 정정 |
| 4 | D1 Addressables 제거 시 `BuildAutomator` 영향 | 소스 확인 — Addressables 호출 없음. `buildAppBundle` 설정만 존재. 영향 없음 |

### Open Items (문서 밖 후속)

| # | 항목 | 담당 단계 |
|---|---|---|
| ~~O1~~ | R9 가챠 방식 변경 GDD 역반영 — **완료.** 확률표 C 55 / R 33 / E 10 / L 2 %, `GACHA_PITY_RESET_ON_LEGENDARY = true` (decision-log D-049~D-051) | 2026-09-20 |
| ~~O2~~ | E1-06 완료 기준 갱신 — **완료** (D-052) | 2026-09-20 |
| O3 | CLAUDE.md 갱신 — Addressables/Input System/JSON 삭제, 구조·규약 반영, JDK 제약 조건부화 | Step 9 Next Steps |
| ~~O4~~ | JDK 11 → 17 전환 여부 — **결정 A: JDK 11 유지** (2026-09-21, 16 KB 로컬 검사 통과) | E1-09 |
| O5 | Higgsfield 생성물 상업 사용권 확인 → Q-7 라이선스 관리표 | E8-01 |

### Validation Date

2026-09-20

---

## Development Environment

### Prerequisites

| 항목 | 요구 | 비고 |
|---|---|---|
| Unity Hub + **Unity 2022.3.62f3** (고정) | Android Build Support + Android SDK/NDK + **OpenJDK 11** 모듈 | 63f1+는 xLTS(유료). **JDK 11 확정** (E1-09 결과 A, 2026-09-21) |
| Android SDK Platform 36 | `Target API Level = 36` (또는 Highest Installed) | 2026-08-31 이후 신규 앱 필수 |
| `Assets/google-services.json` | Firebase Console에서 받아 배치. 커밋 금지 | 없으면 부트가 `local` 모드로 진입 |
| DOTween 설정 | `Tools > Demigiant > DOTween Utility Panel > Setup` + **Create ASMDEF** | asmdef 없으면 `SoloHero.Game`이 참조 못 함 |
| Python 3.10+ / `uv` | MCP for Unity 서버 실행 | `pip install uv` (기존 환경) |
| Node.js 18+ | Context7 (`npx`) | |
| GitHub Actions (주) / Jenkins (휴면) | `unityci/editor:ubuntu-2022.3.62f3-android-3` 직접 `docker run` → `.github/scripts/unity-build.sh`(Licensing Client Personal 활성화 → `BuildAutomator.Build` → 시트 반환). 시크릿 `UNITY_EMAIL`·`UNITY_PASSWORD`만. 러너 디스크 확보 단계 필수 | 2026-09-21 첫 성공(run 35623196814, 22분). Unity가 Personal `.ulf` 수동 활성화를 폐지해 `game-ci/unity-builder`·`UNITY_LICENSE`는 사용 불가. 로컬 Jenkins 미운영, `Jenkinsfile` 참고용 |

### AI Tooling (MCP Servers)

| MCP Server | Purpose | Install Type |
|---|---|---|
| MCP for Unity (`CoplayDev/unity-mcp` v10.0.0) | 에디터 안 씬·프리팹·컴포넌트·스크립트 조작, 콘솔 읽기, 테스트 실행 | Unity Package Manager git URL + 로컬 Python 서버 |
| Context7 (`upstash/context7`) | Unity 2022.3 API 문서 실시간 조회 | `npx` |
| Higgsfield MCP (호스팅) | 배경·아이콘·UI·스토어 자산 이미지 생성 (선택·실험). 캐릭터 시트는 무료팩 유지 | HTTP MCP, 계정 + 크레딧 |

**Setup**

```bash
# 1. MCP for Unity — Unity Package Manager > + > Add package from git URL
#    https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
#    then: Window > MCP for Unity > Configure All Detected Clients  (Claude Code 자동 등록)

# 2. Context7
claude mcp add context7 -- npx -y @upstash/context7-mcp

# 3. Higgsfield (optional)
claude mcp add --transport http higgsfield https://mcp.higgsfield.ai/mcp
```

### Setup Commands

```bash
git clone <repo> solohero && cd solohero
# Unity Hub: 2022.3.62f3 (Android + OpenJDK 11) → 프로젝트 열기 → 첫 임포트 대기
# Unity: Tools > Demigiant > DOTween Utility Panel > Setup DOTween → Create ASMDEF
# Unity: Edit > Project Settings > Player > Android > Target API Level 36, Active Input Handling = Input Manager (Old)  (D9 적용 후)
# 배치: Assets/google-services.json
# 검증: Window > General > Test Runner > EditMode > Run All
```

### First Steps

1. **E1-09 스파이크 (D13)** — 62f3 + SDK Platform 36(타깃 API 36) + Firebase/AdMob 최신으로 AAB 빌드 → Play Console 내부 테스트 트랙에 올려 16 KB 검사 통과 확인. 실패 시 AGP/Gradle/JDK 상향 결정 후 CLAUDE.md 갱신.
2. **E1-03 정리** — 구조 절 "정리 대상" 표대로 이동·삭제. Addressables·Input System 패키지 제거, `Resources`·`StreamingAssets/JSON` 삭제.
3. **E1 골격** — asmdef 4개 + `Core/Common`(Result·Log·Services·IClock·IRandom·IsExternalInit) + `Formulas` + `BalanceConfig` SO + EditMode 테스트 프로젝트. 테스트 1개가 녹색이 되는 것이 골격 완료 기준.
4. **MCP 설정** — 위 AI Tooling 절.
5. **GDD 역반영 (O1·O2)** — `gds-gdd` 업데이트 모드로 가챠 방식·E1-06 문구·상수표(가챠 4행) 갱신. **이것이 끝나기 전에 E5를 시작하지 않는다.**
6. **CLAUDE.md 갱신 (O3)** — 이 문서의 구조·결정·규약을 반영. 3D 시절 서술(Addressables 계획, Input System Both, JSON 로드, 킬 공식) 삭제.

