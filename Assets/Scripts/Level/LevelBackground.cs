using UnityEngine;

// The art behind the play area, sized to exactly the level's box: the
// profile's design width by the level's vertical range. On a wide or tall
// device it reads as the level's boundary against the camera-following
// OutOfLevelBackground behind it, instead of both blending into one field.
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

        Vector2 spriteSize = sr.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        transform.localScale = new Vector3(profile.designWidth / spriteSize.x, (topY - bottomY) / spriteSize.y, 1f);

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
