namespace DeskButler.Core
{
    public enum CaptureState { Idle, Listening, Reviewing, Typing, Completed, Cancelled }

    /// <summary>What the UI/butler should do after a capture event.</summary>
    public enum CapturePrompt
    {
        None,
        Listening,        // show mic indicator, butler says "listening"
        ShowDraft,        // show live/final text and ask to confirm
        ConfirmHint,      // "say yes to pin it, or tell me the correction"
        NoSpeech,         // "I didn't catch that. Try again or use the keyboard."
        KeyboardOpened,   // show the hand keyboard
        Saved,            // note text is final -> DeskAgent.PinNote(Text)
        Cancelled
    }

    /// <summary>
    /// Note-capture state machine (PRD 5.6). Voice and keyboard both end in the same Saved prompt.
    /// It only ever stores text the user dictated or typed; it never fills gaps (PRD 10).
    /// The app forwards STT events, review utterances, gestures, and frame time (Tick).
    /// </summary>
    public sealed class CaptureSession
    {
        public const float SilenceTimeoutSec = 4.5f;
        public const float MaxListenSec = 30f;

        float _silence, _listening;

        public CaptureState State { get; private set; }
        public string Text { get; private set; }

        public bool IsActive
        {
            get { return State == CaptureState.Listening || State == CaptureState.Reviewing || State == CaptureState.Typing; }
        }

        public CapturePrompt Start(string inlineText = null)
        {
            Text = null;
            if (!string.IsNullOrWhiteSpace(inlineText))
            {
                Text = inlineText.Trim();
                State = CaptureState.Reviewing;
                return CapturePrompt.ShowDraft;
            }
            return BeginListening();
        }

        public CapturePrompt OnPartial(string text)
        {
            if (State != CaptureState.Listening) return CapturePrompt.None;
            if (!string.IsNullOrWhiteSpace(text)) { Text = text.Trim(); _silence = 0; }
            return CapturePrompt.ShowDraft;
        }

        public CapturePrompt OnFinal(string text)
        {
            if (State != CaptureState.Listening) return CapturePrompt.None;
            if (string.IsNullOrWhiteSpace(text)) return NoSpeech();
            Text = text.Trim();
            State = CaptureState.Reviewing;
            return CapturePrompt.ShowDraft;
        }

        public CapturePrompt Tick(float deltaSec)
        {
            if (State != CaptureState.Listening) return CapturePrompt.None;
            _silence += deltaSec;
            _listening += deltaSec;

            var heardNothing = string.IsNullOrWhiteSpace(Text);
            if (heardNothing && _silence >= SilenceTimeoutSec) return NoSpeech();
            if (_listening >= MaxListenSec)
            {
                if (heardNothing) return NoSpeech();
                State = CaptureState.Reviewing;   // timeout with partial text -> let the user confirm it
                return CapturePrompt.ShowDraft;
            }
            return CapturePrompt.None;
        }

        /// <summary>Speech heard while the draft is shown.</summary>
        public CapturePrompt OnReviewUtterance(string utterance)
        {
            if (State != CaptureState.Reviewing) return CapturePrompt.None;
            var cmd = CommandParser.Parse(utterance);
            switch (cmd.Kind)
            {
                case CommandKind.Confirm: return Complete();
                case CommandKind.Correction:
                    Text = cmd.Argument;
                    return CapturePrompt.ShowDraft;   // re-confirm the corrected text
                case CommandKind.Deny: return BeginListening();   // bare "no" -> say it again
                case CommandKind.Cancel: return Cancel();
                default: return CapturePrompt.ConfirmHint;
            }
        }

        public CapturePrompt OnConfirmGesture()
        {
            return State == CaptureState.Reviewing ? Complete() : CapturePrompt.None;
        }

        public CapturePrompt OnCancelGesture()
        {
            return IsActive ? Cancel() : CapturePrompt.None;
        }

        /// <summary>Open the hand keyboard: user choice, mic failure, or after NoSpeech.</summary>
        public CapturePrompt OpenKeyboard()
        {
            State = CaptureState.Typing;
            return CapturePrompt.KeyboardOpened;
        }

        public CapturePrompt OnMicUnavailable()
        {
            return OpenKeyboard();
        }

        /// <summary>Keyboard "done" is an explicit confirmation, so it saves directly.</summary>
        public CapturePrompt OnKeyboardSubmit(string text)
        {
            if (State == CaptureState.Completed || State == CaptureState.Cancelled) return CapturePrompt.None;
            if (string.IsNullOrWhiteSpace(text)) return CapturePrompt.None;
            Text = text.Trim();
            return Complete();
        }

        CapturePrompt BeginListening()
        {
            Text = null;
            _silence = 0;
            _listening = 0;
            State = CaptureState.Listening;
            return CapturePrompt.Listening;
        }

        CapturePrompt NoSpeech()
        {
            Text = null;
            State = CaptureState.Idle;
            return CapturePrompt.NoSpeech;
        }

        CapturePrompt Complete()
        {
            if (string.IsNullOrWhiteSpace(Text)) return NoSpeech();
            State = CaptureState.Completed;
            return CapturePrompt.Saved;
        }

        CapturePrompt Cancel()
        {
            Text = null;
            State = CaptureState.Cancelled;
            return CapturePrompt.Cancelled;
        }
    }
}
