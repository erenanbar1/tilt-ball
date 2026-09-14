using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The level design workflow, end to end:
//
//   1. Tools > Tilt Ball > New Level Design Scene — copies Gameplay into
//      Assets/Levels/Designs/, puts a LevelDesignPreview on its Level root and
//      drops a WinningHole under Level/LayoutRoot to start from.
//   2. In that scene: set Level Length on the preview, move the hole and place
//      obstacle prefabs under Level/LayoutRoot against the previewed geometry,
//      press Play to try it.
//   3. Tools > Tilt Ball > Export Level Assets — saves LayoutRoot as
//      Assets/Levels/ObstaclePrefabs/<name>_Obstacles.prefab and writes
//      Assets/Levels/Configs/<name>.asset with the matching levelLength.
//   4. Add the config to GameManager's allLevels / tallLevels in Bootstrap.
//
// Design scenes never ship: they're not in Build Settings and SceneLoader only
// ever loads the scene named Gameplay.
public static class LevelDesignTools
{
    const string GameplayScenePath = "Assets/Scenes/end-to-end/Gameplay.unity";
    const string DesignsFolder = "Assets/Levels/Designs";
    const string ObstaclePrefabsFolder = "Assets/Levels/ObstaclePrefabs";
    const string ConfigsFolder = "Assets/Levels/Configs";
    const string WinningHolePrefabPath = "Assets/Prefabs/WinningHole.prefab";
    // Where the hole sits in a one-screenful level as the game has always
    // authored it — a starting point, not a rule; the designer moves it.
    const float DefaultHoleY = 4f;

    [MenuItem("Tools/Tilt Ball/New Level Design Scene")]
    static void NewLevelDesignScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(DesignsFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath(DesignsFolder + "/LevelDesign.unity");
        if (!AssetDatabase.CopyAsset(GameplayScenePath, path))
        {
            Debug.LogError("Could not copy " + GameplayScenePath + " to " + path);
            return;
        }

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<LevelController>();
        if (controller == null)
        {
            Debug.LogError("The copied scene has no LevelController on its Level root.");
            return;
        }

        var preview = controller.gameObject.AddComponent<LevelDesignPreview>();
        preview.levelName = Path.GetFileNameWithoutExtension(path);

        // A level can't be won without a hole, and the shipping scene doesn't
        // carry one, so every design starts with one already in the layout.
        var holePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WinningHolePrefabPath);
        if (holePrefab != null && controller.layoutRoot != null)
        {
            var hole = (GameObject)PrefabUtility.InstantiatePrefab(holePrefab, controller.layoutRoot);
            hole.transform.position = new Vector3(0f, DefaultHoleY, 0f);
        }
        else
        {
            Debug.LogWarning("Could not add a starting WinningHole — drag " + WinningHolePrefabPath + " under Level/LayoutRoot yourself.");
        }
        EditorSceneManager.SaveScene(scene);

        Selection.activeObject = controller.gameObject;
        Debug.Log("Level design scene ready: " + path + "\nSet Level Length on the LevelDesignPreview, move the hole and place obstacles under Level/LayoutRoot, then Tools > Tilt Ball > Export Level Assets.");
    }

    [MenuItem("Tools/Tilt Ball/Export Level Assets")]
    static void ExportLevelAssets()
    {
        var preview = Object.FindFirstObjectByType<LevelDesignPreview>();
        if (preview == null)
        {
            EditorUtility.DisplayDialog("Export Level Assets", "The open scene has no LevelDesignPreview. Open a level design scene first (Tools > Tilt Ball > New Level Design Scene).", "OK");
            return;
        }

        string name = (preview.levelName ?? "").Trim();
        if (string.IsNullOrEmpty(name) || name == "Level_XX" || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            EditorUtility.DisplayDialog("Export Level Assets", "Give the LevelDesignPreview a real Level Name first (e.g. Level_19 or Tall_03).", "OK");
            return;
        }

        var controller = preview.GetComponent<LevelController>();
        var root = controller.layoutRoot;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Export Level Assets", "LevelController has no LayoutRoot assigned.", "OK");
            return;
        }
        if (root.childCount == 0 && !EditorUtility.DisplayDialog("Export Level Assets", "LayoutRoot is empty — export a level with nothing in it?", "Export", "Cancel"))
        {
            return;
        }
        bool hasHole = root.GetComponentInChildren<WinTrigger>(true) != null;
        if (!hasHole && !EditorUtility.DisplayDialog("Export Level Assets", "There is no WinningHole under LayoutRoot — this level can't be won. Export anyway?", "Export", "Cancel"))
        {
            return;
        }

        EnsureFolder(ObstaclePrefabsFolder);
        EnsureFolder(ConfigsFolder);
        string prefabPath = ObstaclePrefabsFolder + "/" + name + "_Obstacles.prefab";
        string configPath = ConfigsFolder + "/" + name + ".asset";

        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        bool configExists = AssetDatabase.LoadAssetAtPath<LevelConfig>(configPath) != null;
        if ((prefabExists || configExists) && !EditorUtility.DisplayDialog("Export Level Assets",
                "Overwrite existing " + (prefabExists ? prefabPath : "") + (prefabExists && configExists ? " and " : "") + (configExists ? configPath : "") + "?",
                "Overwrite", "Cancel"))
        {
            return;
        }

        // Saved straight from the scene's LayoutRoot so the obstacles inside
        // stay nested prefab instances (a clone would flatten them). If the root
        // is itself a prefab instance — the designer dragged a previous export
        // back into the Project window, say — only that outermost link is
        // unpacked first, or SaveAsPrefabAsset would write a *variant* of the
        // old prefab instead of a standalone one. Not connected afterwards:
        // LayoutRoot stays the plain container it is in Gameplay. Named
        // after the level, like every existing obstacle prefab.
        if (PrefabUtility.IsAnyPrefabInstanceRoot(root.gameObject))
        {
            PrefabUtility.UnpackPrefabInstance(root.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
        }
        string sceneName = root.name;
        root.name = name + "_Obstacles";
        GameObject prefab;
        try
        {
            prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, prefabPath, out bool ok);
            if (!ok || prefab == null)
            {
                Debug.LogError("Failed to save " + prefabPath);
                return;
            }
        }
        finally
        {
            root.name = sceneName;
        }

        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(configPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<LevelConfig>();
            config.levelId = name;
            AssetDatabase.CreateAsset(config, configPath);
        }
        config.layoutPrefab = prefab;
        config.levelLength = preview.levelLength;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        EditorGUIUtility.PingObject(config);
        Selection.activeObject = config;
        Debug.Log("Exported " + name + ":\n  " + prefabPath + " (" + (hasHole ? root.childCount - 1 : root.childCount) + " obstacles" + (hasHole ? " + hole" : ", NO HOLE") + ")\n  " + configPath + " (levelLength " + preview.levelLength + ")\nNow add it to GameManager's " + (preview.levelLength > controller.DesignLength ? "tallLevels" : "allLevels") + " in Bootstrap.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
