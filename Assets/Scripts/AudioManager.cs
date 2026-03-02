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
    public AudioSource flipSource; // dedicated source so pitch jitter doesn't affect other SFX

    [Header("Clips")]
    public AudioClip musicLoop;
    public AudioClip flipClip;
    public AudioClip crashClip;
    public AudioClip sfxPurchase;
    [SerializeField] private AudioClip pickupSFX;      // wisp pickup
    public AudioClip sfxShootDefault;                  // optional default if caller passes null
    [Range(0f, 1f)] public float shootVolume = 0.8f;

    [Header("Flip SFX Pitch Variation")]
    [Range(0f, 0.2f)] public float flipPitchJitter = 0.04f; // ~±4% subtle

    [Header("Volumes")]
    [Range(0f, 1f)] public float sfxPurchaseVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float pickupVolume = 0.8f;

    // PlayerPrefs keys
    const string KEY_MUSIC      = "vol_music";
    const string KEY_SFX        = "vol_sfx";
    const string KEY_MUTE_MUSIC = "mute_music"; // legacy (kept for backward safety)
    const string KEY_MUTE_SFX   = "mute_sfx";   // legacy (kept for backward safety)

    // Default values for a *fresh install / wiped prefs*
    const float DEFAULT_MUSIC = 0.6f;
    const float DEFAULT_SFX   = 0.8f;

    // cached state
    float music01;
    float sfx01;
    bool muteMusic;   // legacy
    bool muteSfx;     // legacy

    // ---------------- volume mapping ----------------
    float ToDecibels(float v) => (v <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(v)) * 20f;

    // ---------------- Unity lifecycle ----------------
    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();

        EnsureDefaultAudioPrefsIfMissing();
        LoadPrefs();
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    void EnsureDefaultAudioPrefsIfMissing()
    {
        bool changed = false;

        // If keys don't exist, it's a fresh install (or prefs wiped)
        if (!PlayerPrefs.HasKey(KEY_MUSIC)) { PlayerPrefs.SetFloat(KEY_MUSIC, DEFAULT_MUSIC); changed = true; }
        if (!PlayerPrefs.HasKey(KEY_SFX))   { PlayerPrefs.SetFloat(KEY_SFX,   DEFAULT_SFX);   changed = true; }

        // Legacy mute keys — keep them sane, but your UI uses sliders now
        if (!PlayerPrefs.HasKey(KEY_MUTE_MUSIC)) { PlayerPrefs.SetInt(KEY_MUTE_MUSIC, 0); changed = true; }
        if (!PlayerPrefs.HasKey(KEY_MUTE_SFX))   { PlayerPrefs.SetInt(KEY_MUTE_SFX,   0); changed = true; }

        if (changed) PlayerPrefs.Save();
    }

    void LoadPrefs()
    {
        // Load saved
        music01   = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, DEFAULT_MUSIC));
        sfx01     = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX,   DEFAULT_SFX));

        // Legacy mute state (kept to avoid breaking older flows)
        muteMusic = PlayerPrefs.GetInt(KEY_MUTE_MUSIC, 0) == 1;
        muteSfx   = PlayerPrefs.GetInt(KEY_MUTE_SFX,   0) == 1;

        // IMPORTANT: your current game uses sliders; treat "0" as mute regardless of legacy flags
        if (music01 <= 0.0001f) muteMusic = true;
        if (sfx01   <= 0.0001f) muteSfx   = true;
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
        sfxSource.spatialBlend = 0f;

        // FLIP (dedicated)
        if (!flipSource || flipSource == musicSource || flipSource == sfxSource)
        {
            var go = new GameObject("FlipSource");
            go.transform.SetParent(transform);
            flipSource = go.AddComponent<AudioSource>();
            flipSource.loop = false;
            flipSource.playOnAwake = false;
            flipSource.spatialBlend = 0f;
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    // ---------------- public API used by UI ----------------
    public void SetMusicVolume(float v)
    {
        music01 = Mathf.Clamp01(v);

        // Slider-driven "mute"
        muteMusic = (music01 <= 0.0001f);

        PlayerPrefs.SetFloat(KEY_MUSIC, music01);
        PlayerPrefs.SetInt(KEY_MUTE_MUSIC, muteMusic ? 1 : 0); // keep legacy in sync
        PlayerPrefs.Save();

        ApplyMusicVolume();
    }

    public void SetSfxVolume(float v)
    {
        sfx01 = Mathf.Clamp01(v);

        // Slider-driven "mute"
        muteSfx = (sfx01 <= 0.0001f);

        PlayerPrefs.SetFloat(KEY_SFX, sfx01);
        PlayerPrefs.SetInt(KEY_MUTE_SFX, muteSfx ? 1 : 0); // keep legacy in sync
        PlayerPrefs.Save();

        ApplySfxVolume();
    }

    // Legacy mute API (kept so any old callers won't break)
    public void SetMusicMuted(bool muted)
    {
        muteMusic = muted;
        PlayerPrefs.SetInt(KEY_MUTE_MUSIC, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public void SetSfxMuted(bool muted)
    {
        muteSfx = muted;
        PlayerPrefs.SetInt(KEY_MUTE_SFX, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    void ApplyMusicVolume()
    {
        // If slider is at 0, treat as mute no matter what
        bool effectiveMute = muteMusic || (music01 <= 0.0001f);
        float db = effectiveMute ? -80f : ToDecibels(music01);

        if (mixer) { mixer.SetFloat(musicVolParam, db); }
        else if (musicSource) { musicSource.mute = effectiveMute; musicSource.volume = music01; }
    }

    void ApplySfxVolume()
    {
        bool effectiveMute = muteSfx || (sfx01 <= 0.0001f);
        float db = effectiveMute ? -80f : ToDecibels(sfx01);

        if (mixer) { mixer.SetFloat(sfxVolParam, db); }
        else if (sfxSource) { sfxSource.mute = effectiveMute; sfxSource.volume = sfx01; }
    }

    // ---------------- playback helpers ----------------
    public void PlayMusic()
    {
        if (!musicSource || !musicLoop) return;

        ApplyMusicVolume(); // ensure volume/mute is current

        if (musicSource.clip != musicLoop) musicSource.clip = musicLoop;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void Play2D(AudioClip clip, float vol01 = 1f)
    {
        if (!clip || !sfxSource) return;
        if (muteSfx || sfx01 <= 0.0001f) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(vol01) * sfx01);
    }

    public void PlayShoot(AudioClip clipOverride = null, float vol01 = -1f)
    {
        var clip = clipOverride ? clipOverride : sfxShootDefault;
        if (!clip) return;
        float v = (vol01 >= 0f) ? vol01 : shootVolume;
        Play2D(clip, v);
    }

    public void PlayFlip()
    {
        EnsureSources();

        if (!flipClip || !flipSource) return;
        if (muteSfx || sfx01 <= 0.0001f) return;

        float jitter = Mathf.Clamp(flipPitchJitter, 0f, 0.5f);
        float newPitch = 1f + Random.Range(-jitter, jitter);

        flipSource.pitch = newPitch;
        flipSource.PlayOneShot(flipClip, sfx01);

        Debug.Log($"[Audio] Flip pitch = {newPitch:0.000}");
    }

    public void PlayCrash()
    {
        if (!crashClip || !sfxSource) return;
        if (muteSfx || sfx01 <= 0.0001f) return;

        sfxSource.PlayOneShot(crashClip, sfx01);
    }

    public void PlayPurchase()
    {
        if (!sfxPurchase || !sfxSource) return;
        if (muteSfx || sfx01 <= 0.0001f) return;

        sfxSource.PlayOneShot(sfxPurchase, sfxPurchaseVolume * sfx01);
    }

    public void PlayPickup()
    {
        if (!pickupSFX || !sfxSource) return;
        if (muteSfx || sfx01 <= 0.0001f) return;

        sfxSource.PlayOneShot(pickupSFX, pickupVolume * sfx01);
    }

    // --- Test helpers for the settings menu ---
    public void TestMusic()
    {
        // In slider-only world, "unmute" just means volume > 0
        if (music01 <= 0.0001f) SetMusicVolume(DEFAULT_MUSIC);
        PlayMusic();
        if (musicSource) musicSource.time = 0f;
    }

    public void TestSfx()
    {
        if (sfx01 <= 0.0001f) SetSfxVolume(DEFAULT_SFX);

        // Use the same path as gameplay so pitch jitter is tested too
        PlayFlip();
    }

    [ContextMenu("Audio: Reset to Defaults")]
    public void ResetToDefaults()
    {
        music01 = DEFAULT_MUSIC;
        sfx01   = DEFAULT_SFX;

        muteMusic = false;
        muteSfx   = false;

        PlayerPrefs.SetFloat(KEY_MUSIC, music01);
        PlayerPrefs.SetFloat(KEY_SFX,   sfx01);
        PlayerPrefs.SetInt(KEY_MUTE_MUSIC, 0);
        PlayerPrefs.SetInt(KEY_MUTE_SFX,   0);
        PlayerPrefs.Save();

        ApplyMusicVolume();
        ApplySfxVolume();
    }

    // Expose current states so SettingsMenu can sync UI
    public float CurrentMusic01 => music01;
    public float CurrentSfx01   => sfx01;

    // Legacy (kept)
    public bool  MusicMuted     => muteMusic;
    public bool  SfxMuted       => muteSfx;
}