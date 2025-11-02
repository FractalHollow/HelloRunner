using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] Button startButton;
    [SerializeField] PanelFader fader;
    [SerializeField] GameManager gm;   // ← drag the scene GameManager here

    [Header("Run Modifiers")]
    [SerializeField] Button runModifiersButton;   // ← assign Btn_RunModifiers (on StartPanel)
    [SerializeField] GameObject runModifiersPanel; // ← assign Panel_RunModifiers (the panel to open)

    CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (!fader) fader = GetComponent<PanelFader>();
    }

    void OnEnable()
    {
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
        Wire();
        UpdateRunModifiersVisibility();
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
        // If it's null, we just skip wiring—feature remains optional.
    }

    void UpdateRunModifiersVisibility()
    {
        bool unlocked = PlayerPrefs.GetInt("mods_unlocked", 0) == 1;
        if (runModifiersButton)
            runModifiersButton.gameObject.SetActive(unlocked);
    }

    void OpenRunModifiers()
    {
        if (!runModifiersPanel)
        {
            Debug.LogWarning("[StartScreen] Run Modifiers Panel not assigned.");
            return;
        }
        runModifiersPanel.SetActive(true);
    }

    void OnStartPressed()
    {
        if (!gm) { Debug.LogError("[StartScreen] GameManager reference missing"); return; }

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

    // Call this after a run ends if you want to refresh visibility without disabling/enabling the panel.
    public void RefreshUI()
    {
        UpdateRunModifiersVisibility();
    }
}
