Set-Location HKLM:\SYSTEM\CurrentControlSet\Services\.NETFramework\Performance
Set-ItemProperty . -Name ProcessNameFormat -Value 1 -Type DWord
