# Build and pack NuGet packages for DataLinq.NET
# Usage: .\pack.ps1 [-Configuration Release] [-Version 1.0.0]

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string]$OutputDir = ".\nupkgs"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " DataLinq.NET NuGet Packer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration"
Write-Host "Version: $Version"
Write-Host "Output: $OutputDir"
Write-Host ""

# Clean output directory
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# Build the solution first
Write-Host "[1/3] Building solution..." -ForegroundColor Yellow
dotnet build DataLinq.Net.sln -c $Configuration /p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Pack DataLinq.Data
Write-Host "[2/3] Packing DataLinq.Data..." -ForegroundColor Yellow
dotnet pack src\DataLinq.Data.Read\DataLinq.Data.Read.csproj -c $Configuration /p:Version=$Version -o $OutputDir --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed for DataLinq.Data!" -ForegroundColor Red
    exit 1
}

# Pack DataLinq.Net (meta-package)
Write-Host "[3/3] Packing DataLinq.Net..." -ForegroundColor Yellow
dotnet pack packaging\DataLinq.Net\DataLinq.Net.csproj -c $Configuration /p:Version=$Version -o $OutputDir --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed for DataLinq.Net!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Packages created successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Get-ChildItem $OutputDir -Filter *.nupkg | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor White
}
Write-Host ""
Write-Host "To publish: .\publish.ps1 -ApiKey YOUR_API_KEY" -ForegroundColor Cyan
