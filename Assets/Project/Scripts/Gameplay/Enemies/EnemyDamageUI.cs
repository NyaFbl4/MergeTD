using UnityEngine;

namespace Project.Scripts.Gameplay.Enemies
{
    public class EnemyDamageUI : MonoBehaviour
    {
        [SerializeField] private DamageUI _damegeUI;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _popupsRoot;
        [SerializeField] private Vector2 _randomOffset = new Vector2(0.15f, 0.1f);
        [SerializeField] private bool _hideTemplateOnAwake = true;

        private void Awake()
        {
            if (_hideTemplateOnAwake && _damegeUI != null)
                _damegeUI.gameObject.SetActive(false);
        }

        public void ShowDamage(int damage)
        {
            if (_damegeUI == null)
                return;

            var spawnPoint = _spawnPoint != null ? _spawnPoint : _damegeUI.transform;
            var offset = new Vector3(
                Random.Range(-_randomOffset.x, _randomOffset.x),
                Random.Range(0f, _randomOffset.y),
                0f);

            var parent = _popupsRoot != null ? _popupsRoot : _damegeUI.transform.parent;
            var damageUI = Instantiate(_damegeUI, spawnPoint.position + offset, spawnPoint.rotation, parent);
            damageUI.gameObject.SetActive(true);
            damageUI.Play(damage);
        }
    }
}

