using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    [CreateAssetMenu(menuName = "Project/Quests/Kill Enemy Quest", fileName = "KillEnemyQuest")]
    public class KillEnemyQuestConfig : BaseQuestConfig
    {
        [SerializeField] private EEnemyType _enemyType;

        public EEnemyType EnemyType => _enemyType;
    }
}