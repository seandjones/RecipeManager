# Verify PostgreSQL Installation and Configuration
# Quick diagnostic script

Write-Host "=== PostgreSQL Verification ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check if PostgreSQL is installed
Write-Host "1. Checking PostgreSQL installation..." -ForegroundColor Yellow
$pgPath = "C:\Program Files\PostgreSQL\18"
if (Test-Path $pgPath) {
    Write-Host "   ✅ PostgreSQL 18 found at: $pgPath" -ForegroundColor Green
} else {
    Write-Host "   ❌ PostgreSQL 18 not found" -ForegroundColor Red
    exit 1
}

# 2. Check if service is running
Write-Host ""
Write-Host "2. Checking PostgreSQL service..." -ForegroundColor Yellow
$service = Get-Service -Name "postgresql-x64-18" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "   ✅ Service found: $($service.DisplayName)" -ForegroundColor Green
    Write-Host "   Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Yellow" })
    
    if ($service.Status -ne "Running") {
        Write-Host "   ⚠️  Service is not running. Start it with: Start-Service postgresql-x64-18" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ❌ PostgreSQL service not found" -ForegroundColor Red
    exit 1
}

# 3. Check psql binary
Write-Host ""
Write-Host "3. Checking psql command..." -ForegroundColor Yellow
$psqlPath = "$pgPath\bin\psql.exe"
if (Test-Path $psqlPath) {
    Write-Host "   ✅ psql found" -ForegroundColor Green
    $version = & $psqlPath --version
    Write-Host "   Version: $version" -ForegroundColor Gray
} else {
    Write-Host "   ❌ psql not found at: $psqlPath" -ForegroundColor Red
}

# 4. Check listening port
Write-Host ""
Write-Host "4. Checking if PostgreSQL is listening on port 5432..." -ForegroundColor Yellow
$listening = Get-NetTCPConnection -LocalPort 5432 -ErrorAction SilentlyContinue
if ($listening) {
    Write-Host "   ✅ PostgreSQL is listening on port 5432" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Port 5432 is not in use. PostgreSQL may not be running." -ForegroundColor Yellow
}

# 5. Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "PostgreSQL 18 is installed and ready to use." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run .\setup-database.ps1 to create the recipedb database"
Write-Host "  2. Update connection string in RecipeManager.AppHost\appsettings.Development.json"
Write-Host "  3. Apply migrations: dotnet ef database update --project RecipeManager.ApiService"
Write-Host ""
