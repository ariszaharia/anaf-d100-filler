# Script de testare locală pentru Azure Functions ANAF

param(
    [string]$BaseUrl = "http://localhost:7071",
    [string]$PdfPath = "decl100.pdf",
    [string]$JsonPath = "date_declaratie.json"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Azure Functions ANAF - Local" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Test Health Check
Write-Host "[1] Testing Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get
    Write-Host "✓ Health: $($health.status)" -ForegroundColor Green
    Write-Host "  Service: $($health.service)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "✗ Health Check Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# 2. Test Fill D100 Base64
Write-Host "[2] Testing Fill D100 Base64..." -ForegroundColor Yellow

if (-not (Test-Path $PdfPath)) {
    Write-Host "✗ PDF not found: $PdfPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $JsonPath)) {
    Write-Host "✗ JSON not found: $JsonPath" -ForegroundColor Red
    exit 1
}

$jsonContent = Get-Content $JsonPath -Raw

# Creare multipart/form-data request
$boundary = [System.Guid]::NewGuid().ToString()
$LF = "`r`n"

$bodyLines = @(
    "--$boundary",
    "Content-Disposition: form-data; name=`"pdf`"; filename=`"$PdfPath`"",
    "Content-Type: application/pdf",
    "",
    ""
)
$bodyStart = ($bodyLines -join $LF)

$bodyEnd = @(
    "",
    "--$boundary",
    "Content-Disposition: form-data; name=`"json`"",
    "",
    $jsonContent,
    "--$boundary--",
    ""
) -join $LF

$pdfBytes = [System.IO.File]::ReadAllBytes($PdfPath)
$startBytes = [System.Text.Encoding]::UTF8.GetBytes($bodyStart)
$endBytes = [System.Text.Encoding]::UTF8.GetBytes($bodyEnd)

$bodyBytes = New-Object byte[] ($startBytes.Length + $pdfBytes.Length + $endBytes.Length)
[System.Array]::Copy($startBytes, 0, $bodyBytes, 0, $startBytes.Length)
[System.Array]::Copy($pdfBytes, 0, $bodyBytes, $startBytes.Length, $pdfBytes.Length)
[System.Array]::Copy($endBytes, 0, $bodyBytes, $startBytes.Length + $pdfBytes.Length, $endBytes.Length)

$headers = @{ "Content-Type" = "multipart/form-data; boundary=$boundary" }

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/fill-d100-base64" -Method Post -Body $bodyBytes -Headers $headers
    
    Write-Host "✓ Success: $($response.success)" -ForegroundColor Green
    Write-Host "  FileName: $($response.fileName)" -ForegroundColor Gray
    Write-Host "  PDF Base64 Length: $($response.pdfBase64.Length) characters" -ForegroundColor Gray
    Write-Host "  XML Length: $($response.xmlContent.Length) characters" -ForegroundColor Gray
    Write-Host ""
    
    # Salvează PDF-ul
    $pdfData = [System.Convert]::FromBase64String($response.pdfBase64)
    $outputPath = "test_output_$($response.fileName)"
    [System.IO.File]::WriteAllBytes($outputPath, $pdfData)
    Write-Host "✓ PDF salvat: $outputPath" -ForegroundColor Green
    
    # Salvează XML-ul
    $xmlPath = "test_output_D100_$(Get-Date -Format 'yyyyMMdd_HHmmss').xml"
    $response.xmlContent | Out-File -FilePath $xmlPath -Encoding UTF8
    Write-Host "✓ XML salvat: $xmlPath" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Fill D100 Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
