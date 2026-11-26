using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Управляет экраном выбора уровня: раскладывает состояния замков, XP-бар и кнопки Back / SKILL TREE.
public class LevelSelectManager : MonoBehaviour
{
    [Header("Levels (по порядку: 1..5)")]
    public LevelNode[] levels;

    [Header("XP bar")]
    public RectTransform xpFill;
    public TMP_Text xpText;

    [Header("Buttons")]
    public Button backButton;
    public Button skillTreeButton;

    void Awake()
    {
        // После Victory / Game Over в GameScene время остановлено — возвращаем его.
        Time.timeScale = 1f;

        foreach (LevelNode node in levels)
        {
            if (node != null)
                node.Clicked += OnLevelClicked;
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (skillTreeButton != null)
            skillTreeButton.onClick.AddListener(OnSkillTreeClicked);
    }

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        int current = CurrentLevel();

        foreach (LevelNode node in levels)
        {
            if (node == null) continue;

            int level = node.levelNumber;
            node.Refresh(
                SaveManager.IsUnlocked(level),
                SaveManager.GetStars(level),
                level == current
            );
        }

        UpdateXpBar();
    }

    // Текущий уровень — первый открытый, но ещё не пройденный. Если всё пройдено — последний.
    int CurrentLevel()
    {
        for (int level = 1; level <= SaveManager.Data.highestUnlockedLevel; level++)
        {
            if (!SaveManager.IsCompleted(level))
                return level;
        }
        return SaveManager.Data.highestUnlockedLevel;
    }

    void UpdateXpBar()
    {
        int xp = SaveManager.XpInCurrentLevel;
        int max = SaveManager.XpPerPlayerLevel;
        float progress = Mathf.Clamp01((float)xp / max);

        if (xpFill != null)
        {
            xpFill.anchorMax = new Vector2(progress, xpFill.anchorMax.y);
            xpFill.gameObject.SetActive(progress > 0.001f);
        }

        if (xpText != null)
            xpText.text = xp + "/" + max;
    }

    void OnLevelClicked(LevelNode node)
    {
        if (!SaveManager.IsUnlocked(node.levelNumber))
        {
            node.PlayLockedFeedback();
            return;
        }

        LevelSession.SelectedLevel = node.levelNumber;
        SceneNames.TryLoad(SceneNames.GameScene);
    }

    void OnBackClicked()
    {
        SceneNames.TryLoad(SceneNames.MainMenu);
    }

    void OnSkillTreeClicked()
    {
        SceneNames.TryLoad(SceneNames.SkillTree);
    }

    // --- Тестовые команды: правый клик по компоненту в Inspector во время Play Mode ---

    [ContextMenu("Debug: пройти текущий уровень на 3 звезды")]
    void DebugCompleteCurrent()
    {
        SaveManager.CompleteLevel(CurrentLevel(), 3, 300);
        Refresh();
    }

    [ContextMenu("Debug: пройти текущий уровень на 1 звезду")]
    void DebugCompleteCurrentOneStar()
    {
        SaveManager.CompleteLevel(CurrentLevel(), 1, 150);
        Refresh();
    }

    [ContextMenu("Debug: сбросить прогресс")]
    void DebugReset()
    {
        SaveManager.ResetProgress();
        Refresh();
    }
}
