using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Edit-mode preview of a level's vertical length, for designing levels in a
// copy of the Gameplay scene (Tools > Tilt Ball > New Level Design Scene).
//
// Sits next to LevelController on the Level root and applies the same stretch
// LevelController applies at runtime — pulleys and background lifted by the
// surplus over one screenful — so the layout (the winning hole and the
// obstacles) can be placed under LayoutRoot against the geometry the level
// will actually have. Gizmos draw the level box, the stick's summit and the
// ceiling; the summit line is the cue for where the hole belongs. The hole
// itself is the designer's to place, like any obstacle — the preview never
// moves it.
//
// Reversible: the displacement currently applied is recorded in appliedExtra,
// so a reopened scene knows its objects are already lifted, and setting the
// length back to 0 puts everything back where it was authored. Set it to 0
// before removing the component.
//
// Play mode is left to LevelController: the authored layout is restored the
// moment the editor leaves Edit mode, and LevelController reads levelLength
// from here when no GameManager is present, so pressing Play in a design
// scene plays the level at its designed length through the normal code path.
//
// Never add this to the shipping Gameplay scene.
[ExecuteAlways]
[RequireComponent(typeof(LevelController))]
public class LevelDesignPreview : MonoBehaviour
{
    [Tooltip("Name for the exported assets: <name>_Obstacles.prefab and <name>.asset (Tools > Tilt Ball > Export Level Assets).")]
    public string levelName = "Level_XX";

    [Tooltip("Vertical extent of the level in world units. 0 = one screenful (a Classic level). Copied into the LevelConfig on export.")]
    [Min(0f)]
    public float levelLength = 0f;

    [Tooltip("Art for both the level panel and the dimmed surround. Copied into the LevelConfig on export. Empty keeps the prefabs' default art.")]
    public Sprite backgroundSprite;

    [SerializeField, HideInInspector] float appliedExtra;

    LevelController controller;

    LevelController Controller => controller != null ? controller : (controller = GetComponent<LevelController>());

    // False from the instant Play is pressed, not just once the game is running:
    // the editor still ticks Update/OnValidate between ExitingEditMode (where
    // the authored layout is restored) and the actual switch, and Application.
    // isPlaying is still false there — an Apply() in that window would re-lift
    // the pulleys and LevelController would then lift them a second time.
    static bool InEditMode =>
#if UNITY_EDITOR
        !Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
#else
        false;
#endif

    void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        Apply();
    }

#if UNITY_EDITOR
    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    // Hand the scene to LevelController exactly as authored, and take it back
    // once Play is over.
    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) SetExtra(0f);
        else if (state == PlayModeStateChange.EnteredEditMode) Apply();
    }
#endif

    void OnValidate()
    {
        Apply();
    }

    // Polled rather than event-driven: LevelBackground refits itself to one
    // screenful in its own OnEnable, which can land after ours on scene load,
    // and prefab instances get reverted/re-applied behind our back. Every write
    // below is skipped when nothing changed, so this doesn't dirty the scene.
    void Update()
    {
        Apply();
    }

    void Apply()
    {
        if (!InEditMode) return;
        var c = Controller;
        if (c == null || c.profile == null) return;

        float design = c.DesignLength;
        float extra = LevelController.ExtraFor(levelLength, design);
        SetExtra(extra);

        float floor = c.FloorY;
        float ceiling = floor + design + extra;
        if (backgroundSprite != null)
        {
            if (c.background != null) c.background.SetSprite(backgroundSprite);
            if (c.surround != null) c.surround.SetSprite(backgroundSprite);
        }
        if (c.background != null) c.background.Fit(floor, ceiling);
        // Sized for the Game view's camera, the same one CameraAspectFit keeps
        // fitted in Edit mode, so the surround previews what the device shows.
        var cam = Camera.main;
        if (c.surround != null && cam != null) c.surround.Cover(c.SurroundRect(floor, ceiling, cam));
    }

    // Moves the pulleys from wherever they are now to where a level of the
    // requested extra puts them — by the difference, so it's correct whatever
    // was applied before.
    void SetExtra(float extra)
    {
        var c = Controller;
        float delta = extra - appliedExtra;
        if (Mathf.Abs(delta) < 1e-4f) return;

#if UNITY_EDITOR
        Undo.RecordObject(this, "Level length preview");
        if (c.pulleys != null) Undo.RecordObject(c.pulleys, "Level length preview");
#endif
        if (c.pulleys != null) c.pulleys.position += new Vector3(0f, delta, 0f);
        appliedExtra = extra;
    }

    void OnDrawGizmos()
    {
        var c = Controller;
        if (c == null || c.profile == null) return;

        float design = c.DesignLength;
        float extra = LevelController.ExtraFor(levelLength, design);
        float floor = c.FloorY;
        float ceiling = floor + design + extra;
        float width = c.profile.designWidth;
        float x = transform.position.x;
        float halfW = width * 0.5f;

        // The level box.
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(x, (floor + ceiling) * 0.5f, 0f), new Vector3(width, ceiling - floor, 0f));

        // Where the authored screenful ends — above this is the Tall surplus.
        if (extra > 0f)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            Gizmos.DrawLine(new Vector3(x - halfW, floor + design, 0f), new Vector3(x + halfW, floor + design, 0f));
        }

        // Highest the stick's centre can reach.
        if (c.stick != null)
        {
            // In Play mode LevelController has already folded extra into maxOffset.
            float summit = c.stick.transform.position.y + c.stick.maxOffset + (Application.isPlaying ? 0f : extra);
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
            Gizmos.DrawLine(new Vector3(x - halfW, summit, 0f), new Vector3(x + halfW, summit, 0f));
#if UNITY_EDITOR
            Handles.Label(new Vector3(x + halfW + 0.15f, summit, 0f), "stick summit " + summit.ToString("F2"));
#endif
        }

#if UNITY_EDITOR
        Handles.Label(new Vector3(x + halfW + 0.15f, ceiling, 0f), "ceiling " + ceiling.ToString("F2"));
        Handles.Label(new Vector3(x + halfW + 0.15f, floor, 0f), "floor " + floor.ToString("F2"));
        Handles.Label(new Vector3(x - halfW, ceiling + 0.6f, 0f),
            levelName + "  —  length " + (design + extra).ToString("F2") + (extra > 0f ? "  (Tall, +" + extra.ToString("F2") + ")" : "  (Classic)"));
#endif
    }
}
