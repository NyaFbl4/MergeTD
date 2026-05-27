using System.Collections;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.Enemies
{
    public class DamageUI : MonoBehaviour
    {
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _criticalColor = Color.red;
        [SerializeField] private TMP_Text _damageText;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _stateName;
        [SerializeField, Min(0.05f)] private float _lifeTime = 0.8f;
        [SerializeField] private bool _destroyAfterPlay = true;

        private Coroutine _destroyRoutine;

        public void Play(int damage, bool isCritical)
        {
            if (_damageText != null)
            {
                _damageText.text = damage.ToString();
                _damageText.color = isCritical ? _criticalColor : _normalColor;
            }

            if (_animator != null && !string.IsNullOrEmpty(_stateName))
            {
                _animator.Play(_stateName, 0, 0f);
                _animator.Update(0f);
            }

            if (!_destroyAfterPlay)
                return;

            if (_destroyRoutine != null)
                StopCoroutine(_destroyRoutine);

            _destroyRoutine = StartCoroutine(DestroyAfterDelay());
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(_lifeTime);
            DestroySelf();
        }
    }
}
