using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A joystick that appears where your thumb touches the left half of the screen.
/// Put your thumb down anywhere on that half, and the stick is centred there; slide to move.
/// Lift the thumb and it returns to its resting spot.
///
/// It sends movement straight to PlayerInputReader.
///
/// Attach to: a transparent UI Image covering the left half of the Canvas ("Raycast Target" on).
/// It needs two children: Base (the ring) and Knob (the inner circle).
/// Build it with FPPFootball > Build Touch Buttons And Floating Joystick.
/// </summary>
public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const int NoPointer = int.MinValue;

    [SerializeField] private RectTransform joystickBase;
    [SerializeField] private RectTransform knob;
    [Tooltip("How far the knob can move from the centre, in canvas units.")]
    [SerializeField] private float radius = 110f;
    [Range(0f, 0.5f)]
    [SerializeField] private float deadZone = 0.1f;
    [Tooltip("If on, the ring follows your thumb when you drag past its edge.")]
    [SerializeField] private bool baseFollowsFinger = false;
    [SerializeField] private float idleAlpha = 0.3f;
    [SerializeField] private float activeAlpha = 0.85f;
    [Tooltip("Resting position: distance from the left and bottom edges.")]
    [SerializeField] private Vector2 idleMargin = new Vector2(280f, 280f);

    private RectTransform area;
    private PlayerInputReader reader;
    private CanvasGroup baseGroup;
    private int activePointer = NoPointer;
    private Vector2 baseOrigin;
    private int idleFramesLeft = 3;

    private void Awake()
    {
        area = (RectTransform)transform;
        if (joystickBase != null)
        {
            baseGroup = joystickBase.GetComponent<CanvasGroup>();
            if (baseGroup == null) baseGroup = joystickBase.gameObject.AddComponent<CanvasGroup>();
            baseGroup.blocksRaycasts = false;   // the area receives the touches, not the ring
        }
    }

    private void Start()
    {
        reader = FindFirstObjectByType<PlayerInputReader>();
        ShowIdle();
    }

    // The canvas may not have its final size on the very first frame, so place the resting stick a few times
    private void LateUpdate()
    {
        if (idleFramesLeft > 0 && activePointer == NoPointer)
        {
            idleFramesLeft--;
            ShowIdle();
        }
    }

    private void OnDisable()
    {
        activePointer = NoPointer;
        if (reader != null) reader.SetTouchMove(Vector2.zero);
    }

    private bool ToLocal(PointerEventData eventData, out Vector2 local)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out local);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointer != NoPointer || joystickBase == null || knob == null) return;
        if (!ToLocal(eventData, out Vector2 local)) return;

        activePointer = eventData.pointerId;

        // Keep the ring fully inside the area
        Rect r = area.rect;
        float pad = joystickBase.sizeDelta.x * 0.5f;
        baseOrigin = new Vector2(
            Mathf.Clamp(local.x, r.xMin + pad, r.xMax - pad),
            Mathf.Clamp(local.y, r.yMin + pad, r.yMax - pad));

        joystickBase.anchoredPosition = baseOrigin;
        knob.anchoredPosition = Vector2.zero;
        if (baseGroup != null) baseGroup.alpha = activeAlpha;

        UpdateStick(local);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer) return;
        if (!ToLocal(eventData, out Vector2 local)) return;
        UpdateStick(local);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer) return;
        activePointer = NoPointer;
        if (reader != null) reader.SetTouchMove(Vector2.zero);
        ShowIdle();
    }

    private void UpdateStick(Vector2 local)
    {
        Vector2 offset = local - baseOrigin;

        if (baseFollowsFinger && offset.magnitude > radius)
        {
            baseOrigin += offset.normalized * (offset.magnitude - radius);
            joystickBase.anchoredPosition = baseOrigin;
            offset = local - baseOrigin;
        }

        Vector2 clamped = Vector2.ClampMagnitude(offset, radius);
        knob.anchoredPosition = clamped;

        Vector2 v = clamped / Mathf.Max(1f, radius);
        float magnitude = v.magnitude;
        v = magnitude < deadZone ? Vector2.zero : v.normalized * ((magnitude - deadZone) / (1f - deadZone));

        if (reader != null) reader.SetTouchMove(v);
    }

    private void ShowIdle()
    {
        if (joystickBase == null || knob == null) return;
        Rect r = area.rect;
        joystickBase.anchoredPosition = new Vector2(-r.width * 0.5f + idleMargin.x, -r.height * 0.5f + idleMargin.y);
        knob.anchoredPosition = Vector2.zero;
        if (baseGroup != null) baseGroup.alpha = idleAlpha;
    }
}
