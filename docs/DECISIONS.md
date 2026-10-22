# Decision Log

Every choice that changes scope, architecture, behaviour or workflow gets an entry. It's short on purpose, so writing one takes two minutes.

**When to add one:** you picked between real options, changed the PRD, or a reviewer could ask "why?".
**How:** next number, append below, link it from the commit (`decision: D-0xx`). Never edit an Accepted entry's decision. Supersede it with a new entry and set the old one's status to `Superseded by D-0yy`.

**Statuses:** `Proposed` (needs a spike or a date) · `Accepted` · `Superseded by …` · `Rejected`

| ID | Title | Status | Date |
|---|---|---|---|
| D-001 | Pure C# core package + thin Unity adapters | Accepted | 2026-10-22 |
| D-002 | One spatial anchor per desk, items as desk-local poses | Accepted | 2026-10-22 |
| D-003 | Data model adds tags, priority, hiddenByFocus | Accepted | 2026-10-22 |
| D-004 | Deterministic grammar decides actions; LLM only re-words | Accepted | 2026-10-22 |
| D-005 | Diff-based one-level undo with full item state | Accepted | 2026-10-22 |
| D-006 | Idle behaviour is cosmetic only | Accepted | 2026-10-22 |
| D-007 | Explicit organise = consent; confirm only deletes | Accepted | 2026-10-22 |
| D-008 | Focus Mode hides normal-priority unlocked items | Accepted | 2026-10-22 |
| D-009 | Voice stack: Meta Voice SDK behind ports | Proposed (spike S-1) | 2026-10-22 |
| D-010 | Quest 3/3S primary, Quest 2 best-effort | Accepted | 2026-10-22 |
| D-011 | No-desk fallback: 10 s → manual placement | Accepted | 2026-10-22 |
| D-012 | Image panels from a local folder; no camera capture | Accepted | 2026-10-22 |
| D-013 | NUnit 3 classic asserts, C# ≤ 9, no Unity types in core | Accepted | 2026-10-22 |
| D-014 | Minimal hand keyboard promoted to Must Have | Accepted | 2026-10-22 |
| D-015 | Character visual style | Proposed (decide by 28 Oct) | 2026-10-22 |
| D-016 | Trunk-based, task-ID commits, TDD Definition of Done | Accepted | 2026-10-22 |

---

### D-001 · Pure C# core package + thin Unity adapters
- **Context:** Hand tracking, anchors and voice only run on device, which makes TDD slow if logic lives in MonoBehaviours.
- **Decision:** All rules live in `packages/com.deskbutler.core`: model, grammar, agent, undo, capture, onboarding, idle and responses. Its asmdef has `noEngineReferences: true`. The world is reached through ports (`IClock`, `IDeskStore`, `ISpeechToText`, `ITextToSpeech`, `IFlavourModel`). Unity code only translates: input → core call, core result → visuals/audio.
- **Consequences:** Core tests run in seconds via `dotnet test` and in CI. Adapters must stay thin; if an `if` about product rules appears in a MonoBehaviour, move it to core.

### D-002 · One spatial anchor per desk
- **Context:** v1.4 had one persistent anchor per item. That costs more to save and load, and items drift apart independently.
- **Decision:** One anchor at the desk origin. Items store `LocalPose` relative to it. The schema keeps `DeskAnchorId` once.
- **Consequences:** Re-anchoring moves the whole desk coherently. A single anchor failure means all items need re-placement, which onboarding handles with "desk moved".

### D-003 · Data model additions
- **Decision:** Add `Id` (GUID), `Tags` (first = grouping tag), `Priority` (Normal/High), `HiddenByFocus`, and timestamps in unix ms. Public fields, `[Serializable]`, ms timestamps: all JsonUtility-friendly.
- **Consequences:** "What's important", "Group X" and Focus Mode now have real data behind them. Schema version 1; snapshots from newer versions are rejected.

### D-004 · Grammar decides, LLM only re-words
- **Context:** Judges reward agentic behaviour, and the PRD says never invent. An LLM choosing actions is non-deterministic and untestable.
- **Decision:** `CommandParser` (ordered regex rules) maps speech to a command. `DeskAgent` executes it and `ResponseCatalog` picks the line. `IFlavourModel.Rephrase` may re-word the line within 700 ms or it's ignored. Unrecognised speech gets `NotUnderstood`; the LLM may answer small talk only.
- **Consequences:** Every behaviour is unit-testable, and new phrases are added test-first. The LLM is a nice-to-have that can be cut without breaking anything.

