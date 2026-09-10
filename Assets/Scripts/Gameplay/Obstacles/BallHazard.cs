using PrimeTween;
using UnityEngine;

// Shared "the ball is destroyed by something that isn't a hole" ending — lasers
// today, anything zappy tomorrow. Mirrors LoseTrigger's flow (hole sound, Ball
// handed over, stick locked, GameManager told after a short beat) but with a
// flash-and-burst instead of a fall-in, since nothing is swallowing the Ball.
public static class BallHazard
{
    public static bool TryShield(Collider2D ballCollider, Vector2 threatCenter)
    {
        var shield = ballCollider.GetComponent<BallShield>();
        return shield != null && shield.TryAbsorb(threatCenter);
    }

    // Returns false if the Ball had a shield up and survived.
    public static bool Zap(Collider2D ballCollider, Vector2 threatCenter, MonoBehaviour owner,
                           ParticleSystem burst = null, float screenDelay = 1f)
    {
        if (TryShield(ballCollider, threatCenter)) return false;

        var rb = ballCollider.attachedRigidbody;
        var ballTransform = ballCollider.transform;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        // Hands the Ball over from BallOnPlatformController, which stops driving
        // it as soon as its collider goes off.
        ballCollider.enabled = false;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayLoseHole();

        var sr = ballTransform.GetComponent<SpriteRenderer>();
        Vector3 startScale = ballTransform.localScale;
        var seq = Sequence.Create();
        if (sr != null)
        {
            seq.Group(Tween.Color(sr, Color.white, new Color(1f, 0.35f, 0.25f), 0.08f, Ease.OutQuad, cycles: 4, cycleMode: CycleMode.Yoyo));
        }
        seq.Group(Tween.Scale(ballTransform, startScale * 1.3f, 0.12f, Ease.OutQuad))
           .Chain(Tween.Scale(ballTransform, Vector3.zero, 0.2f, Ease.InQuad))
           .ChainCallback(() =>
           {
               ballTransform.gameObject.SetActive(false);
               if (burst != null) burst.Play();
               var stick = Object.FindFirstObjectByType<StickController>();
               if (stick != null) stick.inputEnabled = false;
               var gm = GameManager.Instance;
               if (gm == null) gm = Object.FindFirstObjectByType<GameManager>();
               if (screenDelay > 0f) Tween.Delay(owner, screenDelay, _ => { if (gm != null) gm.SetState(GameState.Lose); });
               else if (gm != null) gm.SetState(GameState.Lose);
           });
        return true;
    }
}
