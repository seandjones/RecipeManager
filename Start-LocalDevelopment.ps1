# Run RecipeManager Locally (Without Docker)
# 
# This script runs the API and Web services locally without Docker/Aspire orchestration.
# Perfect for development in VMs where Docker isn't available.

Write-Host "🚀 Starting RecipeManager (Local Mode - No Docker)" -ForegroundColor Cyan
Write-Host ""

# Check if PostgreSQL is running
Write-Host "Checking PostgreSQL connection..." -ForegroundColor Yellow
try {
    $env:PGPASSWORD = "recipe_dev_password"
    $result = & psql -h localhost -p 5432 -U recipeuser -d recipedb -c "SELECT 1;" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PostgreSQL is running and accessible" -ForegroundColor Green
    } else {
        Write-Host "❌ PostgreSQL connection failed. Run setup-database.ps1 first!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ PostgreSQL not found. Install PostgreSQL and run setup-database.ps1" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📦 Building projects..." -ForegroundColor Yellow
dotnet build RecipeManager.sln --configuration Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Start API service in background
Write-Host "🔌 Starting API Service on https://localhost:7000 and http://localhost:5000" -ForegroundColor Cyan
$apiJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    cd RecipeManager.ApiService
    dotnet run --urls "https://localhost:7000;http://localhost:5000"
}

# Wait a few seconds for API to start
Write-Host "Waiting for API service to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Test API health
try {
    $response = Invoke-WebRequest -Uri "https://localhost:7000/" -SkipCertificateCheck -TimeoutSec 5
    Write-Host "✅ API Service is running!" -ForegroundColor Green
} catch {
    Write-Host "⚠️ API Service starting (may take a moment)..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🌐 Starting Web Frontend on https://localhost:7274 and http://localhost:5274" -ForegroundColor Cyan
$webJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    cd RecipeManager.Web
    dotnet run --urls "https://localhost:7274;http://localhost:5274"
}

# Wait for Web to start
Write-Host "Waiting for Web frontend to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "✅ RecipeManager is running!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Web Frontend:  https://localhost:7274" -ForegroundColor Cyan
Write-Host "🔌 API Service:   https://localhost:7000" -ForegroundColor Cyan
Write-Host "🗄️  PostgreSQL:   localhost:5432" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 To test authentication:" -ForegroundColor Yellow
Write-Host "   1. Navigate to https://localhost:7274/counter" -ForegroundColor White
Write-Host "   2. You'll be redirected to /login" -ForegroundColor White
Write-Host "   3. Enter your email and click 'Send Code'" -ForegroundColor White
Write-Host "   4. Check this console for the 6-digit code" -ForegroundColor White
Write-Host "   5. Enter code at /verify-code to authenticate" -ForegroundColor White
Write-Host ""
Write-Host "📊 View logs:" -ForegroundColor Yellow
Write-Host "   API Logs:  Receive-Job -Id $($apiJob.Id) -Keep" -ForegroundColor White
Write-Host "   Web Logs:  Receive-Job -Id $($webJob.Id) -Keep" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop all services..." -ForegroundColor Red
Write-Host ""

# Monitor jobs and display output
try {
    while ($true) {
        # Show API output
        $apiOutput = Receive-Job -Id $apiJob.Id
        if ($apiOutput) {
            Write-Host "[API] " -ForegroundColor Magenta -NoNewline
            Write-Host $apiOutput
        }

        # Show Web output
        $webOutput = Receive-Job -Id $webJob.Id
        if ($webOutput) {
            Write-Host "[WEB] " -ForegroundColor Blue -NoNewline
            Write-Host $webOutput
        }

        # Check if jobs are still running
        if ($apiJob.State -ne 'Running' -or $webJob.State -ne 'Running') {
            Write-Host ""
            Write-Host "⚠️ One or more services stopped unexpectedly" -ForegroundColor Yellow
            
            if ($apiJob.State -ne 'Running') {
                Write-Host "API Service stopped. Last output:" -ForegroundColor Red
                Receive-Job -Id $apiJob.Id
            }
            
            if ($webJob.State -ne 'Running') {
                Write-Host "Web Service stopped. Last output:" -ForegroundColor Red
                Receive-Job -Id $webJob.Id
            }
            
            break
        }

        Start-Sleep -Milliseconds 500
    }
} finally {
    Write-Host ""
    Write-Host "🛑 Stopping services..." -ForegroundColor Yellow
    
    # Stop jobs
    Stop-Job -Id $apiJob.Id, $webJob.Id -ErrorAction SilentlyContinue
    Remove-Job -Id $apiJob.Id, $webJob.Id -Force -ErrorAction SilentlyContinue
    
    Write-Host "✅ All services stopped" -ForegroundColor Green
}
