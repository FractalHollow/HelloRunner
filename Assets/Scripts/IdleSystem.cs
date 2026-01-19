using UnityEngine;
using System;
using System.Collections.Generic;

public static class IdleSystem
{
    const string KEY_LAST_TS = "idle_last_ts";

    // Base tuning
    public static float baseHoursCap = 8f;
    public static int baseEmbersPerHour = 20;

    // Upgrade IDs (must match your UpgradeDef.id values exactly)
    const string ID_IDLE_RATE = "idle_rate";
    const string ID_IDLE_CAP  = "idle_capacity";

    // Prestige
    const string KEY_PRESTIGE_LEVEL = "prestige_level";
    public static float prestigeIdleBonusPerLevel = 0.05f; // +5% per prestige

    static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // Cached UpgradeDefs
    static bool _loaded;
    static Dictionary<string, UpgradeDef> _defs;

    static void EnsureDefsLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        _defs = new Dictionary<string, UpgradeDef>(StringComparer.Ordinal);
        var defs = Resources.LoadAll<UpgradeDef>("Upgrades");
        foreach (var d in defs)
        {
            if (!d || string.IsNullOrEmpty(d.id)) continue;
            if (!_defs.ContainsKey(d.id))
                _defs.Add(d.id, d);
        }
    }

    static UpgradeDef GetDef(string id)
    {
        EnsureDefsLoaded();
        if (_defs != null && _defs.TryGetValue(id, out var def)) return def;
        return null;
    }

    static int GetUpgradeLevel(string id) => PlayerPrefs.GetInt($"upgrade_{id}", 0);

    public static float GetEffectiveHoursCap()
    {
        float cap = baseHoursCap;

        int lvl = GetUpgradeLevel(ID_IDLE_CAP);
        if (lvl > 0)
        {
            var def = GetDef(ID_IDLE_CAP);
            if (def != null)
            {
                float bonus = def.GetV0(lvl); // authored as absolute "bonus hours" at that tier
                if (bonus > 0f) cap += bonus;
            }
        }

        return Mathf.Clamp(cap, 0f, 999f);
    }

    public static float GetEffectiveEmbersPerHour()
    {
        float rate = baseEmbersPerHour;

        int lvl = GetUpgradeLevel(ID_IDLE_RATE);
        if (lvl > 0)
        {
            var def = GetDef(ID_IDLE_RATE);
            if (def != null)
            {
                float bonus = def.GetV0(lvl); // authored as absolute "bonus per hour" at that tier
                if (bonus > 0f) rate += bonus;
            }
        }

        int prestige = PlayerPrefs.GetInt(KEY_PRESTIGE_LEVEL, 0);
        float mult = 1f + prestige * prestigeIdleBonusPerLevel;

        rate = Mathf.Max(0f, rate * mult);
        return rate;
    }

    public static void EnsureStartStamp()
    {
        if (!PlayerPrefs.HasKey(KEY_LAST_TS))
            PlayerPrefs.SetString(KEY_LAST_TS, NowUnix().ToString());
    }

    public static float GetStoredHours()
    {
        long last = long.Parse(PlayerPrefs.GetString(KEY_LAST_TS, NowUnix().ToString()));
        long now  = NowUnix();
        float cap = GetEffectiveHoursCap();
        return Mathf.Clamp((now - last) / 3600f, 0f, cap);
    }

    public static int GetClaimableWisps()
    {
        float hours = GetStoredHours();
        float rate  = GetEffectiveEmbersPerHour();
        return Mathf.Max(0, Mathf.FloorToInt(hours * rate));
    }

    public static void Claim()
    {
        PlayerPrefs.SetString(KEY_LAST_TS, NowUnix().ToString());
        PlayerPrefs.Save();
    }
}
