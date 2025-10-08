using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradeButton : MonoBehaviour
{
    [Header("Wiring")]
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text costText;
    public Button buyButton;

    [HideInInspector] public UpgradeDef def;
    int currentLevel;
    GameManager gm;

    // ---- PUBLIC API ----
    public void Setup(GameManager gameManager, UpgradeDef definition, int ownedLevel)
    {
        gm = gameManager;
        def = definition;
        currentLevel = ownedLevel;

        // (Re)wire the button each time we setup
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(Buy);
        }

        Refresh();
    }

    public int GetLevel() => currentLevel;

    // ---- INTERNAL ----
    void Refresh()
    {
        if (def == null) return;

        if (nameText)  nameText.text  = def.displayName;
        if (levelText) levelText.text = $"Lv {currentLevel}/{def.maxLevel}";

        int nextCost = Mathf.RoundToInt(def.baseCost * Mathf.Pow(def.costScale, currentLevel));
        if (costText)  costText.text  = $"{nextCost} Embers";

        if (buyButton)
            buyButton.interactable = currentLevel < def.maxLevel && gm != null && gm.CanAfford(nextCost);
    }

    void Buy()
    {
        if (gm == null || def == null) return;

        int cost = Mathf.RoundToInt(def.baseCost * Mathf.Pow(def.costScale, currentLevel));
        if (!gm.TrySpendWisps(cost)) return;

        currentLevel++;
        PlayerPrefs.SetInt($"upgrade_{def.id}", currentLevel);
        PlayerPrefs.Save();

        gm.ApplyUpgrade(def);
        gm.RefreshUpgradesUI();
        Refresh();
    }
}
