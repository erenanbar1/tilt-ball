using UnityEngine;

// Insets this RectTransform to the device's safe area (notches, punch-hole
// cameras, rounded corners, the home indicator) — every child anchored to its
// edges (a top bar, a pause button, bottom touch controls) gets pushed clear
// of those cutouts for free, without each one needing its own margin logic.
//
// Composes with CameraAspectFit rather than duplicating it: Screen.safeArea is
// reported in raw device pixels against the FULL screen, but on a pillarboxed
// wide device the Canvas (Screen Space - Camera) only occupies targetCamera's
// own Rect, not the full screen. So the safe area is first intersected with
// the camera's rect, then remapped into that rect's own [0,1] space before
// being applied as anchors — on a device where the letterbox bars already
// clear the cutout, that intersection is just the full camera rect and this
// is a no-op; on a full-bleed phone it reduces to the ordinary safe-area inset.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    // Defaults to the parent Canvas's own render camera — which is exactly
    // the camera CameraAspectFit manages — so this needs no manual wiring in
    // the common case.
    public Camera targetCamera;

    RectTransform rt;
    Rect lastSafeArea;
    Rect lastCamRect;
    int lastScreenWidth;
    int lastScreenHeight;
    bool hasLast;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        hasLast = false;
        Apply();
    }

    void Update()
    {
        Apply();
    }

    void Apply()
    {
        if (rt == null) return;
        if (targetCamera == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) targetCamera = canvas.worldCamera;
        }
        if (targetCamera == null || Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;
        Rect camRect = targetCamera.rect;

        // Screen.safeArea is only meaningful when it is measured in the same
        // pixels as Screen.width/height. On a device it always is. In the editor
        // it often isn't: the Game view reports its own window size while the
        // safe area comes back in the simulated handset's resolution, and
        // dividing one by the other produces an inset out of nowhere — a
        // 668x788 window against a 1080x1920 safe area once pinned the HUD into
        // the left third of the screen, and because this component runs in edit
        // mode that bogus anchor was written into the scene and stayed there.
        // A safe area that doesn't fit inside the screen it is supposedly part of
        // is not a safe area, so it is ignored rather than acted on.
        if (safeArea.width <= 0f || safeArea.height <= 0f
            || safeArea.xMax > Screen.width + 1f || safeArea.yMax > Screen.height + 1f
            || safeArea.xMin < -1f || safeArea.yMin < -1f)
        {
            safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        }

        if (hasLast && safeArea == lastSafeArea && camRect == lastCamRect
            && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }
        hasLast = true;
        lastSafeArea = safeArea;
        lastCamRect = camRect;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // Safe area in normalized full-screen fractions (bottom-left origin,
        // matching Camera.rect's own convention).
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 safeMin = safeArea.min / screenSize;
        Vector2 safeMax = safeArea.max / screenSize;

        // Remap into the camera's own [0,1] viewport space, clamped: outside
        // the camera's rect (the letterbox bars) doesn't exist as far as the
        // Canvas is concerned.
        Vector2 camMin = camRect.min;
        Vector2 camSize = camRect.size;
        if (camSize.x <= 0f || camSize.y <= 0f) return;

        Vector2 anchorMin = (safeMin - camMin) / camSize;
        Vector2 anchorMax = (safeMax - camMin) / camSize;

        anchorMin = Vector2.Max(Vector2.zero, Vector2.Min(anchorMin, Vector2.one));
        anchorMax = Vector2.Max(Vector2.zero, Vector2.Min(anchorMax, Vector2.one));

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
