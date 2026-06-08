using System.Collections;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

public sealed class PlayReviewPromptManager : MonoBehaviour
{
    const string SessionCountKey = "review_session_count";
    const string FlowCompletedKey = "review_flow_completed";

    const int MinimumCompletedRuns = 5;
    const int MinimumSessions = 2;
    const int MinimumClaimedAchievements = 3;
    const float SceneSettleDelaySeconds = 0.5f;

    static PlayReviewPromptManager instance;
    static int preservedSessionCount;
    static int preservedFlowCompleted;
    static bool hasPreservedResetState;

    bool migrationSession;
    bool applicationPaused;
    bool requestPending;
    Coroutine requestCoroutine;

#if UNITY_ANDROID && !UNITY_EDITOR
    ReviewManager reviewManager;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance) return;

        var go = new GameObject(nameof(PlayReviewPromptManager));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PlayReviewPromptManager>();
    }

    void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        RecordSession();
    }

    void OnEnable()
    {
        PrestigeManager.PrestigeCompleted += HandlePrestigeCompleted;
        AchievementManager.AchievementRewardClaimed += HandleAchievementRewardClaimed;
    }

    void OnDisable()
    {
        PrestigeManager.PrestigeCompleted -= HandlePrestigeCompleted;
        AchievementManager.AchievementRewardClaimed -= HandleAchievementRewardClaimed;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
    }

    void RecordSession()
    {
        bool hasSessionHistory = PlayerPrefs.HasKey(SessionCountKey);
        int previousSessions = Mathf.Max(0, PlayerPrefs.GetInt(SessionCountKey, 0));

        if (!hasSessionHistory)
        {
            previousSessions = StatsManager.RunsPlayed > 0 ? 1 : 0;
            migrationSession = true;
        }

        int currentSessions = previousSessions == int.MaxValue
            ? int.MaxValue
            : previousSessions + 1;

        PlayerPrefs.SetInt(SessionCountKey, currentSessions);
        if (!PlayerPrefs.HasKey(FlowCompletedKey))
            PlayerPrefs.SetInt(FlowCompletedKey, 0);
        PlayerPrefs.Save();

        Debug.Log(
            $"[PlayReview] Session recorded: {currentSessions}. " +
            $"Migration session: {migrationSession}.");
    }

    void HandlePrestigeCompleted()
    {
        EvaluateTrigger("prestige");
    }

    void HandleAchievementRewardClaimed()
    {
        EvaluateTrigger("achievement reward claim");
    }

    void EvaluateTrigger(string source)
    {
        if (migrationSession)
        {
            Debug.Log($"[PlayReview] Ignoring {source} during session migration.");
            return;
        }

        if (PlayerPrefs.GetInt(FlowCompletedKey, 0) == 1)
        {
            Debug.Log($"[PlayReview] Ignoring {source}; review flow already completed.");
            return;
        }

        int runs = StatsManager.RunsPlayed;
        int sessions = PlayerPrefs.GetInt(SessionCountKey, 0);
        int claimedAchievements = GetClaimedAchievementCount();
        bool milestoneReached =
            PrestigeManager.Level >= 1 ||
            claimedAchievements >= MinimumClaimedAchievements;

        if (runs < MinimumCompletedRuns ||
            sessions < MinimumSessions ||
            !milestoneReached)
        {
            Debug.Log(
                $"[PlayReview] {source} not eligible. " +
                $"Runs={runs}/{MinimumCompletedRuns}, " +
                $"Sessions={sessions}/{MinimumSessions}, " +
                $"Prestige={PrestigeManager.Level}, " +
                $"Claims={claimedAchievements}/{MinimumClaimedAchievements}.");
            return;
        }

        QueueReviewRequest(source);
    }

    int GetClaimedAchievementCount()
    {
        if (AchievementManager.I != null)
            return AchievementManager.I.ClaimedCount;

        int count = 0;
        var defs = Resources.LoadAll<AchievementDef>("Achievements");
        foreach (var def in defs)
        {
            if (!def || string.IsNullOrEmpty(def.id)) continue;
            if (PlayerPrefs.GetInt($"ach_claimed_{def.id}", 0) == 1)
                count++;
        }
        return count;
    }

    void QueueReviewRequest(string source)
    {
        if (requestPending)
        {
            Debug.Log($"[PlayReview] Request already pending; coalesced {source} trigger.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        requestPending = true;
        requestCoroutine = StartCoroutine(ProcessReviewRequest());
        Debug.Log($"[PlayReview] Queued review flow after {source}.");
#else
        Debug.Log(
            $"[PlayReview] Eligible after {source}; Google Play review flow " +
            "is only launched in an Android player build.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator ProcessReviewRequest()
    {
        if (reviewManager == null)
            reviewManager = new ReviewManager();

        while (requestPending)
        {
            yield return WaitForSafeState();
            if (!requestPending) break;

            yield return new WaitForSecondsRealtime(SceneSettleDelaySeconds);
            if (!IsSafeState()) continue;

            var requestOperation = reviewManager.RequestReviewFlow();
            yield return requestOperation;

            if (!requestPending) break;

            if (requestOperation.Error != ReviewErrorCode.NoError)
            {
                FailCurrentRequest(
                    $"request failed with {requestOperation.Error}");
                yield break;
            }

            PlayReviewInfo reviewInfo = requestOperation.GetResult();

            if (!IsSafeState())
            {
                reviewInfo = null;
                Debug.Log(
                    "[PlayReview] Gameplay or app focus changed while requesting. " +
                    "Waiting to request fresh review information.");
                continue;
            }

            var launchOperation = reviewManager.LaunchReviewFlow(reviewInfo);
            yield return launchOperation;
            reviewInfo = null;

            if (!requestPending) break;

            if (launchOperation.Error != ReviewErrorCode.NoError)
            {
                FailCurrentRequest(
                    $"launch failed with {launchOperation.Error}");
                yield break;
            }

            PlayerPrefs.SetInt(FlowCompletedKey, 1);
            PlayerPrefs.Save();
            requestPending = false;
            requestCoroutine = null;
            Debug.Log("[PlayReview] Review flow completed; no further prompts will be attempted.");
            yield break;
        }

        requestCoroutine = null;
    }

    IEnumerator WaitForSafeState()
    {
        while (requestPending && !IsSafeState())
            yield return null;
    }

    bool IsSafeState()
    {
        if (applicationPaused || !Application.isFocused)
            return false;

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        return gameManager != null && !gameManager.IsPlaying;
    }

    void FailCurrentRequest(string reason)
    {
        requestPending = false;
        requestCoroutine = null;
        Debug.LogWarning(
            $"[PlayReview] {reason}. A later prestige or achievement claim may retry.");
    }
#endif

    void CancelPendingRequest()
    {
        requestPending = false;

        if (requestCoroutine != null)
        {
            StopCoroutine(requestCoroutine);
            requestCoroutine = null;
        }

        Debug.Log("[PlayReview] Cancelled pending review request for full save reset.");
    }

    internal static void PrepareForFullReset()
    {
        preservedSessionCount = Mathf.Max(
            0,
            PlayerPrefs.GetInt(SessionCountKey, 0));
        preservedFlowCompleted = PlayerPrefs.GetInt(FlowCompletedKey, 0) == 1
            ? 1
            : 0;
        hasPreservedResetState = true;

        if (instance)
            instance.CancelPendingRequest();
    }

    internal static void RestoreAfterFullReset()
    {
        if (!hasPreservedResetState) return;

        PlayerPrefs.SetInt(SessionCountKey, preservedSessionCount);
        PlayerPrefs.SetInt(FlowCompletedKey, preservedFlowCompleted);
        hasPreservedResetState = false;
    }
}
