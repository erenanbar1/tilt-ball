using UnityEngine;

// One whole level, packaged. Everything a level owns — how long the board is, the
// art behind it, the obstacles on it, the rig that carries the Ball, and the hole
// at the top — lives inside this prefab. Authoring a new level is duplicating the
// prefab, typing a new length, and dragging obstacles in; nothing in a scene or a
// script has to be touched, and the LevelConfig only has to point at the prefab.
//
// The length is the one number that matters. Everything positional is measured
// from it rather than typed again per level: the stick's climb range, where the
// pulleys and the winning hole sit, how many background tiles get stacked, and
// how far the camera may scroll. Change the length and the whole level restretches
// — in the Scene view too, so it can be judged by eye before ever pressing play.
[ExecuteAlways]
public class LevelBoard : MonoBehaviour
{
    [Header("Length")]
    // How far each end of the stick may rise above where it spawns — the height
    // of the climb, and so the length of the board.
    [Min(0f)]
    public float climbHeight = 34f;

    // Off for the early levels, which are meant to sit still on one screen the way
    // the Classic ones do: the camera pins to the middle of the board and the whole
    // level is in frame from the first moment. A level short enough to fit anyway
    // doesn't need this — CameraClimbFollow already collapses its own travel range
    // — so it is only load-bearing on a long board that should deliberately not
    // move.
    public bool cameraClimbs = true;

    [Header("Framing")]
    // Board kept below the stick's spawn height and above the summit. These two
    // plus climbHeight are the whole column the camera is allowed to see; at the
    // defaults a climbHeight of 10.5 comes to exactly one 14-unit screenful.
    public float floorPadding = 2.75f;
    public float ceilingPadding = 0.75f;

    [Header("Rig — all inside this prefab")]
    public StickController stick;
    // Moved as a unit, so the two pulleys keep whatever spacing the rig was built
    // with. The ropes need no wiring: StringVisual tracks the pulleys itself.
    public Transform pulleys;
    public Transform winningHole;
    // What the camera follows. Left empty this is the stick itself, which is the
    // right answer: the stick is what climbs. Not the rig root, which never moves
    // — only the stick's own body is driven — and not the Ball, whose height
    // swings as it rolls, which would shake the frame for reasons the player
    // didn't cause.
    public Transform cameraTarget;

    [Header("Summit anchors — offsets from the top of the climb")]
    // Where the pulleys and the hole sit relative to the summit, so they ride up
    // with a longer board instead of needing a new number on every level. Drag
    // them in the Scene view and read the figures back off here.
    public Vector2 pulleyOffset = new Vector2(0f, 1f);
    public Vector2 winningHoleOffset = new Vector2(0f, 1f);

    [Header("Background")]
    // The art tiles seamlessly top to bottom, so the board is built by stacking
    // as many copies as the length needs rather than by stretching one.
    public Transform backgroundRoot;
    // Kept as a child of backgroundRoot and used as tile 0; the rest are copies of
    // it, so its sprite, scale, material and sorting order are the only place any
    // of that is set.
    public SpriteRenderer backgroundTile;
    // Extra board past both ends of the column, so a camera that overshoots
    // slightly on its way to a stop never reaches past the art.
    [Min(0f)]
    public float backgroundOverscan = 3f;

    [Header("Obstacles")]
    // Drop LoseHole_* — or anything else the level needs — in here. Nothing reads
    // the contents; they are just children that ride with the board.
    public Transform obstaclesRoot;

    const string TileName = "Tile";

    bool dirty = true;
    CameraClimbFollow follow;
    float lastCameraSize = -1f;

    // Where the stick started. Captured once, because the stick is the thing that
    // climbs: read live it would drag the floor and the ceiling up the level with
    // the ball, and a screen change halfway up would then rebuild the whole board
    // around wherever the player had got to. Left uncaptured while editing, so
    // dragging the rig in the Scene view still updates the preview.
    float capturedSpawnY;
    bool spawnCaptured;

