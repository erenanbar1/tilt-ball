using UnityEngine;

// iOS caps the frame rate at 30fps unless targetFrameRate is set explicitly, so
// without this the game renders at half speed on device no matter how light the
// scene is. The physics step is pinned to the same 60Hz as the display so one
// simulation step lands per rendered frame, which keeps the ball's motion smooth
// instead of beating against a mismatched 50Hz physics rate.
public static class PerformanceSettings
{
    public const int TargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        Application.targetFrameRate = TargetFrameRate;
        QualitySettings.vSyncCount = 0;
        Time.fixedDeltaTime = 1f / TargetFrameRate;
    }
}
