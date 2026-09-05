using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

// Stamps the version onto every iOS build so the phone can tell builds apart.
//
// iOS decides whether an install replaces the app already on the device or lands
// beside it purely by bundle identifier — the home-screen name has nothing to do
// with it. So each *version* gets its own identifier suffix (1.1 becomes
// com.DefaultCompany.tilt-ball.v1-1), which keeps different versions side by side
// while rebuilds of the same version overwrite each other instead of piling up.
//
// The build number rises on every iOS build and goes into the name, so two
// installs of the same version are still distinguishable: "tilt 1.1 (7)".
//
// Player Settings itself is never rewritten with the versioned identifier — only
// the generated Xcode project is — so the suffix can never stack up across builds.
public class IOSBuildStamper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    // iOS truncates the label under a home-screen icon at roughly 12 characters,
    // so the stamped name has to stay short or the version gets cut off — which
    // would defeat the whole point. "tilt-ball 1.1 (7)" shows up as "tilt-ball 1…",
    // hence the shorter base here. Change this string to rename the icon.
    const string DisplayBase = "tilt";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS) return;

        int next = ParseBuildNumber(PlayerSettings.iOS.buildNumber) + 1;
        PlayerSettings.iOS.buildNumber = next.ToString();

        // Persist right away: the counter must survive even if the build later fails,
        // otherwise a failed attempt would hand its number to the next one.
        AssetDatabase.SaveAssets();
        Debug.Log("[IOSBuildStamper] build number -> " + next);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
#if UNITY_IOS
        if (report.summary.platform != BuildTarget.iOS) return;

        string projectRoot = report.summary.outputPath;
        StampDisplayName(projectRoot);
        StampBundleIdentifier(projectRoot);

        Debug.Log("[IOSBuildStamper] '" + DisplayName() + "'  id=" + VersionedBundleIdentifier());
#endif
    }

#if UNITY_IOS
    static void StampDisplayName(string projectRoot)
    {
        string plistPath = Path.Combine(projectRoot, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // CFBundleDisplayName is what the home screen shows; CFBundleName stays as
        // Unity wrote it because Xcode resolves it from the target's PRODUCT_NAME.
        plist.root.SetString("CFBundleDisplayName", DisplayName());
        plist.WriteToFile(plistPath);
    }

    static void StampBundleIdentifier(string projectRoot)
    {
        string pbxPath = PBXProject.GetPBXProjectPath(projectRoot);
        var pbx = new PBXProject();
        pbx.ReadFromFile(pbxPath);

        // Only the app target. UnityFramework and the generated test targets carry
        // their own identifiers and must keep them, or the build stops linking.
        pbx.SetBuildProperty(pbx.GetUnityMainTargetGuid(),
                             "PRODUCT_BUNDLE_IDENTIFIER", VersionedBundleIdentifier());
        pbx.WriteToFile(pbxPath);
    }
#endif

    static string DisplayName()
    {
        return DisplayBase + " " + PlayerSettings.bundleVersion + " (" + PlayerSettings.iOS.buildNumber + ")";
    }

    // "1.1" -> "com.DefaultCompany.tilt-ball.v1-1". Dots become dashes because each
    // dot would otherwise start a new identifier segment. Any suffix an earlier run
    // left behind is stripped first, so re-running never produces ".v1-0.v1-1".
    static string VersionedBundleIdentifier()
    {
        string baseId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
        baseId = Regex.Replace(baseId, @"\.v[0-9][0-9A-Za-z-]*$", "");

        string suffix = Regex.Replace(PlayerSettings.bundleVersion.Trim(), @"[^0-9A-Za-z]+", "-").Trim('-');
        return string.IsNullOrEmpty(suffix) ? baseId : baseId + ".v" + suffix;
    }

    static int ParseBuildNumber(string value)
    {
        int n;
        return int.TryParse(value, out n) ? n : 0;
    }
}
