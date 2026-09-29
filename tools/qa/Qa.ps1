# SoloHero emulator QA (E9-07 smoke, E9-08 force kill, E9-09/10 idle, E9-19 memory).
# FPS is not measured here: the emulator renders with SwiftShader, so frame rates say nothing about devices (E9-12).
# Needs the emulator from tools/android/Emu.ps1 (Google APIs image: adb root works) and an installed build.
# Use a QA build without ads on the emulator (SOLOHERO_NO_ADS=1): the ad WebView crashes the translated arm64
# app on this x86_64 16 KB-page image after ~85 s, which is not a game fault.
#
#   pwsh tools/qa/Qa.ps1 smoke -Count 10          # 5 fresh (data cleared) + 5 existing launches
#   pwsh tools/qa/Qa.ps1 forcekill -Count 20      # purchase, kill 0.1-1.0 s later, relaunch, compare the save
#   pwsh tools/qa/Qa.ps1 idle -Minutes 30         # stay in battle; sample memory, progress, liveness
#   pwsh tools/qa/Qa.ps1 coldstart -Count 10      # launch -> "battle ready" log (E1-10, target 8 s)
#   pwsh tools/qa/Qa.ps1 scenarios                # E9-11: v1 save migration, ad failure path, portrait/safe-area shot
#   pwsh tools/qa/Qa.ps1 firstsession             # E9-20: fresh install, scripted novice taps, check at 5 minutes
param(
    [Parameter(Mandatory = $true)][ValidateSet('smoke', 'forcekill', 'idle', 'coldstart', 'scenarios', 'firstsession')][string]$Cmd,
    [int]$Count = 10,
    [int]$Minutes = 30,
    [string]$Serial = 'emulator-5554',
    [string]$OutDir = 'Builds/qa',
    [string]$Only = ''
)

$ErrorActionPreference = 'Stop'
$adbExe = 'C:\Users\user\android-sdk-tools\platform-tools\adb.exe'
$pkg = 'com.SoloSoft.solohero'
$activity = "$pkg/com.unity3d.player.UnityPlayerActivity"
$prefs = "/data/data/$pkg/shared_prefs/$pkg.v2.playerprefs.xml"
$bootSeconds = 25
New-Item -ItemType Directory -Force $OutDir | Out-Null

function Adb { & $adbExe -s $Serial @args }
function Launch { Adb shell am start -n $activity | Out-Null }
function Stop-App { Adb shell am force-stop $pkg | Out-Null }
# Three looks a second apart: a Unity build or an adb server restart can make one pidof come back empty.
function Alive {
    for ($k = 0; $k -lt 3; $k++) {
        if (Adb shell pidof $pkg 2>$null) { return $true }
        Start-Sleep 1
    }
    return $false
}
function Tap([int]$x, [int]$y) { Adb shell input tap $x $y | Out-Null }

function Read-Save {
    $xml = (Adb shell "cat $prefs" 2>$null) -join "`n"
    $m = [regex]::Match($xml, '<string name="player_data_v2">(.*?)</string>', 'Singleline')
    if (-not $m.Success) { return $null }
    return ([uri]::UnescapeDataString([System.Net.WebUtility]::HtmlDecode($m.Groups[1].Value)) | ConvertFrom-Json)
}

