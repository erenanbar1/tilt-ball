using PrimeTween;
using UnityEngine;

// The live state of a jet boost, parked on the stick by JetBoostPickup. Holds
// the stick's authored riseSpeed so it can be put back exactly, and treats a
// second pickup as extending the timer rather than stacking the multiplier —
// stacking would make the rig uncontrollable.
[RequireComponent(typeof(StickController))]
public class StickBoost : MonoBehaviour
{
    public bool IsActive => Time.time < endTime;
    public float Remaining => Mathf.Max(0f, endTime - Time.time);

    private StickController stick;
    private float baseRiseSpeed;
    private float baseFallSpeed;
    private float endTime = -1f;
    private ParticleSystem trail;
    private Tween pulleyPulse;
    private SpriteRenderer[] pulleys;
    private Color[] pulleyColors;

    void Awake()
    {
        stick = GetComponent<StickController>();
        baseRiseSpeed = stick.riseSpeed;
        baseFallSpeed = stick.fallSpeed;
    }

    public void Apply(float riseMultiplier, float fallMultiplier, float duration, ParticleSystem trailPrefab, Transform trailAnchor, Color tint)
    {
        bool wasActive = IsActive;
        endTime = Mathf.Max(endTime, Time.time) + duration;
        stick.riseSpeed = baseRiseSpeed * riseMultiplier;
        stick.fallSpeed = baseFallSpeed * fallMultiplier;

        if (!wasActive)
        {
            if (trailPrefab != null && trailAnchor != null)
            {
                // The trail prefab uses Local scaling and world-space velocity, so
                // riding under the (heavily scaled, rolling) Ball doesn't distort it.
                trail = Instantiate(trailPrefab, trailAnchor.position, Quaternion.identity, trailAnchor);
                trail.transform.localScale = Vector3.one;
                trail.Play();
            }
            TintPulleys(tint);
        }
    }

    void Update()
    {
        if (endTime < 0f || IsActive) return;
        endTime = -1f;
        stick.riseSpeed = baseRiseSpeed;
        stick.fallSpeed = baseFallSpeed;
        if (trail != null)
        {
            trail.Stop();
            Destroy(trail.gameObject, trail.main.startLifetime.constantMax + 0.2f);
            trail = null;
        }
        RestorePulleys();
    }

    // The pulleys are the rig's motor, so they are what glows while the motor is
    // overdriven.
    void TintPulleys(Color tint)
    {
        var rotators = FindObjectsByType<PulleyRotator>(FindObjectsSortMode.None);
        pulleys = new SpriteRenderer[rotators.Length];
        pulleyColors = new Color[rotators.Length];
        for (int i = 0; i < rotators.Length; i++)
        {
            pulleys[i] = rotators[i].GetComponent<SpriteRenderer>();
            if (pulleys[i] == null) continue;
            pulleyColors[i] = pulleys[i].color;
            Tween.Color(pulleys[i], tint, 0.5f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        }
    }

    void RestorePulleys()
    {
        if (pulleys == null) return;
        for (int i = 0; i < pulleys.Length; i++)
        {
            if (pulleys[i] == null) continue;
            Tween.StopAll(pulleys[i]);
            Tween.Color(pulleys[i], pulleyColors[i], 0.3f);
        }
        pulleys = null;
    }
}
