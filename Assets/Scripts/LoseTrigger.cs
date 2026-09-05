using System.Collections;
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
    public ParticleSystem loseBurst;

    private bool lost;
    private Collider2D holeCollider;

    void Awake()
    {
        if (stick == null) stick = FindFirstObjectByType<StickController>();
        holeCollider = GetComponent<Collider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (lost || !other.CompareTag(ballTag)) return;
        if (!IsSufficientlyContained(other)) return;

        lost = true;
        StartCoroutine(FallIntoHole(other));
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
    IEnumerator FallIntoHole(Collider2D ballCollider)
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

        Vector3 startPos = ballTransform.position;
        Vector3 startScale = ballTransform.localScale;
        // The blob is not always centred on its pivot, so aim at the shape itself.
        Vector3 targetPos = holeCollider.bounds.center;
        targetPos.z = startPos.z;

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fallDuration);
            float eased = p * p; // accelerate inward, like being sucked in

            ballTransform.position = Vector3.Lerp(startPos, targetPos, eased);
            ballTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
            yield return null;
        }

        ballTransform.position = targetPos;
        ballTransform.localScale = Vector3.zero;
        ballTransform.gameObject.SetActive(false);

        if (loseBurst != null) loseBurst.Play();
        if (stick != null) stick.inputEnabled = false;
        if (loseMessage != null) loseMessage.SetActive(true);
        LevelFlow.NotifyLose();
    }
}