### D-005 · Diff-based one-level undo
- **Context:** v1.4 snapshotted only transforms + lock, so delete and focus weren't reversible.
- **Decision:** `UndoBuffer.Begin` clones the desk and `Commit` keeps only the items that changed, as full prior state, plus created ids. Undo writes state back into the **same object instances**. No-op commands don't overwrite the buffer.
- **Consequences:** Delete, focus, tag and pin are all reversible. Manual edits to untouched items survive undo. Views can hold item references. (This bug was caught by `DemoStoryboardTests` on day one; see F-003.)

### D-006 · Idle behaviour is cosmetic
- **Context:** Alden straightening and Pip nudging items moved things without a command, which conflicts with the agent rules and lock.
- **Decision:** `IdleBehaviour` returns render-only `VisualOffset`s. They are never written to the desk, never applied to locked or hidden items, and dropped on restart. Pip: ≤ 1.5 cm, ≤ 5°. Alden: clears offsets.
- **Consequences:** Personality stays charming, data stays trustworthy, and the lock means what it says.

### D-007 · Organise needs no confirmation; delete does
- **Context:** The PRD said "confirm major rearrangements". A confirmation after "clean up the desk" adds friction to the 0:50 demo beat.
- **Decision:** An explicit organise command is consent, and undo is the safety net. Only delete asks "are you sure?".
- **Consequences:** Faster flow. Revisit if playtesters are surprised by organise.

### D-008 · Focus Mode rule
- **Decision:** Hide items with `Priority = Normal` that are not locked. If nothing is High, ask the user to mark something first. Exit restores all. Both are undoable.
- **Consequences:** Focus depends on priority marking, so both are Must Have together.

### D-009 · Voice stack (Proposed)
- **Candidate:** Meta Voice SDK dictation for speech-to-text and its text-to-speech, with two voices.
- **Validate in spike S-1 (by 26 Oct):** live partial transcripts on Quest 3; time to first partial; that two distinct voices exist; offline behaviour; any account or keys needed.
- **Fallbacks:** Android `SpeechRecognizer` for speech-to-text; a cloud text-to-speech service; pre-recorded catalog lines (`ResponseCatalog` is finite).
- **Exit:** Accept or replace by 26 Oct; record the result here.

### D-010 · Devices
- **Decision:** Optimise and test on Quest 3/3S. Quest 2 gets a smoke test only. Its grayscale, low-resolution passthrough undercuts the hook, and 72 FPS is at risk.

### D-011 · No-desk fallback
- **Decision:** 10 s with no surface → "Pinch where your desk is", and the user pinches two corners at desk height. Automatic detection is still accepted during manual placement.

### D-012 · Image source
- **Decision:** Hand-friendly picker over a local folder (`Pictures/DeskButler/`). Camera capture is out of scope, which avoids permission flows and API risk.

### D-013 · Test framework and language level
- **Decision:** NUnit 3.x with classic `Assert.*` (the Unity Test Framework ships NUnit 3; NUnit 4 moves the classic asserts). Core code uses C# ≤ 9 features and no `UnityEngine` types (own `Vec3`/`Quat`/`Pose`).

### D-014 · Hand keyboard is Must Have
- **Context:** The voice error copy says "use the keyboard". It is also needed for deaf/hard-of-hearing users, quiet rooms and mic denial, and the accessibility award depends on it.
- **Decision:** A minimal hand keyboard (letters, space, backspace, done) is Must Have. "Done" saves directly.

### D-015 · Character visual style (Proposed)
- **Options:** stylised low-poly (cheap, readable at 15 cm, fast to animate) vs semi-realistic (higher risk).
- **Leaning:** stylised. Decide by 28 Oct, after S-3 (character rig test).

### D-016 · Workflow
- **Decision:** Trunk-based development with short `task/DB-xxx-slug` branches (≤ 1 day), Conventional Commits with the task ID, and red-green-refactor TDD for core work. The Definition of Done is in `DEVELOPMENT_PLAN.md` §3.
