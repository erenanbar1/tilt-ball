using System.Collections;
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
        StartCoroutine(FallIntoHole(other));
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
        ballCollider.enabled = false;

        Vector3 startPos = ballTransform.position;
        Vector3 startScale = ballTransform.localScale;
        Vector3 targetPos = transform.position;

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

        if (winBurst != null) winBurst.Play();
        if (stick != null) stick.inputEnabled = false;
        if (winMessage != null) winMessage.SetActive(true);
        LevelFlow.NotifyWin();
    }
}
