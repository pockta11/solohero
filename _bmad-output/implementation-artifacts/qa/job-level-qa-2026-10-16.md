# 직업·레벨 콘텐츠 출시 전 QA (2026-10-16)

대상: 직업 16종(초보자 · 1차 3 · 2차 6 · 3차 6)과 레벨 게이트 전부(전직 Lv 10/30/60, 스킬 슬롯 Lv 1/5/12/20/30/45, 치명 줄 Lv 15/25, 레벨 보상 10레벨마다, 레벨당 AP 3·특성 1, 직업 특성 페이지 = 3차). 빌드 = 커밋 `90a3ac5`(D-178), 에뮬레이터 `solohero_fast` x86_64 무광고 빌드(14:42, 소스 변경 없음).

## 1. 계획

| 층 | 무엇을 | 어떻게 | 산출물 |
|---|---|---|---|
| A 정적 콘텐츠 | 16 외형 × 6 클립 시트·`Hero_*.asset`, `Game.unity` 배열 순서, 주공격·궁극기·스킬 75종 HD 아이콘, VFX 클립이 ArtBuilder `SkillClips`와 `Art/Vfx`에 있는지, 스킬 효과음이 `SkillFx.SoundOf`에 매핑되는지, 숙련 스탯마다 팝업 문구·표 키 | 새 EditMode 테스트 `JobContentTests`(6) — 저장소 파일을 읽는다(StringsTests 방식) | Mono/Unity 테스트 |
| B 규칙·동작 | 6경로 × Lv 1~100 매 레벨: 전직 가능 시점·거부, 저장 1회, 전직 후 HP/ATK/DEF ≥ ×1.05, 슬롯 해금 수, 장착 스킬 전부 사용 가능·빈 슬롯 없음, 치명 줄, AP 총량·자동 분배, 특성 포인트, 직업 특성 페이지 개방, 스탯 유한, 궁극기 슬롯·주공격 사거리 / 16 직업 헤드리스 전투(주공격 타격·화상·기절·궁극기 발동·일반 1-1과 보스 1-10 클리어) / 스킬 소환 9 직업 × 소환 단계 20 = 1,800회 10연 전부 자기 계열 / 용어 치환(4 계열 × 직업·스킬·특성·팝업 문구) / Lv 1→100 경험치 레벨업 보상·AP / 알 수 없는 jobId·레벨보다 앞선 직업 저장 | 새 EditMode 테스트 `JobLevelQaTests`(7) | Mono/Unity 테스트 |
| B' 곡선 | 6경로 × 2시드 × 40일 라이트 무광고 시뮬: 전직 도달일, 1·3·7·14·30일 스테이지, GDD 검사 | Mono 드라이버 `jobdays.cs`(`BalanceSimulator.Run`) | 아래 표 |
| C 실기(에뮬레이터) | 레벨 게이트 UI(잠금 문구·힌트 카드·특성 포인트), 전직 팝업 3→2→1장·확정·리본·외형 교체·스킬 자동 장착·HUD 직업명, 2차 궁극기 슬롯·컷인, 3차 스킬 배지·직업 특성 페이지, 16 직업 전투 외형·주공격 VFX, 마법사·궁수 용어(주문력·지능·지팡이 / 민첩·활), 예외 로그 0 | 시드 저장(`seedjob.ps1`) + 탭 스크립트 + 스크린샷 | `qa/` 스크린샷, 아래 표 |

## 2. 결과

(진행 중 — 아래 절이 채워진다)

### 2.1 A·B 테스트

Mono(Unity 없이 Core + EditMode 컴파일, `MonoBleedingEdge csc`) 기준 — 기존 479 + 신규 13 = **492 통과 / 0 실패**. Unity `-runTests -testPlatform EditMode` 결과는 §2.4.

