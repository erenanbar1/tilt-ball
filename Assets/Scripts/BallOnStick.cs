using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallOnStick : MonoBehaviour
{
    [Header("Attachment")]
    public Transform stick;

    [Header("Tuning")]
    public float ballRadius = 0.2f;        // how close to each stick end the ball's center can get
    public float rollFactor = 0.7f;        // solid-sphere rolling reduces linear accel: a = g*sin/(1+2/5)
    public float frictionHalfLife = 0.4f;  // seconds for rolling speed to halve (frame-rate independent)
    public float endBounce = 0.35f;        // fraction of speed kept (reversed) when hitting a stick end

    private Rigidbody2D rb;
    private float localT;         // position along the stick's local X axis, relative to its center
    private float surfaceOffset;  // vertical gap above the stick's centerline, auto-calibrated in Awake
    private float rollVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector2 stickPos = stick.position;
        Vector2 ballPos = transform.position;
        localT = ballPos.x - stickPos.x;
        surfaceOffset = ballPos.y - stickPos.y;
    }

    // Called by StickController.FixedUpdate right after it computes the stick's new pose.
    public void FollowStick(Vector2 stickCenter, float stickAngleDeg, float stickHalfWidth)
    {
        float angleRad = stickAngleDeg * Mathf.Deg2Rad;
        Vector2 right = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        Vector2 up = new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));

        float accel = Vector2.Dot(Physics2D.gravity, right) * rollFactor;
        rollVelocity += accel * Time.fixedDeltaTime;
        rollVelocity *= Mathf.Pow(0.5f, Time.fixedDeltaTime / frictionHalfLife);

        localT += rollVelocity * Time.fixedDeltaTime;
        float maxT = stickHalfWidth - ballRadius;
        if (localT > maxT)
        {
            localT = maxT;
            rollVelocity *= -endBounce;
        }
        else if (localT < -maxT)
        {
            localT = -maxT;
            rollVelocity *= -endBounce;
        }

        Vector2 newPos = stickCenter + right * localT + up * surfaceOffset;
        rb.MovePosition(newPos);
        rb.MoveRotation(stickAngleDeg - (localT / ballRadius) * Mathf.Rad2Deg);
    }
}
