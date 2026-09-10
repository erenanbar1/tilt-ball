using UnityEngine;

// A slippery patch. While the Ball is inside, its roll acceleration is
// multiplied up and its damping half-life stretched out, so it picks up speed
// faster and keeps it far longer — the tiniest tilt sends it skating to the end
// stop. Anything inside or just past the ice becomes hard to avoid.
//
// Purely a surface modifier through BallOnPlatformController.RegisterSurface,
// so it stacks cleanly with wind/magnet pushes and never fights them.
[RequireComponent(typeof(BoxCollider2D))]
public class IcePatch : MonoBehaviour
{
    public float accelerationMultiplier = 1.5f;
    public float dampingHalfLifeMultiplier = 6f;
    public string ballTag = "Ball";

    [Header("Layout — set size here, the parts follow")]
    public Vector2 size = new Vector2(3f, 1.4f);
    public SpriteRenderer sheet;   // 9-sliced ice sprite

    void Awake()
    {
        Layout();
    }

    void OnValidate()
    {
        if (!Application.isPlaying) Layout();
    }

    public void Layout()
    {
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        if (sheet != null) { sheet.drawMode = SpriteDrawMode.Sliced; sheet.size = size; }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;
        var ball = other.GetComponent<BallOnPlatformController>();
        if (ball != null) ball.RegisterSurface(this, accelerationMultiplier, dampingHalfLifeMultiplier);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;
        var ball = other.GetComponent<BallOnPlatformController>();
        if (ball != null) ball.UnregisterSurface(this);
    }

    void OnDisable()
    {
        var ball = FindFirstObjectByType<BallOnPlatformController>();
        if (ball != null) ball.UnregisterSurface(this);
    }
}
