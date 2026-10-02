using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class CryoDischargeVfx : MonoBehaviour
    {
        private const int CircleSegments = 48;
        private const float Duration = 0.65f;

        private LineRenderer _ring;
        private Material _material;
        private float _elapsed;

        public static void Play(Vector2 center, float radius)
        {
            var effectObject = new GameObject("Cryo Discharge Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.position = center;
            effectObject.AddComponent<CryoDischargeVfx>().Initialize(radius);
        }

        private void Initialize(float radius)
        {
            _material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Cryo Discharge Effect Material"
            };

            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.sharedMaterial = _material;
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = CircleSegments;
            _ring.startWidth = 0.1f;
            _ring.endWidth = 0.1f;
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
            var scale = Mathf.Lerp(0.15f, 1.1f, progress);
            transform.localScale = new Vector3(scale, scale, 1f);

            var color = new Color(0.35f, 0.9f, 1f, 1f - progress);
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

    internal sealed class CryoSlowVfx : MonoBehaviour
    {
        private const float FrostTintStrength = 0.6f;
        private const float FadeDuration = 0.5f;

        private SpriteRenderer[] _targetRenderers;
        private Color[] _originalColors;
        private float _duration;
        private float _elapsed;

        public static void Play(Transform target, float duration)
        {
            var effectObject = new GameObject("Cryo Slow Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.SetParent(target, false);
            effectObject.AddComponent<CryoSlowVfx>().Initialize(duration);
        }

        private void Initialize(float duration)
        {
            _duration = duration;
            CacheTargetRenderers();
            ApplyFrostTint(FrostTintStrength);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            var fade = Mathf.Clamp01((_duration - _elapsed) / FadeDuration);
            ApplyFrostTint(FrostTintStrength * fade);

            if (_elapsed >= _duration)
                Destroy(gameObject);
        }

        private void CacheTargetRenderers()
        {
            var renderers = transform.parent.GetComponentsInChildren<SpriteRenderer>(true);
            var targetCount = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].GetComponent<SpellStatusIconVfx>() == null)
                    targetCount++;
            }

            _targetRenderers = new SpriteRenderer[targetCount];
            _originalColors = new Color[targetCount];

            var targetIndex = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer.GetComponent<SpellStatusIconVfx>() != null)
                    continue;

                _targetRenderers[targetIndex] = renderer;
                _originalColors[targetIndex] = renderer.color;
                targetIndex++;
            }
        }

        private void ApplyFrostTint(float strength)
        {
            for (var i = 0; i < _targetRenderers.Length; i++)
            {
                var renderer = _targetRenderers[i];
                if (renderer == null)
                    continue;

                var originalColor = _originalColors[i];
                var frostColor = new Color(0.35f, 0.75f, 1f, originalColor.a);
                renderer.color = Color.Lerp(originalColor, frostColor, strength);
            }
        }

        private void OnDestroy()
        {
            ApplyFrostTint(0f);
        }
    }
}
