#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;

// The official lite SDK ships its AAR but expects these Maven dependencies.
// Keep the generated Gradle project reproducible without an editor resolver.
public sealed class YandexAndroidDependencies : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 900;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var file = Path.Combine(path, "build.gradle");
        var text = File.ReadAllText(file);
        if (!text.Contains("com.yandex.android:mobileads:8.2.0"))
            File.AppendAllText(file, "\n// Yandex Mobile Ads Unity SDK 8.2.0\ndependencies {\n    implementation 'com.yandex.android:mobileads:8.2.0'\n    implementation 'androidx.lifecycle:lifecycle-process:2.4.1'\n    implementation 'io.appmetrica.analytics:analytics:8.0.0'\n}\n");
    }
}
#endif
