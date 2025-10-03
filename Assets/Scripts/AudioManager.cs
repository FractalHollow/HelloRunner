using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;
    public AudioMixer mixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;   // Output -> Master/Music
    public AudioSource sfxSource;     // Output -> Master/SFX

    [Header("Clips")]
    public AudioClip musicLoop;
    public AudioClip flipClip;
    public AudioClip crashClip;

    // PlayerPrefs keys
    const string KEY_MUSIC = "vol_music";
    const string KEY_SFX   = "vol_sfx";
    const string KEY_MUTE_MUSIC = "mute_music";
    const string KEY_MUTE_SFX   = "mute_sfx";

    // cached for convenience
    float music01, sfx01;
    bool muteMusic, muteSfx;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Load saved
        music01  = PlayerPrefs.GetFloat(KEY_MUSIC, 0.8f);
        sfx01    = PlayerPrefs.GetFloat(KEY_SFX,   1.0f);
        muteMusic = PlayerPrefs.GetInt(KEY_MUTE_MUSIC, 0) == 1;
        muteSfx   = PlayerPrefs.GetInt(KEY_MUTE_SFX,   0) == 1;

        // Apply
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    // ---------------- volume mapping ----------------
    float ToDecibels(float v) => (v <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(v)) * 20f;
    float FromDecibels(float db) => (db <= -80f) ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f));

    // ---------------- public API used by UI ----------------
    public void SetMusicVolume(float v)
    {
        music01 = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_MUSIC, music01);
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float v)
    {
        sfx01 = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_SFX, sfx01);
        ApplySfxVolume();
    }

    public void SetMusicMuted(bool muted)
    {
        muteMusic = muted;
        PlayerPrefs.SetInt(KEY_MUTE_MUSIC, muteMusic ? 1 : 0);
        ApplyMusicVolume();
    }

    public void SetSfxMuted(bool muted)
    {
        muteSfx = muted;
        PlayerPrefs.SetInt(KEY_MUTE_SFX, muteSfx ? 1 : 0);
        ApplySfxVolume();
    }

    void ApplyMusicVolume()
    {
        float db = muteMusic ? -80f : ToDecibels(music01);
        if (mixer) mixer.SetFloat("MusicVol", db);
    }

    void ApplySfxVolume()
    {
        float db = muteSfx ? -80f : ToDecibels(sfx01);
        if (mixer) mixer.SetFloat("SFXVol", db);
    }

    public void PlayMusic()
    {
        if (!musicSource || !musicLoop) return;
        musicSource.clip = musicLoop;
        musicSource.loop = true;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void PlayFlip()
    {
        if (flipClip && sfxSource) sfxSource.PlayOneShot(flipClip);
    }

    public void PlayCrash()
    {
        if (crashClip && sfxSource) sfxSource.PlayOneShot(crashClip);
    }

    // test helpers for the settings menu
    public void TestMusic()
    {
        // ensure audible test
        SetMusicMuted(false);
        if (!musicSource || !musicLoop) return;
        PlayMusic();
        musicSource.time = 0f;
    }

    public void TestSfx()
    {
        SetSfxMuted(false);
        var clip = flipClip ? flipClip : crashClip;
        if (clip && sfxSource) sfxSource.PlayOneShot(clip);
    }

    // Expose current states so SettingsMenu can sync UI
    public float CurrentMusic01 => music01;
    public float CurrentSfx01   => sfx01;
    public bool  MusicMuted     => muteMusic;
    public bool  SfxMuted       => muteSfx;
}
