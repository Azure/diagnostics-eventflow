@ECHO OFF
pushd "%~dp0.."
SET parentDir=%cd%
popd

SET SUFFIX=%1
IF '%SUFFIX%' == '' GOTO NOSUFFIX
for /f %%a in ('findstr /sm packOptions %parentDir%\src\project.json') do dotnet pack %%a --no-build -c Release -o "%parentDir%\nugets" --version-suffix %SUFFIX%
GOTO END

:NOSUFFIX
for /f %%a in ('findstr /sm packOptions %parentDir%\src\project.json') do dotnet pack %%a --no-build -c Release -o "%parentDir%\nugets"
GOTO END

:END