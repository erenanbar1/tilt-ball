using UnityEngine;
using UnityEngine.Serialization;

// Lives on the Level root in the Gameplay scene. Reads GameManager.currentLevel
// — set by LevelSelect before Gameplay was loaded — and applies it to the scene:
// the level's vertical length first, then the layout. No progression-tracking
// of its own: GameManager is the persistent source of truth for which level is
// active, since it lives in Bootstrap and survives every scene swap.
//
// One scene serves every level. The scene holds only what every level shares —
// the stick rig and the background — authored as a single screenful,
// profile.designLength tall, centred on this object. Everything that makes a
// level *that* level — the winning hole and the obstacles — comes in with the
// config's layout prefab, and a level that asks for more length than one
// screenful simply has its summit pushed up by the difference.
public class LevelController : MonoBehaviour
{
    public ScreenFitProfile profile;
    [FormerlySerializedAs("obstaclesRoot")]
    public Transform layoutRoot;
    [Header("Background — one sprite, set per level on LevelConfig")]
    // The panel: tiled over exactly the level box.
    public LevelBackground background;
    // The same art, dimmed, tiled over everything the camera can see beyond
    // the box — sides on a wide screen, above/below on a tall phone playing
    // a Classic level. World-space like the panel; it never follows the camera.
    public TiledBackground surround;

    [Header("Climb geometry")]
    public StickController stick;
    public CameraClimbFollow cameraFollow;
    public Transform pulleys;

    float floorY;
    float ceilingY;
    bool hasBounds;

    // The camera view the surround was last sized for; re-covered if it
    // changes (CameraAspectFit re-zooms on a rotation or window resize).
    float surroundOrtho = -1f;
    float surroundAspect = -1f;

    // The geometry LevelDesignPreview needs to draw the same picture in Edit
    // mode, kept here so the two can't drift apart.
    public float DesignLength => profile != null ? profile.designLength : 0f;
    // Bottom of the authored screenful — the scene is authored centred on this object.
    public float FloorY => transform.position.y - DesignLength * 0.5f;
    // How much a level of the given length adds above the authored screenful.
    public static float ExtraFor(float levelLength, float designLength) => Mathf.Max(levelLength, designLength) - designLength;

    void Awake()
    {
        ApplyLevelGeometry();
        SpawnLayout();
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
        var config = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        var sprite = config != null ? config.backgroundSprite : DesignSceneSprite();
        if (sprite != null)
        {
            if (background != null) background.SetSprite(sprite);
            if (surround != null) surround.SetSprite(sprite);
        }
        if (!hasBounds) return;
        if (background != null) background.Fit(floorY, ceilingY);
        CoverSurround();
    }

    void LateUpdate()
    {
        if (!hasBounds || surround == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        if (cam.orthographicSize != surroundOrtho || cam.aspect != surroundAspect) CoverSurround();
    }

    void CoverSurround()
    {
        var cam = Camera.main;
        if (surround == null || cam == null) return;
        surroundOrtho = cam.orthographicSize;
        surroundAspect = cam.aspect;
        surround.Cover(SurroundRect(floorY, ceilingY, cam));
    }

    // Everything the camera can ever show for this level, as a world rect:
    // the full view width (the play area is never wider than the view), and
    // vertically either the level itself — CameraClimbFollow keeps the view
    // inside a level taller than the view — or, for a level shorter than the
    // view, the view centred on the level.
    public Rect SurroundRect(float floor, float ceiling, Camera cam)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float centreY = (floor + ceiling) * 0.5f;
        float bottom = Mathf.Min(floor, centreY - halfH);
        float top = Mathf.Max(ceiling, centreY + halfH);
        return new Rect(transform.position.x - halfW, bottom, halfW * 2f, top - bottom);
    }

    // Stretches the scene's climb to the length this level asks for.
    //
    // The authored screenful is the floor of what a level can be; a longer level
    // adds `extra` on top of it. What belongs at the top of the climb — the
    // pulleys the ropes run over, and the stick's highest reachable point — is
    // lifted by that same amount, so the rig keeps whatever proportions the
    // scene was authored with instead of each piece needing its own per-level
    // number. A Classic level has extra 0 and nothing moves. The winning hole
    // is not the scene's to move: it's part of the layout prefab, placed by the
    // designer in absolute world coordinates.
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
        float designLength = DesignLength;
        float extra = ExtraFor(config != null ? config.levelLength : DesignSceneLength(), designLength);
        float length = designLength + extra;

        floorY = FloorY;
        ceilingY = floorY + length;
        hasBounds = true;

        if (extra > 0f)
        {
            if (stick != null) stick.maxOffset += extra;
            if (pulleys != null) pulleys.position += new Vector3(0f, extra, 0f);
        }

        // Always handed over, even for a Classic level: the clamp collapses to
        // the level's centre when the view is at least as tall as the level, so
        // the camera simply holds still there.
        if (cameraFollow != null) cameraFollow.ConfigureBounds(floorY, ceilingY);
    }

    // Pressing Play in a level design scene (a copy of Gameplay with a
    // LevelDesignPreview, run without Bootstrap) has no GameManager to ask, so
    // the length comes from the preview instead — the level plays at the length
    // it's being designed at, through this same code path.
    float DesignSceneLength()
    {
        var preview = GetComponent<LevelDesignPreview>();
        return preview != null ? preview.levelLength : 0f;
    }

    Sprite DesignSceneSprite()
    {
        var preview = GetComponent<LevelDesignPreview>();
        return preview != null ? preview.backgroundSprite : null;
    }

    void SpawnLayout()
    {
        var currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : null;
        if (currentLevel == null || currentLevel.layoutPrefab == null || layoutRoot == null) return;

        Instantiate(currentLevel.layoutPrefab, layoutRoot);
    }
}
