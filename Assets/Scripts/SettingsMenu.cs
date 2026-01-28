using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;
    public PanelFader fader;
    public AboutMenu aboutMenu;  // drag your AboutPanel here in Inspector

    public void OpenAbout()
    {
        aboutMenu?.Open();
    }

    void OnEnable()
    {
        StartCoroutine(SyncWhenReady());
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
