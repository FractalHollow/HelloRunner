using UnityEngine;

public static class PrestigeManager
{
    const string K_PrestigeLevel = "prestige_level";

    // Existing keys in your project
    const string K_StoreUnlocked = "store_unlocked";
    const string K_WispsTotal    = "wisps_total";

    const string K_ModsUnlocked  = "mods_unlocked";
    const string K_ModSpeedOn    = "mod_speed_on";
    const string K_ModHazardsOn  = "mod_hazards_on";

    // Best distance key you already use/confirmed
    const string K_BestDistanceM = "best_distance_m";

    // Need gate not to persist after prestige
    const string K_PrestigeBestDistanceM = "prestige_best_distance_m";

    // Tunable gate
    public const int PrestigeDistanceRequirementM = 200;


    public static int BestDistanceThisPrestigeM =>
        PlayerPrefs.GetInt(K_PrestigeBestDistanceM, 0);

    public static int Level
    {
        get => PlayerPrefs.GetInt(K_PrestigeLevel, 0);
        set
        {
            PlayerPrefs.SetInt(K_PrestigeLevel, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static float ScoreMult => Mathf.Pow(1.5f, Level);
    public static float WispMult  => Mathf.Pow(1.5f, Level);

    public static int BestDistanceM
    {
        get => Mathf.FloorToInt(PlayerPrefs.GetFloat(K_BestDistanceM, 0f));
    }

    public static bool CanPrestige()
    {
        return BestDistanceThisPrestigeM >= PrestigeDistanceRequirementM;
    }


    public static void DoPrestige()
    {
        // 1) Increase prestige level (persists)
        int newLevel = Level + 1;
        PlayerPrefs.SetInt(K_PrestigeLevel, newLevel);

        // 2) Reset embers bank (fresh start)
        PlayerPrefs.SetInt(K_WispsTotal, 0);

        // 3) Re-lock upgrades system (forces paying 25 again)
        PlayerPrefs.SetInt(K_StoreUnlocked, 0);

        // 4) Re-lock run modifiers + toggles
        PlayerPrefs.SetInt(K_ModsUnlocked, 0);
        PlayerPrefs.SetInt(K_ModSpeedOn, 0);
        PlayerPrefs.SetInt(K_ModHazardsOn, 0);

        // 5) Reset all upgrade levels (data-driven)
        var defs = Resources.LoadAll<UpgradeDef>("Upgrades");
        foreach (var def in defs)
        {
            if (!def || string.IsNullOrEmpty(def.id)) continue;
            PlayerPrefs.SetInt($"upgrade_{def.id}", 0);
        }

        // 6) Reset best distance this prestige
         PlayerPrefs.SetInt(K_PrestigeBestDistanceM, 0);

        // Do NOT touch:
        // - HighScore
        // - best_distance_m
        // - stat_*
        // - ach_unlocked_*, ach_claimed_*

        PlayerPrefs.Save();
    }
}
