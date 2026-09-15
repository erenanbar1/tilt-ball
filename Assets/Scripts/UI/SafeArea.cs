using UnityEngine;

// Insets this RectTransform to the device's safe area (notches, punch-hole
// cameras, rounded corners, the home indicator) — every child anchored to its
// edges (a top bar, a pause button, bottom touch controls) gets pushed clear
// of those cutouts for free, without each one needing its own margin logic.
//
// The gameplay camera always fills the full screen (no pillarboxing), so
// Screen.safeArea can be applied directly against the full screen size.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafeArea;
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
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;

        // Screen.safeArea only means something when it is measured in the same
        // pixels as Screen.width/height. On a device it always is. In the editor it
        // often isn't: the Game view reports its own panel size while the safe area
        // comes back in the simulated handset's resolution, and dividing one by the
        // other invents an inset out of nowhere — a 1179x2556 safe area against a
        // 1546x1062 panel once put this rect's top anchor at 36, thirty-six times
        // the height of the screen. Because this component also runs in edit mode,
        // that invented inset gets written into the scene and saved there. A safe
        // area that does not fit inside the screen it belongs to is not a safe
        // area, so it is ignored rather than acted on.
        if (safeArea.width <= 0f || safeArea.height <= 0f
            || safeArea.xMax > Screen.width + 1f || safeArea.yMax > Screen.height + 1f
            || safeArea.xMin < -1f || safeArea.yMin < -1f)
        {
            safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        }

        if (hasLast && safeArea == lastSafeArea
            && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }
        hasLast = true;
        lastSafeArea = safeArea;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 anchorMin = safeArea.min / screenSize;
        Vector2 anchorMax = safeArea.max / screenSize;

        // Belt and braces after the guard above: an anchor outside the parent is
        // never what a safe area means, and clamping keeps one bad frame from
        // being the thing that gets saved.
        anchorMin = Vector2.Max(Vector2.zero, Vector2.Min(anchorMin, Vector2.one));
        anchorMax = Vector2.Max(Vector2.zero, Vector2.Min(anchorMax, Vector2.one));

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
