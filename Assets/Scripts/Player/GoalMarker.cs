using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a goal: where it is, how big it is, and which team defends it.
///
/// Place the object at the CENTRE OF THE GOAL LINE on the ground.
///   - Local X (red arrow) runs along the goal mouth.
///   - Local Z (blue arrow) points BEHIND the goal, out of the pitch.
/// "Build Goals And Scoreboard" sets the rotation for you.
///
/// MatchManager counts a goal when the ball is inside the goal volume (past the line, between the posts,
/// under the bar), whether it was shot in or carried in.
/// Defended By = which team defends this goal (the other team scores in it).
/// </summary>
public class GoalMarker : MonoBehaviour
{
    public static readonly List<GoalMarker> All = new List<GoalMarker>();

    [SerializeField] private TeamId defendedBy = TeamId.Away;
    [SerializeField] private float width = 6f;
    [SerializeField] private float height = 2.2f;
    [Tooltip("How deep the net is behind the goal line (metres).")]
    [SerializeField] private float depth = 1.8f;

    public TeamId DefendedBy => defendedBy;
    public float Width => width;
    public float Height => height;
    public float Depth => depth;

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    /// <summary>The goal this team shoots at (defended by the other team).</summary>
    public static GoalMarker FindAttackedBy(TeamId team)
    {
        foreach (GoalMarker g in All)
            if (g.defendedBy != team) return g;
        return null;
    }

    /// <summary>The goal this team defends.</summary>
    public static GoalMarker FindDefendedBy(TeamId team)
    {
        foreach (GoalMarker g in All)
            if (g.defendedBy == team) return g;
        return null;
    }

    /// <summary>True if this point (the ball's centre) is inside the goal: over the line, between the posts, under the bar.</summary>
    public bool ContainsPoint(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        return Mathf.Abs(local.x) <= width * 0.5f
               && local.y >= -0.2f && local.y <= height
               && local.z >= 0.15f && local.z <= depth + 0.6f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = defendedBy == TeamId.Home ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.3f, 0.2f);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(new Vector3(0f, height * 0.5f, depth * 0.5f), new Vector3(width, height, depth));
        Gizmos.matrix = old;
    }
}
