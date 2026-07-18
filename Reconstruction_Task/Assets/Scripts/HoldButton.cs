using UnityEngine;
using UnityEngine.EventSystems;

// Attach this to the same GameObject as your zoom in/out Button.
// Exposes IsHeld so another script can check "is this button currently pressed"
// every frame, which a regular Button.onClick can't do (it only fires on release).
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => IsHeld = true;
    public void OnPointerUp(PointerEventData eventData) => IsHeld = false;

    // Stops the zoom if the pointer is dragged off the button while still held down,
    // so it doesn't get stuck zooming after the finger/mouse leaves the button.
    public void OnPointerExit(PointerEventData eventData) => IsHeld = false;
}
