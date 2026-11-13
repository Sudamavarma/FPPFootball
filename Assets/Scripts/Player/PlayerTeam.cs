using System.Collections.Generic;
using UnityEngine;

public enum TeamId { Home, Away }

/// <summary>
/// Says which team a player (or bot) is on. Every player, teammate and opponent needs one.
/// Also tracks how fast the player is moving (used by the through ball to lead a runner).
///
/// Attach to: the Player (Home), teammates (Home) and opponents (Away).
/// </summary>
public class PlayerTeam : MonoBehaviour
{
    public static readonly List<PlayerTeam> All = new List<PlayerTeam>();

    [SerializeField] private TeamId team = TeamId.Home;
    [Tooltip("Untick for dummy teammates that should not pick the ball up.")]
    [SerializeField] private bool canPossess = true;

    public TeamId Team => team;
    public bool CanPossess => canPossess;

    /// <summary>Flat (ground) velocity, smoothed.</summary>
    public Vector3 Velocity { get; private set; }

    private Vector3 lastPosition;

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        lastPosition = transform.position;
        Velocity = Vector3.zero;
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 v = (transform.position - lastPosition) / dt;
        v.y = 0f;
        Velocity = Vector3.Lerp(Velocity, v, Mathf.Clamp01(10f * dt));
        lastPosition = transform.position;
    }
}
