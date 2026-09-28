using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class WeaponBarrageVfx : MonoBehaviour
    {
        private const int CircleSegments = 48;
        private const int EffectSortingOrder = 510;

        private GameObject _warningObject;

        public void ShowWarning(Vector2 center, float radius, Material material)
        {
            _warningObject = CreateCircle(
                "Barrage Warning",
                center,
                radius,
                0.07f,
                new Color(1f, 0.65f, 0.05f, 0.9f),
                material);
        }

        public async UniTask PlayBarrageAsync(
            Vector2 center,
            float radius,
            int projectileCount,
            float duration,
            Material material,
            CancellationToken cancellationToken)
        {
            if (_warningObject != null)
                Destroy(_warningObject);

            var interval = duration / projectileCount;
            for (var i = 0; i < projectileCount; i++)
            {
                var impactPoint = center + UnityEngine.Random.insideUnitCircle * (radius * 0.82f);
                var flightDuration = Mathf.Min(0.12f, interval * 0.75f);

                SpawnStreak(impactPoint, radius, flightDuration, material);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(flightDuration),
                    cancellationToken: cancellationToken);

                SpawnImpactPulse(impactPoint, radius, material);

                var remainingInterval = interval - flightDuration;
                if (remainingInterval > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(remainingInterval),
                        cancellationToken: cancellationToken);
                }
            }

            await UniTask.Delay(
                TimeSpan.FromSeconds(0.3f),
                cancellationToken: cancellationToken);
        }

        private void SpawnStreak(Vector2 impactPoint, float radius, float duration, Material material)
        {
            var streakObject = new GameObject("Barrage Projectile");
            streakObject.transform.SetParent(transform);

            var line = streakObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.1f;
            line.endWidth = 0.03f;
            line.sortingOrder = EffectSortingOrder + 1;

            var start = impactPoint + Vector2.up * (radius + 4f);
            streakObject.AddComponent<WeaponBarrageStreak>().Initialize(
                line,
                start,
                impactPoint,
                duration);
        }

        private void SpawnImpactPulse(Vector2 impactPoint, float radius, Material material)
        {
            var pulseObject = CreateCircle(
                "Barrage Impact",
                impactPoint,
                1f,
                0.08f,
                new Color(0.2f, 0.95f, 1f, 1f),
                material);
            pulseObject.AddComponent<WeaponBarragePulse>().Initialize(
                pulseObject.GetComponent<LineRenderer>(),
                radius * 0.28f,
                0.28f);
        }

        private GameObject CreateCircle(
            string objectName,
            Vector2 center,
            float radius,
            float width,
            Color color,
            Material material)
        {
            var circleObject = new GameObject(objectName);
            circleObject.transform.SetParent(transform);
            circleObject.transform.position = center;

            var line = circleObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = CircleSegments;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = EffectSortingOrder;

            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / CircleSegments;
                line.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f));
            }

            return circleObject;
        }
    }

    internal sealed class WeaponBarrageStreak : MonoBehaviour
    {
        private LineRenderer _line;
        private Vector2 _start;
        private Vector2 _end;
        private float _duration;
        private float _elapsed;

        public void Initialize(LineRenderer line, Vector2 start, Vector2 end, float duration)
        {
            _line = line;
            _start = start;
            _end = end;
            _duration = Mathf.Max(0.01f, duration);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / _duration);
            var current = Vector2.Lerp(_start, _end, progress);

            _line.SetPosition(0, current + Vector2.up * 0.8f);
            _line.SetPosition(1, current);

            var alpha = 1f - progress;
            var color = new Color(0.2f, 0.95f, 1f, alpha);
            _line.startColor = color;
            _line.endColor = color;

            if (progress >= 1f)
                Destroy(gameObject);
        }
    }

    internal sealed class WeaponBarragePulse : MonoBehaviour
    {
        private LineRenderer _line;
        private float _maximumRadius;
        private float _duration;
        private float _elapsed;

        public void Initialize(LineRenderer line, float maximumRadius, float duration)
        {
            _line = line;
            _maximumRadius = maximumRadius;
            _duration = duration;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / _duration);
            var scale = Mathf.Lerp(0.1f, _maximumRadius, progress);
            transform.localScale = new Vector3(scale, scale, 1f);

            var color = new Color(0.2f, 0.95f, 1f, 1f - progress);
            _line.startColor = color;
            _line.endColor = color;

            if (progress >= 1f)
                Destroy(gameObject);
        }
    }
}
