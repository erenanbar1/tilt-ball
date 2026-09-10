using UnityEngine;

// Booster: wraps the Ball in a one-hit bubble. The next losing hole or laser
// that would have ended the run pops the bubble instead and throws the Ball
// clear (see BallShield / BallHazard.TryShield). A second pickup while the
// bubble is up is simply spent — there is no stacking.
public class ShieldPickup : Pickup
{
    [Header("Shield")]
    public Sprite bubbleSprite;

    protected override void OnCollected(BallOnPlatformController ball)
    {
        var shield = ball.GetComponent<BallShield>();
        if (shield == null) shield = ball.gameObject.AddComponent<BallShield>();
        shield.Activate(bubbleSprite);
    }
}
