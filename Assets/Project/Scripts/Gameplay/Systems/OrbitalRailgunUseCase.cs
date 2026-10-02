using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    public sealed class OrbitalRailgunUseCase : IStartable, IDisposable, IGameUpdateListener, IGameFinishListener
    {
        private const int TargetingSortingOrder = 500;

        private readonly RunState _runState;
        private readonly RunEnergyService _energy;
        private readonly OrbitalRailgunConfig _config;
        private readonly HashSet<IEnemyHealth> _damagedEnemies = new();
        private readonly CancellationTokenSource _lifetimeCancellation = new();

        private CancellationTokenSource _castCancellation;
        private Camera _camera;
        private Material _lineMaterial;
        private GameObject _targetingPreviewObject;
        private LineRenderer _targetingPreview;
        private Vector2 _lineStart;
        private Vector2 _lineEnd;
        private float _cooldownRemaining;
        private int _displayedCooldownSeconds;
        private bool _isTargeting;
        private bool _isDragging;
        private bool _isCasting;
        private bool _waitForPointerRelease;
        private bool _isDisposed;

        public event Action StateChanged;

        public float CooldownRemaining => _cooldownRemaining;
        public bool IsTargeting => _isTargeting;
        public bool IsCasting => _isCasting;
        public bool CanInteract => _isTargeting
            || (_runState.CanUseAbilities
                && !_isCasting
                && _cooldownRemaining <= 0f
                && _energy.Current >= _config.ManaCost);

        public OrbitalRailgunUseCase(
            RunState runState,
            RunEnergyService energy,
            SpellCatalog spellCatalog)
        {
            _runState = runState;
            _energy = energy;
            _config = spellCatalog.Get<OrbitalRailgunConfig>();
        }

        public void Start()
        {
            IGameListener.Register(this);

            _camera = Camera.main;
            _lineMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Orbital Railgun Runtime Material"
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
            _isDragging = false;
            _waitForPointerRelease = true;
            StateChanged?.Invoke();
            return true;
        }

        public void OnUpdate(float deltaTime)
        {
            UpdateCooldown(deltaTime);

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

            if (_waitForPointerRelease)
            {
                if (!pointer.press.isPressed)
                    _waitForPointerRelease = false;

                return;
            }

            var worldPosition = ScreenToWorld(pointer.position.ReadValue());
            if (!_isDragging)
            {
                if (!pointer.press.wasPressedThisFrame)
                    return;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                BeginDrag(worldPosition);
                return;
            }

            _lineEnd = worldPosition;
            UpdateTargetingPreview();

            if (!pointer.press.wasReleasedThisFrame)
                return;

            if (Vector2.Distance(_lineStart, _lineEnd) < _config.MinimumLineLength)
            {
                _isDragging = false;
                _targetingPreviewObject.SetActive(false);
                return;
            }

            BeginCast(_lineStart, _lineEnd);
        }

        public void OnFinishGame()
        {
            CancelTargeting();
            CancelActiveCast();
            _cooldownRemaining = 0f;
            _displayedCooldownSeconds = 0;
            StateChanged?.Invoke();
        }

        private void BeginDrag(Vector2 worldPosition)
        {
            _isDragging = true;
            _lineStart = worldPosition;
            _lineEnd = worldPosition;
            _targetingPreviewObject.SetActive(true);
            UpdateTargetingPreview();
        }

        private void UpdateTargetingPreview()
        {
            _targetingPreview.SetPosition(0, _lineStart);
            _targetingPreview.SetPosition(1, _lineEnd);
        }

        private void BeginCast(Vector2 start, Vector2 end)
        {
            if (!_energy.TrySpend(_config.ManaCost))
            {
                CancelTargeting();
                return;
            }

            _isTargeting = false;
            _isDragging = false;
            _waitForPointerRelease = false;
            _targetingPreviewObject.SetActive(false);
            _cooldownRemaining = _config.Cooldown;
            _displayedCooldownSeconds = Mathf.CeilToInt(_cooldownRemaining);

            CancelActiveCast();
            _isCasting = true;
            _castCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
            ExecuteCastAsync(start, end, _castCancellation.Token).Forget();
            StateChanged?.Invoke();
        }

        private async UniTask ExecuteCastAsync(
            Vector2 start,
            Vector2 end,
            CancellationToken cancellationToken)
        {
            var effectObject = new GameObject("Orbital Railgun Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            var effect = effectObject.AddComponent<OrbitalRailgunVfx>();
            effect.ShowWarning(start, end, _config.LineWidth, _lineMaterial);

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_config.ImpactDelay),
                    cancellationToken: cancellationToken);

                DamageEnemies(start, end);
                SpellPrefabVfx.PlayAt(_config, (start + end) * 0.5f);

                await effect.PlayStrikeAsync(
                    start,
                    end,
                    _config.LineWidth,
                    _config.BeamDuration,
                    _lineMaterial,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Object.Destroy(effectObject);

                if (!_isDisposed)
                {
                    _isCasting = false;
                    StateChanged?.Invoke();
                }
            }
        }

        private void DamageEnemies(Vector2 start, Vector2 end)
        {
            _damagedEnemies.Clear();

            var direction = end - start;
            var length = direction.magnitude;
            var center = (start + end) * 0.5f;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var size = new Vector2(length + _config.LineWidth, _config.LineWidth);
            var hits = Physics2D.OverlapCapsuleAll(
                center,
                size,
                CapsuleDirection2D.Horizontal,
                angle,
                _config.EnemyLayer);

            for (var i = 0; i < hits.Length; i++)
            {
                var health = hits[i].GetComponentInParent<IEnemyHealth>();
                if (health == null || !_damagedEnemies.Add(health))
                    continue;

                health.TakeDamage(_config.Damage, false);
            }
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
            _isDragging = false;
            _waitForPointerRelease = false;
            _targetingPreviewObject.SetActive(false);
            StateChanged?.Invoke();
        }

        private void CancelActiveCast()
        {
            if (_castCancellation == null)
                return;

            _castCancellation.Cancel();
            _castCancellation.Dispose();
            _castCancellation = null;
            _isCasting = false;
        }

        private void CreateTargetingPreview()
        {
            _targetingPreviewObject = new GameObject("Orbital Railgun Targeting")
            {
                hideFlags = HideFlags.DontSave
            };
            _targetingPreview = _targetingPreviewObject.AddComponent<LineRenderer>();
            _targetingPreview.sharedMaterial = _lineMaterial;
            _targetingPreview.useWorldSpace = true;
            _targetingPreview.positionCount = 2;
            _targetingPreview.startWidth = _config.LineWidth;
            _targetingPreview.endWidth = _config.LineWidth;
            _targetingPreview.startColor = new Color(1f, 0.75f, 0.1f, 0.45f);
            _targetingPreview.endColor = new Color(1f, 0.25f, 0.1f, 0.45f);
            _targetingPreview.sortingOrder = TargetingSortingOrder;
            _targetingPreviewObject.SetActive(false);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            IGameListener.Unregister(this);

            _lifetimeCancellation.Cancel();
            CancelActiveCast();
            _lifetimeCancellation.Dispose();

            Object.Destroy(_targetingPreviewObject);
            Object.Destroy(_lineMaterial);
        }
    }
}
