using System;
using System.Threading.Tasks;

namespace DeskButler.Core
{
    // Ports: everything outside the core (time, ids, disk, speech, LLM) sits behind these.
    // Unity adapters implement them; tests use fakes. See DECISIONS D-001.

    public interface IClock { long NowMs { get; } }

    public sealed class SystemClock : IClock
    {
        public long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
    }

    public interface IIdGenerator { string NewId(); }

    public sealed class GuidIdGenerator : IIdGenerator
    {
        public string NewId() { return Guid.NewGuid().ToString("N"); }
    }

    public interface IDeskStore
    {
        /// <summary>Returns null when nothing has been saved yet.</summary>
        DeskSnapshot Load();
        void Save(DeskSnapshot snapshot);
    }

    public sealed class InMemoryDeskStore : IDeskStore
    {
        public DeskSnapshot Saved;
        public int SaveCount;
        public DeskSnapshot Load() { return Saved; }
        public void Save(DeskSnapshot snapshot) { Saved = snapshot; SaveCount++; }
    }

    /// <summary>Speech-to-text. Adapter raises events on the main thread.</summary>
    public interface ISpeechToText
    {
        event Action<string> Partial;
        event Action<string> Final;
        event Action<string> Error;   // includes mic permission denied
        void StartListening();
        void StopListening();
    }

    public interface ITextToSpeech
    {
        void Speak(string text, Personality voice);
        bool IsSpeaking { get; }
    }

    /// <summary>
    /// Optional LLM that may only re-word a line the core already chose (DECISIONS D-004).
    /// It never selects actions or names items. Callers must fall back to the baseline on timeout.
    /// </summary>
    public interface IFlavourModel
    {
        Task<string> Rephrase(string baselineLine, Personality personality, int timeoutMs);
    }
}
