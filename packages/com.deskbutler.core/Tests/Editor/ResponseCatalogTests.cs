using System;
using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class ResponseCatalogTests
    {
        [Test]
        public void Every_response_has_a_line_for_both_personalities()
        {
            foreach (ResponseKey key in Enum.GetValues(typeof(ResponseKey)))
            foreach (Personality p in Enum.GetValues(typeof(Personality)))
                Assert.IsTrue(ResponseCatalog.Has(p, key), p + " missing " + key);
        }

        [Test]
        public void Rendered_lines_have_no_unfilled_placeholders()
        {
            var status = new StatusReport { Total = 3, Important = 1, Hidden = 2 };
            foreach (ResponseKey key in Enum.GetValues(typeof(ResponseKey)))
            foreach (Personality p in Enum.GetValues(typeof(Personality)))
            {
                var line = ResponseCatalog.Render(p, new AgentResult { Key = key, Argument = "x", Status = status });
                Assert.IsFalse(line.Contains("{"), p + "/" + key + ": " + line);
            }
        }

        [Test]
        public void Status_line_uses_counts()
        {
            var r = new AgentResult { Key = ResponseKey.Status, Status = new StatusReport { Total = 5, Important = 2, Hidden = 3 } };
            Assert.AreEqual("5 items on the desk, 2 marked important, 3 hidden for focus.", ResponseCatalog.Render(Personality.Alden, r));
        }

        [Test]
        public void Personalities_sound_different()
        {
            var r = new AgentResult { Key = ResponseKey.Organised };
            Assert.AreNotEqual(ResponseCatalog.Render(Personality.Alden, r), ResponseCatalog.Render(Personality.Pip, r));
        }
    }
}
