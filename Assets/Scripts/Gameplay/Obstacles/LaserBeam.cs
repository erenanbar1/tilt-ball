using UnityEngine;

// Sits on the beam child of a LaserGate and forwards the trigger to the gate,
// which owns the on/off state.
[RequireComponent(typeof(Collider2D))]
public class LaserBeam : MonoBehaviour
{
    public LaserGate gate;

    void Awake()
    {
        if (gate == null) gate = GetComponentInParent<LaserGate>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (gate != null) gate.BeamTouched(other);
    }
}
