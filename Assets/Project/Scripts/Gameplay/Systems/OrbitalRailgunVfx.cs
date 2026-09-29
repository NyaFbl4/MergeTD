using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class OrbitalRailgunVfx : MonoBehaviour
    {
        private const int EffectSortingOrder = 520;

        private GameObject _warningObject;

        public void ShowWarning(Vector2 start, Vector2 end, float width, Material material)
        {
            _warningObject = CreateLine(
                "Railgun Warning",
                start,
                end,
                width,
                new Color(1f, 0.25f, 0.1f, 0.55f),
                material);
            _warningObject.AddComponent<OrbitalRailgunWarning>().Initialize(
                _warningObject.GetComponent<LineRenderer>(),
                width);
        }

        public async UniTask PlayStrikeAsync(
            Vector2 start,
            Vector2 end,
            float width,
            float duration,
            Material material,
            CancellationToken cancellationToken)
        {
            if (_warningObject != null)
                Destroy(_warningObject);

            var glowObject = CreateLine(
                "Railgun Beam Glow",
                start,
                end,
                width * 1.8f,
                new Color(0.15f, 0.75f, 1f, 0.65f),
                material);
            glowObject.GetComponent<LineRenderer>().sortingOrder = EffectSortingOrder;

            var coreObject = CreateLine(
                "Railgun Beam Core",
                start,
                end,
                width * 0.42f,
                Color.white,
                material);
            coreObject.GetComponent<LineRenderer>().sortingOrder = EffectSortingOrder + 1;

            glowObject.AddComponent<OrbitalRailgunBeam>().Initialize(
                glowObject.GetComponent<LineRenderer>(),
                width * 1.8f,
                duration,
                new Color(0.15f, 0.75f, 1f, 0.65f));
            coreObject.AddComponent<OrbitalRailgunBeam>().Initialize(
                coreObject.GetComponent<LineRenderer>(),
                width * 0.42f,
                duration,
                Color.white);

            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                cancellationToken: cancellationToken);
        }

        private GameObject CreateLine(
            string objectName,
            Vector2 start,
            Vector2 end,
            float width,
            Color color,
            Material material)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform);

            var line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = EffectSortingOrder;
            return lineObject;
        }
    }

    internal sealed class OrbitalRailgunWarning : MonoBehaviour
    {
        private LineRenderer _line;
        private float _baseWidth;
        private float _elapsed;

        public void Initialize(LineRenderer line, float baseWidth)
        {
            _line = line;
            _baseWidth = baseWidth;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var pulse = 0.75f + (Mathf.Sin(_elapsed * 10f) + 1f) * 0.125f;
            _line.startWidth = _baseWidth * pulse;
            _line.endWidth = _baseWidth * pulse;
        }
    }

    internal sealed class OrbitalRailgunBeam : MonoBehaviour
    {
        private LineRenderer _line;
        private float _startWidth;
        private float _duration;
        private Color _color;
        private float _elapsed;

        public void Initialize(LineRenderer line, float startWidth, float duration, Color color)
        {
            _line = line;
            _startWidth = startWidth;
            _duration = Mathf.Max(0.05f, duration);
            _color = color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / _duration);
            var width = Mathf.Lerp(_startWidth, 0f, progress);
            _line.startWidth = width;
            _line.endWidth = width;

            var color = new Color(_color.r, _color.g, _color.b, _color.a * (1f - progress));
            _line.startColor = color;
            _line.endColor = color;
        }
    }
}
