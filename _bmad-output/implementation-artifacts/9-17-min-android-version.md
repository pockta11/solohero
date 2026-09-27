# Story 9-17: 최소 지원 Android 버전

Status: review (2026-09-28)

**결정: minSdk 24 (Android 7.0), targetSdk 36 (Android 16).** 현재 `ProjectSettings` 값 그대로 확정.

| 근거 | 내용 |
|---|---|
| SDK 하한 | Firebase Unity 13.x·Google Mobile Ads 25.x(play-services-ads)의 요구 minSdk는 23 이하 — 24는 여유 있게 만족 |
| Unity 2022.3 | Android 최소 지원은 API 22 — 제약 아님 |
| 시장 | API 24 이상이 활성 기기의 대부분(Android Studio 배포 통계 기준 약 97% 이상). **출시 직전 Play Console 기기 카탈로그로 재확인** |
| 테스트 부담 | API 23 이하는 16 KB 페이지·최신 WebView 조합과 무관하고 점유율이 낮아 테스트 대상에서 제외 |
| 올릴 조건 | 광고/Firebase SDK가 minSdk를 올리면 그 값을 따른다 (EDM4U 해석 결과 확인) |

targetSdk 36은 2026-08-31부터 신규 앱 필수 (E1-09에서 적용·16 KB 정렬 통과).
