using System.Linq;
using UnityEditor;
using UnityEngine;

// Keeps Assets/Levels/LevelCatalog.asset in sync with the LevelConfig assets
// under Assets/Levels/Configs: sorted by levelIndex, split Classic/Tall by
// climbHeight. Runs automatically after any config is imported, deleted or
// moved, and on demand from the menu.
public class LevelCatalogBuilder : AssetPostprocessor
{
    public const string ConfigsFolder = "Assets/Levels/Configs";
    public const string CatalogPath = "Assets/Levels/LevelCatalog.asset";

    [MenuItem("Tools/Tilt Ball/Rebuild Level Catalog")]
    public static void Rebuild()
    {
        var configs = AssetDatabase.FindAssets("t:LevelConfig", new[] { ConfigsFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<LevelConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null)
            .ToArray();

        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.classicLevels = Ordered(configs, GameMode.Classic);
        catalog.tallLevels = Ordered(configs, GameMode.Tall);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);

        Debug.Log($"LevelCatalog rebuilt: {catalog.classicLevels.Length} Classic, {catalog.tallLevels.Length} Tall.", catalog);
    }

    static LevelConfig[] Ordered(LevelConfig[] all, GameMode mode)
    {
        var list = all.Where(c => c.Mode == mode).OrderBy(c => c.levelIndex).ThenBy(c => c.name).ToArray();
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].levelIndex != i)
                Debug.LogWarning($"{mode} level '{list[i].name}' has levelIndex {list[i].levelIndex} but sits at position {i} — indices should be contiguous from 0.", list[i]);
        }
        return list;
    }

    public static LevelCatalog Catalog => AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        bool touched = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
            .Any(p => p.StartsWith(ConfigsFolder + "/") && p.EndsWith(".asset"));
        if (!touched) return;

        // Deferred: the asset database is still mid-import here.
        EditorApplication.delayCall += Rebuild;
    }
}
