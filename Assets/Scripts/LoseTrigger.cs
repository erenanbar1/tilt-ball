using PrimeTween;
using UnityEngine;

// The losing counterpart of WinTrigger: same fall-in animation, but the Ball has
// to be *fully* inside before it drops, and landing in one ends the level as a
// loss instead of a win.
//
// The winning hole is a disc, so comparing centre distances is enough there. These
// holes are irregular blobs — one is more than twice as wide as it is tall — so a
// single radius would describe them badly. Containment is measured against the
// hole's own collider instead, by testing points around the Ball's rim, which
// stays honest on any shape and needs no per-hole hand-tuning.
[RequireComponent(typeof(Collider2D))]
public class LoseTrigger : MonoBehaviour
{
    // Found at runtime when left empty, so this prefab can be dropped into a new
    // level without hand-wiring it.
    public StickController stick;
    public GameManager gameManager;
    public GameObject loseMessage;
    public string ballTag = "Ball";

    [Header("Capture")]
    [Range(0f, 1f)]
    // How much of the Ball has to be inside before it falls in. Same scale as
    // WinTrigger's: 0 = drops on first touch, 0.5 = centre over the edge (what the
    // winning hole uses), 1 = the whole Ball has to be inside.
    public float containmentFraction = 1f;
    [Range(4, 32)]
    // Points tested around the Ball's rim. More is stricter on jagged edges.
    public int containmentSamples = 12;

    [Header("Fall-in animation")]
    public float fallDuration = 0.35f;
    // Steeper than the winning hole's fall, so dropping into one of these reads
    // as losing the ball rather than as being collected.
    public float spinDegrees = 260f;
    [Range(0f, 0.9f)]
    public float shrinkDelayFraction = 0.3f;
    public ParticleSystem loseBurst;

    private bool lost;
    private Collider2D holeCollider;

    void Awake()
    {
        if (stick == null) stick = FindFirstObjectByType<StickController>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        holeCollider = GetComponent<Collider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (lost || !other.CompareTag(ballTag)) return;
        if (!IsSufficientlyContained(other)) return;

        lost = true;
        FallIntoHole(other);
    }

    // In once every point on a circle of `margin` around the Ball's centre lies
    // inside the hole's shape. margin runs from -ballRadius (just touching)
    // through 0 (centre on the edge) to +ballRadius (fully inside).
    bool IsSufficientlyContained(Collider2D ballCollider)
    {
        if (holeCollider == null) return true;

        Vector2 centre = ballCollider.bounds.center;
        float ballRadius = ballCollider.bounds.extents.x;
        float margin = ballRadius * (2f * containmentFraction - 1f);

        // Below half the Ball's centre may still be outside the shape, and the
        // trigger callback already means the two overlap.
        if (margin < 0f) return true;
        if (!holeCollider.OverlapPoint(centre)) return false;

        for (int i = 0; i < containmentSamples; i++)
        {
            float angle = (i / (float)containmentSamples) * Mathf.PI * 2f;
            Vector2 rimPoint = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * margin;
            if (!holeCollider.OverlapPoint(rimPoint)) return false;
        }
        return true;
    }

    // Pulls the ball to the hole's centre while shrinking it to nothing, so it
    // visibly disappears into the hole — only once that finishes is the loss
    // actually declared. Mirrors WinTrigger.FallIntoHole.
    void FallIntoHole(Collider2D ballCollider)
    {
        Rigidbody2D rb = ballCollider.attachedRigidbody;
        Transform ballTransform = ballCollider.transform;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        // Also hands the Ball over from BallOnPlatformController, which stops
        // driving it as soon as its collider goes off.
        ballCollider.enabled = false;

        Vector3 startScale = ballTransform.localScale;
        Vector3 startAngles = ballTransform.localEulerAngles;
        // The blob is not always centred on its pivot, so aim at the shape itself.
        Vector3 targetPos = holeCollider.bounds.center;
        targetPos.z = ballTransform.position.z;

        // Same three beats as the winning hole, on a sharper curve: the drop
        // accelerates harder and the ball keeps turning until it is gone.
        Sequence.Create(Tween.Position(ballTransform, targetPos, fallDuration, Ease.InCubic))
            .Group(Tween.LocalEulerAngles(ballTransform, startAngles,
                startAngles + new Vector3(0f, 0f, -spinDegrees), fallDuration, Ease.InCubic))
            .Group(Tween.Scale(ballTransform, startScale, Vector3.zero,
                fallDuration * (1f - shrinkDelayFraction), Ease.InQuad,
                startDelay: fallDuration * shrinkDelayFraction))
            .ChainCallback(() => Finish(ballTransform));
    }

    void Finish(Transform ballTransform)
    {
        ballTransform.gameObject.SetActive(false);

        if (loseBurst != null) loseBurst.Play();
        if (stick != null) stick.inputEnabled = false;
        if (loseMessage != null) loseMessage.SetActive(true);
        if (gameManager != null) gameManager.SetState(GameState.Lose);
    }
}
