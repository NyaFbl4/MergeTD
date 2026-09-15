using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.MainMenuUI
{
    public interface IMainMenuUIView : ILayoutView
    {
        public void SetGoldCount(int goldCount);
        public void SetDiamondCount(int diamondCount);
        public void SetLevels();
        
    }
}