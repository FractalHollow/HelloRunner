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
    [SerializeField] Image buyButtonImage; // optional, assign in Inspector for tint

    [Header("Locked UI")]
    [SerializeField] GameObject lockedGroup;
    [SerializeField] TMP_Text lockedText;

    [Header("Colors")]
    [SerializeField] Color colorAffordable = new Color(1f, 0.55f, 0.1f, 1f);   // orange
    [SerializeField] Color colorCantAfford = new Color(0.5f, 0.5f, 0.5f, 0.9f);
    [SerializeField] Color colorMaxed     = new Color(0.35f, 0.8f, 0.4f, 0.9f);
    [SerializeField] Color colorLocked    = new Color(0.35f, 0.35f, 0.35f, 0.9f);

    [HideInInspector] public UpgradeDef def;

    GameManager gm;
    int ownedTier;
    float bestDistance;
    int bank;

    // Called by the panel controller when building the list
    public void Setup(GameManager gm, UpgradeDef def, int ownedTier, float bestDistanceMeters, int currentBank)
    {
        this.gm = gm;
        this.def = def;
        this.ownedTier = Mathf.Max(0, ownedTier);
        this.bestDistance = bestDistanceMeters;
        this.bank = currentBank;

        Refresh();
    }

    public void Refresh()
    {
    if (!def) return;

        // ---------- INFO-ONLY / DUMMY ROWS ----------
        if (def.MaxTier <= 0)
        {
            if (nameText) nameText.text = def.displayName;

            bool infoLocked = bestDistance < def.unlockDistance;

            if (descText)
            {
                descText.text = infoLocked
                    ? $"Unlocks at {def.unlockDistance} m"
                    : "Unlocked — toggle from the Start screen.";
            }

            if (tierText) tierText.text = "";       // no tier label
            if (costText) costText.text = "";       // no price
            if (buyButton) buyButton.gameObject.SetActive(false); // hide Buy

            if (lockedGroup) lockedGroup.SetActive(infoLocked);

            return; // done
        }


    // ---------- NORMAL UPGRADE ROWS CONTINUE BELOW ----------

        if (nameText) nameText.text = def.displayName;

        bool lockedByDistance = bestDistance < def.unlockDistance;
        bool depsOk = def.AreDependenciesMet(id => PlayerPrefs.GetInt($"upgrade_{id}", 0));
        bool lockedByDeps = !depsOk;
        bool locked = lockedByDistance || lockedByDeps;

        bool maxed = ownedTier >= def.MaxTier;
        int nextTier = Mathf.Clamp(ownedTier + 1, 1, def.MaxTier);

        if (descText)
            descText.text = maxed ? "Max level reached." : def.GetDescriptionForTier(nextTier);

        if (tierText)
            tierText.text = $"Tier {ownedTier}/{def.MaxTier}" + (maxed ? "" : $" → {nextTier}");

        int cost = def.GetCostForTier(nextTier);
        bool canAfford = !maxed && !locked && gm && gm.CanAfford(cost);

        if (costText)
            costText.text = maxed ? "-" : cost.ToString("N0");

        // ----- STATE LOGIC -----
        if (locked)
            SetLocked(lockedByDistance, lockedByDeps);
        else if (maxed)
            SetMaxed();
        else if (canAfford)
            SetAffordable();
        else
            SetCantAfford();

        // Handle click
        buyButton.onClick.RemoveAllListeners();
        if (canAfford)
            buyButton.onClick.AddListener(() => Buy(cost, nextTier));
    }

    // ===== Helper state visuals =====
    void SetAffordable()
    {
        buyButton.interactable = true;
        TintButton(colorAffordable);
        if (lockedGroup) lockedGroup.SetActive(false);
    }

    void SetCantAfford()
    {
        buyButton.interactable = false;
        TintButton(colorCantAfford);
        if (lockedGroup) lockedGroup.SetActive(false);
    }

    void SetMaxed()
    {
        buyButton.interactable = false;
        TintButton(colorMaxed);
        if (lockedGroup) lockedGroup.SetActive(false);
    }

    void SetLocked(bool byDistance, bool byDeps)
    {
        buyButton.interactable = false;
        TintButton(colorLocked);

        if (lockedGroup) lockedGroup.SetActive(true);
        if (lockedText)
        {
            if (byDistance && byDeps)
                lockedText.text = $"Unlocks at {def.unlockDistance} m\n+ Dependencies not met";
            else if (byDistance)
                lockedText.text = $"Unlocks at {def.unlockDistance} m";
            else if (byDeps)
                lockedText.text = "Requires other upgrades";
        }
    }

    void TintButton(Color c)
    {
        if (buyButtonImage) buyButtonImage.color = c;
    }

    // ===== Purchase logic =====
    void Buy(int cost, int nextTier)
    {
        if (gm == null) return;
        if (!gm.TrySpendWisps(cost)) return;

        ownedTier = nextTier;
        PlayerPrefs.SetInt($"upgrade_{def.id}", ownedTier);
        PlayerPrefs.Save();

        gm.ApplyUpgrade(def);
        gm.RefreshUpgradesUI();
    }
}
