using UnityEngine;

// Constrains the Ball to exactly one degree of freedom along the Platform,
// reproducing the reference HTML prototype's motion 1:1:
//     v += slope * rollAcceleration * platformLength * dt
//     v *= 0.5^(dt / dampingHalfLife)
//     s += v * dt
// with end stops at ballRadius * endStopFactor that reverse half the speed.
// Unity's physics solver never controls the Ball/Platform contact, so bouncing,
// jitter and launch-on-sudden-movement are not physically possible.
//
// [ExecuteAlways]: the Ball is locked onto the Platform surface in Edit mode too,
// so dragging it in the Scene view has no lasting effect. Its position along the
// Platform is set through distanceAlongPlatform, not by moving the Ball. The
// Ball's and Platform's sprites stay fully independent of this constraint.
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallOnPlatformController : MonoBehaviour
{
    [Header("Platform references")]
    public Transform platform;
    public Transform leftPoint;
    public Transform rightPoint;

    [Header("Tuning — mirrors the HTML prototype")]
    // HTML: v += slope * 1700 * 0.7 * dt on a ~316px bar, i.e. 3.77 bar-lengths/s².
    // Expressed per platform length so the feel is identical at any world scale.
    public float rollAcceleration = 3.77f;
    public float dampingHalfLife = 0.5f;  // HTML: v *= 0.5^(dt/0.5)
    public float endStopFactor = 1.15f;   // HTML: notch = ball.r * 1.15
    public float endStopBounce = 0.5f;    // HTML: v *= -0.5 at an end stop
    public float sinkFactor = 0.88f;      // HTML: ball centre sits r * 0.88 off the bar line

    [Header("Gameplay state")]
    public float distanceAlongPlatform = -1f; // < 0 = start at the platform's midpoint
    public float velocityAlongPlatform;

    private Rigidbody2D rb;
    private CircleCollider2D ballCollider;
    private float ballRadius;
    private float surfaceOffset;   // perpendicular distance from the LeftPoint-RightPoint line to the ball's centre
    private float endStopDistance;
    private float previousDistanceAlongPlatform;
    private bool calibrated;

    void Awake()
    {
        Calibrate();
    }

    void Calibrate()
    {
        rb = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<CircleCollider2D>();
        ballRadius = ballCollider.radius * transform.lossyScale.x;
        endStopDistance = ballRadius * endStopFactor;
        surfaceOffset = SurfaceOffsetFromPointLine();

        if (Application.isPlaying)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            // The Platform's own collider is never used for contact — this script
            // owns that relationship entirely, so the engine can't fight it.
            if (platform != null)
            {
                var platformCollider = platform.GetComponent<Collider2D>();
                if (platformCollider != null) Physics2D.IgnoreCollision(ballCollider, platformCollider, true);
            }
        }

        calibrated = true;
    }

    // LeftPoint/RightPoint sit slightly above the Platform's top face, so the
    // ball's resting height is measured from the Platform's own collider and then
    // expressed relative to that line.
    float SurfaceOffsetFromPointLine()
    {
        float sink = ballRadius * sinkFactor;
        if (platform == null || leftPoint == null) return sink;

        var platformCollider = platform.GetComponent<BoxCollider2D>();
        if (platformCollider == null) return sink;

        float platformTop = (platformCollider.offset.y + platformCollider.size.y * 0.5f) * platform.lossyScale.y;
        float pointLine = Vector3.Dot(leftPoint.position - platform.position, platform.up);
        return (platformTop - pointLine) + sink;
    }

    // HTML starts the ball at the middle of the bar.
    void SeedIfNeeded(float platformLength)
    {
        if (distanceAlongPlatform >= 0f) return;
        distanceAlongPlatform = platformLength * 0.5f;
        previousDistanceAlongPlatform = distanceAlongPlatform;
        velocityAlongPlatform = 0f;
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;
        if (!calibrated) Calibrate();
        if (!ballCollider.enabled) return; // the Hole has taken over — don't fight it
        if (leftPoint == null || rightPoint == null || platform == null) return;

        Vector2 leftPos = leftPoint.position;
        Vector2 rightPos = rightPoint.position;
        Vector2 platformVector = rightPos - leftPos;
        float platformLength = platformVector.magnitude;
        if (platformLength < 0.0001f) return;

        SeedIfNeeded(platformLength);

        // sin(tilt): positive when the right end is the lower one, so the ball
        // accelerates toward it. Same sign convention as the HTML prototype.
        float slope = -platformVector.y / platformLength;
        velocityAlongPlatform += slope * rollAcceleration * platformLength * Time.fixedDeltaTime;
        velocityAlongPlatform *= Mathf.Pow(0.5f, Time.fixedDeltaTime / dampingHalfLife);
        distanceAlongPlatform += velocityAlongPlatform * Time.fixedDeltaTime;

        float minDist = endStopDistance;
        float maxDist = Mathf.Max(minDist, platformLength - endStopDistance);
        if (distanceAlongPlatform <= minDist)
        {
            distanceAlongPlatform = minDist;
            if (velocityAlongPlatform < 0f) velocityAlongPlatform *= -endStopBounce;
        }
        else if (distanceAlongPlatform >= maxDist)
        {
            distanceAlongPlatform = maxDist;
            if (velocityAlongPlatform > 0f) velocityAlongPlatform *= -endStopBounce;
        }
    }

    void LateUpdate()
    {
        if (!calibrated) Calibrate();
        if (leftPoint == null || rightPoint == null || platform == null) return;
        if (Application.isPlaying && !ballCollider.enabled) return; // the Hole owns the Ball now

        Vector2 leftPos = leftPoint.position;
        Vector2 rightPos = rightPoint.position;
        Vector2 platformVector = rightPos - leftPos;
        float platformLength = platformVector.magnitude;
        if (platformLength < 0.0001f) return;
        Vector2 tangent = platformVector / platformLength;

        SeedIfNeeded(platformLength);
        float minDist = endStopDistance;
        float maxDist = Mathf.Max(minDist, platformLength - endStopDistance);
        float clamped = Mathf.Clamp(distanceAlongPlatform, minDist, maxDist);

        Vector2 surfacePoint = leftPos + tangent * clamped;

        // Perpendicular to the platform line, disambiguated toward the Ball's side.
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        if (Vector2.Dot(normal, platform.up) < 0f) normal = -normal;

        Vector2 ballPosition = surfacePoint + normal * surfaceOffset;
        transform.position = ballPosition;
        if (Application.isPlaying) rb.position = ballPosition;

        // Visual-only roll: rotationRadians = deltaDistance / ballRadius (HTML).
        if (Application.isPlaying)
        {
            float deltaDistance = clamped - previousDistanceAlongPlatform;
            transform.Rotate(0f, 0f, -(deltaDistance / ballRadius) * Mathf.Rad2Deg);
        }
        previousDistanceAlongPlatform = clamped;
    }
}
