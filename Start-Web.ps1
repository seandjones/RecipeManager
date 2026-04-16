# Start the Web Frontend
# Make sure you've started the API first with Start-API.ps1

Write-Host "🌐 Starting Web Frontend..." -ForegroundColor Cyan
Write-Host "   URL: https://localhost:7274" -ForegroundColor White
Write-Host ""
Write-Host "After it starts, open your browser to:" -ForegroundColor Yellow
Write-Host "   https://localhost:7274" -ForegroundColor Green
Write-Host ""
Write-Host "To test authentication:" -ForegroundColor Yellow
Write-Host "   1. Go to https://localhost:7274/counter" -ForegroundColor White
Write-Host "   2. You'll be redirected to /login" -ForegroundColor White
Write-Host "   3. Enter your email" -ForegroundColor White
Write-Host "   4. Check the API console for the 6-digit code" -ForegroundColor White
Write-Host "   5. Enter the code to authenticate" -ForegroundColor White
Write-Host ""

cd RecipeManager.Web
dotnet run --urls "https://localhost:7274;http://localhost:5274"
