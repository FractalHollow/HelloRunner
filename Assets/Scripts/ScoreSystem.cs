using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    [Header("Source")]
    public DistanceTracker tracker;

    [Header("Multipliers (>=1)")]
    public float baseMultiplier     = 1f;
    public float upgradeMultiplier  = 1f;
    public float modifierMultiplier = 1f;
    public float prestigeMultiplier = 1f;

    void Awake()
    {
        // Self-heal bad values
        if (baseMultiplier     <= 0f) baseMultiplier     = 1f;
        if (upgradeMultiplier  <= 0f) upgradeMultiplier  = 1f;
        if (modifierMultiplier <= 0f) modifierMultiplier = 1f;
        if (prestigeMultiplier <= 0f) prestigeMultiplier = 1f;
    }

    public float TotalMultiplier =>
        Mathf.Max(0.01f, baseMultiplier * upgradeMultiplier * modifierMultiplier * prestigeMultiplier);

    public int CurrentScore =>
        Mathf.FloorToInt(((tracker ? tracker.distance : 0f) * TotalMultiplier));
}
