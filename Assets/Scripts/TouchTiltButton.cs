using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A single on-screen tilt button as real UI, driving one side of the stick.
// Held state comes from Unity's UI event system (pointer down/up/exit) rather
// than manually polling Touchscreen/Mouse, so multi-touch and mouse-in-editor
// both work for free through the Canvas's GraphicRaycaster.
//
// pointerId tracking matters because OnPointerUp/OnPointerExit can otherwise
// fire from an unrelated pointer (e.g. a second finger hovering off this
// button while the first is still holding it).
[RequireComponent(typeof(Image))]
public class TouchTiltButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    // Found at runtime when left empty, so this prefab can be dropped into a
    // new level without hand-wiring it.
    public StickController stick;
    public bool isLeft;

    public Sprite unpressedSprite;
    public Sprite pressedSprite;

    private Image image;
    private int activePointerId = int.MinValue;

    void Awake()
    {
        image = GetComponent<Image>();
        if (stick == null) stick = FindFirstObjectByType<StickController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        activePointerId = eventData.pointerId;
        SetHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        Release();
    }

    void Release()
    {
        activePointerId = int.MinValue;
        SetHeld(false);
    }

    void SetHeld(bool held)
    {
        if (stick != null)
        {
            if (isLeft) stick.SetLeftHeld(held);
            else stick.SetRightHeld(held);
        }

        Sprite target = held ? pressedSprite : unpressedSprite;
        if (image != null && target != null) image.sprite = target;
    }
}
