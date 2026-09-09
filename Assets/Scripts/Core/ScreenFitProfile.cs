using UnityEngine;

// Shared screen-shape target for CameraAspectFit. designAspect describes the
// physical screen the game was designed for (width / height) — the notch-class
// device this project targets — not any particular scene's content scale or
// zoom. Every scene's CameraAspectFit references the same asset so the
// letterbox behaves identically everywhere.
[CreateAssetMenu(fileName = "ScreenFitProfile", menuName = "Game/Screen Fit Profile")]
public class ScreenFitProfile : ScriptableObject
{
    // width / height of the design target. 1080x1920 — this project's
    // CanvasScaler reference resolution — is 0.5625.
    public float designAspect = 1080f / 1920f;
}
