using UnityEngine;

/// <summary>
/// Spectate / scan the field. Slide the spectate slider left or right:
/// the camera swings round to that side and pulls back into a third-person view.
/// Let go and it snaps back to first person quickly, so you don't lose the ball.
///
/// Attach to: the Player GameObject (needs PlayerInputReader and FPPPlayerController on it).
/// Inspector: drag the Main Camera (child of Player) into "Camera Transform".
/// </summary>
public class PlayerLookAround : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    [Header("Third-person view (at full slide)")]
    [Tooltip("How far the camera swings round to the side, in degrees.")]
    [SerializeField] private float maxHeadTurn = 100f;
    [Tooltip("How far the camera pulls back (metres).")]
    [SerializeField] private float thirdPersonDistance = 3f;
    [Tooltip("How far the camera rises (metres).")]
    [SerializeField] private float thirdPersonHeight = 1.6f;
    [Tooltip("Extra downward tilt so more of the pitch is visible (degrees).")]
    [SerializeField] private float thirdPersonTilt = 12f;

    [Header("Timing (keep the return SHORT)")]
    [Tooltip("Seconds to swing out to full third person.")]
    [SerializeField] private float engageSeconds = 0.12f;
    [Tooltip("Seconds to snap back to first person after letting go.")]
    [SerializeField] private float returnSeconds = 0.15f;

    private PlayerInputReader input;
    private FPPPlayerController controller;
    private Vector3 firstPersonLocalPosition;
    private float blend; // -1 (left) .. 0 (first person) .. +1 (right)

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        controller = GetComponent<FPPPlayerController>();
        if (input == null)
            Debug.LogError("PlayerLookAround needs a PlayerInputReader on the same GameObject.", this);
    }

    private void Start()
    {
        if (cameraTransform != null)
            firstPersonLocalPosition = cameraTransform.localPosition;
    }

    // LateUpdate so this runs after FPPPlayerController has applied the normal look
    private void LateUpdate()
    {
        if (input == null || cameraTransform == null) return;

        float target = Mathf.Clamp(input.SpectateAmount, -1f, 1f);

        // Going out uses the engage time, coming back uses the (shorter) return time
        float seconds = Mathf.Abs(target) >= Mathf.Abs(blend) ? engageSeconds : returnSeconds;
        blend = Mathf.MoveTowards(blend, target, Time.deltaTime / Mathf.Max(0.01f, seconds));

        float eased = Mathf.SmoothStep(0f, 1f, Mathf.Abs(blend));
        float yaw = Mathf.Sign(blend) * eased * maxHeadTurn;

        // Orbit the camera round the player's head, pulled back and up
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 offset = new Vector3(0f, thirdPersonHeight * eased, -thirdPersonDistance * eased);
        cameraTransform.localPosition = firstPersonLocalPosition + yawRotation * offset;

        float pitch = controller != null ? controller.Pitch : 0f;
        cameraTransform.localEulerAngles = new Vector3(pitch + thirdPersonTilt * eased, yaw, 0f);
    }
}

