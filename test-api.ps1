# Test Script for QuanLyNhanSu API - PowerShell 5.1 compatible

$baseUri = "https://localhost:44387"
$adminUser = "admin"
$adminPass = "1q2w3E*"

# Bypass SSL certificate validation for PowerShell 5.1
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

# 1. Test Login - Get Token from OpenIddict Token Endpoint
Write-Host "=== 1. Testing Token Endpoint ===" -ForegroundColor Green
$tokenBody = @"
grant_type=password&username=admin&password=1q2w3E*&client_id=QuanLyNhanSu_Swagger&scope=QuanLyNhanSu
"@

$tokenResponse = Invoke-RestMethod -Uri "$baseUri/connect/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $tokenBody

Write-Host "Token response: $($tokenResponse | ConvertTo-Json)" -ForegroundColor Cyan

$token = $tokenResponse.access_token
$tokenType = "Bearer"
Write-Host "Login successful!" -ForegroundColor Green
Write-Host "Token: $($token.Substring(0, 20))..." -ForegroundColor Yellow
Write-Host ""

# 2. Test Create User Key
Write-Host "=== 2. Testing Create User Key ===" -ForegroundColor Green
$keyBody = @{
    role = "admin"
    description = "Test key"
    expirationDate = $null
} | ConvertTo-Json

$headers = @{
    "Authorization" = "$tokenType $token"
    "Content-Type" = "application/json"
}

try {
    $keyResponse = Invoke-WebRequest -Uri "$baseUri/api/userkey/create" `
        -Method Post `
        -Headers $headers `
        -Body $keyBody `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Write-Host "Response Status: $($keyResponse.StatusCode)" -ForegroundColor Yellow
    Write-Host "Response Content: $($keyResponse.Content)" -ForegroundColor Cyan
    
    $keyData = $keyResponse.Content | ConvertFrom-Json
    Write-Host "Key created successfully!" -ForegroundColor Green
    $keyValue = $keyData.data.key
    Write-Host "Created Key: $keyValue" -ForegroundColor Yellow
    Write-Host ""
} catch {
    Write-Host "Error during key creation:" -ForegroundColor Red
    Write-Host "Exception: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Error Details: $($_.ErrorDetails)" -ForegroundColor Red
    }
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "Response Body: $errorBody" -ForegroundColor Red
        $reader.Close()
    }
    exit 1
}
