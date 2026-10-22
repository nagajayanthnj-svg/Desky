using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class UndoBufferTests
    {
        Desk _desk;
        UndoBuffer _undo;

        [SetUp]
        public void SetUp()
        {
            _desk = Make.Desk();
            _undo = new UndoBuffer();
        }

        [Test]
        public void Nothing_to_undo_initially()
        {
            Assert.IsFalse(_undo.CanUndo);
            Assert.IsFalse(_undo.Undo(_desk));
        }

        [Test]
        public void Restores_moved_item()
        {
            var a = Make.Note(_desk, "a", 0.1f, 0.1f);
            _undo.Begin(_desk, "move");
            a.LocalPose = Pose.At(new Vec3(0.4f, 0, 0.4f));
            _undo.Commit(_desk);

            Assert.IsTrue(_undo.Undo(_desk));

            Assert.IsTrue(_desk.Get(a.Id).LocalPose.Position.ApproxEquals(new Vec3(0.1f, 0, 0.1f)));
            Assert.IsTrue(ReferenceEquals(a, _desk.Get(a.Id)), "held references stay valid after undo");
        }

        [Test]
        public void Restores_deleted_item_with_content_and_flags()
        {
            var a = Make.Note(_desk, "precious", priority: Priority.High, tags: "work");
            _undo.Begin(_desk, "delete");
            _desk.Remove(a.Id);
            _undo.Commit(_desk);

            _undo.Undo(_desk);

            var back = _desk.Get(a.Id);
            Assert.IsNotNull(back);
            Assert.AreEqual("precious", back.Content);
            Assert.AreEqual(Priority.High, back.Priority);
            Assert.AreEqual("work", back.PrimaryTag);
        }

        [Test]
        public void Removes_created_item()
        {
            _undo.Begin(_desk, "pin");
            var a = Make.Note(_desk, "new");
            _undo.Commit(_desk);

            _undo.Undo(_desk);

            Assert.IsNull(_desk.Get(a.Id));
        }

        [Test]
        public void Restores_focus_visibility_and_lock_state()
        {
            var a = Make.Note(_desk, "a");
            _undo.Begin(_desk, "focus");
            a.HiddenByFocus = true;
            a.Locked = true;
            _undo.Commit(_desk);

            _undo.Undo(_desk);

            Assert.IsFalse(_desk.Get(a.Id).HiddenByFocus);
            Assert.IsFalse(_desk.Get(a.Id).Locked);
        }

        [Test]
        public void Manual_edits_to_unaffected_items_survive_undo()
        {
            var a = Make.Note(_desk, "a", 0.1f, 0.1f);
            var b = Make.Note(_desk, "b", 0.2f, 0.2f);
            _undo.Begin(_desk, "move a");
            a.LocalPose = Pose.At(new Vec3(0.3f, 0, 0.3f));
            _undo.Commit(_desk);

            b.LocalPose = Pose.At(new Vec3(0.45f, 0, 0.45f));   // user grabs b afterwards
            _undo.Undo(_desk);

            Assert.IsTrue(_desk.Get(b.Id).LocalPose.Position.ApproxEquals(new Vec3(0.45f, 0, 0.45f)));
        }

        [Test]
        public void Only_one_level_and_cleared_after_use()
        {
            var a = Make.Note(_desk, "a", 0.1f, 0.1f);
            _undo.Begin(_desk, "1");
            a.LocalPose = Pose.At(new Vec3(0.2f, 0, 0.2f));
            _undo.Commit(_desk);
            _undo.Begin(_desk, "2");
            a.LocalPose = Pose.At(new Vec3(0.3f, 0, 0.3f));
            _undo.Commit(_desk);

            _undo.Undo(_desk);

            Assert.IsTrue(_desk.Get(a.Id).LocalPose.Position.ApproxEquals(new Vec3(0.2f, 0, 0.2f)));
            Assert.IsFalse(_undo.CanUndo);
        }

        [Test]
        public void No_op_command_keeps_previous_undo()
        {
            var a = Make.Note(_desk, "a", 0.1f, 0.1f);
            _undo.Begin(_desk, "real");
            a.Locked = true;
            _undo.Commit(_desk);

            _undo.Begin(_desk, "noop");
            Assert.IsFalse(_undo.Commit(_desk));

            Assert.AreEqual("real", _undo.Label);
            Assert.IsTrue(_undo.CanUndo);
        }
    }
}
