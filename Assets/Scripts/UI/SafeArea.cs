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
