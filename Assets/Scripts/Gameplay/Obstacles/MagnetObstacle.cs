using PrimeTween;
using UnityEngine;

// Pulls the Ball along the stick toward the magnet's position while the Ball is
// within the trigger's radius. The pull is strongest at the centre and fades
// toward the edge, so entering the field feels like a gradual drag rather than
// a wall. On its own it only nudges — the danger is what it drags you into.
[RequireComponent(typeof(CircleCollider2D))]
public class MagnetObstacle : MonoBehaviour
{
    [Header("Pull")]
    public float strength = 9f;      // units/s² at the centre
    [Range(0f, 1f)]
    public float minFalloff = 0.35f; // fraction of strength still applied at the very edge
    public string ballTag = "Ball";

    [Header("Visuals")]
    public Transform fieldRing;      // pulses to show the reach
    public float ringPulse = 0.08f;
    public float ringPeriod = 1.2f;

    private CircleCollider2D field;

    void Awake()
    {
        field = GetComponent<CircleCollider2D>();
        field.isTrigger = true;
    }

    void Start()
    {
        if (fieldRing != null)
        {
            Vector3 s = fieldRing.localScale;
            Tween.Scale(fieldRing, s, s * (1f + ringPulse), ringPeriod, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;
        var ball = other.GetComponent<BallOnPlatformController>();
        if (ball == null) return;

        float radius = field.radius * transform.lossyScale.x;
        float distance = Vector2.Distance(other.bounds.center, transform.position);
        float falloff = Mathf.Lerp(1f, minFalloff, Mathf.Clamp01(distance / Mathf.Max(radius, 0.001f)));

        float side = ball.SignedOffsetAlong(transform.position);
        if (Mathf.Abs(side) < 0.02f) return;
        ball.AddAcceleration(Mathf.Sign(side) * strength * falloff);
    }
}
