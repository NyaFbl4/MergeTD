using Project.Scripts.Gameplay.Enemies;

namespace Project.Scripts.Gameplay.QuestEvents
{
    public readonly struct EnemyKilledQuestEventDTO
    {
        public readonly EEnemyType EnemyType;

        public EnemyKilledQuestEventDTO(EEnemyType enemyType)
        {
            EnemyType = enemyType;
        }
    }
}
