using UnityEngine;

namespace Project.Scripts.Gameplay.Quests
{
    [CreateAssetMenu(menuName = "Project/Quests/Complete Wave Quest", fileName = "CompleteWaveQuest")]
    public class CompleteWaveQuestConfig : BaseQuestConfig
    {
        [SerializeField] private bool _onlyLastWave;

        public bool OnlyLastWave => _onlyLastWave;
    }
}