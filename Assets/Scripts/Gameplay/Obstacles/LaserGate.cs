using PrimeTween;
using UnityEngine;

// A beam between two emitters that switches on and off on a timer. Touching the
// live beam destroys the Ball (BallHazard.Zap); while it is off the gap is free
// to pass. The beam telegraphs: it flickers for `warmUp` seconds before going
// live, so a Ball halfway through has a moment to commit or back off.
//
// Layout: this object sits at the gate's centre; `beam` is a child stretched
// across the gap carrying its own BoxCollider2D trigger plus a LaserBeam relay,
// and the two emitters are cosmetic children at either end. Size the gate by
// scaling the beam and moving the emitters — nothing here derives geometry.
public class LaserGate : MonoBehaviour
{
    [Header("Timing")]
    public float onDuration = 1.2f;
    public float offDuration = 1.4f;
    public float warmUp = 0.35f;      // flicker before switching on, taken out of offDuration
    [Range(0f, 1f)] public float phase = 0f;
    public bool startOn = false;

    [Header("Layout — set width here, the parts follow")]
    public float width = 3f;
    public float beamThickness = 0.16f;
    public Transform emitterLeft;
    public Transform emitterRight;

    [Header("Parts")]
    public SpriteRenderer beam;
    public Collider2D beamCollider;
    public SpriteRenderer[] emitterLenses;
    public ParticleSystem zapBurst;
    public string ballTag = "Ball";

    [Header("Look")]
    public Color liveColor = new Color(1f, 0.2f, 0.25f, 1f);
    public Color idleColor = new Color(1f, 0.2f, 0.25f, 0.28f);
    public Color lensLive = new Color(1f, 0.25f, 0.25f, 1f);
    public Color lensIdle = new Color(0.45f, 0.1f, 0.12f, 1f);

    public bool IsLive { get; private set; }

    private float cycle;
    private float timeOffset;
    private bool zapped;

    void OnValidate()
    {
        if (!Application.isPlaying) Layout();
    }

    // The beam sprite is 0.32 units wide at scale 1 and the collider is sized in
    // its local space, so scaling the beam's X is all it takes to span the gap.
    public void Layout()
    {
        if (beam != null)
        {
            float spriteW = beam.sprite != null ? beam.sprite.bounds.size.x : 0.32f;
            float spriteH = beam.sprite != null ? beam.sprite.bounds.size.y : 0.32f;
            beam.transform.localScale = new Vector3(width / spriteW, beamThickness / spriteH, 1f);
        }
        float ex = width * 0.5f + 0.3f;
        if (emitterLeft != null) emitterLeft.localPosition = new Vector3(-ex, 0f, 0f);
        if (emitterRight != null) emitterRight.localPosition = new Vector3(ex, 0f, 0f);
    }

    void Awake()
    {
        Layout();
        cycle = onDuration + offDuration;
        timeOffset = phase * cycle + (startOn ? 0f : onDuration);
        if (beamCollider == null && beam != null) beamCollider = beam.GetComponent<Collider2D>();
        if (beamCollider != null) beamCollider.isTrigger = true;
        ApplyLook(false, 0f);
    }

    void Update()
    {
        if (zapped || cycle <= 0f) return;
        float t = Mathf.Repeat(Time.time + timeOffset, cycle);
        bool live = t < onDuration;
        float flicker = 0f;
        if (!live && cycle - t < warmUp)
        {
            // Nervous pre-strike flicker, ramping up as the strike gets closer.
            float ramp = 1f - (cycle - t) / warmUp;
            flicker = Mathf.PerlinNoise(Time.time * 30f, 0f) * ramp * 0.6f;
        }
        IsLive = live;
        ApplyLook(live, flicker);
    }

    void ApplyLook(bool live, float flicker)
    {
        if (beam != null) beam.color = live ? liveColor : Color.Lerp(idleColor, liveColor, flicker);
        if (emitterLenses != null)
            foreach (var lens in emitterLenses)
                if (lens != null) lens.color = live ? lensLive : Color.Lerp(lensIdle, lensLive, flicker);
    }

    // Relayed by the LaserBeam child (trigger callbacks land on the collider's
    // own GameObject).
    public void BeamTouched(Collider2D other)
    {
        if (zapped || !IsLive || !other.CompareTag(ballTag)) return;
        Vector2 hit = beamCollider != null ? beamCollider.ClosestPoint(other.bounds.center) : (Vector2)transform.position;
        if (!BallHazard.Zap(other, hit, this, zapBurst)) return; // shield took it, the Ball is already thrown clear

        zapped = true;
        if (beam != null) Tween.Color(beam, Color.white, 0.08f, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
    }
}
