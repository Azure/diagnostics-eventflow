$ErrorActionPreference = "Stop"
$installerName = "installer.exe"

Write-Host "Downloading .Net Core SDK"
Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/?LinkID=809122" -OutFile $installerName

Write-Host "Installing .Net Core SDK"
Start-Process $installerName -ArgumentList "/quiet" -Wait

Write-Host ".Net Core installation finished"
Remove-Item $installerName