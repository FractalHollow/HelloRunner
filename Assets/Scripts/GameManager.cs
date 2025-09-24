using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // <-- add this

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public Spawner spawner;
    public PlayerController2D player;

    [Header("UI (TMP)")]
    public TMP_Text scoreText;        // <-- TMP_Text instead of Text
    public GameObject gameOverPanel;
    public Button restartButton;      // Button stays the same (UI Button)
    public TMP_Text finalScoreText;   // optional

    int score = 0;
    bool playing = false;

    void Start()
    {
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        Begin();
    }

    public void Begin()
    {
        score = 0;
        UpdateScoreUI();
        playing = true;
        if (gameOverPanel) gameOverPanel.SetActive(false);
        spawner?.Begin();
        player?.EnableControl(true);
        Time.timeScale = 1f;
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

    public void GameOver()
    {
        if (!playing) return;
        playing = false;
        spawner?.StopSpawning();
        player?.EnableControl(false);

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = $"Score: {score}";
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
