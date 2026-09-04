using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public StickController stick;
    public bool isLeft; // toggle in Inspector: true for SOL, false for SAĞ

    public Image image;
    public Sprite unpressedSprite;
    public Sprite pressedSprite;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLeft) stick.SetLeftHeld(true);
        else stick.SetRightHeld(true);

        if (image != null && pressedSprite != null) image.sprite = pressedSprite;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isLeft) stick.SetLeftHeld(false);
        else stick.SetRightHeld(false);

        if (image != null && unpressedSprite != null) image.sprite = unpressedSprite;
    }
}
