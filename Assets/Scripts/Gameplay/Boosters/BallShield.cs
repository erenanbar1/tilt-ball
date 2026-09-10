using PrimeTween;
using UnityEngine;

// A one-hit bubble around the Ball, granted by ShieldPickup. Anything that would
// end the run — a losing hole, a laser — asks TryAbsorb first; if the shield is
// up it pops, knocks the Ball back along the stick away from the threat, and the
// threat lets the Ball go. Lives on the Ball itself (added at runtime) so it
// survives whatever the obstacle that spawned it does afterwards.
[RequireComponent(typeof(BallOnPlatformController))]
public class BallShield : MonoBehaviour
{
    [Header("Knock-back")]
    // Along-platform speed the Ball is thrown away from the threat with when the
    // bubble pops. Enough to clear a fully-swallowing hole before its grace ends.
    public float knockBackSpeed = 4.5f;

    [Header("Bubble")]
    public float bubbleScale = 1.35f;      // relative to the Ball's own diameter
    public Color bubbleColor = new Color(0.55f, 0.9f, 1f, 0.75f);
    public int bubbleSortingOrder = 3;

    public bool IsActive => active;

    private bool active;
    private BallOnPlatformController ball;
    private SpriteRenderer bubble;
    private Tween breathe;

    void Awake()
    {
        ball = GetComponent<BallOnPlatformController>();
    }

    public void Activate(Sprite bubbleSprite)
    {
        if (bubble == null) BuildBubble(bubbleSprite);
        active = true;
        bubble.gameObject.SetActive(true);
        bubble.color = bubbleColor;

        var t = bubble.transform;
        float baseScale = BubbleLocalScale(bubbleSprite);
        t.localScale = Vector3.zero;
        breathe.Stop();
        Tween.Scale(t, Vector3.one * baseScale, 0.25f, Ease.OutBack)
            .OnComplete(this, self => self.breathe = Tween.Scale(self.bubble.transform, baseScale * 1.08f, 0.9f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo));
    }

    // Called by hazards. Returns true if the hit was absorbed — the caller must
    // then leave the Ball alone. `threatCenter` decides which way the Ball is thrown.
    public bool TryAbsorb(Vector2 threatCenter)
    {
        if (!active) return false;
        active = false;

        float side = ball.SignedOffsetAlong(threatCenter);
        // The threat is dead ahead: throw toward whichever way the Ball was
        // already travelling, falling back to the left.
        float away = Mathf.Abs(side) > 0.01f ? -Mathf.Sign(side)
                   : (Mathf.Abs(ball.VelocityAlongPlatform) > 0.01f ? -Mathf.Sign(ball.VelocityAlongPlatform) : -1f);
        ball.SetVelocity(away * knockBackSpeed);

        Pop();
        return true;
    }

    void Pop()
    {
        if (bubble == null) return;
        breathe.Stop();
        var t = bubble.transform;
        Sequence.Create(Tween.Scale(t, t.localScale * 1.6f, 0.18f, Ease.OutQuad))
            .Group(Tween.Alpha(bubble, 0f, 0.18f, Ease.OutQuad))
            .ChainCallback(this, self => { if (self.bubble != null) self.bubble.gameObject.SetActive(false); });
    }

    void BuildBubble(Sprite sprite)
    {
        var go = new GameObject("ShieldBubble");
        go.transform.SetParent(transform, false);
        bubble = go.AddComponent<SpriteRenderer>();
        bubble.sprite = sprite;
        bubble.sortingOrder = bubbleSortingOrder;
    }

    // The Ball is scaled down heavily (0.05) so the bubble's local scale has to
    // undo that to land at a sensible world size around it.
    float BubbleLocalScale(Sprite sprite)
    {
        if (sprite == null) return 1f;
        float worldDiameter = ball.BallRadius * 2f * bubbleScale;
        float spriteWorld = sprite.bounds.size.x * transform.lossyScale.x;
        return spriteWorld > 0.0001f ? worldDiameter / spriteWorld : 1f;
    }
}
