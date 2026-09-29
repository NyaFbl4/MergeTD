using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class BarrageProtocolVfx : MonoBehaviour
    {
        private const int CircleSegments = 48;
        private const float RingRadius = 0.7f;

        private LineRenderer _ring;
        private Material _material;
        private float _duration;
        private float _elapsed;

        public static BarrageProtocolVfx Play(Transform target, float duration)
        {
            var effectObject = new GameObject("Barrage Protocol Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.SetParent(target, false);

            var effect = effectObject.AddComponent<BarrageProtocolVfx>();
            effect.Initialize(duration);
            return effect;
        }

        public void Stop()
        {
            Destroy(gameObject);
        }

        private void Initialize(float duration)
        {
            _duration = duration;
            _material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Barrage Protocol Runtime Material"
            };

            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.sharedMaterial = _material;
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = CircleSegments;
            _ring.startWidth = 0.06f;
            _ring.endWidth = 0.06f;
            _ring.sortingOrder = 515;

            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / CircleSegments;
                _ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * RingRadius,
                    Mathf.Sin(angle) * RingRadius,
                    0f));
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            var pulse = 1f + Mathf.Sin(_elapsed * 8f) * 0.12f;
            transform.localScale = new Vector3(pulse, pulse, 1f);

            var fade = Mathf.Clamp01((_duration - _elapsed) / 0.5f);
            var color = new Color(0.15f, 0.9f, 1f, fade);
            _ring.startColor = color;
            _ring.endColor = color;

            if (_elapsed >= _duration)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }
    }
}
