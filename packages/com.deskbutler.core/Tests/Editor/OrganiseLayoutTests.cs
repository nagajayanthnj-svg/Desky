using System.Linq;
using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class OrganiseLayoutTests
    {
        [Test]
        public void Groups_by_primary_tag_into_columns_untagged_last()
        {
            var desk = Make.Desk();
            var u = Make.Note(desk, "untagged");
            var r1 = Make.Note(desk, "r1", tags: new[] { "research" });
            var b1 = Make.Note(desk, "b1", tags: new[] { "budget" });
            var r2 = Make.Note(desk, "r2", tags: new[] { "Research" });

            var plan = OrganiseLayout.Plan(desk.Items, desk.Bounds);

            Assert.AreEqual(plan[r1.Id].Position.X, plan[r2.Id].Position.X, 1e-4f);  // same column, case-insensitive
            Assert.Less(plan[b1.Id].Position.X, plan[r1.Id].Position.X);              // budget < research alphabetically
            Assert.Greater(plan[u.Id].Position.X, plan[r1.Id].Position.X);             // untagged last
        }

        [Test]
        public void High_priority_first_within_a_group()
        {
            var desk = Make.Desk();
            var normal = Make.Note(desk, "n", tags: new[] { "work" });
            var high = Make.Note(desk, "h", priority: Priority.High, tags: new[] { "work" });

            var plan = OrganiseLayout.Plan(desk.Items, desk.Bounds);

            Assert.Less(plan[high.Id].Position.Z, plan[normal.Id].Position.Z);
        }

        [Test]
        public void Locked_items_are_not_planned_and_not_overlapped()
        {
            var desk = Make.Desk();
            var firstSlot = new Vec3(desk.Bounds.MinX + OrganiseLayout.Margin, 0, desk.Bounds.MinZ + OrganiseLayout.Margin);
            var locked = Make.Note(desk, "locked", firstSlot.X, firstSlot.Z, locked: true);
            var free = Make.Note(desk, "free", 0.3f, 0.3f);

            var plan = OrganiseLayout.Plan(desk.Items, desk.Bounds);

            Assert.IsFalse(plan.ContainsKey(locked.Id));
            Assert.GreaterOrEqual(Vec3.Distance(plan[free.Id].Position, firstSlot), OrganiseLayout.LockedClearance);
        }

        [Test]
        public void Wraps_to_new_column_when_column_is_full_and_keeps_scale()
        {
            var desk = Make.Desk();
            for (var i = 0; i < 12; i++) Make.Note(desk, "n" + i);
            desk.Items[0].LocalPose = new Pose(Vec3.Zero, Quat.Identity, new Vec3(2, 2, 2));

            var plan = OrganiseLayout.Plan(desk.Items, desk.Bounds);

            Assert.Greater(plan.Values.Select(p => p.Position.X).Distinct().Count(), 1);
            Assert.IsTrue(plan.Values.All(p => p.Position.Z <= desk.Bounds.MaxZ));
            Assert.IsTrue(plan[desk.Items[0].Id].Scale.ApproxEquals(new Vec3(2, 2, 2)));
        }

        [Test]
        public void No_two_items_share_a_slot()
        {
            var desk = Make.Desk();
            for (var i = 0; i < 20; i++) Make.Note(desk, "n" + i, tags: new[] { "t" + (i % 3) });

            var plan = OrganiseLayout.Plan(desk.Items, desk.Bounds);
            var spots = plan.Values.Select(p => p.Position.ToString()).ToList();

            Assert.AreEqual(spots.Count, spots.Distinct().Count());
        }
    }
}
