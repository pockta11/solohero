# Play Console 데이터 보안 양식 답변 (E9-14, 2026-10-08 갱신)

앱이 쓰는 SDK: Firebase Authentication(익명), Firebase Realtime Database, Firebase Analytics, Google Mobile Ads(AdMob) + User Messaging Platform(동의). 인앱 결제 없음. 새 네이티브 SDK는 없다(크래시 수집은 Analytics 이벤트로, D-135).

## 1. 데이터 수집 및 보안

| 질문 | 답 |
|---|---|
| 앱이 필수 사용자 데이터 유형을 수집하거나 공유하나요? | **예** |
| 전송 중 데이터가 암호화되나요? | **예** (Firebase·AdMob 모두 HTTPS/WSS) |
| 사용자가 데이터 삭제를 요청할 수 있는 방법을 제공하나요? | **예** — 앱 안: 설정 → 계정 → **모든 데이터 삭제**(서버 진행 데이터·익명 계정·기기 데이터·분석 ID 초기화, D-134). 앱을 지운 사람은 개인정보처리방침의 이메일로 요청(설정 → 계정의 계정 ID를 알려 주면 처리) |
| 계정 만들기 | 사용자가 만드는 계정은 없다. 진행 저장용 익명 계정이 자동 생성된다 → "앱에서 계정을 만들 수 있나요?"는 **아니요**(익명 인증만) |

## 2. 데이터 유형

| 데이터 유형 | 수집 | 공유 | 선택사항? | 목적 |
|---|---|---|---|---|
| 기기 또는 기타 ID (광고 ID, Firebase 앱 인스턴스 ID, 익명 UID) | 예 | 예 (광고 SDK) | 아니요 | 앱 기능, 분석, 광고 또는 마케팅, 사기 방지·보안 |
| 앱 활동 → 앱 상호작용 (플레이 이벤트) | 예 | 아니요 | 아니요 | 분석 |
| 앱 활동 → 기타 작업 (게임 진행·설정 저장, 기기 이전 코드로 24시간 보관되는 진행 사본) | 예 | 아니요 | 아니요 | 앱 기능 |
| 앱 정보 및 성능 → 비정상 종료 로그, 진단 (C# 예외 요약 `app_exception`, Analytics·AdMob 진단) | 예 | 아니요 | 아니요 | 분석 |
| 위치 → 대략적인 위치 | 예 (AdMob이 IP로 추정) | 예 | 아니요 | 광고 또는 마케팅, 사기 방지 |
| 개인 정보(이름·이메일·전화), 금융 정보, 사진, 연락처, 메시지 | **수집 안 함** | | | |

"공유"는 Google의 AdMob 문서 기준: 광고 SDK가 광고 ID·대략적 위치·기기 정보를 Google로 전송하는 것은 **공유**로 신고한다. Firebase Analytics·Realtime Database는 서비스 제공자 처리로 공유에 해당하지 않는다.

참고 문서: AdMob 데이터 공개 가이드 https://developers.google.com/admob/android/privacy/play-data-disclosure , Firebase https://firebase.google.com/docs/android/play-data-disclosure

## 3. 광고 ID 선언

Play Console → 앱 콘텐츠 → 광고 ID: **예, 광고 ID 사용** — 목적: 광고 또는 마케팅, 분석. (GMA SDK가 `com.google.android.gms.permission.AD_ID` 권한을 병합한다.)
