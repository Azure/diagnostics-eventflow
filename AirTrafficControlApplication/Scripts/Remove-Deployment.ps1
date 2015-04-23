<#
.SYNOPSIS 
Removes the deployment of a Service Fabric application on a cluster.

.DESCRIPTION
This script removes the deployment of a Service Fabric application on a cluster.  It is invoked by Visual Studio when executing the "Remove Deployment" command of a Service Fabric Application project.

.NOTES
WARNING: This script file is invoked by Visual Studio.  Its parameters must not be altered but its logic can be customized as necessary.

.PARAMETER ApplicationManifestPath
Path to the application manifest of the Service Fabric application.

.EXAMPLE
. Scripts\Remove-Deployment.ps1 -ApplicationManifestPath 'ApplicationManifest.xml'

Removes the deployment of the application described by the application manifest.
#>

Param
(
    [String]
    $ApplicationManifestPath
)

$LocalFolder = (Split-Path $MyInvocation.MyCommand.Path)

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

Write-Host "Removing deployment..."

try
{
    [void](Connect-ServiceFabricCluster)
}
catch [System.Fabric.FabricObjectClosedException]
{
    Write-Warning "Service Fabric cluster may not be connected."
    throw
}

# Get image store connection string
$clusterManifestText = Get-ServiceFabricClusterManifest
$imageStoreConnectionString = Get-ImageStoreConnectionString ([xml] $clusterManifestText)

$names = Get-Names -ApplicationManifestPath $ApplicationManifestPath
if (!$names)
{
    return
}

$applicationPackagePathInImageStore = $names.ApplicationTypeName

$app = Get-ServiceFabricApplication -ApplicationName $names.ApplicationName
if ($app)
{
    $app | Remove-ServiceFabricApplication -Force
}

foreach ($node in Get-ServiceFabricNode)
{
    [void](Get-ServiceFabricDeployedReplica -NodeName $node.NodeName -ApplicationName $names.ApplicationName | Remove-ServiceFabricReplica -NodeName $node.NodeName -ForceRemove)
}

$reg = Get-ServiceFabricApplicationType -ApplicationTypeName $names.ApplicationTypeName
if ($reg)
{
    $reg | Unregister-ServiceFabricApplicationType -Force
}

Remove-ServiceFabricApplicationPackage -ApplicationPackagePathInImageStore $applicationPackagePathInImageStore -ImageStoreConnectionString $imageStoreConnectionString

Write-Host "Finished removing the deployment"