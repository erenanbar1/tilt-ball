using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

// Shared entrance animation for WinScreen/GameOver, lifted from the old
// LevelFlow's in-scene panel code now that win/lose are dedicated scenes
// instead of panels toggled within Gameplay.
public static class ScreenEntranceAnimator
{
    // Falls in from off the top and overshoots into place, unwinding a slight
    // tilt as it lands. When idleAfter is set, it keeps breathing gently in
    // place once it lands — same idea as MrBallIdle's body pulse — so a title
    // left on screen (the Main Menu's) doesn't just go static.
    public static void AnimateTitle(RectTransform title, float dropDistance = 520f, float duration = 0.55f, float tiltDegrees = 7f,
        bool idleAfter = false, float idleScale = 1.035f, float idleDuration = 1.8f)
    {
        if (title == null) return;

        Vector2 resting = title.anchoredPosition;
        title.anchoredPosition = resting + new Vector2(0f, dropDistance);
        title.localEulerAngles = new Vector3(0f, 0f, tiltDegrees);

        Vector3 restingScale = title.localScale;

        Tween.UIAnchoredPosition(title, resting, duration, Ease.OutBack)
            .OnComplete(title, t =>
            {
                if (!idleAfter) return;
                Tween.Scale(t, restingScale, restingScale * idleScale, idleDuration, Ease.InOutSine,
                    cycles: -1, cycleMode: CycleMode.Yoyo);
            });
        Tween.LocalEulerAngles(title, new Vector3(0f, 0f, tiltDegrees), Vector3.zero, duration, Ease.OutBack);
    }

    // Pops in a beat after the title, then keeps breathing so it reads as the
    // thing to press.
    public static void AnimateButton(Button button, float delay = 0.26f, float popDuration = 0.42f, float pulseScale = 1.05f, float pulseDuration = 0.9f)
    {
        if (button == null) return;

        Transform t = button.transform;
        t.localScale = Vector3.zero;

        Tween.Scale(t, 0f, 1f, popDuration, Ease.OutBack, startDelay: delay)
            .OnComplete(t, target =>
                Tween.Scale(target, 1f, pulseScale, pulseDuration, Ease.InOutSine,
                    cycles: -1, cycleMode: CycleMode.Yoyo));
    }
}
