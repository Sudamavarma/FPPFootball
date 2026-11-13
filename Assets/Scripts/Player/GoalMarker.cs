using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a goal so shots, clears and bots know where the goals are.
/// Place the object at the CENTRE OF THE GOAL LINE on the ground. Its red arrow (local X) should run
/// along the goal mouth. Width and Height are the size of the opening in metres.
/// Defended By = which team defends this goal (the other team shoots at it).
///
/// This is only a marker (no collider). Real goal frames and scoring come later.
/// </summary>
public class GoalMarker : MonoBehaviour
{
    public static readonly List<GoalMarker> All = new List<GoalMarker>();

    [SerializeField] private TeamId defendedBy = TeamId.Away;
    [SerializeField] private float width = 6f;
    [SerializeField] private float height = 2.2f;

    public TeamId DefendedBy => defendedBy;
    public float Width => width;
    public float Height => height;

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

    private void OnDrawGizmos()
    {
        Gizmos.color = defendedBy == TeamId.Home ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.3f, 0.2f);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * (height * 0.5f), transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, 0.1f));
        Gizmos.matrix = old;
    }
}
