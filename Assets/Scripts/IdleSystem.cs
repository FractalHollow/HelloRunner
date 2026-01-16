using UnityEngine;
using System;
using System.Collections.Generic;

public static class IdleSystem
{
    const string KEY_LAST_TS = "idle_last_ts";

    // --- These are your BASE values (before upgrades / prestige) ---
    public static float baseHoursCap = 8f;
    public static int baseEmbersPerHour = 20;

    // --- PlayerPrefs keys (must match your upgrade ids) ---
    const string UPGRADE_IDLE_RATE_ID = "idle_rate";
    const string UPGRADE_IDLE_CAP_ID  = "idle_capacity";

    // Your existing upgrade storage format: "upgrade_<id>"
    static string KUpgradeLevel(string id) => $"upgrade_{id}";

    // Prestige key (change if your project uses a different name)
    const string KEY_PRESTIGE_LEVEL = "prestige_level";

    // Prestige idle bonus: +5% per prestige level (tweakable)
    public static float prestigeIdleBonusPerLevel = 0.05f;

    static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // --- Cached UpgradeDef lookups (safe + avoids repeated Resources scans) ---
    static bool _defsLoaded = false;
    static Dictionary<string, UpgradeDef> _defsById;

    static void EnsureDefsLoaded()
    {
        if (_defsLoaded) return;
        _defsLoaded = true;

        _defsById = new Dictionary<string, UpgradeDef>(StringComparer.Ordinal);
        var defs = Resources.LoadAll<UpgradeDef>("Upgrades");
        foreach (var d in defs)
        {
            if (!d || string.IsNullOrEmpty(d.id)) continue;
            if (!_defsById.ContainsKey(d.id))
                _defsById.Add(d.id, d);
        }
    }

    static UpgradeDef GetDef(string id)
    {
        EnsureDefsLoaded();
        if (_defsById != null && _defsById.TryGetValue(id, out var def))
            return def;
        return null;
    }

    static int GetUpgradeLevel(string id)
    {
        return PlayerPrefs.GetInt(KUpgradeLevel(id), 0);
    }

    static int GetPrestigeLevel()
    {
        return PlayerPrefs.GetInt(KEY_PRESTIGE_LEVEL, 0);
    }

    // -------------------------
    // Effective tuning helpers
    // -------------------------

    public static float GetEffectiveHoursCap()
    {
        // base + capacity bonus (from UpgradeDef V0 at current level)
        float cap = baseHoursCap;

        int lvl = GetUpgradeLevel(UPGRADE_IDLE_CAP_ID);
        if (lvl > 0)
        {
            var def = GetDef(UPGRADE_IDLE_CAP_ID);
            if (def != null)
            {
                // You authored V0 per tier as the BONUS hours for that level (absolute)
                float bonusHours = def.GetV0(lvl);
                if (bonusHours > 0f) cap += bonusHours;
            }
        }

        // safety
        return Mathf.Clamp(cap, 0f, 999f);
    }

    public static float GetEffectiveEmbersPerHour()
    {
        // base + rate bonus (from UpgradeDef V0 at current level), then prestige multiplier
        float rate = baseEmbersPerHour;

        int lvl = GetUpgradeLevel(UPGRADE_IDLE_RATE_ID);
        if (lvl > 0)
        {
            var def = GetDef(UPGRADE_IDLE_RATE_ID);
            if (def != null)
            {
                float bonus = def.GetV0(lvl); // authored as absolute bonus per hour at that level
                if (bonus > 0f) rate += bonus;
            }
        }

        // prestige bonus multiplier (e.g. +5% per prestige)
        int p = GetPrestigeLevel();
        float mult = 1f + (p * prestigeIdleBonusPerLevel);

        // safety
        rate = Mathf.Max(0f, rate * mult);
        return rate;
    }

    // -------------------------
    // Public API (your original)
    // -------------------------

    public static void EnsureStartStamp()
    {
        if (!PlayerPrefs.HasKey(KEY_LAST_TS))
            PlayerPrefs.SetString(KEY_LAST_TS, NowUnix().ToString());
    }

    public static int GetClaimableWisps()
    {
        long last = long.Parse(PlayerPrefs.GetString(KEY_LAST_TS, NowUnix().ToString()));
        long now = NowUnix();

        float hoursCap = GetEffectiveHoursCap();
        float hours = Mathf.Clamp((now - last) / 3600f, 0f, hoursCap);

        float ratePerHour = GetEffectiveEmbersPerHour();

        // floor to int like before
        int claimable = Mathf.FloorToInt(hours * ratePerHour);
        return Mathf.Max(0, claimable);
    }

    public static void Claim()
    {
        PlayerPrefs.SetString(KEY_LAST_TS, NowUnix().ToString());
        PlayerPrefs.Save();
    }

    // Optional helper for debugging / UI display
    public static float GetHoursSinceLastClaim_Uncapped()
    {
        long last = long.Parse(PlayerPrefs.GetString(KEY_LAST_TS, NowUnix().ToString()));
        long now = NowUnix();
        return Mathf.Max(0f, (now - last) / 3600f);
    }
}
