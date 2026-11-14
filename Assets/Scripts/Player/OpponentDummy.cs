using UnityEngine;

/// <summary>
/// A simple test opponent so you can try the defend buttons and losing the ball.
/// - Ball loose: it chases the ball and picks it up.
/// - You have the ball: it runs at you, and now and then tries to tackle you.
/// - It has the ball: it dribbles toward your goal and shoots when close enough.
/// It is NOT real AI. Real bots come later.
///
/// Attach to: a capsule with a CharacterController and a PlayerTeam set to Away.
/// (FPPFootball > Set Up Possession And Opponent creates all of this for you.)
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerTeam))]
public class OpponentDummy : MonoBehaviour
{
    [Tooltip("Fallback target if no GoalMarker exists in the scene.")]
    [SerializeField] private Transform goalTarget;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float dribbleSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float gravity = -20f;

    [Header("Shooting")]
    [SerializeField] private float shootRange = 10f;
    [SerializeField] private float shootSpeed = 16f;
    [SerializeField] private float shootCooldown = 2f;

    [Header("Passing (so you can test Defend: intercepting a pass)")]
    [Tooltip("Needs a second opponent in the scene (FPPFootball > Add Second Opponent).")]
    [SerializeField] private bool passToTeammates = true;
    [SerializeField] private float passInterval = 3.5f;

    [Header("Tackling you")]
    [Tooltip("Untick to stop the opponent from ever tackling you.")]
    [SerializeField] private bool canTackle = true;
    [SerializeField] private float tackleRange = 1.6f;
    [SerializeField] private float tackleInterval = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float tackleChance = 0.5f;

    private CharacterController controller;
    private PlayerTeam team;
    private BallOwnership ballOwnership;
    private Rigidbody ballBody;
    private float verticalVelocity;
    private float nextShootTime;
    private float nextTackleTime;
    private float nextPassTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        team = GetComponent<PlayerTeam>();
    }

    private void Start()
    {
        ballOwnership = FindFirstObjectByType<BallOwnership>();
        if (ballOwnership != null) ballBody = ballOwnership.GetComponent<Rigidbody>();
        if (ballBody == null)
            Debug.LogWarning("OpponentDummy: no ball with BallOwnership found.", this);
    }

    private Vector3 GoalPosition()
    {
        GoalMarker marker = GoalMarker.FindAttackedBy(team.Team);
        if (marker != null) return marker.transform.position;
        if (goalTarget != null) return goalTarget.position;
        return transform.position + Vector3.back * 20f;
    }

    private void Update()
    {
        if (ballBody == null || ballOwnership == null) return;

        PlayerTeam holder = ballOwnership.Holder;
        bool iHaveBall = holder == team;

        Vector3 target;
        float stopDistance = 0.05f;
        float speed = moveSpeed;

        if (iHaveBall)
        {
            target = GoalPosition();
            speed = dribbleSpeed;
        }
        else if (holder != null)
        {
            target = holder.transform.position;   // press whoever has the ball
            stopDistance = 1.2f;
        }
        else
        {
            target = ballBody.position;
        }

        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        Vector3 direction = toTarget.magnitude > stopDistance ? toTarget.normalized : Vector3.zero;

        if (toTarget.sqrMagnitude > 0.0025f)
        {
            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (iHaveBall)
        {
            if (!TryShoot()) TryPass();
        }
        else if (holder != null) TryTackle(holder);
    }

    private bool TryShoot()
    {
        if (Time.time < nextShootTime) return false;

        Vector3 goal = GoalPosition();
        Vector3 flat = goal - ballBody.position;
        flat.y = 0f;
        if (flat.magnitude > shootRange) return false;

        // Aim a little off-centre so it is not a perfect shot
        Vector3 right = Vector3.Cross(Vector3.up, flat.normalized);
        Vector3 aim = goal + right * Random.Range(-2.5f, 2.5f);
        Vector3 direction = aim - ballBody.position;
        direction.y = 0f;

        Vector3 velocity = direction.normalized * shootSpeed + Vector3.up * 2.5f;
        ballOwnership.Strike(team, velocity);
        nextShootTime = Time.time + shootCooldown;
        return true;
    }

    // Pass to the nearest teammate (another opponent), so the ball travels through the air for you to intercept
    private void TryPass()
    {
        if (!passToTeammates || Time.time < nextPassTime) return;

        PlayerTeam mate = null;
        float best = float.MaxValue;
        foreach (PlayerTeam p in PlayerTeam.All)
        {
            if (p == team || p.Team != team.Team || !p.CanPossess) continue;

            Vector3 to = p.transform.position - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance < 4f || distance > 22f) continue;
            if (distance < best)
            {
                best = distance;
                mate = p;
            }
        }
        if (mate == null) return;

        Vector3 direction = mate.transform.position - ballBody.position;
        direction.y = 0f;
        float speed = Mathf.Clamp(best * 1.5f, 7f, 16f);
        ballOwnership.Strike(team, direction.normalized * speed + Vector3.up * 0.3f);
        nextPassTime = Time.time + passInterval;
    }

    private void TryTackle(PlayerTeam holder)
    {
        if (!canTackle || holder.Team == team.Team) return;
        if (Time.time < nextTackleTime) return;

        Vector3 to = holder.transform.position - transform.position;
        to.y = 0f;
        if (to.magnitude > tackleRange) return;

        nextTackleTime = Time.time + tackleInterval;
        if (Random.value < tackleChance) ballOwnership.TryTackle(team);
    }
}
