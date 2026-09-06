using PrimeTween;
using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    // Found at runtime when left empty, so this prefab can be dropped into a new
    // level without hand-wiring it. Only the stick is resolved this way — the rest
    // of the references live inside the prefab.
    public StickController stick;
    public GameObject winMessage;
    public string ballTag = "Ball";

    [Header("Capture")]
    public SpriteRenderer holeFill; // the black disc — ball must be sufficiently within its bounds before it falls in
    [Range(0f, 1f)]
    public float containmentFraction = 0.5f; // how much of the ball must have entered (0 = just touching, 1 = fully inside)

    [Header("Fall-in animation")]
    public float fallDuration = 0.35f;
    // How far the ball turns on its way down. It is already rolling when it
    // arrives, so carrying that spin into the hole reads better than freezing it.
    public float spinDegrees = 220f;
    // The ball only starts vanishing once it is over the mouth of the hole —
    // shrinking from the first frame looks like it evaporates in mid-air.
    [Range(0f, 0.9f)]
    public float shrinkDelayFraction = 0.3f;
    public ParticleSystem winBurst; // stars that fire outward once the ball is fully swallowed

    private bool won;

    void Awake()
    {
        if (stick == null) stick = FindFirstObjectByType<StickController>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (won || !other.CompareTag(ballTag)) return;
        if (!IsSufficientlyContained(other)) return;

        won = true;
        FallIntoHole(other);
    }

    // True once enough of the ball's circular body has crossed into the black
    // fill's radius. Distance between centers ranges from captureRadius+ballRadius
    // (just touching, 0% in) down to captureRadius-ballRadius (fully inside, 100%
    // in); containmentFraction picks the threshold along that range.
    bool IsSufficientlyContained(Collider2D ballCollider)
    {
        if (holeFill == null) return true;

        float captureRadius = holeFill.bounds.extents.x;
        float ballRadius = ballCollider.bounds.extents.x;
        float distance = Vector2.Distance(ballCollider.bounds.center, holeFill.bounds.center);
        float threshold = captureRadius - ballRadius * (2f * containmentFraction - 1f);
        return distance <= threshold;
    }

    // Pulls the ball to the hole's center while shrinking it to nothing, so it
    // visibly disappears into the black fill — only once that finishes is the
    // win actually declared.
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
        Vector3 targetPos = transform.position;
        targetPos.z = ballTransform.position.z;

        // Three beats over the same window: the hole draws the ball in with an
        // accelerating fall, the ball keeps turning on the way down, and the
        // shrink holds off until it is actually over the mouth.
        Sequence.Create(Tween.Position(ballTransform, targetPos, fallDuration, Ease.InQuad))
            .Group(Tween.LocalEulerAngles(ballTransform, startAngles,
                startAngles + new Vector3(0f, 0f, -spinDegrees), fallDuration, Ease.InQuad))
            .Group(Tween.Scale(ballTransform, startScale, Vector3.zero,
                fallDuration * (1f - shrinkDelayFraction), Ease.InQuad,
                startDelay: fallDuration * shrinkDelayFraction))
            .ChainCallback(() => Finish(ballTransform));
    }

    void Finish(Transform ballTransform)
    {
        ballTransform.gameObject.SetActive(false);

        if (winBurst != null) winBurst.Play();
        if (stick != null) stick.inputEnabled = false;
        if (winMessage != null) winMessage.SetActive(true);
        LevelFlow.NotifyWin();
    }
}
