using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using TMPro;


public class SettingsMenu : MonoBehaviour
{
    const string HowToPlayButtonName = "HowToPlayButton";
    const string HowToPlayButtonLabel = "How to\nPlay";
    const string AboutHowSpacerName = "Spacer_About_HowToPlay";
    const string HowResetSpacerName = "Spacer_HowToPlay_Reset";
    const float SettingsBottomButtonSpacing = 20f;
    const float SettingsPreBottomSpacing = 52f;
    const float SettingsResetCloseSpacing = 34f;

    public Slider musicSlider;
    public Slider sfxSlider;
    public PanelFader fader;
    public AboutMenu aboutMenu;  // drag your AboutPanel here in Inspector

    [Header("Reset Save Data")]
    public UnityEngine.UI.Button resetSaveButton;
    public GameObject confirmResetPanel;
    public UnityEngine.UI.Button confirmResetButton;
    public UnityEngine.UI.Button confirmResetCloseButton;

    Button howToPlayButton;

    void Awake()
    {
        EnsureHowToPlayButton();
    }

    public void OpenAbout()
    {
        aboutMenu?.Open();
    }

    public void OpenHowToPlay()
    {
        FirstRunTutorial tutorial = FirstRunTutorial.FindAvailableTutorial();
        if (!tutorial)
        {
            Debug.LogWarning("[SettingsMenu] FirstRunTutorial not found.");
            return;
        }

        tutorial.BeginManualReview();
    }

    void OnEnable()
    {
        EnsureHowToPlayButton();
        StartCoroutine(SyncWhenReady());

        if (resetSaveButton)
        {
            resetSaveButton.onClick.RemoveAllListeners();
            resetSaveButton.onClick.AddListener(OpenResetConfirmPanel);
        }

        if (confirmResetButton)
        {
            confirmResetButton.onClick.RemoveAllListeners();
            confirmResetButton.onClick.AddListener(PerformFullReset);
        }

        if (confirmResetCloseButton)
        {
            confirmResetCloseButton.onClick.RemoveAllListeners();
            confirmResetCloseButton.onClick.AddListener(CloseResetConfirmPanel);
        }

        HideResetConfirmPanelInstant();
    }

    void EnsureHowToPlayButton()
    {
        Transform buttonsTable = transform.Find("ButtonsTable");
        if (!buttonsTable)
            return;

        Transform aboutButtonTransform = buttonsTable.Find("AboutButton");
        Transform existing = buttonsTable.Find(HowToPlayButtonName);
        if (existing)
        {
            howToPlayButton = existing.GetComponent<Button>();
            WireHowToPlayButton();
            SetButtonText(howToPlayButton, HowToPlayButtonLabel);
            LayoutBottomButtons(buttonsTable, aboutButtonTransform);
            return;
        }

        Button template = aboutButtonTransform
            ? aboutButtonTransform.GetComponent<Button>()
            : resetSaveButton;
        if (!template)
            return;

        GameObject buttonGo = Instantiate(template.gameObject, buttonsTable);
        buttonGo.name = HowToPlayButtonName;
        buttonGo.SetActive(true);

        int insertIndex = aboutButtonTransform
            ? aboutButtonTransform.GetSiblingIndex() + 1
            : buttonsTable.childCount - 1;
        buttonGo.transform.SetSiblingIndex(insertIndex);

        howToPlayButton = buttonGo.GetComponent<Button>();
        SetButtonText(howToPlayButton, HowToPlayButtonLabel);
        WireHowToPlayButton();
        LayoutBottomButtons(buttonsTable, aboutButtonTransform);
    }

