using UnityEngine;

// Fits this camera's world content to the screen by holding the level's
// vertical LENGTH constant whenever possible, and only falling back to
// holding WIDTH constant when the device is narrower than the design allows
// (so nothing horizontal is ever cropped). Whichever dimension isn't pinned
// grows beyond the design size — that surplus shows the camera's clear colour
// around the level's own background (see LevelBackground), not black bars.
//
//  - Device aspect > profile designAspect (wide/square screens): FIT TO
//    LENGTH — orthographicSize is pinned to profile.designLength / 2, so the
//    full level length is always visible. Extra width beyond
//    profile.designWidth appears on the sides.
//  - Device aspect <= profile designAspect (narrow/tall phones): FIT TO
//    WIDTH — orthographicSize grows just enough to keep profile.designWidth
//    fully visible. Extra height beyond profile.designLength appears
//    above/below.
//
// The camera viewport always fills the full screen (rect = 0,0,1,1) — there
// is no pillarbox/letterbox backdrop in this system.
//
// Changing orthographicSize only changes how much of the world the camera
// shows — it never rescales any Transform, so Rigidbody2D physics (gravity,
// the stick's tuned speeds, rope/joint distances) stay exactly as authored
// regardless of which branch is active.
//
// The Canvas showing gameplay UI is expected to be Screen Space - Camera,
// assigned to this same camera, so UI and world share this exact rect.
//
// [ExecuteAlways] so the Scene view / Device Simulator preview the same fit
// in Edit mode as at runtime; the camera's authored Size is just a placeholder.
[ExecuteAlways]
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Camera))]
public class CameraAspectFit : MonoBehaviour
{
    public ScreenFitProfile profile;

    Camera cam;
    int lastScreenWidth;
    int lastScreenHeight;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        lastScreenWidth = lastScreenHeight = 0;
        Apply();
    }

    void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        Apply();
    }

    // Screen size can change without a scene reload — Editor window resizing,
    // Android split-screen/multi-window, a foldable's fold state — so this is
    // checked every frame rather than only once. The comparison is cheap; the
    // actual fit math only runs on an actual change.
    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight) Apply();
    }

    void Apply()
    {
        if (cam == null || profile == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        if (lastScreenWidth <= 0 || lastScreenHeight <= 0) return;

        float currentAspect = (float)lastScreenWidth / lastScreenHeight;
        float targetAspect = profile.designWidth / profile.designLength;

        cam.rect = new Rect(0f, 0f, 1f, 1f);

        cam.orthographicSize = currentAspect > targetAspect
            ? profile.designLength * 0.5f
            : (profile.designWidth * 0.5f) / currentAspect;
    }
}
