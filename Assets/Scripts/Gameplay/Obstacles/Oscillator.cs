using UnityEngine;

// Slides whatever it is on back and forth between two offsets from its authored
// position — a hole that drifts, a bumper on a rail, a magnet that sweeps. Not
// an obstacle by itself; a modifier that turns a static one into a timing one.
// Position is set directly (no Rigidbody) so triggers on the moved object keep
// working exactly as they do when stationary.
public class Oscillator : MonoBehaviour
{
    public Vector2 travel = new Vector2(2f, 0f);  // half-amplitude; the object swings ±travel
    public float period = 3f;
    [Range(0f, 1f)] public float phase = 0f;

    private Vector3 origin;

    void Awake()
    {
        origin = transform.localPosition;
    }

    void Update()
    {
        if (period <= 0f) return;
        float s = Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f);
        transform.localPosition = origin + (Vector3)(travel * s);
    }
}
