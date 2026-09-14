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
// To change an existing level: select its LevelConfig and run
// Tools > Tilt Ball > Edit Selected Level. That opens a design scene named
// after the level with its layout, length and background already in place;
// edit, then Export under the same name to overwrite the level's assets.
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
        var controller = OpenDesignScene(path, out var scene, out var preview);
        if (controller == null) return;

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

    [MenuItem("Tools/Tilt Ball/Edit Selected Level", true)]
    static bool EditSelectedLevelValid() => Selection.activeObject is LevelConfig;

    // Opens (or creates) Assets/Levels/Designs/<level>.unity with the level's
    // current layout, length and background in place. The layout comes in
    // detached from the level's prefab — loose pieces under LayoutRoot, each
    // still its own obstacle prefab — so editing here never touches the
    // level's assets until Export overwrites them under the same name.
    [MenuItem("Tools/Tilt Ball/Edit Selected Level")]
    static void EditSelectedLevel()
    {
        var config = Selection.activeObject as LevelConfig;
        if (config == null)
        {
            EditorUtility.DisplayDialog("Edit Level", "Select a LevelConfig (Assets/Levels/Configs) first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(DesignsFolder);
        string path = DesignsFolder + "/" + config.name + ".unity";
        bool existed = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
        var controller = OpenDesignScene(path, out var scene, out var preview);
        if (controller == null) return;

        var root = controller.layoutRoot;
        if (existed && root != null && root.childCount > 0)
        {
            int choice = EditorUtility.DisplayDialogComplex("Edit Level",
                path + " already has a layout in it.\n\nReplace it with " + config.name + "'s current exported layout, or keep what's in the scene?",
                "Replace with exported", "Cancel", "Keep scene's layout");
            if (choice == 1) return;
            if (choice == 0) LoadLayout(config, root);
        }
        else if (root != null)
        {
            LoadLayout(config, root);
        }

        // Through a SerializedObject so the preview's OnValidate applies the
        // length and sprite immediately, as an Inspector edit would.
        var so = new SerializedObject(preview);
        so.FindProperty("levelName").stringValue = config.name;
        so.FindProperty("levelLength").floatValue = config.levelLength;
        so.FindProperty("backgroundSprite").objectReferenceValue = config.backgroundSprite;
        so.ApplyModifiedProperties();

        // The preview records Undo for the pulleys it moves; that registration
        // is deferred to the next editor tick and would dirty the scene right
        // after it was saved. Flush it now so the save is the last word.
        Undo.FlushUndoRecordObjects();
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = controller.gameObject;
        Debug.Log("Editing " + config.name + " in " + path + "\nEdit under Level/LayoutRoot, then Tools > Tilt Ball > Export Level Assets (same name) to overwrite the level.");
    }

    // Copies Gameplay to path if nothing is there yet, opens it, and makes sure
    // its Level root carries a LevelDesignPreview.
    static LevelController OpenDesignScene(string path, out UnityEngine.SceneManagement.Scene scene, out LevelDesignPreview preview)
    {
        scene = default;
        preview = null;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null && !AssetDatabase.CopyAsset(GameplayScenePath, path))
        {
            Debug.LogError("Could not copy " + GameplayScenePath + " to " + path);
            return null;
        }

        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<LevelController>();
        if (controller == null)
        {
            Debug.LogError(path + " has no LevelController on its Level root.");
            return null;
        }
        preview = controller.GetComponent<LevelDesignPreview>();
        if (preview == null) preview = controller.gameObject.AddComponent<LevelDesignPreview>();
        return controller;
    }

    // Replaces whatever is under root with the level's exported layout, as
    // loose pieces (see DetachBorrowedLayouts). Not undoable: the scene is
    // saved right after, and Undo entries flush a tick later and would mark
    // the freshly saved scene dirty.
    static void LoadLayout(LevelConfig config, Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
        if (config.layoutPrefab == null)
        {
            Debug.LogWarning(config.name + " has no layout prefab — starting empty.");
            return;
        }
        PrefabUtility.InstantiatePrefab(config.layoutPrefab, root);
        DetachBorrowedLayouts(root, undoable: false);
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
        int holes = root.GetComponentsInChildren<WinTrigger>(true).Length;
        bool hasHole = holes > 0;
        if (!hasHole && !EditorUtility.DisplayDialog("Export Level Assets", "There is no WinningHole under LayoutRoot — this level can't be won. Export anyway?", "Export", "Cancel"))
        {
            return;
        }
        // Easy to end up with when a level was started from another level's
        // layout: that one brought its own hole along.
        if (holes > 1 && !EditorUtility.DisplayDialog("Export Level Assets", "There are " + holes + " WinningHoles under LayoutRoot — the first one the ball reaches wins. Export anyway?", "Export", "Cancel"))
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
        int borrowed = DetachBorrowedLayouts(root);
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
        config.backgroundSprite = preview.backgroundSprite;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        EditorGUIUtility.PingObject(config);
        Selection.activeObject = config;
        Debug.Log("Exported " + name + ":\n  " + prefabPath + " (" + (hasHole ? root.childCount - 1 : root.childCount) + " obstacles" + (hasHole ? " + hole" : ", NO HOLE") + ")\n  " + configPath + " (levelLength " + preview.levelLength + ")" + (borrowed > 0 ? "\n  detached " + borrowed + " borrowed layout prefab(s) - the source level(s) were not modified" : "") + "\nNow add it to GameManager's " + (preview.levelLength > controller.DesignLength ? "tallLevels" : "allLevels") + " in Bootstrap.");
    }

    // A designer who starts from an existing level drags its layout prefab
    // under LayoutRoot, which leaves a live link to that level's asset. Left
    // in, the export would nest it — the new level would then silently inherit
    // every later edit to the old one, and an "Apply" on that nested instance
    // would write into the old level. So any instance of a layout prefab found
    // under LayoutRoot has its outer link removed (the obstacles inside keep
    // theirs) and its children hoisted up, leaving a standalone layout.
    static int DetachBorrowedLayouts(Transform root, bool undoable = true)
    {
        int detached = 0;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(child)) continue;
            string source = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(child));
            if (string.IsNullOrEmpty(source) || !source.StartsWith(ObstaclePrefabsFolder + "/")) continue;

            if (undoable) Undo.RegisterFullObjectHierarchyUndo(child, "Detach borrowed layout");
            PrefabUtility.UnpackPrefabInstance(child, PrefabUnpackMode.OutermostRoot, undoable ? InteractionMode.UserAction : InteractionMode.AutomatedAction);
            int insertAt = child.transform.GetSiblingIndex();
            while (child.transform.childCount > 0)
            {
                var grandchild = child.transform.GetChild(child.transform.childCount - 1);
                if (undoable) Undo.SetTransformParent(grandchild, root, "Detach borrowed layout");
                else grandchild.SetParent(root, true);
                grandchild.SetSiblingIndex(insertAt);
            }
            if (undoable) Undo.DestroyObjectImmediate(child);
            else Object.DestroyImmediate(child);
            detached++;
        }
        return detached;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
