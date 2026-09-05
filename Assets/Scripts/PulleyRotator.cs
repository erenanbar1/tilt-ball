using UnityEngine;

// Purely visual — spins the pulley wheel while its side of the stick is
// rising or falling. Reads StickController's held state and rise/fall speeds
// directly so the spin always matches the stick's actual motion; never writes
// back to the stick, so it can't affect gameplay.
public class PulleyRotator : MonoBehaviour
{
    public StickController stick;
    public bool isLeft; // which side of the stick this pulley's rope serves
    public float degreesPerSpeedUnit = 60f; // visual tuning: pulley turn rate per world-unit/sec of stick-end movement

    void Update()
    {
        if (stick == null) return;

        bool held = isLeft ? stick.leftHeld : stick.rightHeld;
        float speed = held ? stick.riseSpeed : -stick.fallSpeed;

        // Mirrored pulleys spin opposite ways for the same rope motion — realistic for a mirrored pair.
        float sideSign = isLeft ? 1f : -1f;

        transform.Rotate(0f, 0f, speed * sideSign * degreesPerSpeedUnit * Time.deltaTime);
    }
}
