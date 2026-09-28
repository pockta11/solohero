<#
.SYNOPSIS
  One-click play on the emulator (desktop shortcut target): boots AVD solohero16k35 if it is not running,
  keeps the screen on and launches SoloHero.

.DESCRIPTION
  Install the emulator build first (x86_64, runs natively - the ARM build is translated on this emulator and
  its ad WebView crashes after ~85 s):  pwsh tools/android/Emu.ps1 install -Aab Builds/game-emulator.aab
  Mouse click = tap, Esc = Android back. Closing the emulator window shuts it down (progress is saved).
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
    Start-Process -FilePath $emu -ArgumentList @("-avd", "solohero16k35", "-gpu", "swiftshader_indirect", "-no-boot-anim") `
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
& $adb -s $serial shell am start -n "$package/com.unity3d.player.UnityPlayerActivity" | Out-Null
Start-Sleep -Seconds 3
