using UnityEngine;
using System;

public static class IdleSystem
{
    const string KEY_LAST_TS = "idle_last_ts";
    public static float hoursCap = 8f;
    public static int wispsPerHour = 20;   // tweak later

    static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static void EnsureStartStamp()
    {
        if (!PlayerPrefs.HasKey(KEY_LAST_TS))
            PlayerPrefs.SetString(KEY_LAST_TS, NowUnix().ToString());
    }

    public static int GetClaimableWisps()
    {
        long last = long.Parse(PlayerPrefs.GetString(KEY_LAST_TS, NowUnix().ToString()));
        long now  = NowUnix();
        float hours = Mathf.Clamp((now - last) / 3600f, 0f, hoursCap);
        return Mathf.FloorToInt(hours * wispsPerHour);
    }

    public static void Claim()
    {
        PlayerPrefs.SetString(KEY_LAST_TS, NowUnix().ToString());
        PlayerPrefs.Save();
    }
}
