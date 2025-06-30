# Build and package Dino for NuGet

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$NoBuild,
    [switch]$NoTest
)

$ErrorActionPreference = "Stop"

Write-Host "Building Dino packages v$Version..." -ForegroundColor Green

# Clean previous packages
if (Test-Path "./artifacts") {
    Remove-Item -Path "./artifacts" -Recurse -Force
}
New-Item -ItemType Directory -Path "./artifacts" | Out-Null

# Build solution
if (-not $NoBuild) {
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build -c $Configuration /p:Version=$Version
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        exit 1
    }
}

# Run tests
if (-not $NoTest) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed!"
        exit 1
    }
}

# Pack NuGet packages
Write-Host "Creating NuGet packages..." -ForegroundColor Yellow

# Pack Dino.Core
dotnet pack ./src/Dino.Core/Dino.Core.csproj `
    -c $Configuration `
    --no-build `
    -p:PackageVersion=$Version `
    -o ./artifacts

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack Dino.Core!"
    exit 1
}

# Pack Dino.EFCore
dotnet pack ./src/Dino.EFCore/Dino.EFCore.csproj `
    -c $Configuration `
    --no-build `
    -p:PackageVersion=$Version `
    -o ./artifacts

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack Dino.EFCore!"
    exit 1
}

Write-Host "Packages created successfully in ./artifacts" -ForegroundColor Green
Write-Host ""
Write-Host "Packages:" -ForegroundColor Cyan
Get-ChildItem -Path "./artifacts" -Filter "*.nupkg" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor White
}

Write-Host ""
Write-Host "To publish to NuGet.org, run:" -ForegroundColor Yellow
Write-Host "  dotnet nuget push ./artifacts/*.nupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor White