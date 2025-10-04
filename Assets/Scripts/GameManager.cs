using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public PlayerGravityFlip player;     // your player controller
    public Spawner spawner;              // your obstacle spawner

    [Header("UI - Panels")]
    public GameObject startPanel;
    public GameObject gameOverPanel;

    [Header("UI - Buttons")]
    public Button startButton;
    public Button restartButton;

    [Header("UI - Text")]
    public TMP_Text scoreText;
    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;

    [Header("Options")]
    public bool pauseOnStart = true;

    [Header("Pause")]
    public GameObject pausePanel;     // assign PausePanel
    public UnityEngine.UI.Button pauseButton; // assign PauseButton

    bool paused = false;

    // runtime
    int score = 0;
    bool playing = false;

    void Start()
    {
        // Button hooks
        if (restartButton) restartButton.onClick.AddListener(Restart);
        if (startButton)   startButton.onClick.AddListener(StartGame);

        // Initial state
        playing = false;
        if (player) player.EnableControl(false);

        // Panels visible states + fade instant setup
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

        // Pause world at start (menus still animate using unscaled time)
        if (pauseOnStart) Time.timeScale = 0f;

        score = 0;
        UpdateScoreUI();
        UpdateHighScoreUI();
    }

    // Called by Start button (or TapToStart if you kept it)
    public void StartGame()
    {
        // Fade out StartPanel, then actually begin gameplay
        if (startPanel)
        {
            var fader = startPanel.GetComponent<PanelFader>();
            if (fader)
            {
                fader.FadeOut(() => BeginGameplay());
                return;
            }
            else startPanel.SetActive(false);
        }

        BeginGameplay();
    }

    void BeginGameplay()
    {
        // Unpause and enable systems
        Time.timeScale = 1f;
        score = 0;
        UpdateScoreUI();
        playing = true;

        if (spawner) spawner.Begin();
        if (player) player.EnableControl(true);

        AudioManager.I?.PlayMusic();

    }

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

// Call from PauseButton
public void PauseGame()
{
    if (paused || !playing) return;          // don't pause if not playing
    paused = true;
    player?.EnableControl(false); 
    Time.timeScale = 0f;
    if (pausePanel)
    {
        pausePanel.SetActive(true);
        var f = pausePanel.GetComponent<PanelFader>();
        if (f) f.FadeIn();
    }
    // Optional: dim music when paused (uncomment if desired)
    // AudioManager.I?.SetMusicVolume(Mathf.Max(0.2f, AudioManager.I.CurrentMusic01));
}

    // Call from Resume button
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
    // Optional: restore music (only if you dimmed it above)
        // AudioManager.I?.SetMusicVolume(PlayerPrefs.GetFloat("vol_music", 0.8f));
    }

// Optional: Android Back button / ESC to toggle
void Update()
{
    if (playing && Input.GetKeyDown(KeyCode.Escape))
    {
        if (paused) ResumeGame();
        else        PauseGame();
    }
}

    public void GameOver()
    {
        if (!playing) return;
        playing = false;

        if (spawner) spawner.StopSpawning();
        if (player) player.EnableControl(false);

        // Crash SFX
        AudioManager.I.PlayCrash();

        // Screen shake
        var camShake = Camera.main.GetComponent<ScreenShake>();
        if (camShake) camShake.Shake(0.25f, 0.15f);

        // Save high score
        int hi = PlayerPrefs.GetInt("HighScore", 0);
        if (score > hi)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
        }
        UpdateHighScoreUI();

        // Show final score
        if (finalScoreText) finalScoreText.text = $"Score: {score}";

        // Show Game Over panel (fade if available)
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            var fader = gameOverPanel.GetComponent<PanelFader>();
            if (fader) fader.FadeIn();
        }
    }

    public void Restart()
    {
        // Safety: unpause in case we were paused in menus
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
