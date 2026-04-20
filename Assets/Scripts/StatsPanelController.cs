using TMPro;
using UnityEngine;

public class StatsPanelController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;     // assign Panel_Stats
    public TMP_Text bodyText;        // assign StatsBodyTxt

    [Header("Best Distance Key (match DistanceTracker)")]
    public string bestDistanceKey = "BestDistance"; // IMPORTANT: update to match your DistanceTracker PlayerPrefs key

    public void Open()
    {
        if (panelRoot) panelRoot.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

        public void Refresh()
    {
        if (!bodyText) return;

        int bestScore = PlayerPrefs.GetInt("HighScore", 0);

        // Best Distance: must match whatever DistanceTracker uses.
        int bestDistanceM = 0;
        if (PlayerPrefs.HasKey(bestDistanceKey))
        {
            // Support either int or float saves:
            bestDistanceM = Mathf.RoundToInt(
                PlayerPrefs.GetFloat(bestDistanceKey, PlayerPrefs.GetInt(bestDistanceKey, 0))
            );
        }

        // NEW: best-attempt stats used by run-based achievements
        int bestFlipsInRun = PlayerPrefs.GetInt("best_flips_in_run", 0);
        int bestNoHitM = PlayerPrefs.GetInt("best_nohit_m", 0);
        int bestHardModeM = PlayerPrefs.GetInt("best_hardmode_m", 0);

        int lifetimeDistanceM = StatsManager.LifetimeDistanceM;
        int lifetimeEmbersEarned = StatsManager.LifetimeEmbersEarned;
        int runsPlayed = StatsManager.RunsPlayed;
        int speedModRuns = StatsManager.SpeedModRuns;
        int hazardsModRuns = StatsManager.HazardsModRuns;
        int verticalModRuns = StatsManager.VerticalModRuns;
        int runsThisPrestige = PrestigeManager.RunAttemptsThisPrestige;

        int totalAchievements = 0;
        int unlockedAchievements = 0;

        if (AchievementManager.I != null)
        {
            var all = AchievementManager.I.GetAllDefs();
            totalAchievements = all.Count;

            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];
                if (def != null && AchievementManager.I.IsUnlocked(def.id))
                    unlockedAchievements++;
            }
        }

        bodyText.text =
            $"Best Distance: {UIIntFormatter.Format(bestDistanceM)} m\n" +
            $"Best Score: {UIIntFormatter.Format(bestScore)}\n" +
            $"\n" +
            $"Most Flips (Single Run): {UIIntFormatter.Format(bestFlipsInRun)}\n" +
            $"Best No-Hit Distance: {UIIntFormatter.Format(bestNoHitM)} m\n" +
            $"Best Hard Mode Distance: {UIIntFormatter.Format(bestHardModeM)} m\n" +
            $"\n" +
            $"Lifetime Distance: {UIIntFormatter.Format(lifetimeDistanceM)} m\n" +
            $"Lifetime Embers Earned: {UIIntFormatter.Format(lifetimeEmbersEarned)}\n" +
            $"Runs Played: {UIIntFormatter.Format(runsPlayed)}\n" +
            $"Runs This Prestige: {UIIntFormatter.Format(runsThisPrestige)}\n" +
            $"Speed Mod Runs: {UIIntFormatter.Format(speedModRuns)}\n" +
            $"Hazards Mod Runs: {UIIntFormatter.Format(hazardsModRuns)}\n" +
            $"Vertical Mod Runs: {UIIntFormatter.Format(verticalModRuns)}\n" +
            $"\n" +
            $"Achievements: {UIIntFormatter.Format(unlockedAchievements)} / {UIIntFormatter.Format(totalAchievements)}\n";
    }

}
