using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CosmeticsButtonIndicator : MonoBehaviour
{
    enum IndicatorSource
    {
        Cosmetics = 0,
        Achievements = 1
    }

    [Header("Targets")]
    [SerializeField] Graphic targetGraphic;
    [SerializeField] IndicatorSource indicatorSource = IndicatorSource.Cosmetics;

    [Header("Pulse")]
    [SerializeField] Color glowColor = new Color(1f, 0.78f, 0.26f, 1f);
    [SerializeField] float pulsesPerSecond = 1.35f;
    [SerializeField] float scaleBoost = 0.06f;

    Color baseColor = Color.white;
    Vector3 baseScale = Vector3.one;
    bool initialized;
    bool isActive;

    void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
    }

    void Awake()
    {
        if (!targetGraphic) targetGraphic = GetComponent<Graphic>();

        if (targetGraphic) baseColor = targetGraphic.color;
        baseScale = transform.localScale;
        initialized = true;
        ApplyRestingState();
    }

    void OnEnable()
    {
        Subscribe();
        RefreshFromManager();
    }

    void OnDisable()
    {
        Unsubscribe();
        ApplyRestingState();
    }

    void Update()
    {
        if (!isActive) return;

        float wave = 0.5f * (1f + Mathf.Sin(Time.unscaledTime * pulsesPerSecond * Mathf.PI * 2f));

        if (targetGraphic)
            targetGraphic.color = Color.Lerp(baseColor, glowColor, wave);

        transform.localScale = baseScale * (1f + scaleBoost * wave);
    }

    void HandleIndicatorChanged(bool hasUnseenUnlockedSkin)
    {
        isActive = hasUnseenUnlockedSkin;
        if (!isActive) ApplyRestingState();
    }

    void RefreshFromManager()
    {
        bool shouldPulse = false;

        switch (indicatorSource)
        {
            case IndicatorSource.Achievements:
                shouldPulse = AchievementManager.I != null && AchievementManager.I.HasUnseenUnlockedAchievement;
                break;

            default:
                shouldPulse = CosmeticsManager.I != null && CosmeticsManager.I.HasUnseenUnlockedSkin;
                break;
        }

        HandleIndicatorChanged(shouldPulse);
    }

    void Subscribe()
    {
        switch (indicatorSource)
        {
            case IndicatorSource.Achievements:
                AchievementManager.NewAchievementIndicatorChanged += HandleIndicatorChanged;
                break;

            default:
                CosmeticsManager.NewSkinIndicatorChanged += HandleIndicatorChanged;
                break;
        }
    }

    void Unsubscribe()
    {
        switch (indicatorSource)
        {
            case IndicatorSource.Achievements:
                AchievementManager.NewAchievementIndicatorChanged -= HandleIndicatorChanged;
                break;

            default:
                CosmeticsManager.NewSkinIndicatorChanged -= HandleIndicatorChanged;
                break;
        }
    }

    void ApplyRestingState()
    {
        if (!initialized) return;

        if (targetGraphic)
            targetGraphic.color = baseColor;

        transform.localScale = baseScale;
    }
}
