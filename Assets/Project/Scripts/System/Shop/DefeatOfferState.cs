namespace Project.Scripts.System.Shop
{
    public sealed class DefeatOfferState
    {
        public const string OfferId = "offer_gold_gems";
        private int _defeatsSinceOffer;
        private bool _offerPending;

        public void RecordDefeat()
        {
            if (++_defeatsSinceOffer < 2)
                return;

            _defeatsSinceOffer = 0;
            _offerPending = true;
        }

        public void RecordVictory()
        {
            _defeatsSinceOffer = 0;
            _offerPending = false;
        }

        public bool TryConsumeOffer()
        {
            if (!_offerPending)
                return false;

            _offerPending = false;
            return true;
        }
    }
}
