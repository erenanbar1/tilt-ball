using UnityEngine;

// [ExecuteAlways]: keeps the string live in the Scene view too, so it tracks
// the Pulley/LeftWall/RightWall while you drag them around in Edit mode.
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class StringVisual : MonoBehaviour
{
    public Transform pulleyEnd;
    public Transform movingEnd;

    [Header("Manual fine-tuning")]
    // Added on top of pulleyEnd/movingEnd's world position, so the string keeps
    // following its target while letting you nudge exactly where it starts/ends.
    // The string attaches directly to these two points — no forced angle — so it
    // stays attached wherever you drag the Pulley (left/right included).
    public Vector2 startPointOffset;
    public Vector2 endPointOffset;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
    }

    void LateUpdate()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (pulleyEnd == null || movingEnd == null) return;
        lr.SetPosition(0, pulleyEnd.position + (Vector3)startPointOffset);
        lr.SetPosition(1, movingEnd.position + (Vector3)endPointOffset);
    }
}
