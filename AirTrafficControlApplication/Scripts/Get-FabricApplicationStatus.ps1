<#
.SYNOPSIS 
Outputs messages indicating the readiness of a Service Fabric application.

.DESCRIPTION
This script outputs messages indicating the readiness of a Service Fabric application.  It is invoked by Visual Studio after starting a Service Fabric Application project.

.NOTES
WARNING: This script file is invoked by Visual Studio.  Its parameters must not be altered but its logic can be customized as necessary.

.PARAMETER ApplicationManifestPath
Path to the application manifest of the Service Fabric application.

.EXAMPLE
. Scripts\Get-FabricApplicationStatus.ps1 -ApplicationManifestPath 'ApplicationManifest.xml'

Get the status of a deployed application as described by the application manifest.
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

try
{
    [void](Connect-ServiceFabricCluster)
}
catch [System.Fabric.FabricObjectClosedException]
{
    Write-Warning "Service Fabric cluster may not be connected."
    throw
}

$names = Get-Names -ApplicationManifestPath $ApplicationManifestPath
if (!$names)
{
    return
}

$started = $false
$ready = $false
$retryCount = 20
do
{
    try
    {
        $app = Get-ServiceFabricApplication -ApplicationName $names.ApplicationName
        if ($app)
        {   
            if (!$started)
            {
                $started = $true
                Write-Host "The application has started."
            }

            $ready = $true
            Write-Host "Service Status:"
            $services = $app | Get-ServiceFabricService
            foreach($s in $services)
            {
                $remaining = $s | Get-ServiceFabricPartition | Where-Object {$_.PartitionStatus -ne "Ready"} | Measure
                if($remaining.Count -gt 0)
                {
                    $ready = $false
                    Write-Host "$($s.ServiceName) is not ready, $($remaining.Count) partitions remaining."
                }
                else
                {
                    Write-Host "$($s.ServiceName) is ready."
                }
            }
        }
        else
        {
            Write-Host "Waiting for the application to start."
        }
        Write-Host ""
    }
    finally
    {
        if(!$ready)
        {
            Start-Sleep -Seconds 5
        }
        $retryCount--
    }
} while (!$ready -and $retryCount -gt 0)

if(!$ready)
{
    Write-Host "Something is taking too long, the application is still not ready."
}
else
{
    Write-Host "The application is ready."
}