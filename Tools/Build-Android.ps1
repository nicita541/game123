[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$GradleCachePath,
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logPath = $null
$previousGradleHome = $env:GRADLE_USER_HOME

function Find-UnityEditor([string]$RequiredVersion) {
    if ($UnityPath) { return $UnityPath }
    if ($env:UNITY_EDITOR_PATH) { return $env:UNITY_EDITOR_PATH }

    $candidates = @()
    $hubRoot = Join-Path $env:APPDATA 'UnityHub'
    $secondaryPathFile = Join-Path $hubRoot 'secondaryInstallPath.json'
    if (Test-Path -LiteralPath $secondaryPathFile) {
        try {
            $installRoot = Get-Content -LiteralPath $secondaryPathFile -Raw | ConvertFrom-Json
            if ($installRoot -is [string]) {
                $candidates += Join-Path $installRoot "$RequiredVersion\Editor\Unity.exe"
            }
        } catch { Write-Verbose "Cannot read Unity Hub install path: $_" }
    }
    $editorsFile = Join-Path $hubRoot 'editors-v2.json'
    if (Test-Path -LiteralPath $editorsFile) {
        try {
            $editors = Get-Content -LiteralPath $editorsFile -Raw | ConvertFrom-Json
            foreach ($editor in $editors.data) {
                if ($editor.version -eq $RequiredVersion -and $editor.location) {
                    $candidates += $editor.location
                    $candidates += Join-Path $editor.location 'Editor\Unity.exe'
                }
            }
        } catch { Write-Verbose "Cannot read Unity Hub editors: $_" }
    }
    $candidates += Join-Path $env:ProgramFiles "Unity\Hub\Editor\$RequiredVersion\Editor\Unity.exe"
    $candidates += Join-Path $env:ProgramFiles 'Unity\Editor\Unity.exe'
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $productVersion = (Get-Item -LiteralPath $candidate).VersionInfo.ProductVersion
            if ($productVersion -like "$RequiredVersion`_*") { return $candidate }
        }
    }
    throw "Unity $RequiredVersion was not found. Install it in Unity Hub or pass -UnityPath 'D:\path\Editor\Unity.exe'."
}

