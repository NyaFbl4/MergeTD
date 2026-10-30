using System;
using Project.Scripts.System.Audio;
using Project.Scripts.System.Localization;
using Project.Scripts.System.Shop;

namespace Project.Scripts.UI.ShopUI
{
    public sealed class DefeatOfferPresenter : IDisposable
    {
        private readonly IDefeatOfferWindowView _view;
        private readonly DefeatOfferState _state;
        private readonly ShopOfferService _offers;
        private readonly ILocalizationService _localization;
        private readonly IAudioManager _audio;
        private readonly Func<ShopOfferState, ShopOfferUIData> _createUIData;
        private readonly Action _openArmy;
        private bool _visible;
        private bool _purchased;
        private bool _purchaseStarted;
        private bool _purchaseFailed;

        public DefeatOfferPresenter(
            IDefeatOfferWindowView view,
            DefeatOfferState state,
            ShopOfferService offers,
            ILocalizationService localization,
            IAudioManager audio,
            Func<ShopOfferState, ShopOfferUIData> createUIData,
            Action openArmy)
        {
            _view = view;
            _state = state;
            _offers = offers;
            _localization = localization;
            _audio = audio;
            _createUIData = createUIData;
            _openArmy = openArmy;
        }

        public void Initialize()
        {
            _view.PurchaseRequested += OnPurchaseRequested;
            _view.CloseRequested += OnCloseRequested;
            _view.UpgradeRequested += OnUpgradeRequested;
            _offers.StateChanged += Refresh;
            _offers.PurchaseSucceeded += OnPurchaseSucceeded;
            _offers.PurchaseFailed += OnPurchaseFailed;
            _localization.OnChangeLanguage += OnLanguageChanged;
            Hide();
        }

        public void ShowPendingOffer()
        {
            if (!_state.TryConsumeOffer() || !TryGetOffer(out var offer) || !offer.CanPurchase)
                return;

            _purchased = false;
            _purchaseStarted = false;
            _purchaseFailed = false;
            _visible = true;
            _view.Bind(_createUIData(offer));
            RefreshText();
            _view.SetVisible(true);
        }

        public void Hide()
        {
            _visible = false;
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            _view.PurchaseRequested -= OnPurchaseRequested;
            _view.CloseRequested -= OnCloseRequested;
            _view.UpgradeRequested -= OnUpgradeRequested;
            _offers.StateChanged -= Refresh;
            _offers.PurchaseSucceeded -= OnPurchaseSucceeded;
            _offers.PurchaseFailed -= OnPurchaseFailed;
            _localization.OnChangeLanguage -= OnLanguageChanged;
        }

        private bool TryGetOffer(out ShopOfferState offer)
        {
            foreach (var candidate in _offers.GetVisibleOffers("offers"))
            {
                if (candidate.Config.Id != DefeatOfferState.OfferId)
                    continue;

                offer = candidate;
                return true;
            }

            offer = default;
            return false;
        }

        private void Refresh()
        {
            if (!_visible || _purchased)
                return;

            if (!TryGetOffer(out var offer))
            {
                Hide();
                return;
            }

            _view.Bind(_createUIData(offer));
            RefreshText();
        }

        private void RefreshText()
        {
            _view.SetContent(
                _localization.Get(_purchased ? "defeat_offer.success_title" : "defeat_offer.title"),
                _localization.Get(_purchased ? "defeat_offer.success_description" : "defeat_offer.description"),
                _localization.Get("defeat_offer.close"),
                _localization.Get("defeat_offer.upgrade"),
                _purchaseFailed ? _localization.Get("defeat_offer.purchase_failed") : string.Empty,
                _purchased);
        }

        private void OnLanguageChanged(string _)
        {
            if (!_visible)
                return;

            if (_purchased)
                RefreshText();
            else
                Refresh();
        }

        private void OnPurchaseRequested()
        {
            if (!_visible || _purchased || _purchaseStarted)
                return;

            _audio.PlaySound(ESoundId.UiButtonClick);
            _purchaseStarted = true;
            _purchaseFailed = false;
            if (!_offers.TryPurchase(DefeatOfferState.OfferId))
                _purchaseStarted = false;
            Refresh();
        }

        private void OnPurchaseSucceeded(string offerId)
        {
            if (!_visible || !_purchaseStarted || offerId != DefeatOfferState.OfferId)
                return;

            _purchaseStarted = false;
            _purchased = true;
            RefreshText();
        }

        private void OnPurchaseFailed(string offerId)
        {
            if (!_visible || !_purchaseStarted || offerId != DefeatOfferState.OfferId)
                return;

            _purchaseStarted = false;
            _purchaseFailed = true;
            Refresh();
        }

        private void OnCloseRequested()
        {
            _audio.PlaySound(ESoundId.UiButtonClick);
            Hide();
        }

        private void OnUpgradeRequested()
        {
            if (!_visible || !_purchased)
                return;

            OnCloseRequested();
            _openArmy();
        }
    }
}
