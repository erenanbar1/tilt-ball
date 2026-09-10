using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Makes pressing Play show the level you are looking at.
//
// A level is a prefab, and a prefab cannot be played. Double-clicking one opens
// it for editing, and pressing Play from there closes the stage and runs whatever
// scene happened to be loaded behind it — usually Bootstrap, whose only job is to
// hand straight off to the main menu. So the level being edited was the one thing
// Play would not show.
//
// Opening a level therefore arms it: its path is remembered, and Unity is pointed
// at the gameplay scene as the place Play starts. LevelController reads the armed
// path when no menu has chosen a level, so Play lands in the level under the
// cursor no matter which scene is open. Tools > Tilt Ball > Clear Level Preview
// puts the normal Bootstrap flow back.
[InitializeOnLoad]
public static class LevelPlayTest
{
    // Play always starts here, never in a gameplay scene. Bootstrap owns
    // GameManager, SceneLoader, AudioManager and SaveManager, and every other
    // scene is loaded additively on top of it — so entering play anywhere else
    // leaves the game with no pausing, no win/lose, no sound and no saves.
    // BootstrapRunner sees the armed level and heads for it instead of the menu.
    const string BootScene = "Assets/Scenes/end-to-end/Bootstrap.unity";

    static LevelPlayTest()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
    }

    static void OnPrefabStageOpened(PrefabStage stage)
    {
        if (stage == null || stage.prefabContentsRoot == null) return;
        if (stage.prefabContentsRoot.GetComponent<LevelBoard>() == null) return;

        // Deliberately not disarmed when the stage closes: closing is exactly what
        // Unity does on its way into Play mode, so tearing this down there would
        // undo the arming a fraction of a second before it is needed.
        Arm(stage.assetPath);
    }

    public static string ArmedLevel => SessionState.GetString(LevelController.PreviewLevelKey, string.Empty);

    public static void Arm(string path)
    {
        if (ArmedLevel == path) return;

        SessionState.SetString(LevelController.PreviewLevelKey, path);
        // Without this, Play runs whichever scene is open — a gameplay scene on
        // its own more often than not, with none of Bootstrap's systems loaded.
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScene);

        Debug.Log("Bölüm önizlemesi: <b>" + System.IO.Path.GetFileNameWithoutExtension(path) +
                  "</b> — Play'e bastığında bu bölüm açılacak. Normal akışa dönmek için " +
                  "Tools ▸ Tilt Ball ▸ Clear Level Preview.");
    }

    [MenuItem("Tools/Tilt Ball/Clear Level Preview", priority = 20)]
    public static void Disarm()
    {
        SessionState.EraseString(LevelController.PreviewLevelKey);
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("Bölüm önizlemesi kapatıldı — Play artık normal akışı (Bootstrap ▸ Main Menu) izler.");
    }

    [MenuItem("Tools/Tilt Ball/Clear Level Preview", validate = true)]
    static bool DisarmValidate() => !string.IsNullOrEmpty(ArmedLevel);

    [MenuItem("Tools/Tilt Ball/Play Selected Level %#p", priority = 0)]
    public static void PlaySelected()
    {
        var prefab = Selection.activeObject as GameObject;
        string path = prefab != null ? AssetDatabase.GetAssetPath(prefab) : null;

        if (string.IsNullOrEmpty(path) || prefab.GetComponent<LevelBoard>() == null)
        {
            EditorUtility.DisplayDialog(
                "Bölüm seçili değil",
                "Project penceresinden bir level prefabı seç (LevelBoard taşıyan bir prefab), sonra tekrar dene.\n\n" +
                "Bölümü düzenlerken Inspector'daki ▶ düğmesini de kullanabilirsin.",
                "Tamam");
            return;
        }

        Play(path);
    }

    [MenuItem("Tools/Tilt Ball/Play Selected Level %#p", validate = true)]
    static bool PlaySelectedValidate()
    {
        var go = Selection.activeObject as GameObject;
        return go != null && go.GetComponent<LevelBoard>() != null && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    // path is the level prefab's asset path.
    public static void Play(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;

        // An unsaved edit to the level is almost certainly the reason it is being
        // played, so it goes in rather than being quietly left behind.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Arm(path);
        EditorApplication.isPlaying = true;
    }
}
