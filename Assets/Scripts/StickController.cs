using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class StickController : MonoBehaviour
{
    [Header("Input state (set by buttons)")]
    public bool leftHeld;
    public bool rightHeld;

    [Header("Tuning")]
    public float stickHalfWidth = 2f;   // distance from center to each end (X)
    public float riseSpeed = 3f;        // units/sec when rising
    public float fallSpeed = 2f;        // units/sec when falling
    public float maxOffset = 1.5f;      // highest an end can go
    public float minOffset = -1.5f;     // lowest an end can go

    private Rigidbody2D rb;
    private float leftY;   // current offset of left end
    private float rightY;  // current offset of right end

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
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
        leftY = MoveEnd(leftY, leftHeld);
        rightY = MoveEnd(rightY, rightHeld);

        float centerY = (leftY + rightY) * 0.5f;
        float angleRad = Mathf.Atan2(rightY - leftY, stickHalfWidth * 2f);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        Vector2 newPos = new Vector2(rb.position.x, centerY);
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
