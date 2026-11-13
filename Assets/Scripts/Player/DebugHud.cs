using UnityEngine;

/// <summary>
/// Small yellow text at the top of the screen so you can see what the game thinks while testing
/// on the phone: frame rate, who has the ball, spectate slider value, and movement input.
/// Untick Visible (or remove it) before a real release.
///
/// Attach to: the Player GameObject.
/// </summary>
public class DebugHud : MonoBehaviour
{
    [SerializeField] private bool visible = true;

    private PlayerInputReader input;
    private PlayerPossession possession;
    private BallOwnership ballOwnership;
    private GUIStyle style;
    private float fps;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        possession = GetComponent<PlayerPossession>();
    }

    private void Start()
    {
        ballOwnership = FindFirstObjectByType<BallOwnership>();
    }

    private void Update()
    {
        float current = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        fps = fps <= 0f ? current : Mathf.Lerp(fps, current, 0.1f);
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
                      + "\nSpectate: " + spectate + "   Move: " + move;

        GUI.Label(new Rect(Screen.width * 0.25f, 8f, Screen.width * 0.5f, Screen.height * 0.2f), text, style);
    }
}
