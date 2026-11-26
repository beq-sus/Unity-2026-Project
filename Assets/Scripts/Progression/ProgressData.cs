using System;

// Всё, что сохраняется между запусками игры. Сериализуется в JSON через JsonUtility.
[Serializable]
public class ProgressData
{
    public int version = 1;

    // Номер самого дальнего открытого уровня (1..LevelCount). Первый уровень открыт сразу.
    public int highestUnlockedLevel = 1;

    // Звёзды по уровням: stars[0] — уровень 1, stars[1] — уровень 2 и т.д. 0 = не пройден.
    public int[] stars = new int[0];

    // Общий накопленный опыт за всю игру.
    public int totalXp = 0;

    // Очки навыков для Skill Tree.
    public int skillPoints = 0;

    public void Normalize(int levelCount)
    {
        if (stars == null || stars.Length != levelCount)
        {
            int[] fixedStars = new int[levelCount];
            if (stars != null)
                Array.Copy(stars, fixedStars, Math.Min(stars.Length, levelCount));
            stars = fixedStars;
        }

        for (int i = 0; i < stars.Length; i++)
            stars[i] = Math.Max(0, Math.Min(3, stars[i]));

        highestUnlockedLevel = Math.Max(1, Math.Min(levelCount, highestUnlockedLevel));
        totalXp = Math.Max(0, totalXp);
        skillPoints = Math.Max(0, skillPoints);
    }
}
