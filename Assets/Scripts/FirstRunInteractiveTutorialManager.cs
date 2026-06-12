using TMPro;
using UnityEngine;

public class FirstRunInteractiveTutorialManager : MonoBehaviour
{
    public const string CompletionKey = "FirstRunInteractiveTutorialComplete";

    const string MigrationKey = "FirstRunInteractiveTutorialMigrationApplied";
    const string OverlayName = "FirstRunInteractiveTutorialOverlay";

    [Header("References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] PlayerGravityFlip player;
    [SerializeField] RectTransform overlayParent;
    [SerializeField] TMP_Text styleSource;
    [SerializeField] Collider2D playerCollider;
    [SerializeField] Collider2D groundCollider;
    [SerializeField] Collider2D ceilingCollider;

    [Header("Gameplay")]
    [SerializeField, Range(0.01f, 1f)] float openingTimeScale = 0.15f;
    [SerializeField, Range(0.01f, 1f)] float dangerTimeScale = 0.05f;
    [SerializeField, Range(0.01f, 1f)] float postFlipTimeScale = 0.35f;
    [SerializeField, Min(0.1f)] float dangerDistance = 3f;

    [Header("Timing (Real Seconds)")]
    [SerializeField, Min(0f)] float flipInstructionMinimumDuration = 4f;
    [SerializeField, Min(0f)] float interMessageGapDuration = 2f;
    [SerializeField, Min(0f)] float survivalHintMinimumDuration = 3f;
    [SerializeField, Min(0f)] float progressionHintMinimumDuration = 3f;
    [SerializeField, Min(0f)] float tapAdvanceTimeout = 9f;
    [SerializeField, Min(0.01f)] float normalSpeedRestoreDuration = 1.5f;

    [Header("Overlay")]
    [SerializeField] string flipInstruction = "Tap anywhere to flip Fox's gravity";
    [SerializeField] string survivalHint = "Stay on screen and dodge enemies";
    [SerializeField] string progressionHint = "Earn Embers to upgrade and go farther";
    [SerializeField] string dangerHint = "Tap now!";
    [SerializeField, Min(1f)] float mainFontSize = 80f;
    [SerializeField, Min(1f)] float warningFontSize = 52f;
    [SerializeField, Range(0f, 100f)] float mainLineSpacing = 10f;
    [SerializeField] Vector2 overlayPosition = new Vector2(0f, 390f);
    [SerializeField] Vector2 overlaySize = new Vector2(980f, 330f);
    [SerializeField] float warningYOffset = -40f;
    [SerializeField, Min(0.1f)] float pulseSpeed = 3.2f;
    [SerializeField, Range(0.5f, 1f)] float pulseMinScale = 0.88f;

    enum TutorialStep
    {
        Inactive,
        FlipInstruction,
        GapBeforeSurvival,
        SurvivalHint,
        GapBeforeProgression,
        ProgressionHint,
        Completing
    }

    RectTransform overlayRoot;
    CanvasGroup overlayGroup;
    TMP_Text mainText;
    TMP_Text warningText;
    TutorialStep step;
    float stepElapsed;
    float restoreStartScale;
    bool dangerCueActive;
    bool dangerCueSuppressedUntilClear;
    bool controlDemonstrated;
    bool subscribedToPlayer;
    bool warnedMissingEssentials;
    bool warnedMissingBoundaries;

    public bool IsActive => step != TutorialStep.Inactive;

    void Awake()
    {
        AutoAssignReferences();
        MigrateLegacyCompletion();
        EnsureOverlay();
    }

    void OnDisable()
    {
        AbortTutorial();
    }

    void Update()
    {
        if (!IsActive || !gameManager || !gameManager.IsPlaying || gameManager.IsPaused)
            return;

        float delta = Time.unscaledDeltaTime;

        switch (step)
        {
            case TutorialStep.FlipInstruction:
                stepElapsed += delta;
                UpdateFlipInstruction();
                if (HasTapAdvanceTimedOut(flipInstructionMinimumDuration))
                    AdvanceCurrentStep();
                break;

            case TutorialStep.GapBeforeSurvival:
                stepElapsed += delta;
                if (stepElapsed >= interMessageGapDuration)
                    ShowMessage(TutorialStep.SurvivalHint, survivalHint);
                break;

            case TutorialStep.SurvivalHint:
                stepElapsed += delta;
                if (HasTapAdvanceTimedOut(survivalHintMinimumDuration))
                    AdvanceCurrentStep();
                break;

            case TutorialStep.GapBeforeProgression:
                stepElapsed += delta;
                if (stepElapsed >= interMessageGapDuration)
                    ShowMessage(TutorialStep.ProgressionHint, progressionHint);
                break;

            case TutorialStep.ProgressionHint:
                stepElapsed += delta;
                if (HasTapAdvanceTimedOut(progressionHintMinimumDuration))
                    AdvanceCurrentStep();
                break;

            case TutorialStep.Completing:
                UpdateCompletion(delta);
                break;
        }
    }

    public void OnRunStarted()
    {
        AbortTutorial();

        if (PlayerPrefs.GetInt(CompletionKey, 0) == 1)
            return;

        AutoAssignReferences();
        if (!HasEssentialReferences() || !EnsureOverlay())
        {
            WarnMissingEssentials();
            gameManager?.ResetRunTimeScale();
            return;
        }

        WarnIfBoundariesMissing();
        SubscribeToPlayer();
        step = TutorialStep.FlipInstruction;
        stepElapsed = 0f;
        dangerCueActive = false;
        dangerCueSuppressedUntilClear = false;
        controlDemonstrated = false;

        overlayRoot.localScale = Vector3.one;
        overlayRoot.SetAsLastSibling();
        overlayGroup.alpha = 1f;
        overlayRoot.gameObject.SetActive(true);
        mainText.text = flipInstruction;
        mainText.gameObject.SetActive(true);
        warningText.text = "";
        warningText.rectTransform.localScale = Vector3.one;
        warningText.gameObject.SetActive(false);

        gameManager.SetRunTimeScale(openingTimeScale);
    }

    public void OnRunEnded()
    {
        AbortTutorial();
    }

    public void OnGamePaused()
    {
        if (IsActive && overlayGroup)
            overlayGroup.alpha = 0f;
    }

    public void OnGameResumed()
    {
        if (!IsActive || !overlayGroup)
            return;

        overlayGroup.alpha = step == TutorialStep.Completing
            ? 1f - Mathf.Clamp01(stepElapsed / normalSpeedRestoreDuration)
            : 1f;
    }

    void UpdateFlipInstruction()
    {
        bool playerNearBoundary = IsPlayerNearBoundary();
        if (dangerCueSuppressedUntilClear)
        {
            if (playerNearBoundary)
                return;

            dangerCueSuppressedUntilClear = false;
        }

        if (!controlDemonstrated && !dangerCueActive && playerNearBoundary)
        {
            dangerCueActive = true;
            warningText.text = dangerHint;
            warningText.gameObject.SetActive(true);
            gameManager.SetRunTimeScale(dangerTimeScale);
        }

        if (!dangerCueActive)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(pulseMinScale, 1f, pulse);
        warningText.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    void HandleInputFlip()
    {
        if (!controlDemonstrated)
        {
            controlDemonstrated = true;
            ClearDangerCue();
            dangerCueSuppressedUntilClear = false;

            if (step == TutorialStep.FlipInstruction)
                gameManager.SetRunTimeScale(openingTimeScale);
        }

        AdvanceCurrentStep();
    }

    void HandleBoundaryHit()
    {
        if (step != TutorialStep.FlipInstruction || !dangerCueActive)
            return;

        ClearDangerCue();
        dangerCueSuppressedUntilClear = true;
        gameManager.SetRunTimeScale(openingTimeScale);
    }

    bool HasTapAdvanceTimedOut(float minimumDuration)
    {
        return stepElapsed >= minimumDuration + tapAdvanceTimeout;
    }

    void AdvanceCurrentStep()
    {
        switch (step)
        {
            case TutorialStep.FlipInstruction:
                if (stepElapsed >= flipInstructionMinimumDuration)
                {
                    gameManager.SetRunTimeScale(postFlipTimeScale);
                    BeginGap(TutorialStep.GapBeforeSurvival);
                }
                break;

            case TutorialStep.SurvivalHint:
                if (stepElapsed >= survivalHintMinimumDuration)
                    BeginGap(TutorialStep.GapBeforeProgression);
                break;

            case TutorialStep.ProgressionHint:
                if (stepElapsed >= progressionHintMinimumDuration)
                {
                    mainText.gameObject.SetActive(false);
                    BeginCompletion();
                }
                break;
        }
    }

    void BeginGap(TutorialStep gapStep)
    {
        step = gapStep;
        stepElapsed = 0f;
        mainText.gameObject.SetActive(false);
        ClearDangerCue();
        dangerCueSuppressedUntilClear = false;
    }

    void ShowMessage(TutorialStep nextStep, string message)
    {
        step = nextStep;
        stepElapsed = 0f;
        mainText.text = message;
        mainText.gameObject.SetActive(true);
        warningText.gameObject.SetActive(false);
        overlayGroup.alpha = 1f;
    }

    void ClearDangerCue()
    {
        dangerCueActive = false;
        warningText.rectTransform.localScale = Vector3.one;
        warningText.text = "";
        warningText.gameObject.SetActive(false);
    }

    void BeginCompletion()
    {
        step = TutorialStep.Completing;
        stepElapsed = 0f;
        restoreStartScale = gameManager.RunTimeScale;
        warningText.gameObject.SetActive(false);
        dangerCueSuppressedUntilClear = false;
    }

    void UpdateCompletion(float delta)
    {
        stepElapsed += delta;
        float t = Mathf.Clamp01(stepElapsed / normalSpeedRestoreDuration);
        float eased = Mathf.SmoothStep(0f, 1f, t);

        gameManager.SetRunTimeScale(Mathf.Lerp(restoreStartScale, 1f, eased));
        overlayGroup.alpha = 1f - t;

        if (t < 1f)
            return;

        gameManager.ResetRunTimeScale();
        player.StopTutorialHitFeedback();
        HideOverlay();
        UnsubscribeFromPlayer();
        step = TutorialStep.Inactive;
        controlDemonstrated = false;
        dangerCueSuppressedUntilClear = false;

        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();
    }

    void AbortTutorial()
    {
        if (!IsActive && !subscribedToPlayer)
            return;

        UnsubscribeFromPlayer();
        player?.StopTutorialHitFeedback();
        HideOverlay();
        step = TutorialStep.Inactive;
        stepElapsed = 0f;
        dangerCueActive = false;
        dangerCueSuppressedUntilClear = false;
        controlDemonstrated = false;
        gameManager?.ResetRunTimeScale();
    }

    bool IsPlayerNearBoundary()
    {
        if (!playerCollider || !groundCollider || !ceilingCollider)
            return false;

        float floorGap = playerCollider.bounds.min.y - groundCollider.bounds.max.y;
        float ceilingGap = ceilingCollider.bounds.min.y - playerCollider.bounds.max.y;
        return Mathf.Min(floorGap, ceilingGap) <= dangerDistance;
    }

    bool HasEssentialReferences()
    {
        return gameManager && player && overlayParent && styleSource;
    }

    void AutoAssignReferences()
    {
        if (!gameManager)
            gameManager = GetComponent<GameManager>() ?? FindFirstObjectByType<GameManager>();

        if (!player)
            player = gameManager && gameManager.player
                ? gameManager.player
                : FindFirstObjectByType<PlayerGravityFlip>();

        if (!styleSource && gameManager)
            styleSource = gameManager.scoreText;

        if (!overlayParent && styleSource)
            overlayParent = styleSource.transform.parent as RectTransform;

        if (!playerCollider && player)
            playerCollider = player.GetComponent<Collider2D>();

        if (!groundCollider)
        {
            GameObject ground = GameObject.FindGameObjectWithTag("Ground");
            if (ground)
                groundCollider = ground.GetComponent<Collider2D>();
        }

        if (!ceilingCollider)
        {
            GameObject ceiling = GameObject.FindGameObjectWithTag("Ceiling");
            if (ceiling)
                ceilingCollider = ceiling.GetComponent<Collider2D>();
        }
    }

    bool EnsureOverlay()
    {
        if (overlayRoot && overlayGroup && mainText && warningText)
            return true;

        if (!overlayParent || !styleSource)
            return false;

        Transform existing = overlayParent.Find(OverlayName);
        if (existing)
            Destroy(existing.gameObject);

        GameObject rootObject = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasGroup));
        overlayRoot = rootObject.GetComponent<RectTransform>();
        overlayRoot.SetParent(overlayParent, false);
        overlayRoot.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        overlayRoot.anchoredPosition = overlayPosition;
        overlayRoot.sizeDelta = overlaySize;
        overlayRoot.SetAsLastSibling();

        overlayGroup = rootObject.GetComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        mainText = CreateText("Instruction", overlayRoot, mainFontSize, FontStyles.Bold);
        mainText.lineSpacing = mainLineSpacing;
        SetRect(mainText.rectTransform, new Vector2(0f, 0.32f), Vector2.one);

        warningText = CreateText("Warning", overlayRoot, warningFontSize, FontStyles.Bold);
        SetWarningRect(warningText.rectTransform);

        rootObject.SetActive(false);
        return true;
    }

    TMP_Text CreateText(string objectName, Transform parent, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = styleSource.font;
        text.fontSharedMaterial = styleSource.fontSharedMaterial;
        text.color = styleSource.color;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.enableAutoSizing = false;

        return text;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    void SetWarningRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, warningYOffset);
        rect.sizeDelta = new Vector2(0f, 100f);
        rect.localScale = Vector3.one;
    }

