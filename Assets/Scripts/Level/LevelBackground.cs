using UnityEngine;

// The art behind the play area — sized to exactly the level's box (the design
// width by the level's vertical range) so on a wide or tall device it reads as
// the level's boundary against the camera's plain clear colour. The sprite is
// 9-sliced with a feathered alpha border, so the edge fades out smoothly at a
// constant thickness no matter how tall the level is; the fade sits just
// outside the play area rather than eating into it.
//
// Classic levels are one design-length screenful centred on this object;
// LevelController stretches Tall levels to their climb range.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class LevelBackground : MonoBehaviour
{
    public ScreenFitProfile profile;

    SpriteRenderer sr;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        FitToDesignBox();
    }

    void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        FitToDesignBox();
    }

    public void SetSprite(Sprite sprite)
    {
        sr.sprite = sprite;
        FitToDesignBox();
    }

    public void Fit(float bottomY, float topY)
    {
        if (sr == null || sr.sprite == null || profile == null) return;

        float fade = sr.sprite.border.x / sr.sprite.pixelsPerUnit;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(profile.designWidth + 2f * fade, topY - bottomY + 2f * fade);

        Vector3 pos = transform.position;
        pos.y = (bottomY + topY) * 0.5f;
        transform.position = pos;
    }

    void FitToDesignBox()
    {
        if (profile == null) return;
        float half = profile.designLength * 0.5f;
        Fit(transform.position.y - half, transform.position.y + half);
    }
}