# Rewrites the local save while the app is stopped. A higher saveRevision makes the local copy win over the
# remote one on the next boot (SaveService.LoadAsync), which also uploads it.
function Write-Save($save) {
    $json = $save | ConvertTo-Json -Depth 5 -Compress
    $xml = (Adb shell "cat $prefs") -join "`n"
    $encoded = [System.Net.WebUtility]::HtmlEncode([uri]::EscapeDataString($json))
    $xml = [regex]::Replace($xml, '<string name="player_data_v2">.*?</string>', { param($x) "<string name=`"player_data_v2`">$encoded</string>" }, 'Singleline')
    $tmp = Join-Path $OutDir 'prefs.xml'
    [System.IO.File]::WriteAllText((Resolve-Path $OutDir).Path + '\prefs.xml', $xml)
    $owner = (Adb shell "stat -c %u:%g $prefs").Trim()
    Adb push $tmp /data/local/tmp/qa_prefs.xml | Out-Null
    Adb shell "cp /data/local/tmp/qa_prefs.xml $prefs && chown $owner $prefs && chmod 660 $prefs" | Out-Null
}

function Close-Popups { Tap 350 1100; Start-Sleep -Milliseconds 700 }

function Shot([string]$name) {
    Adb shell screencap -p /sdcard/qa_shot.png | Out-Null
    Adb pull /sdcard/qa_shot.png "$OutDir/$name" | Out-Null
}

# Home key = OnApplicationPause(true) = the pause save; waits until the local save exists.
function Pause-Save {
    Adb shell input keyevent KEYCODE_HOME | Out-Null
    for ($k = 0; $k -lt 20; $k++) { Start-Sleep 1; if (Read-Save) { return } }
}

function Unity-Errors {
    $log = Adb logcat -d -s Unity:E Unity:W AndroidRuntime:E libc:F
    return @($log | Select-String -Pattern 'Exception|Fatal signal|FATAL' | Where-Object { $_ -notmatch 'TranslateDllNotFoundException' })
}

# Launch and wait for the "battle ready" marker (CombatSession) instead of a fixed delay: a first launch after an
# install or a data wipe takes 30+ s on this emulator. Falls back after 120 s.
function Wait-Ready([int]$timeoutSec = 120) {
    $appPid = $null
    for ($k = 0; $k -lt $timeoutSec * 2; $k++) {
        Start-Sleep -Milliseconds 500
        if (-not $appPid) { $appPid = (Adb shell pidof $pkg) ; if ($appPid) { $appPid = $appPid.Trim() } ; continue }
        # Only this process's log: a marker left by the previous run must not count.
        if (Adb logcat -d --pid=$appPid -s Unity:I | Select-String 'battle ready') { return $true }
    }
    return $false
}

function Boot {
    Adb logcat -c
    Launch
    if (-not (Wait-Ready)) { Write-Warning 'battle ready not seen within 120 s' }
    Start-Sleep -Seconds 2
    Close-Popups
}

switch ($Cmd) {
    'smoke' {
        Adb root | Out-Null; Start-Sleep 2
        $pass = 0
        for ($i = 1; $i -le $Count; $i++) {
            Stop-App
            $fresh = $i -le [math]::Ceiling($Count / 2)
            if ($fresh) { Adb shell pm clear $pkg | Out-Null }
            Boot
            $alive = Alive
            $errors = Unity-Errors
            Shot "smoke-$i.png"
            Pause-Save
            $save = Read-Save
            $ok = $alive -and $errors.Count -eq 0
            if ($ok) { $pass++ }
            "smoke {0,2} {1,-8} alive={2} errors={3} save={4} -> {5}" -f $i, ($(if ($fresh) { 'fresh' } else { 'existing' })), $alive, $errors.Count, ($null -ne $save), ($(if ($ok) { 'PASS' } else { 'FAIL' }))
            $errors | Select-Object -First 3 | ForEach-Object { "    " + $_.Line }
        }
        "smoke result: $pass / $Count"
    }

    'forcekill' {
        Adb root | Out-Null; Start-Sleep 2
        Boot; Start-Sleep 30; Pause-Save; Stop-App
        $seed = Read-Save
        if ($null -eq $seed) { throw 'no local save to seed' }
        $seed.gold = 1e9
        $seed.saveRevision = [long]$seed.saveRevision + 1000
        Write-Save $seed
        $delays = @(0.1, 0.3, 0.6, 1.0)
        $lost = 0
        for ($i = 1; $i -le $Count; $i++) {
            Boot
            $before = Read-Save
            $gacha = ($i % 2 -eq 0)
            if ($gacha) { Tap 540 1853; Start-Sleep 1.5; Tap 205 1710 } else { Tap 756 1853; Start-Sleep 1; Tap 108 1853; Start-Sleep 1.5; Tap 865 1345 }
            $delay = $delays[$i % $delays.Count]
            Start-Sleep -Milliseconds ([int]($delay * 1000))
            Stop-App
            Boot
            Pause-Save
            $after = Read-Save
            Stop-App
            if ($gacha) { $want = [int]$before.totalPullCount + 1; $got = [int]$after.totalPullCount; $what = 'pulls' }
            else { $want = [int]$before.upgradeHp + 1; $got = [int]$after.upgradeHp; $what = 'upgradeHp' }
            $ok = $got -ge $want -and [int]$after.highestStage -ge [int]$before.highestStage -and @($after.ownedEquipment).Count -ge @($before.ownedEquipment).Count
            if (-not $ok) { $lost++ }
            "kill {0,2} {1,-9} after {2:0.0}s  {3} {4} -> {5} (want {6}) rev {7}->{8}  {9}" -f $i, $what, $delay, $what, ($want - 1), $got, $want, $before.saveRevision, $after.saveRevision, ($(if ($ok) { 'PASS' } else { 'LOST' }))
        }
        "forcekill result: lost $lost / $Count"
    }

    'scenarios' {
        Adb root | Out-Null; Start-Sleep 2
        # 1. v1 -> v2 migration: a fresh install that only has the 3D-era backup key.
        if ($Only -eq '' -or $Only -eq 'migration') {
        Stop-App
        Adb shell pm clear $pkg | Out-Null
        $v1 = '{"gold":1000,"chapter":2,"stageNumber":3,"upgradeHpLevel":3,"upgradeAtkLevel":0,"upgradeDefLevel":0,"upgradeSpdLevel":0,"gachaPullCount":7,"equippedWeapon":"Iron_Sword","ownedEquipmentCsv":"Iron_Sword","dataVersion":1}'
        $xml = "<?xml version='1.0' encoding='utf-8' standalone='yes' ?>`n<map>`n    <string name=`"player_data_backup`">" + [System.Net.WebUtility]::HtmlEncode([uri]::EscapeDataString($v1)) + "</string>`n</map>`n"
        [System.IO.File]::WriteAllText((Resolve-Path $OutDir).Path + '/v1.xml', $xml)
        $uid = (Adb shell "stat -c %u /data/data/$pkg").Trim()
        Adb push "$OutDir/v1.xml" /data/local/tmp/qa_v1.xml | Out-Null
        Adb shell "mkdir -p /data/data/$pkg/shared_prefs && cp /data/local/tmp/qa_v1.xml $prefs && chown -R ${uid}:${uid} /data/data/$pkg/shared_prefs && chmod 771 /data/data/$pkg/shared_prefs && chmod 660 $prefs" | Out-Null
        Boot; Start-Sleep 5; Pause-Save
        $m = Read-Save
        $refund = 100 * 3 * 4 / 2
        $migOk = $m -and [double]$m.gold -ge (1000 + $refund) -and [int]$m.upgradeHp -eq 0 -and [int]$m.highestStage -gt 1 -and $m.equippedSword -ne ''
        "scenario migration: gold {0} (>= {1}), upgradeHp {2}, highestStage {3}, sword '{4}' -> {5}" -f $m.gold, (1000 + $refund), $m.upgradeHp, $m.highestStage, $m.equippedSword, ($(if ($migOk) { 'PASS' } else { 'FAIL' }))
        Stop-App
        }

        # 2. Ad failure (QA build has no ad SDK): tapping the gem ad keeps gems and the daily count, app stays up.
        Boot
        $before = Read-Save
        Tap 96 399; Start-Sleep 2   # D-089 gem ad: second square of the left rail
        Shot 'scenario-ad-fail.png'
        Pause-Save
        $after = Read-Save
        $adOk = $null -ne $before -and $null -ne $after -and [double]$after.gem -eq [double]$before.gem -and [int]$after.adCountA2 -eq [int]$before.adCountA2
        "scenario ad failure: gem {0} -> {1}, count {2} -> {3} -> {4}" -f $before.gem, $after.gem, $before.adCountA2, $after.adCountA2, ($(if ($adOk) { 'PASS' } else { 'FAIL' }))

        # 3. Portrait / safe area: 16:9 and a tall 20:9 screen, every control inside (look at the shots).
        Stop-App; Boot
        Shot 'scenario-portrait-16x9.png'
        Stop-App
        Adb shell wm size 1080x2400 | Out-Null
        Boot
        Shot 'scenario-portrait-20x9.png'
        Stop-App
        Adb shell wm size reset | Out-Null
        "scenario portrait: see $OutDir/scenario-portrait-16x9.png and -20x9.png"
        Stop-App
    }

    'firstsession' {
        Adb root | Out-Null; Start-Sleep 2
        Stop-App
        Adb shell pm clear $pkg | Out-Null
        Boot
        $start = Get-Date
        $pulled = $false
        while (((Get-Date) - $start).TotalSeconds -lt 300) {
            Start-Sleep 15
            # The character panel opens on start; go through the skill tab so the next tap always opens it.
            Tap 756 1853; Start-Sleep 1
            Tap 108 1853; Start-Sleep 1
            Tap 865 1345; Start-Sleep 0.5
            Tap 865 1465; Start-Sleep 0.5
            if (-not $pulled -and ((Get-Date) - $start).TotalSeconds -gt 90) {
                Tap 540 1853; Start-Sleep 1
                Tap 205 1710; Start-Sleep 3
                Tap 540 1300; Start-Sleep 1
                $pulled = $true
            }
        }
        Shot 'firstsession-5min.png'
        Pause-Save
        $s = Read-Save
        $upg = [int]$s.upgradeHp + [int]$s.upgradeAtk + [int]$s.upgradeDef + [int]$s.upgradeSpd
        $ok = [int]$s.highestStage -ge 5 -and $upg -ge 3 -and [int]$s.totalPullCount -ge 1
        "firstsession result: highest stage {0} (>= 5), upgrades {1} (>= 3), pulls {2} (>= 1) -> {3}" -f $s.highestStage, $upg, $s.totalPullCount, ($(if ($ok) { 'PASS' } else { 'FAIL' }))
        Stop-App
    }

    'coldstart' {
        $times = @()
        for ($i = 1; $i -le $Count; $i++) {
            Stop-App
            Start-Sleep 2
            Adb logcat -c
            $t0 = Get-Date
            Launch
            $ready = $null
            $appPid = $null
            for ($k = 0; $k -lt 480 -and -not $ready; $k++) {
                Start-Sleep -Milliseconds 250
                if (-not $appPid) { $appPid = (Adb shell pidof $pkg); if ($appPid) { $appPid = $appPid.Trim() }; continue }
                if (Adb logcat -d --pid=$appPid -s Unity:I | Select-String 'battle ready') { $ready = Get-Date }
            }
            $sec = if ($ready) { ($ready - $t0).TotalSeconds } else { -1 }
            $times += $sec
            "cold {0,2}: {1:0.0} s" -f $i, $sec
        }
        $ok = $times | Where-Object { $_ -gt 0 }
        "coldstart result: avg {0:0.0} s, max {1:0.0} s over {2} runs (target 8 s)" -f ($ok | Measure-Object -Average).Average, ($ok | Measure-Object -Maximum).Maximum, $ok.Count
    }

    'idle' {
        # Keep the screen on: a sleeping screen pauses the app and the protocol would measure nothing.
        Adb shell svc power stayon true | Out-Null
        Adb shell settings put system screen_off_timeout 2147483647 | Out-Null
        Boot
        Tap 540 1300
        $samples = @()
        $start = Get-Date
        $startSave = Read-Save
        while (((Get-Date) - $start).TotalMinutes -lt $Minutes) {
            $alive = Alive
            $pss = 0
            if ($alive) {
                $mem = Adb shell dumpsys meminfo $pkg
                $line = $mem | Select-String 'TOTAL PSS:|TOTAL:' | Select-Object -First 1
                if ($line -and $line.Line -match '(\d+)') { $pss = [int]$Matches[1] }
            }
            $row = [pscustomobject]@{ minute = [math]::Round(((Get-Date) - $start).TotalMinutes, 1); alive = $alive; pssMB = [math]::Round($pss / 1024, 1) }
            $samples += $row
            "{0,5} min alive={1} pss={2} MB" -f $row.minute, $row.alive, $row.pssMB
            if (-not $alive) { break }
            Start-Sleep -Seconds 60
        }
        Shot 'idle-end.png'
        Adb shell input keyevent KEYCODE_HOME | Out-Null
        Start-Sleep 3
        $endSave = Read-Save
        $samples | Export-Csv "$OutDir/idle-$Minutes.csv" -NoTypeInformation
        $first = ($samples | Where-Object { $_.pssMB -gt 0 } | Select-Object -First 3 | Measure-Object pssMB -Average).Average
        $last = ($samples | Where-Object { $_.pssMB -gt 0 } | Select-Object -Last 3 | Measure-Object pssMB -Average).Average
        $growth = if ($first -gt 0) { ($last - $first) / $first * 100 } else { 0 }
        "idle result: alive={0} pss start {1:0} MB -> end {2:0} MB ({3:+0.0;-0.0}%), stage {4} -> {5}, level {6} -> {7}, errors {8}" -f `
            ($samples[-1].alive), $first, $last, $growth, $startSave.highestStage, $endSave.highestStage, $startSave.heroLevel, $endSave.heroLevel, (Unity-Errors).Count
    }
}
