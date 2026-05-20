using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public static class PlayGamesLeaderboardService
{
    const string LogPrefix = "[GPGS Leaderboards]";

    static bool initialized;

#if UNITY_ANDROID
    static bool authInProgress;
    static bool showLeaderboardsAfterAuth;
#endif

    public static void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

#if UNITY_ANDROID
        Debug.Log($"{LogPrefix} Initializing Play Games.");
        PlayGamesPlatform.DebugLogEnabled = Debug.isDebugBuild;
        PlayGamesPlatform.Activate();
        StartAutomaticAuth();
#else
        Debug.Log($"{LogPrefix} Play Games unavailable on this platform; leaderboard service is disabled.");
#endif
    }

    public static void SubmitGameOverScores(long highScore, long bestDistanceM, long prestigeLevel)
    {
        Initialize();

#if UNITY_ANDROID
        if (!IsAuthenticated())
        {
            Debug.Log($"{LogPrefix} Skipping score submission; player is not authenticated. " +
                      $"highScore={highScore}, bestDistanceM={bestDistanceM}, prestigeLevel={prestigeLevel}");
            return;
        }

        Debug.Log($"{LogPrefix} Submitting Game Over scores. " +
                  $"highScore={highScore}, bestDistanceM={bestDistanceM}, prestigeLevel={prestigeLevel}");

        SubmitScore("High Score", GPGSIds.leaderboard_high_score, highScore);
        SubmitScore("Longest Distance", GPGSIds.leaderboard_distance, bestDistanceM);
        SubmitScore("Prestige Level", GPGSIds.leaderboard_prestige_level, prestigeLevel);
#else
        Debug.Log($"{LogPrefix} Score submission ignored on this platform. " +
                  $"highScore={highScore}, bestDistanceM={bestDistanceM}, prestigeLevel={prestigeLevel}");
#endif
    }

    public static void ShowLeaderboards()
    {
        Initialize();

#if UNITY_ANDROID
        if (IsAuthenticated())
        {
            ShowLeaderboardsUi();
            return;
        }

        if (authInProgress)
        {
            showLeaderboardsAfterAuth = true;
            Debug.Log($"{LogPrefix} Auth is already in progress; leaderboards UI will open if auth succeeds.");
            return;
        }

        StartManualAuthForLeaderboards();
#else
        Debug.Log($"{LogPrefix} Built-in leaderboard UI unavailable on this platform.");
#endif
    }

#if UNITY_ANDROID
    static void StartAutomaticAuth()
    {
        if (authInProgress)
            return;

        authInProgress = true;
        Debug.Log($"{LogPrefix} Starting automatic Play Games auth check.");

        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            authInProgress = false;
            bool authenticated = IsAuthenticated();
            Debug.Log($"{LogPrefix} Automatic auth completed. status={status}, authenticated={authenticated}");

            if (!showLeaderboardsAfterAuth)
                return;

            showLeaderboardsAfterAuth = false;

            if (authenticated)
                ShowLeaderboardsUi();
            else
                StartManualAuthForLeaderboards();
        });
    }

    static void StartManualAuthForLeaderboards()
    {
        if (authInProgress)
        {
            showLeaderboardsAfterAuth = true;
            return;
        }

        authInProgress = true;
        Debug.Log($"{LogPrefix} Starting manual Play Games sign-in for leaderboards UI.");

        PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
        {
            authInProgress = false;
            bool authenticated = IsAuthenticated();
            Debug.Log($"{LogPrefix} Manual auth completed. status={status}, authenticated={authenticated}");

            if (authenticated)
                ShowLeaderboardsUi();
            else
                Debug.Log($"{LogPrefix} Leaderboards UI not shown because sign-in did not succeed.");
        });
    }

    static bool IsAuthenticated()
    {
        return PlayGamesPlatform.Instance.IsAuthenticated();
    }

    static void ShowLeaderboardsUi()
    {
        if (!IsAuthenticated())
        {
            Debug.Log($"{LogPrefix} Cannot show leaderboards UI; player is not authenticated.");
            return;
        }

        Debug.Log($"{LogPrefix} Opening built-in all-leaderboards UI.");
        PlayGamesPlatform.Instance.ShowLeaderboardUI((string)null, status =>
        {
            Debug.Log($"{LogPrefix} Built-in leaderboard UI closed. status={status}");
        });
    }

    static void SubmitScore(string label, string leaderboardId, long score)
    {
        if (score < 0L)
            score = 0L;

        Debug.Log($"{LogPrefix} Reporting {label}. leaderboardId={leaderboardId}, score={score}");
        PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, success =>
        {
            Debug.Log($"{LogPrefix} ReportScore callback. label={label}, leaderboardId={leaderboardId}, score={score}, success={success}");
        });
    }
#endif
}
