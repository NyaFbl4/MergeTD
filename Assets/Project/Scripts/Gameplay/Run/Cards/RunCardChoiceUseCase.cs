using System;
using System.Collections.Generic;
using Project.Scripts.Gameplay.Base;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Run.Configs;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Audio;
using Project.Scripts.System.UseCases;
using UnityEngine;
using VContainer.Unity;
using YG;

namespace Project.Scripts.Gameplay.Run.Cards
{
    public sealed class RunCardChoiceUseCase : IInitializable, IDisposable
    {
        private readonly RunCardCatalog _catalog;
        private readonly RunCardRuntimeState _runtimeState;
        private readonly RunBattleRuntime _runBattleRuntime;
        private readonly IPlayerStatsUseCase _playerStats;
        private readonly BaseHealth _baseHealth;
        private readonly BattlefieldContext _battlefield;
        private readonly IUnitsCatalog _unitsCatalog;
        private readonly IAudioManager _audioManager;
        private readonly List<RunCardConfig> _choices = new();

        private bool _isActive;
        private bool _rerollUsed;
        private bool _isWaitingAdReward;

        public event Action StateChanged;

        public bool IsActive => _isActive;
        public bool CanReroll => _isActive && !_rerollUsed && !_isWaitingAdReward;
        public IReadOnlyList<RunCardConfig> Choices => _choices;
        public string Title => _catalog.ChoiceTitle;
        public string RerollButtonText => _catalog.RerollButtonText;

        public RunCardChoiceUseCase(
            RunCardCatalog catalog,
            RunCardRuntimeState runtimeState,
            RunBattleRuntime runBattleRuntime,
            IPlayerStatsUseCase playerStats,
            BaseHealth baseHealth,
            BattlefieldContext battlefield,
            IUnitsCatalog unitsCatalog,
            IAudioManager audioManager)
        {
            _catalog = catalog;
            _runtimeState = runtimeState;
            _runBattleRuntime = runBattleRuntime;
            _playerStats = playerStats;
            _baseHealth = baseHealth;
            _battlefield = battlefield;
            _unitsCatalog = unitsCatalog;
            _audioManager = audioManager;
        }

        public void Initialize()
        {
            ValidateCatalog();
            _runBattleRuntime.WaveCompleted += OnWaveCompleted;
        }

        public void TrySelect(int index)
        {
            if (!_isActive || index < 0 || index >= _choices.Count)
                return;

            var card = _choices[index];
            if (!_runtimeState.TryAdd(card))
                return;

            ApplyImmediateEffects(card);
            _audioManager.PlaySound(ESoundId.UiButtonClick);

            _isActive = false;
            _choices.Clear();
            StateChanged?.Invoke();
            _runBattleRuntime.ContinueToNextPreparation();
        }

        public void TryReroll()
        {
            if (!CanReroll)
                return;

            _isWaitingAdReward = true;
            SubscribeRewardedAdEvents();
            StateChanged?.Invoke();
            YG2.RewardedAdvShow(_catalog.RewardedRerollAdId);
        }

        private void OnWaveCompleted(int _, IReadOnlyList<WaveRewardConfig> __, ERunPhase phase)
        {
            if (phase != ERunPhase.CardChoice)
                return;

            _rerollUsed = false;
            _isWaitingAdReward = false;
            _isActive = true;
            GenerateChoices(false);

            if (_choices.Count == 0)
            {
                Debug.LogWarning("RunCardChoiceUseCase: no eligible cards. Card choice was skipped.");
                _isActive = false;
                _runBattleRuntime.ContinueToNextPreparation();
            }

            StateChanged?.Invoke();
        }

        private void GenerateChoices(bool excludeCurrentChoices)
        {
            var excludedIds = new HashSet<string>();
            if (!_catalog.AllowDuplicateSelections)
            {
                for (var i = 0; i < _runtimeState.SelectedCardIds.Count; i++)
                    excludedIds.Add(_runtimeState.SelectedCardIds[i]);
            }

            var pool = CreateEligiblePool(excludedIds);
            if (excludeCurrentChoices)
            {
                var withoutCurrent = new List<RunCardConfig>(pool);
                for (var i = withoutCurrent.Count - 1; i >= 0; i--)
                {
                    if (_choices.Contains(withoutCurrent[i]))
                        withoutCurrent.RemoveAt(i);
                }

                if (withoutCurrent.Count >= Mathf.Min(_catalog.ChoiceCount, pool.Count))
                    pool = withoutCurrent;
            }

            _choices.Clear();
            while (_choices.Count < _catalog.ChoiceCount && pool.Count > 0)
            {
                var selectedIndex = SelectWeightedIndex(pool);
                _choices.Add(pool[selectedIndex]);
                pool.RemoveAt(selectedIndex);
            }
        }