| 테스트 | 검사 | 결과 |
|---|---|---|
| `JobContentTests.Art_EveryJobLook_HasSixClipSheetsAndItsCharacterAsset` | 16 외형 × idle/run/attack/hit/dead/skill 시트 + `Data/Art/Hero_*.asset` | PASS |
| `JobContentTests.Scene_Game_WiresJobLooksInCatalogOrder` | `Game.unity`의 `CombatWorldView._jobArts`·`JobPresenter._arts` 16칸이 `JobCatalog.All` 순서의 asset guid와 일치 | PASS |
| `JobContentTests.Icons_EveryMainAttackUltimateAndSkill_HasAnHdIcon` | 스킬 63 + 궁극기 12 + 주공격 15 HD 아이콘 | PASS |
| `JobContentTests.Vfx_EveryMainAttackUltimateAndSkillClip_IsBuiltByArtBuilderAndOnDisk` | 쓰이는 VFX 이름 전부 ArtBuilder `SkillClips`에 있고 `Art/Vfx/vfx{name}_play*.png` 존재 | PASS |
| `JobContentTests.Sounds_EverySkillAndUltimateSound_IsMappedInSkillFx` | 스킬·궁극기 `Sound` 값 8종이 `SkillFx.SoundOf`에 매핑(기본값 제외 오타 없음) | PASS |
| `JobContentTests.Strings_EveryMasteryStat_HasAJobStatLineInThePopupAndTheTable` | `JobPresenter.MasteryKeys`가 `TalentStat` 16개와 길이 일치, 숙련에 쓰인 11 스탯 전부 문구·표 키 존재, 직업 특성 15 노드 이름·설명 | PASS |
| `JobLevelQaTests.Catalog_JobTree_SixteenJobsWithConsistentTiersParentsLinesAndLooks` | 16종, 티어 1/3/6/6, 부모·계열 일관, 1차마다 2차 2개, 궁극기 고유·전설·자기 계열·도감 스킬과 겹치지 않음 | PASS |
| `JobLevelQaTests.EveryPath_LevelOneToHundred_GatesLoadoutStatsAndPointsHold` | 6경로 × 100레벨 = 600 레벨 상태(본문 B열) | PASS |
| `JobLevelQaTests.EveryJob_HeadlessStage_MainAttackHitsEffectsLandUltimateFiresAndStagesClear` | 16 직업 헤드리스 전투: 주공격 ≥ 10타, 스킬 발동, 2차 이상 궁극기 발동, 화염술사·화염 마도사 화상, 빙뢰술사·빙뢰 마도사 기절, 1-1·1-10 클리어 | PASS |
| `JobLevelQaTests.SkillSummon_EveryLineTierAndSummonLevel_PullsOnlyUsableSkills` | 9 직업(1·2·3차 × 3계열) × 소환 단계 1~20, 10연마다 전부 자기 계열·티어 | PASS |
| `JobLevelQaTests.Terms_EveryJobLine_LeavesNoPlaceholderInJobSkillOrStatText` | 4 계열 용어 세트로 직업·궁극기·스킬 63·직업 특성·팝업 문구에 `{atk}/{main}/{weapon}` 잔류 없음, 마법사 주문력·지능·지팡이, 궁수 민첩·활 | PASS |
| `JobLevelQaTests.LevelUp_OneToHundredThroughExp_PaysEveryTenthRewardOnceAndSettlesAp` | 경험치로 Lv 100: 레벨업 99회, 보상 10회(50 젬·장비권 3·돌파석 10, 50·100은 +200 젬·스킬/펫권 3), AP 297 자동 분배 | PASS |
| `JobLevelQaTests.Save_UnknownJobIdOrJobAheadOfLevel_FallsBackWithoutBreakingTheLoadout` | 알 수 없는 jobId → 초보자, 레벨 1에 3차 직업 저장 16종 → 커먼 지급·장착 수리 멱등·궁극기·숙련 | PASS |

### 2.2 B' 시뮬(라이트 무광고, 40일)

