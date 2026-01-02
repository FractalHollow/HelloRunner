using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager I { get; private set; }

    [Header("Toast (Game Over)")]
    public AchievementToast toast; // assign in Inspector (toast object on GameOver panel)

    List<AchievementDef> defs = new List<AchievementDef>();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadDefs();
    }

    void LoadDefs()
    {
        defs.Clear();
        defs.AddRange(Resources.LoadAll<AchievementDef>("Achievements"));
        defs.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        Debug.Log($"[Achievements] Loaded {defs.Count} defs.");
    }

    // --- Persistence helpers ---
    string KUnlocked(string id) => $"ach_unlocked_{id}";
    string KClaimed(string id) => $"ach_claimed_{id}";

    public bool IsUnlocked(string id) => PlayerPrefs.GetInt(KUnlocked(id), 0) == 1;
    public bool IsClaimed(string id) => PlayerPrefs.GetInt(KClaimed(id), 0) == 1;

    void SetUnlocked(string id)
    {
        PlayerPrefs.SetInt(KUnlocked(id), 1);
    }

    void SetClaimed(string id)
    {
        PlayerPrefs.SetInt(KClaimed(id), 1);
    }

    // --- Progress reading ---
    public int GetProgress(AchievementDef def, int bestDistanceM, int runDistanceM, int runScore, int runEmbersEarned)
    {
        switch (def.progressType)
        {
            case AchievementDef.ProgressType.BestDistanceM:
                return bestDistanceM;

            case AchievementDef.ProgressType.LifetimeDistanceM:
                return StatsManager.LifetimeDistanceM;

            case AchievementDef.ProgressType.RunsPlayed:
                return StatsManager.RunsPlayed;

            case AchievementDef.ProgressType.LifetimeEmbersEarned:
                return StatsManager.LifetimeEmbersEarned;

            case AchievementDef.ProgressType.SpeedModRuns:
                return StatsManager.SpeedModRuns;

            case AchievementDef.ProgressType.HazardsModRuns:
                return StatsManager.HazardsModRuns;
        }
        return 0;
    }

    public bool IsComplete(AchievementDef def, int bestDistanceM, int runDistanceM, int runScore, int runEmbersEarned)
    {
        int p = GetProgress(def, bestDistanceM, runDistanceM, runScore, runEmbersEarned);
        return p >= def.targetValue;
    }

    // Call this ONLY at GameOver (as you requested)
    public List<AchievementDef> EvaluateUnlocksOnGameOver(int bestDistanceM, int runDistanceM, int runScore, int runEmbersEarned)
    {
        var newlyUnlocked = new List<AchievementDef>();

        foreach (var def in defs)
        {
            if (!def || string.IsNullOrEmpty(def.id)) continue;
            if (IsUnlocked(def.id)) continue;

            if (IsComplete(def, bestDistanceM, runDistanceM, runScore, runEmbersEarned))
            {
                SetUnlocked(def.id);
                newlyUnlocked.Add(def);
            }
        }

        if (newlyUnlocked.Count > 0)
        {
            PlayerPrefs.Save();
            toast?.ShowUnlocked(newlyUnlocked);
        }

        return newlyUnlocked;
    }

    // Claim flow
    public bool TryClaim(AchievementDef def, GameManager gm)
    {
        if (!def || gm == null) return false;
        if (!IsUnlocked(def.id)) return false;
        if (IsClaimed(def.id)) return false;

        int reward = Mathf.Max(0, def.rewardEmbers);
        if (reward > 0)
        {
            gm.AddToWispsBank(reward);
        }

        SetClaimed(def.id);
        PlayerPrefs.Save();
        return true;
    }

    public IReadOnlyList<AchievementDef> GetAllDefs() => defs;
}
