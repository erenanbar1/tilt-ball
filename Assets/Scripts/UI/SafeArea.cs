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

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
