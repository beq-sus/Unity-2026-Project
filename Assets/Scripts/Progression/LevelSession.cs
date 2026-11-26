// Передаёт выбранный на карте уровень в GameScene.
// В GameScene (позже LevelManager) читает LevelSession.SelectedLevel, чтобы загрузить нужные данные уровня.
public static class LevelSession
{
    // Номер уровня с 1. Если GameScene запущена напрямую из редактора — будет 1.
    public static int SelectedLevel = 1;
}
