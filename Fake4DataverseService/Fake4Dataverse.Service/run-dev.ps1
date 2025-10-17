#!/usr/bin/env pwsh
# Development script to run both backend and frontend concurrently
# This provides a single command to start full-stack development with hot reload

Write-Host "=== Starting Fake4Dataverse Full-Stack Development ===" -ForegroundColor Green
Write-Host ""
Write-Host "This script will start:" -ForegroundColor Cyan
Write-Host "  1. ASP.NET Core backend on http://localhost:5000" -ForegroundColor Cyan
Write-Host "  2. Next.js frontend on http://localhost:3000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Both will have hot reload enabled. Press Ctrl+C to stop both." -ForegroundColor Yellow
Write-Host ""

# Start backend in background
Write-Host "Starting backend service..." -ForegroundColor Green
$backendJob = Start-Job -ScriptBlock {
    Set-Location $using:PSScriptRoot
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    dotnet run -- start --port 5000
}

# Wait a moment for backend to start
Start-Sleep -Seconds 3

# Start frontend in background
Write-Host "Starting frontend development server..." -ForegroundColor Green
$frontendJob = Start-Job -ScriptBlock {
    Set-Location "$using:PSScriptRoot/mda-app"
    npm run dev
}

# Monitor both jobs and show their output
Write-Host ""
Write-Host "=== Services Started ===" -ForegroundColor Green
Write-Host "Backend: http://localhost:5000" -ForegroundColor Cyan
Write-Host "Frontend: http://localhost:3000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop all services..." -ForegroundColor Yellow
Write-Host ""

try {
    # Monitor and display output from both jobs
    while ($true) {
        # Show backend output
        $backendOutput = Receive-Job -Job $backendJob
        if ($backendOutput) {
            Write-Host $backendOutput
        }
        
        # Show frontend output
        $frontendOutput = Receive-Job -Job $frontendJob
        if ($frontendOutput) {
            Write-Host $frontendOutput
        }
        
        # Check if jobs are still running
        if ($backendJob.State -eq 'Failed' -or $frontendJob.State -eq 'Failed') {
            Write-Host "One or more services failed!" -ForegroundColor Red
            break
        }
        
        Start-Sleep -Milliseconds 500
    }
}
finally {
    # Cleanup: stop both jobs
    Write-Host ""
    Write-Host "Stopping services..." -ForegroundColor Yellow
    Stop-Job -Job $backendJob, $frontendJob
    Remove-Job -Job $backendJob, $frontendJob
    Write-Host "Services stopped." -ForegroundColor Green
}
