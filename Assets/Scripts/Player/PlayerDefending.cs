using UnityEngine;

/// <summary>
/// Defending actions. Active whenever the ball is NOT with you (loose, or with an opponent).
///
/// TACKLE: a quick slide forward. If the ball carrier (or a loose ball) is within reach in front of
///         you during the slide, you snatch the ball. Cooldown applies. When you get a 3D model,
///         play the slide animation when StartSlide() runs.
/// DEFEND: hold it for "match-up", like eFootball: you automatically close in on the ball carrier
///         (or the loose ball), turn to face them, and keep a short distance. You can still steer a bit.
///
/// Clear is in PlayerBallInteraction.
///
/// Attach to: the Player GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerDefending : MonoBehaviour
{
    [Header("Tackle (slide to snatch the ball)")]
    [SerializeField] private float slideDistance = 3f;
    [SerializeField] private float slideSeconds = 0.3f;
    [Tooltip("How far from you the carrier or ball can be for the tackle to win it (metres).")]
    [SerializeField] private float tackleReach = 1.8f;
    [SerializeField] private float tackleCooldown = 1.2f;
    [Tooltip("1 = always wins the ball when in reach. Lower it to make tackles fail sometimes.")]
    [Range(0f, 1f)]
    [SerializeField] private float tackleSuccessChance = 1f;

    [Header("Defend (match-up)")]
    [Tooltip("Match-up running speed as a fraction of your walking speed.")]
    [SerializeField] private float matchUpSpeedFactor = 0.9f;
    [Tooltip("How close you stand to a ball carrier while matching up (metres).")]
    [SerializeField] private float matchUpDistance = 1.6f;
    [Tooltip("How fast you turn to face the carrier (degrees per second).")]
    [SerializeField] private float matchUpTurnSpeed = 200f;

    private PlayerInputReader input;
    private PlayerPossession possession;
    private PlayerTeam team;
    private CharacterController controller;
    private FPPPlayerController mover;
    private BallOwnership ballOwnership;
    private Rigidbody ballBody;

    private float slideTimeLeft;
    private float nextTackleTime;
    private bool tackleResolved;
    private Vector3 slideDirection;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        possession = GetComponent<PlayerPossession>();
        team = GetComponent<PlayerTeam>();
        controller = GetComponent<CharacterController>();
        mover = GetComponent<FPPPlayerController>();
        if (input == null)
            Debug.LogError("PlayerDefending needs a PlayerInputReader on the same GameObject.", this);
    }

    private void Start()
    {
        ballOwnership = FindFirstObjectByType<BallOwnership>();
        if (ballOwnership != null) ballBody = ballOwnership.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (input == null) return;

        if (slideTimeLeft > 0f)
        {
            Slide();
            return;
        }

        BallState state = possession != null ? possession.State : BallState.Loose;
        if (state == BallState.WithMe)
        {
            SetAssist(Vector3.zero);
            return;
        }

        if (input.DefendHeld) MatchUp();
        else SetAssist(Vector3.zero);

        if (input.TacklePressed && Time.time >= nextTackleTime)
            StartSlide();
    }

    // ---------------------------------------------------------------- match-up

    private void MatchUp()
    {
        if (ballBody == null || ballOwnership == null || mover == null) return;

        PlayerTeam holder = ballOwnership.Holder;
        bool chasingCarrier = holder != null && team != null && holder.Team != team.Team;

        Vector3 focus = chasingCarrier ? holder.transform.position : ballBody.position;
        Vector3 toFocus = focus - transform.position;
        toFocus.y = 0f;
        float distance = toFocus.magnitude;
        if (distance < 0.01f)
        {
            SetAssist(Vector3.zero);
            return;
        }

        // Close in, then hold a short distance from a carrier; go right up to a loose ball
        float holdDistance = chasingCarrier ? matchUpDistance : 0.8f;
        if (distance > holdDistance)
            SetAssist(toFocus / distance * (mover.MoveSpeed * matchUpSpeedFactor));
        else
            SetAssist(Vector3.zero);

        // Turn to face them
        float targetYaw = Mathf.Atan2(toFocus.x, toFocus.z) * Mathf.Rad2Deg;
        float newYaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, matchUpTurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
    }

    private void SetAssist(Vector3 velocity)
    {
        if (mover != null) mover.AssistVelocity = velocity;
    }

    // ---------------------------------------------------------------- tackle

    private void StartSlide()
    {
        SetAssist(Vector3.zero);

        slideDirection = transform.forward;
        slideDirection.y = 0f;
        slideDirection.Normalize();

        slideTimeLeft = slideSeconds;
        nextTackleTime = Time.time + tackleCooldown;
        tackleResolved = false;
        // Play your slide animation here when the 3D model is added
    }

    private void Slide()
    {
        float speed = slideDistance / Mathf.Max(0.05f, slideSeconds);
        controller.Move(slideDirection * speed * Time.deltaTime);
        slideTimeLeft -= Time.deltaTime;

        if (!tackleResolved) TryWinBall();
    }

    private void TryWinBall()
    {
        if (ballOwnership == null || ballBody == null || team == null) return;

        PlayerTeam holder = ballOwnership.Holder;
        if (holder == team)
        {
            tackleResolved = true;
            return;
        }

        Vector3 targetPos = holder != null ? holder.transform.position : ballBody.position;
        Vector3 to = targetPos - transform.position;
        to.y = 0f;
        float distance = to.magnitude;
        if (distance > tackleReach || distance < 0.01f) return;
        if (Vector3.Dot(slideDirection, to / distance) < 0.2f) return;

        tackleResolved = true;

        if (holder == null)
        {
            ballOwnership.Possess(team);      // loose ball: slide in and win it
        }
        else if (holder.Team != team.Team && Random.value <= tackleSuccessChance)
        {
            ballOwnership.TryTackle(team);    // snatch it from the opponent
        }
    }
}
