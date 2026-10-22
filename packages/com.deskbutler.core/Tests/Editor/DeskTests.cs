using System;
using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class DeskTests
    {
        [Test]
        public void Add_sets_id_type_pose_and_timestamps()
        {
            var clock = new FakeClock(5000);
            var desk = Make.Desk(clock);

            var item = desk.Add(ItemType.Panel, "Body", Pose.At(new Vec3(0.2f, 0, 0.3f)), "Title");

            Assert.AreEqual("item1", item.Id);
            Assert.AreEqual(ItemType.Panel, item.Type);
            Assert.AreEqual("Title", item.Title);
            Assert.AreEqual(5000L, item.CreatedAtMs);
            Assert.AreEqual(5000L, item.UpdatedAtMs);
            Assert.IsTrue(item.LocalPose.Position.ApproxEquals(new Vec3(0.2f, 0, 0.3f)));
        }

        [Test]
        public void Touch_updates_only_UpdatedAt()
        {
            var clock = new FakeClock(1000);
            var desk = Make.Desk(clock);
            var item = Make.Note(desk, "a");
            clock.Advance(500);

            desk.Touch(item);

            Assert.AreEqual(1000L, item.CreatedAtMs);
            Assert.AreEqual(1500L, item.UpdatedAtMs);
        }

        [Test]
        public void Snapshot_round_trip_preserves_every_field()
        {
            var desk = Make.Desk();
            desk.AnchorId = "anchor-uuid";
            var a = Make.Note(desk, "Pricing tiers", 0.1f, 0.2f, Priority.High, true, "pricing", "launch");
            a.HiddenByFocus = true;
            a.LocalPose = new Pose(new Vec3(0.1f, 0, 0.2f), Quat.FromYawDegrees(30), new Vec3(2, 2, 2));

            var restored = Desk.FromSnapshot(desk.ToSnapshot(), new FakeClock(), new SequentialIds());

            Assert.AreEqual("anchor-uuid", restored.AnchorId);
            Assert.AreEqual(1, restored.Items.Count);
            Assert.IsTrue(restored.Items[0].SameStateAs(a));
            Assert.AreEqual(a.CreatedAtMs, restored.Items[0].CreatedAtMs);
        }

        [Test]
        public void Snapshot_is_a_deep_copy()
        {
            var desk = Make.Desk();
            var a = Make.Note(desk, "a", tags: "x");
            var snap = desk.ToSnapshot();

            a.Tags.Add("y");
            a.Content = "changed";

            Assert.AreEqual("a", snap.Items[0].Content);
            Assert.AreEqual(1, snap.Items[0].Tags.Count);
        }

        [Test]
        public void Snapshot_from_newer_schema_is_rejected()
        {
            var snap = new DeskSnapshot { SchemaVersion = DeskSnapshot.CurrentSchema + 1 };
            Assert.Throws<NotSupportedException>(() => Desk.FromSnapshot(snap, new FakeClock(), new SequentialIds()));
        }

        [Test]
        public void Null_tag_lists_from_json_become_empty()
        {
            var snap = new DeskSnapshot();
            snap.Items.Add(new DeskItem { Id = "x", Content = "c", Tags = null });

            var desk = Desk.FromSnapshot(snap, new FakeClock(), new SequentialIds());

            Assert.IsNotNull(desk.Items[0].Tags);
        }

        [Test]
        public void LoadOrCreate_returns_empty_desk_without_anchor_on_first_run()
        {
            var desk = Desk.LoadOrCreate(new InMemoryDeskStore(), new FakeClock(), new SequentialIds());
            Assert.AreEqual(0, desk.Items.Count);
            Assert.IsFalse(desk.HasAnchor);
        }

        [Test]
        public void LoadOrCreate_rehydrates_saved_desk()
        {
            var store = new InMemoryDeskStore();
            var desk = Make.Desk();
            desk.AnchorId = "a1";
            Make.Note(desk, "kept");
            store.Save(desk.ToSnapshot());

            var loaded = Desk.LoadOrCreate(store, new FakeClock(), new SequentialIds());

            Assert.IsTrue(loaded.HasAnchor);
            Assert.AreEqual("kept", loaded.Items[0].Content);
        }

        [Test]
        public void Put_replaces_by_id_or_appends()
        {
            var desk = Make.Desk();
            var a = Make.Note(desk, "a");
            var copy = a.Clone();
            copy.Content = "a2";

            desk.Put(copy);
            desk.Put(new DeskItem { Id = "new", Content = "n" });

            Assert.AreEqual(2, desk.Items.Count);
            Assert.AreEqual("a2", desk.Get(a.Id).Content);
            Assert.IsTrue(ReferenceEquals(a, desk.Get(a.Id)), "existing instance is updated in place");
        }
    }
}
