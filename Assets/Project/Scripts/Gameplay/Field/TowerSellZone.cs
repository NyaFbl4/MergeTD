using Project.Scripts.Gameplay.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Gameplay.Field
{
    public sealed class TowerSellZone : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _background;
        [SerializeField] private Text _label;

        [Header("View")]
        [SerializeField] private Vector3 _worldOffset = new(0f, 0.35f, 0f);
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _hoverColor = new(1f, 0.82f, 0.35f, 1f);

        public int CurrentRefund { get; private set; }
        public int SortingLayerId => _canvas.sortingLayerID;
        public int SortingOrder => _canvas.sortingOrder;

        public TowerSellZone InstantiateFor(Transform anchor)
        {
            var sellZone = Instantiate(this, anchor.parent);
            sellZone.Initialize(anchor);
            return sellZone;
        }

        public void Show(TowerUnit tower)
        {
            CurrentRefund = TowerEconomy.GetSellRefund(tower.PurchaseCost);
            _label.text = $"Продать за {CurrentRefund}";
            _background.color = _idleColor;
            gameObject.SetActive(true);
        }

        public void SetHovered(bool isHovered)
        {
            _background.color = isHovered ? _hoverColor : _idleColor;
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                _rectTransform,
                screenPoint,
                eventCamera);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Initialize(Transform anchor)
        {
            _rectTransform.position = anchor.position + _worldOffset;
            _rectTransform.rotation = Quaternion.identity;
            _canvas.worldCamera = Camera.main;
            gameObject.SetActive(false);
        }
    }
}