try {
    $versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
    $versionText = Get-Content -LiteralPath $versionFile -Raw
    if ($versionText -notmatch '(?m)^m_EditorVersion:\s*(\S+)') {
        throw 'Cannot read the Unity version from ProjectVersion.txt.'
    }
    $requiredVersion = $Matches[1]
    $UnityPath = Find-UnityEditor $requiredVersion
    if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
        throw "Unity.exe does not exist: $UnityPath"
    }
    $UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
    $installedVersion = (Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion
    if ($installedVersion -notlike "$requiredVersion`_*") {
        throw "This project needs Unity $requiredVersion, but the selected editor is $installedVersion."
    }

    $androidRoot = Join-Path (Split-Path -Parent $UnityPath) 'Data\PlaybackEngines\AndroidPlayer'
    foreach ($component in @('UnityEditor.Android.Extensions.dll', 'SDK\platform-tools\adb.exe', 'NDK\source.properties', 'OpenJDK\bin\java.exe')) {
        if (-not (Test-Path -LiteralPath (Join-Path $androidRoot $component))) {
            throw "Android component is missing: $component. In Unity Hub, add Android Build Support, Android SDK & NDK Tools, and OpenJDK for Unity $requiredVersion."
        }
    }

    # Unity owns this lock while the project is open. Never remove its lock file.
    $lockPath = Join-Path $projectRoot 'Temp\UnityLockfile'
    if (Test-Path -LiteralPath $lockPath) {
        try {
            $lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $lockStream.Dispose()
        } catch {
            throw 'Save your scenes and close this project in Unity, then run Build-Android.cmd again.'
        }
    }

    $settingsText = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectSettings.asset') -Raw
    if ($settingsText -notmatch '(?m)^\s*activeInputHandler:\s*0\s*$') {
        throw 'Set Player Settings > Active Input Handling to Input Manager (Old), restart Unity, save and close the project, then retry. This project uses StandaloneInputModule.'
    }
    if ($settingsText -notmatch '(?m)^\s*bundleVersion:\s*(.+)$') {
        throw 'Cannot read Version from Player Settings.'
    }
    $currentVersion = $Matches[1].Trim().Trim('"').Trim("'")
    $versionMatch = [regex]::Match($currentVersion, '\A([0-9]+\.[0-9]+\.)([0-9]+)\z')
    $patchNumber = 0
    if (-not $versionMatch.Success -or
        -not [int]::TryParse($versionMatch.Groups[2].Value, [ref]$patchNumber) -or
        $patchNumber -eq [int]::MaxValue) {
        throw 'Set Version in Player Settings to three numbers, for example 0.1.2. The last number must be smaller than 2147483647.'
    }
    $bundleVersion = $versionMatch.Groups[1].Value + ($patchNumber + 1).ToString([Globalization.CultureInfo]::InvariantCulture)
    $currentVersionCode = 0
    if ($settingsText -notmatch '(?m)^\s*AndroidBundleVersionCode:\s*([0-9]+)\s*$' -or
        -not [int]::TryParse($Matches[1], [ref]$currentVersionCode) -or
        $currentVersionCode -ge 2100000000) {
        throw 'Bundle Version Code in Player Settings must be a non-negative number smaller than 2100000000.'
    }
    $nextVersionCode = $currentVersionCode + 1

    $outputDirectory = Join-Path $projectRoot 'Builds\Android'
    if (-not $GradleCachePath) {
        # Reuse the dependency cache from previous builds on this computer.
        $legacyCache = Join-Path $projectRoot '_feedback_validation\GradleCache'
        if (Test-Path -LiteralPath $legacyCache -PathType Container) {
            $GradleCachePath = $legacyCache
        } else {
            $GradleCachePath = Join-Path $outputDirectory 'GradleCache'
        }
    }
    $GradleCachePath = [IO.Path]::GetFullPath($GradleCachePath)
    if ($GradleCachePath -match '[^\x00-\x7F]') {
        throw "Gradle cache needs a path without Cyrillic characters. Pass -GradleCachePath 'F:\UnityGradleCache'. Current path: $GradleCachePath"
    }

    Write-Host "Project: $projectRoot"
    Write-Host "Unity:   $UnityPath"
    Write-Host "Gradle:  $GradleCachePath"
    Write-Host 'Target:  Android APK / ARM64 / IL2CPP / test signature'
    Write-Host "Version: $currentVersion -> $bundleVersion"
    Write-Host "Code:    $currentVersionCode -> $nextVersionCode"
    if ($CheckOnly) {
        Write-Host 'CHECK OK. Preview only; Unity was not started and the version was not changed.' -ForegroundColor Green
        exit 0
    }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $buildName = "Erudition-$bundleVersion-$stamp-arm64"
    $apkPath = Join-Path $outputDirectory "$buildName.apk"
    $logDirectory = Join-Path $outputDirectory 'Logs'
    New-Item -ItemType Directory -Force -Path $logDirectory, $GradleCachePath | Out-Null
    $logPath = Join-Path $logDirectory "$buildName.log"
    $env:GRADLE_USER_HOME = $GradleCachePath

    # Start-Process joins ArgumentList on Windows: quote path arguments explicitly.
    $unityArguments = @(
        '-batchmode', '-nographics',
        '-projectPath', ('"{0}"' -f $projectRoot),
        '-buildTarget', 'Android',
        '-executeMethod', 'EruditionAndroidBuild.Run',
        '-apkOutput', ('"{0}"' -f $apkPath),
        '-apkVersion', $bundleVersion,
        '-apkVersionCode', $nextVersionCode.ToString([Globalization.CultureInfo]::InvariantCulture),
        '-logFile', ('"{0}"' -f $logPath)
    )
    Write-Host "APK:     $apkPath"
    Write-Host "Log:     $logPath"
    Write-Host 'Building. The first build can take longer while Unity imports assets and downloads dependencies.'
    $process = Start-Process -FilePath $UnityPath -ArgumentList $unityArguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while (-not $process.WaitForExit(30000)) {
        Write-Host ("Unity is building... {0:mm\:ss} elapsed. Details are in the log." -f $timer.Elapsed)
    }
    $process.WaitForExit()
    $process.Refresh()
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode)." }
    if (-not (Test-Path -LiteralPath $apkPath -PathType Leaf) -or (Get-Item -LiteralPath $apkPath).Length -eq 0) {
        throw 'Unity did not create an APK.'
    }
    if (-not (Select-String -LiteralPath $logPath -SimpleMatch 'ANDROID_APK_BUILD_SUCCEEDED' -Quiet)) {
        throw 'Unity did not report a successful build. Check the log.'
    }
    $hash = (Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash
    "$hash  $([IO.Path]::GetFileName($apkPath))" | Set-Content -LiteralPath "$apkPath.sha256" -Encoding ASCII
    Write-Host ''
    Write-Host 'BUILD SUCCEEDED' -ForegroundColor Green
    Write-Host "Saved in Player Settings: Version $bundleVersion / Bundle Version Code $nextVersionCode"
    Write-Host "APK: $apkPath"
    Write-Host ("Size: {0:N1} MB" -f ((Get-Item -LiteralPath $apkPath).Length / 1MB))
    Write-Host "Log: $logPath"
    exit 0
} catch {
    Write-Host "BUILD FAILED: $($_.Exception.Message)" -ForegroundColor Red
    if ($logPath -and (Test-Path -LiteralPath $logPath)) {
        Write-Host "Log: $logPath"
        Write-Host 'Last log lines:'
        Get-Content -LiteralPath $logPath -Tail 35 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
    }
    exit 1
} finally {
    $env:GRADLE_USER_HOME = $previousGradleHome
}
