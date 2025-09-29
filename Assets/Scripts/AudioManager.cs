using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;   // singleton instance
    public AudioMixer mixer;        // assign your Master mixer in Inspector

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // Slider [0..1] mapped to -80..0 dB
    float ToDecibels(float v) => Mathf.Lerp(-80f, 0f, Mathf.Clamp01(v));

    public void SetMusicVolume(float v)
    {
        mixer.SetFloat("MusicVol", ToDecibels(v));
        PlayerPrefs.SetFloat("vol_music", v);
    }

    public void SetSfxVolume(float v)
    {
        mixer.SetFloat("SFXVol", ToDecibels(v));
        PlayerPrefs.SetFloat("vol_sfx", v);
    }
}
