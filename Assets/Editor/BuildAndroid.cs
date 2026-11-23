using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 命令行构建 Android APK：Unity -executeMethod BuildAndroid.Build
/// </summary>
public static class BuildAndroid
{
    public static void Build()
    {
        Directory.CreateDirectory("Build");

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Mindscape.unity" },
            locationPathName = "Build/Mindscape.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("BUILD_FAILED: " + report.summary.result);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("BUILD_OK: " + report.summary.outputPath);
    }
}
