$projectPath = ".\QuanLyNhanSu.DesktopClient\QuanLyNhanSu.DesktopClient.csproj"
$outputDir = ".\Release_DesktopClient"

Write-Host "=================================================="
Write-Host "     DONG GOI QUAN LY NHAN SU DESKTOP CLIENT"
Write-Host "=================================================="
Write-Host "Dang don dep thu muc cu..."
if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir
}

Write-Host "Dang bien dich va dong goi (Publish)..."
dotnet publish $projectPath -c Release -r win-x64 --self-contained false -o $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "--------------------------------------------------"
    Write-Host "[THANH CONG] Ung dung da duoc dong goi tai thu muc: $outputDir" -ForegroundColor Green
    Write-Host "Luu y: Kiem tra file appsettings.json ben trong va doi URL khi mang sang may khac." -ForegroundColor Yellow
} else {
    Write-Host "--------------------------------------------------"
    Write-Host "[THAT BAI] Co loi xay ra trong qua trinh dong goi." -ForegroundColor Red
}
