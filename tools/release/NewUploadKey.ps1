<#
.SYNOPSIS
  Creates the Google Play upload key (launch plan P0-1) and prints what the Release workflow needs.

.DESCRIPTION
  Run once, by the developer. Writes an RSA 4096 keystore valid for 30 years OUTSIDE the repository (default
  %USERPROFILE%\.solohero\upload.keystore) with a random password, then prints:
    - the SHA-1 / SHA-256 fingerprints (Firebase and Play Console ask for them),
    - the values for the GitHub secrets UPLOAD_KEYSTORE_BASE64 / UPLOAD_KEYSTORE_PASS / UPLOAD_KEY_ALIAS /
      UPLOAD_KEY_PASS (written to secrets.txt next to the keystore).
  Back the keystore and secrets.txt up in two offline places: losing the upload key means asking Google to reset it.
  Use Play App Signing (Google keeps the app signing key; this key only uploads).

.EXAMPLE
  pwsh tools/release/NewUploadKey.ps1
  pwsh tools/release/NewUploadKey.ps1 -OutDir D:\keys -DName "CN=SoloSoft, C=KR"
#>
param(
    [string] $OutDir = (Join-Path $env:USERPROFILE ".solohero"),
    [string] $Alias = "upload",
    [string] $DName = "CN=SoloSoft, O=SoloSoft, C=KR",
    [string] $Keytool = ""
)

$ErrorActionPreference = "Stop"

if (-not $Keytool) {
    $unityJdk = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
    if (Test-Path $unityJdk) { $Keytool = $unityJdk }
    elseif (Get-Command keytool -ErrorAction SilentlyContinue) { $Keytool = (Get-Command keytool).Source }
    else { throw "keytool not found: install a JDK or pass -Keytool" }
}

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
New-Item -ItemType Directory -Force $OutDir | Out-Null
$outFull = (Resolve-Path $OutDir).Path
if ($outFull.StartsWith($repo, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "keep the upload key outside the repository ($repo)"
}

$keystore = Join-Path $outFull "upload.keystore"
if (Test-Path $keystore) { throw "$keystore already exists - an upload key is made once; delete it yourself only if it was never used" }

# 32 random characters from an alphabet that needs no quoting.
$alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"
$bytes = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$password = -join ($bytes | ForEach-Object { $alphabet[$_ % $alphabet.Length] })

& $Keytool -genkeypair -v -keystore $keystore -storetype PKCS12 -alias $Alias -keyalg RSA -keysize 4096 `
    -validity 10950 -storepass $password -keypass $password -dname $DName | Out-Host
if ($LASTEXITCODE -ne 0) { throw "keytool failed" }

$listing = & $Keytool -list -v -keystore $keystore -alias $Alias -storepass $password
$sha1 = ($listing | Select-String "SHA1:").ToString().Trim()
$sha256 = ($listing | Select-String "SHA256:").ToString().Trim()
$base64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($keystore))

$secrets = Join-Path $outFull "secrets.txt"
@(
    "# GitHub > Settings > Secrets and variables > Actions (Release workflow). Keep this file offline.",
    "UPLOAD_KEYSTORE_PASS=$password",
    "UPLOAD_KEY_ALIAS=$Alias",
    "UPLOAD_KEY_PASS=$password",
    "UPLOAD_KEYSTORE_BASE64=$base64",
    "# Certificate fingerprints (Firebase project settings, Play Console app integrity):",
    "# $sha1",
    "# $sha256"
) | Set-Content -Encoding utf8 $secrets

Write-Host ""
Write-Host "Upload key : $keystore"
Write-Host "Secrets    : $secrets"
Write-Host $sha1
Write-Host $sha256
Write-Host ""
Write-Host "Next: back up both files offline (two places), add the four secrets plus GOOGLE_SERVICES_JSON_BASE64"
Write-Host "      ([Convert]::ToBase64String([IO.File]::ReadAllBytes('Assets\google-services.json'))), then run the"
Write-Host "      'Release AAB (Play upload)' workflow."
