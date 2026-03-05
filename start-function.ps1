# Simple script to start Azure Functions
# Handles both cases: func in PATH or not

Write-Host "Building project..." -ForegroundColor Cyan
dotnet build ANAF.AzureFunction.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting Azure Functions..." -ForegroundColor Green
Write-Host "Endpoints:" -ForegroundColor Cyan
Write-Host "  GET  http://localhost:7071/api/health"
Write-Host "  POST http://localhost:7071/api/fill-d100"
Write-Host "  POST http://localhost:7071/api/fill-d100-base64"
Write-Host ""

# Try to find func.exe
$funcPath = "func"
if (-not (Get-Command "func" -ErrorAction SilentlyContinue)) {
    $funcPath = "C:\Program Files\Microsoft\Azure Functions Core Tools\func.exe"
    if (-not (Test-Path $funcPath)) {
        Write-Host "func.exe not found. Please install Azure Functions Core Tools." -ForegroundColor Red
        Write-Host "Run: winget install Microsoft.Azure.FunctionsCoreTools" -ForegroundColor Yellow
        exit 1
    }
}

# Run from build output to avoid MSBUILD conflicts
Set-Location bin\Debug\net8.0
& $funcPath start --no-build
