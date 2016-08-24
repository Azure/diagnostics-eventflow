@ECHO OFF
SET SUFFIX=%1
IF '%SUFFIX%' == '' GOTO NOSUFFIX
for /f %%a in ('findstr /sm packOptions project.json %~dp0..\src') do dotnet pack %%a --no-build -c Release -o "%~dp0..\nugets" --version-suffix %SUFFIX%
GOTO END

:NOSUFFIX
for /f %%a in ('findstr /sm packOptions project.json %~dp0..\src') do dotnet pack %%a --no-build -c Release -o "%~dp0..\nugets"
GOTO END

:END