using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class CaptureSessionTests
    {
        CaptureSession _s;

        [SetUp]
        public void SetUp() { _s = new CaptureSession(); }

        [Test]
        public void Happy_path_dictate_then_confirm()
        {
            Assert.AreEqual(CapturePrompt.Listening, _s.Start());
            Assert.AreEqual(CapturePrompt.ShowDraft, _s.OnPartial("Buy oat"));
            Assert.AreEqual("Buy oat", _s.Text);
            Assert.AreEqual(CapturePrompt.ShowDraft, _s.OnFinal("Buy oat milk"));
            Assert.AreEqual(CaptureState.Reviewing, _s.State);
            Assert.AreEqual(CapturePrompt.Saved, _s.OnReviewUtterance("yes"));
            Assert.AreEqual("Buy oat milk", _s.Text);
            Assert.AreEqual(CaptureState.Completed, _s.State);
        }

        [Test]
        public void Inline_text_goes_straight_to_review()
        {
            Assert.AreEqual(CapturePrompt.ShowDraft, _s.Start("call the vendor"));
            Assert.AreEqual(CaptureState.Reviewing, _s.State);
        }

        [Test]
        public void Correction_replaces_text_and_reconfirms()
        {
            _s.Start();
            _s.OnFinal("Buy goat milk");

            Assert.AreEqual(CapturePrompt.ShowDraft, _s.OnReviewUtterance("No, I said buy oat milk"));
            Assert.AreEqual("buy oat milk", _s.Text);
            Assert.AreEqual(CaptureState.Reviewing, _s.State);

            Assert.AreEqual(CapturePrompt.ShowDraft, _s.OnReviewUtterance("change that to Buy Oat Milk"));
            Assert.AreEqual("Buy Oat Milk", _s.Text);
            Assert.AreEqual(CapturePrompt.Saved, _s.OnConfirmGesture());
        }

        [Test]
        public void Bare_no_restarts_listening()
        {
            _s.Start();
            _s.OnFinal("wrong");
            Assert.AreEqual(CapturePrompt.Listening, _s.OnReviewUtterance("no"));
            Assert.IsNull(_s.Text);
        }

        [Test]
        public void Unclear_review_reply_gives_hint_and_keeps_draft()
        {
            _s.Start();
            _s.OnFinal("draft");
            Assert.AreEqual(CapturePrompt.ConfirmHint, _s.OnReviewUtterance("hmm"));
            Assert.AreEqual("draft", _s.Text);
        }

        [Test]
        public void Silence_times_out_with_no_speech_prompt()
        {
            _s.Start();
            Assert.AreEqual(CapturePrompt.None, _s.Tick(4.0f));
            Assert.AreEqual(CapturePrompt.NoSpeech, _s.Tick(0.6f));
            Assert.AreEqual(CaptureState.Idle, _s.State);
            Assert.IsNull(_s.Text);
        }

        [Test]
        public void Speech_resets_silence_timer()
        {
            _s.Start();
            _s.Tick(4.0f);
            _s.OnPartial("hello");
            Assert.AreEqual(CapturePrompt.None, _s.Tick(4.0f));
            Assert.AreEqual(CaptureState.Listening, _s.State);
        }

        [Test]
        public void Max_listen_timeout_with_text_moves_to_review()
        {
            _s.Start();
            _s.OnPartial("long thought");
            for (var i = 0; i < 31; i++) { _s.OnPartial("long thought"); _s.Tick(1f); }
            Assert.AreEqual(CaptureState.Reviewing, _s.State);
            Assert.AreEqual("long thought", _s.Text);
        }

        [Test]
        public void Empty_final_transcript_is_no_speech_never_invented_text()
        {
            _s.Start();
            Assert.AreEqual(CapturePrompt.NoSpeech, _s.OnFinal("   "));
            Assert.IsNull(_s.Text);
        }

        [TestCase("cancel")]
        [TestCase("never mind")]
        public void Cancel_by_voice(string utterance)
        {
            _s.Start();
            _s.OnFinal("x");
            Assert.AreEqual(CapturePrompt.Cancelled, _s.OnReviewUtterance(utterance));
            Assert.AreEqual(CaptureState.Cancelled, _s.State);
        }

        [Test]
        public void Cancel_by_open_hand_gesture_while_listening()
        {
            _s.Start();
            Assert.AreEqual(CapturePrompt.Cancelled, _s.OnCancelGesture());
        }

        [Test]
        public void Mic_unavailable_opens_keyboard_and_submit_saves()
        {
            _s.Start();
            Assert.AreEqual(CapturePrompt.KeyboardOpened, _s.OnMicUnavailable());
            Assert.AreEqual(CaptureState.Typing, _s.State);
            Assert.AreEqual(CapturePrompt.Saved, _s.OnKeyboardSubmit("Typed note"));
            Assert.AreEqual("Typed note", _s.Text);
        }

        [Test]
        public void Keyboard_after_no_speech()
        {
            _s.Start();
            _s.Tick(5f);
            _s.OpenKeyboard();
            Assert.AreEqual(CapturePrompt.Saved, _s.OnKeyboardSubmit("quiet room note"));
        }

        [Test]
        public void Empty_keyboard_submit_is_ignored()
        {
            _s.OpenKeyboard();
            Assert.AreEqual(CapturePrompt.None, _s.OnKeyboardSubmit(""));
            Assert.AreEqual(CaptureState.Typing, _s.State);
        }

        [Test]
        public void Events_after_completion_are_ignored()
        {
            _s.Start("done");
            _s.OnConfirmGesture();
            Assert.AreEqual(CapturePrompt.None, _s.OnFinal("late transcript"));
            Assert.AreEqual("done", _s.Text);
        }
    }
}
