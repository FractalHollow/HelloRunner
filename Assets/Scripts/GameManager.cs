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

    public void GameOver()
    {
        if (!playing) return;
        playing = false;

        if (spawner) spawner.StopSpawning();
        if (player)  player.EnableControl(false);

        // Crash SFX
        AudioManager.I.PlayCrash();

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
