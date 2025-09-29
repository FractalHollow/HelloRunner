using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;
    public PanelFader fader;

    const string KEY_MUSIC = "vol_music";
    const string KEY_SFX   = "vol_sfx";

    void OnEnable()
    {
        float mv = PlayerPrefs.GetFloat(KEY_MUSIC, 0.8f);
        float sv = PlayerPrefs.GetFloat(KEY_SFX,   1.0f);
        if (musicSlider) musicSlider.SetValueWithoutNotify(mv);
        if (sfxSlider)   sfxSlider.SetValueWithoutNotify(sv);
    }

    public void Open()   // must be public, no parameters
    {
        gameObject.SetActive(true);
        if (fader) fader.FadeIn();
    }

    public void Close()  // must be public, no parameters
    {
        if (fader) fader.FadeOut(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
    }

    public void OnMusicChanged(float v)
    {
        if (AudioManager.I) AudioManager.I.SetMusicVolume(v);
    }

    public void OnSfxChanged(float v)
    {
        if (AudioManager.I) AudioManager.I.SetSfxVolume(v);
    }
}
