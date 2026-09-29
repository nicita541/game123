@echo off
setlocal

title Erudition - Android Release Build

echo ==================================
echo   BUILDING ANDROID RELEASE APK
echo ==================================

echo.

call "%~dp0Build-Android.cmd"

if errorlevel 1 (
    echo BUILD FAILED
    pause
    exit /b 1
)


echo.
echo ==================================
echo Searching APK...
echo ==================================

set APK=

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

echo Found APK:
echo %APK%


set ZIPALIGN=D:\uni\uniti\6000.3.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\36.0.0\zipalign.exe

set APKSIGNER=D:\uni\uniti\6000.3.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\36.0.0\apksigner.bat


set KEYSTORE=%~dp0erudition-release.keystore


if not exist "%KEYSTORE%" (
    echo ERROR: Keystore not found
    pause
    exit /b 1
)


echo.
echo ==================================
echo ZIPALIGN
echo ==================================

set ALIGNED=%APK:.apk=-aligned.apk%

"%ZIPALIGN%" -f -p 4 "%APK%" "%ALIGNED%"

if errorlevel 1 (
    echo ZIPALIGN FAILED
    pause
    exit /b 1
)


echo.
echo ==================================
echo KEYSTORE PASSWORD
echo ==================================

set /p STOREPASS=Enter keystore password:


echo.
echo ==================================
echo SIGNING APK
echo ==================================

"%APKSIGNER%" sign ^
--ks "%KEYSTORE%" ^
--ks-key-alias erudition ^
--ks-pass pass:%STOREPASS% ^
--key-pass pass:%STOREPASS% ^
"%ALIGNED%"


if errorlevel 1 (
    echo SIGN FAILED
    pause
    exit /b 1
)


echo.
echo ==================================
echo VERIFY SIGNATURE
echo ==================================

"%APKSIGNER%" verify --verbose --print-certs "%ALIGNED%"


echo.
echo ==================================
echo RELEASE BUILD COMPLETE
echo ==================================

echo Final APK:
echo %ALIGNED%

pause