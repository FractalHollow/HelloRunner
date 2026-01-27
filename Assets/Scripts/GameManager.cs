using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public PlayerGravityFlip player;
    public Spawner spawner;
    public WispSpawner wispSpawner;
    public static event System.Action<int> OnBankChanged;

    [Header("Run Scoring")]
    public DistanceTracker distanceTracker;
    public ScoreSystem scoreSystem;

    [Header("UI - Panels")]
    public GameObject startPanel;
    public GameObject gameOverPanel;

    [Header("UI - Buttons")]
    public Button startButton;    // not required (StartScreen wires button)
    public Button restartButton; //"End Run" button on Pause Panel

    [Header("UI - Text (HUD)")]
    public TMP_Text distanceText;     // "123 m"
    public TMP_Text scoreText;        // "Score: 456"
    public TMP_Text wispTotalHUD;     // "Wisps: 1234" (total bank)
    float _hudProbeTimer = 0f;
    public bool hudProbeEnabled = true; // toggle off when done

    [Header("UI - Text (Game Over)")]
    public TMP_Text finalScoreText;   // "Score: 456"
    public TMP_Text highScoreText;    // "Best: 9999"
    public TMP_Text finalDistanceText;// "Distance: 123 m"
    public TMP_Text bestDistanceText; // "Best Distance: 456 m"
    public TMP_Text wispsRunText;     // "+123 Wisps"
    public TMP_Text wispTotalFinal;   // "Total Wisps: 2345"

    [Header("UI - Settings")]
    public SettingsMenu settingsMenu;   // drag your Settings Panel object (with SettingsMenu.cs) here

    [Header("Options")]
    public bool pauseOnStart = true;

    [Header("Pause")]
    public GameObject pausePanel;
    public Button pauseButton;

    [Header("Prestige Difficulty Scaling")]
    public int prestigeDifficultyStart = 3;     // no scaling until this prestige
    public float prestigeDifficultyStep = 0.05f; // +5% speed per prestige AFTER start
    public float prestigeDifficultyMax = 1.6f;   // cap

    int PrestigeLevel => PlayerPrefs.GetInt("prestige_level", 0);

    float DifficultyMultiplier()
    {
        int steps = Mathf.Max(0, PrestigeLevel - prestigeDifficultyStart);
        float mult = 1f + steps * prestigeDifficultyStep;
        return Mathf.Min(mult, prestigeDifficultyMax);
    }


    // spawn reset
    public Vector3 playerStartPos;
    public Quaternion playerStartRot;

    public UpgradesPanelController upgradesPanel; // assign in Inspector later

    // state
    bool paused = false;
    bool playing = false;
    public bool IsPlaying => playing;

    // --- Run Modifiers tuning ---
    [Header("Run Modifiers Tuning")]
    public float speedMultWhenOn = 1.5f;   // world speed when Speed mod is ON
    public float rewardBonusPerMod = 0.25f; // +25% score/embers per enabled mod

    // --- Convenience flags pulled from PlayerPrefs ---
    public bool ModSpeedOn   => PlayerPrefs.GetInt("mod_speed_on", 0) == 1;
    public bool ModHazardsOn => PlayerPrefs.GetInt("mod_hazards_on", 0) == 1;

    // --- Multipliers other systems can read ---
    public float RunSpeedMultiplier
    {
        get
        {
            float modMult = ModSpeedOn ? speedMultWhenOn : 1f;
            return modMult * DifficultyMultiplier();
        }
    }

    static GameManager _inst;

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;

        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }

        if (player)
        {
            playerStartPos = player.transform.position;
            playerStartRot = player.transform.rotation;
        }

        wispsTotal = PlayerPrefs.GetInt("wisps_total", 0);
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // on Android this is fine; device will still vsync
        
        playing = false;
        if (player) player.EnableControl(false);

        if (startPanel)
        {
            startPanel.SetActive(true);
            var f = startPanel.GetComponent<PanelFader>();
            if (f) f.ShowInstant();
        }
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(false);
            var f = gameOverPanel.GetComponent<PanelFader>();
            if (f) f.HideInstant();
        }

        if (pauseOnStart) Time.timeScale = 0f;

        UpdateHighScoreUI();
        UpdateWispHUD();
        UpdateUILive();

        StartCoroutine(EnsureSceneLocal());
        ApplyAllOwnedUpgrades();
    }

    IEnumerator EnsureSceneLocal()
    {
        yield return null;
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }
    }

    // -------------------- GAME FLOWS --------------------

    public void StartGame()
    {
        Time.timeScale = 1f;
        paused = false;
        HideGameOverPanel();

        if (spawner) spawner.StopSpawning();
        if (wispSpawner) wispSpawner.StopSpawning();
        ClearWorld();
        ResetPlayerToStart();

        Debug.Log($"[D2] Prestige={PlayerPrefs.GetInt("prestige_level", 0)} | " +
                $"DifficultyMult={DifficultyMultiplier():0.00} | " +
                $"ModSpeedOn={ModSpeedOn} | RunSpeedMult={RunSpeedMultiplier:0.00}");

        // Record stats at run start (persists across prestiges)
        StatsManager.RecordRunStarted(ModSpeedOn, ModHazardsOn);
        StatsManager.Save();

        // Reset currency for this run
        wispsRun = 0;
        UpdateWispHUD();

        // Reset run scoring
        if (distanceTracker)
        {
            distanceTracker.ResetRun();
            distanceTracker.tracking = true;
        }

        // Self-wire ScoreSystem → tracker in case it wasn't set in Inspector
        if (scoreSystem && !scoreSystem.tracker)
            scoreSystem.tracker = distanceTracker;

        // Ensure multipliers sane
        if (scoreSystem && scoreSystem.baseMultiplier <= 0f)
            scoreSystem.baseMultiplier = 1f;

        // Reset per-run shields (based on owned level applied earlier)
        if (player)
        {
            var ps = player.GetComponent<PlayerShield>();
            if (ps) ps.SetCharges(ps.maxCharges);
        }

        BeginGameplay();
        UpdateUILive();
    }

    void BeginGameplay()
    {
        Time.timeScale = 1f;
        playing = true;

        if (spawner) spawner.Begin();
        if (wispSpawner) wispSpawner.StartSpawning();
        if (player) player.EnableControl(true);

        AudioManager.I?.PlayMusic();
    }

    public void GameOver()
    {

        int runDistM = (int)(distanceTracker ? distanceTracker.distance : 0f);
        // bestDistance is updated by StopAndRecordBest(), so call that first (you already do)

        // Update "best distance this prestige" gate
        int cur = PlayerPrefs.GetInt("prestige_best_distance_m", 0);
        if (runDistM > cur)
        {
            PlayerPrefs.SetInt("prestige_best_distance_m", runDistM);
            PlayerPrefs.Save();
            Debug.Log($"[PrestigeGate] Updated this-prestige best: {runDistM}m");
        }
        else
        {
            Debug.Log($"[PrestigeGate] No update. this-prestige best stays {cur}m (run {runDistM}m)");
        }

        
        if (!playing) return;
        playing = false;

        if (spawner) spawner.StopSpawning();
        if (wispSpawner) wispSpawner.StopSpawning();
        if (player) player.EnableControl(false);
        if (player)
        {
            var ps = player.GetComponent<PlayerShield>();
            if (ps) ps.StopAllRegen(); 
        }
        if (distanceTracker) distanceTracker.StopAndRecordBest();

        AudioManager.I?.PlayCrash();

        var cam = Camera.main;
        if (cam)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake) shake.Shake(0.25f, 0.15f);
        }

        int bestDistM = (int)(distanceTracker ? distanceTracker.bestDistance : 0f);

        // Final scoring snapshot (includes run-modifier bonus)
        int finalScore = ComputeScoreWithMods();

        // lifetime distance
        StatsManager.AddLifetimeDistance(runDistM);
        StatsManager.Save();

        // High score
        int hi = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > hi)
        {
            PlayerPrefs.SetInt("HighScore", finalScore);
        }

        int bank = GetWispsBank();
        bank += wispsRun;
        SetWispsBank(bank);
        UpdateHighScoreUI();
        UpdateWispHUD();

        // Fill final UI
        if (finalScoreText) finalScoreText.text = $"Score: {finalScore:N0}";
        if (finalDistanceText) finalDistanceText.text = $"Distance: {(int)(distanceTracker ? distanceTracker.distance : 0f)} m";
        if (bestDistanceText) bestDistanceText.text = $"Best Distance: {(int)(distanceTracker ? distanceTracker.bestDistance : 0f)} m";
        if (wispsRunText) wispsRunText.text = $"+{wispsRun} Embers";
        if (wispTotalFinal) wispTotalFinal.text = $"Total Embers: {bank:N0}";
        if (scoreText) scoreText.text = $"Score: {finalScore:N0}";
        if (distanceText) distanceText.text = $"{(int)(distanceTracker ? distanceTracker.distance : 0f)} m";

        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            var fader = gameOverPanel.GetComponent<PanelFader>();
            if (fader) fader.FadeIn();
        }

        int runEmbersEarned = wispsRun;
        AchievementManager.I?.EvaluateUnlocksOnGameOver(bestDistM, runDistM, finalScore, runEmbersEarned);

        // reset run currency so it can't be re-added accidentally
        wispsRun = 0;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // -------------------- UI / INPUT --------------------

    // Old scoring shim (safe to delete after removing calls elsewhere)
    public void AddPoint() { /* no-op */ }

    void UpdateHighScoreUI()
    {
        if (!highScoreText) return;
        int hi = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = $"Best: {hi:N0}";
    }

    void UpdateWispHUD()
    {
        // HUD shows Embers collected THIS RUN
        if (wispTotalHUD) wispTotalHUD.text = $"Embers: {wispsRun}";
    }

    public void PauseGame()
    {
        if (paused || !playing) return;
        paused = true;
        player?.EnableControl(false);
        Time.timeScale = 0f;

        if (pausePanel)
        {
            pausePanel.SetActive(true);
            var f = pausePanel.GetComponent<PanelFader>();
            if (f) f.FadeIn();
        }
    }

    public void ResumeGame()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = 1f;

        if (pausePanel)
        {
            var f = pausePanel.GetComponent<PanelFader>();
            if (f) f.FadeOut(() => pausePanel.SetActive(false));
            else pausePanel.SetActive(false);
        }
        player?.EnableControl(true);
    }

    void Update()
{
    if (playing && Input.GetKeyDown(KeyCode.Escape))   // Android Back maps to Escape
    {
        if (paused)
        {
            if (settingsMenu && settingsMenu.gameObject.activeSelf) settingsMenu.Close();
            else ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    if (playing) UpdateUILive();
}



    void UpdateUILive()
    {
        if (distanceText && distanceTracker)
            distanceText.text = $"Dist: {(int)distanceTracker.distance} m";

        if (scoreText && scoreSystem)
            scoreText.text = $"Score: {ComputeScoreWithMods():N0}";

    }

// -------------------- CURRENCY --------------------

// ====== In-Memory Totals ======
int wispsRun = 0;     // earned this run
int wispsTotal = 0;   // total bank (mirrors PlayerPrefs)

// ====== Core Accessors ======

    // Read the total from PlayerPrefs
    public int GetWispsBank()
    {
        return PlayerPrefs.GetInt("wisps_total", 0);
    }

    // Write a new total and refresh UI
    public void SetWispsBank(int value)
    {
        value = Mathf.Max(0, value);
        PlayerPrefs.SetInt("wisps_total", value);
        PlayerPrefs.Save();

        wispsTotal = value;            // keep local copy in sync
        RefreshAllCurrencyUI();        // update any open panels       
        OnBankChanged?.Invoke(value);  // 🔔 Notify listeners (e.g., DenMenu)
    }

    // Add or subtract from the bank
    public void AddToWispsBank(int delta)
    {
        int newValue = GetWispsBank() + delta;
        SetWispsBank(newValue);
    }

    // Check if player can afford a cost
    public bool CanAfford(int cost)
    {
        return GetWispsBank() >= cost;
    }

    // Try to spend Wisps; return true if successful
    public bool TrySpendWisps(int amount)
    {
        if (amount <= 0) return true;

        int bank = GetWispsBank();
        if (bank < amount) return false;

        AddToWispsBank(-amount);
        AudioManager.I?.PlayPurchase();
        return true;
    }

// Add Wisps earned during this run (scaled by run modifiers)
public void AddWisps(int baseAmount)
{
    if (!playing) return;           // ✅ prevents post-death pickups
    if (baseAmount <= 0) return;

    int finalAmount = Mathf.Max(1, Mathf.RoundToInt(baseAmount * CurrentWispMultiplier()));
    wispsRun += finalAmount;

    UpdateWispHUD();               // live HUD update (run only)
    AudioManager.I?.PlayPickup();  // pickup sound

    StatsManager.AddLifetimeEmbersEarned(finalAmount);

}


// ====== UI Refresh ======

// Update all panels that show totals
public void RefreshAllCurrencyUI()
{
    UpdateWispHUD();               // run HUD (shows this-run or bank depending on your design)
    upgradesPanel?.RefreshAll();   // refresh buttons/prices
    // If you later have a DenPanel, refresh it here:
    // denPanel?.RefreshTotals(GetWispsBank());
}


    // -------------------- HELPERS --------------------

    void ResetPlayerToStart()
    {
        if (!player) return;

        var t = player.transform;
        t.position = playerStartPos;
        t.rotation = playerStartRot;

        var rb = t.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        try { player.ResetState(); } catch { /* ok if not implemented */ }

        player.EnableControl(false); // enable in BeginGameplay
    }

    void ClearWorld()
    {
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var o in obstacles) Destroy(o);

        var wisps = FindObjectsOfType<WispPickup>();
        foreach (var w in wisps) Destroy(w.gameObject);
    }

    void HideGameOverPanel()
    {
        if (!gameOverPanel) return;
        var f = gameOverPanel.GetComponent<PanelFader>();
        if (f) f.HideInstant();
        gameOverPanel.SetActive(false);
    }

    // ==================== UPGRADES / EMBERS HELPERS ====================

    // Difficulty modifier flags (read by Spawner at Begin())
    //public bool mod_EnemyVerticalMovement;
    //public bool mod_EnemyProjectiles;

    // Called after purchases or when panel opens to refresh labels/buttons
    public void RefreshUpgradesUI()
    {
        upgradesPanel?.RefreshAll();
    }

    // Convenience opener for your Den button
    public void OpenUpgradesPanel()
    {
        upgradesPanel?.Open();
    }

    // Apply effects for purchased upgrades and update score modifier
    public void ApplyUpgrade(UpgradeDef def)
    {
        if (def == null) return;
        int level = PlayerPrefs.GetInt($"upgrade_{def.id}", 0);

    // --- Run Modifiers unlock via purchase ---
        if (def.id == "mods_info")
        {
            int levelb = PlayerPrefs.GetInt($"upgrade_{def.id}", 0);
            if (levelb >= 1 && PlayerPrefs.GetInt("mods_unlocked", 0) == 0)
            {
                PlayerPrefs.SetInt("mods_unlocked", 1);
                PlayerPrefs.Save();
                FindFirstObjectByType<StartScreen>()?.RefreshLockUI();
            }
            RefreshUpgradesUI();
            return;
        }

        switch (def.effectType)
        {
            case UpgradeDef.EffectType.ComboBoost:
                {
                    if (!scoreSystem) break;

                    // v0PerTier = literal score multiplier (e.g. 1.5 = x1.5)
                    float mult = def.GetV0(level);
                    scoreSystem.upgradeMultiplier = Mathf.Max(1f, mult);
                    break;
                }

            case UpgradeDef.EffectType.SmallerHitbox:
                {
                    var tight = FindFirstObjectByType<ColliderTightener2D>();
                    if (tight) tight.ApplyLevel(level);
                    break;
                }

            case UpgradeDef.EffectType.Magnet:
                {
                    var magnet = FindFirstObjectByType<PlayerMagnet>();
                    if (!magnet && player) magnet = player.GetComponent<PlayerMagnet>();
                    if (!magnet && player) magnet = player.gameObject.AddComponent<PlayerMagnet>();

                    if (!magnet || level <= 0)
                    {
                        if (magnet) magnet.enabled = false;
                        break;
                    }

                    // Pull per-tier numbers from UpgradeDef
                    // v0 = radius (meters), v1 = pullSpeed, v2 = maxChaseSpeed
                    float r  = def.GetV0(level);
                    float ps = def.GetV1(level);
                    float ms = def.GetV2(level);

                    // Provide sane fallbacks if tiers left blank
                    magnet.radius        = (r  > 0f) ? r  : 3.5f + 0.8f * (level - 1);
                    magnet.pullSpeed     = (ps > 0f) ? ps : 8f;
                    magnet.maxChaseSpeed = (ms > 0f) ? ms : 14f;

                    magnet.enabled = true;
                    break;
                }


            case UpgradeDef.EffectType.Shield:
            {
                if (!player) break;

                var ps = player.GetComponent<PlayerShield>() ?? player.gameObject.AddComponent<PlayerShield>();

                // --- Charges from V0 (fallback to old logic if V0 not filled) ---
                int charges;
                if (level > 0)
                {
                    // V0 is authoring-time "charges per tier": e.g. [1,2,2]
                    charges = Mathf.RoundToInt(def.GetV0(level));
                    if (charges <= 0) charges = (level >= 2) ? 2 : (level >= 1 ? 1 : 0); // safety
                }
                else charges = 0;

                ps.maxCharges = charges;
                ps.charges    = Mathf.Min(ps.charges, ps.maxCharges);
                ps.RefreshVisual();

                // --- Regen at L3+ using V1 cooldown seconds (e.g. [0,0,12]) ---
                bool enableRegen = (level >= 3);
                ps.regenEnabled  = enableRegen;

                if (enableRegen)
                {
                    float cd = def.GetV1(level);          // cooldown seconds for this tier
                    if (cd > 0f) ps.regenCooldown = cd;   // keep existing default if not provided
                }

                // Start/stop the coroutine based on flag (method lives on PlayerShield)
                ps.BeginRegenIfNeeded();

                break;
            }

            case UpgradeDef.EffectType.IdleRate:
                {
                    // Each tier adds flat wisps/hour
                    int bonus = Mathf.RoundToInt(def.GetV0(level));
                    PlayerPrefs.SetInt("idle_rate_bonus", bonus);
                    break;
                }

             case UpgradeDef.EffectType.IdleCapacity:
                {
                    // Each tier adds hours of storage
                    float bonusHours = def.GetV0(level);
                    PlayerPrefs.SetFloat("idle_hours_cap_bonus", bonusHours);
                    break;
                }

            case UpgradeDef.EffectType.ShieldIFrames:
                {
                    if (!player) break;
                    var ps = player.GetComponent<PlayerShield>() ?? player.gameObject.AddComponent<PlayerShield>();

                    // v0PerTier = invuln seconds (absolute value per tier) OR bonus seconds — pick one.
                    // I recommend absolute values per tier for clarity.
                    float seconds = def.GetV0(level);
                    if (seconds > 0f) ps.invulnDuration = seconds;

                    break;
                }


        }

        // Update score bonus from difficulty toggles
        if (scoreSystem)
        {
            float modBonus = 1f;
            //if (mod_EnemyVerticalMovement) modBonus *= 1.10f; // +10% score
            //if (mod_EnemyProjectiles)      modBonus *= 1.20f; // +20% score
            scoreSystem.modifierMultiplier = modBonus;
        }

        RefreshUpgradesUI();


    }

    void ApplyAllOwnedUpgrades()
    {
        var defs = Resources.LoadAll<UpgradeDef>("Upgrades");
        foreach (var d in defs)
        {
            // Re-use your method so each upgrade applies itself
            ApplyUpgrade(d);
        }
    }

    public void OpenSettingsProxy()
    {
        if (settingsMenu)
        {
            settingsMenu.Open();
            Debug.Log("[GM] SettingsMenu.Open() called on " + settingsMenu.gameObject.name);
        }
        else
        {
            Debug.LogWarning("[GM] settingsMenu ref is null");
        }
    }




        public float CurrentScoreMultiplier()
        {
            float mult = 1f;
            if (ModSpeedOn)   mult += rewardBonusPerMod;
            if (ModHazardsOn) mult += rewardBonusPerMod;
            // later: add prestige/achievements here
            mult *= PrestigeManager.ScoreMult;

            return mult;
        }

            public float CurrentWispMultiplier()
            {
                float mult = 1f;
                if (ModSpeedOn) mult += rewardBonusPerMod;
                if (ModHazardsOn) mult += rewardBonusPerMod;

                mult *= PrestigeManager.WispMult;

                return mult;
            }

        int ComputeScoreWithMods()
        {
            int raw = scoreSystem ? scoreSystem.CurrentScore : 0;
            float mult = CurrentScoreMultiplier();
            return Mathf.Max(0, Mathf.RoundToInt(raw * mult));
        }

    // Called by PausePanel “End Run” button
    public void EndRun()
    {
        // Only valid during a run; ignore if already at start or already game over
        if (!playing) return;

        // Ensure we’re not paused so faders/timers use unscaled/normal flow
        // (If your faders use unscaled time, this is still fine.)
        paused = false;
        Time.timeScale = 1f;

        // Hide Pause overlay if it’s up
        if (pausePanel)
        {
            var f = pausePanel.GetComponent<PanelFader>();
            if (f) f.HideInstant();
            pausePanel.SetActive(false);
        }

        // Stop immediate control & spawning to avoid one more frame of inputs/spawns
        if (player) player.EnableControl(false);
        if (spawner) spawner.StopSpawning();
        if (wispSpawner) wispSpawner.StopSpawning();

        // Let the standard Game Over flow do the rest (distance snapshot, hi score, banking, UI)
        GameOver();
    }


}
