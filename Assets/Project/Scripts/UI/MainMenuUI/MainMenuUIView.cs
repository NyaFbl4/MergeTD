using Project.Scripts.Systems.UI;
using UnityEngine;
using UnityEngine.UIElements;
using NotImplementedException = System.NotImplementedException;

namespace Project.Scripts.UI.MainMenuUI
{
    public class MainMenuUIView : LayoutViewBase, IMainMenuUIView
    {
        private Label _goldCountLabel;
        private Label _gemsCountLabel;
        private Button _nextLevelButton;
        private Button _prevLevelButton;

        public override void Awake()
        {
            base.Awake();
            
            _goldCountLabel = _root.Q<Label>("goldCount");
            _gemsCountLabel = _root.Q<Label>("diamondCount");
            _nextLevelButton = _root.Q<Button>("nextLevel");
            _prevLevelButton = _root.Q<Button>("prevLevel");
        }

        public void SetGoldCount(int goldCount)
        {
            if (goldCount < 0)
                _goldCountLabel.text = 0.ToString();
            else
                _goldCountLabel.text = goldCount.ToString();
        }

        public void SetDiamondCount(int diamondCount)
        {
            if (diamondCount < 0)
                _goldCountLabel.text = 0.ToString();
            else
                _goldCountLabel.text = diamondCount.ToString();
        }

        public void SetLevels()
        {
            
        }
    }
}