`jobdays.cs`(Mono, `BalanceSimulator.Run`), 6경로 × 2시드. 7일 표준 검사에서 실패한 항목은 기존과 같은 V-7(일반 스테이지 실패율 — 플레이어 모델 의존, 실측 대기)·E9-04(보스 돌파 추가 파밍 비율)뿐이고 경로 간 차이는 없다.

| 경로 | 1차 Lv 10 | 2차 Lv 30 | 3차 Lv 60 | 1일 | 3일 | 7일 | 14일 | 30일 | 40일 (Lv) |
|---|---|---|---|---|---|---|---|---|---|
| 전사→나이트 | 1일 | 5일 | 27·28일 | 2-7 | 3-4 | 4-6 · 4-5 | 5-7 · 5-6 | 8-4 · 8-1 | 10-9 · 10-7 (L75·74) |
| 전사→버서커 | 1일 | 5일 | 25·26일 | 2-7 | 3-4 | 4-7 | 5-8 | 8-10 · 8-8 | 11-9 · 10-8 (L81·77) |
| 마법사→화염술사 | 1일 | 5일 | 24일 | 2-7 · 2-6 | 3-6 · 3-5 | 4-9 · 4-8 | 6-1 | 9-5 · 9-7 | 12-3 · 11-6 (L82·81) |
| 마법사→빙뢰술사 | 1일 | 5일 | 24일 | 2-7 · 2-6 | 3-6 · 3-5 | 4-9 | 6-1 · 5-10 | 9-9 | 11-9 (L83·80) |
| 궁수→레인저 | 1일 | 5일 | 25일 | 2-7 · 2-6 | 3-5 · 3-6 | 4-9 · 4-8 | 6-2 · 6-3 | 8-7 · 8-4 | 10-4 · 9-10 (L74) |
| 궁수→저격수 | 1일 | 5일 | 27·29일 | 2-7 · 2-6 | 3-5 · 3-6 | 4-7 · 4-8 | 5-10 | 8-5 · 7-6 | 9-5 · 9-4 (L71·70) |

- 7일 곡선 4-5~4-9는 GDD 띠(1일 2-5~2-9 · 7일 4-4~4-10) 안, 경로 간 최대 4스테이지 차이.
- 3차 전직은 라이트 무광고 24~29일(365일 원자료 `p1_final_light`의 30일 Lv 61~71과 일치), 광고 시청 시 더 이름.

### 2.3 C 에뮬레이터

`solohero_fast`(1080×2400, host GPU), 저장 시드 31회 + 탭 스크립트, 스크린샷 약 150장(세션 스크래치 `qa/`). 모든 시드·탭에서 Unity 예외/오류 로그 0줄.

