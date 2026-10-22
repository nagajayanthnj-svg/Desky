using System;
using System.Threading.Tasks;

namespace DeskButler.Core
{
    /// <summary>
    /// Lets the optional LLM re-word a line the core already chose, and only if it is quick and sane (D-004).
    /// Any timeout, error, empty or rambling answer falls back to the deterministic baseline.
    /// No ConfigureAwait(false): in Unity the continuation must come back to the main thread.
    /// </summary>
    public sealed class FlavourGate
    {
        public const int DefaultTimeoutMs = 700;   // PRD 5.8 latency budget
        public const int MaxChars = 200;           // subtitles stay readable; no monologues

        readonly IFlavourModel _model;
        readonly int _timeoutMs;

        public FlavourGate(IFlavourModel model, int timeoutMs = DefaultTimeoutMs)
        {
            _model = model;
            _timeoutMs = timeoutMs;
        }

        public async Task<string> Line(string baseline, Personality personality)
        {
            if (_model == null) return baseline;
            try
            {
                var call = _model.Rephrase(baseline, personality, _timeoutMs);
                var winner = await Task.WhenAny(call, Task.Delay(_timeoutMs));
                if (winner != call) return baseline;

                var text = (await call ?? "").Trim();
                return text.Length == 0 || text.Length > MaxChars ? baseline : text;
            }
            catch (Exception)
            {
                return baseline;
            }
        }
    }
}
