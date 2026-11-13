using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag a finger (or the mouse, in the Editor) across this UI area to look around.
/// PlayerInputReader reads the accumulated drag each frame.
///
/// Attach to: a UI Image on the Canvas that covers the right half of the screen.
/// The Image can be fully transparent but "Raycast Target" must stay ticked.
/// Put it ABOVE the stick and buttons in the Hierarchy list so they draw on top of it.
/// </summary>
public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const int NoPointer = int.MinValue;

    private Vector2 pendingDelta;
    private int activePointerId = NoPointer;

    private void Start()
    {
        // Default drag threshold is 10 px, which makes the first part of a swipe feel dead.
        if (EventSystem.current != null)
            EventSystem.current.pixelDragThreshold = 1;
    }

    private void OnDisable()
    {
        pendingDelta = Vector2.zero;
        activePointerId = NoPointer;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Only follow the first finger on this area
        if (activePointerId == NoPointer)
            activePointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        pendingDelta += eventData.delta;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
            activePointerId = NoPointer;
    }

    /// <summary>Returns the drag (in pixels) since the last call, then resets it.</summary>
    public Vector2 ConsumeDelta()
    {
        Vector2 d = pendingDelta;
        pendingDelta = Vector2.zero;
        return d;
    }
}
