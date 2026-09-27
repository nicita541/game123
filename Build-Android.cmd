@echo off
setlocal
title Erudition - Android APK build
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build-Android.ps1" %*
set "BUILD_EXIT_CODE=%ERRORLEVEL%"
echo.
if not "%BUILD_EXIT_CODE%"=="0" echo BUILD FAILED. See the message above.
pause
exit /b %BUILD_EXIT_CODE%
