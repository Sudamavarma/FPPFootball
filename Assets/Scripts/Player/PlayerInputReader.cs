using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Actions that the on-screen action buttons can trigger directly.</summary>
public enum TouchAction { None = 0, Shoot, Pass, Through, Tackle, Defend, Clear }

/// <summary>
/// The one place that reads player input. Keyboard, mouse, gamepad and the touch controls
/// all end up here, so other scripts never touch devices directly.
///
/// Touch controls:
///  - TouchActionButton (the smart action buttons) call SetTouchAction directly.
///  - FloatingJoystick calls SetTouchMove directly.
///  - The Sprint button still uses an On-Screen Button (virtual gamepad).
///  - The look area and the spectate slider are custom UI scripts this script finds automatically.
///
/// Attach to: the Player GameObject (same one as FPPPlayerController).
/// </summary>
public class PlayerInputReader : MonoBehaviour
{
    [Header("Touch (optional, found automatically if empty)")]
    [SerializeField] private TouchLookArea touchLookArea;
    [SerializeField] private SpectateSlider spectateSlider;

    private InputAction moveAction;
    private InputAction shootAction;
    private InputAction passAction;
    private InputAction throughAction;
    private InputAction tackleAction;
    private InputAction defendAction;
    private InputAction clearAction;
    private InputAction sprintAction;
    private InputAction spectateAction;
    private InputAction[] allActions;

    // Written by the touch UI
    private const int TouchActionCount = 7;
    private readonly bool[] touchHeld = new bool[TouchActionCount];
    private readonly bool[] touchPressedLatch = new bool[TouchActionCount];
    private Vector2 touchMove;

    /// <summary>Called by TouchActionButton when a finger goes down (true) or lifts (false).</summary>
    public void SetTouchAction(TouchAction action, bool down)
    {
        int i = (int)action;
        if (i <= 0 || i >= TouchActionCount) return;
        if (down && !touchHeld[i]) touchPressedLatch[i] = true;
        touchHeld[i] = down;
    }

    /// <summary>Called by the FloatingJoystick. x = strafe, y = forward/back, length 0 to 1.</summary>
    public void SetTouchMove(Vector2 value)
    {
        touchMove = Vector2.ClampMagnitude(value, 1f);
    }

    private bool TouchPressed(TouchAction a) { return touchPressedLatch[(int)a]; }
    private bool TouchHeld(TouchAction a) { return touchHeld[(int)a]; }

    /// <summary>Movement: x = strafe, y = forward/back.</summary>
    public Vector2 Move
    {
        get
        {
            Vector2 m = moveAction.ReadValue<Vector2>();
            return touchMove.sqrMagnitude > m.sqrMagnitude ? touchMove : m;
        }
    }

    // Ball with me
    public bool ShootPressed => shootAction.WasPressedThisFrame() || TouchPressed(TouchAction.Shoot);
    public bool PassPressed => passAction.WasPressedThisFrame() || TouchPressed(TouchAction.Pass);
    public bool ThroughPressed => throughAction.WasPressedThisFrame() || TouchPressed(TouchAction.Through);

    // Ball loose or with an opponent
    public bool TacklePressed => tackleAction.WasPressedThisFrame() || TouchPressed(TouchAction.Tackle);
    public bool DefendHeld => defendAction.IsPressed() || TouchHeld(TouchAction.Defend);
    public bool ClearPressed => clearAction.WasPressedThisFrame() || TouchPressed(TouchAction.Clear);

    // Always available
    public bool SprintHeld => sprintAction.IsPressed();

    /// <summary>
    /// Spectate: -1 = look fully left, 0 = normal first person, +1 = look fully right.
    /// Comes from the on-screen slider, or Z (left) and X (right) on the keyboard.
    /// </summary>
    public float SpectateAmount
    {
        get
        {
            float keys = spectateAction.ReadValue<float>();
            float slider = spectateSlider != null ? spectateSlider.Value : 0f;
            return Mathf.Clamp(keys + slider, -1f, 1f);
        }
    }

    /// <summary>Touch drag since last call (in pixels). Returns zero if there is no touch look area.</summary>
    public Vector2 ConsumeTouchLook()
    {
        return touchLookArea != null ? touchLookArea.ConsumeDelta() : Vector2.zero;
    }

    private static InputAction CreateButton(string actionName, params string[] bindings)
    {
        InputAction action = new InputAction(name: actionName, type: InputActionType.Button);
        foreach (string binding in bindings)
            action.AddBinding(binding);
        return action;
    }

    private void Awake()
    {
        moveAction = new InputAction(name: "Move", type: InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        // keyboard key, extra desktop binding, virtual/real gamepad button
        shootAction = CreateButton("Shoot", "<Keyboard>/f", "<Mouse>/rightButton", "<Gamepad>/buttonSouth");
        passAction = CreateButton("Pass", "<Keyboard>/e", "<Gamepad>/buttonEast");
        throughAction = CreateButton("Through", "<Keyboard>/r", "<Gamepad>/buttonNorth");
        tackleAction = CreateButton("Tackle", "<Keyboard>/q", "<Gamepad>/buttonWest");
        defendAction = CreateButton("Defend", "<Keyboard>/g", "<Gamepad>/leftShoulder");
        clearAction = CreateButton("Clear", "<Keyboard>/v", "<Gamepad>/rightShoulder");
        sprintAction = CreateButton("Sprint", "<Keyboard>/leftShift", "<Gamepad>/rightTrigger");

        spectateAction = new InputAction(name: "Spectate", type: InputActionType.Value, expectedControlType: "Axis");
        spectateAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/z")
            .With("Positive", "<Keyboard>/x");

        allActions = new[]
        {
            moveAction, shootAction, passAction, throughAction, tackleAction,
            defendAction, clearAction, sprintAction, spectateAction
        };
    }

    private void Start()
    {
        if (touchLookArea == null)
            touchLookArea = FindFirstObjectByType<TouchLookArea>();
        if (spectateSlider == null)
            spectateSlider = FindFirstObjectByType<SpectateSlider>();
    }

    private void OnEnable()
    {
        foreach (InputAction a in allActions) a.Enable();
    }

    private void OnDisable()
    {
        foreach (InputAction a in allActions) a.Disable();
        for (int i = 0; i < TouchActionCount; i++)
        {
            touchHeld[i] = false;
            touchPressedLatch[i] = false;
        }
        touchMove = Vector2.zero;
    }

    private void LateUpdate()
    {
        // A tap counts as "pressed" for the rest of the frame it arrived in
        for (int i = 0; i < TouchActionCount; i++) touchPressedLatch[i] = false;
    }

    private void OnDestroy()
    {
        foreach (InputAction a in allActions) a.Dispose();
    }
}
