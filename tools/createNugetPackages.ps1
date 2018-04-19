param (
    [ValidateSet("Debug", "Release")] [String] $Configuration = "Release",
    [String] $Suffix = "",
    [switch] $CleanDestinationFolder
)

$currentDir = (Get-Location).Path;
$repoRoot = Split-Path $PSScriptRoot -Parent
$destination = "$repoRoot\nugets"

try 
{
    Set-Location -Path $repoRoot

    if ($CleanDestinationFolder -and (Test-Path "$destination"))
    {
        Remove-Item "$destination" -Recurse -Force
    }

    $nugetProjects = &'findstr.exe' /sm PackageId *.csproj

    $nugetCmd = Join-Path $PSScriptRoot nuget.exe

    if ([string]::IsNullOrEmpty($Suffix))
    {
        foreach($projectFile in $nugetProjects)
        {
            &"dotnet" pack "$projectFile" --no-build -c "$Configuration" -o "$destination"
        }
    }
    else
    {
        foreach($projectFile in $nugetProjects)
        {
            &"dotnet" pack "$projectFile" --no-build -c "$Configuration" -o "$destination" --version-suffix $Suffix
        }
    }    
}
finally 
{
    Set-Location -Path $currentDir
}
