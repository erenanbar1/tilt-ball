using PrimeTween;
using UnityEngine;

// The losing counterpart of WinTrigger: same fall-in animation, but the Ball has
// to be *fully* inside before it drops, and landing in one ends the level as a
// loss instead of a win.
//
// The winning hole is a disc, so comparing centre distances is enough there. These
// holes are irregular blobs — one is more than twice as wide as it is tall, another
// is a crescent — so a single radius would describe them badly. Both the drop test
// and the drop target are measured against the hole's own collider instead, which
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

    // Resolution of the search for somewhere to drop the Ball (see FindDropPoint).
    // Fixed rather than exposed: these are accuracy dials with one sensible answer,
    // not things a level should ever want to differ on, and the search runs once in
    // the frame the level is lost, so there is nothing to tune for cost.
    const int DropSearchSteps = 5;
    const int DropSearchDirections = 8;
    const int PathSamples = 10;
    const int ClearanceSamples = 12;
    const int ClearanceRefineSteps = 6;

    [Header("Fall-in animation")]
    public float fallDuration = 0.35f;
    // Steeper than the winning hole's fall, so dropping into one of these reads
    // as losing the ball rather than as being collected.
    public float spinDegrees = 260f;
    [Range(0f, 0.9f)]
    public float shrinkDelayFraction = 0.3f;
    public ParticleSystem loseBurst;

    [Header("Result screen")]
    // Beat between the ball disappearing and the game over screen taking over, so
    // the hole sound gets clear air before the screen brings its own — without
    // waiting out the whole clip, which drags.
    public float screenDelay = 1f;

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
        // Sounds the drop as it starts, so it reads with the fall rather than
        // arriving on top of the game over screen's own sound a moment later.
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLoseHole();
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

    // Pulls the ball into the hole while shrinking it to nothing, so it visibly
    // disappears down it — only once that finishes is the loss actually declared.
    // Mirrors WinTrigger.FallIntoHole, except that the winning hole is a disc and
    // can just aim at its own centre, while these have to look for a spot that is
    // really inside the shape (see FindDropPoint).
    void FallIntoHole(Collider2D ballCollider)
    {
        Rigidbody2D rb = ballCollider.attachedRigidbody;
        Transform ballTransform = ballCollider.transform;

        // Read while the collider is still enabled — a disabled one reports no
        // bounds, and it gets switched off a few lines down.
        Vector2 ballCentre = ballCollider.bounds.center;
        float ballRadius = ballCollider.bounds.extents.x;

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
        Vector2 dropPoint = FindDropPoint(ballCentre, ballRadius);
        Vector3 targetPos = new Vector3(dropPoint.x, dropPoint.y, ballTransform.position.z);

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

    // Where the Ball should end up: a point inside the hole that it can reach in a
    // straight line without leaving the hole on the way.
    //
    // This used to be holeCollider.bounds.center, which is the middle of the
    // bounding box, not of the shape. On a round or roughly convex blob the two sit
    // in the same place, but not every hole is convex. One is a crescent, and its
    // box centre falls in the hollow of the curve — half a unit clear of the hole,
    // out on solid board — so the Ball slid out of the hole and shrank away on the
    // board. Aiming inside the shape is not enough on its own either: a straight
    // line across a crescent leaves it and comes back, and from either tip of that
    // one, a third of the trip to the box centre ran over the board.
    //
    // So the target is searched for. The Ball's own centre is the starting point —
    // it is already inside, that being what triggered the drop — and the search
    // walks towards more room, only ever accepting somewhere the Ball could tween
    // to in an unbroken straight line inside the hole. Steps halve as it goes, so
    // it stays local and the Ball settles into the arm it fell into rather than
    // sliding along to a roomier one.
    Vector2 FindDropPoint(Vector2 ballCentre, float ballRadius)
    {
        if (holeCollider == null) return ballCentre;

        // Still the right answer wherever it holds — which on a plain round hole is
        // always — so those keep dropping dead centre exactly as they did before.
        Vector2 boxCentre = holeCollider.bounds.center;
        if (PathStaysInside(ballCentre, boxCentre)) return boxCentre;

        if (!holeCollider.OverlapPoint(ballCentre)) return ballCentre;

        Vector2 best = ballCentre;
        float bestClearance = Clearance(best, ballRadius);
        float step = ballRadius;

        for (int s = 0; s < DropSearchSteps; s++)
        {
            Vector2 origin = best;
            for (int i = 0; i < DropSearchDirections; i++)
            {
                float angle = (i / (float)DropSearchDirections) * Mathf.PI * 2f;
                Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * step;
                // Measured from the Ball, not from origin, so whatever this returns
                // is reachable by the one straight line the tween actually travels.
                if (!PathStaysInside(ballCentre, candidate)) continue;

                float clearance = Clearance(candidate, ballRadius);
                if (clearance <= bestClearance) continue;

                best = candidate;
                bestClearance = clearance;
            }
            step *= 0.5f;
        }
        return best;
    }

    // Walks the straight line the Ball would tween along and reports whether all of
    // it lies inside the hole. Both ends are sampled, so a target out on the board
    // fails here too.
    bool PathStaysInside(Vector2 from, Vector2 to)
    {
        for (int i = 0; i <= PathSamples; i++)
        {
            if (!holeCollider.OverlapPoint(Vector2.Lerp(from, to, i / (float)PathSamples))) return false;
        }
        return true;
    }

    // How much room a point has: the radius of the largest circle around it that
    // still fits in the hole. Capped at the Ball's own radius, because past that
    // one spot is no more comfortable than another and the search should stop
    // wandering and let the Ball drop where it is.
    float Clearance(Vector2 point, float cap)
    {
        if (RimInside(point, cap)) return cap;

        float low = 0f;
        float high = cap;
        for (int i = 0; i < ClearanceRefineSteps; i++)
        {
            float mid = (low + high) * 0.5f;
            if (RimInside(point, mid)) low = mid;
            else high = mid;
        }
        return low;
    }

    bool RimInside(Vector2 centre, float radius)
    {
        for (int i = 0; i < ClearanceSamples; i++)
        {
            float angle = (i / (float)ClearanceSamples) * Mathf.PI * 2f;
            Vector2 rimPoint = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (!holeCollider.OverlapPoint(rimPoint)) return false;
        }
        return true;
    }

    void Finish(Transform ballTransform)
    {
        ballTransform.gameObject.SetActive(false);

        if (loseBurst != null) loseBurst.Play();
        if (stick != null) stick.inputEnabled = false;
        if (loseMessage != null) loseMessage.SetActive(true);

        // The burst and the message above land immediately, so this hold isn't
        // dead air — it just keeps the game over screen's own sound off the top
        // of the hole sound.
        if (screenDelay > 0f) Tween.Delay(this, screenDelay, self => self.DeclareLoss());
        else DeclareLoss();
    }

    void DeclareLoss()
    {
        if (gameManager != null) gameManager.SetState(GameState.Lose);
    }
}
