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

    $nugetProjects = &'findstr.exe' /sm packOptions project.json

    $nugetCmd = Join-Path $PSScriptRoot nuget.exe

    if ([string]::IsNullOrEmpty($Suffix))
    {
        foreach($projectFile in $nugetProjects)
        {
            &"dotnet" pack "$projectFile" --no-build -c "$Configuration" -o "$destination"
        }
        &$nugetCmd pack "$repoRoot\nuspecs\Suite\Microsoft.Diagnostics.EventFlow.Suite.nuspec" -OutputDirectory "$destination"
        &$nugetCmd pack "$repoRoot\nuspecs\ServiceFabric\Microsoft.Diagnostics.EventFlow.ServiceFabric.nuspec" -BasePath "$repoRoot" -OutputDirectory "$destination" -Properties "Configuration=$Configuration"
    }
    else
    {
        foreach($projectFile in $nugetProjects)
        {
            &"dotnet" pack "$projectFile" --no-build -c "$Configuration" -o "$destination" --version-suffix $Suffix
        }
        &$nugetCmd pack "$repoRoot\nuspecs\Suite\Microsoft.Diagnostics.EventFlow.Suite.nuspec" -OutputDirectory "$destination" -Suffix $Suffix
        &$nugetCmd pack "$repoRoot\nuspecs\ServiceFabric\Microsoft.Diagnostics.EventFlow.ServiceFabric.nuspec" -BasePath "$repoRoot" -OutputDirectory "$destination" -Suffix $Suffix -Properties "Configuration=$Configuration"
    }    
}
finally 
{
    Set-Location -Path $currentDir
}
