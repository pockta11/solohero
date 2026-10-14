# 365일 장기 시뮬레이션 (2026-10-14)

`_bmad-output/planning-artifacts/roadmap/content-roadmap-2026-10-14.md` §2(현재 예상 플레이)와 §2.6(레버 실험)의 원자료다. 기준 값은 D-150(커밋 `1c7af57`).

- `driver/`: Unity Mono csc로 Core와 함께 컴파일하는 임시 드라이버
  - `driver.cs`: 6경로 × 시드, `--days=N --days --seeds=N --ads --path=knight`, `FIELD=value`로 `BalanceValues` 덮어쓰기
  - `axes.cs`: 강화·스킬·장비·지출 스냅숏
  - `axes2.cs`: 소환 단계·정체(`--ads`, `--heavy` = 하루 3시간)
  - `piecewise_snippet.cs.txt`: 레버 실험용 `Formulas.EnemyHp/EnemyAtk` 교체 코드(환경변수 `SEG_START`·`SEG_GROWTH`)
  - **빌드하면 `files.txt`·`*.exe`가 같은 폴더에 생기므로 스크래치 폴더에 복사해서 쓴다.** 방법은 계획 문서 부록 B
- `raw/`: 출력 원문 (파일 설명은 계획 문서 부록 B)
