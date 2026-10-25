using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Project.Scripts.System.Analytics.Editor
{
    public sealed class AnalyticsEventBufferTests
    {
        private double _time;
        private List<(string Name, Dictionary<string, object> Data)> _sent;
        private AnalyticsEventBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _time = 0;
            _sent = new List<(string, Dictionary<string, object>)>();
            _buffer = new AnalyticsEventBuffer((name, data) => _sent.Add((name, data)), () => _time);
        }

        [Test]
        public void FrequentIncome_IsSummarizedByCurrencyAndSource()
        {
            var dimensions = new Dictionary<string, object> { ["currency"] = "gold", ["source"] = "wave" };
            _buffer.Add("currency_earned", 10, dimensions);
            _buffer.Add("currency_earned", 25, dimensions);
            Assert.That(_sent, Is.Empty);
            _buffer.Flush();
            Assert.That(_sent.Count, Is.EqualTo(1));
            Assert.That(_sent[0].Data["count"], Is.EqualTo(2));
            Assert.That(_sent[0].Data["amount"], Is.EqualTo(35d));
        }

        [Test]
        public void ParameterOrder_DoesNotSplitOneAggregate()
        {
            _buffer.Add("tower_buy", 50, new Dictionary<string, object> { ["type"] = "Combat", ["level"] = 1 });
            _buffer.Add("tower_buy", 50, new Dictionary<string, object> { ["level"] = 1, ["type"] = "Combat" });
            _buffer.Flush();
            Assert.That(_sent.Count, Is.EqualTo(1));
            Assert.That(_sent[0].Data["count"], Is.EqualTo(2));
        }

        [Test]
        public void DifferentWaves_KeepOriginalContextWhileWaitingForRateLimit()
        {
            var dimensions = new Dictionary<string, object> { ["wave"] = 1 };
            _buffer.Add("tower_buy", 50, dimensions);
            dimensions["wave"] = 2;
            _buffer.Add("tower_buy", 100, dimensions);
            _buffer.Flush();
            Assert.That(_sent.Count, Is.EqualTo(1));
            Assert.That(_sent[0].Data["wave"], Is.EqualTo(1));
            _time = 1.1;
            _buffer.Drain();
            Assert.That(_sent.Count, Is.EqualTo(2));
            Assert.That(_sent[1].Data["wave"], Is.EqualTo(2));
        }

        [Test]
        public void RepeatedGoal_PreservesFunnelOrderWhileWaitingOneSecond()
        {
            _buffer.Track("offer_view", new Dictionary<string, object> { ["offer_id"] = "one" });
            _buffer.Track("offer_view", new Dictionary<string, object> { ["offer_id"] = "two" });
            _buffer.Track("purchase_start", new Dictionary<string, object> { ["product_id"] = "coins" });
            Assert.That(_sent.Count, Is.EqualTo(1));
            _time = 1;
            _buffer.Drain();
            Assert.That(_sent.Count, Is.EqualTo(1));
            _time = 1.1;
            _buffer.Drain();
            Assert.That(_sent.Count, Is.EqualTo(3));
            Assert.That(_sent[1].Data["offer_id"], Is.EqualTo("two"));
            Assert.That(_sent[2].Name, Is.EqualTo("purchase_start"));
        }

        [Test]
        public void TransportFailure_DoesNotThrowOrPreventNextEvent()
        {
            var calls = 0;
            var buffer = new AnalyticsEventBuffer((name, data) =>
            {
                calls++;
                if (name == "broken") throw new InvalidOperationException("unavailable");
            }, () => _time);
            Assert.DoesNotThrow(() => buffer.Track("broken", new Dictionary<string, object>()));
            buffer.Track("healthy", new Dictionary<string, object>());
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void FlushingTwice_DoesNotRepeatAnAchievement()
        {
            _buffer.Add("tower_merge", 1, new Dictionary<string, object> { ["level"] = 2 });
            _buffer.Flush();
            _time = 2;
            _buffer.Flush();
            Assert.That(_sent.Count, Is.EqualTo(1));
        }

        [Test]
        public void SdkNotReady_QueuesEventsUntilSendingIsAvailable()
        {
            var ready = false;
            var buffer = new AnalyticsEventBuffer((name, data) => _sent.Add((name, data)), () => _time, () => ready);
            buffer.Track("game_ready", new Dictionary<string, object>());
            buffer.Track("run_start", new Dictionary<string, object>());
            Assert.That(_sent, Is.Empty);
            ready = true;
            buffer.Drain();
            Assert.That(_sent.Count, Is.EqualTo(2));
            Assert.That(_sent[0].Name, Is.EqualTo("game_ready"));
            Assert.That(_sent[1].Name, Is.EqualTo("run_start"));
        }

        [Test]
        public void InvalidAmounts_AreIgnored()
        {
            _buffer.Add("generator_used", double.NaN, new Dictionary<string, object>());
            _buffer.Add("generator_used", double.PositiveInfinity, new Dictionary<string, object>());
            _buffer.Flush();
            Assert.That(_sent, Is.Empty);
        }
    }
}
