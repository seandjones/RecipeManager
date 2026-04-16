# Simple script to start RecipeManager locally without Docker
# Run this script, then open https://localhost:7274 in your browser

Write-Host "🚀 RecipeManager - Local Development (No Docker)" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will start the API service. After it's running," -ForegroundColor Yellow
Write-Host "open a NEW terminal and run: cd RecipeManager.Web; dotnet run" -ForegroundColor Yellow
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green

# Start API Service
Write-Host ""
Write-Host "🔌 Starting API Service..." -ForegroundColor Cyan
Write-Host "   URL: https://localhost:7000" -ForegroundColor White
Write-Host "   📧 Email codes will appear in this console" -ForegroundColor Yellow
Write-Host ""

cd RecipeManager.ApiService
dotnet run --urls "https://localhost:7000;http://localhost:5000"
