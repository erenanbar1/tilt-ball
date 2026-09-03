using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public StickController stick;
    public bool isLeft; // toggle in Inspector: true for SOL, false for SAĞ

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLeft) stick.SetLeftHeld(true);
        else stick.SetRightHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isLeft) stick.SetLeftHeld(false);
        else stick.SetRightHeld(false);
    }
}
