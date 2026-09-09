using UnityEngine;

// Fits this camera's world content into the screen the way a fixed-design
// mobile layout is meant to: the visible world WIDTH is held constant on every
// device — designOrthographicSize * profile.designAspect, captured from
// whatever this scene's camera was authored with — so nothing at the design's
// horizontal edges (the stick, the pulleys) can ever be cropped, on any aspect
// ratio. Height is what's allowed to vary between devices, never width.
//
//  - Device aspect >= profile.designAspect (tablets, squarer screens):
//    orthographicSize is pinned back to its authored value and the viewport
//    WIDTH is capped to match the design aspect at full height, centered —
//    black bars appear on the sides.
//  - Device aspect <  profile.designAspect (ordinary tall phones, and very
//    elongated ones like a folding phone's cover screen): the viewport is
//    left full-screen, and orthographicSize is grown just enough
//    (designHalfWidth / deviceAspect) to keep the same world width visible at
//    that full-screen aspect — the extra room lands as headroom above/below,
//    never as a horizontal crop.
//
// Changing orthographicSize only changes how much of the world the camera
// shows — it never rescales any Transform, so Rigidbody2D physics (gravity,
// the stick's tuned speeds, rope/joint distances) stay exactly as authored
// regardless of which branch is active.
//
// A second camera paints the bars black — a camera's own Clear Flags only
// clear its own viewport rect, so without one, whatever the GPU last drew
// would show through outside the game's rect. This component creates and
// owns that backdrop camera itself, so no scene needs to hand-author one and
// it can never drift out of sync with the main camera's rect.
//
// The Canvas showing gameplay UI is expected to be Screen Space - Camera,
// assigned to this same camera, so UI and world share this exact rect and can
// never separate from each other on any aspect ratio — see BackgroundFitter
// for the equivalent story on world-space background art.
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Camera))]
public class CameraAspectFit : MonoBehaviour
{
    public ScreenFitProfile profile;

    Camera cam;
    Camera backdropCam;
    float designOrthographicSize; // captured from whatever this scene's camera was authored with
    int lastScreenWidth;
    int lastScreenHeight;

    const string BackdropName = "LetterboxBackdrop";

    void Awake()
    {
        cam = GetComponent<Camera>();
        designOrthographicSize = cam.orthographicSize;
        CreateBackdrop();
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

    void CreateBackdrop()
    {
        var go = new GameObject(BackdropName);
        go.transform.SetParent(transform.parent, false);

        backdropCam = go.AddComponent<Camera>();
        backdropCam.clearFlags = CameraClearFlags.SolidColor;
        backdropCam.backgroundColor = Color.black;
        backdropCam.cullingMask = 0; // renders nothing of its own — a pure fill
        backdropCam.orthographic = true;
        backdropCam.rect = new Rect(0f, 0f, 1f, 1f);
        backdropCam.depth = cam.depth - 1; // draws first; the main camera composites on top
        backdropCam.useOcclusionCulling = false;
        backdropCam.allowHDR = false;
        backdropCam.allowMSAA = false;
    }

    void Apply()
    {
        if (cam == null || profile == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        if (lastScreenWidth <= 0 || lastScreenHeight <= 0) return;

        float deviceAspect = (float)lastScreenWidth / lastScreenHeight;

        if (deviceAspect >= profile.designAspect)
        {
            // Wide/square screen: never re-zoom — pillarbox the sides instead.
            cam.orthographicSize = designOrthographicSize;
            float w = profile.designAspect / deviceAspect;
            cam.rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }
        else
        {
            // Narrow/tall screen: fill edge-to-edge, but zoom out just enough
            // to keep the design's full width on screen — the width is never
            // allowed to shrink below what the design authored.
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            float designHalfWidth = designOrthographicSize * profile.designAspect;
            cam.orthographicSize = designHalfWidth / deviceAspect;
        }
    }
}
