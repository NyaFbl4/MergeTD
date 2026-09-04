namespace Project.Scripts.Gameplay.Run
{
    public enum ERunPhase
    {
        Preparation, // можно покупать, ставить, мержить
        Wave,        // враги идут, башни стреляют, можно юзать способности
        Reward,      // выдали золото
        CardChoice,  // выбор усиления после 3/6/9
        Victory,     // победа
        Defeat       // поражение
    }
}