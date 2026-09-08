using PrimeTween;
using UnityEngine;

// Idle "alive" loop for the Mr Ball mascot: a slow breathing scale pulse on the
// body, with each arm swaying independently. Durations are deliberately not
// equal between the arms and the breath, so the loop never lines up into a
// mechanically repeating pose.
//
// [ExecuteAlways] only so the shadow settings below apply while editing; the idle
// animation itself still starts in Play mode only.
[ExecuteAlways]
public class MrBallIdle : MonoBehaviour
{
    [Header("References")]
    public Transform body;
    public Transform leftArm;
    public Transform rightArm;

    [Header("Breathing")]
    public float breathScale = 1.035f;
    public float breathDuration = 1.6f;

    [Header("Arm sway")]
    public float armSwayDegrees = 6f;
    public float leftArmDuration = 1.9f;
    public float rightArmDuration = 2.3f;

    [Header("Shadow")]
    // Left empty, the child named "Shadow" is used.
    public SpriteRenderer shadow;
    // 0 = invisible, 1 = the sprite's full density.
    [Range(0f, 1f)]
    public float shadowOpacity = 0.82f;
    public Color shadowTint = new Color(44f / 255f, 26f / 255f, 78f / 255f, 1f);
    // How far the shadow spreads sideways and front-to-back, as a multiple of the
    // sprite's own size.
    public float shadowWidth = 3f;
    public float shadowHeight = 1f;

    void OnEnable()
    {
        ApplyShadow();
    }

    void OnValidate()
    {
        ApplyShadow();
    }

#if UNITY_EDITOR
    // Editor only (see [ExecuteAlways]), so dragging the sliders updates the shadow
    // immediately. Compiled out of player builds so it costs nothing per frame there.
    void Update()
    {
        if (!Application.isPlaying) ApplyShadow();
    }
#endif

    void ApplyShadow()
    {
        if (shadow == null)
        {
            Transform t = transform.Find("Shadow");
            if (t != null) shadow = t.GetComponent<SpriteRenderer>();
            if (shadow == null) return;
        }

        Color c = shadowTint;
        c.a = shadowOpacity;
        if (shadow.color != c) shadow.color = c;

        Vector3 size = new Vector3(shadowWidth, shadowHeight, 1f);
        if (shadow.transform.localScale != size) shadow.transform.localScale = size;
    }

    void Start()
    {
        ApplyShadow();
        if (!Application.isPlaying) return;

        if (body != null)
        {
            Vector3 baseScale = body.localScale;
            Tween.Scale(body, baseScale, baseScale * breathScale, breathDuration, Ease.InOutSine,
                cycles: -1, cycleMode: CycleMode.Yoyo);
        }

        StartSway(leftArm, armSwayDegrees, leftArmDuration);
        StartSway(rightArm, -armSwayDegrees, rightArmDuration);
    }

    void StartSway(Transform arm, float swayDegrees, float duration)
    {
        if (arm == null) return;
        Vector3 baseAngles = arm.localEulerAngles;
        Tween.LocalEulerAngles(arm, baseAngles, baseAngles + new Vector3(0f, 0f, swayDegrees),
            duration, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
    }
}
