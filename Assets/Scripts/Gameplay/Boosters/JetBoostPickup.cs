using UnityEngine;

// Booster: overdrives the rig's motor for a few seconds. Both ends rise much
// faster (and fall a touch faster too, so tilting stays snappy), which is a way
// to dash through a laser window or rush a gusting fan — or, misused, to fling
// yourself into a hole twice as fast.
public class JetBoostPickup : Pickup
{
    [Header("Boost")]
    public float duration = 3f;
    public float riseMultiplier = 2f;
    public float fallMultiplier = 1.3f;
    public ParticleSystem trailPrefab;   // parented under the Ball for the duration
    public Color pulleyTint = new Color(1f, 0.85f, 0.2f, 1f);

    protected override void OnCollected(BallOnPlatformController ball)
    {
        var stick = ball.platform != null ? ball.platform.GetComponent<StickController>() : null;
        if (stick == null) stick = FindFirstObjectByType<StickController>();
        if (stick == null) return;

        var boost = stick.GetComponent<StickBoost>();
        if (boost == null) boost = stick.gameObject.AddComponent<StickBoost>();
        boost.Apply(riseMultiplier, fallMultiplier, duration, trailPrefab, ball.transform, pulleyTint);
    }
}
