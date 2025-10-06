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

    [Header("UI - Panels")]
    public GameObject startPanel;
    public GameObject gameOverPanel;

    [Header("UI - Buttons")]
    public Button startButton;    // not required (StartScreen wires button), ok to leave null
    public Button restartButton;

    [Header("UI - Text")]
    public TMP_Text scoreText;
    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;

    [Header("Options")]
    public bool pauseOnStart = true;

    [Header("Pause")]
    public GameObject pausePanel;
    public Button pauseButton;

    // spawn reset
    public Vector3 playerStartPos;
    public Quaternion playerStartRot;

    // state
    bool paused = false;
    int score = 0;
    bool playing = false;

    // singleton guard (prevents dupes)
    static GameManager _inst;

    // -------------------- LIFECYCLE --------------------

    void Awake()
    {
        // kill duplicates
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;

        // if someone shoved us into DDOL, move back to active scene
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
    }

    void Start()
    {
        // Initial state
        playing = false;
        if (player) player.EnableControl(false);

        // Panels initial states
        if (startPanel)
        {
            startPanel.SetActive(true);
            var f = startPanel.GetComponent<PanelFader>();
            if (f) f.ShowInstant(); // alpha=1, interactable=true, blocksRaycasts=true
        }
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(false);
            var f = gameOverPanel.GetComponent<PanelFader>();
            if (f) f.HideInstant();
        }

        // Pause world at start (menus animate with unscaled time)
        if (pauseOnStart) Time.timeScale = 0f;

        score = 0;
        UpdateScoreUI();
        UpdateHighScoreUI();

        // last-chance safety in case someone moved us after Awake
        StartCoroutine(EnsureSceneLocal());
    }

    IEnumerator EnsureSceneLocal()
    {
        yield return null; // one frame later
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            Debug.LogWarning("[GM] Moved back to scene in Start().");
        }
    }

    // -------------------- GAME FLOWS --------------------

    // Called by StartScreen (after it fades & disables itself)
    public void StartGame()
    {
        Time.timeScale = 1f;
        paused = false;
        HideGameOverPanel();

        // Reset world state
        if (spawner)     spawner.StopSpawning();
        if (wispSpawner) wispSpawner.StopSpawning();
        ClearWorld();
        ResetPlayerToStart();

        // Fresh score/UI
        score = 0;
        UpdateScoreUI();

        // Begin new run
        BeginGameplay();
    }

    void BeginGameplay()
    {
        Debug.Log("[GM] BeginGameplay()");
        Time.timeScale = 1f;
        playing = true;

        if (spawner)     spawner.Begin();
        if (wispSpawner) wispSpawner.StartSpawning();
        if (player)      player.EnableControl(true);

        AudioManager.I?.PlayMusic();
    }

    public void GameOver()
    {
        if (!playing) return;
        playing = false;

        if (spawner)     spawner.StopSpawning();
        if (wispSpawner) wispSpawner.StopSpawning();
        if (player)      player.EnableControl(false);

        AudioManager.I?.PlayCrash();

        var cam = Camera.main;
        if (cam)
        {
            var shake = cam.GetComponent<ScreenShake>();
            if (shake) shake.Shake(0.25f, 0.15f);
        }

        int hi = PlayerPrefs.GetInt("HighScore", 0);
        if (score > hi)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
        }
        UpdateHighScoreUI();

        if (finalScoreText) finalScoreText.text = $"Score: {score}";

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

    public void AddPoint()
    {
        if (!playing) return;
        score++;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = score.ToString();
    }

    void UpdateHighScoreUI()
    {
        if (!highScoreText) return;
        int hi = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = $"Best: {hi}";
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
            else  pausePanel.SetActive(false);
        }
        player?.EnableControl(true);
    }

    void Update()
    {
        if (playing && Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused) ResumeGame();
            else        PauseGame();
        }
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
            // If your gravity uses rb.gravityScale sign, normalize here if needed:
            // rb.gravityScale = Mathf.Abs(rb.gravityScale);
        }

        // If you created this helper in PlayerGravityFlip, great; if not, remove this call.
        try { player.ResetState(); } catch { /* ok if not implemented */ }

        player.EnableControl(false); // enable in BeginGameplay
    }

    void ClearWorld()
    {
        // obstacles: tag them "Obstacle"
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var o in obstacles) Destroy(o);

        // wisps by component
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
}