    void OnValidate()
    {
        dirty = true;
#if UNITY_EDITOR
        // Two reasons not to just wait for Update. Tiles cannot be created or
        // destroyed from inside OnValidate, and an edit-mode Update only ticks
        // when the editor decides to repaint — which it does not while the window
        // sits in the background. delayCall lands the rebuild on a moment that is
        // both legal and guaranteed to arrive.
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += RebuildFromEditor;
#endif
    }

    void OnEnable() { dirty = true; }

#if UNITY_EDITOR
    void RebuildFromEditor()
    {
        // The component can be gone by the time the call comes back round — the
        // object deleted, or the prefab stage closed.
        if (this == null || !isActiveAndEnabled || Application.isPlaying) return;
        dirty = false;
        Apply();
    }
#endif

    void Awake()
    {
        if (!Application.isPlaying) return;
        capturedSpawnY = stick != null ? stick.transform.position.y : transform.position.y;
        spawnCaptured = true;
        Apply();
    }

    // Deferred so the camera lands on a rig that has finished waking, rather than
    // gliding in from wherever the scene left it.
    void Start()
    {
        if (!Application.isPlaying) return;
        CameraClimbFollow cameraFollow = Follow();
        if (cameraFollow == null) return;
        lastCameraSize = cameraFollow.GetComponent<Camera>().orthographicSize;
        cameraFollow.SnapToTarget();
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            // Nothing about a level changes once it spawns, with one exception:
            // CameraAspectFit resizes the camera whenever the screen does, and
            // both how much board has to be built and how far the camera may
            // travel are measured from that size. Rotating the phone, resizing
            // the Game view or switching device in the Simulator all land here.
            CameraClimbFollow cameraFollow = Follow();
            if (cameraFollow == null) return;

            float size = cameraFollow.GetComponent<Camera>().orthographicSize;
            if (Mathf.Approximately(size, lastCameraSize)) return;
            lastCameraSize = size;

            ConfigureCamera();
            RebuildBackground();
            return;
        }

