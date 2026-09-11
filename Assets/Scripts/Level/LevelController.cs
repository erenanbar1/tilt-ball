using UnityEngine;

// Lives on the Level root in the gameplay scene. Reads LevelManager.Current
// — set by LevelSelect before Gameplay was loaded — and applies it to the scene:
// the climb geometry first, then the obstacles. No progression-tracking of its
// own: LevelManager is the persistent source of truth for which level is active,
// since it lives in Bootstrap and survives every scene swap.
//
// Classic configs have climbHeight = 0, so the geometry step is a no-op for
// them and the one scene-authored layout is used as-is.
public class LevelController : MonoBehaviour
{
    public Transform obstaclesRoot;
    public LevelBackground background;

    [Header("Tall levels — leave empty in the Classic scene")]
    public StickController stick;
    public CameraClimbFollow cameraFollow;
    public Transform pulleys;
    public Transform winningHole;

    float floorY;
    float ceilingY;
    bool hasClimbRange;

    void Awake()
    {
        ApplyLevelGeometry();
        SpawnObstacles();
    }

    // Deferred to Start so the camera lands on a rig that has finished waking up,
    // rather than gliding in from wherever the scene left it — and so the
    // background's own OnEnable fit can't run after ours and undo it.
    void Start()
    {
        ApplyBackground();
        if (cameraFollow != null) cameraFollow.SnapToTarget();
    }

    void ApplyBackground()
    {
        if (background == null) return;

        var config = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
        if (config != null && config.backgroundSprite != null) background.SetSprite(config.backgroundSprite);
        if (hasClimbRange) background.Fit(floorY, ceilingY);
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
        var config = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
        if (config == null || config.climbHeight <= 0f || stick == null) return;

        float spawnY = stick.transform.position.y;
        float rise = config.climbHeight - stick.maxOffset;
        stick.maxOffset = config.climbHeight;

        if (pulleys != null) pulleys.position += new Vector3(0f, rise, 0f);
        if (winningHole != null) winningHole.position += new Vector3(0f, rise, 0f);

        floorY = config.levelFloorY;
        ceilingY = spawnY + config.climbHeight + config.ceilingPadding;
        hasClimbRange = true;

        if (cameraFollow != null) cameraFollow.ConfigureBounds(floorY, ceilingY);
    }

    void SpawnObstacles()
    {
        if (obstaclesRoot == null) return;

        // A LevelPreview instance should never survive into Play, but if one
        // did (crash mid-edit, hooks skipped) it would double the obstacles.
        for (int i = obstaclesRoot.childCount - 1; i >= 0; i--)
        {
            var child = obstaclesRoot.GetChild(i).gameObject;
            if (LevelPreview.IsPreview(child)) Destroy(child);
        }

        var currentLevel = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
        if (currentLevel == null || currentLevel.obstaclesPrefab == null) return;

        Instantiate(currentLevel.obstaclesPrefab, obstaclesRoot);
    }
}
