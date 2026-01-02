using UnityEngine;

public static class StatsManager
{
    // PlayerPrefs keys
    const string K_LifetimeDistanceM = "stat_lifetime_distance_m";
    const string K_LifetimeEmbersEarned = "stat_lifetime_embers_earned";
    const string K_RunsPlayed = "stat_runs_played";
    const string K_SpeedModRuns = "stat_speed_mod_runs";
    const string K_HazardsModRuns = "stat_hazards_mod_runs";

    public static int LifetimeDistanceM => PlayerPrefs.GetInt(K_LifetimeDistanceM, 0);
    public static int LifetimeEmbersEarned => PlayerPrefs.GetInt(K_LifetimeEmbersEarned, 0);
    public static int RunsPlayed => PlayerPrefs.GetInt(K_RunsPlayed, 0);
    public static int SpeedModRuns => PlayerPrefs.GetInt(K_SpeedModRuns, 0);
    public static int HazardsModRuns => PlayerPrefs.GetInt(K_HazardsModRuns, 0);

    public static void AddLifetimeDistance(int meters)
    {
        if (meters <= 0) return;
        PlayerPrefs.SetInt(K_LifetimeDistanceM, LifetimeDistanceM + meters);
    }

    public static void AddLifetimeEmbersEarned(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(K_LifetimeEmbersEarned, LifetimeEmbersEarned + amount);
    }

    public static void RecordRunStarted(bool speedOn, bool hazardsOn)
    {
        PlayerPrefs.SetInt(K_RunsPlayed, RunsPlayed + 1);

        if (speedOn) PlayerPrefs.SetInt(K_SpeedModRuns, SpeedModRuns + 1);
        if (hazardsOn) PlayerPrefs.SetInt(K_HazardsModRuns, HazardsModRuns + 1);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
