@echo off
setlocal
title Archeo - Deploy

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy.ps1" %*
set "taskExitCode=%ERRORLEVEL%"

echo.
if "%taskExitCode%"=="0" (
    echo Deployment completed successfully.
) else (
    echo Deployment failed. Exit code: %taskExitCode%
)
echo.
pause
exit /b %taskExitCode%
