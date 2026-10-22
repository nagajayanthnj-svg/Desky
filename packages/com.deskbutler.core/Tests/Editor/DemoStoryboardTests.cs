using System.Collections.Generic;
using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    /// <summary>
    /// Outer-loop acceptance test: the judged demo video (PRD section 6) played through the core.
    /// If this is green, every storyboard beat is supported by logic; what's left is Unity wiring.
    /// </summary>
    [TestFixture]
    public class DemoStoryboardTests
    {
        [Test]
        public void Full_demo_run()
        {
            var clock = new FakeClock();
            var store = new InMemoryDeskStore();

            // 0:00 Hook: first run, butler floats, greets, jumps down
            var desk = Desk.LoadOrCreate(store, clock, new SequentialIds());
            var onboarding = new OnboardingFlow(desk.HasAnchor);
            Assert.AreEqual(OnboardingCue.SpawnFloatingAndGreet, onboarding.Begin());
            onboarding.OnGreetingFinished();
            Assert.AreEqual(OnboardingCue.PlayJumpDown, onboarding.OnDeskDetected());
            desk.AnchorId = "desk-anchor-1";
            Assert.AreEqual(OnboardingCue.InviteFirstNote, onboarding.OnJumpFinished());

            var agent = new DeskAgent(desk, Personality.Alden);

            // 0:25 First note by dictation, with one correction
            Assert.AreEqual(Outcome.StartCapture, agent.Handle(CommandParser.Parse("New note")).Outcome);
            var capture = new CaptureSession();
            capture.Start();
            capture.OnPartial("Finalise pricing");
            capture.OnFinal("Finalise pricing for Q3 launch");
            capture.OnReviewUtterance("Change that to Finalise pricing for Q4 launch");
            Assert.AreEqual(CapturePrompt.Saved, capture.OnReviewUtterance("yes"));
            var first = agent.PinNote(capture.Text, Pose.At(new Vec3(0.31f, 0, 0.42f)));
            var pricing = desk.Get(first.ItemIds[0]);
            Assert.AreEqual("Finalise pricing for Q4 launch", pricing.Content);

            // a few more notes, scattered
            var research = desk.Get(agent.PinNote("Competitor teardown", Pose.At(new Vec3(-0.2f, 0, 0.5f))).ItemIds[0]);
            var lunch = desk.Get(agent.PinNote("Order lunch", Pose.At(new Vec3(0.1f, 0, 0.12f))).ItemIds[0]);
            agent.Handle(CommandParser.Parse("tag this as research"), research.Id);
            agent.Handle(CommandParser.Parse("mark this important"), pricing.Id);

            // 0:50 Agentic organise, then undo works
            var organised = agent.Handle(CommandParser.Parse("Alden, clean up the desk"));
            Assert.AreEqual(ResponseKey.Organised, organised.Key);
            Assert.AreEqual("The desk is in order. 3 items set straight.", ResponseCatalog.Render(agent.Personality, organised));
            Assert.AreEqual(ResponseKey.Undone, agent.Handle(CommandParser.Parse("undo")).Key);
            agent.Handle(CommandParser.Parse("clean up the desk"));

            // 1:15 Personality contrast: Pip's idle mess is cosmetic, Alden tidies it
            agent.Handle(CommandParser.Parse("switch to Pip"));
            var offsets = IdleBehaviour.PipNudge(desk.Items, new Dictionary<string, VisualOffset>(), new System.Random(1));
            Assert.AreEqual(1, offsets.Count);
            agent.Handle(CommandParser.Parse("switch to Alden"));
            Assert.AreEqual(0, IdleBehaviour.AldenStraighten().Count);

            // 1:45 Focus mode + persistence across restart
            Assert.AreEqual(ResponseKey.FocusOn, agent.Handle(CommandParser.Parse("focus mode")).Key);
            Assert.IsTrue(lunch.HiddenByFocus);
            store.Save(desk.ToSnapshot());

            var restarted = Desk.LoadOrCreate(store, clock, new SequentialIds());
            var flow2 = new OnboardingFlow(restarted.HasAnchor);
            Assert.AreEqual(OnboardingCue.RestoreDesk, flow2.Begin());
            Assert.AreEqual(OnboardingCue.WelcomeBack, flow2.OnDeskRestored(true));
            Assert.AreEqual(3, restarted.Items.Count);
            Assert.IsTrue(restarted.Get(lunch.Id).HiddenByFocus);

            var agent2 = new DeskAgent(restarted);

            // 2:15 Keyboard path
            var kb = new CaptureSession();
            kb.OpenKeyboard();
            Assert.AreEqual(CapturePrompt.Saved, kb.OnKeyboardSubmit("Quiet-room note"));
            agent2.PinNote(kb.Text, Pose.At(new Vec3(0.2f, 0, 0.2f)));

            // 2:30 Closing status
            var status = agent2.Handle(CommandParser.Parse("give me a quick overview"));
            Assert.AreEqual(4, status.Status.Total);
            Assert.AreEqual(1, status.Status.Important);
            Assert.AreEqual(ResponseKey.Found, agent2.Handle(CommandParser.Parse("where did I put the pricing notes")).Key);
        }
    }
}
