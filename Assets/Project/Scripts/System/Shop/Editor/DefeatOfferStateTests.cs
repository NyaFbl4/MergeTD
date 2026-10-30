using NUnit.Framework;

namespace Project.Scripts.System.Shop.Editor
{
    public sealed class DefeatOfferStateTests
    {
        [Test]
        public void ConsecutiveDefeats_OfferOnSecondFourthAndSixth()
        {
            var state = new DefeatOfferState();
            for (var loss = 1; loss <= 6; loss++)
            {
                state.RecordDefeat();
                Assert.That(state.TryConsumeOffer(), Is.EqualTo(loss % 2 == 0), $"Loss {loss}");
            }
        }

        [Test]
        public void ReturningToMenu_DoesNotRepeatConsumedOffer()
        {
            var state = new DefeatOfferState();
            state.RecordDefeat();
            state.RecordDefeat();
            Assert.That(state.TryConsumeOffer(), Is.True);
            Assert.That(state.TryConsumeOffer(), Is.False);
            Assert.That(state.TryConsumeOffer(), Is.False);
        }

        [Test]
        public void VictoryBetweenLosses_RequiresTwoNewDefeats()
        {
            var state = new DefeatOfferState();
            state.RecordDefeat();
            state.RecordVictory();
            state.RecordDefeat();
            Assert.That(state.TryConsumeOffer(), Is.False);
            state.RecordDefeat();
            Assert.That(state.TryConsumeOffer(), Is.True);
        }

        [Test]
        public void Victory_CancelsPendingOffer()
        {
            var state = new DefeatOfferState();
            state.RecordDefeat();
            state.RecordDefeat();
            state.RecordVictory();
            Assert.That(state.TryConsumeOffer(), Is.False);
        }

        [Test]
        public void NewSession_DoesNotKeepPreviousLossOrPendingOffer()
        {
            var previousSession = new DefeatOfferState();
            previousSession.RecordDefeat();
            previousSession.RecordDefeat();
            var newSession = new DefeatOfferState();
            Assert.That(newSession.TryConsumeOffer(), Is.False);
            newSession.RecordDefeat();
            Assert.That(newSession.TryConsumeOffer(), Is.False);
            newSession.RecordDefeat();
            Assert.That(newSession.TryConsumeOffer(), Is.True);
        }
    }
}
