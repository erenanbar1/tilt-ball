using UnityEngine;

// Keeps the pulleys out from under the HUD's top bar.
//
// The pulleys sit near the level's ceiling, and the top bar hangs down over
// that same strip of the screen. How far it hangs differs per device: its
// bottom edge is a fixed distance below the safe area, so a phone with a deep
// cutout has a taller bar, and on such a phone the authored pulleys end up
// behind it. This measures the bar every frame and lowers the pulleys by
// exactly the overlap — none on a device where they already clear it — so
// the wheels the ropes run over are always in view.
//
// The comparison is made at the summit of the climb: the camera is clamped to
// the ceiling there, which is the one place the pulleys and the bar meet, and
// it doesn't depend on where the camera is right now. That makes the result a
// fixed world position per device rather than something that chases the camera.
//
// Runtime only, on purpose. LevelController and LevelDesignPreview both move
// the pulleys by a delta; a third writer running in Edit mode would leave the
// scene with a device-specific offset baked in. The rest position is taken in
// Start, after LevelController's Awake has lifted the pulleys for a tall level.
//
// Runs after TopBarFit's LateUpdate so the height read is this frame's.
[DefaultExecutionOrder(50)]
public class PulleysBelowTopBar : MonoBehaviour
{
    [Tooltip("The HUD's top bar. Its height in world units is what the pulleys are kept below.")]
    public RectTransform topBar;

    [Tooltip("The gameplay camera's climb follow — tells where the view's top edge is at the summit.")]
    public CameraClimbFollow cameraFollow;

    [Tooltip("Gap kept between the bar's bottom edge and the highest pulley, in world units.")]
    [Min(0f)]
    public float clearance = 0.15f;

    float restY;
    float topAboveRoot;
    float appliedPush = -1f;

    void Start()
    {
        restY = transform.position.y;
        topAboveRoot = 0f;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            topAboveRoot = Mathf.Max(topAboveRoot, sr.bounds.max.y - restY);
        }
        appliedPush = -1f;
        Apply();
    }

    void LateUpdate() { Apply(); }

    void Apply()
    {
        if (topBar == null || cameraFollow == null) return;

        // The bar is pinned to the top of a Screen Space - Camera canvas on an
        // orthographic camera, so its height in canvas units times the canvas
        // scale is its height in world units, and its top edge is the view's.
        float barHeight = topBar.rect.height * topBar.lossyScale.y;
        float barBottom = cameraFollow.TopmostViewTop() - barHeight;

        float push = Mathf.Max(0f, restY + topAboveRoot + clearance - barBottom);
        if (Mathf.Approximately(push, appliedPush)) return;
        appliedPush = push;

        Vector3 pos = transform.position;
        pos.y = restY - push;
        transform.position = pos;
    }
}
