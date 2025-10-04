using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle musicMuteToggle;
    public Toggle sfxMuteToggle;
    public PanelFader fader;
    public AboutMenu aboutMenu;  // drag your AboutPanel here in Inspector

    public void OpenAbout()
    {
        aboutMenu?.Open();
    }


    void OnEnable()
    {
        SyncFromAudioManager();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        SyncFromAudioManager();
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

    public void OnMusicMuteChanged(bool muted)
    {
        if (AudioManager.I) AudioManager.I.SetMusicMuted(muted);
    }

    public void OnSfxMuteChanged(bool muted)
    {
        if (AudioManager.I) AudioManager.I.SetSfxMuted(muted);
    }

    public void OnTestMusic()
    {
        AudioManager.I?.TestMusic();
        // Optionally ensure sliders/toggles reflect that we unmuted for the test
        SyncFromAudioManager();
    }

    public void OnTestSfx()
    {
        AudioManager.I?.TestSfx();
        SyncFromAudioManager();
    }


    // ---- helpers ----
    void SyncFromAudioManager()
    {
        if (!AudioManager.I) return;

        if (musicSlider)     musicSlider.SetValueWithoutNotify(AudioManager.I.CurrentMusic01);
        if (sfxSlider)       sfxSlider.SetValueWithoutNotify(AudioManager.I.CurrentSfx01);
        if (musicMuteToggle) musicMuteToggle.SetIsOnWithoutNotify(AudioManager.I.MusicMuted);
        if (sfxMuteToggle)   sfxMuteToggle.SetIsOnWithoutNotify(AudioManager.I.SfxMuted);
    }
}
