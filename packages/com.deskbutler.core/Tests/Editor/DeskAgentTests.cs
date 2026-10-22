using System.Linq;
using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class DeskAgentTests
    {
        FakeClock _clock;
        Desk _desk;
        DeskAgent _agent;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock();
            _desk = Make.Desk(_clock);
            _agent = new DeskAgent(_desk);
        }

        AgentResult Say(string utterance, string target = null) { return _agent.Handle(Make.Cmd(utterance), target); }

        // ---------- capture ----------

        [Test]
        public void New_note_starts_capture_and_does_not_touch_desk()
        {
            var r = Say("new note");
            Assert.AreEqual(Outcome.StartCapture, r.Outcome);
            Assert.AreEqual(0, _desk.Items.Count);
        }

        [Test]
        public void Inline_note_text_is_passed_to_capture()
        {
            Assert.AreEqual("call the vendor", Say("pin this idea: call the vendor").Argument);
        }

        [Test]
        public void PinNote_creates_item_and_is_undoable()
        {
            var r = _agent.PinNote("Buy SD card", Pose.At(new Vec3(0.1f, 0, 0.1f)));

            Assert.AreEqual(ResponseKey.Pinned, r.Key);
            Assert.AreEqual(1, _desk.Items.Count);
            Say("undo");
            Assert.AreEqual(0, _desk.Items.Count);
        }

        // ---------- organise ----------

        [Test]
        public void Organise_moves_unlocked_items_and_reports_count()
        {
            Make.Note(_desk, "a", 0.33f, 0.41f);
            Make.Note(_desk, "b", -0.2f, 0.5f);

            var r = Say("clean up the desk");

            Assert.AreEqual(ResponseKey.Organised, r.Key);
            Assert.AreEqual(2, r.ItemIds.Count);
            Assert.IsTrue(r.DeskChanged);
        }

        [Test]
        public void Organise_never_moves_locked_items()
        {
            var locked = Make.Note(_desk, "locked", 0.33f, 0.41f, locked: true);
            Make.Note(_desk, "free", 0.2f, 0.2f);

            Say("organise the desk");

            Assert.IsTrue(_desk.Get(locked.Id).LocalPose.Position.ApproxEquals(new Vec3(0.33f, 0, 0.41f)));
        }

        [Test]
        public void Organise_twice_reports_nothing_to_do_and_keeps_undo()
        {
            var a = Make.Note(_desk, "a", 0.33f, 0.41f);
            Say("clean up the desk");
            var r = Say("clean up the desk");

            Assert.AreEqual(ResponseKey.NothingToOrganise, r.Key);
            Assert.IsFalse(r.DeskChanged);
            Say("undo");
            Assert.IsTrue(_desk.Get(a.Id).LocalPose.Position.ApproxEquals(new Vec3(0.33f, 0, 0.41f)));
        }

        [Test]
        public void Group_unknown_tag_finds_nothing_and_moves_nothing()
        {
            Make.Note(_desk, "a", 0.33f, 0.41f);
            var r = Say("group research notes");
            Assert.AreEqual(Outcome.NothingFound, r.Outcome);
            Assert.IsFalse(_agent.CanUndo);
        }

        [Test]
        public void Group_known_tag_organises()
        {
            Make.Note(_desk, "paper", 0.33f, 0.41f, tags: new[] { "research" });
            var r = Say("group research notes");
            Assert.AreEqual(ResponseKey.Grouped, r.Key);
            Assert.AreEqual("research", r.Argument);
        }

        [Test]
        public void Undo_after_organise_restores_layout()
        {
            var a = Make.Note(_desk, "a", 0.33f, 0.41f);
            Say("clean up the desk");
            var r = Say("undo");

            Assert.AreEqual(ResponseKey.Undone, r.Key);
            Assert.IsTrue(_desk.Get(a.Id).LocalPose.Position.ApproxEquals(new Vec3(0.33f, 0, 0.41f)));
        }

        // ---------- find / status / important ----------

        [Test]
        public void Find_returns_matching_items_best_first()
        {
            Make.Note(_desk, "Groceries");
            var pricing = _desk.Add(ItemType.Panel, "Tier A 499, Tier B 999", Pose.Default, "Pricing");

            var r = Say("where did I put the pricing notes");

            Assert.AreEqual(ResponseKey.Found, r.Key);
            Assert.AreEqual(pricing.Id, r.ItemIds[0]);
            Assert.AreEqual(1, r.ItemIds.Count);
        }

        [Test]
        public void Find_never_invents_a_result()
        {
            Make.Note(_desk, "Groceries");
            var r = Say("where is the contract");
            Assert.AreEqual(Outcome.NothingFound, r.Outcome);
            Assert.AreEqual(0, r.ItemIds.Count);
        }

        [Test]
        public void Find_with_only_filler_words_asks_what_to_find()
        {
            Assert.AreEqual(ResponseKey.WhatToFind, Say("where is my note").Key);
        }

        [Test]
        public void Status_counts_items()
        {
            Make.Note(_desk, "a", priority: Priority.High);
            Make.Note(_desk, "b", locked: true);

            var r = Say("give me a status");

            Assert.AreEqual(ResponseKey.Status, r.Key);
            Assert.AreEqual(2, r.Status.Total);
            Assert.AreEqual(1, r.Status.Important);
            Assert.AreEqual(1, r.Status.Locked);
        }

        [Test]
        public void Status_on_empty_desk()
        {
            Assert.AreEqual(ResponseKey.StatusEmpty, Say("overview").Key);
        }

        [Test]
        public void Important_lists_high_priority_items()
        {
            var hi = Make.Note(_desk, "ship build", priority: Priority.High);
            Make.Note(_desk, "lunch");

            var r = Say("what's important today");

            Assert.AreEqual(ResponseKey.ImportantList, r.Key);
            CollectionAssertEqual(new[] { hi.Id }, r.ItemIds.ToArray());
        }

        [Test]
        public void Important_when_none_marked()
        {
            Make.Note(_desk, "lunch");
            Assert.AreEqual(ResponseKey.NothingImportant, Say("what's important").Key);
        }

        // ---------- focus ----------

        [Test]
        public void Focus_hides_normal_items_but_keeps_important_and_locked()
        {
            var hi = Make.Note(_desk, "hi", priority: Priority.High);
            var lo = Make.Note(_desk, "lo");
            var locked = Make.Note(_desk, "pinned", locked: true);

            var r = Say("focus mode");

            Assert.AreEqual(ResponseKey.FocusOn, r.Key);
            Assert.IsFalse(hi.HiddenByFocus);
            Assert.IsTrue(lo.HiddenByFocus);
            Assert.IsFalse(locked.HiddenByFocus);
            Assert.IsTrue(_desk.FocusActive);
        }

        [Test]
        public void Focus_without_important_items_asks_user_to_mark_one()
        {
            Make.Note(_desk, "lo");
            var r = Say("focus mode");
            Assert.AreEqual(ResponseKey.FocusNeedsImportant, r.Key);
            Assert.IsFalse(_desk.FocusActive);
        }

        [Test]
        public void Exit_focus_shows_everything_and_undo_rehides()
        {
            Make.Note(_desk, "hi", priority: Priority.High);
            var lo = Make.Note(_desk, "lo");
            Say("focus mode");

            Assert.AreEqual(ResponseKey.FocusOff, Say("exit focus").Key);
            Assert.IsFalse(lo.HiddenByFocus);

            Say("undo");
            Assert.IsTrue(_desk.Get(lo.Id).HiddenByFocus);
        }

        [Test]
        public void Exit_focus_when_not_focused()
        {
            Assert.AreEqual(ResponseKey.NotInFocus, Say("exit focus").Key);
        }

        // ---------- targeted commands ----------

        [Test]
        public void Targeted_commands_without_target_ask_which_item()
        {
            Make.Note(_desk, "a");
            foreach (var u in new[] { "lock this", "unlock this", "mark this important", "tag this as work", "delete this" })
                Assert.AreEqual(ResponseKey.WhichItem, Say(u).Key, u);
        }

        [Test]
        public void Lock_mark_and_tag_the_pointed_item()
        {
            var a = Make.Note(_desk, "a");

            Say("lock this", a.Id);
            Say("mark this important", a.Id);
            var r = Say("tag this as Research", a.Id);

            Assert.IsTrue(a.Locked);
            Assert.AreEqual(Priority.High, a.Priority);
            Assert.AreEqual("research", a.PrimaryTag);
            Assert.AreEqual("research", r.Argument);
        }

        [Test]
        public void Unknown_target_is_nothing_found()
        {
            Assert.AreEqual(Outcome.NothingFound, Say("lock this", "ghost").Outcome);
        }

        // ---------- delete ----------

        [Test]
        public void Delete_asks_for_confirmation_first()
        {
            var a = Make.Note(_desk, "a");

            var r = Say("delete this", a.Id);

            Assert.AreEqual(Outcome.NeedsConfirmation, r.Outcome);
            Assert.IsNotNull(_desk.Get(a.Id));
            Assert.IsTrue(_agent.AwaitingConfirmation);
        }

        [Test]
        public void Delete_confirmed_removes_and_undo_restores()
        {
            var a = Make.Note(_desk, "keep me");
            Say("delete this", a.Id);

            Assert.AreEqual(ResponseKey.Deleted, Say("yes").Key);
            Assert.IsNull(_desk.Get(a.Id));

            Say("undo");
            Assert.AreEqual("keep me", _desk.Get(a.Id).Content);
        }

        [Test]
        public void Delete_denied_keeps_item()
        {
            var a = Make.Note(_desk, "a");
            Say("delete this", a.Id);

            Assert.AreEqual(ResponseKey.DeleteCancelled, Say("no").Key);
            Assert.IsNotNull(_desk.Get(a.Id));
            Assert.IsFalse(_agent.AwaitingConfirmation);
        }

        [Test]
        public void Another_command_drops_pending_delete()
        {
            var a = Make.Note(_desk, "a");
            Say("delete this", a.Id);

            Assert.AreEqual(ResponseKey.Status, Say("status").Key);
            Assert.AreEqual(ResponseKey.NothingPending, Say("yes").Key);
            Assert.IsNotNull(_desk.Get(a.Id));
        }

        [Test]
        public void Locked_item_cannot_be_deleted()
        {
            var a = Make.Note(_desk, "a", locked: true);
            var r = Say("delete this", a.Id);
            Assert.AreEqual(Outcome.Refused, r.Outcome);
            Assert.AreEqual(ResponseKey.ItemIsLocked, r.Key);
        }

        // ---------- misc ----------

        [Test]
        public void Undo_with_nothing_to_undo()
        {
            Assert.AreEqual(ResponseKey.NothingToUndo, Say("undo").Key);
        }

        [Test]
        public void Switch_personality_preserves_desk()
        {
            var a = Make.Note(_desk, "a", 0.33f, 0.41f);
            var r = Say("switch to pip");

            Assert.AreEqual(Personality.Pip, _agent.Personality);
            Assert.AreEqual(ResponseKey.Switched, r.Key);
            Assert.IsFalse(r.DeskChanged);
            Assert.IsTrue(a.LocalPose.Position.ApproxEquals(new Vec3(0.33f, 0, 0.41f)));
        }

        [Test]
        public void Partial_command_asks_for_clarification()
        {
            Assert.AreEqual(Outcome.NeedsClarification, Say("where is").Outcome);
        }

        [Test]
        public void Small_talk_is_not_understood_and_changes_nothing()
        {
            Make.Note(_desk, "a");
            var r = Say("what a lovely day");
            Assert.AreEqual(Outcome.NotUnderstood, r.Outcome);
            Assert.IsFalse(r.DeskChanged);
        }

        [Test]
        public void Manual_pose_updates_timestamp_and_is_not_undoable()
        {
            var a = Make.Note(_desk, "a");
            _clock.Advance(100);
            _agent.ApplyManualPose(a.Id, Pose.At(new Vec3(0.2f, 0, 0.2f)));

            Assert.AreEqual(_clock.NowMs, a.UpdatedAtMs);
            Assert.IsFalse(_agent.CanUndo);
        }

        static void CollectionAssertEqual(string[] expected, string[] actual)
        {
            Assert.AreEqual(string.Join(",", expected), string.Join(",", actual));
        }
    }
}