        // While editing, the length and the anchors can change at any moment, so
        // the board is rebuilt whenever something was touched.
        if (!dirty) return;
        dirty = false;
        Apply();
    }

    // The heights the level occupies. Everything downstream is phrased in these
    // rather than in the raw length, so there is one place to read them from.
    public float SpawnY => spawnCaptured ? capturedSpawnY
                         : (stick != null ? stick.transform.position.y : transform.position.y);
    public float SummitY => SpawnY + climbHeight;
    public float FloorY => SpawnY - floorPadding;
    public float CeilingY => SummitY + ceilingPadding;

    void Apply()
    {
        // Only maxOffset is written. The stick captures its position and minOffset
        // in its own Awake, and Unity doesn't order Awake between objects, so
        // touching either from here would be a race; maxOffset is re-read every
        // FixedUpdate and is safe.
        if (stick != null) stick.maxOffset = climbHeight;

        float summitY = SummitY;
        PlaceAtSummit(pulleys, pulleyOffset, summitY);
        PlaceAtSummit(winningHole, winningHoleOffset, summitY);

        // Camera first: the background is built to cover what the camera can
        // actually see, which is not known until its travel range is set.
        ConfigureCamera();
        RebuildBackground();
    }

    void PlaceAtSummit(Transform t, Vector2 offset, float summitY)
    {
        if (t == null) return;
        t.position = new Vector3(transform.position.x + offset.x, summitY + offset.y, t.position.z);
    }

    void ConfigureCamera()
    {
        CameraClimbFollow follow = Follow();
        if (follow == null) return;

        Transform followTarget = cameraTarget != null ? cameraTarget
                               : (stick != null ? stick.transform : null);
        if (followTarget != null) follow.target = followTarget;

        if (cameraClimbs)
        {
            follow.ConfigureBounds(FloorY, CeilingY);
            return;
        }

        // A range with no width: ConfigureBounds collapses anything it cannot fit
        // a full view inside down to its midpoint, so handing it the same height
        // twice parks the camera there and it never moves again.
        float centre = (FloorY + CeilingY) * 0.5f;
        follow.ConfigureBounds(centre, centre);
    }

    // Cached at runtime: this is asked for every frame while watching for a
    // screen change, and a scene-wide search per frame is not the way to do that.
    CameraClimbFollow Follow()
    {
        if (Application.isPlaying && follow != null) return follow;
        return follow = FindCamera();
    }

    CameraClimbFollow FindCamera()
    {
#if UNITY_EDITOR
        // Opening this prefab for editing must not reach out and reconfigure a
        // level nobody is playing. A prefab stage is a scene of its own with no
        // camera in it, so an unguarded search walks straight past it and finds
        // whichever gameplay scene happens to be open behind the stage — which is
        // how editing a level ended up nulling the real camera's target.
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null) return null;
#endif
        // Otherwise found rather than wired: the camera belongs to the scene while
        // this belongs to a prefab, so there is no reference to drag across.
        return FindFirstObjectByType<CameraClimbFollow>();
    }

    // Stacks enough copies of the tile to cover the column plus its overscan. The
    // art meets itself exactly at top and bottom, so the tiles are simply butted
    // together a tile-height apart and the seams don't read.
    void RebuildBackground()
    {
        if (backgroundRoot == null || backgroundTile == null) return;

        float tileHeight = backgroundTile.bounds.size.y;
        if (tileHeight <= 0.0001f) return;

        // The column is the floor of it, but not always the whole story. On a tall
        // phone CameraAspectFit zooms out to keep the design's width on screen,
        // and on a level too short to scroll the camera parks in the middle and
        // its view can then reach past both ends of the level. Building only to
        // the column would leave the art short exactly there, so whatever the
        // camera can actually reach is taken into account too.
        float bottom = FloorY;
        float top = CeilingY;
        CameraClimbFollow cameraFollow = Follow();
        if (cameraFollow != null)
        {
            cameraFollow.GetVisibleRange(out float visibleBottom, out float visibleTop);
            bottom = Mathf.Min(bottom, visibleBottom);
            top = Mathf.Max(top, visibleTop);
        }

        bottom -= backgroundOverscan;
        float span = (top + backgroundOverscan) - bottom;
        int needed = Mathf.Max(1, Mathf.CeilToInt(span / tileHeight));

        // Where the tile's own pivot sits relative to its middle, so art pivoted
        // anywhere still lands with its edges where the maths says they are.
        float pivotToCentre = backgroundTile.transform.position.y - backgroundTile.bounds.center.y;

        ClearSurplusTiles(needed);

        for (int i = 0; i < needed; i++)
        {
            SpriteRenderer tile = i == 0 ? backgroundTile : GetOrCreateTile(i);
            if (tile == null) continue;
            tile.name = i == 0 ? backgroundTile.name : TileName + "_" + i.ToString("00");
            float centreY = bottom + tileHeight * (i + 0.5f);
            tile.transform.position = new Vector3(
                transform.position.x,
                centreY + pivotToCentre,
                backgroundRoot.position.z);
        }
    }

    SpriteRenderer GetOrCreateTile(int index)
    {
        Transform existing = backgroundRoot.Find(TileName + "_" + index.ToString("00"));
        if (existing != null) return existing.GetComponent<SpriteRenderer>();

        GameObject copy = Instantiate(backgroundTile.gameObject, backgroundRoot);
        copy.transform.localScale = backgroundTile.transform.localScale;
        copy.transform.localRotation = backgroundTile.transform.localRotation;
        return copy.GetComponent<SpriteRenderer>();
    }

    // Anything past what this length needs — left over from a longer board — goes,
    // so shortening a level cleans up after itself instead of leaving art hanging
    // off the top.
    void ClearSurplusTiles(int needed)
    {
        for (int i = backgroundRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = backgroundRoot.GetChild(i);
            if (child == backgroundTile.transform) continue;
            if (!child.name.StartsWith(TileName + "_")) continue;

            if (int.TryParse(child.name.Substring(TileName.Length + 1), out int index) && index < needed) continue;
            Discard(child.gameObject);
        }
    }

    static void Discard(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }
}
