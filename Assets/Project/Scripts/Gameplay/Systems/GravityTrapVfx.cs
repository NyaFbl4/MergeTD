using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class GravityTrapVfx : MonoBehaviour
    {
        private const int CircleSegments = 48;
        private const int EffectSortingOrder = 518;

        private GameObject _outerRingObject;
        private GameObject _innerRingObject;
        private LineRenderer _outerRing;
        private LineRenderer _innerRing;
        private Material _material;
        private float _duration;
        private float _elapsed;

        public static GravityTrapVfx Play(Vector2 center, float radius, float duration)
        {
            var effectObject = new GameObject("Gravity Trap Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.position = center;
            var effect = effectObject.AddComponent<GravityTrapVfx>();
            effect.Initialize(radius, duration);
            return effect;
        }

        private void Initialize(float radius, float duration)
        {
            _duration = duration;
            _material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Gravity Trap Effect Material"
            };

            _outerRingObject = CreateRing(
                "Gravity Outer Ring",
                radius,
                0.09f,
                new Color(0.65f, 0.2f, 1f, 0.9f),
                false,
                out _outerRing);
            _innerRingObject = CreateRing(
                "Gravity Core",
                radius * 0.36f,
                0.12f,
                new Color(0.2f, 0.75f, 1f, 1f),
                true,
                out _innerRing);
        }

        private GameObject CreateRing(
            string objectName,
            float radius,
            float width,
            Color color,
            bool distort,
            out LineRenderer ring)
        {
            var ringObject = new GameObject(objectName);
            ringObject.transform.SetParent(transform, false);

            ring = ringObject.AddComponent<LineRenderer>();
            ring.sharedMaterial = _material;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = CircleSegments;
            ring.startWidth = width;
            ring.endWidth = width;
            ring.startColor = color;
            ring.endColor = color;
            ring.sortingOrder = EffectSortingOrder;

            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / CircleSegments;
                var distortion = distort && i % 2 == 0 ? 0.72f : 1f;
                ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * radius * distortion,
                    Mathf.Sin(angle) * radius * distortion,
                    0f));
            }

            return ringObject;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _outerRingObject.transform.Rotate(0f, 0f, 55f * Time.deltaTime);
            _innerRingObject.transform.Rotate(0f, 0f, -125f * Time.deltaTime);

            var pulse = 1f + Mathf.Sin(_elapsed * 7f) * 0.06f;
            _innerRingObject.transform.localScale = new Vector3(pulse, pulse, 1f);

            var fade = Mathf.Clamp01((_duration - _elapsed) / 0.35f);
            var outerColor = new Color(0.65f, 0.2f, 1f, 0.9f * fade);
            var innerColor = new Color(0.2f, 0.75f, 1f, fade);
            _outerRing.startColor = outerColor;
            _outerRing.endColor = outerColor;
            _innerRing.startColor = innerColor;
            _innerRing.endColor = innerColor;

            if (_elapsed >= _duration)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }
    }
}
