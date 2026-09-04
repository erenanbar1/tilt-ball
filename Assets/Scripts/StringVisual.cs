using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class StringVisual : MonoBehaviour
{
    public Transform pulleyEnd;
    public Transform movingEnd;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
    }

    void LateUpdate()
    {
        if (pulleyEnd == null || movingEnd == null) return;
        lr.SetPosition(0, pulleyEnd.position);
        lr.SetPosition(1, movingEnd.position);
    }
}
