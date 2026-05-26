using System;
using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scripts.UI.ShopUI
{
    public class ShopUIView : LayoutViewBase, IShopUIView
    {
        [SerializeField] private VisualTreeAsset _shopItemTemplate;

        private Label _titleLabel;
        private Button _closeButton;
        private ScrollView _scrollView;
        
        public event Action CloseButtonClicked;
        
        public override void Awake()
        {
            base.Awake();

            _closeButton = _root.Q<Button>("CloseButton");
            _scrollView = _root.Q<ScrollView>("ScrollView");
            _titleLabel = _root.Q<Label>("TitleLabel");
            
            if (_closeButton != null)
                _closeButton.clicked += OnCloseButtonClicked;
        }

        public void SetTitle(string title)
        {
            _titleLabel.text = title;
        }

        public void AddItem()
        {
            TemplateContainer newItem = _shopItemTemplate.Instantiate();
            
            _scrollView.Add(newItem);
        }
        
        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseButtonClicked;
        }
        
        private void OnCloseButtonClicked() => CloseButtonClicked?.Invoke();
    }
}