using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The level designer's entry points, all under Tools > Tilt Ball. See the
// README section "Adding a level" for the intended workflow.
public static class LevelAuthoring
{
    const string ObstaclePrefabsFolder = "Assets/Levels/ObstaclePrefabs";
    const string GameplayScenePath = "Assets/Scenes/end-to-end/Gameplay.unity";
    const string BootstrapScenePath = "Assets/Scenes/end-to-end/Bootstrap.unity";

    // Matches the defaults Tall_01/Tall_02 were authored with.
    const float DefaultClimbHeight = 34f;

    [MenuItem("Tools/Tilt Ball/New Classic Level", priority = 0)]
    public static void NewClassicLevel() => NewLevel(GameMode.Classic);

    [MenuItem("Tools/Tilt Ball/New Tall Level", priority = 1)]
    public static void NewTallLevel() => NewLevel(GameMode.Tall);

    public static LevelConfig NewLevel(GameMode mode)
    {
        var catalog = LevelCatalogBuilder.Catalog;
        var existing = catalog != null ? catalog.LevelsFor(mode) : null;
        int index = existing != null ? existing.Length : 0;

        string prefix = mode == GameMode.Tall ? "Tall" : "Level";
        string name = $"{prefix}_{index + 1:00}";

        Directory.CreateDirectory(ObstaclePrefabsFolder);
        Directory.CreateDirectory(LevelCatalogBuilder.ConfigsFolder);

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{ObstaclePrefabsFolder}/{name}_Obstacles.prefab");
        var root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        var config = ScriptableObject.CreateInstance<LevelConfig>();
        config.levelIndex = index;
        config.obstaclesPrefab = prefab;
        if (mode == GameMode.Tall) config.climbHeight = DefaultClimbHeight;
        string configPath = AssetDatabase.GenerateUniqueAssetPath($"{LevelCatalogBuilder.ConfigsFolder}/{name}.asset");
        AssetDatabase.CreateAsset(config, configPath);
        AssetDatabase.SaveAssets();

        LevelCatalogBuilder.Rebuild();
        OpenInGameplay(config);

        Debug.Log($"Created {mode} level '{config.name}' (#{index + 1}) with obstacle prefab '{prefab.name}'. Place pieces under ObstaclesRoot, then Apply from the Level object's Inspector.", config);
        return config;
    }

    [MenuItem("Assets/Tilt Ball/Open Level in Gameplay Scene", validate = true)]
    static bool OpenInGameplayValidate() => Selection.activeObject is LevelConfig;

    [MenuItem("Assets/Tilt Ball/Open Level in Gameplay Scene")]
    static void OpenInGameplayMenu() => OpenInGameplay((LevelConfig)Selection.activeObject);

