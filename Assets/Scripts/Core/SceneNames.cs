using UnityEngine;
using UnityEngine.SceneManagement;

// Имена сцен в одном месте, чтобы не писать строки по всему проекту.
public static class SceneNames
{
    public const string MainMenu = "MainMenu";
    public const string LevelSelect = "LevelSelect";
    public const string GameScene = "Game";
    public const string SkillTree = "SkillTree";

    // Загружает сцену, если она добавлена в Build Profiles / Scene List.
    // Если сцены ещё нет (например, MainMenu или SkillTree не готовы) — пишет предупреждение и возвращает false.
    public static bool TryLoad(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("Сцена '" + sceneName + "' не найдена в Scene List. " +
                             "Создайте её и добавьте в File > Build Profiles.");
            return false;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
        return true;
    }
}
