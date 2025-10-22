using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Mixer (optional but recommended)")]
    public AudioMixer mixer;                 // expose "MusicVol" & "SFXVol" in this mixer
    [SerializeField] string musicVolParam = "MusicVol";
    [SerializeField] string sfxVolParam   = "SFXVol";

    [Header("Audio Sources")]
    public AudioSource musicSource;          // route to Mixer Music group
    public AudioSource sfxSource;            // route to Mixer SFX group

    [Header("Clips")]
    public AudioClip musicLoop;
    public AudioClip flipClip;
    public AudioClip crashClip;
    public AudioClip sfxPurchase;
    [SerializeField] private AudioClip pickupSFX;  // wisp pickup

    [Header("Volumes")]
    [Range(0f, 1f)] public float sfxPurchaseVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float pickupVolume = 0.8f;

    // PlayerPrefs keys
    const string KEY_MUSIC      = "vol_music";
    const string KEY_SFX        = "vol_sfx";
    const string KEY_MUTE_MUSIC = "mute_music";
    const string KEY_MUTE_SFX   = "mute_sfx";

    // cached state
    float music01;
    float sfx01;
    bool muteMusic;
    bool muteSfx;

    const float MIN_AUDIBLE = 0.001f;  // floor to avoid -80 dB unless explicitly muted

    // Slider floor so a tiny move can't drive the mixer to -80dB.
    // 0.1 ≈ -20 dB (quiet but still audible). Adjust later if you like.
    public const float SLIDER_FLOOR = 0.1f;


    [ContextMenu("Audio: Reset to Defaults")]
public void ResetToDefaults()
{
    // Defaults
    music01 = 0.8f;
    sfx01   = 1.0f;
    muteMusic = false;
    muteSfx   = false;

    PlayerPrefs.SetFloat("vol_music", music01);
    PlayerPrefs.SetFloat("vol_sfx",   sfx01);
    PlayerPrefs.SetInt("mute_music",  0);
    PlayerPrefs.SetInt("mute_sfx",    0);
    PlayerPrefs.Save();

    ApplyMusicVolume();
    ApplySfxVolume();

    Debug.Log("[Audio] Reset to defaults.");
}

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();

        // Load saved
        music01   = PlayerPrefs.GetFloat(KEY_MUSIC, 0.8f);
        sfx01     = PlayerPrefs.GetFloat(KEY_SFX,   1.0f);
        muteMusic = PlayerPrefs.GetInt(KEY_MUTE_MUSIC, 0) == 1;
        muteSfx   = PlayerPrefs.GetInt(KEY_MUTE_SFX,   0) == 1;

        // Apply immediately
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    // Ensure we have two distinct sources; never reuse the same component for both
    void EnsureSources()
    {
        // MUSIC
        if (!musicSource)
        {
            var go = new GameObject("MusicSource");
            go.transform.SetParent(transform);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        // SFX
        if (!sfxSource || sfxSource == musicSource)
        {
            var go = new GameObject("SFXSource");
            go.transform.SetParent(transform);
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
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
    PlayerPrefs.SetInt(KEY_MUTE_MUSIC, muted ? 1 : 0);
    ApplyMusicVolume();
}

public void SetSfxMuted(bool muted)
{
    muteSfx = muted;
    PlayerPrefs.SetInt(KEY_MUTE_SFX, muted ? 1 : 0);
    ApplySfxVolume();
}



 void ApplyMusicVolume()
{
    float db = muteMusic ? -80f : ToDecibels(music01);
    if (mixer) { mixer.SetFloat(musicVolParam, db); }
    else if (musicSource) { musicSource.mute = muteMusic; musicSource.volume = music01; }
    Debug.Log($"[AM] ApplyMusicVolume -> {(mixer ? "Mixer" : "Source")} db={db:0.##} vol={music01:0.###} mute={muteMusic}");
}
void ApplySfxVolume()
{
    float db = muteSfx ? -80f : ToDecibels(sfx01);
    if (mixer) { mixer.SetFloat(sfxVolParam, db); }
    else if (sfxSource) { sfxSource.mute = muteSfx; sfxSource.volume = sfx01; }
    Debug.Log($"[AM] ApplySfxVolume -> {(mixer ? "Mixer" : "Source")} db={db:0.##} vol={sfx01:0.###} mute={muteSfx}");
}


    // ---------------- playback helpers ----------------
    public void PlayMusic()
    {
        if (!musicSource || !musicLoop) return;
        if (musicSource.clip != musicLoop) musicSource.clip = musicLoop;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void PlayFlip()
    {
        if (flipClip && sfxSource && !muteSfx) sfxSource.PlayOneShot(flipClip, sfx01);
    }

    public void PlayCrash()
    {
        if (crashClip && sfxSource && !muteSfx) sfxSource.PlayOneShot(crashClip, sfx01);
    }

    public void PlayPurchase()
    {
        if (sfxPurchase && sfxSource && !muteSfx)
            sfxSource.PlayOneShot(sfxPurchase, sfxPurchaseVolume * sfx01);
    }

    public void PlayPickup()
    {
        if (pickupSFX && sfxSource && !muteSfx)
            sfxSource.PlayOneShot(pickupSFX, pickupVolume * sfx01);
    }

    // --- Test helpers for the settings menu ---
    public void TestMusic()
    {
        SetMusicMuted(false);
        PlayMusic();
        if (musicSource) musicSource.time = 0f;
    }

    public void TestSfx()
    {
        SetSfxMuted(false);
        var clip = flipClip ? flipClip : crashClip;
        if (clip && sfxSource) sfxSource.PlayOneShot(clip, sfx01);
    }

    // Expose current states so SettingsMenu can sync UI
    public float CurrentMusic01 => music01;
    public float CurrentSfx01   => sfx01;
    public bool  MusicMuted     => muteMusic;
    public bool  SfxMuted       => muteSfx;
}
