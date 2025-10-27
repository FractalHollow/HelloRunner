using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descText;
    [SerializeField] TMP_Text tierText;
    [SerializeField] TMP_Text costText;
    [SerializeField] Button buyButton;

    [Header("Locked UI")]
    [SerializeField] GameObject lockedGroup;   // e.g. an overlay panel
    [SerializeField] TMP_Text lockedText;      // "Unlocks at 100 m" or dependency text

    [HideInInspector] public UpgradeDef def;

    GameManager gm;
    int ownedTier;
    float bestDistance;
    int bank;

    // Keep Setup signature your controller expects, but add data we need
    public void Setup(GameManager gm, UpgradeDef def, int ownedTier, float bestDistanceMeters, int currentBank)
    {
        this.gm = gm;
        this.def = def;
        this.ownedTier = Mathf.Max(0, ownedTier);
        this.bestDistance = bestDistanceMeters;
        this.bank = currentBank;

        Refresh();
    }

    void Refresh()
    {
        if (!def) return;

        // Title **Changed from "titleText" to "nameText"**
        if (nameText) nameText.text = def.displayName;

        // Lock check (distance + dependencies)
        bool lockedByDistance = bestDistance < def.unlockDistance;

        bool depsOk = def.AreDependenciesMet(id => PlayerPrefs.GetInt($"upgrade_{id}", 0));
        bool lockedByDeps = !depsOk;

        bool locked = lockedByDistance || lockedByDeps;

        // Maxed?
        bool maxed = ownedTier >= def.MaxTier;

        // Next tier to buy (1-based)
        int nextTier = Mathf.Clamp(ownedTier + 1, 1, def.MaxTier);

        // Description (for next tier)
        if (descText)
        {
            if (!maxed)
                descText.text = def.GetDescriptionForTier(nextTier);
            else
                descText.text = "Max level reached.";
        }

        // Tier label
        if (tierText)
        {
            tierText.text = maxed
                ? $"Tier {ownedTier}/{def.MaxTier}"
                : $"Tier {ownedTier}/{def.MaxTier} → {nextTier}";
        }

        // Cost & button state
        int cost = def.GetCostForTier(nextTier);
        bool canAfford = !maxed && !locked && gm && gm.CanAfford(cost);

        if (costText)
            costText.text = maxed ? "-" : cost.ToString("N0");

        if (buyButton)
        {
            buyButton.interactable = canAfford;
            buyButton.onClick.RemoveAllListeners();
            if (canAfford)
                buyButton.onClick.AddListener(() => Buy(cost, nextTier));
        }

        // Locked visuals
        if (lockedGroup) lockedGroup.SetActive(locked);
        if (locked && lockedText)
        {
            if (lockedByDistance && lockedByDeps)
                lockedText.text = $"Unlocks at {def.unlockDistance} m\n+ Dependencies not met";
            else if (lockedByDistance)
                lockedText.text = $"Unlocks at {def.unlockDistance} m";
            else if (lockedByDeps)
                lockedText.text = "Requires other upgrades";
        }
    }

    void Buy(int cost, int nextTier)
    {
        if (gm == null) return;
        if (!gm.TrySpendWisps(cost)) return;

        ownedTier = nextTier;
        PlayerPrefs.SetInt($"upgrade_{def.id}", ownedTier);
        PlayerPrefs.Save();

        // Let GameManager apply the effect data if needed (optional immediate apply)
        gm.ApplyUpgrade(def);

        // Ask the panel to refresh everything (including bank text)
        gm.RefreshUpgradesUI();
    }
}
