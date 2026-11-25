using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Main menu actions. Hooked up to the buttons by Tools > Doomspire > Build Main Menu Scene.</summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    private void Start()
    {
        ClosePanels();
    }

    public void Play()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"[MainMenu] Scene '{gameSceneName}' is not in Build Settings (File > Build Profiles > Scene List).");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions() => ShowOnly(optionsPanel);
    public void OpenCredits() => ShowOnly(creditsPanel);
    public void ClosePanels() => ShowOnly(null);

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowOnly(GameObject panel)
    {
        if (optionsPanel != null) optionsPanel.SetActive(panel == optionsPanel);
        if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
    }
}
