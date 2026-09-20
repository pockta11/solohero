<#
.SYNOPSIS
  Story 1-09 helper: verifies an AAB for Google Play's 16 KB page-size requirement.

.DESCRIPTION
  1. bundletool build-apks --mode=universal  -> universal.apk
  2. extracts lib/arm64-v8a/*.so and lib/armeabi-v7a/*.so
  3. llvm-readelf -l on every .so: all LOAD segments must have Align >= 0x4000
  4. zipalign -c -P 16 -v 4 universal.apk: uncompressed .so entries must be 16 KB aligned in the zip
  Prints a markdown table you can paste into the story's Dev Agent Record.

.EXAMPLE
  pwsh tools/spike/Check16Kb.ps1            # defaults: Builds/game.aab, bundled JDK/NDK, user-local bundletool + build-tools 35
  pwsh tools/spike/Check16Kb.ps1 -Aab Builds/other.aab
#>
param(
    [string] $Aab = "Builds/game.aab",
    [string] $Bundletool = "C:\Users\user\android-sdk-tools\bundletool-all.jar",
    [string] $Ndk = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\PlaybackEngines\AndroidPlayer\NDK",
    [string] $BuildTools = "C:\Users\user\android-sdk-tools\build-tools\35.0.0",
    [string] $Java = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe",
    [string] $WorkDir = "Builds/spike16kb"
)

$ErrorActionPreference = "Stop"
$readelf = Join-Path $Ndk "toolchains\llvm\prebuilt\windows-x86_64\bin\llvm-readelf.exe"
$zipalign = Join-Path $BuildTools "zipalign.exe"
foreach ($tool in @($readelf, $zipalign, $Bundletool, $Aab, $Java)) {
    if (-not (Test-Path $tool)) { throw "not found: $tool" }
}

if (Test-Path $WorkDir) { Remove-Item -Recurse -Force $WorkDir }
New-Item -ItemType Directory -Force $WorkDir | Out-Null
$apks = Join-Path $WorkDir "universal.apks"

Write-Host "== bundletool build-apks (universal)"
& $Java -jar $Bundletool build-apks --bundle=$Aab --output=$apks --mode=universal --overwrite
if ($LASTEXITCODE -ne 0) { throw "bundletool failed ($LASTEXITCODE)" }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$apkDir = Join-Path $WorkDir "apks"
[System.IO.Compression.ZipFile]::ExtractToDirectory($apks, $apkDir)
$apk = Join-Path $apkDir "universal.apk"
$apkExtract = Join-Path $WorkDir "apk"
[System.IO.Compression.ZipFile]::ExtractToDirectory($apk, $apkExtract)

$soFiles = Get-ChildItem -Recurse -Path (Join-Path $apkExtract "lib") -Filter *.so
if ($soFiles.Count -eq 0) { throw "no .so files found under lib/ - is the build IL2CPP + ARM64?" }

Write-Host ""
Write-Host "| ABI | Library | LOAD Align (min) | Result |"
Write-Host "|---|---|---|---|"
$allOk = $true
foreach ($so in $soFiles) {
    $abi = $so.Directory.Name
    $lines = & $readelf -l $so.FullName | Select-String "^\s*LOAD"
    $minAlign = [int64]::MaxValue
    foreach ($l in $lines) {
        $parts = ($l.Line -split "\s+") | Where-Object { $_ -ne "" }
        $alignHex = $parts[-1]
        $align = [Convert]::ToInt64($alignHex, 16)
        if ($align -lt $minAlign) { $minAlign = $align }
    }
    $ok = $minAlign -ge 16384
    if (-not $ok) { $allOk = $false }
    $mark = if ($ok) { "PASS" } else { "FAIL" }
    Write-Host ("| {0} | {1} | 0x{2:X} | {3} |" -f $abi, $so.Name, $minAlign, $mark)
}

Write-Host ""
Write-Host "== zipalign -c -P 16 -v 4 universal.apk"
& $zipalign -c -P 16 -v 4 $apk | Select-String "lib/.*\.so|Verification" | ForEach-Object { Write-Host $_.Line }
$zipOk = ($LASTEXITCODE -eq 0)
Write-Host ("zipalign 16 KB: {0}" -f $(if ($zipOk) { "PASS" } else { "FAIL (exit $LASTEXITCODE)" }))

Write-Host ""
if ($allOk -and $zipOk) {
    Write-Host "RESULT A: all native libs 16 KB aligned and packaged correctly (JDK 11 stays)." -ForegroundColor Green
    exit 0
} elseif (-not $allOk) {
    Write-Host "RESULT B-1/B-2: some .so are not 16 KB aligned - see FAIL rows (third-party -> B-1, libunity/libil2cpp/libmain -> B-2)." -ForegroundColor Yellow
    exit 2
} else {
    Write-Host "RESULT B-3: libs are aligned but zip packaging is not (AGP 7.4.2 limit) - try AGP 8.5+/Gradle 8.7+/JDK 17 path." -ForegroundColor Yellow
    exit 3
}
