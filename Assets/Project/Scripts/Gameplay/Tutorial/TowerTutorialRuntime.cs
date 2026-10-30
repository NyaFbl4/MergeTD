using System;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Save;
using Project.Scripts.UI.LevelUI;
using Project.Scripts.UI.TutorialUI;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Tutorial
{
    public sealed class TowerTutorialRuntime : IStartable, ITickable, IDisposable
    {
        private readonly WorldService _world;
        private readonly RunState _run;
        private readonly RunBattleRuntime _battle;
        private readonly BattlefieldContext _field;
        private readonly LevelUIView _levelView;
        private readonly ILocalizationService _localization;
        private readonly IUnitsCatalog _catalog;
        private TowerTutorialView _view;
        private TowerSlot _first;
        private TowerSlot _second;
        private float _successUntil;
        private TowerTutorialSaveData _progress;

        public TowerTutorialRuntime(WorldService world, RunState run, RunBattleRuntime battle,
            BattlefieldContext field, LevelUIView levelView, ILocalizationService localization, IUnitsCatalog catalog)
        {
            _world = world;
            _run = run;
            _battle = battle;
            _field = field;
            _levelView = levelView;
            _localization = localization;
            _catalog = catalog;
        }

        public void Start()
        {
            _view = new TowerTutorialView(_levelView.GetComponent<UIDocument>().rootVisualElement, Skip);
            _run.PhaseChanged += OnPhaseChanged;
        }

        public void Tick()
        {
            if (_progress != _world.TowerTutorial)
            {
                _progress = _world.TowerTutorial;
                _successUntil = 0;
            }

            if (!_levelView.Visible || !_battle.CanUseNextWaveButton || _progress.step == TowerTutorialStep.Completed)
            {
                Hide();
                return;
            }

            var step = TowerTutorialRules.Reconcile(_progress, _world.Towers);
            if (step != _progress.step)
            {
                if (step == TowerTutorialStep.StartWave)
                    _successUntil = Time.unscaledTime + 1.8f;
                _world.SaveTowerTutorial(step, _progress.towerLevel);
            }

            FindPair();
            if (step == TowerTutorialStep.BuyFirst || step == TowerTutorialStep.BuySecond || step == TowerTutorialStep.BuyThird)
            {
                var requiredLevel = step == TowerTutorialStep.BuyFirst ? _world.SelectedTowerLevel : _progress.towerLevel;
                if (requiredLevel != _progress.towerLevel)
                    _world.SaveTowerTutorial(step, requiredLevel);

                var remaining = step == TowerTutorialStep.BuyFirst ? 3 : step == TowerTutorialStep.BuySecond ? 2 : 1;
                var freeSlots = 0;
                foreach (var slot in _field.TowerSlots)
                    if (slot.CanPlaceTower && !slot.IsOccupied) freeSlots++;

                // Never trap a modified or partially migrated world behind an unaffordable step.
                if (_world.Gold < remaining * TowerEconomy.CombatPurchaseCost || freeSlots < remaining
                    || !_catalog.HasTowerLevel(requiredLevel + 1, ETowerType.Combat)
                    || _world.SelectedTowerLevel != requiredLevel)
                {
                    Skip();
                    return;
                }
            }

            _run.TutorialInput.Set(step, _progress.towerLevel,
                _first == null ? null : _first.PersistentId, _second == null ? null : _second.PersistentId);
            _view.Show(step, _first, _second, _run.TutorialInput.IsDragging,
                Time.unscaledTime < _successUntil, _localization);
        }

        private void FindPair()
        {
            _first = null;
            _second = null;
            foreach (var tower in _world.Towers)
            {
                if (tower.towerType != ETowerType.Combat || tower.towerLevel != _progress.towerLevel)
                    continue;
                var slot = _field.FindSlotByPersistentId(tower.slotId);
                if (slot == null) continue;
                if (_first == null) _first = slot;
                else { _second = slot; break; }
            }
        }

        private void OnPhaseChanged(ERunPhase phase)
        {
            if (phase == ERunPhase.Wave && _run.TutorialInput.Active
                && _world.TowerTutorial.step == TowerTutorialStep.StartWave)
                _world.SaveTowerTutorial(TowerTutorialStep.Completed, _world.TowerTutorial.towerLevel);
            if (phase != ERunPhase.Preparation) Hide();
        }

        private void Skip()
        {
            _world.SaveTowerTutorial(TowerTutorialStep.Completed, _world.TowerTutorial.towerLevel);
            Hide();
        }

        private void Hide()
        {
            _run.TutorialInput.Clear();
            _view.Hide();
        }

        public void Dispose()
        {
            _run.PhaseChanged -= OnPhaseChanged;
            _run.TutorialInput.Clear();
            _view.Dispose();
        }
    }
}
