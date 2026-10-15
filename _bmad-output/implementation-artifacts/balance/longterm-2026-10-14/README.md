# 365일 장기 시뮬레이션 (2026-10-14)

`_bmad-output/planning-artifacts/roadmap/content-roadmap-2026-10-14.md` §2(현재 예상 플레이)와 §2.6(레버 실험)의 원자료다. 기준 값은 D-150(커밋 `1c7af57`).

- `driver/`: Unity Mono csc로 Core와 함께 컴파일하는 임시 드라이버
  - `driver.cs`: 6경로 × 시드, `--days=N --days --seeds=N --ads --path=knight`, `FIELD=value`로 `BalanceValues` 덮어쓰기
  - `axes.cs`: 강화·스킬·장비·지출 스냅숏
  - `axes2.cs`: 소환 단계·정체(`--ads`, `--heavy` = 하루 3시간)
  - `piecewise_snippet.cs.txt`: 레버 실험용 `Formulas.EnemyHp/EnemyAtk` 교체 코드(환경변수 `SEG_START`·`SEG_GROWTH`)
  - **빌드하면 `files.txt`·`*.exe`가 같은 폴더에 생기므로 스크래치 폴더에 복사해서 쓴다.** 방법은 계획 문서 부록 B
- `raw/`: 출력 원문 (파일 설명은 계획 문서 부록 B)

## D-151~D-156 작업 (같은 날, 로드맵 P0 구현)

- `driver/lt.cs`: 한 회(페르소나·경로·시드) 365일을 돌려 날별·첫 클리어별 원자료를 덤프(`--persona=light|ads|heavy --path=knight --seed=1 --days=365 --out=FILE`, `FIELD=value`로 상수 덮어쓰기). Core와 함께 Mono csc로 빌드
- `driver/runall.sh <outdir> <persona> <seeds> [FIELD=value]`: 6경로 × 시드를 12개씩 병렬로(라이트 약 5분, 헤비 약 12분)
- `driver/analyze.py <dir>`: 날별 진도·레벨·탑 범위와 12회 중앙값, `driver/stalls.py <dir>`: 기간별 최장 정체·일반 실패율·보스 실패 중 사망 비율
- `driver/cpfit2.py <start> <minRuns> <dir>`: 권장 전투력 두 구간 적합(첫 클리어 전투력 중앙값)
- `driver/mkprobe.py` + `driver/combo.sh`: 보스 실패 순간의 영웅 체력·보스 공격과 장 보스 첫 격파 때의 체력 비율을 찍는 진단 빌드(`probe.exe`)
- `driver/par7.sh` + `driver/digest7.py`: 7일 표준 체크(6경로 × 3시드 병렬, `driver.cs` 빌드 필요)
- `raw/d151_*.txt`: 실험별 요약(`r110`·`r111` = 공격 1.21에 후반 1.10·1.11, `cA~cD` = 공격 조합 3경로, `e*`·`f*` = 공격 1.17/1.07에 후반 체력 1.115~1.13, `a1125`·`h1125` = 광고·헤비, `final_light` = 레벨·단계 보상까지 넣은 최종), `d151_ratio_*` = 장별 영웅 체력 ÷ 일반 적 공격, `d151_bossdeath_probe_knight.txt` = 진단 빌드 출력
