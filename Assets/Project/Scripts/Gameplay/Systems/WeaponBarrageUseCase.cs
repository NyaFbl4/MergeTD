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
    public sealed class WeaponBarrageUseCase : IStartable, IDisposable, IGameUpdateListener, IGameFinishListener
    {
        private const int TargetingCircleSegments = 64;
        private const int TargetingSortingOrder = 500;

        private readonly RunState _runState;
        private readonly WeaponBarrageConfig _config;
        private readonly HashSet<IEnemyHealth> _damagedEnemies = new();
        private readonly CancellationTokenSource _lifetimeCancellation = new();

        private CancellationTokenSource _castCancellation;
        private Camera _camera;
        private Material _lineMaterial;
        private GameObject _targetingPreviewObject;
        private LineRenderer _targetingPreview;
        private float _cooldownRemaining;
        private int _displayedCooldownSeconds;
        private bool _isTargeting;
        private bool _isCasting;
        private bool _waitForPointerRelease;
        private bool _isDisposed;

        public event Action StateChanged;

        public float CooldownRemaining => _cooldownRemaining;
        public bool IsTargeting => _isTargeting;
        public bool CanInteract => _isTargeting
            || (_runState.CanUseAbilities && !_isCasting && _cooldownRemaining <= 0f);

        public WeaponBarrageUseCase(RunState runState, WeaponBarrageConfig config)
        {
            _runState = runState;
            _config = config;
        }

        public void Start()
        {
            IGameListener.Register(this);

            _camera = Camera.main;
            _lineMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Weapon Barrage Runtime Material"
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

            BeginCast(worldPosition);
        }

        public void OnFinishGame()
        {
            CancelTargeting();
            CancelActiveCast();
            _cooldownRemaining = 0f;
            _displayedCooldownSeconds = 0;
            StateChanged?.Invoke();
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

        private void BeginCast(Vector2 center)
        {
            _isTargeting = false;
            _isCasting = true;
            _targetingPreviewObject.SetActive(false);
            _cooldownRemaining = _config.Cooldown;
            _displayedCooldownSeconds = Mathf.CeilToInt(_cooldownRemaining);

            CancelActiveCast();
            _isCasting = true;
            _castCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
            ExecuteCastAsync(center, _castCancellation.Token).Forget();
            StateChanged?.Invoke();
        }

        private async UniTask ExecuteCastAsync(Vector2 center, CancellationToken cancellationToken)
        {
            var effectObject = new GameObject("Weapon Barrage Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            var effect = effectObject.AddComponent<WeaponBarrageVfx>();
            effect.ShowWarning(center, _config.Radius, _lineMaterial);

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_config.ImpactDelay),
                    cancellationToken: cancellationToken);

                DamageEnemies(center);

                await effect.PlayBarrageAsync(
                    center,
                    _config.Radius,
                    _config.ProjectileCount,
                    _config.BarrageDuration,
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

        private void DamageEnemies(Vector2 center)
        {
            _damagedEnemies.Clear();

            var hits = Physics2D.OverlapCircleAll(center, _config.Radius, _config.EnemyLayer);
            for (var i = 0; i < hits.Length; i++)
            {
                var health = hits[i].GetComponentInParent<IEnemyHealth>();
                if (health == null || !_damagedEnemies.Add(health))
                    continue;

                health.TakeDamage(_config.Damage, false);
            }
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
            _targetingPreviewObject = new GameObject("Weapon Barrage Targeting")
            {
                hideFlags = HideFlags.DontSave
            };
            _targetingPreview = _targetingPreviewObject.AddComponent<LineRenderer>();
            _targetingPreview.sharedMaterial = _lineMaterial;
            _targetingPreview.useWorldSpace = false;
            _targetingPreview.loop = true;
            _targetingPreview.positionCount = TargetingCircleSegments;
            _targetingPreview.startWidth = 0.06f;
            _targetingPreview.endWidth = 0.06f;
            _targetingPreview.startColor = new Color(0.1f, 0.9f, 1f, 0.9f);
            _targetingPreview.endColor = new Color(0.1f, 0.9f, 1f, 0.9f);
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
