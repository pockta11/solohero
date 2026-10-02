<#
.SYNOPSIS
  One-click play on the emulator (desktop shortcut target): boots AVD solohero16k35 if it is not running,
  keeps the screen on and launches SoloHero.

.DESCRIPTION
  Install the emulator build first: x86_64 (runs natively) and without the ad SDK, whose WebView crashes the
  app on this emulator. Build with SOLOHERO_X86_64=1 SOLOHERO_NO_ADS=1 (BuildAutomator.Build), copy
  Builds/game.aab to Builds/game-emulator.aab, then:  pwsh tools/android/Emu.ps1 install -Aab Builds/game-emulator.aab
  Ad buttons stay on screen in that build but do nothing.
  Mouse click = tap, Esc = Android back. Closing the emulator window shuts it down (progress is saved).
  Always a cold boot (-no-snapshot): a Quick Boot snapshot restores the app process that was running when it was
  saved (an old build) and SwiftShader loses its textures on restore (rainbow stripes).
#>
$ErrorActionPreference = "Stop"
$root = "C:\Users\user\android-sdk-tools"
$adb = Join-Path $root "platform-tools\adb.exe"
$emu = Join-Path $root "emulator\emulator.exe"
$serial = "emulator-5554"
$package = "com.SoloSoft.solohero"
$env:ANDROID_SDK_ROOT = $root; $env:ANDROID_HOME = $root

function Say([string]$text) { Write-Host ("[SoloHero] " + $text) }

$running = (& $adb devices) -match "^$serial\s+device"
if (-not $running) {
    Say "Starting the emulator (1-2 minutes)..."
    Start-Process -FilePath $emu -ArgumentList @("-avd", "solohero16k35", "-no-snapshot", "-gpu", "swiftshader_indirect", "-no-boot-anim") `
        -RedirectStandardOutput (Join-Path $root "emulator.log") -RedirectStandardError (Join-Path $root "emulator.err")
    $booted = $false
    for ($i = 0; $i -lt 90; $i++) {
        Start-Sleep -Seconds 4
        if ("$(& $adb -s $serial shell getprop sys.boot_completed 2>$null)".Trim() -eq "1") { $booted = $true; break }
    }
    if (-not $booted) { Say "The emulator did not boot. Close its window and try again."; Start-Sleep 8; exit 1 }
    Start-Sleep -Seconds 10
}

& $adb -s $serial shell svc power stayon true | Out-Null
& $adb -s $serial shell settings put system screen_off_timeout 2147483647 | Out-Null
if (-not (& $adb -s $serial shell pm list packages $package)) {
    Say "The game is not installed: pwsh tools/android/Emu.ps1 install -Aab Builds/game-emulator.aab"
    Start-Sleep 10
    exit 1
}

Say "Launching SoloHero."
# A fresh process every time, so a running emulator never shows a stale one.
& $adb -s $serial shell am force-stop $package | Out-Null
& $adb -s $serial shell am start -n "$package/com.unity3d.player.UnityPlayerActivity" | Out-Null
Start-Sleep -Seconds 3
