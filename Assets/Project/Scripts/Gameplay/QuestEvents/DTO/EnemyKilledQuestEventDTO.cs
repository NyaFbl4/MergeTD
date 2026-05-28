using Project.Scripts.Gameplay.Enemies;

namespace Project.Scripts.Gameplay.QuestEvents
{
    public readonly struct EnemyKilledQuestEventDTO
    {
        public readonly EEnemyType EnemyType;
        public readonly int RewardGold;

        public EnemyKilledQuestEventDTO(EEnemyType enemyType, int rewardGold)
        {
            EnemyType = enemyType;
            RewardGold = rewardGold;
        }
    }
}