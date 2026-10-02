using Project.Scripts.Configs;
using UnityEngine;

namespace Project.Scripts.Gameplay.Systems
{
    public sealed class BaseRepairVfx : MonoBehaviour
    {
        private const int RingCount = 3;
        private const int CircleSegments = 48;
        private const float RingDelay = 0.14f;
        private const float RingDuration = 0.55f;

        private readonly LineRenderer[] _rings = new LineRenderer[RingCount];

        private Material _material;
        private float _elapsed;

        public static void Play(Vector3 position)
        {
            var effectObject = new GameObject("Base Repair Effect")
            {
                hideFlags = HideFlags.DontSave
            };
            effectObject.transform.position = position;
            effectObject.AddComponent<BaseRepairVfx>().Initialize();
        }

        private void Initialize()
        {
            _material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Base Repair Runtime Material"
            };

            for (var i = 0; i < RingCount; i++)
                _rings[i] = CreateRing(i);
        }

        private LineRenderer CreateRing(int index)
        {
            var ringObject = new GameObject($"Repair Ring {index + 1}");
            ringObject.transform.SetParent(transform, false);

            var line = ringObject.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = CircleSegments;
            line.startWidth = 0.07f;
            line.endWidth = 0.07f;
            line.sortingOrder = 520 + index;
            line.enabled = false;

            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = Mathf.PI * 2f * i / CircleSegments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }

            return line;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            for (var i = 0; i < RingCount; i++)
            {
                var progress = (_elapsed - i * RingDelay) / RingDuration;
                var line = _rings[i];

                if (progress < 0f)
                    continue;

                line.enabled = progress < 1f;
                if (!line.enabled)
                    continue;

                var normalizedProgress = Mathf.Clamp01(progress);
                var scale = Mathf.Lerp(0.25f, 1.35f, normalizedProgress);
                line.transform.localScale = new Vector3(scale, scale, 1f);

                var color = new Color(0.2f, 1f, 0.55f, 1f - normalizedProgress);
                line.startColor = color;
                line.endColor = color;
            }

            if (_elapsed >= RingDuration + (RingCount - 1) * RingDelay)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }
    }

    public static class SpellPrefabVfx
    {
        private const int EffectSortingOrder = 520;

        public static GameObject PlayAt(BaseSpellConfig config, Vector3 position)
        {
            return Create(config, position, null);
        }

        public static GameObject PlayAttached(BaseSpellConfig config, Transform target)
        {
            if (target == null)
                return null;

            return Create(config, target.position, target);
        }

        private static GameObject Create(
            BaseSpellConfig config,
            Vector3 position,
            Transform parent)
        {
            if (config == null || config.VfxPrefab == null)
                return null;

            var instanceObject = UnityEngine.Object.Instantiate((UnityEngine.Object)config.VfxPrefab);
            var instance = instanceObject as GameObject;
            if (instance == null)
            {
                Debug.LogError($"Spell VFX '{config.VfxPrefab.name}' is not a GameObject prefab.");
                UnityEngine.Object.Destroy(instanceObject);
                return null;
            }

            if (parent == null)
            {
                instance.transform.position = position;
                instance.transform.rotation *= Quaternion.Euler(config.VfxEulerAngles);
            }
            else
            {
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation *= Quaternion.Euler(config.VfxEulerAngles);
            }

            instance.transform.localScale *= config.VfxScale;
            instance.hideFlags = HideFlags.DontSave;
            ApplySortingOrder(instance);

            if (config.VfxLifetime > 0f)
                UnityEngine.Object.Destroy(instance, config.VfxLifetime);

            return instance;
        }

        private static void ApplySortingOrder(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].sortingOrder += EffectSortingOrder;
        }
    }
}
