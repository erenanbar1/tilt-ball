using UnityEngine;

// Stretches this sprite to exactly cover the target camera's orthographic view,
// so one background prefab works unmodified across every level regardless of
// that level's camera size/aspect. [ExecuteAlways] keeps it correct in Edit mode too.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    public Camera targetCamera;

    private SpriteRenderer sr;
    private float lastSize = -1f;
    private float lastAspect = -1f;
    private Vector3 lastCameraPos;
    private Sprite lastSprite;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        lastSize = -1f;   // force one fit
        Fit();
    }

    void LateUpdate()
    {
        Fit();
    }

    // Writing the transform every frame would dirty it (and the renderer's bounds)
    // for nothing: the camera only changes on a rotation or a resolution change.
    void Fit()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || sr == null || sr.sprite == null) return;
        if (!targetCamera.orthographic) return;

        Vector3 camPos = targetCamera.transform.position;
        if (targetCamera.orthographicSize == lastSize && targetCamera.aspect == lastAspect &&
            camPos == lastCameraPos && sr.sprite == lastSprite)
        {
            return;
        }

        Vector2 spriteSize = sr.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        transform.localScale = new Vector3(viewWidth / spriteSize.x, viewHeight / spriteSize.y, 1f);

        Vector3 pos = camPos;
        pos.z = transform.position.z;
        transform.position = pos;

        lastSize = targetCamera.orthographicSize;
        lastAspect = targetCamera.aspect;
        lastCameraPos = camPos;
        lastSprite = sr.sprite;
    }
}
