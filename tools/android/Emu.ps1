<#
.SYNOPSIS
  Android emulator helper for SoloHero (16 KB page-size Pixel, Android 15 / API 35, SwiftShader).

.DESCRIPTION
  Uses the user-local SDK root created in story 1-09 (C:\Users\user\android-sdk-tools).
  Why this config: the API 36.1 16 KB image crash-loops surfaceflinger on emulator 37.1,
  and host GPU mode asserts (hasReadColorBufferDma) - API 35 + swiftshader_indirect is stable.

.EXAMPLE
  pwsh tools/android/Emu.ps1 start            # boot AVD solohero16k35 and wait for boot_completed
  pwsh tools/android/Emu.ps1 install          # bundletool: Builds/game.aab -> device split APKs -> install
  pwsh tools/android/Emu.ps1 run              # launch UnityPlayerActivity, tail [Spike]/Unity/crash lines for 30 s
  pwsh tools/android/Emu.ps1 shot out.png     # screenshot
  pwsh tools/android/Emu.ps1 stop
#>
param(
    [Parameter(Mandatory)] [ValidateSet("start", "install", "run", "shot", "logcat", "stop")] [string] $Cmd,
    [string] $Arg = "",
    [string] $Avd = "solohero16k35",
    [string] $Package = "com.SoloSoft.solohero",
    [string] $Aab = "Builds/game.aab",
    [string] $Root = "C:\Users\user\android-sdk-tools",
    [string] $Java = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe"
)
$ErrorActionPreference = "Stop"
$env:ANDROID_SDK_ROOT = $Root; $env:ANDROID_HOME = $Root
$adb = Join-Path $Root "platform-tools\adb.exe"
$emu = Join-Path $Root "emulator\emulator.exe"
$jar = Join-Path $Root "bundletool-all.jar"

switch ($Cmd) {
    "start" {
        Start-Process -FilePath $emu -ArgumentList @("-avd", $Avd, "-no-snapshot", "-gpu", "swiftshader_indirect", "-no-boot-anim") `
            -RedirectStandardOutput (Join-Path $Root "emulator.log") -RedirectStandardError (Join-Path $Root "emulator.err") -WindowStyle Normal
        for ($i = 0; $i -lt 72; $i++) {
            Start-Sleep -Seconds 5
            $b = (& $adb -s emulator-5554 shell getprop sys.boot_completed 2>$null)
            if ("$b".Trim() -eq "1") { break }
        }
        Start-Sleep -Seconds 15
        "booted: sdk $(& $adb shell getprop ro.build.version.sdk) | PAGE_SIZE $(& $adb shell getconf PAGE_SIZE) | abilist $(& $adb shell getprop ro.product.cpu.abilist)"
    }
    "install" {
        & $Java -jar $jar build-apks --bundle=$Aab --output=Builds/device.apks --connected-device --adb=$adb --overwrite | Select-Object -Last 1
        & $Java -jar $jar install-apks --apks=Builds/device.apks --adb=$adb | Select-Object -Last 1
        "installed: $(& $adb shell pm list packages | Select-String $Package)"
    }
    "run" {
        & $adb logcat -c
        & $adb shell am start -n "$Package/com.unity3d.player.UnityPlayerActivity" | Out-Null
        Start-Sleep -Seconds 30
        "alive: $((& $adb shell pidof $Package) -ne $null)"
        & $adb logcat -d | Select-String "\[Spike\]|\[Boot\]|FATAL|Fatal signal|SIGSEGV|SIGABRT|Unity   : E" | Select-Object -First 60 | ForEach-Object { $_.Line.Substring([Math]::Min(31, $_.Line.Length)) }
    }
    "shot" {
        $out = if ($Arg) { $Arg } else { "Builds/screenshot.png" }
        & $adb shell screencap -p /sdcard/shot.png; & $adb pull /sdcard/shot.png $out | Out-Null
        "saved $out"
    }
    "logcat" {
        & $adb logcat -d | Select-String "Unity|$Package" | Select-Object -Last 80 | ForEach-Object { $_.Line }
    }
    "stop" {
        & $adb emu kill 2>$null | Out-Null
        "stopped"
    }
}
