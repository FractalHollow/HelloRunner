using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDef", menuName = "Game/Upgrade Definition")]
public class UpgradeDef : ScriptableObject
{
    [Header("Identity")]
    public string id;             // stable key: "shield", "magnet", etc.
    public string displayName;    // shown in UI
    [TextArea] public string description;   // fallback static description (optional)
    [Tooltip("Optional: use {v0}, {v1}, {v2} placeholders to auto-fill per-tier text.")]
    public string descriptionTemplate;

    [Header("Economy")]
    [Tooltip("Cost of Tier 1")]
    public int baseCost = 50;
    [Tooltip("Cost growth per tier. TierN = baseCost * costScale^(N-1)")]
    public float costScale = 1.5f;
    [Tooltip("Maximum purchasable level/tiers for this upgrade.")]
    public int maxLevel = 5; // kept for backward-compatibility (aka maxTier)

    [Header("Progress Gating")]
    [Tooltip("Minimum best distance needed for this upgrade to appear in the shop.")]
    public int unlockDistance = 0;

    [Header("Dependencies (optional)")]
    [Tooltip("Require other upgrades to be at certain tiers before this one is purchasable.")]
    public Dependency[] dependencies;
    [System.Serializable]
    public struct Dependency
    {
        public string upgradeId;
        public int minTier; // 1..N
    }

    [Header("Effect Payload (data-driven values per tier)")]
    [Tooltip("Primary channel (e.g., radius, charges, multiplier). One entry per tier.")]
    public float[] v0PerTier;
    [Tooltip("Secondary channel (e.g., pull speed, duration). One entry per tier.")]
    public float[] v1PerTier;
    [Tooltip("Tertiary channel (e.g., max chase speed, damage). One entry per tier.")]
    public float[] v2PerTier;

    // Type of effect; we’ll switch on this in GameManager.ApplyUpgrade later
    public enum EffectType
    {
        None,
        Shield,
        Magnet,
        SmallerHitbox,
        ComboBoost,

        // Future/advanced:
        ShieldRegen,
        InvulnAfterShield,
        EmberMultiplier,

        // Run modifiers / toggles:
        RunMod_Speed,
        RunMod_Projectiles,
        RunMod_EnemyScale,

        // New mechanics:
        PlayerProjectiles
    }
    public EffectType effectType;

    

    // ----------------- Helpers (safe defaults) -----------------

    /// <summary>Maximum tier (alias for maxLevel). Always >= 1.</summary>
    public int MaxTier => Mathf.Max(1, maxLevel);

    /// <summary>Cost for a given tier (1-based). Clamped to valid range.</summary>
    public int GetCostForTier(int tier)
    {
        int t = Mathf.Clamp(tier, 1, MaxTier);
        double cost = baseCost * System.Math.Pow(System.Math.Max(1.0, costScale), t - 1);
        return Mathf.Max(1, Mathf.RoundToInt((float)cost));
    }

    /// <summary>Get per-tier value from a channel array; if missing/short, returns last known or 0.</summary>
    public float GetV0(int tier) => GetValueFromArray(v0PerTier, tier);
    public float GetV1(int tier) => GetValueFromArray(v1PerTier, tier);
    public float GetV2(int tier) => GetValueFromArray(v2PerTier, tier);

    float GetValueFromArray(float[] arr, int tier)
    {
        if (arr == null || arr.Length == 0) return 0f;
        int idx = Mathf.Clamp(tier - 1, 0, arr.Length - 1);
        return arr[idx];
    }

    /// <summary>
    /// Utility: build a description string replacing {v0},{v1},{v2} with the tier values.
    /// Falls back to 'description' if template is empty.
    /// </summary>
    public string GetDescriptionForTier(int tier)
    {
        if (string.IsNullOrEmpty(descriptionTemplate))
            return description;

        string s = descriptionTemplate;
        s = s.Replace("{v0}", GetV0(tier).ToString("0.##"));
        s = s.Replace("{v1}", GetV1(tier).ToString("0.##"));
        s = s.Replace("{v2}", GetV2(tier).ToString("0.##"));
        return s;
    }

    /// <summary>
    /// Quick dependency check callback. Pass a function that returns the owned tier for an upgradeId.
    /// Example usage: def.AreDependenciesMet(id => PlayerPrefs.GetInt($"upgrade_{id}", 0));
    /// </summary>
    public bool AreDependenciesMet(System.Func<string, int> getTierById)
    {
        if (dependencies == null || dependencies.Length == 0) return true;
        foreach (var dep in dependencies)
        {
            int have = getTierById?.Invoke(dep.upgradeId) ?? 0;
            if (have < dep.minTier) return false;
        }
        return true;
    }
}
