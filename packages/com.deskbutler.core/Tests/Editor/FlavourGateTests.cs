using System;
using System.Threading.Tasks;
using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class FlavourGateTests
    {
        sealed class FakeModel : IFlavourModel
        {
            public Func<string, Personality, Task<string>> Impl;
            public Personality? LastPersonality;

            public Task<string> Rephrase(string baselineLine, Personality personality, int timeoutMs)
            {
                LastPersonality = personality;
                return Impl(baselineLine, personality);
            }
        }

        const string Baseline = "The desk is in order.";

        static string Run(FlavourGate gate, Personality p = Personality.Alden)
        {
            return gate.Line(Baseline, p).GetAwaiter().GetResult();
        }

        [Test]
        public void No_model_uses_baseline()
        {
            Assert.AreEqual(Baseline, Run(new FlavourGate(null)));
        }

        [Test]
        public void Fast_answer_is_used_trimmed_and_gets_personality()
        {
            var model = new FakeModel { Impl = (b, p) => Task.FromResult("  All tidy, as it should be.  ") };

            Assert.AreEqual("All tidy, as it should be.", Run(new FlavourGate(model), Personality.Pip));
            Assert.AreEqual(Personality.Pip, model.LastPersonality);
        }

        [Test]
        public void Slow_answer_falls_back_to_baseline()
        {
            var model = new FakeModel
            {
                Impl = async (b, p) => { await Task.Delay(2000); return "too late"; }
            };
            Assert.AreEqual(Baseline, Run(new FlavourGate(model, timeoutMs: 50)));
        }

        [Test]
        public void Synchronous_throw_falls_back()
        {
            var model = new FakeModel { Impl = (b, p) => { throw new InvalidOperationException("no network"); } };
            Assert.AreEqual(Baseline, Run(new FlavourGate(model)));
        }

        [Test]
        public void Faulted_task_falls_back()
        {
            var model = new FakeModel
            {
                Impl = async (b, p) => { await Task.Yield(); throw new TimeoutException(); }
            };
            Assert.AreEqual(Baseline, Run(new FlavourGate(model)));
        }

        [Test]
        public void Empty_answer_falls_back()
        {
            var model = new FakeModel { Impl = (b, p) => Task.FromResult("   ") };
            Assert.AreEqual(Baseline, Run(new FlavourGate(model)));
        }

        [Test]
        public void Rambling_answer_falls_back()
        {
            var model = new FakeModel { Impl = (b, p) => Task.FromResult(new string('a', FlavourGate.MaxChars + 1)) };
            Assert.AreEqual(Baseline, Run(new FlavourGate(model)));
        }

        [Test]
        public void Default_timeout_matches_latency_budget()
        {
            Assert.AreEqual(700, FlavourGate.DefaultTimeoutMs);
        }
    }
}
