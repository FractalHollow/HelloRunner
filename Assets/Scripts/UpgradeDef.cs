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
        ShieldIFrames,
        IdleRate,
        IdleCapacity,
    }
    public EffectType effectType;

    [Header("UI (Optional)")]
    [Tooltip("If set (size > 0), overrides description per tier. Index = tier (1..MaxTier).")]
    [TextArea]
    public string[] descriptionPerTier;

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
    /// Returns the description string for a given tier (1-based).
    /// Priority:
    /// 1) descriptionPerTier[tier] (if provided and non-empty)
    /// 2) descriptionTemplate (formatted)
    /// 3) description
    /// </summary>
    public string GetDescriptionForTier(int tier)
    {
        int t = Mathf.Clamp(tier, 1, MaxTier);

        // 1) Per-tier override (index is 1-based in intent; we store as 0-based array)
        if (descriptionPerTier != null && descriptionPerTier.Length > 0)
        {
            int idx = Mathf.Clamp(t - 1, 0, descriptionPerTier.Length - 1);
            string perTier = descriptionPerTier[idx];
            if (!string.IsNullOrEmpty(perTier))
            {
                // Allow per-tier strings to still use placeholders if you want.
                // (e.g. "Gain {v0_i} shield charge(s)." for tiers 1-2, etc.)
                return FormatTemplate(perTier, t);
            }
        }

        // 2) Template fallback
        if (!string.IsNullOrEmpty(descriptionTemplate))
            return FormatTemplate(descriptionTemplate, t);

        // 3) Static fallback
        return description;
    }

    string FormatTemplate(string template, int tier)
    {
        float v0 = GetV0(tier);
        float v1 = GetV1(tier);
        float v2 = GetV2(tier);

        string s = template;

        // generic numeric
        s = s.Replace("{v0}", v0.ToString("0.##"));
        s = s.Replace("{v1}", v1.ToString("0.##"));
        s = s.Replace("{v2}", v2.ToString("0.##"));

        // typed formats (rounded ints)
        s = s.Replace("{v0_i}", Mathf.RoundToInt(v0).ToString());
        s = s.Replace("{v1_i}", Mathf.RoundToInt(v1).ToString());
        s = s.Replace("{v2_i}", Mathf.RoundToInt(v2).ToString());

        // meters with 1 decimal
        s = s.Replace("{v0_m}", v0.ToString("0.0"));
        s = s.Replace("{v1_m}", v1.ToString("0.0"));
        s = s.Replace("{v2_m}", v2.ToString("0.0"));

        // percents as rounded ints (you control whether v0 is 10 or 0.10 in your data)
        s = s.Replace("{v0_pct}", Mathf.RoundToInt(v0).ToString());
        s = s.Replace("{v1_pct}", Mathf.RoundToInt(v1).ToString());
        s = s.Replace("{v2_pct}", Mathf.RoundToInt(v2).ToString());

        // simple pluralization helper for shield charges based on v0_i
        s = s.Replace("{charge_s}", (Mathf.RoundToInt(v0) == 1) ? "charge" : "charges");

        return s;
    }

    /// Dependency check callback. Pass a function that returns the owned tier for an upgradeId.
    /// Example usage: def.AreDependenciesMet(id => PlayerPrefs.GetInt($"upgrade_{id}", 0));
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
