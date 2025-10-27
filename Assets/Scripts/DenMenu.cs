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

    public UpgradesPanelController upgradesPanel;

    GameManager gm;

    void Awake()
    {
        gm = FindObjectOfType<GameManager>();
    }

    void OnEnable()
    {
        // Subscribe to GameManager bank changes
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
        return gm ? gm.GetWispsBank() : PlayerPrefs.GetInt("wisps_total", 0);
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
        {
            bool canAfford = gm && gm.CanAfford(50);
            unlockUpgradesButton.interactable = canAfford;
        }
    }

    // ---------- Buttons ----------
    public void OnWakeUp()
    {
        int claim = IdleSystem.GetClaimableWisps();
        if (claim > 0 && gm)
        {
            gm.AddToWispsBank(claim);   // ✅ add to bank via GameManager
            IdleSystem.Claim();
        }
        RefreshIdleUI();
        UpdateWispsUI(CurrentBank());
    }

    public void OnUnlockStore()
    {
        const int COST = 50;
        if (gm && gm.TrySpendWisps(COST)) // ✅ spend via GameManager
        {
            PlayerPrefs.SetInt(KEY_STORE_UNLOCKED, 1);
            PlayerPrefs.Save();
            RefreshStoreUI();
        }
    }

    public void OnOpenStore()
    {
        // TODO: open StorePanel when available
        Debug.Log("Open Upgrades: TODO");
    }

    public void OpenUpgradesPanel()
    {
        upgradesPanel?.Open();
    }

    public void OnOpenAchievements() { Debug.Log("Open Achievements: TODO"); }
    public void OnOpenPrestige()     { Debug.Log("Open Prestige: TODO"); }
}
