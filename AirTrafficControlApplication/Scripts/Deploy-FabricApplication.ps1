<#
.SYNOPSIS 
Deploys a Service Fabric application type to a cluster.

.DESCRIPTION
This script deploys a Service Fabric application type to a cluster.  It is invoked by Visual Studio when deploying a Service Fabric Application project.

.NOTES
WARNING: This script file is invoked by Visual Studio.  Its parameters must not be altered but its logic can be customized as necessary.

.PARAMETER ApplicationPackagePath
Path to the folder of the packaged Service Fabric application.

.PARAMETER DoNotCreateApplication
Indicates that the Service Fabric application should not be created after registering the application type.

.PARAMETER ApplicationParameter
Hashtable of the Service Fabric application parameters to be used for the application.

.EXAMPLE
. Scripts\Deploy-FabricApplication.ps1 -ApplicationPackagePath 'pkg\Debug'

Deploy the application using the default package location for a Debug build.

.EXAMPLE
. Scripts\Deploy-FabricApplication.ps1 -ApplicationPackagePath 'pkg\Debug' -DoNotCreateApplication

Deploy the application but do not create the application instance.

.EXAMPLE
. Scripts\Deploy-FabricApplication.ps1 -ApplicationPackagePath 'pkg\Debug' -ApplicationParameter @{CustomParameter1='MyValue'; CustomParameter2='MyValue'}

Deploy the application by providing values for parameters that are defined in the application manifest.
#>

Param
(
    [String]
    $ApplicationPackagePath,

    [Switch]
    $DoNotCreateApplication,

    [Hashtable]
    $ApplicationParameter
)

$LocalFolder = (Split-Path $MyInvocation.MyCommand.Path)

$UtilitiesModulePath = "$LocalFolder\Utilities.psm1"
Import-Module $UtilitiesModulePath

if (!$ApplicationPackagePath)
{
    $ApplicationPackagePath = "$LocalFolder\..\pkg\Release"
}

$ApplicationManifestPath = "$ApplicationPackagePath\ApplicationManifest.xml"

if (!(Test-Path $ApplicationManifestPath))
{
    throw "$ApplicationManifestPath is not found. You may need to create a package by running the 'Package' command in Visual Studio for the desired build configuration (Debug or Release)."
}

$packageValidationSuccess = (Test-ServiceFabricApplicationPackage $ApplicationPackagePath)
if (!$packageValidationSuccess)
{
    throw "Validation failed for package: $ApplicationPackagePath"
}

Write-Host 'Deploying application...'

try
{
    Write-Host 'Connecting to the cluster...'
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

$tmpPackagePath = Copy-Temp $ApplicationPackagePath $names.ApplicationTypeName
$applicationPackagePathInImageStore = $names.ApplicationTypeName

$app = Get-ServiceFabricApplication -ApplicationName $names.ApplicationName
if ($app)
{
    Write-Host 'Removing application instance...'
    $app | Remove-ServiceFabricApplication -Force
}

foreach ($node in Get-ServiceFabricNode)
{
    [void](Get-ServiceFabricDeployedReplica -NodeName $node.NodeName -ApplicationName $names.ApplicationName | Remove-ServiceFabricReplica -NodeName $node.NodeName -ForceRemove)
}

$reg = Get-ServiceFabricApplicationType -ApplicationTypeName $names.ApplicationTypeName
if ($reg)
{
    Write-Host 'Unregistering application type...'
    $reg | Unregister-ServiceFabricApplicationType -Force
}

Write-Host 'Copying application package...'
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $tmpPackagePath -ImageStoreConnectionString $imageStoreConnectionString -ApplicationPackagePathInImageStore $applicationPackagePathInImageStore

Write-Host 'Registering application type...'
Register-ServiceFabricApplicationType -ApplicationPathInImageStore $applicationPackagePathInImageStore

if (!$DoNotCreateApplication)
{
    Write-Host 'Creating application...'
    [void](New-ServiceFabricApplication -ApplicationName $names.ApplicationName -ApplicationTypeName $names.ApplicationTypeName -ApplicationTypeVersion $names.ApplicationTypeVersion -ApplicationParameter $ApplicationParameter)
    Write-Host 'Create application succeeded'
}