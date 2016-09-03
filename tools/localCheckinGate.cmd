@ECHO OFF
git status
ECHO.=========================================== WARNING ===========================================
ECHO.This will call git clean, all untracked change will be lost. Use Ctrl+C to break.
ECHO.=========================================== WARNING ===========================================
PAUSE
call %~dp0scorch.cmd
call %~dp0restore.cmd
call %~dp0buildRelease.cmd
call %~dp0runtest.cmd
