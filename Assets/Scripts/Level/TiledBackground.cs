using UnityEngine;

// A world-space background that tiles its sprite over a rectangle instead of
// stretching it. World-space is the point: the camera scrolls *over* it, so
// the art passes by at true speed and the climb reads as climbing. Nothing
// here follows the camera.
//
// One tile is exactly the play area's width (profile.designWidth) — the art is
// authored at that width, so features keep their intended size whatever the
// rectangle, and the level panel and the surround (both use this) tile in
// lockstep. Tiling needs the sprite imported with Mesh Type = Full Rect.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class TiledBackground : MonoBehaviour
{
    public ScreenFitProfile profile;

    protected SpriteRenderer sr;

    protected SpriteRenderer Renderer => sr != null ? sr : (sr = GetComponent<SpriteRenderer>());

    // Last rectangle covered, so a change of sprite can re-cover it.
    Rect covered;
    bool hasCovered;

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null || Renderer.sprite == sprite) return;
        Renderer.sprite = sprite;
        if (hasCovered) Cover(covered);
    }

    // Tiles the sprite over the given world rectangle. Every write is skipped
    // when it wouldn't change anything, so Edit-mode callers can poll this
    // without dirtying the scene.
    public void Cover(Rect worldRect)
    {
        var r = Renderer;
        if (r == null || r.sprite == null || profile == null) return;

        Vector2 spriteSize = r.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f || worldRect.width <= 0f || worldRect.height <= 0f) return;

        covered = worldRect;
        hasCovered = true;

        float tileScale = profile.designWidth / spriteSize.x;
        var scale = new Vector3(tileScale, tileScale, 1f);
        var size = new Vector2(worldRect.width / tileScale, worldRect.height / tileScale);
        var pos = new Vector3(worldRect.center.x, worldRect.center.y, transform.position.z);

        if (r.drawMode != SpriteDrawMode.Tiled) r.drawMode = SpriteDrawMode.Tiled;
        if (r.tileMode != SpriteTileMode.Continuous) r.tileMode = SpriteTileMode.Continuous;
        if (transform.localScale != scale) transform.localScale = scale;
        if (r.size != size) r.size = size;
        if (transform.position != pos) transform.position = pos;
    }
}
