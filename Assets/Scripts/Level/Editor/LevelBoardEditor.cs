using UnityEditor;
using UnityEngine;

// Adds the play button to a level's own Inspector, so a level can be tried from
// the thing being edited rather than from the scene that happens to host it.
[CustomEditor(typeof(LevelBoard))]
public class LevelBoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var board = (LevelBoard)target;
        string path = ResolveAssetPath(board);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(path) || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("▶  Bu bölümü oyna", GUILayout.Height(32)))
            {
                LevelPlayTest.Play(path);
            }
        }

        if (string.IsNullOrEmpty(path))
        {
            EditorGUILayout.HelpBox(
                "Bu bir prefab örneği değil. Bölümü oynamak için Project penceresindeki prefabı aç.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Uzunluk: " + board.climbHeight.ToString("0.##") +
                (board.cameraClimbs ? "  ·  kamera tırmanır" : "  ·  kamera sabit") +
                "\nKolon: " + board.FloorY.ToString("0.##") + " … " + board.CeilingY.ToString("0.##") +
                "  ·  arka plan " + (board.backgroundRoot != null ? board.backgroundRoot.childCount : 0) + " parça",
                MessageType.None);

            // Which level Play will actually show. Worth stating outright: the
            // armed level is whichever was opened last, which is not necessarily
            // the one whose Inspector is on screen.
            string armed = LevelPlayTest.ArmedLevel;
            if (string.IsNullOrEmpty(armed))
            {
                EditorGUILayout.HelpBox("Play şu an normal akışı izler (Bootstrap ▸ Main Menu).", MessageType.None);
            }
            else if (armed == path)
            {
                EditorGUILayout.HelpBox("Play bu bölümü açar.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Dikkat: Play şu an '" + System.IO.Path.GetFileNameWithoutExtension(armed) + "' bölümünü açar, bunu değil.\n" +
                    "Bunu oynamak için yukarıdaki ▶ düğmesine bas.",
                    MessageType.Warning);
            }

            if (!string.IsNullOrEmpty(armed) && GUILayout.Button("Önizlemeyi kapat (normal akışa dön)"))
            {
                LevelPlayTest.Disarm();
            }
        }
    }

    // The level may be open in a prefab stage, selected as an asset, or dropped
    // in a scene as an instance; all three should lead back to the same prefab.
    static string ResolveAssetPath(LevelBoard board)
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(board.gameObject);
        if (stage != null) return stage.assetPath;

        string path = AssetDatabase.GetAssetPath(board.gameObject);
        if (!string.IsNullOrEmpty(path)) return path;

        var source = PrefabUtility.GetCorrespondingObjectFromSource(board.gameObject);
        return source != null ? AssetDatabase.GetAssetPath(source) : null;
    }
}
