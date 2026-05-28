using Project.Scripts.Gameplay.Enemies;
using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    [CreateAssetMenu(menuName = "Project/Quests/Deal Damage Quest", fileName = "DealDamageQuest")]
    public class DealDamageQuestConfig : BaseQuestConfig
    {
        [SerializeField] private bool _onlyCritical;
        [SerializeField] private bool _filterByEnemyType;
        [SerializeField] private EEnemyType _enemyType;

        public bool OnlyCritical => _onlyCritical;
        public bool FilterByEnemyType => _filterByEnemyType;
        public EEnemyType EnemyType => _enemyType;
    }
}