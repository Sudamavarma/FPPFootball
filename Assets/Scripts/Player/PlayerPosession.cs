using UnityEngine;

/// <summary>
/// Decides whether this player is ATTACKING (team has the ball) or DEFENDING.
/// The touch buttons and the kick types depend on this.
///
/// TEMPORARY RULE until teams exist: attacking = the ball is close to the player.
/// Once the owner adds teams and match logic, replace the Auto branch with real team possession.
///
/// Attach to: the Player GameObject.
/// Inspector: Ball can stay empty if your ball GameObject is named "Ball".
/// </summary>
public class PlayerPossession : MonoBehaviour
{
    public enum Mode { Auto, ForceAttacking, ForceDefending }

    [Tooltip("Auto = decided by distance to the ball. Use the Force options to test the buttons.")]
    [SerializeField] private Mode mode = Mode.Auto;
    [SerializeField] private Transform ball;
    [Tooltip("Becomes ATTACKING when the ball is closer than this (metres).")]
    [SerializeField] private float gainRange = 3f;
    [Tooltip("Becomes DEFENDING again only when the ball is farther than this. Must be bigger than Gain Range, so the buttons don't flicker.")]
    [SerializeField] private float loseRange = 4.5f;

    public bool IsAttacking { get; private set; }

    private void Start()
    {
        if (ball == null)
        {
            GameObject found = GameObject.Find("Ball");
            if (found != null) ball = found.transform;
            else Debug.LogWarning("PlayerPossession: no ball found. Assign it in the Inspector.", this);
        }
    }

    private void Update()
    {
        switch (mode)
        {
            case Mode.ForceAttacking:
                IsAttacking = true;
                return;
            case Mode.ForceDefending:
                IsAttacking = false;
                return;
        }

        if (ball == null) { IsAttacking = false; return; }

        Vector3 offset = ball.position - transform.position;
        offset.y = 0f;
        float sqr = offset.sqrMagnitude;

        if (IsAttacking)
            IsAttacking = sqr <= loseRange * loseRange;
        else
            IsAttacking = sqr <= gainRange * gainRange;
    }
}
