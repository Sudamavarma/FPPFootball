using UnityEngine;

/// <summary>
/// The ball actions.
///
/// BALL WITH ME:      Shoot, Pass, Through
///   Shoot   - fires at the opponent goal. It MAY MISS: accuracy drops with distance (see Accuracy fields).
///   Pass    - to the teammate in the direction of your joystick (or the one most in front if the stick is neutral).
///   Through - to free space AHEAD of that teammate (where they are running), not at their feet.
///
/// BALL LOOSE or WITH OPPONENT:  Clear
///   Clear   - boots the ball far away (away from your own goal if one is set up).
///             It also works on a ball an opponent is holding, if you are close enough.
///
/// Tackle and Defend are in PlayerDefending.
/// Kick direction uses where your BODY faces, so the spectate camera never changes it.
///
/// Attach to: the Player GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerBallInteraction : MonoBehaviour
{
    [Header("Shoot (may or may not hit the goal)")]
    [Tooltip("Ball speed of a shot (metres per second).")]
    [SerializeField] private float shotSpeed = 20f;
    [Tooltip("Chance of an on-target shot from point blank.")]
    [Range(0f, 1f)]
    [SerializeField] private float accuracyClose = 0.85f;
    [Tooltip("Chance of an on-target shot from Max Shot Distance or further.")]
    [Range(0f, 1f)]
    [SerializeField] private float accuracyFar = 0.35f;
    [SerializeField] private float maxShotDistance = 25f;

    [Header("Pass (to the teammate in your joystick direction)")]
    [SerializeField] private float passSpeedPerMetre = 1.5f;
    [SerializeField] private float minPassSpeed = 7f;
    [SerializeField] private float maxPassSpeed = 22f;
    [SerializeField] private float maxPassDistance = 40f;
    [Tooltip("A teammate more than this many degrees off your joystick direction is ignored.")]
    [SerializeField] private float maxPassAngle = 70f;
    [Tooltip("Pass speed when there is no teammate to aim at.")]
    [SerializeField] private float fallbackPassSpeed = 11f;

    [Header("Through ball (to free space near the teammate)")]
    [Tooltip("How far ahead of the running teammate the ball is sent (metres).")]
    [SerializeField] private float throughLead = 6f;
    [SerializeField] private float throughLift = 0.8f;

    [Header("Clear")]
    [SerializeField] private float clearReach = 2.2f;
    [SerializeField] private float clearSpeed = 20f;
    [SerializeField] private float clearLiftSpeed = 8f;
    [Tooltip("Random sideways spread of a clear, in degrees.")]
    [SerializeField] private float clearSpread = 15f;

    private PlayerInputReader input;
    private PlayerPossession possession;
    private PlayerTeam team;
    private BallOwnership ballOwnership;
    private Rigidbody ballBody;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        possession = GetComponent<PlayerPossession>();
        team = GetComponent<PlayerTeam>();
        if (input == null)
            Debug.LogError("PlayerBallInteraction needs a PlayerInputReader on the same GameObject.", this);
    }

    private void Start()
    {
        ballOwnership = FindFirstObjectByType<BallOwnership>();
        if (ballOwnership != null) ballBody = ballOwnership.GetComponent<Rigidbody>();
        if (ballOwnership == null)
            Debug.LogWarning("PlayerBallInteraction: no BallOwnership found. Add one to the Ball.", this);
    }

    private void Update()
    {
        if (input == null || ballOwnership == null || team == null) return;

        BallState state = possession != null ? possession.State : BallState.Loose;

        if (state == BallState.WithMe)
        {
            if (input.ShootPressed) Shoot();
            else if (input.PassPressed) Pass(false);
            else if (input.ThroughPressed) Pass(true);
        }
        else
        {
            if (input.ClearPressed) Clear();
        }
    }

    // ---------------------------------------------------------------- helpers

    private Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

    // Direction of the joystick in the world (forward if the stick is neutral)
    private Vector3 InputDirection()
    {
        Vector2 m = input.Move;
        Vector3 d = transform.right * m.x + transform.forward * m.y;
        d.y = 0f;
        return d.sqrMagnitude > 0.04f ? d.normalized : FlatForward();
    }

    private float FlatDistanceToBall()
    {
        Vector3 d = ballBody.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // You can strike the ball if you hold it (or, when testing with a forced mode, it is right beside you)
    private bool CanStrike()
    {
        if (ballBody == null) return false;
        if (ballOwnership.Holder == team) return true;
        return FlatDistanceToBall() <= 2.5f;
    }

    // Velocity that sends the ball from 'from' to 'to' with the given flat speed, allowing for gravity
    private static Vector3 VelocityToReach(Vector3 from, Vector3 to, float flatSpeed)
    {
        Vector3 flat = to - from;
        flat.y = 0f;
        float distance = flat.magnitude;
        if (distance < 0.1f) return Vector3.up * 2f;

        float t = distance / Mathf.Max(1f, flatSpeed);
        float vy = (to.y - from.y) / t + 0.5f * Mathf.Abs(Physics.gravity.y) * t;
        Vector3 v = flat / distance * flatSpeed;
        v.y = Mathf.Clamp(vy, -5f, 14f);
        return v;
    }

    // The teammate best matching the direction, or null
    private PlayerTeam PickTeammate(Vector3 wantedDirection)
    {
        PlayerTeam best = null;
        float bestScore = float.MaxValue;

        foreach (PlayerTeam p in PlayerTeam.All)
        {
            if (p == team || p.Team != team.Team) continue;

            Vector3 to = p.transform.position - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance < 1f || distance > maxPassDistance) continue;

            float angle = Vector3.Angle(wantedDirection, to);
            if (angle > maxPassAngle) continue;

            float score = angle + distance * 0.2f;
            if (score < bestScore)
            {
                bestScore = score;
                best = p;
            }
        }
        return best;
    }

    // ---------------------------------------------------------------- actions

    private void Shoot()
    {
        if (!CanStrike()) return;

        Vector3 ballPos = ballBody.position;
        GoalMarker goal = GoalMarker.FindAttackedBy(team.Team);
        Vector3 aim;

        if (goal != null)
        {
            Vector3 flatToGoal = goal.transform.position - ballPos;
            flatToGoal.y = 0f;
            float distanceFactor = Mathf.Clamp01(flatToGoal.magnitude / Mathf.Max(1f, maxShotDistance));
            float chance = Mathf.Lerp(accuracyClose, accuracyFar, distanceFactor);

            Vector3 right = goal.transform.right;
            Vector3 origin = goal.transform.position;

            if (Random.value < chance)
            {
                // On target: somewhere inside the goal mouth
                float x = Random.Range(-0.4f, 0.4f) * goal.Width;
                float y = Random.Range(0.15f, 0.85f) * goal.Height;
                aim = origin + right * x + Vector3.up * y;
            }
            else if (Random.value < 0.7f)
            {
                // Miss wide of a post
                float side = Random.value < 0.5f ? -1f : 1f;
                float x = side * (goal.Width * 0.5f + Random.Range(0.6f, 3f));
                float y = Random.Range(0.2f, goal.Height);
                aim = origin + right * x + Vector3.up * y;
            }
            else
            {
                // Miss over the bar
                float x = Random.Range(-0.5f, 0.5f) * goal.Width;
                float y = goal.Height + Random.Range(0.6f, 2.5f);
                aim = origin + right * x + Vector3.up * y;
            }
        }
        else
        {
            // No goal set up yet: shoot straight ahead
            aim = ballPos + FlatForward() * 20f + Vector3.up * 1f;
        }

        ballOwnership.Strike(team, VelocityToReach(ballPos, aim, shotSpeed));
    }

    private void Pass(bool through)
    {
        if (!CanStrike()) return;

        Vector3 ballPos = ballBody.position;
        Vector3 wanted = InputDirection();
        PlayerTeam mate = PickTeammate(wanted);

        if (mate == null)
        {
            // Nobody to aim at: send it along the joystick direction
            float speed = through ? fallbackPassSpeed * 1.3f : fallbackPassSpeed;
            Vector3 v = wanted * speed;
            v.y = through ? throughLift : 0.2f;
            ballOwnership.Strike(team, v);
            return;
        }

        Vector3 target = mate.transform.position;

        if (through)
        {
            // Free space ahead of the teammate, in the direction they are running
            Vector3 run = mate.Velocity;
            run.y = 0f;
            if (run.magnitude < 0.5f)
            {
                GoalMarker goal = GoalMarker.FindAttackedBy(team.Team);
                Vector3 toGoal = goal != null ? goal.transform.position - mate.transform.position : wanted;
                toGoal.y = 0f;
                run = toGoal.sqrMagnitude > 0.0001f ? toGoal : wanted;
            }
            target = mate.transform.position + run.normalized * throughLead;
        }

        Vector3 flat = target - ballPos;
        flat.y = 0f;
        float distance = flat.magnitude;
        if (distance < 0.1f) return;

        float passSpeed = Mathf.Clamp(distance * passSpeedPerMetre, minPassSpeed, maxPassSpeed);
        if (through) passSpeed = Mathf.Clamp(distance * passSpeedPerMetre * 1.15f, minPassSpeed + 2f, maxPassSpeed + 2f);

        Vector3 velocity = flat / distance * passSpeed;
        velocity.y = through ? throughLift : 0.2f;
        ballOwnership.Strike(team, velocity);
    }

    private void Clear()
    {
        if (ballBody == null) return;
        if (FlatDistanceToBall() > clearReach) return;

        // Away from our own goal if we have one, otherwise the way the body faces
        Vector3 dir;
        GoalMarker ownGoal = GoalMarker.FindDefendedBy(team.Team);
        if (ownGoal != null)
        {
            dir = ballBody.position - ownGoal.transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : FlatForward();
        }
        else
        {
            dir = FlatForward();
        }

        dir = Quaternion.Euler(0f, Random.Range(-clearSpread, clearSpread), 0f) * dir;

        Vector3 velocity = dir * clearSpeed + Vector3.up * clearLiftSpeed;
        ballOwnership.Strike(team, velocity);
    }
}
