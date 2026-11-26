using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Подключает кнопки навигации в сцене боя, не меняя саму сцену Game и GameManager:
//   BackButton (стрелка ←)  -> карта уровней (LevelSelect)
//   MainMenuButton          -> главное меню (MainMenu)
// Работает автоматически при загрузке сцены Game, ставить в сцену ничего не нужно.
public static class GameSceneNavigation
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.GameScene)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // true — включая выключенные объекты (EndScreen скрыт до конца боя).
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "BackButton")
                    Bind(button, SceneNames.LevelSelect);
                else if (button.name == "MainMenuButton")
                    Bind(button, SceneNames.MainMenu);
            }
        }
    }

    static void Bind(Button button, string sceneName)
    {
        // Если Сердар позже сам подключит кнопку в Inspector — не дублируем.
        if (button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.AddListener(() => SceneNames.TryLoad(sceneName));
    }
}
