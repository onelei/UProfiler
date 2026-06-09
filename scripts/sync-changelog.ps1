param(
    [ValidateSet("patch", "minor", "major")]
    [string]$Bump,
    [switch]$ListCommits,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$versionFile = Join-Path $repoRoot "VERSION"
$changelogCn = Join-Path $repoRoot "CHANGELOG.md"
$changelogEn = Join-Path $repoRoot "CHANGELOG_EN.md"
$packageJson = Join-Path $repoRoot "UProfiler-Unity/Packages/com.lemonframework.uprofiler/package.json"
$serverCsproj = Join-Path $repoRoot "UProfiler-Server/UProfiler-Server.csproj"

function Get-CurrentVersion {
    if (-not (Test-Path $versionFile)) {
        throw "VERSION file not found at $versionFile"
    }
    return (Get-Content $versionFile -Raw).Trim()
}

function Bump-Version([string]$version, [string]$part) {
    $segments = $version.Split(".")
    if ($segments.Count -ne 3) {
        throw "Invalid version format: $version"
    }

    [int]$major = $segments[0]
    [int]$minor = $segments[1]
    [int]$patch = $segments[2]

    switch ($part) {
        "major" { $major++; $minor = 0; $patch = 0 }
        "minor" { $minor++; $patch = 0 }
        "patch" { $patch++ }
    }

    return "$major.$minor.$patch"
}

function Get-GitCommits {
    $logFile = Join-Path $env:TEMP "uprofiler-git-log.txt"
    cmd /c "git -C `"$repoRoot`" log --oneline > `"$logFile`""
    return Get-Content $logFile -Encoding utf8 | ForEach-Object {
        if ([string]::IsNullOrWhiteSpace($_)) { return }
        if ($_ -match '^([0-9a-f]+)\s+(.+)$') {
            [pscustomobject]@{
                Hash    = $Matches[1]
                Date    = ""
                Subject = $Matches[2]
            }
        }
    }
}

function Set-VersionEverywhere([string]$version) {
    Set-Content -Path $versionFile -Value $version -NoNewline -Encoding UTF8

    $pkg = Get-Content $packageJson -Raw -Encoding UTF8 | ConvertFrom-Json
    $pkg.version = $version
    $pkg | ConvertTo-Json -Depth 10 | Set-Content $packageJson -Encoding UTF8

    $csproj = Get-Content $serverCsproj -Raw -Encoding UTF8
    if ($csproj -match "<Version>.*?</Version>") {
        $csproj = $csproj -replace "<Version>.*?</Version>", "<Version>$version</Version>"
    }
    else {
        $csproj = $csproj -replace "(<TargetFramework>net8\.0</TargetFramework>)", "`$1`r`n    <Version>$version</Version>"
    }
    Set-Content $serverCsproj $csproj -Encoding UTF8

    $staticAssets = Join-Path $repoRoot "UProfiler-Server/Services/StaticAssets.cs"
    $cacheVersion = $version.Replace(".", "")
    $staticContent = Get-Content $staticAssets -Raw -Encoding UTF8
    $staticContent = $staticContent -replace 'public const string Version = "[^"]+";', "public const string Version = `"$cacheVersion`";"
    Set-Content $staticAssets $staticContent -Encoding UTF8

    $unityVersion = Join-Path $repoRoot "UProfiler-Unity/Packages/com.lemonframework.uprofiler/Runtime/Scripts/Core/UProfilerVersion.cs"
    $unityContent = Get-Content $unityVersion -Raw -Encoding UTF8
    $unityContent = $unityContent -replace 'public const string Version = "[^"]+";', "public const string Version = `"$version`";"
    Set-Content $unityVersion $unityContent -Encoding UTF8

    $readmeCn = Join-Path $repoRoot "README.md"
    $readmeEn = Join-Path $repoRoot "README_EN.md"
    foreach ($readme in @($readmeCn, $readmeEn)) {
        $content = Get-Content $readme -Raw -Encoding UTF8
        $content = $content -replace 'badge/version-[^-]+-', "badge/version-$version-"
        Set-Content $readme $content -Encoding UTF8
    }

    Write-Host "Updated VERSION, package.json, csproj, StaticAssets, UProfilerVersion, README badges -> $version"
}

$currentVersion = Get-CurrentVersion
Write-Host "Current version: $currentVersion"

if ($ListCommits) {
    Write-Host ""
    Write-Host "Git commits:"
    Get-GitCommits | ForEach-Object {
        Write-Host "  $($_.Hash) $($_.Subject)"
    }
    exit 0
}

if ($Bump) {
    $newVersion = Bump-Version $currentVersion $Bump
    Write-Host "Bump ${Bump}: $currentVersion -> $newVersion"

    if ($DryRun) {
        Write-Host "[DryRun] Version files would be updated. Edit CHANGELOG.md manually with git commits."
        exit 0
    }

    Set-VersionEverywhere $newVersion
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "  1. Add a new section to CHANGELOG.md and CHANGELOG_EN.md for [$newVersion]"
    Write-Host "  2. Run: .\scripts\sync-changelog.ps1 -ListCommits"
    Write-Host "  3. Commit changes"
    exit 0
}

Write-Host ""
Write-Host "Usage:"
Write-Host "  .\scripts\sync-changelog.ps1 -ListCommits          # list git commits"
Write-Host "  .\scripts\sync-changelog.ps1 -Bump patch           # 1.1.0 -> 1.1.1"
Write-Host "  .\scripts\sync-changelog.ps1 -Bump minor           # 1.1.0 -> 1.2.0"
Write-Host "  .\scripts\sync-changelog.ps1 -Bump major -DryRun   # preview only"
