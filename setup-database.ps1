# Setup PostgreSQL Database for RecipeManager
# Run this script to create the database and user

Write-Host "=== PostgreSQL Setup for RecipeManager ===" -ForegroundColor Cyan
Write-Host ""

$PSQL_PATH = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

# Check if PostgreSQL is installed
if (!(Test-Path $PSQL_PATH)) {
    Write-Host "❌ PostgreSQL not found at: $PSQL_PATH" -ForegroundColor Red
    Write-Host "Please install PostgreSQL 18 first." -ForegroundColor Yellow
    exit 1
}

# Check if service is running
$service = Get-Service -Name "postgresql-x64-18" -ErrorAction SilentlyContinue
if ($service.Status -ne "Running") {
    Write-Host "⚠️  PostgreSQL service is not running. Starting..." -ForegroundColor Yellow
    Start-Service postgresql-x64-18
    Start-Sleep -Seconds 3
}

Write-Host "✅ PostgreSQL 18 service is running" -ForegroundColor Green
Write-Host ""

# Prompt for postgres password
Write-Host "Enter the postgres superuser password (set during PostgreSQL installation):" -ForegroundColor Yellow
$postgresPassword = Read-Host -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($postgresPassword)
$env:PGPASSWORD = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

Write-Host ""
Write-Host "Creating database and user..." -ForegroundColor Cyan

try {
    # Test connection
    Write-Host "Testing connection to PostgreSQL..." -ForegroundColor Gray
    & $PSQL_PATH -U postgres -h localhost -c "SELECT version();" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to connect to PostgreSQL. Please check your password." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Connected to PostgreSQL" -ForegroundColor Green
    
    # Create database
    Write-Host "Creating database 'recipedb'..." -ForegroundColor Gray
    & $PSQL_PATH -U postgres -h localhost -c "CREATE DATABASE recipedb;" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database 'recipedb' created" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Database 'recipedb' may already exist (this is OK)" -ForegroundColor Yellow
    }
    
    # Create user
    Write-Host "Creating user 'recipeuser'..." -ForegroundColor Gray
    & $PSQL_PATH -U postgres -h localhost -c "CREATE USER recipeuser WITH PASSWORD 'recipe_dev_password';" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ User 'recipeuser' created" -ForegroundColor Green
    } else {
        Write-Host "⚠️  User 'recipeuser' may already exist (this is OK)" -ForegroundColor Yellow
    }
    
    # Grant privileges
    Write-Host "Granting privileges..." -ForegroundColor Gray
    & $PSQL_PATH -U postgres -h localhost -c "GRANT ALL PRIVILEGES ON DATABASE recipedb TO recipeuser;" 2>&1 | Out-Null
    & $PSQL_PATH -U postgres -h localhost -c "ALTER DATABASE recipedb OWNER TO recipeuser;" 2>&1 | Out-Null
    & $PSQL_PATH -U postgres -h localhost -d recipedb -c "GRANT ALL ON SCHEMA public TO recipeuser;" 2>&1 | Out-Null
    
    Write-Host "✅ Privileges granted" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "=== Setup Complete ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Database Configuration:" -ForegroundColor Cyan
    Write-Host "  Host: localhost" -ForegroundColor Gray
    Write-Host "  Port: 5432" -ForegroundColor Gray
    Write-Host "  Database: recipedb" -ForegroundColor Gray
    Write-Host "  Username: recipeuser" -ForegroundColor Gray
    Write-Host "  Password: recipe_dev_password" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Connection String:" -ForegroundColor Cyan
    Write-Host "  Host=localhost;Port=5432;Database=recipedb;Username=recipeuser;Password=recipe_dev_password" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Update RecipeManager.AppHost/appsettings.Development.json with the postgres password" -ForegroundColor Yellow
    Write-Host "  2. Run: dotnet ef database update --project RecipeManager.ApiService" -ForegroundColor Yellow
    Write-Host "  3. Run: dotnet run --project RecipeManager.AppHost" -ForegroundColor Yellow
    
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    $env:PGPASSWORD = $null
}
