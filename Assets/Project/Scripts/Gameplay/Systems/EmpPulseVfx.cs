using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class EmpPulseVfx : MonoBehaviour
    {
        private const int CircleSegments = 48;
        private const float Duration = 0.55f;

        private LineRenderer _ring;
        private Material _material;
        private float _elapsed;

        public static void Play(Vector2 center, float radius)
        {
            var effectObject = new GameObject("EMP Pulse Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.position = center;
            effectObject.AddComponent<EmpPulseVfx>().Initialize(radius);
        }

        private void Initialize(float radius)
        {
            _material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "EMP Pulse Effect Material"
            };

            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.sharedMaterial = _material;
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = CircleSegments;
            _ring.startWidth = 0.12f;
            _ring.endWidth = 0.12f;
            _ring.sortingOrder = 515;

            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / CircleSegments;
                _ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f));
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / Duration);
            var scale = Mathf.Lerp(0.1f, 1.15f, progress);
            transform.localScale = new Vector3(scale, scale, 1f);

            var color = Color.Lerp(
                new Color(0.8f, 0.35f, 1f, 1f - progress),
                new Color(0.15f, 0.9f, 1f, 1f - progress),
                progress);
            _ring.startColor = color;
            _ring.endColor = color;

            if (progress >= 1f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }
    }

    internal sealed class EmpStunVfx : MonoBehaviour
    {
        private const int CircleSegments = 28;
        private const float RingRadius = 0.58f;

        private LineRenderer _ring;
        private Material _material;
        private float _duration;
        private float _elapsed;

        public static void Play(Transform target, float duration)
        {
            var effectObject = new GameObject("EMP Stun Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.SetParent(target, false);
            effectObject.AddComponent<EmpStunVfx>().Initialize(duration);
        }

        private void Initialize(float duration)
        {
            _duration = duration;
            _material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "EMP Stun Effect Material"
            };

            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.sharedMaterial = _material;
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = CircleSegments;
            _ring.startWidth = 0.06f;
            _ring.endWidth = 0.06f;
            _ring.sortingOrder = 516;

            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / CircleSegments;
                var distortion = i % 2 == 0 ? 1f : 0.78f;
                _ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * RingRadius * distortion,
                    Mathf.Sin(angle) * RingRadius * distortion,
                    0f));
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.Rotate(0f, 0f, 150f * Time.deltaTime);

            var pulse = 1f + Mathf.Sin(_elapsed * 11f) * 0.12f;
            transform.localScale = new Vector3(pulse, pulse, 1f);

            var fade = Mathf.Clamp01((_duration - _elapsed) / 0.35f);
            var color = new Color(0.45f, 0.75f, 1f, fade);
            _ring.startColor = color;
            _ring.endColor = new Color(0.75f, 0.35f, 1f, fade);

            if (_elapsed >= _duration)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }
    }
}
