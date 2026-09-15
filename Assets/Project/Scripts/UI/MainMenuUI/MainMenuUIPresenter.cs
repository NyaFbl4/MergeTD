using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.MainMenuUI
{
    public class MainMenuUIPresenter: LayoutPresenterBase<IMainMenuUIView>, IMainMenuUIPresenter
    {
        private readonly MainMenuUIUseCase _menuUIUseCase;

        public MainMenuUIPresenter(MainMenuUIUseCase menuUIUseCase)
        {
            _menuUIUseCase = menuUIUseCase;    
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            _menuUIUseCase.GoldChanged += OnGoldChanged;
            _menuUIUseCase.GemsChanged += OnGemsChanged;

            _layoutView.SetGoldCount(_menuUIUseCase.Gold);
            _layoutView.SetDiamondCount(_menuUIUseCase.Gems);
        }
        
        private void OnGoldChanged(int value)
        {
            _layoutView.SetGoldCount(value);
        }

        private void OnGemsChanged(int value)
        {
            _layoutView.SetDiamondCount(value);
        }

        public override void Dispose()
        {
            _menuUIUseCase.GoldChanged -= OnGoldChanged;
            _menuUIUseCase.GemsChanged -= OnGemsChanged;

            base.Dispose();
        }
    }
}