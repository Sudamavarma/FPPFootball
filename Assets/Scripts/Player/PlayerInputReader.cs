using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The one place that reads player input. Keyboard, mouse, gamepad and the
/// on-screen touch controls all end up here, so other scripts never touch devices directly.
///
/// On-screen buttons (On-Screen Button) create a virtual Gamepad, so each one works
/// through one "&lt;Gamepad&gt;" binding below. The look area and the spectate slider are
/// custom UI scripts that this script finds automatically.
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

    /// <summary>Movement: x = strafe, y = forward/back.</summary>
    public Vector2 Move => moveAction.ReadValue<Vector2>();

    // Attacking buttons
    public bool ShootPressed => shootAction.WasPressedThisFrame();
    public bool PassPressed => passAction.WasPressedThisFrame();
    public bool ThroughPressed => throughAction.WasPressedThisFrame();

    // Defending buttons
    public bool TacklePressed => tackleAction.WasPressedThisFrame();
    public bool DefendHeld => defendAction.IsPressed();
    public bool ClearPressed => clearAction.WasPressedThisFrame();

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

        // keyboard key, extra desktop binding, virtual/real gamepad button (used by the on-screen buttons)
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
    }

    private void OnDestroy()
    {
        foreach (InputAction a in allActions) a.Dispose();
    }
}
