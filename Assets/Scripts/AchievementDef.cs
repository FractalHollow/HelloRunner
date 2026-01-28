using UnityEngine;

[CreateAssetMenu(menuName = "FoxRunRest/AchievementDef")]
public class AchievementDef : ScriptableObject
{
    public string id;               // unique: "dist_500", "runs_10", etc.
    public string displayName;
    [TextArea] public string description;

    [Header("UI")]
    public int sortOrder = 0;

    public enum ProgressType
    {
        BestDistanceM,
        LifetimeDistanceM,
        RunsPlayed,
        LifetimeEmbersEarned,
        SpeedModRuns,
        HazardsModRuns,
        PrestigeLevel,
        FlipsInRun,
        LongestNoHitDistanceM,
        HardModeDistanceM
    }

    public ProgressType progressType;
    public int targetValue = 1;

    [Header("Reward")]
    public int rewardEmbers = 10;    // claimable
}
