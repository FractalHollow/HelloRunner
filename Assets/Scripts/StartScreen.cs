using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    [SerializeField] Button startButton;
    [SerializeField] PanelFader fader;
    [SerializeField] GameManager gm;   // ← drag the scene GameManager here

    CanvasGroup cg;

    void Awake() {
        cg = GetComponent<CanvasGroup>();
        if (!fader) fader = GetComponent<PanelFader>();
    }

    void OnEnable() {
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
        Wire();
    }

    void Wire() {
        if (!startButton) startButton = GetComponentInChildren<Button>(true);
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartPressed);
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

    void DisablePanel() {
        if (cg) { cg.interactable = false; cg.blocksRaycasts = false; cg.alpha = 0f; }
        gameObject.SetActive(false);
    }
}