    void LayoutBottomButtons(Transform buttonsTable, Transform aboutButtonTransform)
    {
        Button aboutButton = aboutButtonTransform
            ? aboutButtonTransform.GetComponent<Button>()
            : null;
        ResizeLikeResetButton(aboutButton);
        ResizeLikeResetButton(howToPlayButton);
        MatchResetButtonTextSize(howToPlayButton);

        Transform aboutHowSpacer = EnsureSpacer(buttonsTable, AboutHowSpacerName, SettingsBottomButtonSpacing);
        Transform howResetSpacer = buttonsTable.Find("Filler2 (1)")
            ?? EnsureSpacer(buttonsTable, HowResetSpacerName, SettingsBottomButtonSpacing);
        Transform resetCloseSpacer = buttonsTable.Find("Filler2 (2)");
        Transform preBottomSpacer = buttonsTable.Find("Filler2");
        Transform closeButton = buttonsTable.Find("CloseButton");

        SetSpacerHeight(preBottomSpacer, SettingsPreBottomSpacing);
        SetSpacerHeight(howResetSpacer, SettingsBottomButtonSpacing);
        SetSpacerHeight(resetCloseSpacer, SettingsResetCloseSpacing);

        if (!aboutButtonTransform)
            return;

        int nextIndex = preBottomSpacer
            ? preBottomSpacer.GetSiblingIndex() + 1
            : aboutButtonTransform.GetSiblingIndex();

        aboutButtonTransform.SetSiblingIndex(nextIndex++);
        aboutHowSpacer.SetSiblingIndex(nextIndex++);
        howToPlayButton.transform.SetSiblingIndex(nextIndex++);
        howResetSpacer.SetSiblingIndex(nextIndex++);

        if (resetSaveButton)
            resetSaveButton.transform.SetSiblingIndex(nextIndex++);

        if (resetCloseSpacer)
            resetCloseSpacer.SetSiblingIndex(nextIndex++);

        if (closeButton)
            closeButton.SetSiblingIndex(nextIndex);
    }

    void ResizeLikeResetButton(Button button)
    {
        if (!button || !resetSaveButton)
            return;

        RectTransform buttonRect = button.transform as RectTransform;
        RectTransform resetRect = resetSaveButton.transform as RectTransform;
        if (!buttonRect || !resetRect)
            return;

        buttonRect.sizeDelta = resetRect.sizeDelta;
    }

    void MatchResetButtonTextSize(Button button)
    {
        if (!button || !resetSaveButton)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        TMP_Text resetText = resetSaveButton.GetComponentInChildren<TMP_Text>(true);
        if (!text || !resetText)
            return;

        text.enableAutoSizing = resetText.enableAutoSizing;
        text.fontSize = resetText.fontSize;
        text.fontSizeMin = resetText.fontSizeMin;
        text.fontSizeMax = resetText.fontSizeMax;
        text.lineSpacing = resetText.lineSpacing;
    }

    Transform EnsureSpacer(Transform parent, string name, float height)
    {
        Transform spacer = parent.Find(name);
        if (!spacer)
        {
            GameObject spacerGo = new GameObject(name, typeof(RectTransform));
            spacerGo.layer = parent.gameObject.layer;
            spacer = spacerGo.transform;
            spacer.SetParent(parent, false);
        }

        SetSpacerHeight(spacer, height);
        return spacer;
    }

    void SetSpacerHeight(Transform spacer, float height)
    {
        if (!spacer)
            return;

        RectTransform rect = spacer as RectTransform;
        if (!rect)
            return;

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    void WireHowToPlayButton()
    {
        if (!howToPlayButton)
            return;

        howToPlayButton.onClick = new Button.ButtonClickedEvent();
        howToPlayButton.onClick.AddListener(OpenHowToPlay);
    }

    void SetButtonText(Button button, string label)
    {
        if (!button)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text)
            text.text = label;
    }

    void OnDisable()
    {
        HideResetConfirmPanelInstant();
    }

    void OpenResetConfirmPanel()
    {
        if (!confirmResetPanel) return;

        var resetFader = PanelFader.Ensure(confirmResetPanel);
        if (resetFader) resetFader.FadeIn();
        else confirmResetPanel.SetActive(true);
    }

    void CloseResetConfirmPanel()
    {
        if (!confirmResetPanel) return;

        var resetFader = PanelFader.Ensure(confirmResetPanel);
        if (resetFader) resetFader.FadeOut();
        else confirmResetPanel.SetActive(false);
    }

