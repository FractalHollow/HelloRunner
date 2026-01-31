using UnityEngine;

public static class SaveSanitizer
{
    // Bump this if you ever change key formats / add migrations later.
    const int SAVE_VERSION = 1;
    const string KEY_SAVE_VERSION = "save_version";

    // Align to your actual PlayerPrefs keys
    const string KEY_PRESTIGE_LEVEL = "prestige_level";
    const string KEY_WISPS_TOTAL = "wisps_total";

    // Safety caps to prevent absurd values from breaking UI/economy
    const int MAX_PRESTIGE = 999;
    const int MAX_CURRENCY = 2_000_000_000;

    // Optional float key used elsewhere as a fallback
    const string KEY_BEST_DISTANCE_M = "best_distance_m";
    const float MAX_DISTANCE_SANITY = 1_000_000f;

    public static void Run()
    {
        bool changed = false;

        // --- Version key (future-proofing) ---
        int ver = PlayerPrefs.GetInt(KEY_SAVE_VERSION, 0);
        if (ver != SAVE_VERSION)
        {
            // If you ever need migrations later:
            // if (ver < 1) { ... }
            PlayerPrefs.SetInt(KEY_SAVE_VERSION, SAVE_VERSION);
            changed = true;
        }

        // --- Prestige clamp ---
        changed |= ClampIntKey(KEY_PRESTIGE_LEVEL, 0, MAX_PRESTIGE);

        // --- Currency clamp ---
        changed |= ClampIntKey(KEY_WISPS_TOTAL, 0, MAX_CURRENCY);

        // --- Upgrade tiers clamp (uses UpgradeDef.maxLevel/MaxTier) ---
        changed |= ClampUpgradeTierKeys();

        // --- Float sanity ---
        changed |= ClampFloatKey(KEY_BEST_DISTANCE_M, 0f, MAX_DISTANCE_SANITY);

        if (changed)
            PlayerPrefs.Save();
    }

    static bool ClampIntKey(string key, int min, int max)
    {
        if (!PlayerPrefs.HasKey(key)) return false;

        int v = PlayerPrefs.GetInt(key, 0);
        int clamped = Mathf.Clamp(v, min, max);

        if (clamped != v)
        {
            PlayerPrefs.SetInt(key, clamped);
            return true;
        }
        return false;
    }

    static bool ClampFloatKey(string key, float min, float max)
    {
        if (!PlayerPrefs.HasKey(key)) return false;

        float v = PlayerPrefs.GetFloat(key, 0f);

        if (float.IsNaN(v) || float.IsInfinity(v))
        {
            PlayerPrefs.SetFloat(key, min);
            return true;
        }

        float clamped = Mathf.Clamp(v, min, max);
        if (!Mathf.Approximately(clamped, v))
        {
            PlayerPrefs.SetFloat(key, clamped);
            return true;
        }
        return false;
    }

    static bool ClampUpgradeTierKeys()
    {
        bool changed = false;

        var defs = Resources.LoadAll<UpgradeDef>("Upgrades");
        if (defs == null || defs.Length == 0)
            return false;

        for (int i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            if (!def || string.IsNullOrEmpty(def.id)) continue;

            string key = $"upgrade_{def.id}";
            if (!PlayerPrefs.HasKey(key)) continue;

            int tier = PlayerPrefs.GetInt(key, 0);

            // Your tiers are stored as 0..N where N is def.MaxTier
            // But MaxTier is 1-based count, while stored tier is "level" count (0..MaxTier).
            // Example: MaxTier=5 => allowed stored values 0..5.
            int maxStoredTier = Mathf.Max(0, def.MaxTier);

            int clamped = Mathf.Clamp(tier, 0, maxStoredTier);
            if (clamped != tier)
            {
                PlayerPrefs.SetInt(key, clamped);
                changed = true;
            }
        }

        return changed;
    }
}
