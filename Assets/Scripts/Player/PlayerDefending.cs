using UnityEngine;

/// <summary>
/// Defending actions. Active whenever the ball is NOT with you (loose, or with an opponent).
///
/// TACKLE (tap): you slide AT the opponent who has the ball (or at a loose ball nearby) and capture it.
///   - It picks the target for you: the opponent ball carrier within Tackle Seek Range.
///   - The body turns to face the target and keeps homing on it during the slide.
///   - Touch the carrier or the ball (within Capture Reach) and the ball is yours.
///   - If nobody is in range, nothing happens (and no cooldown is used).
///   When you add the 3D model, play the slide animation on SlideStarted and stop it on SlideEnded.
///
/// DEFEND (hold): you stretch a leg to grab a LOOSE ball that is near you, for example an opponent's
///   pass in flight, or a shot. If the ball is within Intercept Reach, you capture it.
///   It does nothing while an opponent is holding the ball (use Tackle for that).
///   Play your stretch animation on StretchStarted.
///
/// Clear is in PlayerBallInteraction.
///
/// Attach to: the Player GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerDefending : MonoBehaviour
{
    [Header("Tackle: slide to the opponent and win the ball")]
    [Tooltip("An opponent carrier (or a loose ball) farther than this is ignored (metres).")]
    [SerializeField] private float tackleSeekRange = 6f;
    [SerializeField] private float slideSpeed = 11f;
    [SerializeField] private float maxSlideSeconds = 0.7f;
    [Tooltip("Within this distance of the carrier or the ball, the tackle wins the ball (metres).")]
    [SerializeField] private float captureReach = 1.2f;
    [SerializeField] private float tackleCooldown = 1.2f;
    [Tooltip("1 = always wins the ball on contact. Lower it to make tackles fail sometimes.")]
    [Range(0f, 1f)]
    [SerializeField] private float tackleSuccessChance = 1f;

    [Header("Defend: stretch a leg to intercept a loose ball")]
    [Tooltip("A loose ball closer than this to you can be grabbed (metres).")]
    [SerializeField] private float interceptReach = 2.6f;
    [SerializeField] private float interceptMaxHeight = 1.8f;
    [Tooltip("Faster balls than this cannot be grabbed (metres per second).")]
    [SerializeField] private float interceptMaxBallSpeed = 20f;

    // Hooks for animations when the 3D player model is added
    public event System.Action SlideStarted;
    public event System.Action SlideEnded;
    public event System.Action BallWon;
    public event System.Action StretchStarted;

    public bool IsSliding => slideTimeLeft > 0f;

    private PlayerInputReader input;
    private PlayerPossession possession;
    private PlayerTeam team;
    private CharacterController controller;
    private FPPPlayerController mover;
    private BallOwnership ballOwnership;
    private Rigidbody ballBody;

    private float slideTimeLeft;
    private float nextTackleTime;
    private bool slideResolved;
    private PlayerTeam slideCarrier;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        possession = GetComponent<PlayerPossession>();
        team = GetComponent<PlayerTeam>();
        controller = GetComponent<CharacterController>();
        mover = GetComponent<FPPPlayerController>();
        if (input == null)
            Debug.LogError("PlayerDefending needs a PlayerInputReader on the same GameObject.", this);
        if (team == null)
            Debug.LogWarning("PlayerDefending: add a PlayerTeam to the Player (use FPPFootball > Set Up Possession And Opponent).", this);
    }

    private void Start()
    {
        ballOwnership = FindFirstObjectByType<BallOwnership>();
        if (ballOwnership != null) ballBody = ballOwnership.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (input == null || team == null || ballOwnership == null || ballBody == null) return;

        if (slideTimeLeft > 0f)
        {
            Slide();
            return;
        }

        BallState state = possession != null ? possession.State : BallState.Loose;
        if (state == BallState.WithMe) return;

        if (input.TacklePressed && Time.time >= nextTackleTime) StartSlide();
        else if (input.DefendHeld) TryIntercept();
    }

    // ---------------------------------------------------------------- Defend: intercept

    private void TryIntercept()
    {
        if (ballOwnership.Holder != null) return;     // only a loose ball (a pass or a shot in flight)

        Vector3 toBall = ballBody.position - transform.position;
        if (ballBody.position.y > interceptMaxHeight) return;
        toBall.y = 0f;
        if (toBall.magnitude > interceptReach) return;
        if (ballBody.linearVelocity.magnitude > interceptMaxBallSpeed) return;

        ballOwnership.Possess(team);
        StretchStarted?.Invoke();
        BallWon?.Invoke();
    }

    // ---------------------------------------------------------------- Tackle: slide at the opponent

    private void StartSlide()
    {
        PlayerTeam holder = ballOwnership.Holder;
        slideCarrier = null;
        Vector3 targetPosition;

        if (holder == null)
        {
            targetPosition = ballBody.position;                 // loose ball: slide to it
        }
        else if (holder.Team != team.Team)
        {
            slideCarrier = holder;                              // opponent with the ball: slide at them
            targetPosition = holder.transform.position;
        }
        else
        {
            return;                                             // a teammate has it: nothing to tackle
        }

        Vector3 to = targetPosition - transform.position;
        to.y = 0f;
        float distance = to.magnitude;
        if (distance > tackleSeekRange) return;                 // nobody close enough to slide at

        slideTimeLeft = Mathf.Clamp((distance + 1f) / slideSpeed, 0.25f, maxSlideSeconds);
        nextTackleTime = Time.time + tackleCooldown;
        slideResolved = false;

        if (mover != null) mover.SpeedMultiplier = 0f;          // no walking while sliding
        FaceTowards(to);
        SlideStarted?.Invoke();
    }

    private void Slide()
    {
        // If the carrier lost the ball (kicked it or got tackled), chase the ball instead
        if (slideCarrier != null && ballOwnership.Holder != slideCarrier) slideCarrier = null;

        Vector3 targetPosition = slideCarrier != null ? slideCarrier.transform.position : ballBody.position;
        Vector3 to = targetPosition - transform.position;
        to.y = 0f;
        FaceTowards(to);

        controller.Move(transform.forward * slideSpeed * Time.deltaTime);
        slideTimeLeft -= Time.deltaTime;

        if (!slideResolved) TryCapture();

        if (slideResolved || slideTimeLeft <= 0f) EndSlide();
    }

    private void TryCapture()
    {
        Vector3 toBall = ballBody.position - transform.position;
        toBall.y = 0f;
        float nearest = toBall.magnitude;

        if (slideCarrier != null)
        {
            Vector3 toCarrier = slideCarrier.transform.position - transform.position;
            toCarrier.y = 0f;
            nearest = Mathf.Min(nearest, toCarrier.magnitude);
        }

        if (nearest > captureReach) return;

        slideResolved = true;
        if (slideCarrier != null)
        {
            if (Random.value <= tackleSuccessChance && ballOwnership.TryTackle(team))
                BallWon?.Invoke();
        }
        else
        {
            ballOwnership.Possess(team);
            BallWon?.Invoke();
        }
    }

    private void EndSlide()
    {
        slideTimeLeft = 0f;
        slideCarrier = null;
        if (mover != null) mover.SpeedMultiplier = 1f;
        SlideEnded?.Invoke();
    }

    private void FaceTowards(Vector3 flatDirection)
    {
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
    }
}
