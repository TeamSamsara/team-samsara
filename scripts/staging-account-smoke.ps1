# Staging smoke test for the account lifecycle: registration, logout everywhere, account deletion,
# the immediate block, session revocation and restore. Run it against a THROWAWAY mailbox: the
# script creates an account and leaves it deleted so the purge can be tested by hand afterwards.
param(
    [string]$Api = 'https://teamsamsara-api-staging.onrender.com',
    [string]$EnvFile = (Join-Path $PSScriptRoot '..\apps\web\.env.staging')
)

$ErrorActionPreference = 'Stop'

$FirebaseKey = ((Get-Content $EnvFile | Where-Object { $_ -like 'VITE_FIREBASE_API_KEY=*' }) -replace '^VITE_FIREBASE_API_KEY=', '').Trim()
$Work = Join-Path $env:TEMP 'samsara-smoke'
New-Item -ItemType Directory -Force $Work | Out-Null
$script:Pass = 0
$script:Fail = 0

function Call {
    param([string]$Method, [string]$Path, [string]$Token, $Json = $null)
    $out = Join-Path $Work 'out.txt'
    if (Test-Path $out) { Remove-Item $out }
    $curlArgs = @('-s', '--max-time', '120', '-o', $out, '-w', '%{http_code}', '-X', $Method, "$Api$Path")
    if ($Token) { $curlArgs += @('-H', "Authorization: Bearer $Token") }
    if ($null -ne $Json) {
        $jsonFile = Join-Path $Work 'body.json'
        [IO.File]::WriteAllText($jsonFile, [string]$Json, (New-Object System.Text.UTF8Encoding $false))
        $curlArgs += @('-H', 'Content-Type: application/json', '--data-binary', "@$jsonFile")
    }
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

function Firebase($endpoint, $body) {
    Invoke-RestMethod -Method Post -Uri "https://identitytoolkit.googleapis.com/v1/accounts:$endpoint`?key=$FirebaseKey" -ContentType 'application/json' -Body ($body | ConvertTo-Json)
}

function SignIn {
    (Firebase 'signInWithPassword' @{ email = $script:Email; password = $script:Password; returnSecureToken = $true })
}

# Asks Firebase for a new ID token from a refresh token; answers $null when Firebase refuses
function Refresh($refreshToken) {
    try {
        return Invoke-RestMethod -Method Post -Uri "https://securetoken.googleapis.com/v1/token?key=$FirebaseKey" -ContentType 'application/x-www-form-urlencoded' -Body "grant_type=refresh_token&refresh_token=$refreshToken"
    }
    catch { return $null }
}

# Brings a signed-in token to verified member, asking for the emailed code when the API wants one
function Ensure-Member($token) {
    $me = Call 'GET' '/account/me' $token
    if ($me.Json.authState -eq 'member') { return $me }

    $check = Call 'POST' '/account/login/check' $token
    Write-Host "login/check: $($check.Body)"
    if ($check.Body -match 'challengeRequired') {
        $code = Read-Host 'Login code emailed to the test mailbox'
        $confirm = Call 'POST' '/account/login/confirm' $token (@{ code = [string]$code } | ConvertTo-Json)
        Write-Host "login/confirm: $($confirm.Body)"
    }
    return Call 'GET' '/account/me' $token
}

# --- wake the server (Render free instances sleep)
Write-Host 'Waking the Staging API...'
for ($i = 1; $i -le 12; $i++) {
    $r = Call 'GET' '/health/ready' ''
    if ($r.Status -eq 200) { break }
    Start-Sleep -Seconds 10
}
Check 'health/ready' $r 200

# --- throwaway account: Firebase user, then registration with the emailed code
$script:Email = Read-Host 'THROWAWAY mailbox (receives the codes; example: you+smoke1@gmail.com)'
$script:Password = "Smoke-$([guid]::NewGuid().ToString('N'))-Aa1!"
$user = Firebase 'signUp' @{ email = $script:Email; password = $script:Password; returnSecureToken = $true }
$uid = $user.localId
$tok = $user.idToken

Check 'new Firebase user is a guest' (Call 'GET' '/account/profile' $tok) 403 'registrationIncomplete'

$reg = Call 'POST' '/account/register' $tok
Check 'register sends a code' $reg 200 'success'
$code = Read-Host 'Registration code emailed to the test mailbox'
Check 'register confirm' (Call 'POST' '/account/register/confirm' $tok (@{ code = [string]$code } | ConvertTo-Json)) 200 'success'

# The member access level is written on the ID token, so the client must refresh it after registering
$refreshed = Refresh $user.refreshToken
if ($null -eq $refreshed) { throw 'Firebase refused to refresh the token; stopping.' }
$tok = $refreshed.id_token

$me = Ensure-Member $tok
CheckTrue 'precondition: caller is a verified member' ($me.Json.authState -eq 'member') $me.Body
if ($me.Json.authState -ne 'member') { throw 'Test account is not a verified member; stopping.' }

# --- logout everywhere: verified sign-ins are forgotten at once, refresh tokens are revoked
Check 'logout/all' (Call 'POST' '/account/logout/all' $tok) 204
Check 'after logout/all the old token is not a member' (Call 'GET' '/account/profile' $tok) 403

$signed = SignIn
$tok = $signed.idToken
$me = Ensure-Member $tok
CheckTrue 'signing in again and verifying restores member access' ($me.Json.authState -eq 'member') $me.Body

# --- deletion: wrong code, then the real one
$req = Call 'POST' '/account/delete/request-code' $tok
Check 'delete request-code' $req 200 'success'
$len = [int]$req.Json.codeLength
Check 'wrong deletion code refused' (Call 'POST' '/account/delete' $tok (@{ code = ('0' * $len) } | ConvertTo-Json)) 400 'invalidCode'

Check 'asking again at once is rate limited' (Call 'POST' '/account/delete/request-code' $tok) 429 'cooldownActive'

$delCode = Read-Host 'Deletion code emailed to the test mailbox'
$refreshBefore = $signed.refreshToken
Check 'delete account' (Call 'POST' '/account/delete' $tok (@{ code = [string]$delCode } | ConvertTo-Json)) 200 'success'

# --- the block is immediate, even though Firebase still accepts the token
Check 'deleted: same token no longer reaches the profile' (Call 'GET' '/account/profile' $tok) 403
$me = Call 'GET' '/account/me' $tok
CheckTrue 'deleted: same token is no longer a member' ($me.Json.authState -ne 'member') $me.Body
Check 'deleted: the used code cannot delete again' (Call 'POST' '/account/delete' $tok (@{ code = [string]$delCode } | ConvertTo-Json)) 403

# --- sessions were revoked
CheckTrue 'deleted: the refresh token is revoked' ($null -eq (Refresh $refreshBefore))

$notice = Read-Host 'Did the "account deleted" email arrive (check spam too) and name the 30-day window? (y/n)'
CheckTrue 'deletion notice received' ($notice -eq 'y')

# --- restore through the normal sign-in
$signed = SignIn
$tokRestore = $signed.idToken
Check 'deleted account, fresh sign-in: not a member before verifying' (Call 'GET' '/account/profile' $tokRestore) 403
$me = Ensure-Member $tokRestore
CheckTrue 'restored: verifying the sign-in makes the member active again' ($me.Json.authState -eq 'member') $me.Body
Check 'restored: profile reachable' (Call 'GET' '/account/profile' $tokRestore) 200
Check 'restored: the token from before the deletion stays unverified' (Call 'GET' '/account/profile' $tok) 403

# --- delete once more and leave it deleted for the manual purge test
$req = Call 'POST' '/account/delete/request-code' $tokRestore
Check 'second delete request-code' $req 200 'success'
$delCode = Read-Host 'Second deletion code emailed to the test mailbox'
Check 'second delete' (Call 'POST' '/account/delete' $tokRestore (@{ code = [string]$delCode } | ConvertTo-Json)) 200 'success'

Write-Host ''
Write-Host "Done: $script:Pass passed, $script:Fail failed." -ForegroundColor Cyan
Write-Host ''
Write-Host 'The account is left deleted for the purge test. Do this by hand:' -ForegroundColor Yellow
Write-Host "  1. Firestore (staging) > users > $uid : set DeletedAt to a time more than 30 days ago."
Write-Host '  2. Restart the staging service on Render (the purge runs at startup), or wait up to 6 hours.'
Write-Host '  3. Logs should show "Account purge finished: 1 purged".'
Write-Host "  4. users/$uid and profiles/$uid are gone, and the Firebase Auth user is gone."
