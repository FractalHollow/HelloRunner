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
            $"Best Distance: {bestDistanceM:N0} m\n" +
            $"Best Score: {bestScore:N0}\n" +
            $"\n" +
            $"Best Flips (Single Run): {bestFlipsInRun:N0}\n" +
            $"Best No-Hit Distance (Single Run): {bestNoHitM:N0} m\n" +
            $"Best Hard Mode Distance (Single Run): {bestHardModeM:N0} m\n" +
            $"\n" +
            $"Lifetime Distance: {lifetimeDistanceM:N0} m\n" +
            $"Lifetime Embers Earned: {lifetimeEmbersEarned:N0}\n" +
            $"Runs Played: {runsPlayed:N0}\n" +
            $"Speed Mod Runs: {speedModRuns:N0}\n" +
            $"Hazards Mod Runs: {hazardsModRuns:N0}\n" +
            $"\n" +
            $"Achievements Unlocked: {unlockedAchievements:N0} / {totalAchievements:N0}\n";
    }

}
