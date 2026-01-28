using UnityEngine;
using System.Linq;

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
            return;

        // Clear existing rows
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        var am = AchievementManager.I;
        if (am == null)
            return;

        int bestDistM = 0;
        if (gm != null && gm.distanceTracker != null)
            bestDistM = (int)gm.distanceTracker.bestDistance;
        else
            bestDistM = PlayerPrefs.GetInt("best_distance_m", 0); // fallback

        var defs = am.GetAllDefs();

        // Group + sort:
        // 0 = ready to claim, 1 = locked, 2 = claimed
        var ordered = defs
            .Where(d => d != null && !string.IsNullOrEmpty(d.id))
            .Select(d =>
            {
                bool unlocked = am.IsUnlocked(d.id);
                bool claimed = am.IsClaimed(d.id);
                bool claimable = unlocked && !claimed;

                int group = claimable ? 0 : (!unlocked ? 1 : 2);
                return (def: d, group: group, unlocked: unlocked, claimed: claimed);
            })
            .OrderBy(x => x.group)
            .ThenBy(x => x.def.sortOrder)
            .ThenBy(x => x.def.displayName)
            .ToList();

        foreach (var x in ordered)
        {
            int progress = am.GetProgress(x.def, bestDistM, 0, 0, 0);

            var row = Instantiate(rowPrefab, contentRoot);
            row.Bind(x.def, progress, x.def.targetValue, x.unlocked, x.claimed, gm);
        }
    }

}
