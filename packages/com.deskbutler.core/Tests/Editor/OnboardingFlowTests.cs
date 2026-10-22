using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class OnboardingFlowTests
    {
        [Test]
        public void First_run_hook_greet_look_detect_jump_invite()
        {
            var f = new OnboardingFlow(hasSavedDesk: false);

            Assert.AreEqual(OnboardingCue.SpawnFloatingAndGreet, f.Begin());
            Assert.AreEqual(OnboardingCue.PromptLookAtDesk, f.OnGreetingFinished());
            Assert.AreEqual(OnboardingCue.None, f.Tick(3f));
            Assert.AreEqual(OnboardingCue.PlayJumpDown, f.OnDeskDetected());
            Assert.AreEqual(OnboardingCue.InviteFirstNote, f.OnJumpFinished());
            Assert.AreEqual(OnboardingState.Ready, f.State);
        }

        [Test]
        public void Desk_detection_before_greeting_ends_is_ignored()
        {
            var f = new OnboardingFlow(false);
            f.Begin();
            Assert.AreEqual(OnboardingCue.None, f.OnDeskDetected());
            Assert.AreEqual(OnboardingState.Greeting, f.State);
        }

        [Test]
        public void No_desk_after_timeout_falls_back_to_manual_placement()
        {
            var f = new OnboardingFlow(false);
            f.Begin();
            f.OnGreetingFinished();

            Assert.AreEqual(OnboardingCue.None, f.Tick(9.9f));
            Assert.AreEqual(OnboardingCue.PromptManualPlacement, f.Tick(0.2f));
            Assert.AreEqual(OnboardingCue.PlayJumpDown, f.OnManualPlaneConfirmed());
            Assert.AreEqual(OnboardingCue.InviteFirstNote, f.OnJumpFinished());
        }

        [Test]
        public void Late_detection_during_manual_placement_still_works()
        {
            var f = new OnboardingFlow(false);
            f.Begin();
            f.OnGreetingFinished();
            f.Tick(11f);
            Assert.AreEqual(OnboardingCue.PlayJumpDown, f.OnDeskDetected());
        }

        [Test]
        public void Returning_user_restores_and_skips_greeting()
        {
            var f = new OnboardingFlow(hasSavedDesk: true);
            Assert.AreEqual(OnboardingCue.RestoreDesk, f.Begin());
            Assert.AreEqual(OnboardingCue.WelcomeBack, f.OnDeskRestored(true));
            Assert.AreEqual(OnboardingState.Ready, f.State);
        }

        [Test]
        public void Returning_user_with_lost_anchor_searches_again()
        {
            var f = new OnboardingFlow(true);
            f.Begin();
            Assert.AreEqual(OnboardingCue.PromptDeskMoved, f.OnDeskRestored(false));
            Assert.AreEqual(OnboardingState.AwaitingDesk, f.State);
            Assert.AreEqual(OnboardingCue.PromptManualPlacement, f.Tick(10f));
        }
    }
}
