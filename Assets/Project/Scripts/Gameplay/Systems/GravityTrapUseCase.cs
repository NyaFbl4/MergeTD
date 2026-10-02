using System;
using System.Collections.Generic;
using Project.Scripts.Configs;
using Project.Scripts.GameManager;
using Project.Scripts.Gameplay.Enemies;
using Project.Scripts.Gameplay.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class GravityTrapUseCase : IStartable, IDisposable, IGameUpdateListener, IGameFinishListener
    {
        private const int TargetingCircleSegments = 64;
        private const int TargetingSortingOrder = 500;

        private readonly RunState _runState;
        private readonly RunEnergyService _energy;
        private readonly GravityTrapConfig _config;
        private readonly HashSet<EnemyUnit> _affectedEnemies = new();
        private readonly HashSet<EnemyUnit> _frameEnemies = new();

        private Camera _camera;
        private Material _lineMaterial;
        private GameObject _targetingPreviewObject;
        private LineRenderer _targetingPreview;
        private GameObject _activeVfx;
        private Vector2 _activeCenter;
        private float _activeDurationRemaining;
        private float _cooldownRemaining;
        private int _displayedActiveSeconds;
        private int _displayedCooldownSeconds;
        private bool _isTargeting;
        private bool _waitForPointerRelease;

        public event Action StateChanged;

        public float ActiveDurationRemaining => _activeDurationRemaining;
        public float CooldownRemaining => _cooldownRemaining;
        public bool IsTargeting => _isTargeting;
        public bool CanInteract => _isTargeting
            || (_runState.CanUseAbilities
                && _activeDurationRemaining <= 0f
                && _cooldownRemaining <= 0f
                && _energy.Current >= _config.ManaCost);

        public GravityTrapUseCase(
            RunState runState,
            RunEnergyService energy,
            SpellCatalog spellCatalog)
        {
            _runState = runState;
            _energy = energy;
            _config = spellCatalog.Get<GravityTrapConfig>();
        }

        public void Start()
        {
            IGameListener.Register(this);

            _camera = Camera.main;
            _lineMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Gravity Trap Runtime Material"
            };

            CreateTargetingPreview();
        }

        public bool ToggleTargeting()
        {
            if (_isTargeting)
            {
                CancelTargeting();
                return true;
            }

            if (!CanInteract)
                return false;

            _camera = Camera.main;
            _isTargeting = true;
            _waitForPointerRelease = true;
            _targetingPreviewObject.SetActive(true);
            StateChanged?.Invoke();
            return true;
        }

        public void OnUpdate(float deltaTime)
        {
            UpdateCooldown(deltaTime);
            UpdateActiveTrap(deltaTime);

            if (!_isTargeting)
                return;

            if (!_runState.CanUseAbilities)
            {
                CancelTargeting();
                return;
            }

            if (IsCancelPressed())
            {
                CancelTargeting();
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null)
                return;

            var worldPosition = ScreenToWorld(pointer.position.ReadValue());
            _targetingPreviewObject.transform.position = worldPosition;

            if (_waitForPointerRelease)
            {
                if (!pointer.press.isPressed)
                    _waitForPointerRelease = false;

                return;
            }

            if (!pointer.press.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Cast(worldPosition);
        }

        public void OnFinishGame()
        {
            CancelTargeting();
            CancelActiveTrap();
            _cooldownRemaining = 0f;
            _displayedCooldownSeconds = 0;
            StateChanged?.Invoke();
        }

        public void Dispose()
        {
            IGameListener.Unregister(this);
            CancelActiveTrap();
            Object.Destroy(_targetingPreviewObject);
            Object.Destroy(_lineMaterial);
        }

        private void Cast(Vector2 center)
        {
            if (!_energy.TrySpend(_config.ManaCost))
            {
                CancelTargeting();
                return;
            }

            _isTargeting = false;
            _waitForPointerRelease = false;
            _targetingPreviewObject.SetActive(false);
            _cooldownRemaining = _config.Cooldown;
            _displayedCooldownSeconds = Mathf.CeilToInt(_cooldownRemaining);
            _activeCenter = center;
            _activeDurationRemaining = _config.Duration;
            _displayedActiveSeconds = Mathf.CeilToInt(_activeDurationRemaining);
            _affectedEnemies.Clear();
            _activeVfx = SpellPrefabVfx.PlayAt(_config, center);
            if (_activeVfx == null)
                _activeVfx = GravityTrapVfx.Play(center, _config.Radius, _config.Duration).gameObject;
            StateChanged?.Invoke();
        }

        private void UpdateActiveTrap(float deltaTime)
        {
            if (_activeDurationRemaining <= 0f)
                return;

            ApplyGravityPull();
            _activeDurationRemaining = Mathf.Max(0f, _activeDurationRemaining - deltaTime);

            var activeSeconds = Mathf.CeilToInt(_activeDurationRemaining);
            if (activeSeconds != _displayedActiveSeconds)
            {
                _displayedActiveSeconds = activeSeconds;
                StateChanged?.Invoke();
            }

            if (_activeDurationRemaining <= 0f)
                CancelActiveTrap();
        }

        private void ApplyGravityPull()
        {
            _frameEnemies.Clear();

            var hits = Physics2D.OverlapCircleAll(_activeCenter, _config.Radius, _config.EnemyLayer);
            for (var i = 0; i < hits.Length; i++)
            {
                var enemy = hits[i].GetComponentInParent<EnemyUnit>();
                if (enemy == null || !_frameEnemies.Add(enemy))
                    continue;

                if (enemy.TryApplyGravityPull(
                        _activeCenter,
                        _config.PullSpeed,
                        _activeDurationRemaining))
                {
                    _affectedEnemies.Add(enemy);
                }
            }
        }

        private void CancelActiveTrap()
        {
            _activeDurationRemaining = 0f;
            _displayedActiveSeconds = 0;

            foreach (var enemy in _affectedEnemies)
            {
                if (enemy != null)
                    enemy.ClearGravityPull();
            }

            _affectedEnemies.Clear();
            _frameEnemies.Clear();

            if (_activeVfx != null)
                Object.Destroy(_activeVfx);

            _activeVfx = null;
        }

        private void UpdateCooldown(float deltaTime)
        {
            if (_cooldownRemaining <= 0f)
                return;

            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
            var cooldownSeconds = Mathf.CeilToInt(_cooldownRemaining);
            if (cooldownSeconds == _displayedCooldownSeconds)
                return;

            _displayedCooldownSeconds = cooldownSeconds;
            StateChanged?.Invoke();
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            var screen = new Vector3(
                screenPosition.x,
                screenPosition.y,
                Mathf.Abs(_camera.transform.position.z));
            var world = _camera.ScreenToWorldPoint(screen);
            return new Vector2(world.x, world.y);
        }

        private static bool IsCancelPressed()
        {
            var keyboardCancelled = Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame;
            var mouseCancelled = Mouse.current != null
                && Mouse.current.rightButton.wasPressedThisFrame;
            return keyboardCancelled || mouseCancelled;
        }

        private void CancelTargeting()
        {
            if (!_isTargeting)
                return;

            _isTargeting = false;
            _waitForPointerRelease = false;
            _targetingPreviewObject.SetActive(false);
            StateChanged?.Invoke();
        }

        private void CreateTargetingPreview()
        {
            _targetingPreviewObject = new GameObject("Gravity Trap Targeting")
            {
                hideFlags = HideFlags.DontSave
            };
            _targetingPreview = _targetingPreviewObject.AddComponent<LineRenderer>();
            _targetingPreview.sharedMaterial = _lineMaterial;
            _targetingPreview.useWorldSpace = false;
            _targetingPreview.loop = true;
            _targetingPreview.positionCount = TargetingCircleSegments;
            _targetingPreview.startWidth = 0.07f;
            _targetingPreview.endWidth = 0.07f;
            _targetingPreview.startColor = new Color(0.75f, 0.2f, 1f, 0.95f);
            _targetingPreview.endColor = new Color(0.2f, 0.65f, 1f, 0.95f);
            _targetingPreview.sortingOrder = TargetingSortingOrder;

            for (var i = 0; i < TargetingCircleSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / TargetingCircleSegments;
                _targetingPreview.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * _config.Radius,
                    Mathf.Sin(angle) * _config.Radius,
                    0f));
            }

            _targetingPreviewObject.SetActive(false);
        }
    }
}
