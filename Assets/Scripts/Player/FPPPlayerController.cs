using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPPPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 80f;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        LockCursor(true);
    }

    private void Update()
    {
        // Esc frees the mouse, click in the Game view to lock it again
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(false);
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            LockCursor(true);

        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        if (Mouse.current == null || cameraTransform == null) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;

        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
        cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        Vector2 input = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
        }

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}