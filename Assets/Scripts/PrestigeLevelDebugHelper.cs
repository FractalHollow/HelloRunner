using UnityEngine;

[DisallowMultipleComponent]
public class PrestigeLevelDebugHelper : MonoBehaviour
{
    [Header("Editor-Only Override")]
    [SerializeField] bool overrideEnabled;
    [SerializeField, Min(0)] int testPrestigeLevel;

    void OnEnable()
    {
        ApplyIfAllowed();
    }

    void OnDisable()
    {
        ClearIfAllowed();
    }

    void OnValidate()
    {
        testPrestigeLevel = Mathf.Max(0, testPrestigeLevel);
        ApplyIfAllowed();
    }

    [ContextMenu("Apply Prestige Override")]
    void ApplyOverride()
    {
        overrideEnabled = true;
        ApplyIfAllowed();
    }

    [ContextMenu("Clear Prestige Override")]
    void ClearOverride()
    {
        overrideEnabled = false;
        ClearIfAllowed();
    }

    [ContextMenu("Sync Test Level From Saved Prestige")]
    void SyncFromSavedPrestige()
    {
        testPrestigeLevel = PrestigeManager.Level;
        ApplyIfAllowed();
    }

    void ApplyIfAllowed()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;

        if (overrideEnabled)
            PrestigeManager.SetEffectiveLevelOverride(testPrestigeLevel);
        else
            PrestigeManager.ClearEffectiveLevelOverride();
#endif
    }

    void ClearIfAllowed()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            PrestigeManager.ClearEffectiveLevelOverride();
#endif
    }
}
