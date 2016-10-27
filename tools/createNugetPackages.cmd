@ECHO OFF
pushd "%~dp0.."
SET parentDir=%cd%
popd

SET CONFIGURATION=%2
IF '%CONFIGURATION%' == '' SET CONFIGURATION=Release

SET SUFFIX=%1
IF '%SUFFIX%' == '' GOTO NOSUFFIX

for /f %%a in ('findstr /sm packOptions %parentDir%\src\project.json') do dotnet pack %%a --no-build -c %CONFIGURATION% -o "%parentDir%\nugets" --version-suffix %SUFFIX%
"%~dp0\nuget.exe" pack "%parentDir%\nuspecs\Suite\Microsoft.Diagnostics.EventFlow.Suite.nuspec" -OutputDirectory "%parentDir%\nugets" -Suffix %SUFFIX%
GOTO END

:NOSUFFIX
for /f %%a in ('findstr /sm packOptions %parentDir%\src\project.json') do dotnet pack %%a --no-build -c %CONFIGURATION% -o "%parentDir%\nugets"
"%~dp0\nuget.exe" pack "%parentDir%\nuspecs\Suite\Microsoft.Diagnostics.EventFlow.Suite.nuspec" -OutputDirectory "%parentDir%\nugets"
GOTO END

:END