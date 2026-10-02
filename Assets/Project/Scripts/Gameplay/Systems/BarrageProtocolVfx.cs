using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class BarrageProtocolVfx : MonoBehaviour
    {
        private SpellStatusIconVfx _statusIcon;
        private float _duration;
        private float _elapsed;

        public static BarrageProtocolVfx Play(Transform target, float duration, Sprite icon)
        {
            var effectObject = new GameObject("Barrage Protocol Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.SetParent(target, false);

            var effect = effectObject.AddComponent<BarrageProtocolVfx>();
            effect.Initialize(duration, icon);
            return effect;
        }

        public void Stop()
        {
            Destroy(gameObject);
        }

        private void Initialize(float duration, Sprite icon)
        {
            _duration = duration;
            _statusIcon = SpellStatusIconVfx.Create(
                transform,
                icon,
                Color.white,
                new Vector3(0f, 0.95f, 0f),
                0.42f,
                7f,
                0.08f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            var fade = Mathf.Clamp01((_duration - _elapsed) / 0.5f);
            _statusIcon.SetOpacity(fade);

            if (_elapsed >= _duration)
                Destroy(gameObject);
        }
    }

    internal sealed class SpellStatusIconVfx : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private Color _baseColor;
        private float _pulseSpeed;
        private float _bobAmplitude;
        private float _elapsed;

        public static SpellStatusIconVfx Create(
            Transform parent,
            Sprite sprite,
            Color color,
            Vector3 localPosition,
            float scale,
            float pulseSpeed,
            float bobAmplitude)
        {
            var iconObject = new GameObject("Spell Status Icon");
            iconObject.transform.SetParent(parent, false);
            iconObject.transform.localPosition = localPosition;
            iconObject.transform.localScale = Vector3.one * scale;

            var renderer = iconObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 530;

            var effect = iconObject.AddComponent<SpellStatusIconVfx>();
            effect.Initialize(renderer, localPosition, color, pulseSpeed, bobAmplitude);
            return effect;
        }

        public void SetOpacity(float opacity)
        {
            _renderer.color = new Color(
                _baseColor.r,
                _baseColor.g,
                _baseColor.b,
                _baseColor.a * opacity);
        }

        private void Initialize(
            SpriteRenderer renderer,
            Vector3 basePosition,
            Color baseColor,
            float pulseSpeed,
            float bobAmplitude)
        {
            _renderer = renderer;
            _basePosition = basePosition;
            _baseScale = transform.localScale;
            _baseColor = baseColor;
            _pulseSpeed = pulseSpeed;
            _bobAmplitude = bobAmplitude;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var wave = Mathf.Sin(_elapsed * _pulseSpeed);
            var scale = 1f + wave * 0.12f;
            transform.localScale = _baseScale * scale;
            transform.localPosition = _basePosition + Vector3.up * (wave * _bobAmplitude);
        }
    }
}
