using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;


public class SettingsMenu : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;
    public PanelFader fader;
    public AboutMenu aboutMenu;  // drag your AboutPanel here in Inspector

    [Header("Reset Save Data")]
    public UnityEngine.UI.Button resetSaveButton;
    public GameObject confirmResetPanel;
    public UnityEngine.UI.Button confirmResetButton;
    public UnityEngine.UI.Button confirmResetCloseButton;


    public void OpenAbout()
    {
        aboutMenu?.Open();
    }

    void OnEnable()
    {
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

    void OnDisable()
    {
        HideResetConfirmPanelInstant();
    }

    void OpenResetConfirmPanel()
    {
        if (confirmResetPanel)
            confirmResetPanel.SetActive(true);
    }

    void CloseResetConfirmPanel()
    {
        if (confirmResetPanel)
            confirmResetPanel.SetActive(false);
    }

    void HideResetConfirmPanelInstant()
    {
        if (confirmResetPanel && confirmResetPanel.activeSelf)
            confirmResetPanel.SetActive(false);
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
