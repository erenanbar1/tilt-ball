using UnityEngine;

// Lives on the Level root in a gameplay scene. Reads GameManager.currentLevel
// — set by LevelSelect before Gameplay was loaded — and applies it to the scene:
// the climb geometry first, then the obstacles. No progression-tracking of its
// own: GameManager is the persistent source of truth for which level is active,
// since it lives in Bootstrap and survives every scene swap.
//
// The geometry references below are left empty in the Classic scene, where every
// level shares one scene-authored layout. Only the tall scene wires them.
public class LevelController : MonoBehaviour
{
    public Transform obstaclesRoot;

    [Header("Tall levels — leave empty in the Classic scene")]
    public StickController stick;
    public CameraClimbFollow cameraFollow;
    public Transform pulleys;
    public Transform winningHole;

    [Header("Standalone preview")]
    // Used only when this scene is played on its own. Normally the level comes
    // from GameManager, which lives in Bootstrap and is told which level to load
    // before this scene is ever opened — but pressing play on this scene directly,
    // which is the quickest way to try a level while building it, means no
    // Bootstrap and so no GameManager at all. Without this the scene would simply
    // come up empty, since the board, rig and background all arrive with the
    // level prefab now. Ignored whenever a real level has been chosen.
    public GameObject previewLevelPrefab;

    void Awake()
    {
        var config = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;

        // A level that ships as one prefab brings its own board, rig, background
        // and winning hole, and a LevelBoard that stretches all of it to the
        // length the prefab was authored at. Nothing here has anything left to
        // apply to it, so the scene-authored path is skipped entirely rather than
        // being run first and then overridden.
        GameObject prefab = config != null ? config.levelPrefab : PreviewLevel();
        if (prefab != null)
        {
            Instantiate(prefab, transform);
            return;
        }

        ApplyLevelGeometry();
        SpawnObstacles();
    }

#if UNITY_EDITOR
    // Where the "play this level" button leaves its choice. SessionState rather
    // than EditorPrefs: which level was last tried is scratch state, and should
    // not still be in force after the editor is restarted.
    public const string PreviewLevelKey = "TiltBall.PreviewLevelPrefab";
#endif

    // Which level to show when nobody picked one — that is, when this scene was
    // played on its own instead of being reached through the menus.
    GameObject PreviewLevel()
    {
#if UNITY_EDITOR
        string path = UnityEditor.SessionState.GetString(PreviewLevelKey, string.Empty);
        if (!string.IsNullOrEmpty(path))
        {
            var chosen = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (chosen != null) return chosen;
        }
#endif
        return previewLevelPrefab;
    }

    // Deferred to Start so the camera lands on a rig that has finished waking up,
    // rather than gliding in from wherever the scene left it. A prefab-based level
    // does its own snap, from LevelBoard.
    void Start()
    {
        if (cameraFollow != null) cameraFollow.SnapToTarget();
    }

    // Stretches the scene's climb to the height this level asks for.
    //
    // Everything that belongs at the top of the climb — the pulleys the ropes run
    // over, the hole the ball is aiming for — is lifted by the same amount the
    // summit moved, so the rig keeps whatever proportions the scene was authored
    // with instead of each piece needing its own per-level number.
    //
    // Only maxOffset is written on the stick: it's re-read every FixedUpdate, so
    // undefined Awake order between objects can't bite. The stick's position and
    // minOffset are captured in its own Awake and must not be touched from here.
    void ApplyLevelGeometry()
    {
        var config = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        if (config == null || config.climbHeight <= 0f || stick == null) return;

        float spawnY = stick.transform.position.y;
        float rise = config.climbHeight - stick.maxOffset;
        stick.maxOffset = config.climbHeight;

        if (pulleys != null) pulleys.position += new Vector3(0f, rise, 0f);
        if (winningHole != null) winningHole.position += new Vector3(0f, rise, 0f);

        if (cameraFollow != null)
            cameraFollow.ConfigureBounds(config.levelFloorY, spawnY + config.climbHeight + config.ceilingPadding);
    }

    void SpawnObstacles()
    {
        var currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        if (currentLevel == null || currentLevel.obstaclesPrefab == null || obstaclesRoot == null) return;

        Instantiate(currentLevel.obstaclesPrefab, obstaclesRoot);
    }
}
