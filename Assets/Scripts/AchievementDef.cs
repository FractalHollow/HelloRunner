using UnityEngine;

[CreateAssetMenu(menuName = "FoxRunRest/AchievementDef")]
public class AchievementDef : ScriptableObject
{
    public string id;               // unique: "dist_500", "runs_10", etc.
    public string displayName;
    [TextArea] public string description;

    public enum ProgressType
    {
        BestDistanceM,
        LifetimeDistanceM,
        RunsPlayed,
        LifetimeEmbersEarned,
        SpeedModRuns,
        HazardsModRuns
    }

    public ProgressType progressType;
    public int targetValue = 1;

    [Header("Reward")]
    public int rewardEmbers = 10;    // claimable
}
