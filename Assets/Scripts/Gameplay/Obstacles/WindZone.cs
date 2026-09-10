using PrimeTween;
using UnityEngine;

// A band of moving air. While the Ball is inside the trigger it is pushed along
// the stick at `strength` (units/s²) in `direction` (+1 = toward the stick's
// right end, -1 = left). The player has to hold a counter-tilt to stand still —
// or ride it, if the wind happens to blow the right way.
//
// Optional gusting: with `period` > 0 the fan runs for `dutyCycle` of each period
// and idles the rest; `phase` offsets where in the cycle it starts so two zones
// can alternate. The rotor child spins only while the wind is on, which is the
// player's tell.
[RequireComponent(typeof(BoxCollider2D))]
public class WindZone : MonoBehaviour
{
    [Header("Wind")]
    [Range(-1, 1)] public int direction = 1;
    public float strength = 6f;
    public string ballTag = "Ball";

    [Header("Gusting (period 0 = always on)")]
    public float period = 0f;
    [Range(0f, 1f)] public float dutyCycle = 0.5f;
    [Range(0f, 1f)] public float phase = 0f;

    [Header("Layout — set size here, the parts follow")]
    public Vector2 size = new Vector2(4f, 1.2f);
    public SpriteRenderer band;           // 9-sliced tint of the whole zone
    public Transform housing;             // the fan, parked at the upwind end

    [Header("Visuals")]
    public Transform rotor;               // spins while blowing
    public float rotorSpeed = 540f;       // deg/s
    public SpriteRenderer[] chevrons;     // fade while idle
    public ParticleSystem streaks;        // optional, emits while blowing

    public bool IsBlowing { get; private set; } = true;

    void Awake()
    {
        Layout();
    }

    void OnValidate()
    {
        if (!Application.isPlaying) Layout();
    }

    // Sizes the trigger and the band together and spreads the chevrons across
    // the downwind side, so a zone is resized from one field instead of four.
    public void Layout()
    {
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        if (band != null) { band.drawMode = SpriteDrawMode.Sliced; band.size = size; }
        int dir = direction == 0 ? 1 : direction;
        float fanX = -dir * (size.x * 0.5f - 0.5f);
        if (housing != null) housing.localPosition = new Vector3(fanX, 0f, 0f);
        if (chevrons != null && chevrons.Length > 0)
        {
            float start = fanX + dir * 0.9f;
            float end = dir * (size.x * 0.5f - 0.35f);
            for (int i = 0; i < chevrons.Length; i++)
            {
                if (chevrons[i] == null) continue;
                float t = chevrons.Length == 1 ? 0.5f : i / (float)(chevrons.Length - 1);
                chevrons[i].transform.localPosition = new Vector3(Mathf.Lerp(start, end, t), 0f, 0f);
                chevrons[i].flipX = dir < 0;
            }
        }
    }

    void Update()
    {
        bool blowing = period <= 0f || Mathf.Repeat(Time.time / period + phase, 1f) < dutyCycle;
        if (blowing != IsBlowing)
        {
            IsBlowing = blowing;
            float target = blowing ? 1f : 0.25f;
            if (chevrons != null)
                foreach (var c in chevrons) if (c != null) Tween.Alpha(c, target, 0.2f);
            if (streaks != null) { if (blowing) streaks.Play(); else streaks.Stop(); }
        }
        if (rotor != null && IsBlowing)
            rotor.Rotate(0f, 0f, -direction * rotorSpeed * Time.deltaTime);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsBlowing || !other.CompareTag(ballTag)) return;
        var ball = other.GetComponent<BallOnPlatformController>();
        if (ball != null) ball.AddAcceleration(direction * strength);
    }
}
