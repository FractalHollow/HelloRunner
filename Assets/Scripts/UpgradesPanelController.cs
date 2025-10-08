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
            row.Setup(gameManager, def, owned);
        }
        built = true;
    }

    public void RefreshAll()
    {
        if (emberBankText) emberBankText.text = $"Embers: {gameManager.GetWispsBank():N0}";

        // refresh all rows' interactable state
        foreach (Transform child in contentParent)
        {
            var btn = child.GetComponent<UpgradeButton>();
            if (btn != null)
            {
                // trick: call Setup again to refresh cost/interactable using current bank
                btn.Setup(gameManager, btn.def, PlayerPrefs.GetInt($"upgrade_{btn.def.id}", 0));
            }
        }
    }
}
