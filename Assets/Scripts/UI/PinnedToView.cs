using UnityEngine;

// Pins this object to a fixed fraction of the camera's current view rather
// than a fixed world position, so it doesn't drift when CameraAspectFit grows
// the camera's orthographic size on a taller/narrower device to keep the
// design's width on screen. BackgroundFitter always stretches the background
// to exactly fill that same view, so mapping to a view fraction here is
// equivalent to pinning to a fixed spot on the background art itself.
//
// (0.5, 0.5) is dead centre; (0.5, 0) is bottom-centre — same convention as a
// UI anchor. Only X/Y move; Z (and sorting) stay whatever they were authored
// as.
[ExecuteAlways]
public class PinnedToView : MonoBehaviour
{
    public Camera targetCamera;

    [Range(0f, 1f)] public float normalizedX = 0.5f;
    [Range(0f, 1f)] public float normalizedY = 0.5f;

    void OnEnable() => Apply();
    void LateUpdate() => Apply();

    void Apply()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || !targetCamera.orthographic) return;

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        Vector3 camPos = targetCamera.transform.position;

        var pos = transform.position;
        pos.x = camPos.x + (normalizedX - 0.5f) * viewWidth;
        pos.y = camPos.y + (normalizedY - 0.5f) * viewHeight;
        transform.position = pos;
    }
}
