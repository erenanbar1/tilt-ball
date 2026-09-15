using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// One-click WebGL build to Builds/WebGL, with every enabled scene from Build
// Settings. Needs the WebGL Build Support module installed for this editor
// version (Unity Hub > Installs > Add modules).
public static class WebGLBuild
{
    const string OutputDir = "Builds/WebGL";

    [MenuItem("Tools/Tilt Ball/Build WebGL")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            EditorUtility.DisplayDialog("Build WebGL", "WebGL Build Support is not installed for this Unity version.\n\nUnity Hub > Installs > " + Application.unityVersion + " > Add modules > WebGL Build Support, then restart the editor.", "OK");
            return;
        }

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuild] No enabled scenes in Build Settings.");
            return;
        }

        Directory.CreateDirectory(OutputDir);
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[WebGLBuild] Succeeded: " + Path.GetFullPath(OutputDir) + " (" + (summary.totalSize / (1024f * 1024f)).ToString("F1") + " MB, " + summary.totalTime.TotalSeconds.ToString("F0") + "s). Serve that folder over HTTP — opening index.html from disk won't work.");
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputDir + "/index.html"));
        }
        else
        {
            Debug.LogError("[WebGLBuild] " + summary.result + " with " + summary.totalErrors + " error(s). See the console above.");
        }
    }
}
