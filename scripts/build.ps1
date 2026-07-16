<#
    build.ps1 - produce the shippable Kite Glance executable.

    Output: dist\KiteGlance.exe
      - one file, nothing beside it
      - .NET is bundled; the target machine needs no runtime installed
      - native ARM64 (no x64 emulation on Snapdragon)

    Usage:
      .\build.ps1              # ARM64 (Snapdragon X Elite)
      .\build.ps1 -Arch x64    # Intel / AMD
#>

param(
    [ValidateSet('arm64', 'x64')]
    [string]$Arch = 'arm64'
)

$ErrorActionPreference = 'Stop'
$rid = "win-$Arch"

# Repo root is the parent of this script's folder; the project lives under
# src\KiteGlance regardless of where the caller invoked this from.
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectDir = Join-Path $RepoRoot 'src\KiteGlance'
Push-Location $ProjectDir

Write-Host ""
Write-Host "  Kite Glance - production build ($rid)" -ForegroundColor Cyan
Write-Host "  ------------------------------------" -ForegroundColor DarkGray
Write-Host ""

# A running instance holds a lock on the exe and the publish will fail.
Get-Process KiteGlance -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  stopping running instance..." -ForegroundColor DarkYellow
    $_ | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

Remove-Item -Recurse -Force obj, bin, dist -ErrorAction SilentlyContinue

Write-Host "  publishing..." -ForegroundColor DarkGray
Write-Host ""

dotnet publish `
    -c Release `
    -r $rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:DebugType=none `
    -o "publish\$rid"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  BUILD FAILED" -ForegroundColor Red
    Pop-Location
    exit 1
}

New-Item -ItemType Directory -Force -Path dist | Out-Null
Copy-Item "publish\$rid\KiteGlance.exe" -Destination "dist\KiteGlance.exe" -Force

$exe = Get-Item "dist\KiteGlance.exe"
$mb = [math]::Round($exe.Length / 1MB, 1)

Write-Host ""
Write-Host "  Built  dist\KiteGlance.exe  ($mb MB)" -ForegroundColor Green
Write-Host ""
Write-Host "  Run it:      .\dist\KiteGlance.exe"
Write-Host "  Install it:  .\install.ps1"
Write-Host ""
