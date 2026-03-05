# Script pentru pornire Azure Functions local

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting ANAF Azure Functions Local" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verifică licența Syncfusion
$settings = Get-Content "local.settings.json" | ConvertFrom-Json
$license = $settings.Values.SyncfusionLicenseKey

if ($license -eq "YOUR_SYNCFUSION_LICENSE_KEY_HERE") {
    Write-Host "⚠ ATENȚIE: Trebuie să configurezi SyncfusionLicenseKey în local.settings.json" -ForegroundColor Yellow
    Write-Host "  Obține licența de la: https://www.syncfusion.com/account/claim-license-key" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "Continui fără licență? (da/nu)"
    if ($continue -ne "da") {
        exit 1
    }
}

Write-Host "Starting Azure Functions runtime..." -ForegroundColor Green
Write-Host ""
Write-Host "Endpoints disponibile:" -ForegroundColor Cyan
Write-Host "  GET  http://localhost:7071/api/health" -ForegroundColor Gray
Write-Host "  POST http://localhost:7071/api/fill-d100" -ForegroundColor Gray
Write-Host "  POST http://localhost:7071/api/fill-d100-base64" -ForegroundColor Gray
Write-Host ""
Write-Host "Pentru testare, deschide un nou terminal și rulează:" -ForegroundColor Yellow
Write-Host "  .\test-local.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Apasă Ctrl+C pentru a opri serverul." -ForegroundColor Gray
Write-Host ""

# Build the project first to avoid MSBUILD conflicts
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build ANAF.AzureFunction.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting function host..." -ForegroundColor Yellow

# Run from the build output directory to avoid MSBUILD issues
Push-Location bin\Debug\net8.0
try {
    & "func" start --no-build
} finally {
    Pop-Location
}
