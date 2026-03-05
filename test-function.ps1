# Simple test script for ANAF Azure Functions
param(
    [string]$BaseUrl = "http://localhost:7071"
)

Write-Host "Testing ANAF Azure Functions..." -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "[1] Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get
    Write-Host "SUCCESS: $($health.status) - $($health.service)" -ForegroundColor Green
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Fill D100
Write-Host "[2] Fill D100 (Base64 response)..." -ForegroundColor Yellow

if (-not (Test-Path "decl100.pdf")) {
    Write-Host "FAILED: decl100.pdf not found" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "date_declaratie.json")) {
    Write-Host "FAILED: date_declaratie.json not found" -ForegroundColor Red
    exit 1
}

# Use curl.exe for reliable multipart/form-data
$response = curl.exe -s -X POST "$BaseUrl/api/fill-d100-base64" `
    -F "pdf=@decl100.pdf" `
    -F "json=@date_declaratie.json" | ConvertFrom-Json

if ($response.success) {
    Write-Host "SUCCESS: $($response.fileName)" -ForegroundColor Green
    
    # Save the PDF
    $pdfBytes = [Convert]::FromBase64String($response.pdfBase64)
    $outputFile = "output_filled.pdf"
    [IO.File]::WriteAllBytes($outputFile, $pdfBytes)
    Write-Host "  Saved: $outputFile" -ForegroundColor Gray
    
    # Save the XML
    $xmlFile = "output_D100.xml"
    $response.xmlContent | Out-File -FilePath $xmlFile -Encoding UTF8
    Write-Host "  Saved: $xmlFile" -ForegroundColor Gray
} else {
    Write-Host "FAILED: $($response.error)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test complete!" -ForegroundColor Cyan
