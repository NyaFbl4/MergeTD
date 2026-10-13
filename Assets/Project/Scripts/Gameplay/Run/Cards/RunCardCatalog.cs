using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Gameplay.Run.Cards
{
    [CreateAssetMenu(fileName = "Run Card Catalog", menuName = "Configs/Run/Card Catalog")]
    public sealed class RunCardCatalog : ScriptableObject
    {
        [SerializeField] private List<RunCardConfig> _cards = new();
        [SerializeField, Range(1, 3)] private int _choiceCount = 3;
        [SerializeField] private bool _allowDuplicateSelections;
        [SerializeField] private string _choiceTitle = "Выбери награду";
        [SerializeField] private string _rerollButtonText = "Обновить";
        [SerializeField] private string _rewardedRerollAdId = "card_choice_reroll";

        public IReadOnlyList<RunCardConfig> Cards => _cards;
        public int ChoiceCount => Mathf.Clamp(_choiceCount, 1, 3);
        public bool AllowDuplicateSelections => _allowDuplicateSelections;
        public string ChoiceTitle => _choiceTitle;
        public string RerollButtonText => _rerollButtonText;
        public string RewardedRerollAdId => _rewardedRerollAdId;

        public bool TryGetById(string id, out RunCardConfig card)
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                var candidate = _cards[i];
                if (candidate != null && candidate.Id == id)
                {
                    card = candidate;
                    return true;
                }
            }

            card = null;
            return false;
        }
    }
}
