# Pharmacy Management System - Windows Build Script
# Requires .NET 8.0 SDK

Write-Host "========================================"
Write-Host "Pharmacy Management System Build"
Write-Host "========================================"

# Restore packages
Write-Host "`n[1/4] Restoring NuGet packages..."
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Package restore failed!"
    exit 1
}

# Build solution
Write-Host "`n[2/4] Building solution..."
dotnet build --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# Run tests
Write-Host "`n[3/4] Running unit tests..."
dotnet test --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Some tests failed!"
}

# Publish application
Write-Host "`n[4/4] Publishing application..."
$publishDir = "./publish"
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

dotnet publish src/PharmacyManagement.UI/PharmacyManagement.UI.csproj `
    --no-build `
    --configuration Release `
    --output $publishDir `
    --self-contained false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n========================================"
    Write-Host "Build completed successfully!"
    Write-Host "Published to: $publishDir"
    Write-Host "========================================"
} else {
    Write-Error "Publish failed!"
    exit 1
}
