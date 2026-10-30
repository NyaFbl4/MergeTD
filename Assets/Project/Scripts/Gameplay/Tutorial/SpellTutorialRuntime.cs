using System;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.Field;
using Project.Scripts.Gameplay.Run;
using Project.Scripts.Gameplay.Systems;
using Project.Scripts.Gameplay.Towers;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Save;
using Project.Scripts.System.UseCases;
using Project.Scripts.UI.LevelUI;
using Project.Scripts.UI.TutorialUI;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Project.Scripts.Gameplay.Tutorial
{
    public sealed class SpellTutorialRuntime : IStartable, ITickable, IDisposable, IGameFinishListener
    {
        private readonly WorldService _world;
        private readonly RunState _run;
        private readonly RunBattleRuntime _battle;
        private readonly BattlefieldContext _field;
        private readonly LevelUIView _levelView;
        private readonly ILocalizationService _localization;
        private readonly IGameManagerService _game;
        private readonly IBuyTowerUseCase _buy;
        private readonly RunEnergyService _energy;
        private readonly WeaponBarrageUseCase _spell;
        private readonly WeaponBarrageConfig _config;
        private TowerTutorialView _view;
        private VisualElement _generatorButton;
        private VisualElement _startButton;
        private VisualElement _energyBar;
        private VisualElement _spellButton;
        private SpellTutorialSaveData _progress;
        private EnemyHealth _target;
        private Camera _camera;
        private float _nextEnemyScanAt;
        private float _energyHintUntil;
        private float _resultUntil;
        private bool _ownsPause;

        public bool OwnsPause => _ownsPause;

        public SpellTutorialRuntime(WorldService world, RunState run, RunBattleRuntime battle,
            BattlefieldContext field, LevelUIView levelView, ILocalizationService localization,
            IGameManagerService game, IBuyTowerUseCase buy, RunEnergyService energy,
            WeaponBarrageUseCase spell, SpellCatalog catalog)
        {
            _world = world;
            _run = run;
            _battle = battle;
            _field = field;
            _levelView = levelView;
            _localization = localization;
            _game = game;
            _buy = buy;
            _energy = energy;
            _spell = spell;
            _config = catalog.Get<WeaponBarrageConfig>();
        }

        public void Start()
        {
            var root = _levelView.GetComponent<UIDocument>().rootVisualElement;
            _view = new TowerTutorialView(root, Skip, "SpellTutorialOverlay");
            _generatorButton = root.Q("PayElectricTowerButton");
            _startButton = root.Q("GoNextWaveButton");
            _energyBar = root.Q("CurrentEnergyLabel").parent;
            _spellButton = root.Q("WeaponBarrageSpell").Q("SpellButton");
            _camera = Camera.main;
            _run.PhaseChanged += OnPhaseChanged;
            _spell.CastStarted += OnCastStarted;
            IGameListener.Register(this);
        }

        public void Tick()
        {
            if (_progress != _world.SpellTutorial)
            {
                ReleasePause();
                _progress = _world.SpellTutorial;
                _target = null;
                _resultUntil = 0;
                _energyHintUntil = 0;
            }

            if (!_levelView.Visible || _run.TutorialInput.Active || _world.TowerTutorial.step != TowerTutorialStep.Completed)
            {
                Hide();
                return;
            }

            if (_progress.step == SpellTutorialStep.Completed)
            {
                if (Time.unscaledTime < _resultUntil && _run.Phase == ERunPhase.Wave)
                    Show("success", "success", "", default, false, false);
                else Hide();
                return;
            }

            if (_run.RunId != "default-run-1" || !_world.IsSpellSelected(SpellIds.WeaponBarrage)
                || !_world.IsSpellUnlocked(SpellIds.WeaponBarrage) || _energy.Max < _config.ManaCost)
            {
                Hide();
                return;
            }

            if (_run.Phase == ERunPhase.Preparation && _battle.CanUseNextWaveButton)
            {
                UpdatePreparation();
                return;
            }

            if (_run.Phase != ERunPhase.Wave || _progress.step < SpellTutorialStep.AccumulateEnergy)
            {
                Hide();
                return;
            }

            if (_ownsPause)
            {
                // The battle loop is paused; only the existing spell targeting input continues.
                _spell.UpdateTargeting();
                if (_progress.step == SpellTutorialStep.Completed) return;
            }

            FindTarget();
            if (_target == null)
            {
                CancelTargeting();
                ReleasePause();
                SetStep(SpellTutorialStep.AccumulateEnergy);
                ShowEnergyHint();
                return;
            }

            if (!SpellTutorialRules.CanPrompt(_energy.Current, _config.ManaCost,
                    _spell.CanInteract, true, _ownsPause || Time.timeScale > 0))
            {
                ReleasePause();
                SetStep(SpellTutorialStep.AccumulateEnergy);
                ShowEnergyHint();
                return;
            }

            if (!_ownsPause)
            {
                _game.PauseGame();
                _ownsPause = true;
            }

            var targeting = _spell.IsTargeting;
            var step = targeting ? SpellTutorialStep.SelectTarget : SpellTutorialStep.SelectSpell;
            SetStep(step);
            _run.SpellTutorialInput.Set(step, _target.transform.position, _config.Radius * .8f);
            if (targeting)
                Show("target", "target", "5 / 5", _view.WorldRect(_target.transform.position, _config.Radius * .45f), true, true);
            else
                Show("select", "select", "4 / 5", _view.ControlRect(_spellButton), true, true);
        }

        private void UpdatePreparation()
        {
            ReleasePause();
            if (_progress.step == SpellTutorialStep.WaitingForPreparation
                && !SpellTutorialRules.CanBegin(_world.TowerTutorial.step, _run.RunId, _run.CurrentWave, _run.Phase))
            {
                Hide();
                return;
            }

            var hasGenerator = false;
            foreach (var tower in _world.Towers)
                if (tower.towerType == ETowerType.Generator) { hasGenerator = true; break; }

            if (!hasGenerator && (_world.Gold < _buy.GeneratorCost || _field.FindFirstFreePlaceableSlot() == null))
            {
                Hide();
                return;
            }

            var step = SpellTutorialRules.PreparationStep(hasGenerator);
            SetStep(step);
            _run.SpellTutorialInput.Set(step);
            Show(step == SpellTutorialStep.BuyGenerator ? "generator" : "start",
                step == SpellTutorialStep.BuyGenerator ? "generator" : "start",
                step == SpellTutorialStep.BuyGenerator ? "1 / 5" : "2 / 5",
                _view.ControlRect(step == SpellTutorialStep.BuyGenerator ? _generatorButton : _startButton), true, true);
        }

        private void ShowEnergyHint()
        {
            _run.SpellTutorialInput.Clear();
            if (Time.unscaledTime < _energyHintUntil)
                Show("energy", "energy", "3 / 5", _view.ControlRect(_energyBar), false, false);
            else _view.Hide();
        }

        private void FindTarget()
        {
            if (_target != null && !_target.IsDead && _target.gameObject.activeInHierarchy
                && SpellTutorialRules.IsTargetInView(_camera.WorldToViewportPoint(_target.transform.position))) return;
            _target = null;
            if (Time.unscaledTime < _nextEnemyScanAt) return;
            _nextEnemyScanAt = Time.unscaledTime + .25f;
            for (var i = 0; i < _field.EnemiesRoot.childCount; i++)
            {
                var candidate = _field.EnemiesRoot.GetChild(i);
                if (!candidate.gameObject.activeInHierarchy || !candidate.TryGetComponent<EnemyHealth>(out var health) || health.IsDead)
                    continue;
                var viewport = _camera.WorldToViewportPoint(candidate.position);
                if (SpellTutorialRules.IsTargetInView(viewport))
                {
                    _target = health;
                    return;
                }
            }
        }

        private void OnPhaseChanged(ERunPhase phase)
        {
            if (phase == ERunPhase.Wave && _world.SpellTutorial.step == SpellTutorialStep.StartWave)
            {
                SetStep(SpellTutorialStep.AccumulateEnergy);
                _energyHintUntil = Time.unscaledTime + 3.5f;
                _nextEnemyScanAt = 0;
            }
            if (phase != ERunPhase.Wave) Hide();
        }

        private void OnCastStarted(Vector2 center)
        {
            if (!SpellTutorialRules.CanCountCast(_world.SpellTutorial.step, _run.Phase)) return;
            SetStep(SpellTutorialStep.Completed);
            _resultUntil = Time.unscaledTime + 2.5f;
            _run.SpellTutorialInput.Clear();
            _view.Hide();
            ReleasePause();
        }

        private void SetStep(SpellTutorialStep step)
        {
            if (_world.SpellTutorial.step != step) _world.SaveSpellTutorial(step);
        }

        private void Show(string titleKey, string messageKey, string counter, Rect target, bool hand, bool blocking)
        {
            _view.SetSkipText(_localization.Get("tutorial.skip"));
            _view.ShowPrompt(messageKey, _localization.Get("tutorial.spells." + titleKey + "_title"),
                _localization.Get("tutorial.spells." + messageKey), counter, target, hand, blocking);
        }

        private void CancelTargeting()
        {
            if (_ownsPause && _spell.IsTargeting) _spell.ToggleTargeting();
        }

        private void ReleasePause()
        {
            if (!_ownsPause) return;
            _ownsPause = false;
            if (Application.isPlaying && _levelView.Visible && _run.Phase != ERunPhase.Defeat
                && _run.Phase != ERunPhase.Victory && Time.timeScale == 0)
                _game.ResumeGame();
        }

        private void Hide()
        {
            CancelTargeting();
            ReleasePause();
            _run.SpellTutorialInput.Clear();
            _view.Hide();
        }

        private void Skip()
        {
            SetStep(SpellTutorialStep.Completed);
            _resultUntil = 0;
            Hide();
        }

        public void OnFinishGame()
        {
            _ownsPause = false;
            _run.SpellTutorialInput.Clear();
            _view.Hide();
        }

        public void Dispose()
        {
            _run.PhaseChanged -= OnPhaseChanged;
            _spell.CastStarted -= OnCastStarted;
            IGameListener.Unregister(this);
            Hide();
            _view.Dispose();
        }
    }
}
