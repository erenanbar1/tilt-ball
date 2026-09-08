using UnityEngine;
using UnityEngine.InputSystem;

// On-screen tilt controls for touch devices.
//
// The touchable region is the whole of `touchArea`'s sprite, split down its
// middle: a finger anywhere on the left half acts as the left arrow key, a
// finger anywhere on the right half as the right arrow key. The two button
// sprites are feedback only — they show their pressed state whenever their
// half of the region is held, even when the finger never lands on the button
// graphic itself.
//
// The held state is written to StickController.touchLeft/RightHeld, which OR's
// it with the keyboard, so the arrow keys keep behaving exactly as before and
// both halves can be held at the same time (multi-touch).
//
// Runs ahead of StickController so a press is picked up on the same frame it
// happens rather than the next one.
//
// Swapping the artwork: the backdrop is just the sprite on `touchArea`'s own
// SpriteRenderer — change it there and the touchable region follows it, since
// the halves are derived from its bounds rather than from fixed numbers. The
// buttons need two images each, so theirs live on `unpressedSprite` and
// `pressedSprite` below; the renderers are driven from those and whatever
// sprite is assigned to them in the inspector gets overwritten on Start.
[DefaultExecutionOrder(-100)]
public class TouchTiltControls : MonoBehaviour
{
    // Both scene references are resolved at runtime when left empty, so the whole
    // rig can live in a prefab: dropping it into a level wires itself to whatever
    // stick and camera that level has.
    [Header("References (found automatically if left empty)")]
    public StickController stick;
    public Camera targetCamera;

    [Header("Rig (inside the prefab)")]
    public SpriteRenderer touchArea;   // the Square — its bounds are the touchable region

    [Header("Buttons (visual feedback only)")]
    public SpriteRenderer leftButton;
    public SpriteRenderer rightButton;
    public Sprite unpressedSprite;
    public Sprite pressedSprite;

    [Header("Editor testing")]
    public bool mouseActsAsTouch = true;

    private Vector3 leftButtonBasePosition;
    private Vector3 rightButtonBasePosition;
    private bool leftPressedVisual;
    private bool rightPressedVisual;

    void Awake()
    {
        if (stick == null) stick = FindFirstObjectByType<StickController>();
        if (targetCamera == null) targetCamera = Camera.main;
        if (leftButton != null) leftButtonBasePosition = leftButton.transform.localPosition;
        if (rightButton != null) rightButtonBasePosition = rightButton.transform.localPosition;
    }

    void Start()
    {
        // Force both buttons through the swap once so a scene saved with the
        // pressed sprite assigned still starts out looking unpressed.
        leftPressedVisual = true;
        rightPressedVisual = true;
        ApplyVisual(leftButton, leftButtonBasePosition, false, ref leftPressedVisual);
        ApplyVisual(rightButton, rightButtonBasePosition, false, ref rightPressedVisual);
    }

    void Update()
    {
        bool leftHeld = false;
        bool rightHeld = false;

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.press.isPressed) continue;
                Accumulate(touch.position.ReadValue(), ref leftHeld, ref rightHeld);
            }
        }

        if (mouseActsAsTouch)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
                Accumulate(mouse.position.ReadValue(), ref leftHeld, ref rightHeld);
        }

        if (stick != null)
        {
            stick.touchLeftHeld = leftHeld;
            stick.touchRightHeld = rightHeld;
        }

        ApplyVisual(leftButton, leftButtonBasePosition, leftHeld, ref leftPressedVisual);
        ApplyVisual(rightButton, rightButtonBasePosition, rightHeld, ref rightPressedVisual);
    }

    // Marks whichever half of the touch area this screen point falls in. Points
    // outside the area are ignored entirely, so a finger elsewhere on screen
    // never tilts the stick.
    void Accumulate(Vector2 screenPosition, ref bool leftHeld, ref bool rightHeld)
    {
        if (touchArea == null || targetCamera == null) return;

        Vector3 world = targetCamera.ScreenToWorldPoint(screenPosition);
        Bounds area = touchArea.bounds;

        if (world.x < area.min.x || world.x > area.max.x) return;
        if (world.y < area.min.y || world.y > area.max.y) return;

        if (world.x < area.center.x) leftHeld = true;
        else rightHeld = true;
    }

    void ApplyVisual(SpriteRenderer button, Vector3 basePosition, bool pressed, ref bool current)
    {
        if (button == null || pressed == current) return;
        current = pressed;

        Sprite target = pressed ? pressedSprite : unpressedSprite;
        if (target == null) return;

        button.sprite = target;

        // The two sprites are trimmed differently inside the same source texture,
        // so their centre pivots sit at different spots on that texture. Shifting
        // by the difference keeps the artwork's own canvas aligned across the
        // swap, instead of letting the button hop by the trim delta.
        Vector2 offset = PivotOffsetFromTextureCentre(target) - PivotOffsetFromTextureCentre(unpressedSprite);
        Vector3 scale = button.transform.localScale;
        button.transform.localPosition = basePosition + new Vector3(offset.x * scale.x, offset.y * scale.y, 0f);
    }

    static Vector2 PivotOffsetFromTextureCentre(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return Vector2.zero;
        Vector2 textureCentre = new Vector2(sprite.texture.width, sprite.texture.height) * 0.5f;
        return (sprite.rect.center - textureCentre) / sprite.pixelsPerUnit;
    }
}
