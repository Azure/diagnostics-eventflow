@ECHO OFF
for /f %%a in ('dir ~dp0test /b /s project.json ^| find /i "Tests"') do dotnet test %%a