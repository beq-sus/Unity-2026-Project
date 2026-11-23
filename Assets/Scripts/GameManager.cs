
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int startingCoins = 1000;
    public int totalEnemies = 24;

    [Header("HUD")]
    public TMP_Text coinsCounter;
    public TMP_Text enemyCounter;

    [Header("End Screen")]
    public GameObject endScreen;
    public TMP_Text resultTitle;

    private int coins;
    private int defeatedEnemies = 0;

    public bool IsGameEnded { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        coins = startingCoins;
    }

    void Start()
    {
        Time.timeScale = 1f;
        IsGameEnded = false;

        if (endScreen != null)
            endScreen.SetActive(false);

        UpdateUI();
    }

    public void EnemyDefeated(int reward)
    {
        if (IsGameEnded) return;

        defeatedEnemies++;
        coins += reward;

        UpdateUI();

        Debug.Log("Enemy defeated! Coins: " + coins);
    }

    void UpdateUI()
    {
        if (coinsCounter != null)
            coinsCounter.text = coins.ToString();

        if (enemyCounter != null)
            enemyCounter.text =
                defeatedEnemies + " / " + totalEnemies;
    }

    public void WinGame()
    {
        ShowEndScreen("VICTORY!", Color.yellow);
    }

    public void LoseGame()
    {
        ShowEndScreen("GAME OVER", Color.red);
    }

    void ShowEndScreen(string message, Color color)
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        if (resultTitle != null)
        {
            resultTitle.text = message;
            resultTitle.color = color;
        }

        if (endScreen != null)
            endScreen.SetActive(true);

        Time.timeScale = 0f;

        Debug.Log(message);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    [ContextMenu("Test Victory")]
    void TestVictory()
    {
        if (Application.isPlaying)
            WinGame();
    }

    [ContextMenu("Test Game Over")]
    void TestGameOver()
    {
        if (Application.isPlaying)
            LoseGame();
    }
}
