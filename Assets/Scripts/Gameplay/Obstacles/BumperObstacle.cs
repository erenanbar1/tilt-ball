using PrimeTween;
using UnityEngine;

// Pinball bumper. The moment the Ball touches it, the Ball is thrown along the
// stick *away* from the bumper's centre at kickSpeed — whichever side of the
// bumper the Ball is on decides the direction, so a bumper sitting dead in the
// middle of a path can't be rolled through, only around.
//
// Works on the Ball's along-platform velocity (BallOnPlatformController.SetVelocity)
// rather than on any physics contact: the Ball is kinematic and owns its own
// motion, so this is the only channel that actually moves it.
[RequireComponent(typeof(CircleCollider2D))]
public class BumperObstacle : MonoBehaviour
{
    [Header("Kick")]
    public float kickSpeed = 5f;
    // Minimum gap between two kicks so a Ball resting against the bumper isn't
    // machine-gunned into a jitter.
    public float cooldown = 0.25f;
    public string ballTag = "Ball";

    [Header("Feedback")]
    public Transform visual;          // squashes on hit; defaults to this transform
    public float squash = 0.78f;
    public float squashDuration = 0.22f;

    private float nextKickTime;
    private Vector3 restScale;
    private Tween squashTween;

    void Awake()
    {
        if (visual == null) visual = transform;
        restScale = visual.localScale;
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) => TryKick(other);
    void OnTriggerStay2D(Collider2D other) => TryKick(other);

    void TryKick(Collider2D other)
    {
        if (Time.time < nextKickTime || !other.CompareTag(ballTag)) return;
        var ball = other.GetComponent<BallOnPlatformController>();
        if (ball == null) return;

        float side = ball.SignedOffsetAlong(transform.position);
        // Dead centre: continue whichever way the Ball was already moving,
        // defaulting right.
        float away = Mathf.Abs(side) > 0.01f ? -Mathf.Sign(side)
                   : (Mathf.Abs(ball.VelocityAlongPlatform) > 0.01f ? Mathf.Sign(ball.VelocityAlongPlatform) : 1f);
        ball.SetVelocity(away * kickSpeed);
        nextKickTime = Time.time + cooldown;

        squashTween.Stop();
        visual.localScale = restScale;
        squashTween = Tween.Scale(visual, restScale * squash, restScale, squashDuration, Ease.OutBack);
    }
}
