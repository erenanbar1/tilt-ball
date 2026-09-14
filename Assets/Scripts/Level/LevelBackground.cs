using UnityEngine;

// The art behind the play area, tiled over exactly the level's box: the
// profile's design width by the level's vertical range. On a wide or tall
// device it reads as the level's boundary against the dimmer surround behind
// it (see LevelController.surround), instead of both blending into one field.
//
// Classic levels are one design-length screenful centred on this object;
// LevelController stretches longer levels to their climb range.
[ExecuteAlways]
public class LevelBackground : TiledBackground
{
    void OnEnable()
    {
        FitToDesignBox();
    }

    void OnValidate()
    {
        FitToDesignBox();
    }

    public void Fit(float bottomY, float topY)
    {
        if (profile == null) return;
        float w = profile.designWidth;
        Cover(new Rect(transform.position.x - w * 0.5f, bottomY, w, topY - bottomY));
    }

    void FitToDesignBox()
    {
        if (profile == null) return;
        float half = profile.designLength * 0.5f;
        Fit(transform.position.y - half, transform.position.y + half);
    }
}
