using System;
using System.Collections.Generic;
using System.Linq;
using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class IdleBehaviourTests
    {
        [Test]
        public void Pip_never_changes_saved_desk_state()
        {
            var desk = Make.Desk();
            Make.Note(desk, "a");
            Make.Note(desk, "b");
            var before = desk.ToSnapshot();
            var offsets = new Dictionary<string, VisualOffset>();
            var rng = new Random(42);

            for (var i = 0; i < 50; i++) offsets = IdleBehaviour.PipNudge(desk.Items, offsets, rng);

            for (var i = 0; i < desk.Items.Count; i++) Assert.IsTrue(desk.Items[i].SameStateAs(before.Items[i]));
        }

        [Test]
        public void Pip_skips_locked_and_hidden_items()
        {
            var desk = Make.Desk();
            var locked = Make.Note(desk, "l", locked: true);
            var hidden = Make.Note(desk, "h");
            hidden.HiddenByFocus = true;
            var free = Make.Note(desk, "f");
            var offsets = new Dictionary<string, VisualOffset>();
            var rng = new Random(1);

            for (var i = 0; i < 50; i++) offsets = IdleBehaviour.PipNudge(desk.Items, offsets, rng);

            Assert.IsFalse(offsets.ContainsKey(locked.Id));
            Assert.IsFalse(offsets.ContainsKey(hidden.Id));
            Assert.IsTrue(offsets.ContainsKey(free.Id));
        }

        [Test]
        public void Pip_offsets_stay_small()
        {
            var desk = Make.Desk();
            for (var i = 0; i < 5; i++) Make.Note(desk, "n" + i);
            var offsets = new Dictionary<string, VisualOffset>();
            var rng = new Random(7);

            for (var i = 0; i < 200; i++) offsets = IdleBehaviour.PipNudge(desk.Items, offsets, rng);

            Assert.IsTrue(offsets.Values.All(o =>
                Math.Abs(o.Position.X) <= IdleBehaviour.PipMaxOffsetMetres &&
                Math.Abs(o.Position.Z) <= IdleBehaviour.PipMaxOffsetMetres &&
                o.Position.Y == 0 &&
                Math.Abs(o.YawDegrees) <= IdleBehaviour.PipMaxYawDegrees));
        }

        [Test]
        public void Offsets_on_items_that_become_locked_are_dropped()
        {
            var desk = Make.Desk();
            var a = Make.Note(desk, "a");
            var offsets = IdleBehaviour.PipNudge(desk.Items, new Dictionary<string, VisualOffset>(), new Random(3));
            Assert.IsTrue(offsets.ContainsKey(a.Id));

            a.Locked = true;
            offsets = IdleBehaviour.PipNudge(desk.Items, offsets, new Random(3));

            Assert.IsFalse(offsets.ContainsKey(a.Id));
        }

        [Test]
        public void Same_seed_same_nudges()
        {
            var desk = Make.Desk();
            for (var i = 0; i < 4; i++) Make.Note(desk, "n" + i);
            var a = IdleBehaviour.PipNudge(desk.Items, new Dictionary<string, VisualOffset>(), new Random(9));
            var b = IdleBehaviour.PipNudge(desk.Items, new Dictionary<string, VisualOffset>(), new Random(9));
            Assert.AreEqual(a.Keys.Single(), b.Keys.Single());
        }

        [Test]
        public void Alden_straightens_everything_Pip_moved()
        {
            var desk = Make.Desk();
            Make.Note(desk, "a");
            var offsets = IdleBehaviour.PipNudge(desk.Items, new Dictionary<string, VisualOffset>(), new Random(5));

            Assert.AreEqual(offsets.Count, IdleBehaviour.AldenTargets(offsets).Count);
            Assert.AreEqual(0, IdleBehaviour.AldenStraighten().Count);
        }
    }
}
