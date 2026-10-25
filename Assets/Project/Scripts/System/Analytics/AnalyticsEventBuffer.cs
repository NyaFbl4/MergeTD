using System;
using System.Collections.Generic;
using YG.Utils.Metrica;

namespace Project.Scripts.System.Analytics
{
    // Frequent actions are summarized by their dimensions instead of sent on every occurrence.
    public sealed class AnalyticsEventBuffer
    {
        private const int MaximumGroups = 128;
        private readonly Action<string, Dictionary<string, object>> _send;
        private readonly Func<double> _clock;
        private readonly Func<bool> _canSend;
        private readonly Queue<(string Name, Dictionary<string, object> Data)> _pending = new();
        private readonly Dictionary<string, double> _lastSent = new();
        private readonly Dictionary<string, Dictionary<string, object>> _groups = new();
        private readonly Dictionary<string, string> _names = new();

        public AnalyticsEventBuffer(Action<string, Dictionary<string, object>> send, Func<double> clock = null, Func<bool> canSend = null)
        {
            _send = send ?? throw new ArgumentNullException(nameof(send));
            _clock = clock ?? (() => global::System.Diagnostics.Stopwatch.GetTimestamp() /
                                    (double)global::System.Diagnostics.Stopwatch.Frequency);
            _canSend = canSend ?? (() => true);
        }

        public void Track(string name, Dictionary<string, object> parameters)
        {
            if (_pending.Count < 256)
                _pending.Enqueue((name, new Dictionary<string, object>(parameters)));
            Drain();
        }

        public void Drain()
        {
            if (!_canSend()) return;
            var now = _clock();
            var count = _pending.Count;
            for (var i = 0; i < count; i++)
            {
                var entry = _pending.Peek();
                if (_lastSent.TryGetValue(entry.Name, out var last) && now - last < 1.05d)
                {
                    break;
                }
                _pending.Dequeue();
                _lastSent[entry.Name] = now;
                try { _send(entry.Name, entry.Data); }
                catch { /* Analytics must never interrupt gameplay or reward delivery. */ }
            }
        }

        public void Add(string name, double amount, Dictionary<string, object> dimensions)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
                return;

            var sorted = new SortedDictionary<string, object>(dimensions, StringComparer.Ordinal);
            var key = name + ":" + JsonUtils.ToJson(sorted);
            if (!_groups.TryGetValue(key, out var group))
            {
                if (_groups.Count >= MaximumGroups)
                    Flush();
                group = new Dictionary<string, object>(dimensions) { ["count"] = 0, ["amount"] = 0d };
                _groups.Add(key, group);
                _names.Add(key, name);
            }

            group["count"] = (int)group["count"] + 1;
            group["amount"] = (double)group["amount"] + amount;
        }

        public void Flush()
        {
            var events = new List<(string Name, Dictionary<string, object> Data)>(_groups.Count);
            foreach (var pair in _groups)
                events.Add((_names[pair.Key], pair.Value));
            _groups.Clear();
            _names.Clear();
            foreach (var entry in events)
                Track(entry.Name, entry.Data);
        }
    }
}
