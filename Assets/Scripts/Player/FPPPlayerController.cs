using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class FPPPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float sprintMultiplier = 1.4f;

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 0.1f;          // mouse
    [Tooltip("Finger drag: degrees turned per inch of finger movement. Higher = faster. Same feel on every phone.")]
    [SerializeField] private float touchLookDegreesPerInch = 140f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Cursor (desktop)")]
    [Tooltip("Untick this to click the on-screen touch controls with a mouse in the Editor.")]
    [SerializeField] private bool lockCursor = true;

    private CharacterController controller;
    private PlayerInputReader input;
    private float verticalVelocity;
    private float pitch;

    /// <summary>Current up/down look angle. PlayerLookAround reads this.</summary>
    public float Pitch => pitch;

    /// <summary>Normal walking speed (metres per second).</summary>
    public float MoveSpeed => moveSpeed;

    /// <summary>Other scripts can slow or speed the player. 1 = normal.</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// Extra flat velocity added on top of the joystick (used by the Defend match-up button
    /// to run you toward the ball carrier). PlayerDefending sets this every frame.
    /// </summary>
    public Vector3 AssistVelocity { get; set; }

    // Mouse locking only makes sense on desktop
    private bool CursorLockActive => lockCursor && !Application.isMobilePlatform;

    // Finger drag arrives in pixels; convert using the screen's density so every phone feels the same
    private float TouchDegreesPerPixel
    {
        get
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 320f;
            dpi = Mathf.Clamp(dpi, 120f, 700f);
            return touchLookDegreesPerInch / dpi;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        if (input == null)
            Debug.LogError("FPPPlayerController needs a PlayerInputReader on the same GameObject.", this);

        // Android often runs at 30 fps by default, which makes touch feel laggy and slow
        if (Application.isMobilePlatform)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }

    private void Start()
    {
        if (CursorLockActive) LockCursor(true);
    }

    private void Update()
    {
        if (input == null) return;

        // Esc frees the mouse, click in the Game view to lock it again
        if (CursorLockActive)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                LockCursor(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor(true);
        }

        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        // Finger drag
        Vector2 delta = input.ConsumeTouchLook() * TouchDegreesPerPixel;

        // Mouse (only while the cursor is locked)
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            delta += Mouse.current.delta.ReadValue() * lookSensitivity;

        if (delta == Vector2.zero) return;

        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
        cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        Vector2 moveInput = input.Move;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        float speed = input.SprintHeld ? moveSpeed * sprintMultiplier : moveSpeed;
        speed *= SpeedMultiplier;

        Vector3 horizontal = move * speed + AssistVelocity;
        float maxSpeed = Mathf.Max(speed, AssistVelocity.magnitude);
        if (horizontal.magnitude > maxSpeed) horizontal = horizontal.normalized * maxSpeed;

        Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
