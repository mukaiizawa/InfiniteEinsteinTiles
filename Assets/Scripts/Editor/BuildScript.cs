using System.Linq;

using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Entry point for the batchmode build pipeline (build_iet1.bat).
public static class BuildScript
{
    const string ProjectRoot = @"C:\InfiniteEinsteinTiles";
    const string BuildRoot = ProjectRoot + @"\build";
    const string ExeName = "Infinite Einstein Tiles.exe";
    const string WinOutPath = BuildRoot + @"\windows\" + ExeName;
    const string MacOutPath = BuildRoot + @"\macOS.app";

    public static void BuildAllProduct()
    {
        BuildAll();
        EditorApplication.Exit(0);
    }

    static void BuildAll()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        BuildAddressables();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        Build(BuildTarget.StandaloneWindows64, WinOutPath, scenes);
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
        Build(BuildTarget.StandaloneOSX, MacOutPath, scenes);
    }

    static void BuildAddressables()
    {
        Debug.Log("[BuildScript] Building Addressables...");
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"[BuildScript] Addressables FAILED: {result.Error}");
            EditorApplication.Exit(1);
        }
        Debug.Log("[BuildScript] Addressables done.");
    }

    static void Build(BuildTarget target, string outputPath, string[] scenes)
    {
        Debug.Log($"[BuildScript] Building {target} -> {outputPath}");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
        });
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildScript] FAILED: {target} - {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
        Debug.Log($"[BuildScript] Done: {target}");
    }
}
