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

    [Header("Run Scoring")]
    public DistanceTracker distanceTracker;
    public ScoreSystem scoreSystem;

    [Header("UI - Panels")]
    public GameObject startPanel;
    public GameObject gameOverPanel;

    [Header("UI - Buttons")]
    public Button startButton;    // not required (StartScreen wires button)
    public Button restartButton;

    [Header("UI - Text (HUD)")]
    public TMP_Text distanceText;     // "123 m"
    public TMP_Text scoreText;        // "Score: 456"
    public TMP_Text wispTotalHUD;     // "Wisps: 1234" (total bank)

    [Header("UI - Text (Game Over)")]
    public TMP_Text finalScoreText;   // "Score: 456"
    public TMP_Text highScoreText;    // "Best: 9999"
    public TMP_Text finalDistanceText;// "Distance: 123 m"
    public TMP_Text bestDistanceText; // "Best Distance: 456 m"
    public TMP_Text wispsRunText;     // "+123 Wisps"
    public TMP_Text wispTotalFinal;   // "Total Wisps: 2345"

    [Header("Options")]
    public bool pauseOnStart = true;

    [Header("Pause")]
    public GameObject pausePanel;
    public Button pauseButton;

    // spawn reset
    public Vector3 playerStartPos;
    public Quaternion playerStartRot;

    public UpgradesPanelController upgradesPanel; // assign in Inspector later

    // state
    bool paused = false;
    bool playing = false;

    // currency
    int wispsRun = 0;
    int wispsTotal = 0;

    static GameManager _inst;

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;

        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            Debug.LogWarning("[GM] Was in DontDestroyOnLoad — moved back to scene.");
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
            Debug.LogWarning("[GM] Moved back to scene in Start().");
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
        Debug.Log("[GM] BeginGameplay()");
        Time.timeScale = 1f;
        playing = true;

        if (spawner) spawner.Begin();
        if (wispSpawner) wispSpawner.StartSpawning();
        if (player) player.EnableControl(true);

        AudioManager.I?.PlayMusic();
    }

    public void GameOver()
    {
        if (!playing) return;
        playing = false;

        if (spawner) spawner.StopSpawning();
        if (wispSpawner) wispSpawner.StopSpawning();
        if (player) player.EnableControl(false);

        if (distanceTracker) distanceTracker.StopAndRecordBest();

        AudioManager.I?.PlayCrash();

        var cam = Camera.main;
        if (cam)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake) shake.Shake(0.25f, 0.15f);
        }

        // Final scoring snapshot
        int finalScore = scoreSystem ? scoreSystem.CurrentScore : 0;

        // High score
        int hi = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > hi)
        {
            PlayerPrefs.SetInt("HighScore", finalScore);
        }

        // Bank run wisps into total
        wispsTotal += wispsRun;
        PlayerPrefs.SetInt("wisps_total", wispsTotal);
        PlayerPrefs.Save();

        UpdateHighScoreUI();
        UpdateWispHUD();

        // Fill final UI
        if (finalScoreText) finalScoreText.text = $"Score: {finalScore:N0}";
        if (finalDistanceText) finalDistanceText.text = $"Distance: {(int)(distanceTracker ? distanceTracker.distance : 0f)} m";
        if (bestDistanceText) bestDistanceText.text = $"Best Distance: {(int)(distanceTracker ? distanceTracker.bestDistance : 0f)} m";
        if (wispsRunText) wispsRunText.text = $"+{wispsRun} Embers";
        if (wispTotalFinal) wispTotalFinal.text = $"Total Embers: {wispsTotal:N0}";
        if (scoreText) scoreText.text = $"Score: {finalScore:N0}";
        if (distanceText) distanceText.text = $"{(int)(distanceTracker ? distanceTracker.distance : 0f)} m";

        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            var fader = gameOverPanel.GetComponent<PanelFader>();
            if (fader) fader.FadeIn();
        }
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
        if (playing && Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused) ResumeGame();
            else PauseGame();
        }

        if (playing) UpdateUILive();
    }

    void UpdateUILive()
    {
        if (distanceText && distanceTracker)
            distanceText.text = $"{(int)distanceTracker.distance} m";

        if (scoreText && scoreSystem)
            scoreText.text = $"Score: {scoreSystem.CurrentScore:N0}";
    }

    // -------------------- CURRENCY --------------------
    public void AddWisps(int amount)
    {
        if (amount <= 0) return;
        wispsRun += amount;
        UpdateWispHUD();               // <- updates HUD immediately
        AudioManager.I?.PlayPickup();  // plays your pickup sound
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

// Current bank from PlayerPrefs (keep in sync with your wispsTotal field)
public int GetWispsBank() => PlayerPrefs.GetInt("wisps_total", 0);
public bool CanAfford(int cost) => GetWispsBank() >= cost;

// Spend from the BANK (not this-run). Also refresh Upgrades UI if open.
public bool TrySpendWisps(int amount)
{
    if (amount <= 0) return true;
    int bank = PlayerPrefs.GetInt("wisps_total", 0);
    if (bank < amount) return false;
    bank -= amount;
    PlayerPrefs.SetInt("wisps_total", bank);
    PlayerPrefs.Save();

    // keep your in-memory field in sync if you have one
    try { wispsTotal = bank; } catch { /* ok if field name differs */ }

    RefreshUpgradesUI();
    return true;
}

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

        switch (def.effectType)
        {
            case UpgradeDef.EffectType.ComboBoost:
                if (scoreSystem) scoreSystem.upgradeMultiplier = 1f + 0.1f * level;
                break;

            case UpgradeDef.EffectType.SmallerHitbox:
                {
                    var tight = FindObjectOfType<ColliderTightener2D>();
                    if (tight) tight.ApplyLevel(level);
                    break;
                }

            case UpgradeDef.EffectType.Magnet:
                {
                    // Find or add the magnet component on the player
                    var magnet = FindObjectOfType<PlayerMagnet>();
                    if (!magnet && player) magnet = player.GetComponent<PlayerMagnet>();
                    if (!magnet && player) magnet = player.gameObject.AddComponent<PlayerMagnet>();

                    if (magnet)
                    {
                        if (level <= 0)
                        {
                            magnet.enabled = false;
                        }
                        else
                        {
                            // Simple scale: base 1.5 + 0.5 per level (tweak as you like)
                            magnet.radius = 3.5f + 0.5f * (level - 1);
                            magnet.pullSpeed = 6f;       // you can tune later
                            magnet.maxChaseSpeed = 10f;  // cap movement speed of pickups
                            magnet.enabled = true;
                        }
                    }
                    break;
                }

            case UpgradeDef.EffectType.Shield:
                {
                    if (!player) break;
                    var ps = player.GetComponent<PlayerShield>();
                    if (!ps) ps = player.gameObject.AddComponent<PlayerShield>();

                    // Example tiering: L1=1 charge, L2=2 charges, L3=2 charges + (regen later)
                    int charges = 0;
                    if (level == 1) charges = 1;
                    else if (level >= 2) charges = 2;

                    // We only *store* desired max; actual per-run reset happens in StartGame()
                    ps.maxCharges = charges;
                    ps.charges = Mathf.Min(ps.charges, ps.maxCharges);
                    ps.RefreshVisual(); // if you make it public; otherwise keep internal
                    break;
                }




                break;
                
            // case UpgradeDef.EffectType.RunModifier_Vertical:
            //     mod_EnemyVerticalMovement = level > 0;
            //     break;

            //case UpgradeDef.EffectType.RunModifier_Projectiles:
            //    mod_EnemyProjectiles = level > 0;
            //    break;
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
}
