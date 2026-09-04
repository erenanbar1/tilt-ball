using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class StickController : MonoBehaviour
{
    [Header("Input state (set by buttons)")]
    public bool leftHeld;
    public bool rightHeld;

    // Tuning mirrors the HTML prototype's stick, matched on the quantities that
    // are independent of board size:
    //   full travel rises in 0.88/0.55 = 1.60s and falls in 0.88/0.25 = 3.52s
    //   tilting (one end held) turns at 0.8 * 1.6 rad/s = 73.34 deg/s
    //   tilt saturates at 0.9 rad = 51.6 deg
    // so riseSpeed = travel/1.60, fallSpeed = travel/3.52 and
    // angleGain = 73.34 / (riseSpeed + fallSpeed).
    [Header("Tuning")]
    public float stickHalfWidth = 2f;   // distance from center to each end (X)
    public float riseSpeed = 6.5625f;   // units/sec when rising (10.5 units of travel in 1.60s)
    public float fallSpeed = 2.983f;    // units/sec when falling (10.5 units of travel in 3.52s)
    public float maxOffset = 10.5f;     // highest an end can go
    public float minOffset = 0f;        // lowest an end can go — also the spawn height
    public float angleGain = 7.682f;    // degrees of tilt per world-unit of height difference between ends
    public float maxTiltAngle = 51.6f;  // steepest the stick may tilt from horizontal, in degrees

    [Header("Win State")]
    public bool inputEnabled = true;

    private Rigidbody2D rb;
    private float baseY;   // spawn height the offsets are measured from
    private float leftY;   // current offset of left end
    private float rightY;  // current offset of right end

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Start at the bottom of the travel range, exactly where the stick was
        // placed in the scene — like the HTML prototype, which begins each level
        // with both ends already at their floor value. Deriving baseY from
        // minOffset means the stick no longer sags downward on the first frame.
        baseY = rb.position.y - minOffset;
        leftY = minOffset;
        rightY = minOffset;
    }

    void Update()
    {
        if (!inputEnabled) return;

        // Keyboard input for development (project uses the new Input System).
        var kb = Keyboard.current;
        if (kb != null)
        {
            leftHeld = kb.leftArrowKey.isPressed;
            rightHeld = kb.rightArrowKey.isPressed;
        }
    }

    void FixedUpdate()
    {
        if (!inputEnabled) return;

        leftY = MoveEnd(leftY, leftHeld);
        rightY = MoveEnd(rightY, rightHeld);

        // Cap the stored height difference itself (not just the displayed angle) at what
        // maxTiltAngle allows, redistributing any excess symmetrically so the center height
        // is unaffected. Without this, leftY/rightY could keep drifting apart well past the
        // point the angle visually saturates, and reversing input would first have to "unwind"
        // that hidden excess — seen as the stick briefly rising before tilting the other way.
        float maxDiff = maxTiltAngle / angleGain;
        float diff = rightY - leftY;
        if (diff > maxDiff)
        {
            float excess = (diff - maxDiff) * 0.5f;
            rightY -= excess;
            leftY += excess;
            diff = maxDiff;
        }
        else if (diff < -maxDiff)
        {
            float excess = (diff + maxDiff) * 0.5f;
            rightY -= excess;
            leftY += excess;
            diff = -maxDiff;
        }

        float centerOffset = (leftY + rightY) * 0.5f;
        // Linear in the height difference (not atan2) so the turn rate stays constant
        // all the way to vertical instead of slowing down as it approaches 90°.
        float angleDeg = diff * angleGain;

        float newY = baseY + centerOffset;
        Vector2 newPos = new Vector2(rb.position.x, newY);
        rb.MovePosition(newPos);
        rb.MoveRotation(angleDeg);
    }

    float MoveEnd(float current, bool held)
    {
        float speed = held ? riseSpeed : -fallSpeed;
        float next = current + speed * Time.fixedDeltaTime;
        return Mathf.Clamp(next, minOffset, maxOffset);
    }

    // Called by the UI buttons
    public void SetLeftHeld(bool value) => leftHeld = value;
    public void SetRightHeld(bool value) => rightHeld = value;
}
