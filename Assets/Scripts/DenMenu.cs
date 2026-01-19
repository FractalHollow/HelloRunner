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

    // NOTE: This key name is currently used for your "unlock upgrades" button.
    // If you ever rename it later, update it here too.
    const string KEY_STORE_UNLOCKED = "store_unlocked";

    public UpgradesPanelController upgradesPanel;

    GameManager gm;

    void Awake()
    {
        // Unity 6+ replacement for deprecated FindObjectOfType
        gm = FindFirstObjectByType<GameManager>();

        // Ensure idle has a timestamp as soon as Den exists
        IdleSystem.EnsureStartStamp();
    }

    void OnEnable()
    {
        GameManager.OnBankChanged += UpdateWispsUI;

        // Initial UI sync
        UpdateWispsUI(CurrentBank());
        RefreshIdleUI();
        RefreshStoreUI();
    }

    void OnDisable()
    {
        GameManager.OnBankChanged -= UpdateWispsUI;
    }

    // ---------- Open / Close ----------
    public void Open()
    {
        gameObject.SetActive(true);

        IdleSystem.EnsureStartStamp();
        RefreshIdleUI();
        UpdateWispsUI(CurrentBank());
        RefreshStoreUI();

        if (fader) fader.FadeIn();
    }

    public void Close()
    {
        if (fader) fader.FadeOut(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
    }

    // ---------- Helpers ----------
    int CurrentBank()
    {
        if (gm) return gm.GetWispsBank();
        return PlayerPrefs.GetInt("wisps_total", 0);
    }

    // ---------- UI Refresh ----------
    void UpdateWispsUI(int total)
    {
        if (wispsValue) wispsValue.text = total.ToString();
        RefreshStoreUI();
    }

    void RefreshIdleUI()
    {
        IdleSystem.EnsureStartStamp();

        int claim = IdleSystem.GetClaimableWisps();
        float stored = IdleSystem.GetStoredHours();
        float capHrs  = IdleSystem.GetEffectiveHoursCap();
        float rate    = IdleSystem.GetEffectiveEmbersPerHour();

        if (idleText)
        {
            idleText.text =
                $"You can claim <b>{claim:N0}</b> Embers\n" +
                $"({rate:0.#}/hr \u2022 {stored:0.0}/{capHrs:0.0}h stored)";
        }

        if (wakeUpButton) wakeUpButton.interactable = (claim > 0);
}



    void RefreshStoreUI()
    {
        bool unlocked = PlayerPrefs.GetInt(KEY_STORE_UNLOCKED, 0) == 1;

        if (unlockUpgradesButton) unlockUpgradesButton.gameObject.SetActive(!unlocked);
        if (openUpgradesButton)   openUpgradesButton.gameObject.SetActive(unlocked);

        if (!unlocked && unlockUpgradesButton)
        {
            bool canAfford = gm && gm.CanAfford(25);
            unlockUpgradesButton.interactable = canAfford;
        }
    }

        public void RefreshAfterMetaChange()
        {
            RefreshIdleUI();
            UpdateWispsUI(CurrentBank());
            RefreshStoreUI();
        }


    // ---------- Buttons ----------
    public void OnWakeUp()
    {
        IdleSystem.EnsureStartStamp();

        int claim = IdleSystem.GetClaimableWisps();

        if (claim > 0 && gm)
        {
            gm.AddToWispsBank(claim); // add to bank via GameManager
            IdleSystem.Claim();       // reset idle timestamp
        }

        // Refresh everything
        RefreshIdleUI();
        UpdateWispsUI(CurrentBank());
    }

    public void OnUnlockStore()
    {
        const int COST = 25;

        if (gm && gm.TrySpendWisps(COST))
        {
            PlayerPrefs.SetInt(KEY_STORE_UNLOCKED, 1);
            PlayerPrefs.Save();
            RefreshStoreUI();

            // Optional: after unlocking upgrades, refresh idle text as well
            RefreshIdleUI();
        }
    }

    public void OnOpenStore()
    {
        // TODO: open StorePanel when available
        Debug.Log("Open Store: TODO");
    }

    public void OpenUpgradesPanel()
    {
        upgradesPanel?.Open();
    }

    public void OnOpenAchievements() { Debug.Log("Open Achievements: TODO"); }
    public void OnOpenPrestige()     { Debug.Log("Open Prestige: TODO"); }
}