    void HideOverlay()
    {
        if (!overlayRoot)
            return;

        overlayRoot.localScale = Vector3.one;
        if (warningText)
            warningText.rectTransform.localScale = Vector3.one;
        if (overlayGroup)
            overlayGroup.alpha = 0f;

        overlayRoot.gameObject.SetActive(false);
    }

    void SubscribeToPlayer()
    {
        if (subscribedToPlayer || !player)
            return;

        player.InputFlipPerformed += HandleInputFlip;
        player.BoundaryHit += HandleBoundaryHit;
        subscribedToPlayer = true;
    }

    void UnsubscribeFromPlayer()
    {
        if (!subscribedToPlayer)
            return;

        if (player)
        {
            player.InputFlipPerformed -= HandleInputFlip;
            player.BoundaryHit -= HandleBoundaryHit;
        }

        subscribedToPlayer = false;
    }

    void MigrateLegacyCompletion()
    {
        if (PlayerPrefs.GetInt(MigrationKey, 0) == 1)
            return;

        if (PlayerPrefs.GetInt(FirstRunTutorial.SeenKey, 0) == 1)
            PlayerPrefs.SetInt(CompletionKey, 1);

        PlayerPrefs.SetInt(MigrationKey, 1);
        PlayerPrefs.Save();
    }

    void WarnMissingEssentials()
    {
        if (warnedMissingEssentials)
            return;

        warnedMissingEssentials = true;
        Debug.LogWarning(
            "[FirstRunInteractiveTutorial] Missing GameManager, player, overlay parent, or ScoreText style reference. " +
            "Skipping this tutorial attempt without marking it complete.");
    }

    void WarnIfBoundariesMissing()
    {
        if (warnedMissingBoundaries || (playerCollider && groundCollider && ceilingCollider))
            return;

        warnedMissingBoundaries = true;
        Debug.LogWarning(
            "[FirstRunInteractiveTutorial] One or more boundary colliders are missing. " +
            "The tutorial will continue without the urgent edge warning.");
    }
}
