using UnityEngine;

// Lives on the Level root in the Gameplay scene. Reads GameManager.currentLevel
// — set by LevelSelect before Gameplay was loaded — and applies it to the scene:
// the level's vertical length first, then the obstacles. No progression-tracking
// of its own: GameManager is the persistent source of truth for which level is
// active, since it lives in Bootstrap and survives every scene swap.
//
// One scene serves every level. The scene is authored as a single screenful —
// profile.designLength tall, centred on this object — and a level that asks for
// more length than that simply has its summit pushed up by the difference.
public class LevelController : MonoBehaviour
{
    public ScreenFitProfile profile;
    public Transform obstaclesRoot;
    public LevelBackground background;

    [Header("Climb geometry")]
    public StickController stick;
    public CameraClimbFollow cameraFollow;
    public Transform pulleys;
    public Transform winningHole;

    float floorY;
    float ceilingY;
    bool hasBounds;

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

        var config = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        if (config != null && config.backgroundSprite != null) background.SetSprite(config.backgroundSprite);
        if (hasBounds) background.Fit(floorY, ceilingY);
    }

    // Stretches the scene's climb to the length this level asks for.
    //
    // The authored screenful is the floor of what a level can be; a longer level
    // adds `extra` on top of it. Everything that belongs at the top of the climb
    // — the pulleys the ropes run over, the hole the ball is aiming for, the
    // stick's highest reachable point — is lifted by that same amount, so the
    // rig keeps whatever proportions the scene was authored with instead of
    // each piece needing its own per-level number. A Classic level has extra 0
    // and nothing moves.
    //
    // Only maxOffset is written on the stick: it's re-read every FixedUpdate, so
    // undefined Awake order between objects can't bite. The stick's position and
    // minOffset are captured in its own Awake and must not be touched from here.
    //
    // With no profile there's no screenful to measure against, so the scene is
    // left exactly as authored.
    void ApplyLevelGeometry()
    {
        if (profile == null) return;

        var config = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        float designLength = profile.designLength;
        float length = Mathf.Max(config != null ? config.levelLength : 0f, designLength);
        float extra = length - designLength;

        floorY = transform.position.y - designLength * 0.5f;
        ceilingY = floorY + length;
        hasBounds = true;

        if (extra > 0f)
        {
            if (stick != null) stick.maxOffset += extra;
            if (pulleys != null) pulleys.position += new Vector3(0f, extra, 0f);
            if (winningHole != null) winningHole.position += new Vector3(0f, extra, 0f);
        }

        // Always handed over, even for a Classic level: the clamp collapses to
        // the level's centre when the view is at least as tall as the level, so
        // the camera simply holds still there.
        if (cameraFollow != null) cameraFollow.ConfigureBounds(floorY, ceilingY);
    }

    void SpawnObstacles()
    {
        var currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        if (currentLevel == null || currentLevel.obstaclesPrefab == null || obstaclesRoot == null) return;

        Instantiate(currentLevel.obstaclesPrefab, obstaclesRoot);
    }
}