    void HideResetConfirmPanelInstant()
    {
        if (!confirmResetPanel || !confirmResetPanel.activeSelf) return;

        var resetFader = confirmResetPanel.GetComponent<PanelFader>();
        if (resetFader) resetFader.HideInstant();
        else confirmResetPanel.SetActive(false);
    }

    void PerformFullReset()
    {
        // 1. Cache paid skin unlocks
        var paidSkinUnlocks = new System.Collections.Generic.Dictionary<string, int>();
        float? musicVolume = null;
        float? sfxVolume = null;
        bool? musicMuted = null;
        bool? sfxMuted = null;

        if (AudioManager.I != null)
        {
            musicVolume = AudioManager.I.CurrentMusic01;
            sfxVolume = AudioManager.I.CurrentSfx01;
            musicMuted = AudioManager.I.MusicMuted;
            sfxMuted = AudioManager.I.SfxMuted;
        }

        if (CosmeticsManager.I != null)
        {
            var skins = CosmeticsManager.I.GetAllSkins();
            for (int i = 0; i < skins.Count; i++)
            {
                var def = skins[i];
                if (!def) continue;

                if (def.unlockType == SkinDef.UnlockType.Paid)
                {
                    string key = $"skin_unlocked_{def.id}";
                    int unlocked = PlayerPrefs.GetInt(key, 0);
                    paidSkinUnlocks[key] = unlocked;
                }
            }
        }

        // 2. Nuclear wipe
        PlayerPrefs.DeleteAll();

        // 3. Restore paid skin unlocks
        foreach (var kv in paidSkinUnlocks)
        {
            if (kv.Value == 1)
                PlayerPrefs.SetInt(kv.Key, 1);
        }

        if (AudioManager.I != null && musicVolume.HasValue && sfxVolume.HasValue)
        {
            AudioManager.I.SetMusicVolume(musicVolume.Value);
            AudioManager.I.SetSfxVolume(sfxVolume.Value);

            if (musicMuted.HasValue)
                AudioManager.I.SetMusicMuted(musicMuted.Value);

            if (sfxMuted.HasValue)
                AudioManager.I.SetSfxMuted(sfxMuted.Value);
        }

        PlayerPrefs.Save();

        // AudioManager is DontDestroyOnLoad, so force it to seed sane default
        // audio prefs after the wipe before reloading the scene.
        AudioManager.I?.ReinitializeAfterPrefsWipe();

        // 4. Reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    System.Collections.IEnumerator SyncWhenReady()
    {
        // wait a few frames for AudioManager to spawn (scene load order can vary)
        for (int i = 0; i < 10 && !AudioManager.I; i++)
            yield return null;

        SyncFromAudioManager();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        HideResetConfirmPanelInstant();
        SyncFromAudioManager();   // set sliders to saved values once
        if (fader) fader.FadeIn();
    }

    public void Close()
    {
        HideResetConfirmPanelInstant();
        if (fader) fader.FadeOut(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
    }

    // ---- UI events ----
    public void OnMusicChanged(float v)
    {
        if (AudioManager.I) AudioManager.I.SetMusicVolumeFromSlider(v);
    }

    public void OnSfxChanged(float v)
    {
        if (AudioManager.I) AudioManager.I.SetSfxVolumeFromSlider(v);
    }
    public void OnTestMusic()
    {
        AudioManager.I?.TestMusic();
        //SyncFromAudioManager(); (removed sync to avoid overwrite issue)
    }

    public void OnTestSfx()
    {
        AudioManager.I?.TestSfx();
        //SyncFromAudioManager(); (removed sync to avoid overwrite issue)
    }


    // ---- helpers ----
    void SyncFromAudioManager()
    {
        if (!AudioManager.I) return;

        if (musicSlider) musicSlider.SetValueWithoutNotify(AudioManager.I.CurrentMusicSlider01);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(AudioManager.I.CurrentSfxSlider01);
    }

    public void SetMusicSliderWithoutNotify(float v)
    {
        if (musicSlider) musicSlider.SetValueWithoutNotify(v);
    }
    public void SetSfxSliderWithoutNotify(float v)
    {
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(v);
    }

public void DbgPrint(float v)
{
    Debug.Log($"[SM] Slider value = {v:0.###}");
}


}
