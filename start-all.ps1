# [ONBOARDING COMMENT]: Script này dùng để tự động khởi động đồng thời Backend API (ABP Framework) và UI Middleware (WinForms) trong 2 cửa sổ Console riêng biệt, giúp dễ dàng theo dõi log.

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "🚀 KHỞI ĐỘNG HỆ THỐNG QUẢN LÝ NHÂN SỰ" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Khởi động Backend API
Write-Host "[1/2] Đang khởi động Backend API (ABP Framework)..." -ForegroundColor Yellow
$apiPath = Join-Path -Path $PWD -ChildPath "aspnet-core\src\QuanLyNhanSu.HttpApi.Host"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$apiPath'; Write-Host '--- BACKEND API LOG ---' -ForegroundColor Green; dotnet run" -Title "Backend API - HttpApi.Host"

# Đợi 3 giây để API khởi động trước khi mở UI (đảm bảo UI có thể ping được API ngay lập tức)
Start-Sleep -Seconds 3

# 2. Khởi động UI Middleware (WinForms)
Write-Host "[2/2] Đang khởi động Middleware UI (WinForms)..." -ForegroundColor Yellow
$uiPath = Join-Path -Path $PWD -ChildPath "QuanLyNhanSu.DesktopClient"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$uiPath'; Write-Host '--- WINFORMS MIDDLEWARE LOG ---' -ForegroundColor Cyan; dotnet run" -Title "Middleware UI - Desktop Client"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ Đã gửi lệnh khởi động! Vui lòng kiểm tra 2 cửa sổ PowerShell mới bật lên." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
