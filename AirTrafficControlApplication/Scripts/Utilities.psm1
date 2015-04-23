function Copy-Temp
{
    <#
    .SYNOPSIS 
    Copies files to a temp folder.

    .PARAMETER From
    Source location from which to copy files.

    .PARAMETER Name
    Folder name within temp location to store the files.
    #>

    [CmdletBinding()]
    Param
    (
        [String]
        $From,
        
        [String]
        $Name
    )

    if (!(Test-Path $From))
    {
        return $null
    }

    $To = $env:Temp + '\' + $Name
    
    if (Test-Path $To)
    {
        Remove-Item -Path $To -Recurse -ErrorAction Stop | Out-Null
    }

    New-Item $To -ItemType directory | Out-Null

    robocopy "$From" "$To" /E | Out-Null

    return $env:Temp + '\' + $Name
}

function Get-Names
{
    <#
    .SYNOPSIS 
    Returns an object containing common information from the application manifest.

    .PARAMETER ApplicationManifestPath
    Path to the application manifest file.
    #>

    [CmdletBinding()]
    Param
    (
        [String]
        $ApplicationManifestPath
    )

    $appXml = [xml] (Get-Content $ApplicationManifestPath)
    if (!$appXml)
    {
        return
    }

    $appMan = $appXml.ApplicationManifest
    $FabricNamespace = 'fabric:'
    $appTypeSuffix = 'Type'

    $shortAppName = $appMan.ApplicationTypeName
    if ($shortAppName.EndsWith($appTypeSuffix))
    {
        $shortAppName = $shortAppName.Substring(0, $shortAppName.Length - $appTypeSuffix.Length)
    }

    $h = @{
        FabricNamespace = $FabricNamespace;
        ApplicationTypeName = $appMan.ApplicationTypeName;
        ApplicationTypeVersion = $appMan.ApplicationTypeVersion;
        ApplicationName = $FabricNamespace + "/" + $shortAppName;
    }

    Write-Output (New-Object psobject -Property $h)
}

function Get-ImageStoreConnectionString
{
    <#
    .SYNOPSIS 
    Returns the value of the image store connection string from the cluster manifest.

    .PARAMETER ApplicationManifestPath
    Path to the application manifest file.
    #>

    [CmdletBinding()]
    Param
    (
        [xml]
        $ClusterManifest
    )

    $managementSection = $ClusterManifest.ClusterManifest.FabricSettings.Section | ? { $_.Name -eq "Management" }
    return $managementSection.ChildNodes | ? { $_.Name -eq "ImageStoreConnectionString" } | Select-Object -Expand Value
}