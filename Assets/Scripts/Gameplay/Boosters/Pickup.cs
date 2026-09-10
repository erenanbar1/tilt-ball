using PrimeTween;
using UnityEngine;

// Base for anything the Ball collects by touching it. Handles the shared bits —
// the idle bob so pickups read as "alive", the pop-and-vanish on collection,
// and only ever firing once — and leaves what the pickup *does* to OnCollected.
[RequireComponent(typeof(CircleCollider2D))]
public abstract class Pickup : MonoBehaviour
{
    public string ballTag = "Ball";

    [Header("Idle")]
    public float bobHeight = 0.08f;
    public float bobPeriod = 1.4f;
    public float pulseScale = 1.06f;

    [Header("Collect")]
    public float popScale = 1.5f;
    public float popDuration = 0.22f;
    public ParticleSystem collectBurst;
    public AudioClip collectSound;   // falls back to the menu click when empty

    private bool collected;
    private Tween bob;
    private Tween pulse;

    protected virtual void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    protected virtual void Start()
    {
        Vector3 p = transform.localPosition;
        bob = Tween.LocalPositionY(transform, p.y - bobHeight, p.y + bobHeight, bobPeriod, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        Vector3 s = transform.localScale;
        pulse = Tween.Scale(transform, s, s * pulseScale, bobPeriod * 0.5f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag(ballTag)) return;
        var ball = other.GetComponent<BallOnPlatformController>();
        if (ball == null) return;

        collected = true;
        GetComponent<CircleCollider2D>().enabled = false;
        OnCollected(ball);

        if (collectSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(collectSound);
        else AudioManager.PlayClick();
        if (collectBurst != null)
        {
            collectBurst.transform.SetParent(null, true);
            collectBurst.Play();
            Destroy(collectBurst.gameObject, collectBurst.main.duration + collectBurst.main.startLifetime.constantMax + 0.1f);
        }

        bob.Stop();
        pulse.Stop();
        var seq = Sequence.Create(Tween.Scale(transform, transform.localScale * popScale, popDuration, Ease.OutQuad));
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            seq.Group(Tween.Alpha(sr, 0f, popDuration, Ease.InQuad));
        seq.ChainCallback(this, self => self.gameObject.SetActive(false));
    }

    protected abstract void OnCollected(BallOnPlatformController ball);
}
