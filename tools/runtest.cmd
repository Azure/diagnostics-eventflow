@ECHO OFF
for /f %%a in ('dir %~dp0..\test\project.json /b /s ^| find /i "Tests"') do dotnet test %%a