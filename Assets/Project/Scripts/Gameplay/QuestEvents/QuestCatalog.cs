using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    [CreateAssetMenu(menuName = "Project/Quests/Quest Catalog", fileName = "QuestCatalog")]
    public class QuestCatalog : ScriptableObject
    {
        [SerializeField] private List<BaseQuestConfig> _quests;

        public IReadOnlyList<BaseQuestConfig> Quests => _quests;
    }
}