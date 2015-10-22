Switch-AzureMode AzureResourceManager

Select-AzureSubscription 'BinDu Microsoft Subscription'

$location = 'West US'
$vmName = 'binduvmext2'
$resourceGroupName = 'binduvmext2'
$extensionName = "InstallNetFx46"
$extensionType = 'CustomScriptExtension' 
$publisher = 'Microsoft.Compute'
$version = '1.4'

$settings = @"
{
  "fileUris": [
    "https://raw.githubusercontent.com/northtyphoon/ServiceProfiler/master/InstallNetFx46.ps1"
  ],
  "commandToExecute": "powershell.exe -ExecutionPolicy Unrestricted -File InstallNetFx46.ps1"
}
"@

Set-AzureVMExtension `
    -ResourceGroupName $resourceGroupName `
    -VMName $vmName `
    -Name $extensionName `
    -ExtensionType $extensionType `
    -Publisher $publisher `
    -TypeHandlerVersion $version `
    -SettingString $settings `
    -Location $location