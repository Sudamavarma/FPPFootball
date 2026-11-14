using UnityEngine;

public enum BallState { WithMe, Loose, WithOpponent, WithTeammate }

/// <summary>
/// The ball's three situations (from one player's point of view):
///   WithMe       - this player is holding the ball
///   Loose        - nobody has it
///   WithOpponent - an opponent is holding it
///
/// HOW IT WORKS
/// - The first player (or bot) to get close to a loose ball takes it. The ball then sticks to their feet
///   and follows them until something takes it away: a shot, pass, through ball, clear, or a tackle.
/// - While held, the ball is kinematic and its collider is off, so it can't snag on anything.
/// - After kicking, that player can't instantly pick the ball back up (short lockout).
///
/// Attach to: the Ball (needs a Rigidbody and a Collider).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallOwnership : MonoBehaviour
{
    [Header("Pick up (loose ball)")]
    [Tooltip("A player closer than this (flat metres) to a slow, low ball takes it.")]
    [SerializeField] private float pickupRadius = 1.1f;
    [Tooltip("Faster balls bounce off instead of being trapped.")]
    [SerializeField] private float maxPickupSpeed = 14f;
    [SerializeField] private float maxPickupHeight = 1.2f;
    [SerializeField] private float pickupLockSeconds = 0.35f;

    [Header("Carry (ball follows the holder)")]
    [Tooltip("How far in front of the holder's feet the ball sits (metres).")]
    [SerializeField] private float carryDistance = 0.8f;
    [SerializeField] private float carryFollowSpeed = 25f;

    public PlayerTeam Holder { get; private set; }
    public bool IsHeld => Holder != null;

    private Rigidbody body;
    private Collider ballCollider;
    private CharacterController holderController;
    private float radius = 0.15f;
    private PlayerTeam lockedPlayer;
    private float lockedUntil;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
        if (ballCollider != null) radius = Mathf.Max(0.05f, ballCollider.bounds.extents.y);
    }

    /// <summary>What the ball is doing, from this player's point of view.</summary>
    public BallState StateFor(PlayerTeam viewer)
    {
        if (Holder == null) return BallState.Loose;
        if (Holder == viewer) return BallState.WithMe;
        if (viewer != null && Holder.Team == viewer.Team) return BallState.WithTeammate;
        return BallState.WithOpponent;
    }

    /// <summary>Make this player the holder (works from loose, or to snatch it from someone).</summary>
    public void Possess(PlayerTeam player)
    {
        if (player == null) return;

        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        Holder = player;
        holderController = player.GetComponent<CharacterController>();
        body.isKinematic = true;
        if (ballCollider != null) ballCollider.enabled = false;
    }

    /// <summary>Kick the ball with this velocity (shot, pass, clear...). Works whether it is held or loose.</summary>
    public void Strike(PlayerTeam by, Vector3 velocity)
    {
        ReleaseInternal();
        body.linearVelocity = velocity;
        body.angularVelocity = Vector3.zero;
        Lock(by, pickupLockSeconds);
    }

    /// <summary>The tackler snatches the ball from an opponent holder. Returns true if it worked.</summary>
    public bool TryTackle(PlayerTeam tackler)
    {
        if (tackler == null || Holder == null || Holder.Team == tackler.Team) return false;

        PlayerTeam former = Holder;
        Possess(tackler);
        Lock(former, 0.7f);
        return true;
    }

    /// <summary>Put the ball back at a spot, stopped and loose (used for kickoff after a goal).</summary>
    public void ResetTo(Vector3 position)
    {
        ReleaseInternal();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = position;
        transform.position = position;
        lockedPlayer = null;
    }

    private void Lock(PlayerTeam player, float seconds)
    {
        lockedPlayer = player;
        lockedUntil = Time.time + seconds;
    }

    private void ReleaseInternal()
    {
        Holder = null;
        holderController = null;
        if (ballCollider != null) ballCollider.enabled = true;
        body.isKinematic = false;
    }

    private float FlatDistanceTo(Vector3 position)
    {
        Vector3 d = position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private void Update()
    {
        // The holder was destroyed or disabled while holding: let the ball go
        if (Holder == null && body.isKinematic)
        {
            ReleaseInternal();
            return;
        }

        if (Holder != null) return;

        if (body.linearVelocity.magnitude > maxPickupSpeed) return;
        if (transform.position.y > maxPickupHeight) return;

        PlayerTeam best = null;
        float bestDistance = pickupRadius;
        foreach (PlayerTeam p in PlayerTeam.All)
        {
            if (!p.CanPossess) continue;
            if (p == lockedPlayer && Time.time < lockedUntil) continue;

            float d = FlatDistanceTo(p.transform.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = p;
            }
        }

        if (best != null) Possess(best);
    }

    private void LateUpdate()
    {
        if (Holder == null) return;

        Vector3 forward = Holder.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        float groundY = holderController != null
            ? holderController.bounds.min.y
            : Holder.transform.position.y - 1f;

        Vector3 target = Holder.transform.position + forward * carryDistance;
        target.y = groundY + radius;

        Vector3 old = transform.position;
        Vector3 next = Vector3.Lerp(old, target, 1f - Mathf.Exp(-carryFollowSpeed * Time.deltaTime));
        transform.position = next;

        // Roll the ball visually as it moves
        Vector3 delta = next - old;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.000001f)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, delta).normalized;
            float angle = delta.magnitude / radius * Mathf.Rad2Deg;
            transform.Rotate(axis, angle, Space.World);
        }
    }
}
