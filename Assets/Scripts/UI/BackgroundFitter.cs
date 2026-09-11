using UnityEngine;

// Uniformly scales this sprite to cover (never stretch/distort) the parent
// camera's orthographic view — the art overflows/crops at the edges rather
// than squishing. Lives as a child of the camera so it follows for free; only
// the scale is managed here. [ExecuteAlways] keeps it correct in Edit mode too.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    Camera cam;
    SpriteRenderer sr;
    float lastSize = -1f;
    float lastAspect = -1f;
    Sprite lastSprite;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = GetComponentInParent<Camera>();
        lastSize = -1f;
        Fit();
    }

    void LateUpdate()
    {
        Fit();
    }

    // Writing the transform every frame would dirty it (and the renderer's bounds)
    // for nothing: the view only changes on a rotation or a resolution change.
    void Fit()
    {
        if (cam == null) cam = GetComponentInParent<Camera>();
        if (cam == null || sr == null || sr.sprite == null || !cam.orthographic) return;

        if (cam.orthographicSize == lastSize && cam.aspect == lastAspect && sr.sprite == lastSprite) return;

        Vector2 spriteSize = sr.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;
        float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
        transform.localScale = new Vector3(scale, scale, 1f);

        lastSize = cam.orthographicSize;
        lastAspect = cam.aspect;
        lastSprite = sr.sprite;
    }
}
