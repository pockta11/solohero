# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SoloHero** — a solo-developed idle/AFK RPG for Android (Google Play Store). Core mechanics are offline gold rewards (UTC timestamp-based, capped at 6 hours) and a Gaussian-distribution gacha system. Monetized via AdMob rewarded interstitial ads.

- **Engine**: Unity 2022.3.62f3 LTS, C#
- **Platform**: Android (AAB output)
- **Backend**: Firebase Realtime Database, Authentication, Analytics
- **Asset delivery**: Unity Addressables with Firebase Hosting as remote path

## Build Commands

### Jenkins (local Windows)
Jenkins runs Unity in batchmode. The Jenkinsfile calls:
```
"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -quit -batchmode -projectPath "%WORKSPACE%" -executeMethod BuildAutomator.Build -logFile Builds/build.log
```
Output: `Builds/game.aab`

### GitHub Actions (CI)
Triggered on push to `main` or manually. Uses GameCI `unity-builder@v4` on `ubuntu-latest`.
Requires repository secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
Build artifact uploaded as `android-aab` with 7-day retention.

### Manual build in Unity Editor
`Tools > Build > Android AAB` runs `BuildAutomator.Build` which builds LoginScene + GameScene into `Builds/game.aab` with AAB mode enabled.

## Architecture

### Current state
The project is in early development — there are ~24 custom `.cs` files. Most code in `Assets/` belongs to third-party SDKs (Firebase, AdMob, DOTween, EDM4U).

### Key custom scripts
- `Assets/Editor/BuildAutomator.cs` — Unity Editor build pipeline entry point; called by both Jenkins and GitHub Actions via `-executeMethod BuildAutomator.Build`
- `Assets/Scripts/UI/SafeAreaAdjuster.cs` — handles notch/cutout safe area for mobile UI

### Planned architecture patterns (from project docs)
- **UI**: MVP (Model/Presenter/View) separation; ScriptableObjects as Models
- **Combat**: FSM via `StateMachineBehaviour`, `IDamageable` interface, Physics2D detection, object pooling for monsters and effects
- **Gacha**: Gaussian distribution with pity (ceiling) system; drop tables per equipment slot; custom Editor for weight visualization
- **Offline rewards**: Save UTC on `OnApplicationPause`/`OnApplicationQuit`, calculate elapsed seconds on resume (max 21,600 s = 6 hours)
- **Data persistence**: DTO pattern → JSON → Firebase Realtime Database; auto-save on pause/quit
- **Attributes**: `[Flags]` enum with bitwise operations for 8-bit element/attribute system
- **Async**: UniTask throughout (not Unity coroutines)
- **Tweening**: DOTween for all animations

### Scenes
- `Assets/Scenes/LoginScene.unity` — entry scene (Addressables patch check, auth)
- `Assets/Scenes/GameScene.unity` — main gameplay scene

### SDKs & packages
- **Firebase** (Auth, Realtime DB, Analytics) — managed via EDM4U; `google-services.json` must be in `Assets/`
- **Google Mobile Ads** v25.0.0 — AdMob; App ID in `Assets/Plugins/Android/AndroidManifest.xml`
- **Addressables** 1.22.3 — remote load path points to Firebase Hosting URL
- **UniTask** — async/await (added via Git URL in `Packages/manifest.json`)
- **DOTween** — requires running setup (Window > DOTween Utility Panel > Setup DOTween)
- **URP** 14.0.12 — Universal Render Pipeline
- **Input System** 1.14.2 — configured in **Both** mode (old + new simultaneously)

## Environment Setup Requirements

| Requirement | Note |
|---|---|
| Unity 2022.3.62f3 | Via Unity Hub with Android Build Support + SDK/NDK + **OpenJDK (JDK 11)** |
| JDK version | **Use JDK 11 only** — JDK 21 causes Firebase compatibility issues |
| `google-services.json` | Must be placed in `Assets/` (not committed; get from Firebase Console) |
| DOTween setup | Run setup wizard after import or after clean Library |
| CanvasScaler reference | 1080 × 2340 (portrait phone baseline) |

## Android Build Configuration

Custom Gradle templates are in `Assets/Plugins/Android/`:
- `mainTemplate.gradle` / `settingsTemplate.gradle` — modified by EDM4U to inject Firebase and AdMob dependencies
- `AndroidManifest.xml` — contains AdMob Application ID (`ca-app-pub-1435934257467286~9895276357`)
- `FirebaseApp.androidlib`, `GoogleMobileAdsPlugin.androidlib` — pre-built Android libraries

Java compatibility is set to **Java 11** in the Gradle template. Do not change this to Java 17/21 — it breaks Firebase.

## Development Status (Milestones)

| Milestone | Status |
|---|---|
| Core systems & environment | Done |
| Combat + Gacha | In progress |
| UI · Inventory · Save (MVP/DTO/Firebase) | Planned |
| QA · Balancing · Store release | Planned |
