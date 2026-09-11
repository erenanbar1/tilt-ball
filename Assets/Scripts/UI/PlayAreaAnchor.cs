using UnityEngine;

// Narrows this RectTransform horizontally to the level's play area — the
// profile's design width, centred on the camera — so HUD pieces that belong
// to the level (the top bar) track its edges instead of the screen's when a
// wide device shows surplus space either side. Only the X anchors are
// written; Y is left to whatever the layout authored, so a bar pinned to the
// top of the screen stays there. On a device at or narrower than the design
// aspect the play area spans the full width and this is a no-op.
//
// Composes with SafeArea above it: this expects to sit inside a full-width
// rect (portrait cutouts only inset the top/bottom), so viewport fractions
// map straight onto anchors.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class PlayAreaAnchor : MonoBehaviour
{
    public ScreenFitProfile profile;

    RectTransform rt;
    Camera cam;
    float lastLeft = -1f;
    float lastRight = -1f;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        lastLeft = lastRight = -1f;
        Apply();
    }

    void Update()
    {
        Apply();
    }

    void Apply()
    {
        if (rt == null || profile == null) return;
        if (cam == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            cam = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (cam == null) return;
        }

        float viewWidth = cam.orthographicSize * 2f * cam.aspect;
        if (viewWidth <= 0f) return;

        float halfFraction = Mathf.Min(0.5f, profile.designWidth / viewWidth * 0.5f);
        float left = 0.5f - halfFraction;
        float right = 0.5f + halfFraction;
        if (left == lastLeft && right == lastRight) return;
        lastLeft = left;
        lastRight = right;

        rt.anchorMin = new Vector2(left, rt.anchorMin.y);
        rt.anchorMax = new Vector2(right, rt.anchorMax.y);
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
    }
}
