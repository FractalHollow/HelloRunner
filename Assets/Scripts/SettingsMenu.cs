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
    public TMPro.TMP_Text resetSaveButtonText;

bool resetConfirmArmed = false;


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
                resetSaveButton.onClick.AddListener(OnResetSaveClicked);
            }

        resetConfirmArmed = false;
        if (resetSaveButtonText)
            resetSaveButtonText.text = "Reset Save Data";

    }

    void OnResetSaveClicked()
    {
        if (!resetConfirmArmed)
        {
            resetConfirmArmed = true;
            if (resetSaveButtonText)
                resetSaveButtonText.text = "Confirm Reset";
            return;
        }

        PerformFullReset();
    }

    void PerformFullReset()
    {
        // 1. Cache paid skin unlocks
        var paidSkinUnlocks = new System.Collections.Generic.Dictionary<string, int>();

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

        PlayerPrefs.Save();

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
        SyncFromAudioManager();   // set sliders to saved values once
        if (fader) fader.FadeIn();
    }

    public void Close()
    {
        if (fader) fader.FadeOut(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
    }

    // ---- UI events ----
    public void OnMusicChanged(float v)
    {
        if (AudioManager.I) AudioManager.I.SetMusicVolume(v);
    }

    public void OnSfxChanged(float v)
    {
        if (AudioManager.I) AudioManager.I.SetSfxVolume(v);
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

        if (musicSlider) musicSlider.SetValueWithoutNotify(AudioManager.I.CurrentMusic01);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(AudioManager.I.CurrentSfx01);
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
