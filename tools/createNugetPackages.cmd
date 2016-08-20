@ECHO OFF
SET SUFFIX=%1
IF '%SUFFIX%' == '' GOTO NOSUFFIX
for /f %%a in ('dir %~dp0..\src\project.json /b /s ^| find /v "Consumers"') do dotnet pack %%a --no-build -c Release -o "%~dp0..\nugets" --version-suffix %SUFFIX%
GOTO END

:NOSUFFIX
ECHO Please provide verison suffix.
ECHO.For example:
ECHO.createNugetPackages 20160818
GOTO END

:END