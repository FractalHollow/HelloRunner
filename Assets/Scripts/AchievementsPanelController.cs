using UnityEngine;

public class AchievementsPanelController : MonoBehaviour
{
    [Header("Refs")]
    public GameManager gm;
    public Transform contentRoot;
    public AchievementRowUI rowPrefab;

    void OnEnable()
    {
        Refresh();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (!contentRoot || !rowPrefab)
        {
            Debug.LogWarning("[AchievementsPanel] Missing contentRoot or rowPrefab.");
            return;
        }

        // Clear existing
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        var am = AchievementManager.I;
        if (am == null)
        {
            Debug.LogWarning("[AchievementsPanel] AchievementManager not found in scene.");
            return;
        }

        // We use best distance from PlayerPrefs (same key DistanceTracker uses)
        int bestDistM = PlayerPrefs.GetInt("best_distance_m", 0);

        var defs = am.GetAllDefs();
        foreach (var def in defs)
        {
            if (!def || string.IsNullOrEmpty(def.id)) continue;

            bool unlocked = am.IsUnlocked(def.id);
            bool claimed  = am.IsClaimed(def.id);

            // progress reading uses manager helper (runDistance/runScore/runEmbers irrelevant in Den view)
            int progress = am.GetProgress(def, bestDistM, 0, 0, 0);

            var row = Instantiate(rowPrefab, contentRoot);
            row.Bind(def, progress, def.targetValue, unlocked, claimed, gm);
        }
    }
}
