using UnityEngine;

// Sizes the top bar so that the strip of it reaching below the safe area is the
// same on every device, and only the part covering the cutout above it changes.
//
// The bar is pinned across the top of the canvas and grows downward. Its bottom
// edge is placed a fixed distance below the safe area's top edge, and that strip
// is the bar's usable room — where a button or a label actually sits — so it must
// not move when the handset does. Everything above that edge is cosmetic cover
// for the notch and the status bar, and is exactly as tall as this particular
// device's cutout needs: on a phone with a deep cutout the bar reaches further
// up, on one with none it stops at the screen edge, and in both cases what the
// player sees below the safe area is identical.
//
// That is the whole reason this is a script rather than plain anchors. The bar's
// two edges answer to different things — the top to the screen, the bottom to the
// safe area — and a RectTransform can only anchor to one parent.
//
// LateUpdate rather than Update: SafeArea moves its own rect in Update, and this
// measures against the result.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class TopBarFit : MonoBehaviour
{
    [Tooltip("The rect SafeArea drives. The bar's bottom edge is placed below this rect's top edge. Empty means no cutout is assumed and the bar is exactly Drop Below Safe Area tall.")]
    public RectTransform safeArea;

    [Tooltip("How far the bar reaches below the safe area's top edge, in canvas units. This is the strip that stays identical on every device — leave enough room for whatever sits in the bar.")]
    [Min(0f)]
    public float dropBelowSafeArea = 220f;

    RectTransform rt;
    readonly Vector3[] corners = new Vector3[4];

    RectTransform Rect => rt != null ? rt : (rt = (RectTransform)transform);

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }
    void LateUpdate() { Apply(); }

    void Apply()
    {
        RectTransform self = Rect;
        RectTransform parent = self.parent as RectTransform;
        if (parent == null) return;

        PinAcrossTop(self);

        float height = TopInset(parent) + dropBelowSafeArea;
        var size = new Vector2(0f, height);
        if (self.sizeDelta != size) self.sizeDelta = size;
    }

    // How far the safe area's top edge sits below the parent's own top edge.
    // Measured off the SafeArea rect rather than off Screen.safeArea directly, so
    // there is one place that decides what the safe area is and this follows it.
    float TopInset(RectTransform parent)
    {
        if (safeArea == null) return 0f;
        safeArea.GetWorldCorners(corners);
        float safeTop = parent.InverseTransformPoint(corners[1]).y; // corners[1] is top-left
        return Mathf.Max(0f, parent.rect.yMax - safeTop);
    }

    // Full width, top edge on the parent's top edge, growing downward. Written
    // every frame rather than authored once so a stray drag in the Scene view
    // can't leave the bar in a shape it never recovers from. Each field is only
    // touched when it actually differs, so an untouched scene stays unmodified.
    static void PinAcrossTop(RectTransform t)
    {
        var anchorMin = new Vector2(0f, 1f);
        var anchorMax = new Vector2(1f, 1f);
        var pivot = new Vector2(0.5f, 1f);

        if (t.anchorMin != anchorMin) t.anchorMin = anchorMin;
        if (t.anchorMax != anchorMax) t.anchorMax = anchorMax;
        if (t.pivot != pivot) t.pivot = pivot;
        if (t.anchoredPosition != Vector2.zero) t.anchoredPosition = Vector2.zero;
        if (t.localScale != Vector3.one) t.localScale = Vector3.one;
        if (t.localRotation != Quaternion.identity) t.localRotation = Quaternion.identity;
        if (t.localPosition.z != 0f) t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, 0f);
    }
}
