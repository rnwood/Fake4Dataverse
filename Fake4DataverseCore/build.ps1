param (
    [string]$targetFramework = "all",
    [string]$configuration = "FAKE_XRM_EASY_9"
 )

$localPackagesFolder = '../local-packages'
Write-Host "Checking if local packages folder '$($localPackagesFolder)' exists..."

$packagesFolderExists = Test-Path $localPackagesFolder -PathType Container

if(!($packagesFolderExists)) 
{
    New-Item $localPackagesFolder -ItemType Directory
}
if($targetFramework -eq "all")
{
    dotnet restore /p:Configuration=$configuration
}
else {
    dotnet restore /p:Configuration=$configuration -p:TargetFrameworks=$targetFramework
}
if(!($LASTEXITCODE -eq 0)) {
    throw "Error restoring packages"
}

if($targetFramework -eq "all")
{
    dotnet build --configuration $configuration --no-restore
}
else 
{
    dotnet build --configuration $configuration --no-restore --framework $targetFramework
}
if(!($LASTEXITCODE -eq 0)) {
    throw "Error during build step"
}

# For testing: always test net8.0 only since net462 can't run on Linux
# When targetFramework is "all", we build both frameworks but only test net8.0
dotnet test --configuration $configuration --no-restore --framework net8.0 --verbosity normal --collect:"XPlat code coverage" --settings tests/.runsettings --results-directory ./coverage

if(!($LASTEXITCODE -eq 0)) {
    throw "Error during test step"
}

Write-Host  "*** Succeeded :)  **** " -ForegroundColor Green