| 항목 | 확인 | 결과 |
|---|---|---|
| 새 저장(초보자 Lv 2) | HUD 궁극기 칸 "2차 전직" 잠금, 스킬 슬롯 2~6 "Lv 5/12/20/30/45", 캐릭터 창 버튼 "전직 Lv10", 치명 줄 "Lv 15/25에 열려요", 스킬 창 힌트 카드 "Lv 10 전직 후 직업 스킬 20종 해금!" + "Lv 10 필요", 특성 포인트 1 | PASS |
| 초보자 Lv 10 | 버튼 "전직 가능!", 스킬 창 힌트 버튼 "전직하기", 팝업 "현재 직업: 초보자 · Lv 10에 1차 전직" + 전사·마법사·궁수 3장(초상·주공격·숙련·설명) | PASS |
| 1차 전직 실행 | 선택 → "정말 전직" → 확정: 리본 "전사 전직!" + 전투력 상승 띠(1.08K → 1.2K) + 팡파레, HUD 이름·초상 전사, 전투 외형 knight1, 슬롯에 전사 커먼 자동 장착(연속 베기·방패 강타), 캐릭터 창 "전사 Lv 10 / 전직 Lv30", 스킬 창 도감 4/20 | PASS |
| 2차 팝업(전사·마법사·궁수 Lv 30) | 2장씩(나이트·버서커 / 화염술사·빙뢰술사 / 레인저·저격수), 창 높이 2장에 맞춤, 마법사 카드는 "주문력" 표기, 궁수 치명타 19.7%·공속 1.43(숙련 반영) | PASS |
| 3차 팝업(2차 6종 Lv 60) | 1장씩(팔라딘·워로드·화염 마도사·빙뢰 마도사·보우마스터·신궁), "전직 가능!", HUD 궁극기 칸 활성·쿨타임 순환, 직업 특성 페이지 "3차 전직(Lv 60) 후 열립니다" + 5노드 잠금 | PASS |
| 16 직업 전투(Lv 60, 5-5) | 외형 16종 모두 자기 시트(초보자 맨머리 → 전직 단계마다 갑옷·장식), 주공격 VFX(섬광 파동·볼트·창·화염·얼음), 스킬 이름 띠·전설 리본, 2차 이상 궁극기 아이콘·발동(생츄어리 십자, 메테오 스톰, 빙뢰 폭풍, 애로우 스톰 …), 캐릭터 창 이름·주문력/공격력 용어·숙련 수치(치명 19.7/22.7/24.7 %, 공속 1.43/1.54/1.65, 치명 피해 246~296 %), 3차는 "직업 정보" + "(최종 전직)" 팝업 | PASS |
| 3차 직업 특성 | 6종 모두 페이지 개방("전사 특성 · 랭크당 2포인트"), 배우기 → 1/6, 포인트 59 → 57 | PASS |
| 레벨 게이트 | Lv 15 치명타 줄 개방·치명 피해 잠금, Lv 25 둘 다 개방·특성 24, Lv 45 슬롯 6개 전부 개방, HUD·스킬 창 잠금 문구 일치 | PASS |
| 용어 | 화염술사 스탯 창 "지능 78 · 주문력 기본값 +195%", 장비 창 "보유 효과 주문력", 무기 칸 지팡이; 레인저 "민첩", 무기 칸 활 | PASS |
| 3차 스킬 배지 | 나이트 Lv 60 스킬 도감을 내리면 마지막 줄 4칸이 흐리게 + "3차" 배지 | PASS |
| 뒤로가기 | 전직 팝업이 열린 채 BACK → 팝업만 닫힘(종료 확인 없음) | PASS |
| 잠긴 궁극기 칸 탭 | 1차 전사 Lv 12에서 궁극기 칸 탭 → 토스트 "Lv 30 2차 전직 후 궁극기를 쓸 수 있어요" | PASS |
| 3차 전직 실행(나이트 → 팔라딘) | 1장 팝업 선택 → "정말 전직" → 확정: HUD 이름·초상 팔라딘, 전투력 상승 띠(33.4M → 36.1M), 전직 리본 뒤 "도감 세트 완성!" 리본(컬렉션 세트), 외형 knight5, 스킬 도감 9 → 10/20(3차 커먼 지급), 캐릭터 창 "직업 정보", 직업 특성 페이지 개방 → 배우기 1/6(59 → 57) | PASS |
| 궁극기 컷인 | 나이트 수호의 십자가 시전 때 "궁극기 · 수호의 십자가" 컷인 띠 + 직업 초상 | PASS |

캡처 노이즈: host GPU 에뮬레이터에서 간헐적으로 검은 프레임(약 150장 중 4장) — 앞뒤 프레임으로 확인, 앱 결함 아님.

### 2.4 Unity EditMode

`Unity.exe -batchmode -runTests -testPlatform EditMode`: **492 / 492 통과**(신규 `JobLevelQaTests` 7 + `JobContentTests` 6 포함, 기존 `JobTests` 10·`TalentTests`·`SkillCollectionTests`·`StringsTests` 그대로). 실행이 건드린 `ProjectSettings.asset`은 줄바꿈 변경뿐이라 되돌렸다.

### 2.5 도구

