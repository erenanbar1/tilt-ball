using UnityEngine;

// Traces a thin coloured line along a hole's own collider — the very boundary the
// drop test measures against — so how far the Ball has to get is readable at a
// glance. Thickness is in world units and colour is a plain colour; both are meant
// to be dialled in from the Inspector.
//
// Strictly cosmetic: this only ever writes to its own child LineRenderer and never
// touches the collider, so no outline setting can change the size of the hole or
// how much of the Ball has to be inside before it falls in.
[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class HoleOutline : MonoBehaviour
{
    [Header("Outline — visual only, never affects the drop")]
    public bool showOutline = true;
    public Color outlineColor = new Color(0.24f, 1f, 0.36f, 1f);
    [Min(0f)]
    public float outlineThickness = 0.03f; // world units, so it stays put if the hole is rescaled
    // Drawn in front of the hole by default, straddling the edge.
    public int sortingOrderOffset = 1;
    // Only used when the hole's collider is a circle; polygons trace their own path.
    [Range(8, 128)]
    public int circleSegments = 48;
    public Material outlineMaterial;

    private const string OutlineChildName = "Outline";

    private Collider2D holeCollider;
    private SpriteRenderer holeRenderer;
    private LineRenderer line;

    void OnEnable() { Refresh(); }

    void LateUpdate() { Refresh(); }

    void OnDisable()
    {
        if (line != null) line.enabled = false;
    }

    void Refresh()
    {
        if (holeCollider == null) holeCollider = GetComponent<Collider2D>();
        if (holeRenderer == null) holeRenderer = GetComponent<SpriteRenderer>();
        if (holeCollider == null) return;

        EnsureLine();
        if (line == null) return;

        line.enabled = showOutline && outlineThickness > 0f;
        if (!line.enabled) return;

        // Local-space points, so the outline rides along with the hole without
        // needing a rewrite every time it moves. LineRenderer widths are applied in
        // world units regardless, so the Inspector figure goes straight in and a
        // hole scaled up or down keeps the same on-screen line weight.
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = outlineThickness;
        line.startColor = outlineColor;
        line.endColor = outlineColor;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        if (holeRenderer != null)
        {
            line.sortingLayerID = holeRenderer.sortingLayerID;
            line.sortingOrder = holeRenderer.sortingOrder + sortingOrderOffset;
        }

        if (outlineMaterial != null && line.sharedMaterial != outlineMaterial)
        {
            line.sharedMaterial = outlineMaterial;
        }

        WritePoints();
    }

    // The outline child sits at the hole's own origin with no scale of its own, so
    // the hole's local space is also the line's — collider paths go straight in.
    void WritePoints()
    {
        var polygon = holeCollider as PolygonCollider2D;
        if (polygon != null && polygon.pathCount > 0)
        {
            Vector2[] path = polygon.GetPath(0);
            if (line.positionCount != path.Length) line.positionCount = path.Length;
            for (int i = 0; i < path.Length; i++)
            {
                Vector2 p = path[i] + polygon.offset;
                line.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }
            return;
        }

        var circle = holeCollider as CircleCollider2D;
        if (circle != null)
        {
            if (line.positionCount != circleSegments) line.positionCount = circleSegments;
            for (int i = 0; i < circleSegments; i++)
            {
                float angle = (i / (float)circleSegments) * Mathf.PI * 2f;
                Vector2 p = circle.offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circle.radius;
                line.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }
            return;
        }

        // Anything else: fall back to the collider's box, which at least marks the
        // area out rather than leaving the outline silently missing.
        Bounds b = holeCollider.bounds;
        Vector3 min = transform.InverseTransformPoint(b.min);
        Vector3 max = transform.InverseTransformPoint(b.max);
        if (line.positionCount != 4) line.positionCount = 4;
        line.SetPosition(0, new Vector3(min.x, min.y, 0f));
        line.SetPosition(1, new Vector3(max.x, min.y, 0f));
        line.SetPosition(2, new Vector3(max.x, max.y, 0f));
        line.SetPosition(3, new Vector3(min.x, max.y, 0f));
    }


    void EnsureLine()
    {
        if (line != null) return;

        Transform child = transform.Find(OutlineChildName);
        if (child == null)
        {
            var go = new GameObject(OutlineChildName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        line = child.GetComponent<LineRenderer>();
        if (line == null) line = child.gameObject.AddComponent<LineRenderer>();
    }
}
