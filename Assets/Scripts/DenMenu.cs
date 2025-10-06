using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DenMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI wispsValue;
    [SerializeField] TextMeshProUGUI idleText;
    [SerializeField] Button wakeUpButton;
    [SerializeField] Button unlockUpgradesButton;
    [SerializeField] Button openUpgradesButton;

    [SerializeField] PanelFader fader;

    const string KEY_STORE_UNLOCKED = "store_unlocked";

    void Start()
    {
        // Subscribe to currency changes so the UI updates live
        if (Currency.I != null)
            Currency.I.OnChanged += UpdateWispsUI;

        UpdateWispsUI(Currency.I ? Currency.I.Total : 0);
        RefreshIdleUI();
        RefreshStoreUI();
    }

    void OnDestroy()
    {
        if (Currency.I != null)
            Currency.I.OnChanged -= UpdateWispsUI;
    }

    // ---------- Open / Close ----------
    public void Open()
    {
        gameObject.SetActive(true);

        // Ensure idle has a baseline timestamp and refresh the pending amount
        IdleSystem.EnsureStartStamp();
        RefreshIdleUI();

        if (fader) fader.FadeIn();
    }

    public void Close()
    {
        if (fader) fader.FadeOut(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
    }

    // ---------- UI Refresh ----------
    void UpdateWispsUI(int total)
    {
        if (wispsValue) wispsValue.text = total.ToString();
        RefreshStoreUI();
    }

    void RefreshIdleUI()
    {
        int claim = IdleSystem.GetClaimableWisps();
        if (idleText) idleText.text = $"You can claim <b>{claim}</b> Embers (up to 8h stored).";
        if (wakeUpButton) wakeUpButton.interactable = (claim > 0);
    }

void RefreshStoreUI()
{
    bool unlocked = PlayerPrefs.GetInt(KEY_STORE_UNLOCKED, 0) == 1;

    if (unlockUpgradesButton) unlockUpgradesButton.gameObject.SetActive(!unlocked);
    if (openUpgradesButton)   openUpgradesButton.gameObject.SetActive(unlocked);

    if (!unlocked && unlockUpgradesButton)
        unlockUpgradesButton.interactable = (Currency.I && Currency.I.Total >= 50);
}

    // ---------- Buttons ----------
    public void OnWakeUp()
    {
        int claim = IdleSystem.GetClaimableWisps();
        if (claim > 0) Currency.I?.Add(claim);
        IdleSystem.Claim();
        RefreshIdleUI();
    }

public void OnUnlockStore()  // rename this too if you like
{
    const int COST = 50;
    if (Currency.I != null && Currency.I.Spend(COST))
    {
        PlayerPrefs.SetInt(KEY_STORE_UNLOCKED, 1);
        PlayerPrefs.Save();
        RefreshStoreUI();
    }
}
    public void OnOpenStore()
    {
        // TODO: open your StorePanel when it exists
        Debug.Log("Open Upgrades: TODO");
    }

    public void OnOpenUpgrades()     { Debug.Log("Open Upgrades: TODO"); }
    public void OnOpenAchievements() { Debug.Log("Open Achievements: TODO"); }
    public void OnOpenPrestige()     { Debug.Log("Open Prestige: TODO"); }
}
