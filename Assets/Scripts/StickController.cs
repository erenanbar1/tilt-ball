using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class StickController : MonoBehaviour
{
    [Header("Input state (set by buttons)")]
    public bool leftHeld;
    public bool rightHeld;

    // Held state coming from the on-screen controls, kept separate from the
    // keyboard's so the two can be OR'd together in Update. Without this the
    // keyboard read would overwrite a touch hold on the very next frame.
    [HideInInspector] public bool touchLeftHeld;
    [HideInInspector] public bool touchRightHeld;

    // Tuning mirrors the HTML prototype's stick. The speeds are matched to the
    // prototype's actual velocity relative to the bar's own length, not to the
    // time it takes to cross the board — this playfield is much taller relative
    // to the bar than the prototype's, so matching the crossing time would have
    // made the stick climb ~1.8x too fast.
    //   HTML rise = 0.55 * (boardHeight / barLength) = 1.088 bar-lengths/sec
    //   HTML fall = 0.25 * (boardHeight / barLength) = 0.495 bar-lengths/sec
    //     (boardHeight/barLength = 1.982 at an iPhone portrait aspect)
    //   tilting with both ends free turns at 0.8 * 1.6 rad/s = 73.34 deg/s,
    //     so angleGain = 73.34 / (riseSpeed + fallSpeed)
    //   tilt saturates at 0.9 rad = 51.6 deg
    [Header("Tuning")]
    public float stickHalfWidth = 2f;   // distance from center to each end (X)
    public float riseSpeed = 3.570f;    // units/sec when rising (1.088 bar-lengths/sec on a 3.276 bar)
    public float fallSpeed = 1.623f;    // units/sec when falling (0.495 bar-lengths/sec on a 3.276 bar)
    public float maxOffset = 10.5f;     // highest an end can go
    public float minOffset = 0f;        // lowest an end can go — also the spawn height
    public float angleGain = 14.122f;   // degrees of tilt per world-unit of height difference between ends
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
        bool keyLeft = kb != null && kb.leftArrowKey.isPressed;
        bool keyRight = kb != null && kb.rightArrowKey.isPressed;

        leftHeld = keyLeft || touchLeftHeld;
        rightHeld = keyRight || touchRightHeld;
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

    // Called by the on-screen controls
    public void SetLeftHeld(bool value) => touchLeftHeld = value;
    public void SetRightHeld(bool value) => touchRightHeld = value;
}
