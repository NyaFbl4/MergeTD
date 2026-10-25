using System;
using Project.Scripts.Configs;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Enums;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Save;

namespace Project.Scripts.UI.TowerSlotUnlockUI
{
    public sealed class TowerSlotUnlockPresenter : IDisposable
    {
        private readonly ITowerSlotUnlockView _view;
        private readonly IWorldService _world;
        private readonly ILocalizationService _localization;
        private readonly IAudioManager _audio;
        private readonly Func<bool> _canInteract;
        private readonly TowerSlotUnlockConfig _config;
        private string _slotId;
        private int _shownPrice;

        public TowerSlotUnlockPresenter(
            ITowerSlotUnlockView view,
            IWorldService world,
            ILocalizationService localization,
            IAudioManager audio,
            Func<bool> canInteract)
        {
            _view = view;
            _world = world;
            _localization = localization;
            _audio = audio;
            _canInteract = canInteract;
            _config = TowerSlotUnlockConfig.LoadFromResources();
            _view.PurchaseRequested += OnPurchaseRequested;
            _view.CloseRequested += OnCloseRequested;
            _world.GoldChanged += OnGoldChanged;
            _world.TowerSlotsChanged += Refresh;
            _localization.OnChangeLanguage += OnLanguageChanged;
        }

        public bool IsVisible => _slotId != null;

        public void Show(string slotId)
        {
            if (!_canInteract() || TowerSlotUnlockConfig.GetIndex(slotId) < 0
                                || _world.IsTowerSlotUnlocked(slotId))
                return;

            _slotId = slotId;
            _audio.PlaySound(ESoundId.UiButtonClick);
            Refresh();
            _view.SetVisible(true);
        }

        public void Hide()
        {
            _slotId = null;
            _view.SetVisible(false);
        }

        private void Refresh()
        {
            if (!IsVisible)
                return;

            if (!_canInteract() || _world.IsTowerSlotUnlocked(_slotId))
            {
                Hide();
                return;
            }

            _shownPrice = _config.GetPrice(_slotId);
            var isNext = TowerSlotUnlockConfig.IsNextLockedSlot(_world, _slotId);
            var enoughGold = _world.Gold >= _shownPrice;
            var status = !isNext
                ? _localization.Get("slots.previous_required")
                : !enoughGold
                    ? _localization.Format("slots.missing_gold", _shownPrice - _world.Gold)
                    : _localization.Get("slots.ready");

            _view.SetContent(
                _localization.Get("slots.title"),
                _localization.Format("slots.ordinal", TowerSlotUnlockConfig.GetIndex(_slotId) + 1, 5),
                _localization.Get("slots.description"),
                _localization.Format("slots.price", _shownPrice),
                status,
                _localization.Format("slots.unlock", _shownPrice),
                _localization.Get("common.close"),
                isNext && enoughGold);
        }

        private void OnPurchaseRequested()
        {
            if (!IsVisible)
                return;

            if (!_canInteract())
            {
                Hide();
                return;
            }

            if (_world.TryPurchaseTowerSlot(_slotId, _shownPrice))
            {
                _audio.PlaySound(ESoundId.AddUpgrade);
                Hide();
            }
            else
            {
                Refresh();
            }
        }

        private void OnCloseRequested()
        {
            _audio.PlaySound(ESoundId.UiButtonClick);
            Hide();
        }

        private void OnGoldChanged(int _) => Refresh();
        private void OnLanguageChanged(string _) => Refresh();

        public void Dispose()
        {
            _view.PurchaseRequested -= OnPurchaseRequested;
            _view.CloseRequested -= OnCloseRequested;
            _world.GoldChanged -= OnGoldChanged;
            _world.TowerSlotsChanged -= Refresh;
            _localization.OnChangeLanguage -= OnLanguageChanged;
        }
    }
}
