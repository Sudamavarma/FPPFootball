using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One round action button that changes what it does (and its text) with the ball:
///   ball with me             -> Ball With Me Action  (Shoot / Pass / Through)
///   ball loose or opponent's -> Otherwise Action     (Tackle / Defend / Clear)
///
/// It talks to PlayerInputReader directly, so it does not depend on the virtual gamepad.
/// Defend is a hold button; the others are taps.
///
/// Attach to: a UI Image (round) on the touch Canvas. Build them with
/// FPPFootball > Build Touch Buttons And Floating Joystick.
/// </summary>
public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("What this button does")]
    [SerializeField] private TouchAction ballWithMeAction = TouchAction.Shoot;
    [SerializeField] private TouchAction otherwiseAction = TouchAction.Tackle;

    [Header("Look")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color pressedColor = new Color(0.65f, 0.85f, 1f, 1f);

    private PlayerInputReader reader;
    private PlayerPossession possession;
    private TouchAction current = TouchAction.None;
    private bool pressed;

    private void Update()
    {
        if (reader == null) reader = FindFirstObjectByType<PlayerInputReader>();
        if (possession == null) possession = FindFirstObjectByType<PlayerPossession>();

        bool withMe = possession != null && possession.IsAttacking;
        TouchAction wanted = withMe ? ballWithMeAction : otherwiseAction;

        if (wanted != current)
        {
            Release();               // the button changed meaning: let go of the old action
            current = wanted;
            if (label != null) label.text = current.ToString().ToUpperInvariant();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (reader == null || current == TouchAction.None) return;
        pressed = true;
        reader.SetTouchAction(current, true);
        if (background != null) background.color = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        if (pressed && reader != null) reader.SetTouchAction(current, false);
        pressed = false;
        if (background != null) background.color = normalColor;
    }

    private void OnDisable()
    {
        Release();
    }
}
