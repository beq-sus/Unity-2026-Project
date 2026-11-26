using System;
using System.IO;
using UnityEngine;

// Хранит прогресс игрока в JSON-файле (Application.persistentDataPath/progress.json).
// Статический класс: не нужно ставить объект в сцену, работает из любой сцены.
//
// Пример использования из GameScene при победе:
//     SaveManager.CompleteLevel(LevelSession.SelectedLevel, stars, xpReward);
public static class SaveManager
{
    public const int LevelCount = 5;
    public const int XpPerPlayerLevel = 1000;
    public const int SkillPointsPerPlayerLevel = 1;

    const string FileName = "progress.json";

    static ProgressData data;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static ProgressData Data
    {
        get
        {
            if (data == null)
                Load();
            return data;
        }
    }

    public static void Load()
    {
        data = null;

        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                data = JsonUtility.FromJson<ProgressData>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Не удалось прочитать сохранение, создаём новый прогресс. " + e.Message);
            data = null;
        }

        if (data == null)
            data = new ProgressData();

        data.Normalize(LevelCount);
    }

    public static void Save()
    {
        try
        {
            Data.Normalize(LevelCount);
            string json = JsonUtility.ToJson(Data, true);

            // Пишем во временный файл и подменяем — так файл не сломается, если игра закроется посреди записи.
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Ошибка сохранения прогресса: " + e.Message);
        }
    }

    public static void ResetProgress()
    {
        data = new ProgressData();
        data.Normalize(LevelCount);
        Save();
    }

    // level — номер уровня с 1.
    public static bool IsUnlocked(int level)
    {
        return level >= 1 && level <= Data.highestUnlockedLevel;
    }

    public static int GetStars(int level)
    {
        if (level < 1 || level > LevelCount) return 0;
        return Data.stars[level - 1];
    }

    public static bool IsCompleted(int level)
    {
        return GetStars(level) > 0;
    }

    // Вызывать после Victory. Сохраняет лучший результат по звёздам, открывает следующий уровень, начисляет XP.
    public static void CompleteLevel(int level, int stars, int xpReward)
    {
        if (level < 1 || level > LevelCount)
        {
            Debug.LogWarning("CompleteLevel: неверный номер уровня " + level);
            return;
        }

        stars = Mathf.Clamp(stars, 1, 3);
        Data.stars[level - 1] = Mathf.Max(Data.stars[level - 1], stars);

        if (level < LevelCount && Data.highestUnlockedLevel < level + 1)
            Data.highestUnlockedLevel = level + 1;

        AddXp(xpReward);
        Save();
    }

    // Начисляет XP. За каждые XpPerPlayerLevel опыта игрок получает очки навыков.
    public static void AddXp(int amount)
    {
        if (amount <= 0) return;

        int levelBefore = Data.totalXp / XpPerPlayerLevel;
        Data.totalXp += amount;
        int levelAfter = Data.totalXp / XpPerPlayerLevel;

        if (levelAfter > levelBefore)
            Data.skillPoints += (levelAfter - levelBefore) * SkillPointsPerPlayerLevel;
    }

    // Опыт внутри текущего уровня игрока, например 200 из 1000.
    public static int XpInCurrentLevel => Data.totalXp % XpPerPlayerLevel;

    // Простое правило звёзд по доле оставшегося HP башни (этап 10 плана).
    public static int StarsForHealth(float currentHp, float maxHp)
    {
        if (maxHp <= 0f) return 1;
        float ratio = currentHp / maxHp;
        if (ratio >= 0.75f) return 3;
        if (ratio >= 0.4f) return 2;
        return 1;
    }
}
