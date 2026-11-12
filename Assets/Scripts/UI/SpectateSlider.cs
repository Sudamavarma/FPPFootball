using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Horizontal "spectate" slider. Hold and slide the knob left or right; let go and the knob
/// springs back to the middle on its own. Value goes from -1 (full left) to +1 (full right),
/// 0 at rest. PlayerInputReader reads Value and PlayerLookAround turns the camera with it.
///
/// Attach to: the slider TRACK (the pill-shaped UI Image on the Canvas).
/// Inspector: drag the child "Knob" Image into the Knob slot (if empty, the first child is used).
/// The Knob Image should have Raycast Target UNTICKED; the track gets the touches.
/// </summary>
public class SpectateSlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    private const int NoPointer = int.MinValue;

    [SerializeField] private RectTransform knob;
    [Tooltip("Ignore tiny movements around the middle (0 to 1).")]
    [SerializeField] private float deadZone = 0.05f;
    [Tooltip("How long the knob takes to slide back to the middle after letting go (seconds).")]
    [SerializeField] private float knobReturnSeconds = 0.1f;

    /// <summary>-1 = full left, 0 = rest, +1 = full right. Drops to 0 the moment the finger lifts.</summary>
    public float Value { get; private set; }

    private RectTransform track;
    private int activePointerId = NoPointer;
    private float knobPosition; // -1..1, what the knob shows

    private void Awake()
    {
        track = (RectTransform)transform;

        if (knob == null && transform.childCount > 0)
            knob = transform.GetChild(0) as RectTransform;
        if (knob == null)
            Debug.LogWarning("SpectateSlider: no Knob assigned. Drag the Knob Image into the Knob slot.", this);
    }

    private void OnDisable()
    {
        Release();
        knobPosition = 0f;
        MoveKnob();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != NoPointer) return;
        activePointerId = eventData.pointerId;
        UpdateValue(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        UpdateValue(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId) Release();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId) Release();
    }

    private void Update()
    {
        // Safety net: if the release event was ever missed (finger lifted outside the
        // window, app lost focus...), let go as soon as nothing is pressed any more.
        if (activePointerId != NoPointer && !AnyPointerHeld())
            Release();

        if (activePointerId != NoPointer)
            knobPosition = Value;
        else
            knobPosition = Mathf.MoveTowards(knobPosition, 0f,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, knobReturnSeconds));

        MoveKnob();
    }

    private void Release()
    {
        activePointerId = NoPointer;
        Value = 0f;
    }

    private static bool AnyPointerHeld()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
        if (Touchscreen.current != null && Touchscreen.current.press.isPressed) return true;
        return false;
    }

    private void UpdateValue(PointerEventData eventData)
    {
        float travel = MaxTravel();
        if (travel <= 0.01f) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            track, eventData.position, eventData.pressEventCamera, out Vector2 local);

        float v = Mathf.Clamp(local.x / travel, -1f, 1f);
        Value = Mathf.Abs(v) < deadZone ? 0f : v;
    }

    private void MoveKnob()
    {
        if (knob == null || track == null) return;
        knob.anchoredPosition = new Vector2(knobPosition * MaxTravel(), 0f);
    }

    // How far (in UI units) the knob centre can move from the middle
    private float MaxTravel()
    {
        if (knob == null) return 0f;
        return (track.rect.width - knob.rect.width) * 0.5f;
    }
}

