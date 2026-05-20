using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartScreen : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] Button startButton;
    [SerializeField] PanelFader fader;
    [SerializeField] GameManager gm;   // drag the scene GameManager here
    [SerializeField] FirstRunTutorial tutorial;

    [Header("Run Modifiers")]
    [SerializeField] Button runModifiersButton;     // Btn_RunModifiers on StartPanel
    [SerializeField] GameObject runModifiersPanel;  // Panel_RunModifiers to open

    [Header("Leaderboards")]
    [SerializeField] Button leaderboardsButton;

    [Header("Prestige")]
    [SerializeField] TMP_Text prestigeText; // assign in inspector (optional)

    CanvasGroup cg;

    void Awake()
    {
        if (!fader) fader = GetComponent<PanelFader>();
        if (!fader) fader = PanelFader.Ensure(gameObject);
        cg = GetComponent<CanvasGroup>();
        if (!tutorial) tutorial = GetComponent<FirstRunTutorial>();
    }

    void OnEnable()
    {
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
        Wire();
        RefreshLockUI();  // lock/unlock the button based on purchase
    }

    void Wire()
    {
        // Start button
        if (!startButton) startButton = GetComponentInChildren<Button>(true);
        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartPressed);
        }
        else
        {
            Debug.LogWarning("[StartScreen] Start Button reference missing.");
        }

        // Run Modifiers button
        if (runModifiersButton)
        {
            runModifiersButton.onClick.RemoveAllListeners();
            runModifiersButton.onClick.AddListener(OpenRunModifiers);
        }

        if (!leaderboardsButton)
            leaderboardsButton = FindButtonByName("LeaderboardsButton");

        if (leaderboardsButton)
        {
            leaderboardsButton.onClick.RemoveAllListeners();
            leaderboardsButton.onClick.AddListener(PlayGamesLeaderboardService.ShowLeaderboards);
        }
    }

    Button FindButtonByName(string buttonName)
    {
        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] && buttons[i].name == buttonName)
                return buttons[i];
        }

        return null;
    }

    public static void SetVisibleInLayout(GameObject go, bool visible)
    {
        if (!go) return;

        var cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();

        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    void OpenRunModifiers()
    {
        if (!runModifiersPanel)
        {
            Debug.LogWarning("[StartScreen] Run Modifiers Panel not assigned.");
            return;
        }

        var modifiers = runModifiersPanel.GetComponent<RunModifiersPanel>();
        if (modifiers) modifiers.Open();
        else runModifiersPanel.SetActive(true);
    }

    void OnStartPressed()
    {
        if (!gm) { Debug.LogError("[StartScreen] GameManager reference missing"); return; }
        if (tutorial && tutorial.IsShowing) return;

        if (tutorial && tutorial.ShouldShow())
        {
            tutorial.Begin(StartGameFromMenu);
            return;
        }

        StartGameFromMenu();
    }

    void StartGameFromMenu()
    {
        if (fader && fader.isActiveAndEnabled)
            fader.FadeOut(() => { DisablePanel(); gm.StartGame(); });
        else
        {
            DisablePanel();
            gm.StartGame();
        }
    }

    void DisablePanel()
    {
        if (cg) { cg.interactable = false; cg.blocksRaycasts = false; cg.alpha = 0f; }
        gameObject.SetActive(false);
    }

    // Keep the button visible, but disable it until the purchasable unlock has been bought.
    public void RefreshLockUI()
    {
        bool unlocked = PlayerPrefs.GetInt("mods_unlocked", 0) == 1;

        if (runModifiersButton)
        {
            ApplyRunModifiersButtonState(unlocked);
        }

        RefreshPrestigeUI();
    }

    void ApplyRunModifiersButtonState(bool unlocked)
    {
        var buttonObject = runModifiersButton.gameObject;
        var buttonGroup = buttonObject.GetComponent<CanvasGroup>();
        if (!buttonGroup) buttonGroup = buttonObject.AddComponent<CanvasGroup>();

        buttonGroup.alpha = 1f;
        buttonGroup.interactable = unlocked;
        buttonGroup.blocksRaycasts = unlocked;

        runModifiersButton.interactable = unlocked;

        var pulse = buttonObject.GetComponentInChildren<TapPromptPulse>(true);
        if (pulse) pulse.enabled = unlocked;
    }

    void RefreshPrestigeUI()
    {
        if (!prestigeText) return;

        int lvl = PrestigeManager.EffectiveLevel;
        if (lvl <= 0)
        {
            prestigeText.gameObject.SetActive(false);
            return;
        }

        prestigeText.gameObject.SetActive(true);
        prestigeText.text = $"Prestige {UIIntFormatter.Format(lvl)} - x{PrestigeManager.ScoreMult:0.#} Score & Embers";
    }

    // Legacy alias if other code calls it
    public void RefreshUI() => RefreshLockUI();
}