- 테스트: `Assets/SoloHero/Scripts/Tests/EditMode/JobLevelQaTests.cs`, `JobContentTests.cs`(저장소에 남김 — 직업·외형·아이콘·VFX·씬 연결 회귀 검사)
- 시뮬 드라이버·시드 스크립트·탭 스크립트·스크린샷: 세션 스크래치(`sim/jobdays.cs`, `seedjob.ps1`, `tap.sh`, `burst.sh`, `qa_chain.sh`, `post_chain.sh`, `qa/`). 좌표(1080×2400): 탭 y 2320 · x 110/330/540/760/970, 캐릭터 창 전직 버튼 (910, 1494), 스탯 페이지 (560, 1690), 직업 특성 탭 (650, 1487), 배우기 (910, 2148), 팝업 X (1008, 496), 3장 팝업 선택 y 946/1356/1770, 1장 팝업 선택 (828, 1360)
- 에뮬레이터 운영 주의: Claude 백그라운드 작업에서 띄운 에뮬레이터는 작업이 끝나면 함께 죽는다 → WMI `Win32_Process.Create`로 분리 실행. adb 서버가 파이프를 물려받으면 `| tail` 파이프라인이 영원히 기다린다 → 먼저 `adb start-server >/dev/null 2>&1 </dev/null`

## 3. 발견 사항

출시를 막는 결함은 없다. 아래는 관찰·개선 후보(순서 = 권장 우선순위).

| # | 내용 | 영향 | 제안 |
|---|---|---|---|
| O-1 | 전직 분석 이벤트가 없다. `job_final`(3차 도달 축 상한)만 있고 어떤 직업을 언제 골랐는지는 기록되지 않는다 | 출시 후 계열 선호·2차/3차 도달일을 알 수 없음 | **반영됨(D-179)**: `JobPresenter.Pick`이 `job_advance {job, tier, level}`을 보낸다 |
| O-2 | 전직 팝업 카드 문구가 **현재** 직업의 용어 세트를 쓴다. 초보자가 보는 마법사 카드는 "매직 볼트 대상 2명 · 공격력 280%", "공격력 +3%"로 표기되고 전직 뒤에야 주문력이 된다 (D-140 설계대로) | 선택 전 마법사 카드만 어색, 기능 영향 없음 | 유지. 바꾸려면 `Strings.Term(name, set)` 오버로드로 카드별 계열 용어를 쓰면 된다 |
| O-3 | 가이드 퀘스트는 1차·2차 전직(`GuideKind.JobTier` 1·2)까지만 안내하고 3차(Lv 60, 라이트 24~29일)는 없다 | 3차 전직은 캐릭터 창 버튼 "전직 가능!"·스킬 창 3차 배지로만 안내 | 선택. 반복 구간에 `JobTier 3` 퀘스트 1개 추가 가능 |
| O-4 | 1차 주공격의 한 번 휘두름 총량은 마법사(2명 × 280%)·궁수(2명 × 140% × 2회)가 초보자 섬광 베기(3명 × 300%)보다 낮다. 사거리 4.0/5.0으로 먼저 교전하고 계열 스킬이 보완하며, 시뮬 7일 곡선은 세 계열이 같다 | 없음(수치 설계) | 유지 |

## 4. 판정

**직업·레벨 콘텐츠는 출시 가능.** 16 직업 × 레벨 1~100의 규칙(Core 테스트 600 레벨 상태 + 헤드리스 전투 16종), 콘텐츠 파일(외형 96 시트·아이콘 90·VFX·효과음·씬 연결), 곡선(6경로 7일 4-5~4-9, 3차 전직 24~29일), 실기 UI(전직 1·2·3차 팝업과 실행, 궁극기, 특성, 게이트 문구, 용어)에서 결함 0. 남은 것은 §3의 선택 개선(O-1 전직 분석 이벤트 권장)과 실기기 확인(release-gate B-001)뿐이다.
