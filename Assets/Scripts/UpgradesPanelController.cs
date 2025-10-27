// UpgradesPanelController.cs
using UnityEngine;
using TMPro;

public class UpgradesPanelController : MonoBehaviour
{
    [Header("Wiring")]
    public GameManager gameManager;
    public TMP_Text emberBankText;
    public Transform contentParent;      // ScrollView/Viewport/Content
    public GameObject upgradeRowPrefab;  // UpgradeRow.prefab

    UpgradeDef[] allDefs;
    bool built;

    void Awake()
    {
        allDefs = Resources.LoadAll<UpgradeDef>("Upgrades");
    }

    public void Open()
    {
        gameObject.SetActive(true);
        BuildIfNeeded();
        RefreshAll();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void BuildIfNeeded()
    {
        if (built) return;

        foreach (var def in allDefs)
        {
            var rowGO = Instantiate(upgradeRowPrefab, contentParent);
            var row = rowGO.GetComponent<UpgradeButton>();
            int owned = PlayerPrefs.GetInt($"upgrade_{def.id}", 0);

            // NEW: pass bank and best distance so the row can decide lock/cost/tier
            row.Setup(
                gameManager,
                def,
                owned,
                GetBestDistanceMeters(),
                gameManager ? gameManager.GetWispsBank() : 0
            );
        }
        built = true;
    }

    public void RefreshAll()
    {
        if (emberBankText) emberBankText.text = $"Embers: {gameManager.GetWispsBank():N0}";

        float best = GetBestDistanceMeters();
        int bank = gameManager ? gameManager.GetWispsBank() : 0;

        // refresh all rows' interactable state
        foreach (Transform child in contentParent)
        {
            var btn = child.GetComponent<UpgradeButton>();
            if (btn != null)
            {
                // Re-run setup with fresh bank & distance; keeps logic in one place
                int owned = PlayerPrefs.GetInt($"upgrade_{btn.def.id}", 0);
                btn.Setup(gameManager, btn.def, owned, best, bank);
            }
        }
    }

    float GetBestDistanceMeters()
    {
        // Prefer live tracker value if present; fall back to PlayerPrefs
        if (gameManager && gameManager.distanceTracker)
            return gameManager.distanceTracker.bestDistance;

        // Fallback key — change if your DistanceTracker uses a different key
        return PlayerPrefs.GetFloat("best_distance_m", 0f);

    }
}
