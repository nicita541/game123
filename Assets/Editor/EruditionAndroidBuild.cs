using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Entry point used by Build-Android.cmd. This is editor-only code.
public static class EruditionAndroidBuild
{
    public static void Run()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run Build-Android.cmd with the Unity project closed.");

        var exitCode = 1;
        try
        {
            Build();
            exitCode = 0;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        EditorApplication.Exit(exitCode);
    }

    private static void Build()
    {
        var output = Argument("-apkOutput");
        if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output) ||
            !string.Equals(Path.GetExtension(output), ".apk", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Pass -apkOutput with an absolute .apk path.");
        if (File.Exists(output))
            throw new IOException("Refusing to overwrite an existing APK: " + output);
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Start Unity with -buildTarget Android.");

        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene)))
            throw new InvalidOperationException("Build Settings must contain enabled scenes that exist on disk.");

        var previousVersion = PlayerSettings.bundleVersion;
        var previousVersionCode = PlayerSettings.Android.bundleVersionCode;
        var nextVersion = NextPatchVersion(previousVersion);
        if (previousVersionCode < 0 || previousVersionCode >= 2100000000)
            throw new InvalidOperationException("Bundle Version Code must be between 0 and 2099999999.");
        var nextVersionCode = previousVersionCode + 1;
        if (Argument("-apkVersion") != nextVersion ||
            Argument("-apkVersionCode") != nextVersionCode.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("Version settings changed after the build was started. Run Build-Android.cmd again.");

        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var target = NamedBuildTarget.Android;
        var backend = PlayerSettings.GetScriptingBackend(target);
        var stripping = PlayerSettings.GetManagedStrippingLevel(target);
        var architectures = PlayerSettings.Android.targetArchitectures;
        var customKeystore = PlayerSettings.Android.useCustomKeystore;
        var splitBinary = PlayerSettings.Android.splitApplicationBinary;
        var splitArchitectures = PlayerSettings.Android.buildApkPerCpuArchitecture;
        var appBundle = EditorUserBuildSettings.buildAppBundle;
        var exportProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        var development = EditorUserBuildSettings.development;
        var buildSucceeded = false;
        try
        {
            // The APK receives the new version. Failed builds restore the old one below.
            PlayerSettings.bundleVersion = nextVersion;
            PlayerSettings.Android.bundleVersionCode = nextVersionCode;
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.splitApplicationBinary = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;

            Debug.Log("ANDROID_APK_BUILD_BEGIN output=" + output + " version=" + nextVersion +
                " versionCode=" + nextVersionCode + " scenes=" + string.Join(",", scenes));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4HC
            });
            Debug.Log("ANDROID_APK_BUILD_RESULT result=" + report.summary.result +
                " bytes=" + report.summary.totalSize + " seconds=" + report.summary.totalTime.TotalSeconds +
                " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
            if (report.summary.result != BuildResult.Succeeded || !File.Exists(output) || new FileInfo(output).Length == 0)
                throw new InvalidOperationException("Android APK build failed. See the Unity log for details.");
            buildSucceeded = true;
        }
        finally
        {
            if (!buildSucceeded)
            {
                PlayerSettings.bundleVersion = previousVersion;
                PlayerSettings.Android.bundleVersionCode = previousVersionCode;
            }
            PlayerSettings.SetScriptingBackend(target, backend);
            PlayerSettings.SetManagedStrippingLevel(target, stripping);
            PlayerSettings.Android.targetArchitectures = architectures;
            PlayerSettings.Android.useCustomKeystore = customKeystore;
            PlayerSettings.Android.splitApplicationBinary = splitBinary;
            PlayerSettings.Android.buildApkPerCpuArchitecture = splitArchitectures;
            EditorUserBuildSettings.buildAppBundle = appBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = exportProject;
            EditorUserBuildSettings.development = development;
            AssetDatabase.SaveAssets();
        }
        Debug.Log("ANDROID_APK_BUILD_SUCCEEDED output=" + output);
    }

    private static string NextPatchVersion(string version)
    {
        var match = Regex.Match(version, @"\A([0-9]+\.[0-9]+\.)([0-9]+)\z");
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var patch) || patch == int.MaxValue)
            throw new InvalidOperationException("Version must contain three numbers, for example 0.1.2, with a last number smaller than 2147483647.");
        return match.Groups[1].Value + (patch + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static string Argument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