        private List<RunCardConfig> CreateEligiblePool(HashSet<string> excludedIds)
        {
            var result = new List<RunCardConfig>();
            var seenIds = new HashSet<string>();

            for (var i = 0; i < _catalog.Cards.Count; i++)
            {
                var card = _catalog.Cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id)
                                 || card.SelectionWeight <= 0f
                                 || excludedIds.Contains(card.Id)
                                 || !seenIds.Add(card.Id)
                                 || !CanApply(card))
                    continue;

                result.Add(card);
            }

            return result;
        }

        private bool CanApply(RunCardConfig card)
        {
            for (var i = 0; i < card.Effects.Count; i++)
            {
                var effect = card.Effects[i];
                if (effect.EffectType != ERunCardEffectType.FreeTower)
                    continue;

                if (_battlefield.FindFirstFreePlaceableSlot() == null
                    || _unitsCatalog.GetTowerPrefabByLevel(effect.TowerLevel, effect.TowerType) == null)
                    return false;
            }

            return true;
        }

        private static int SelectWeightedIndex(IReadOnlyList<RunCardConfig> pool)
        {
            var totalWeight = 0f;
            for (var i = 0; i < pool.Count; i++)
                totalWeight += pool[i].SelectionWeight;

            var roll = UnityEngine.Random.value * totalWeight;
            for (var i = 0; i < pool.Count; i++)
            {
                roll -= pool[i].SelectionWeight;
                if (roll <= 0f)
                    return i;
            }

            return pool.Count - 1;
        }

        private void ApplyImmediateEffects(RunCardConfig card)
        {
            for (var i = 0; i < card.Effects.Count; i++)
            {
                var effect = card.Effects[i];
                switch (effect.EffectType)
                {
                    case ERunCardEffectType.Gold:
                        _playerStats.AddGold(effect.IntValue);
                        break;
                    case ERunCardEffectType.RepairBase:
                        _baseHealth.AddCurrentHealth(effect.IntValue);
                        break;
                    case ERunCardEffectType.MaxBaseHealth:
                        _baseHealth.AddTemporaryMaxHealth(effect.IntValue);
                        break;
                    case ERunCardEffectType.FreeTower:
                        GrantTower(effect);
                        break;
                }
            }
        }

        private void GrantTower(RunCardEffectConfig effect)
        {
            var slot = _battlefield.FindFirstFreePlaceableSlot();
            var prefab = _unitsCatalog.GetTowerPrefabByLevel(effect.TowerLevel, effect.TowerType);
            if (slot == null || prefab == null)
                return;

            slot.TryPlaceTower(
                prefab,
                _playerStats,
                _audioManager,
                true,
                TowerEconomy.GetTierGoldValue(effect.TowerLevel),
                true);
        }

        private void ValidateCatalog()
        {
            var ids = new HashSet<string>();
            for (var i = 0; i < _catalog.Cards.Count; i++)
            {
                var card = _catalog.Cards[i];
                if (card == null)
                {
                    Debug.LogWarning($"RunCardCatalog: empty card reference at index {i}.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(card.Id))
                    Debug.LogError($"RunCardCatalog: card '{card.name}' has no id.");
                else if (!ids.Add(card.Id))
                    Debug.LogError($"RunCardCatalog: duplicate card id '{card.Id}'.");

                if (card.Effects.Count == 0)
                    Debug.LogWarning($"RunCardCatalog: card '{card.name}' has no effects.");
            }
        }

        private void SubscribeRewardedAdEvents()
        {
            YG2.onRewardAdv += OnRewardedAdReward;
            YG2.onCloseRewardedAdv += OnRewardedAdClosed;
            YG2.onErrorRewardedAdv += OnRewardedAdError;
        }

        private void UnsubscribeRewardedAdEvents()
        {
            YG2.onRewardAdv -= OnRewardedAdReward;
            YG2.onCloseRewardedAdv -= OnRewardedAdClosed;
            YG2.onErrorRewardedAdv -= OnRewardedAdError;
        }

        private void OnRewardedAdReward(string rewardId)
        {
            if (!_isWaitingAdReward || rewardId != _catalog.RewardedRerollAdId)
                return;

            _isWaitingAdReward = false;
            _rerollUsed = true;
            UnsubscribeRewardedAdEvents();
            GenerateChoices(true);
            StateChanged?.Invoke();
        }

        private void OnRewardedAdClosed()
        {
            if (!_isWaitingAdReward)
                return;

            _isWaitingAdReward = false;
            UnsubscribeRewardedAdEvents();
            StateChanged?.Invoke();
        }

        private void OnRewardedAdError()
        {
            if (!_isWaitingAdReward)
                return;

            _isWaitingAdReward = false;
            UnsubscribeRewardedAdEvents();
            StateChanged?.Invoke();
            Debug.LogWarning("RunCardChoiceUseCase: rewarded reroll ad failed.");
        }

        public void Dispose()
        {
            _runBattleRuntime.WaveCompleted -= OnWaveCompleted;
            if (_isWaitingAdReward)
                UnsubscribeRewardedAdEvents();
        }
    }
}
