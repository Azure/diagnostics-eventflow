@ECHO OFF
SET failed=0
for /f %%a in ('dir %~dp0..\test\*.csproj /b /s ^| find /i "Tests.csproj"') do dotnet test %%a || set failed=1
EXIT /B %failed%