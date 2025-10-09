param (
    [string]$versionSuffix = "",
    [string]$targetFrameworks = "netcoreapp3.1",
    [string]$configuration = "FAKE_XRM_EASY_9"
 )

Write-Host "Packing configuration $($configuration)..."

$project = "FakeXrmEasy.Core"
$packageId = $('"' + $project + '.v9"')

Write-Host "Running with versionSuffix '$($versionSuffix)'..."

$tempNupkgFolder = './nupkgs'

Write-Host "Packing assembly for targetFrameworks $($targetFrameworks)..."
if($targetFrameworks -eq "all")
{
    if($versionSuffix -eq "") 
    {
        dotnet pack --configuration $configuration /p:PackageId=$packageId -o $tempNupkgFolder src/$project/$project.csproj
    }
    else {
        dotnet pack --configuration $configuration /p:PackageId=$packageId -o $tempNupkgFolder src/$project/$project.csproj --version-suffix $versionSuffix
    }
}
else 
{
    if($versionSuffix -eq "") 
    {
        dotnet pack --configuration $configuration /p:PackageId=$packageId -p:TargetFrameworks=$targetFrameworks -o $tempNupkgFolder src/$project/$project.csproj
    }
    else {
        dotnet pack --configuration $configuration /p:PackageId=$packageId -p:TargetFrameworks=$targetFrameworks -o $tempNupkgFolder src/$project/$project.csproj --version-suffix $versionSuffix
    }
}
if(!($LASTEXITCODE -eq 0)) {
    throw "Error when packing the assembly"
}

Write-Host $("Pack $($packageId) Succeeded :)") -ForegroundColor Green