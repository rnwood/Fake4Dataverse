param (
    [string]$targetFrameworks = "netcoreapp3.1"
 )

./build.ps1 -targetFramework $targetFrameworks -configuration "FAKE_XRM_EASY_9"


Write-Host "Build All Configurations Succeeded  :)" -ForegroundColor Green