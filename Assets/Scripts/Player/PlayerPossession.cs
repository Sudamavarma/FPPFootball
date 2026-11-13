using UnityEngine;

/// <summary>
/// Tells the rest of the game what the ball is doing for THIS player:
///   State = WithMe / Loose / WithOpponent / WithTeammate.
///
/// IsAttacking is true only when the ball is WithMe. The touch buttons use it:
///   WithMe                       -> Shoot, Pass, Through
///   Loose / WithOpponent / other -> Clear, Defend, Tackle
///
/// Attach to: the Player GameObject (which also needs a PlayerTeam).
/// </summary>
public class PlayerPossession : MonoBehaviour
{
    public enum Mode { Auto, ForceAttacking, ForceDefending }

    [Tooltip("Auto = decided by the ball. Use the Force options to test the two button sets.")]
    [SerializeField] private Mode mode = Mode.Auto;
    [Tooltip("Leave empty to find the BallOwnership in the scene automatically.")]
    [SerializeField] private BallOwnership ballOwnership;

    public BallState State { get; private set; } = BallState.Loose;
    public bool IsAttacking { get; private set; }

    private PlayerTeam me;

    private void Awake()
    {
        me = GetComponent<PlayerTeam>();
        if (me == null)
            Debug.LogWarning("PlayerPossession: add a PlayerTeam to the Player (use FPPFootball > Set Up Possession And Opponent).", this);
    }

    private void Start()
    {
        if (ballOwnership == null)
            ballOwnership = FindFirstObjectByType<BallOwnership>();
        if (ballOwnership == null)
            Debug.LogWarning("PlayerPossession: no BallOwnership found. Add one to the Ball.", this);
    }

    private void Update()
    {
        if (mode == Mode.ForceAttacking)
        {
            State = BallState.WithMe;
            IsAttacking = true;
            return;
        }
        if (mode == Mode.ForceDefending)
        {
            State = BallState.WithOpponent;
            IsAttacking = false;
            return;
        }

        if (ballOwnership == null || me == null)
        {
            State = BallState.Loose;
            IsAttacking = false;
            return;
        }

        State = ballOwnership.StateFor(me);
        IsAttacking = State == BallState.WithMe;
    }
}
