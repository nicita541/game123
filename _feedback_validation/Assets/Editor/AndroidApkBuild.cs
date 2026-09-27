using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Copy into Assets/Editor in an isolated build workspace, then run with
// Unity -batchmode -buildTarget Android -executeMethod AndroidApkBuild.Run.
public static class AndroidApkBuild
{
    public static void Run()
    {
        try
        {
            var output = Argument("-apkOutput");
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -apkOutput absolute path");
            if (!Path.IsPathRooted(output)) throw new ArgumentException("APK output must be absolute");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var target = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.splitApplicationBinary = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length != 2 || !scenes[0].EndsWith("MainScene.unity") || !scenes[1].EndsWith("GameplayScene.unity"))
                throw new InvalidOperationException("Expected MainScene and GameplayScene in the build");
            Debug.Log("ANDROID_APK_BUILD_BEGIN output=" + output + " scenes=" + string.Join(",", scenes));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4HC
            });
            Debug.Log("ANDROID_APK_BUILD_RESULT result=" + report.summary.result + " bytes=" + report.summary.totalSize
                + " seconds=" + report.summary.totalTime.TotalSeconds + " errors=" + report.summary.totalErrors
                + " warnings=" + report.summary.totalWarnings);
            if (report.summary.result != BuildResult.Succeeded || !File.Exists(output))
                throw new InvalidOperationException("Android APK build did not succeed");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static string Argument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
