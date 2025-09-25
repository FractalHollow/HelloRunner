using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public Spawner spawner;
    public PlayerGravityFlip player;

    [Header("UI (TMP)")]
    public TMP_Text scoreText;
    public GameObject startPanel;        // NEW
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button startButton;           // optional
    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;       // optional

    int score = 0;
    bool playing = false;

    void Start()
    {
        if (restartButton) restartButton.onClick.AddListener(Restart);
        if (startButton)   startButton.onClick.AddListener(StartGame);

        // Start on menu
        Time.timeScale = 0f;
        playing = false;
        player.EnableControl(false);
        if (startPanel) startPanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateScoreUI();
        UpdateHighScoreUI();
    }

    public void StartGame()
{
    // fade out the start panel, then begin gameplay
    if (startPanel)
    {
        var fader = startPanel.GetComponent<PanelFader>();
        if (fader)
        {
            fader.FadeOut(onComplete: () =>
            {
                BeginGameplay();
            });
            return; // prevent falling through
        }
        else startPanel.SetActive(false);
    }

    BeginGameplay();
}

void BeginGameplay()
{
    Time.timeScale = 1f;
    score = 0;
    UpdateScoreUI();
    playing = true;
    spawner.Begin();
    player.EnableControl(true);
}


    public void AddPoint()
    {
        if (!playing) return;
        score++;
        UpdateScoreUI();
    }

    void UpdateScoreUI() { if (scoreText) scoreText.text = score.ToString(); }

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
        spawner.StopSpawning();
        player.EnableControl(false);

        // High score
        int hi = PlayerPrefs.GetInt("HighScore", 0);
        if (score > hi) { PlayerPrefs.SetInt("HighScore", score); PlayerPrefs.Save(); }
        UpdateHighScoreUI();

        var fader = gameOverPanel.GetComponent<PanelFader>();
        if (fader) fader.FadeIn();
        else gameOverPanel.SetActive(true);

        // Optional freeze after panel appears:
        // StartCoroutine(FreezeAfter(0.1f));
    }

    IEnumerator FreezeAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