    // Loads the gameplay scene (offering to save the current one) and points
    // LevelPreview at the config, so the designer edits with full context.
    public static void OpenInGameplay(LevelConfig config)
    {
        if (config == null) return;
        if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play mode first."); return; }

        var active = EditorSceneManager.GetActiveScene();
        if (active.path != GameplayScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(GameplayScenePath);
        }

        var preview = Object.FindFirstObjectByType<LevelPreview>();
        if (preview == null)
        {
            var controller = Object.FindFirstObjectByType<LevelController>();
            if (controller == null) { Debug.LogError("Gameplay scene has no LevelController."); return; }
            preview = Undo.AddComponent<LevelPreview>(controller.gameObject);
        }

        Undo.RecordObject(preview, "Preview level");
        preview.previewLevel = config;
        preview.Refresh();
        EditorUtility.SetDirty(preview);
        Selection.activeGameObject = preview.gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    [MenuItem("Assets/Tilt Ball/Play This Level", validate = true)]
    static bool PlayLevelValidate() => Selection.activeObject is LevelConfig && !EditorApplication.isPlaying;

    [MenuItem("Assets/Tilt Ball/Play This Level")]
    static void PlayLevelMenu() => PlayLevel((LevelConfig)Selection.activeObject);

    // Enters Play mode from Bootstrap with LevelManager told to skip the menus
    // and load this level directly.
    public static void PlayLevel(LevelConfig config)
    {
        if (config == null || EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        LevelManager.RequestDebugStart(config);
        EditorSceneManager.OpenScene(BootstrapScenePath);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Tilt Ball/Progress/Unlock All Levels", priority = 100)]
    static void UnlockAll()
    {
        var catalog = LevelCatalogBuilder.Catalog;
        if (catalog == null) { Debug.LogWarning("No LevelCatalog — rebuild it first."); return; }
        PlayerPrefs.SetInt("HighestUnlockedLevelIndex", Mathf.Max(0, catalog.classicLevels.Length - 1));
        PlayerPrefs.SetInt("Tall_HighestUnlockedLevelIndex", Mathf.Max(0, catalog.tallLevels.Length - 1));
        PlayerPrefs.Save();
        Debug.Log("All levels unlocked (takes effect next Play).");
    }

    [MenuItem("Tools/Tilt Ball/Progress/Reset Progress", priority = 101)]
    static void ResetProgress()
    {
        PlayerPrefs.DeleteKey("HighestUnlockedLevelIndex");
        PlayerPrefs.DeleteKey("Tall_HighestUnlockedLevelIndex");
        PlayerPrefs.Save();
        Debug.Log("Level progress reset (takes effect next Play).");
    }
}

[CustomEditor(typeof(LevelPreview))]
public class LevelPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var preview = (LevelPreview)target;
        DrawDefaultInspector();

        var inst = preview.CurrentInstance;
        bool dirty = preview.HasUnsavedEdits;

        EditorGUILayout.Space();
        if (preview.previewLevel == null)
        {
            EditorGUILayout.HelpBox("Pick a LevelConfig to edit its obstacles in place. Drag pieces from Assets/Prefabs (LoseHole_*, Obstacles/*) under the preview object inside ObstaclesRoot.", MessageType.Info);
            return;
        }

        if (preview.previewLevel.obstaclesPrefab == null)
            EditorGUILayout.HelpBox("This level has no obstacle prefab assigned.", MessageType.Warning);
        else if (inst == null)
            EditorGUILayout.HelpBox("Preview not spawned — press Refresh.", MessageType.Warning);
        else
            EditorGUILayout.HelpBox(dirty ? "Unsaved edits — Apply to write them into the obstacle prefab." : "Preview matches the prefab.", dirty ? MessageType.Warning : MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!dirty))
            {
                if (GUILayout.Button("Apply to Prefab")) preview.ApplyToPrefab();
                if (GUILayout.Button("Revert")) preview.RevertToPrefab();
            }
            if (GUILayout.Button("Refresh")) { preview.Clear(); preview.Refresh(); }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(dirty || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Play This Level")) LevelAuthoring.PlayLevel(preview.previewLevel);
            }
            if (GUILayout.Button("Select Config")) Selection.activeObject = preview.previewLevel;
        }
        if (dirty) EditorGUILayout.LabelField("Apply before playing — the game spawns the saved prefab, not the preview.", EditorStyles.miniLabel);
    }
}

[CustomEditor(typeof(LevelConfig))]
public class LevelConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var config = (LevelConfig)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Mode: {config.Mode}  (Tall when climbHeight > 0)", EditorStyles.miniLabel);

        var catalog = LevelCatalogBuilder.Catalog;
        var list = catalog != null ? catalog.LevelsFor(config.Mode) : null;
        int pos = list != null ? System.Array.IndexOf(list, config) : -1;
        if (pos < 0) EditorGUILayout.HelpBox("Not in the LevelCatalog. Save it under Assets/Levels/Configs or run Tools > Tilt Ball > Rebuild Level Catalog.", MessageType.Warning);
        else if (pos != config.levelIndex) EditorGUILayout.HelpBox($"levelIndex {config.levelIndex} but the catalog sorts it to position {pos} — make indices contiguous.", MessageType.Warning);
        else EditorGUILayout.LabelField($"Catalog position {pos + 1} of {list.Length} {config.Mode}", EditorStyles.miniLabel);

        if (config.obstaclesPrefab == null) EditorGUILayout.HelpBox("No obstacle prefab — the level will be empty.", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Open in Gameplay Scene")) LevelAuthoring.OpenInGameplay(config);
                if (GUILayout.Button("Play This Level")) LevelAuthoring.PlayLevel(config);
            }
        }
    }
}
