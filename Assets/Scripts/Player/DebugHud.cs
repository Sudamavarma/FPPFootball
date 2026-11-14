using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Small yellow text at the top-left of the screen so you can see what the game thinks while testing:
/// frame rate, who has the ball, spectate value, movement input, and WHICH UI OBJECT IS UNDER YOUR FINGER.
/// That last line shows what is catching your touches if a control does not respond.
/// Untick Visible (or remove it) before a real release.
///
/// Attach to: the Player GameObject.
/// </summary>
public class DebugHud : MonoBehaviour
{
    [SerializeField] private bool visible = true;

    private PlayerInputReader input;
    private PlayerPossession possession;
    private GUIStyle style;
    private float fps;
    private string fingerOn = "-";
    private readonly List<RaycastResult> hits = new List<RaycastResult>();

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        possession = GetComponent<PlayerPossession>();
    }

    private void Update()
    {
        float current = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        fps = fps <= 0f ? current : Mathf.Lerp(fps, current, 0.1f);

        if (!visible) return;
        UpdateFingerInfo();
    }

    private void UpdateFingerInfo()
    {
        fingerOn = "-";
        if (EventSystem.current == null) { fingerOn = "no EventSystem!"; return; }

        Vector2 position = Vector2.zero;
        bool down = false;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            position = touchscreen.primaryTouch.position.ReadValue();
            down = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            position = Mouse.current.position.ReadValue();
            down = true;
        }
        if (!down) return;

        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = position };
        hits.Clear();
        EventSystem.current.RaycastAll(pointer, hits);
        fingerOn = hits.Count > 0 ? hits[0].gameObject.name : "nothing";
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (style == null) style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.RoundToInt(Screen.height * 0.035f);
        style.normal.textColor = Color.yellow;

        string ball = "no possession script";
        if (possession != null)
        {
            switch (possession.State)
            {
                case BallState.WithMe: ball = "BALL: WITH ME"; break;
                case BallState.WithOpponent: ball = "BALL: OPPONENT"; break;
                case BallState.WithTeammate: ball = "BALL: TEAMMATE"; break;
                default: ball = "BALL: LOOSE"; break;
            }
        }

        string spectate = input != null ? input.SpectateAmount.ToString("0.00") : "n/a";
        string move = input != null ? input.Move.ToString("0.0") : "n/a";

        string text = "FPS " + Mathf.RoundToInt(fps) + "   " + ball
                      + "\nSpectate: " + spectate + "   Move: " + move
                      + "\nFinger on: " + fingerOn;

        GUI.Label(new Rect(Screen.width * 0.02f, 8f, Screen.width * 0.45f, Screen.height * 0.25f), text, style);
    }
}
