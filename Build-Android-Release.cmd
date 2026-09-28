@echo off
setlocal

title Erudition - Android Release Build

echo ==================================
echo Building Android Release APK
echo ==================================

call "%~dp0Build-Android.cmd"

if errorlevel 1 (
    echo BUILD FAILED
    pause
    exit /b 1
)

echo.
echo Searching APK...

for /f "delims=" %%i in ('dir /b /s "%~dp0Builds\Android\*.apk" ^| sort /r') do (
    set APK=%%i
    goto found
)

:found

if not defined APK (
    echo APK NOT FOUND
    pause
    exit /b 1
)

echo APK:
echo %APK%

set KEYSTORE=%~dp0erudition-release.keystore

if not exist "%KEYSTORE%" (
    echo ERROR: keystore not found
    pause
    exit /b 1
)


echo.
echo Signing APK...


set APKSIGNER=D:\uni\uniti\6000.3.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\36.0.0\apksigner.bat


"%APKSIGNER%" sign ^
--ks "%KEYSTORE%" ^
--ks-key-alias erudition ^
"%APK%"


echo.
echo Checking signature...

"%APKSIGNER%" verify --verbose --print-certs "%APK%"


echo.
echo RELEASE BUILD COMPLETE

pause