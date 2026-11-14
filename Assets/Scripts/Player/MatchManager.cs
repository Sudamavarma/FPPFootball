using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the score. A goal is counted when the ball is inside a GoalMarker's goal volume.
/// After a goal there is a short celebration pause, then the ball and every player go back to
/// where they started (kickoff).
///
/// Attach to: one empty GameObject called MatchManager.
/// (FPPFootball > Build Goals And Scoreboard creates it.)
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private string homeName = "HOME";
    [SerializeField] private string awayName = "AWAY";
    [Tooltip("Seconds between a goal and the kickoff reset.")]
    [SerializeField] private float resetDelay = 2.5f;

    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public TeamId LastScorer { get; private set; }
    public bool GoalPending { get; private set; }

    public string TeamName(TeamId team) { return team == TeamId.Home ? homeName : awayName; }

    private class StartPose
    {
        public PlayerTeam player;
        public Vector3 position;
        public Quaternion rotation;
    }

    private BallOwnership ball;
    private Vector3 ballStart;
    private readonly List<StartPose> starts = new List<StartPose>();
    private float resetAt;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        ball = FindFirstObjectByType<BallOwnership>();
        if (ball != null) ballStart = ball.transform.position;
        else Debug.LogWarning("MatchManager: no BallOwnership found, goals cannot be counted.", this);

        foreach (PlayerTeam p in PlayerTeam.All)
        {
            starts.Add(new StartPose
            {
                player = p,
                position = p.transform.position,
                rotation = p.transform.rotation
            });
        }
    }

    private void Update()
    {
        if (ball == null) return;

        if (GoalPending)
        {
            if (Time.time >= resetAt) ResetKickoff();
            return;
        }

        Vector3 ballPosition = ball.transform.position;
        foreach (GoalMarker goal in GoalMarker.All)
        {
            if (goal.ContainsPoint(ballPosition))
            {
                ScoreGoal(goal);
                return;
            }
        }
    }

    private void ScoreGoal(GoalMarker goal)
    {
        // The team that does NOT defend this goal scores in it
        LastScorer = goal.DefendedBy == TeamId.Home ? TeamId.Away : TeamId.Home;
        if (LastScorer == TeamId.Home) HomeScore++;
        else AwayScore++;

        GoalPending = true;
        resetAt = Time.time + resetDelay;
        Debug.Log("GOAL for " + TeamName(LastScorer) + "!  " + homeName + " " + HomeScore + " - " + AwayScore + " " + awayName);
    }

    private void ResetKickoff()
    {
        GoalPending = false;

        if (ball != null) ball.ResetTo(ballStart);

        foreach (StartPose s in starts)
        {
            if (s.player == null) continue;
            CharacterController cc = s.player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;   // a CharacterController must be off to teleport it
            s.player.transform.SetPositionAndRotation(s.position, s.rotation);
            if (cc != null) cc.enabled = true;
        }
    }

    /// <summary>Set the score back to 0 - 0 (for a restart button later).</summary>
    public void ResetScore()
    {
        HomeScore = 0;
        AwayScore = 0;
    }
}
