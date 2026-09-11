using UnityEngine;

// Editor-only level authoring aid on the Level root of the gameplay scene.
// Pick a LevelConfig and its obstacle prefab is instantiated under
// ObstaclesRoot as an ordinary prefab instance — edit the pieces in place with
// the stick, ball, hole and play-area box around them, then apply from the
// Inspector button or the usual Overrides dropdown.
//
// The instance is a scene object like any other so all prefab tooling works
// on it, but it never reaches disk or Play mode: it's removed just before
// the scene saves and before entering Play, and put back afterwards.
// LevelController also drops any leftover preview at runtime as a safety net.
//
// Gizmos draw the play-area box (design width x design length, or the climb
// range for Tall levels), the summit, and where the pulleys and winning hole
// will sit after LevelController lifts them.
[ExecuteAlways]
public class LevelPreview : MonoBehaviour
{
    public const string PreviewSuffix = " (preview)";

    public LevelConfig previewLevel;

#if UNITY_EDITOR
    LevelController controller;
    LevelController Controller => controller != null ? controller : (controller = GetComponent<LevelController>());

    void OnEnable()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
        Refresh();
    }

    void OnDisable()
    {
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
        if (!Application.isPlaying) Clear();
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Refresh(); };
    }

    void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change == UnityEditor.PlayModeStateChange.ExitingEditMode) Clear();
        else if (change == UnityEditor.PlayModeStateChange.EnteredEditMode) Refresh();
    }

    void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        if (scene == gameObject.scene) Clear();
    }

    void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
    {
        if (scene == gameObject.scene) Refresh();
    }

    public static bool IsPreview(GameObject go) => go != null && go.name.EndsWith(PreviewSuffix);

    public GameObject CurrentInstance
    {
        get
        {
            var root = Controller != null ? Controller.obstaclesRoot : null;
            if (root == null) return null;
            foreach (Transform child in root)
                if (IsPreview(child.gameObject)) return child.gameObject;
            return null;
        }
    }

    // Pieces are nested prefab instances (LoseHole_*, Obstacles/*), and Unity
    // files an instance root's transform under "default overrides" — invisible
    // to HasPrefabInstanceAnyOverrides and the Overrides dropdown, though Apply
    // does write it. So moves are detected by comparing against the source.
    public bool HasUnsavedEdits
    {
        get
        {
            var inst = CurrentInstance;
            if (inst == null) return false;
            if (UnityEditor.PrefabUtility.HasPrefabInstanceAnyOverrides(inst, false)) return true;

            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
            {
                if (t == inst.transform) continue;
                var src = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(t);
                if (src == null) return true;
                if (t.localPosition != src.localPosition || t.localRotation != src.localRotation || t.localScale != src.localScale)
                    return true;
            }
            return false;
        }
    }

    public void Refresh()
    {
        var existing = CurrentInstance;
        var wanted = previewLevel != null ? previewLevel.obstaclesPrefab : null;
        if (existing != null && (wanted == null || UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(existing) != wanted))
        {
            DestroyImmediate(existing);
            existing = null;
        }

        if (existing != null || wanted == null) return;
        var root = Controller != null ? Controller.obstaclesRoot : null;
        if (root == null) return;

        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(wanted, root);
        go.name = wanted.name + PreviewSuffix;
    }

    public void Clear()
    {
        var existing = CurrentInstance;
        if (existing != null) DestroyImmediate(existing);
    }

    // The "(preview)" suffix is a default override that ApplyPrefabInstance
    // would otherwise write into the asset's name.
    public void ApplyToPrefab()
    {
        var go = CurrentInstance;
        if (go == null) return;
        string previewName = go.name;
        go.name = previewLevel.obstaclesPrefab.name;
        UnityEditor.PrefabUtility.ApplyPrefabInstance(go, UnityEditor.InteractionMode.UserAction);
        go.name = previewName;
    }

    public void RevertToPrefab()
    {
        var go = CurrentInstance;
        if (go == null) return;
        UnityEditor.PrefabUtility.RevertPrefabInstance(go, UnityEditor.InteractionMode.UserAction);
        go.name = previewLevel.obstaclesPrefab.name + PreviewSuffix;
    }

    void OnDrawGizmos()
    {
        var ctrl = Controller;
        var profile = ctrl != null && ctrl.background != null ? ctrl.background.profile : null;
        if (profile == null) return;

        float halfW = profile.designWidth * 0.5f;
        float bottom, top;
        bool tall = previewLevel != null && previewLevel.IsTall && ctrl.stick != null;
        if (tall)
        {
            float spawnY = ctrl.stick.transform.position.y;
            bottom = previewLevel.levelFloorY;
            top = spawnY + previewLevel.climbHeight + previewLevel.ceilingPadding;
        }
        else
        {
            bottom = transform.position.y - profile.designLength * 0.5f;
            top = transform.position.y + profile.designLength * 0.5f;
        }

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Vector3 centre = new Vector3(transform.position.x, (bottom + top) * 0.5f, 0f);
        Gizmos.DrawWireCube(centre, new Vector3(halfW * 2f, top - bottom, 0f));

        if (!tall) return;

        float rise = previewLevel.climbHeight - ctrl.stick.maxOffset;
        float summitY = ctrl.stick.transform.position.y + previewLevel.climbHeight;
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.9f);
        Gizmos.DrawLine(new Vector3(-halfW, summitY, 0f), new Vector3(halfW, summitY, 0f));

        var lift = new Vector3(0f, rise, 0f);
        if (ctrl.winningHole != null) Gizmos.DrawWireSphere(ctrl.winningHole.position + lift, 0.45f);
        if (ctrl.pulleys != null)
            foreach (Transform p in ctrl.pulleys) Gizmos.DrawWireSphere(p.position + lift, 0.3f);
    }
#endif
}
