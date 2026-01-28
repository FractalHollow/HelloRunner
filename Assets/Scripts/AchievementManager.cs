using System.Collections.Generic;
using UnityEngine;


public class AchievementManager : MonoBehaviour
{
    public static AchievementManager I { get; private set; }
    GameManager gm;


    [Header("Toast (Game Over)")]
    public AchievementToast toast; // optional: will auto-find if null

    private readonly List<AchievementDef> defs = new List<AchievementDef>();

    void Awake()
    {
        // Singleton
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadDefs();

        EnsureToast();

        gm = FindFirstObjectByType<GameManager>();

    }

    void EnsureToast()
    {
        if (toast) return;

        // Unity 6: can include inactive objects
        toast = FindFirstObjectByType<AchievementToast>(FindObjectsInactive.Include);
        Debug.Log($"[Achievements] Auto-found toast: {(toast ? toast.name : "NONE")}");
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

    void SetUnlocked(string id) => PlayerPrefs.SetInt(KUnlocked(id), 1);
    void SetClaimed(string id) => PlayerPrefs.SetInt(KClaimed(id), 1);

    // --- Progress reading ---
    public int GetProgress(AchievementDef def, int bestDistanceM, int runDistanceM, int runScore, int runEmbersEarned)
    {
        if (def == null) return 0;

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

            case AchievementDef.ProgressType.PrestigeLevel:
                return PrestigeManager.Level;

            case AchievementDef.ProgressType.FlipsInRun:
                return PlayerPrefs.GetInt("best_flips_in_run", 0);

            case AchievementDef.ProgressType.LongestNoHitDistanceM:
                return PlayerPrefs.GetInt("best_nohit_m", 0);

            case AchievementDef.ProgressType.HardModeDistanceM:
                return PlayerPrefs.GetInt("best_hardmode_m", 0);

            default:
                // If you add new enum values later and forget to update this,
                // we still compile and just treat as 0 progress until implemented.
                return 0;
        }
    }

    public bool IsComplete(AchievementDef def, int bestDistanceM, int runDistanceM, int runScore, int runEmbersEarned)
    {
        int p = GetProgress(def, bestDistanceM, runDistanceM, runScore, runEmbersEarned);
        return p >= def.targetValue;
    }

    // Call this ONLY at GameOver
    public List<AchievementDef> EvaluateUnlocksOnGameOver(int bestDistanceM, int runDistanceM, int runScore, int runEmbersEarned)
    {
        var newlyUnlocked = new List<AchievementDef>();

        Debug.Log($"[Achievements] Evaluate @GameOver | best={bestDistanceM} runDist={runDistanceM} runEmbers={runEmbersEarned}");

        foreach (var def in defs)
        {
            if (!def || string.IsNullOrEmpty(def.id)) continue;
            if (IsUnlocked(def.id)) continue;

            if (IsComplete(def, bestDistanceM, runDistanceM, runScore, runEmbersEarned))
            {
                SetUnlocked(def.id);
                newlyUnlocked.Add(def);
                Debug.Log($"[Achievements] UNLOCKED: {def.id} ({def.displayName})");
            }
        }

        Debug.Log($"[Achievements] Newly unlocked count: {newlyUnlocked.Count}");

        if (newlyUnlocked.Count > 0)
        {
            PlayerPrefs.Save();

            EnsureToast();

            Debug.Log($"[Achievements] toast is {(toast ? "SET" : "NULL")}");
            if (toast)
            {
                toast.ShowUnlocked(newlyUnlocked);
                Debug.Log("[Achievements] toast.ShowUnlocked() CALLED");
            }
            else
            {
                Debug.LogWarning("[Achievements] toast is NULL - cannot show toast.");
            }
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
            gm.AddToWispsBank(reward);

        SetClaimed(def.id);
        PlayerPrefs.Save();
        return true;
    }

    public IReadOnlyList<AchievementDef> GetAllDefs() => defs;
}
