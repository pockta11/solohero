---
title: 'Game Architecture'
project: 'SoloHero'
date: '2026-09-20'
author: 'Tae-jun'
version: '1.0'
stepsCompleted: [1]
status: 'in-progress'

# Source Documents
gdd: '_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/gdd.md'
epics: '_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/epics.md'
decision_log: '_bmad-output/planning-artifacts/gdds/gdd-solohero-2026-09-20/decision-log.md'
brief: null
---

# SoloHero — Game Architecture

## Document Status

GDS 아키텍처 워크플로로 작성 중이다.

**완료 단계:** 1 / 9 (초기화)

**이 문서가 반드시 닫아야 할 이월 항목** — GDD가 아키텍처 단계로 명시 이월한 4건이다.

| ID | 이월 항목 | 출처 |
|---|---|---|
| C-004 | 에셋 전달 방식: Addressables + Firebase Hosting 유지 여부 | `decision-log.md` 미해결 사항 |
| C-007 | 데이터 정의 방식: JSON(StreamingAssets) vs ScriptableObject | `decision-log.md` 미해결 사항 |
| — | 큰 수 표현 타입 (강화가 승산·무상한이 되어 필요) | `gdd.md` Dependencies |
| — | 저장 노드 명칭 전환 및 v1→v2 이관 방식 | `gdd.md` Dependencies |

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

_내용은 워크플로 진행에 따라 추가된다._
