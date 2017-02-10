@ECHO OFF
pushd %~dp0

powershell.exe -File InstallCoreSDK.ps1

ECHO %PATH% | findstr /I /C:"C:\Program Files\dotnet" >NUL 2>NUL
IF %ERRORLEVEL% NEQ 0 SET PATH=%PATH%;C:\Program Files\dotnet\

popd