# TunesLayer Build Script
# Builds and packages the application

param(
    [switch]$Release,
    [switch]$Installer
)

$ErrorActionPreference = "Stop"

Write-Host "=== TunesLayer Build Script ===" -ForegroundColor Cyan

# Configuration
$SolutionDir = Split-Path $PSScriptRoot -Parent
$SrcDir = Join-Path $SolutionDir "src"
$PublishDir = Join-Path $SolutionDir "publish"
$DistDir = Join-Path $SolutionDir "dist"

# Clean previous build
Write-Host "`nCleaning previous build..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }

New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# Build configuration
$Configuration = if ($Release) { "Release" } else { "Debug" }
Write-Host "Building in $Configuration mode..." -ForegroundColor Yellow

# Restore and build
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore "$SrcDir\TunesLayer.sln"

Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build "$SrcDir\TunesLayer.sln" -c $Configuration --no-restore

# Publish
Write-Host "`nPublishing application..." -ForegroundColor Yellow
dotnet publish "$SrcDir\TunesLayer.App\TunesLayer.App.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -o $PublishDir

# Create installer if requested
if ($Installer) {
    Write-Host "`nCreating installer..." -ForegroundColor Yellow
    
    $InnoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $InnoSetup)) {
        Write-Host "Inno Setup not found. Please install from https://jrsoftware.org/isinfo.php" -ForegroundColor Red
        exit 1
    }
    
    & $InnoSetup "$PSScriptRoot\installer.iss"
}

Write-Host "`n=== Build Complete ===" -ForegroundColor Green
Write-Host "Output: $PublishDir" -ForegroundColor Cyan

if ($Installer) {
    Write-Host "Installer: $DistDir" -ForegroundColor Cyan
}
