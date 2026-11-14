using TMPro;
using UnityEngine;

/// <summary>
/// Shows the score at the top of the screen and a "GOAL!" banner for a moment after each goal.
/// It only reads from MatchManager.
///
/// Attach to: the ScoreboardUI object on the Canvas.
/// (FPPFootball > Build Goals And Scoreboard builds it.)
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bannerText;

    private int shownHome = -1;
    private int shownAway = -1;

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        if (match == null) return;

        if (scoreText != null && (match.HomeScore != shownHome || match.AwayScore != shownAway))
        {
            shownHome = match.HomeScore;
            shownAway = match.AwayScore;
            scoreText.text = match.TeamName(TeamId.Home) + "   " + shownHome + " - " + shownAway + "   " + match.TeamName(TeamId.Away);
        }

        if (bannerText != null)
        {
            bool show = match.GoalPending;
            if (bannerText.gameObject.activeSelf != show) bannerText.gameObject.SetActive(show);
            if (show) bannerText.text = "GOAL!\n" + match.TeamName(match.LastScorer);
        }
    }
}
