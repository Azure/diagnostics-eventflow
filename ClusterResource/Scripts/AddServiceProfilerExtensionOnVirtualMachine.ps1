Switch-AzureMode AzureResourceManager

Select-AzureSubscription 'BinDu Microsoft Subscription'

$location = 'West US'
$vmName = 'binduvmext2'
$resourceGroupName = 'binduvmext2'
$extensionName = "ServiceProfilerAgent"
$extensionType = 'ServiceProfilerAgent' 
$publisher = 'Microsoft.VisualStudio.ServiceProfiler'
$version = '0.1'

$protectedsettings = @"
{
  "storageAccountEndPoint": "https://core.windows.net/",
  "storageAccountKey": "Wn53dWj+kg7laY8Um5+Y9JffnRrbYb/O8A7BKjxmA7rS36uGLRGeWeRE7HBeCpzSIS/xeh1rnD8rA6MHCTE6PA==",
  "storageAccountName": "bindutagtest"
}
"@

$settings = @"
{
  "config": {
    "ServiceName": "bindutagtest",
    "CircularEtlBufferMB": 200,
    "SamplingRate": 0.5,
    "AgentLogFilter": "Warning",
    "CollectMemorySummary": true,
    "CollectGCDump": true,
    "MemoryDumpPriority": "Normal",
    "EtwMetrics": [
      {
        "ProviderName": "Microsoft-Windows-ASPNET",
        "ProviderKeywords": 72057594037927940,
        "ProviderLevel": "Informational",
        "Event": "Request/Start",
        "EventStop": "Request/Stop",
        "Name": "FullUrl"
      }
    ],
    "Tags": [
      {
        "Type": "Performance",
        "Settings": {
          "SampleIntervalInSeconds": "5",
          "SamplesToConsider": "6",
          "Triggers": [
            {
              "Name": "High CPU",
              "Description": "High CPU usage",
              "PerfCounter": "Processor Information\\% Processor Time\\_Total",
              "Operator": ">",
              "Metric": "50"
            },
            {
              "Name": "Busy Disk",
              "Description": "High disk usage",
              "PerfCounter": "PhysicalDisk\\% Disk Time\\_Total",
              "Operator": ">",
              "Metric": "10"
            },
            {
              "Name": "Memory Pressure",
              "Description": "High memory usage",
              "PerfCounter": "Memory\\Available MBytes",
              "Operator": "<",
              "Metric": "4000"
            },
            {
              "Name": "High GC",
              "Description": "High GC time",
              "PerfCounter": ".NET CLR Memory\\% Time in GC\\_Global_",
              "Operator": ">",
              "Metric": "10"
            }
          ]
        }
      }
    ]
  }
}
"@

Set-AzureVMExtension `
    -ResourceGroupName $resourceGroupName `
    -VMName $vmName `
    -Name $extensionName `
    -ExtensionType $extensionType `
    -Publisher $publisher `
    -TypeHandlerVersion $version `
    -ProtectedSettingString $protectedsettings `
    -SettingString $settings `
    -Location $location
