<#
.SYNOPSIS 
Creates an instance of a Service Fabric application type.

.DESCRIPTION
This script creates an instance of a Service Fabric application type.  It is invoked by Visual Studio when executing the "Create Application" command of a Service Fabric Application project.

.NOTES
WARNING: This script file is invoked by Visual Studio.  Its parameters must not be altered but its logic can be customized as necessary.

.PARAMETER PublishProfileFile
Path to the file containing the publish profile.

.PARAMETER ApplicationManifestPath
Path to the application manifest of the Service Fabric application.

.PARAMETER ApplicationParameter
Hashtable of the Service Fabric application parameters to be used for the application.

.EXAMPLE
. Scripts\Create-FabricApplication.ps1 -ApplicationManifestPath 'ApplicationManifest.xml'

Create the application using the application manifest file.

.EXAMPLE
. Scripts\Create-FabricApplication.ps1 -ApplicationManifestPath 'ApplicationManifest.xml' -ApplicationParameter @{CustomParameter1='MyValue'; CustomParameter2='MyValue'}

Create the application by providing values for parameters that are defined in the application manifest.
#>

Param
(
    [String]
    $PublishProfileFile,

    [String]
    $ApplicationManifestPath,
    
    [Hashtable]
    $ApplicationParameter
)

$LocalFolder = (Split-Path $MyInvocation.MyCommand.Path)

if (!$PublishProfileFile)
{
    $PublishProfileFile = "$LocalFolder\..\PublishProfiles\Local.xml"
}

if (!$ApplicationManifestPath)
{
    $ApplicationManifestPath = "$LocalFolder\..\ApplicationManifest.xml"
}

if (!(Test-Path $ApplicationManifestPath))
{
    throw "$ApplicationManifestPath is not found."
}

$UtilitiesModulePath = "$LocalFolder\Utilities.psm1"
Import-Module $UtilitiesModulePath

$PublishProfile = Read-PublishProfile $PublishProfileFile
$ClusterConnectionParameters = $PublishProfile.ClusterConnectionParameters

try
{
    [void](Connect-ServiceFabricCluster @ClusterConnectionParameters)
}
catch [System.Fabric.FabricObjectClosedException]
{
    Write-Warning "Service Fabric cluster may not be connected."
    throw
}

$names = Get-Names -ApplicationManifestPath $ApplicationManifestPath -PublishProfile $PublishProfile
if (!$names)
{
    return
}

New-ServiceFabricApplication -ApplicationName $names.ApplicationName -ApplicationTypeName $names.ApplicationTypeName -ApplicationTypeVersion $names.ApplicationTypeVersion -ApplicationParameter $ApplicationParameter
