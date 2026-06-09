param(
    [int]$Port = 8080
)

$ErrorActionPreference = "Stop"
$path = Join-Path $PSScriptRoot "auth.json"

if (-not (Test-Path $path)) {
    $example = Join-Path $PSScriptRoot "auth.example.json"
    if (Test-Path $example) {
        Copy-Item $example $path
        Write-Host "  [INFO] auth.json created from auth.example.json"
    }
    else {
        Write-Host "  [WARN] auth.json not found. Auth will be disabled until you add one."
        exit 0
    }
}

try {
    $json = Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json
}
catch {
    Write-Host "  [WARN] Could not parse auth.json: $($_.Exception.Message)"
    Write-Host "  Server will start with auth disabled."
    exit 1
}

$issues = @()
$expectedRedirect = "http://localhost:$Port/auth/feishu/callback"
$appId = [string]$json.feishu.appId
$appSecret = [string]$json.feishu.appSecret
$redirect = [string]$json.feishu.redirectUri
$secret = [string]$json.sessionSecret

if ($json.enabled) {
    Write-Host "  Auth:        ENABLED"
    Write-Host "  View auth:   $($json.requireAuthForView)"
    Write-Host "  Upload auth: $($json.requireAuthForUpload)"
}
else {
    Write-Host "  Auth:        disabled (set enabled=true to turn on)"
}

if ([string]::IsNullOrWhiteSpace($appId) -or $appId -match '^(cli_)?x+$') {
    if ($json.enabled) {
        $issues += "  [ERROR] feishu.appId is empty or placeholder. Get it from Feishu Open Platform."
    }
    else {
        Write-Host "  Feishu:      appId not set (fill before enabling auth)"
    }
}
elseif ($json.enabled) {
    Write-Host "  Feishu:      appId configured ($appId)"
}

if ([string]::IsNullOrWhiteSpace($appSecret) -or $appSecret -match '^x+$') {
    if ($json.enabled) {
        $issues += "  [ERROR] feishu.appSecret is empty or placeholder."
    }
}
elseif ($json.enabled) {
    Write-Host "  Feishu:      appSecret configured"
}

if ($json.enabled -and [string]::IsNullOrWhiteSpace($redirect)) {
    $issues += "  [ERROR] feishu.redirectUri is empty."
}
elseif (-not [string]::IsNullOrWhiteSpace($redirect) -and $redirect -ne $expectedRedirect) {
    $issues += "  [WARN]  redirectUri is $redirect but server port is $Port."
    $issues += "          Expected: $expectedRedirect"
}
elseif ($json.enabled) {
    Write-Host "  Callback:    $redirect"
}

if ($secret -match 'change-me|please-change') {
    $issues += "  [WARN]  sessionSecret is still default. Change it before production deploy."
}

if ($json.enabled -and ($issues | Where-Object { $_ -match '\[ERROR\]' }).Count -gt 0) {
    $issues += "  [HINT]  Set enabled=false in auth.json to start without login, or fill Feishu credentials."
}

foreach ($item in $issues) {
    Write-Host $item -ForegroundColor Yellow
}

if (($issues | Where-Object { $_ -match '\[ERROR\]' }).Count -gt 0 -and $json.enabled) {
    exit 2
}

exit 0
