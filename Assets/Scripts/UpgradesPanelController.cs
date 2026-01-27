// UpgradesPanelController.cs
using UnityEngine;
using TMPro;
using System.Linq;

public class UpgradesPanelController : MonoBehaviour
{
    [Header("Wiring")]
    public GameManager gameManager;
    public TMP_Text emberBankText;
    public Transform contentParent;      // ScrollView/Viewport/Content
    public GameObject upgradeRowPrefab;  // UpgradeRow.prefab
    public TMP_Text bestDistanceText;

    UpgradeDef[] allDefs;
    bool built;

    void Awake()
    {
        // Load and sort so the list feels progressive
        allDefs = Resources.LoadAll<UpgradeDef>("Upgrades")
            .OrderBy(d => d.unlockDistance)
            .ThenBy(d => d.displayName)
            .ToArray();
    }

    void OnEnable()
    {
        // If panel is re-enabled, make sure the UI reflects latest bank/distance
        if (built) RefreshAll();
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

        // Refresh Den idle text (rate/cap) after upgrades are applied
        var den = UnityEngine.Object.FindFirstObjectByType<DenMenu>();
        if (den && den.gameObject.activeInHierarchy)
            den.RefreshAfterMetaChange();
    }

    void OnDisable()
    {
        var den = UnityEngine.Object.FindFirstObjectByType<DenMenu>();
        if (den && den.gameObject.activeInHierarchy)
            den.RefreshAfterMetaChange();
    }


    void BuildIfNeeded()
    {
        if (built) return;

        if (!gameManager)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        float best = GetBestDistanceMeters();
        int bank = gameManager ? gameManager.GetWispsBank() : 0;

        foreach (var def in allDefs)
        {
            var rowGO = Instantiate(upgradeRowPrefab, contentParent);
            var row = rowGO.GetComponent<UpgradeButton>();
            if (!row)
            {
                Debug.LogWarning("UpgradeRow prefab is missing UpgradeButton component.");
                continue;
            }

            int owned = PlayerPrefs.GetInt($"upgrade_{def.id}", 0);

            row.Setup(
                gameManager,
                def,
                owned,
                best,
                bank
            );
        }
        built = true;
    }

    public void RefreshAll()
    {
        if (!gameManager)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        int bank = gameManager ? gameManager.GetWispsBank() : 0;
        float best = GetBestDistanceMeters();

         if (bestDistanceText) bestDistanceText.text = $"Best: {(int)best} m";
        if (emberBankText) emberBankText.text = $"Embers: {bank:N0}";

        // Refresh all rows with current bank & distance
        foreach (Transform child in contentParent)
        {
            var btn = child.GetComponent<UpgradeButton>();
            if (btn == null || btn.def == null) continue;

            int owned = PlayerPrefs.GetInt($"upgrade_{btn.def.id}", 0);
            btn.Setup(gameManager, btn.def, owned, best, bank);
        }
    }

    float GetBestDistanceMeters()
    {
    // Gate upgrades by best distance THIS prestige (relocks after prestige)
    return PrestigeManager.BestDistanceThisPrestigeM;
    }

    // Optional: Use this if you ever add/remove upgrade defs at runtime.
    public void Rebuild()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        built = false;
        BuildIfNeeded();
        RefreshAll();
    }
}
