namespace DeskButler.Core
{
    public enum OnboardingState { NotStarted, Greeting, AwaitingDesk, ManualPlacement, JumpingDown, Restoring, Ready }

    public enum OnboardingCue
    {
        None,
        SpawnFloatingAndGreet,   // butler appears in front of the face, personality greeting
        PromptLookAtDesk,
        PromptManualPlacement,   // "Pinch where your desk is" -> user defines the plane by hand
        PlayJumpDown,
        InviteFirstNote,
        RestoreDesk,             // returning user: resolve the saved desk anchor
        WelcomeBack,
        PromptDeskMoved          // saved anchor failed to resolve -> back to desk search
    }

    /// <summary>
    /// The 60-second hook (PRD 5.5) plus the no-desk fallback (D-011) and returning-user path.
    /// Pure state machine; Unity drives animations and plane detection from the cues.
    /// </summary>
    public sealed class OnboardingFlow
    {
        public const float DeskSearchTimeoutSec = 10f;

        readonly bool _hasSavedDesk;
        float _searching;

        public OnboardingState State { get; private set; }

        public OnboardingFlow(bool hasSavedDesk) { _hasSavedDesk = hasSavedDesk; }

        public OnboardingCue Begin()
        {
            if (_hasSavedDesk)
            {
                State = OnboardingState.Restoring;
                return OnboardingCue.RestoreDesk;
            }
            State = OnboardingState.Greeting;
            return OnboardingCue.SpawnFloatingAndGreet;
        }

        public OnboardingCue OnGreetingFinished()
        {
            if (State != OnboardingState.Greeting) return OnboardingCue.None;
            return StartDeskSearch(OnboardingCue.PromptLookAtDesk);
        }

        public OnboardingCue Tick(float deltaSec)
        {
            if (State != OnboardingState.AwaitingDesk) return OnboardingCue.None;
            _searching += deltaSec;
            if (_searching < DeskSearchTimeoutSec) return OnboardingCue.None;
            State = OnboardingState.ManualPlacement;
            return OnboardingCue.PromptManualPlacement;
        }

        /// <summary>Plane detection found a suitable horizontal surface. Still accepted during manual placement.</summary>
        public OnboardingCue OnDeskDetected()
        {
            if (State != OnboardingState.AwaitingDesk && State != OnboardingState.ManualPlacement) return OnboardingCue.None;
            State = OnboardingState.JumpingDown;
            return OnboardingCue.PlayJumpDown;
        }

        public OnboardingCue OnManualPlaneConfirmed()
        {
            if (State != OnboardingState.ManualPlacement) return OnboardingCue.None;
            State = OnboardingState.JumpingDown;
            return OnboardingCue.PlayJumpDown;
        }

        public OnboardingCue OnJumpFinished()
        {
            if (State != OnboardingState.JumpingDown) return OnboardingCue.None;
            State = OnboardingState.Ready;
            return OnboardingCue.InviteFirstNote;
        }

        public OnboardingCue OnDeskRestored(bool resolved)
        {
            if (State != OnboardingState.Restoring) return OnboardingCue.None;
            if (resolved)
            {
                State = OnboardingState.Ready;
                return OnboardingCue.WelcomeBack;
            }
            return StartDeskSearch(OnboardingCue.PromptDeskMoved);
        }

        OnboardingCue StartDeskSearch(OnboardingCue cue)
        {
            _searching = 0;
            State = OnboardingState.AwaitingDesk;
            return cue;
        }
    }
}
