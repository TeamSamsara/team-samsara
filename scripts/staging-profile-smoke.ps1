# Staging smoke test for the profile endpoints: read and edit, picture and banner upload and
# removal, validation limits, and the guest and anonymous refusals. Run it with a verified member
# test account; it restores that account's name and bio at the end.
param(
    [string]$Api = 'https://teamsamsara-api-staging.onrender.com',
    [string]$EnvFile = (Join-Path $PSScriptRoot '..\apps\web\.env.staging')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$FirebaseKey = ((Get-Content $EnvFile | Where-Object { $_ -like 'VITE_FIREBASE_API_KEY=*' }) -replace '^VITE_FIREBASE_API_KEY=', '').Trim()
$Work = Join-Path $env:TEMP 'samsara-smoke'
New-Item -ItemType Directory -Force $Work | Out-Null
$script:Pass = 0
$script:Fail = 0

function Call {
    param([string]$Method, [string]$Path, [string]$Token, $Json = $null, [string]$File = '', [string]$FileType = 'image/png')
    $out = Join-Path $Work 'out.txt'
    if (Test-Path $out) { Remove-Item $out }
    $curlArgs = @('-s', '--max-time', '120', '-o', $out, '-w', '%{http_code}', '-X', $Method, "$Api$Path")
    if ($Token) { $curlArgs += @('-H', "Authorization: Bearer $Token") }
    if ($null -ne $Json) {
        $jsonFile = Join-Path $Work 'body.json'
        [IO.File]::WriteAllText($jsonFile, [string]$Json, (New-Object System.Text.UTF8Encoding $false))
        $curlArgs += @('-H', 'Content-Type: application/json', '--data-binary', "@$jsonFile")
    }
    if ($File) { $curlArgs += @('-F', "file=@$File;type=$FileType") }
    $status = & curl.exe @curlArgs
    $body = ''
    if (Test-Path $out) { $raw = Get-Content $out -Raw; if ($raw) { $body = $raw } }
    $parsed = $null
    try { $parsed = $body | ConvertFrom-Json } catch { }
    return [pscustomobject]@{ Status = [int]$status; Body = $body; Json = $parsed }
}

function Check($name, $res, [int]$expected, $pattern = $null) {
    $ok = ($res.Status -eq $expected) -and ((-not $pattern) -or ($res.Body -match $pattern))
    if ($ok) { $script:Pass++; Write-Host "PASS  $name ($($res.Status))" -ForegroundColor Green }
    else { $script:Fail++; Write-Host "FAIL  $name - expected $expected /$pattern/, got $($res.Status): $($res.Body)" -ForegroundColor Red }
}

function CheckTrue($name, [bool]$cond, $detail = '') {
    if ($cond) { $script:Pass++; Write-Host "PASS  $name" -ForegroundColor Green }
    else { $script:Fail++; Write-Host "FAIL  $name $detail" -ForegroundColor Red }
}

function New-Image($path, $format, $color) {
    $bmp = New-Object System.Drawing.Bitmap 16, 16
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear($color); $g.Dispose()
    $bmp.Save($path, $format); $bmp.Dispose()
}

function New-Padded($src, $dest, $size) {
    $bytes = [IO.File]::ReadAllBytes($src)
    $buf = New-Object byte[] $size
    [Array]::Copy($bytes, $buf, $bytes.Length)
    [IO.File]::WriteAllBytes($dest, $buf)
}

function Firebase($endpoint, $body) {
    Invoke-RestMethod -Method Post -Uri "https://identitytoolkit.googleapis.com/v1/accounts:$endpoint`?key=$FirebaseKey" -ContentType 'application/json' -Body ($body | ConvertTo-Json)
}

# --- test files
$png1 = Join-Path $Work 'a.png'; New-Image $png1 ([System.Drawing.Imaging.ImageFormat]::Png) ([System.Drawing.Color]::Red)
$png2 = Join-Path $Work 'b.png'; New-Image $png2 ([System.Drawing.Imaging.ImageFormat]::Png) ([System.Drawing.Color]::Blue)
$jpg = Join-Path $Work 'c.jpg'; New-Image $jpg ([System.Drawing.Imaging.ImageFormat]::Jpeg) ([System.Drawing.Color]::Green)
$fake = Join-Path $Work 'fake.png'; Set-Content -Path $fake -Value 'this is not an image'
$bigPicture = Join-Path $Work 'big-picture.png'; New-Padded $png1 $bigPicture 2300000
$bigBanner = Join-Path $Work 'big-banner.png'; New-Padded $png1 $bigBanner 5500000

# --- wake the server (Render free instances sleep)
Write-Host 'Waking the Staging API...'
for ($i = 1; $i -le 12; $i++) {
    $r = Call 'GET' '/health/ready' ''
    if ($r.Status -eq 200) { break }
    Start-Sleep -Seconds 10
}
Check 'health/ready' $r 200

# --- sign in as the member test account
$email = Read-Host 'Test account email'
$sec = Read-Host 'Test account password' -AsSecureString
$pw = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
$tok = (Firebase 'signInWithPassword' @{ email = $email; password = $pw; returnSecureToken = $true }).idToken

$me = Call 'GET' '/account/me' $tok
Write-Host "me: $($me.Body)"
if ($me.Json.authState -ne 'member') {
    $lc = Call 'POST' '/account/login/check' $tok
    Write-Host "login/check: $($lc.Body)"
    if ($lc.Body -match 'challengeRequired') {
        $code = Read-Host 'Code emailed to the test account'
        $lcf = Call 'POST' '/account/login/confirm' $tok (@{ code = [string]$code } | ConvertTo-Json)
        Write-Host "login/confirm: $($lcf.Body)"
    }
    $me = Call 'GET' '/account/me' $tok
}
CheckTrue 'precondition: caller is a verified member' ($me.Json.authState -eq 'member') $me.Body
if ($me.Json.authState -ne 'member') { throw 'Test account is not a verified member; stopping.' }

# --- member: read and edit
$read = Call 'GET' '/account/profile' $tok
Check 'read profile' $read 200 '"status":"success"'
$orig = $read.Json.profile

$edit = Call 'PUT' '/account/profile' $tok (@{ displayName = 'Smoke Test'; bio = "line one`nline two" } | ConvertTo-Json)
Check 'edit name and bio' $edit 200 'Smoke Test'
Check 'edit persisted' (Call 'GET' '/account/profile' $tok) 200 'Smoke Test'

Check 'blank name refused' (Call 'PUT' '/account/profile' $tok (@{ displayName = '   '; bio = $null } | ConvertTo-Json)) 400 'invalidDisplayName'
Check 'name over 50 refused' (Call 'PUT' '/account/profile' $tok (@{ displayName = ('x' * 51); bio = $null } | ConvertTo-Json)) 400 'invalidDisplayName'
Check 'bio over 300 refused' (Call 'PUT' '/account/profile' $tok (@{ displayName = 'Smoke Test'; bio = ('y' * 301) } | ConvertTo-Json)) 400 'invalidBio'

# --- member: picture
$p1 = Call 'PUT' '/account/profile/picture' $tok -File $png1 -FileType 'image/png'
Check 'set picture (png)' $p1 200 'profilePictureAssetId'
$id1 = $p1.Json.profile.profilePictureAssetId
CheckTrue 'picture id returned' ([bool]$id1)
Check 'picture is publicly fetchable' (Call 'GET' "/assets/$id1/file" '') 200

$p2 = Call 'PUT' '/account/profile/picture' $tok -File $png2 -FileType 'image/png'
Check 'replace picture' $p2 200 'profilePictureAssetId'
$id2 = $p2.Json.profile.profilePictureAssetId
CheckTrue 'replacement has a new id' (($id2) -and ($id2 -ne $id1))
Check 'old picture was deleted' (Call 'GET' "/assets/$id1/file" '') 404

Check 'png sent as text/plain still accepted (real bytes)' (Call 'PUT' '/account/profile/picture' $tok -File $png1 -FileType 'text/plain') 200
Check 'non-image refused' (Call 'PUT' '/account/profile/picture' $tok -File $fake -FileType 'image/png') 415 'imageUnsupportedType'
Check 'picture over 2 MB refused' (Call 'PUT' '/account/profile/picture' $tok -File $bigPicture -FileType 'image/png') 413 'imageTooLarge'

# --- member: banner
$b1 = Call 'PUT' '/account/profile/banner' $tok -File $jpg -FileType 'image/jpeg'
Check 'set banner (jpeg)' $b1 200 'bannerAssetId'
Check 'banner over 5 MB refused' (Call 'PUT' '/account/profile/banner' $tok -File $bigBanner -FileType 'image/png') 413 'imageTooLarge'

# --- member: remove images and restore the original profile
$dp = Call 'DELETE' '/account/profile/picture' $tok
Check 'delete picture' $dp 200
CheckTrue 'picture cleared' (-not $dp.Json.profile.profilePictureAssetId) $dp.Body
$db = Call 'DELETE' '/account/profile/banner' $tok
Check 'delete banner' $db 200
CheckTrue 'banner cleared' (-not $db.Json.profile.bannerAssetId) $db.Body
Check 'restore original name and bio' (Call 'PUT' '/account/profile' $tok (@{ displayName = $orig.displayName; bio = $orig.bio } | ConvertTo-Json)) 200

# --- anonymous
Check 'anonymous GET profile' (Call 'GET' '/account/profile' '') 401
Check 'anonymous PUT profile, empty body' (Call 'PUT' '/account/profile' '') 401

# --- guest: a throwaway Firebase user that never registered
$stamp = Get-Date -Format 'yyyyMMddHHmmss'
$guest = Firebase 'signUp' @{ email = "smoke-guest-$stamp@example.com"; password = "Smoke-$([guid]::NewGuid().ToString('N'))"; returnSecureToken = $true }
$gt = $guest.idToken
Check 'guest GET profile' (Call 'GET' '/account/profile' $gt) 403 'registrationIncomplete'
Check 'guest PUT profile, valid body' (Call 'PUT' '/account/profile' $gt (@{ displayName = 'Nope'; bio = $null } | ConvertTo-Json)) 403
Check 'guest PUT profile, NO body (ordering check)' (Call 'PUT' '/account/profile' $gt) 403
Check 'guest PUT profile, JSON type + empty body (ordering check)' (Call 'PUT' '/account/profile' $gt '') 403
Check 'guest PUT picture, NO body (ordering check)' (Call 'PUT' '/account/profile/picture' $gt) 403
Check 'guest PUT picture, real file' (Call 'PUT' '/account/profile/picture' $gt -File $png1 -FileType 'image/png') 403
Check 'guest DELETE picture' (Call 'DELETE' '/account/profile/picture' $gt) 403

# --- cleanup the throwaway user
try { Firebase 'delete' @{ idToken = $gt } | Out-Null; Write-Host 'Throwaway guest user deleted.' } catch { Write-Host 'Could not delete the throwaway guest user; remove it in the Firebase console.' -ForegroundColor Yellow }

Write-Host ''
Write-Host "Done: $script:Pass passed, $script:Fail failed." -ForegroundColor Cyan
