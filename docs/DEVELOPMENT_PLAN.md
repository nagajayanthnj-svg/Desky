# Desk Butler – Development Plan

**Scope:** PRD v1.6 Must Haves · **Deadline:** submit by **Tue 17 Nov 2026** (official: 18 Nov 12:00 PT = 19 Nov 01:30 IST; one day buffer)
**Companion docs:** [`PRD.md`](PRD.md) · [`DECISIONS.md`](DECISIONS.md) · [`FRICTION_LOG.md`](FRICTION_LOG.md)

---

## 1. Timeline

| Phase | Dates | Goal | Exit gate |
|---|---|---|---|
| **P0 Foundations + spikes** | Fri 23 – Mon 26 Oct | Unity project builds to Quest 3, core tests green in Unity and CI, risky SDKs proven | S-1/S-2/S-3 results recorded in DECISIONS |
| **P1 Desk + capture slice** | Tue 27 Oct – Sun 1 Nov | Onboarding → dictate a note → restart → note still there | Storyboard beats 0:00, 0:25, 1:45 (persistence) on device |
| **P2 Agent wiring** | Mon 2 – Sat 7 Nov | Every grammar command works by voice **and** by hand menu | Beats 0:50, 1:45 (focus), 2:15, 2:30 on device |
| **P3 Character + personality** | Sun 8 – Thu 12 Nov | Alden and Pip feel alive; idle, switching, reactions | Beat 1:15 on device; full storyboard runnable |
| **P4 Hardening** | Fri 13 – Sun 15 Nov | 72 FPS, two full device-script passes, bug bash. **Feature freeze: end of Fri 13 Nov** | All Must Have device tests pass twice |
| **P5 Demo + submit** | Mon 16 – Tue 17 Nov | Record, edit, submit | Devpost submission confirmed |
| Buffer | Wed 18 Nov | Only for submission emergencies | — |

**Friday checkpoints (30 Oct, 6 Nov, 13 Nov):** upload the current APK to the **Competition** release channel and install it through the Invite URL on the headset (D-018). Record every storyboard beat that works *on device* as a rough clip. This is a progress check and the fallback demo footage. Then review the friction log and open decisions (§4).

---

## 2. Methodology

### 2.1 Architecture boundary (D-001)

```
┌──────────────────────── Unity project (unity/DeskButler) ────────────────────────┐
│  Adapters (thin, no product rules)                                               │
│  MRUK planes · spatial anchor · Interaction SDK · Voice SDK · TTS · JSON file    │
│  Views: ItemView, ButlerView, SubtitleBubble, HandKeyboard, HandMenus             │
│        │ calls                          ▲ results / cues                         │
└────────┼────────────────────────────────┼────────────────────────────────────────┘
         ▼                                │
┌──────── packages/com.deskbutler.core (pure C#, noEngineReferences) ──────────────┐
│  CommandParser → DeskAgent → AgentResult → ResponseCatalog                        │
│  Desk / DeskItem / DeskSnapshot · UndoBuffer · OrganiseLayout · DeskQueries       │
│  CaptureSession · OnboardingFlow · IdleBehaviour · Ports (IClock, IDeskStore …)   │
└───────────────────────────────────────────────────────────────────────────────────┘
```

**Rule:** if you're writing an `if` about *what the product should do* inside a MonoBehaviour, stop. Write a failing core test and put the rule in core. Adapters only translate.

### 2.2 Two-loop TDD

- **Outer loop (acceptance):** a storyboard beat or PRD behaviour, written first as a scenario. Core scenarios go in `DemoStoryboardTests` or a feature-level test. Device behaviour goes in a numbered **device test (DT-xx)** in §6.
- **Inner loop (unit):** red → green → refactor, in core with NUnit, seconds per cycle (`scripts/test.sh`).

| Layer | What's tested | Tool | Speed | Runs |
|---|---|---|---|---|
| Core unit | Grammar, agent rules, undo, layout, capture, onboarding, idle, catalog | NUnit via `dotnet test` and Unity EditMode | ~1 s | Every save; CI on push |
| Core acceptance | Storyboard end-to-end through core | `DemoStoryboardTests` | ~1 s | Every save; CI |
| Adapter (EditMode) | JSON store round-trip, presenters mapping results to view calls, with fake ports | Unity Test Framework EditMode | seconds | Before each commit touching adapters |
| Scene (PlayMode) | Wiring: spawn, bind by id, anchor × pose maths, onboarding cues → animator | Unity PlayMode + Meta XR Simulator | ~1 min | Before merging a task |
| Device | Hands, anchors across restarts, voice, latency, FPS | Scripted checklist §6 on Quest 3 | 5–15 min | End of each task with device impact; full pass at checkpoints |

**What not to unit-test:** animation feel, SDK internals, shader looks. Those are covered by device tests and checkpoint clips.

### 2.3 Per-task workflow

1. **Pick** the top unblocked task (WIP limit: one task in progress). Create branch `task/DB-0xx-short-slug`.
2. **Write the tests listed on the task card first**, and watch them fail for the right reason.
3. **Make them pass** with the simplest code; then **refactor** with the tests green.
4. If you chose between real options → add a **D-0xx** entry. If something cost > 15 min → add an **F-0xx** entry *now*.
5. For device-facing tasks, run the task's **DT** checks on the headset.
6. **Commit** using Conventional Commits with the task ID (§3.2), merge to `main` the same day, and delete the branch.
7. Tick the task's status in §5.

### 2.4 Rituals (solo-friendly)

- **Start of session (5 min):** read yesterday's last commit message and open frictions; pick one task.
- **End of session (5 min):** commit even if red (`wip:` prefix, on the branch only); write one line in the task card's notes about where you stopped.
- **Friday checkpoint (45 min):** record working beats, triage frictions (close, accept, or turn into a task), resolve `Proposed` decisions, re-check the cut line (§8).

---

## 3. Working agreements

### 3.1 Definition of Done (every task)
- [ ] Tests listed on the card written first and passing; `scripts/test.sh` green; CI green
- [ ] No product rule added to an adapter
- [ ] Device checks (DT-xx) on the card pass on Quest 3, if any
- [ ] Hands-only path exists for any new user-facing action
- [ ] Every butler reply also shown as a subtitle
- [ ] DECISIONS / FRICTION entries added if applicable
- [ ] Merged to `main`; status ticked here

### 3.2 Commits and branches
- Trunk-based; branches live ≤ 1 day. `main` must always build and pass core tests.
- Format: `type(scope): summary [DB-0xx]`, with types `feat`, `fix`, `test`, `refactor`, `docs`, `chore`, `spike`, `wip`
  - e.g. `test(agent): focus keeps locked items visible [DB-033]`
  - e.g. `feat(capture): open keyboard on mic denial [DB-024]`
- Reference decisions in the body: `decision: D-006`.

### 3.3 Adding a voice command (most common change)
1. Add `[TestCase]`s in `CommandParserTests` (positive, plus one near-miss that must **not** match)
2. Add the rule in `CommandParser` (order matters: specific before general)
3. Add agent tests for its outcomes, then implement in `DeskAgent`
4. Add `ResponseKey` lines for **both** personalities (the catalog test fails until you do)
5. Add the same action to a hand menu
6. Update the PRD §5.2 grammar list

---

## 4. Spikes (time-boxed; output = a decision, not code to keep)

| ID | Question | Box | Output |
|---|---|---|---|
| S-0 | Which Unity version + Meta XR SDK (v81+) + MRUK + Interaction SDK combination builds a passthrough hands-only app on Quest 3? | 3 h | Pinned versions in README; `unity/DeskButler` created |
| S-1 | Voice: live partial transcripts, time to first partial, two distinct TTS voices, offline behaviour, keys or accounts | 4 h | Accept or replace D-009 |
| S-2 | Anchors: create one desk anchor, save, restart app, resolve. Measure resolve time and failure modes. | 3 h | Notes in D-002; resolve-timeout value |
| S-3 | Character: placeholder rigged model walking and jumping on a detected plane at 72 FPS | 3 h | Decide D-015 |

---

## 5. Task Board

Sizes: **S** ≤ 2 h · **M** ≈ ½ day · **L** ≈ 1 day. Status: ✅ done · 🔄 in progress · ⬜ todo · ✂️ cut

### Core domain (built test-first in this repo, 146 tests)

| ID | Task | Tests (exist) | Status |
|---|---|---|---|
| DB-010 | Desk model, snapshot, schema guard, LoadOrCreate | `DeskTests` | ✅ |
| DB-011 | Command grammar incl. wake words, partials, free-text args | `CommandParserTests` | ✅ |
| DB-012 | Diff-based one-level undo, in-place restore | `UndoBufferTests` | ✅ |
| DB-013 | Capture state machine (dictation, correction, silence, keyboard) | `CaptureSessionTests` | ✅ |
| DB-014 | Onboarding flow incl. no-desk fallback and returning user | `OnboardingFlowTests` | ✅ |
| DB-015 | Agent: all commands, lock rules, delete confirm, focus | `DeskAgentTests` | ✅ |
| DB-016 | Organise layout + find/status/important queries | `OrganiseLayoutTests`, `DeskAgentTests` | ✅ |
| DB-017 | Cosmetic idle behaviour | `IdleBehaviourTests` | ✅ |
| DB-018 | Response catalog for both personalities | `ResponseCatalogTests` | ✅ |
| DB-019 | Storyboard acceptance through core | `DemoStoryboardTests` | ✅ |
| DB-039a | `FlavourGate`: LLM re-wording only within 700 ms, else baseline | `FlavourGateTests` | ✅ |

### P0 · Foundations (23–26 Oct)

| ID | Task | Size | Tests first | Acceptance | Deps | Status |
|---|---|---|---|---|---|---|
| DB-001 | Run `scripts/test.sh` locally with real NUnit; push to GitHub; CI green | S | — (existing 138) | Local and CI green; close F-001 | — | ⬜ |
| DB-002 | Read Devpost official rules: video length, required assets, build format | S | — | PRD v1.6 §6, §12; F-002 closed | — | ✅ |
| DB-009 | Dashboard: create app, **Competition** release channel, Invite URL; upload the DB-004 hello-world APK and install via the invite | S | — | Hello-world installs from the invite on Quest 3 | DB-004 | ⬜ |
| DB-009b | Eligibility: confirm VR Start membership; ask the sponsor in writing about India and prize payment (F-004) | S | — | Written answer saved in `docs/` | — | ⬜ |
| DB-003 | S-0: create `unity/DeskButler`; add the core package via `"com.deskbutler.core": "file:../../../packages/com.deskbutler.core"` and list it under `"testables"` | M | Core tests appear and pass in Unity Test Runner (EditMode) | 138 green inside Unity | DB-001 | ⬜ |
| DB-004 | Passthrough + hands-only hello world on Quest 3 (no controllers) | S | — | DT-00: app launches, hands visible, 72 FPS | DB-003 | ⬜ |
| DB-005 | `FileDeskStore : IDeskStore` (JsonUtility, write temp + rename) | S | EditMode: round-trip through real JSON incl. tags and enums; missing file → null; corrupt file → backup + null | Tests green | DB-003 | ⬜ |
| DB-006 | S-1 voice spike | M | — | D-009 Accepted/Replaced | DB-004 | ⬜ |
| DB-007 | S-2 anchor spike | S | — | D-002 notes + resolve timeout | DB-004 | ⬜ |
| DB-008 | S-3 character spike | S | — | D-015 decided | DB-004 | ⬜ |

### P1 · Desk + capture slice (27 Oct – 1 Nov)

| ID | Task | Size | Tests first | Acceptance | Deps | Status |
|---|---|---|---|---|---|---|
| DB-020 | `DeskSurfaceService`: MRUK plane → `DeskBounds` + desk anchor; `OnboardingDriver` feeds plane events and `Tick` into `OnboardingFlow` | M | EditMode with a fake plane provider: cue sequence for detect, timeout and late-detect | DT-01, DT-02 | DB-007 | ⬜ |
| DB-021 | Onboarding presentation: floating spawn, greeting line, jump-down, manual two-pinch placement UI | L | PlayMode: each cue triggers its animator state / UI | DT-01 hook ≤ 60 s; DT-02 | DB-020, DB-008 | ⬜ |
| DB-022 | `ItemView` spawn/bind by id; `anchor × LocalPose`; grab/move/rotate/scale → `agent.ApplyManualPose`; debounced save | L | EditMode: pose conversion core↔Unity (Vec3/Quat) round-trip; PlayMode: view count = desk items after load | DT-03 | DB-005, DB-020 | ⬜ |
| DB-023 | `ISpeechToText` adapter (per D-009); capture bubble with live text; poke Confirm/Cancel; open-hand cancel | L | EditMode: presenter maps each `CapturePrompt` to the right UI state with a fake STT | DT-04, DT-05 | DB-006 | ⬜ |
| DB-024 | Minimal hand keyboard (letters, space, backspace, done) → `OnKeyboardSubmit`; auto-open on mic error | M | EditMode: key buffer logic; mic error → keyboard visible | DT-06 | DB-023 | ⬜ |
| DB-025 | Launch rehydration: load → resolve anchor → spawn; "desk moved" path re-attaches items | M | PlayMode with a fake anchor service: resolved → views; failed → `PromptDeskMoved` | DT-03 | DB-022 | ⬜ |
| DB-026 | `ITextToSpeech` adapter + subtitle bubble for every `ResponseCatalog` line | M | EditMode: presenter speaks and subtitles the same text; personality → voice | DT-13 | DB-006 | ⬜ |

### P2 · Agent wiring (2 – 7 Nov)

| ID | Task | Size | Tests first | Acceptance | Deps | Status |
|---|---|---|---|---|---|---|
| DB-030 | `CommandRouter`: STT final → capture (if active) or `CommandParser` → `DeskAgent` → presenter; save when `DeskChanged` | M | EditMode: routing truth table (capturing vs not, pending confirm) with fakes | DT-07 | DB-023, DB-026 | ⬜ |
| DB-031 | `TargetResolver`: pointed item, then gazed item → `targetItemId` | S | EditMode: priority order, nothing hit → null | DT-08 | DB-022 | ⬜ |
| DB-032 | Organise animation: items tween to planned poses; butler gestures | M | PlayMode: after organise, views reach saved poses | DT-07 | DB-030 | ⬜ |
| DB-033 | Find highlight + butler points; Focus fade in/out | M | PlayMode: `ItemIds` → highlighted; hidden → faded and non-interactive | DT-09, DT-10 | DB-030 | ⬜ |
| DB-034 | Item hand menu: lock, important, tag (recent tags), delete → same `Command`s | M | EditMode: each button produces the same `Command` as its voice phrase | DT-12 | DB-030 | ⬜ |
| DB-035 | Butler hand menu: new note, organise, focus, status, switch, undo | M | EditMode: same mapping test | DT-12 | DB-030 | ⬜ |
| DB-036 | Delete confirmation UI (voice yes/no + poke buttons) | S | EditMode: `NeedsConfirmation` shows dialog; any answer hides it | DT-08 | DB-034 | ⬜ |
| DB-037 | Image panels: folder picker → `ItemType.Image` panel | M | Core test first: pin image item stores path; EditMode: missing file → placeholder | DT-15 | DB-022 | ⬜ |
| DB-038 | Latency instrumentation + debug overlay (end-of-speech → ack / change / voice) | S | EditMode: timer records the three marks | DT-13 within budget | DB-030 | ⬜ |
| DB-039 | Optional LLM adapter implementing `IFlavourModel`, wired through `FlavourGate` (core done in DB-039a), behind a toggle; API key never in the repo | S | EditMode: toggle off → model never called | Works with toggle off and on; DT-13 still in budget | DB-026 | ⬜ |

### P3 · Character + personality (8 – 12 Nov)

| ID | Task | Size | Tests first | Acceptance | Deps | Status |
|---|---|---|---|---|---|---|
| DB-040 | Alden and Pip models + animation set (idle, walk, jump, land, point, straighten, hop, dance, talk) | L | — (art) | Readable at 15–20 cm in passthrough | DB-008 | ⬜ |
| DB-041 | `ButlerController`: walk to a desk-local target within `DeskBounds`; face the user when addressed | M | EditMode: path target clamped to bounds | DT-11 | DB-040 | ⬜ |
| DB-042 | Idle loop: Pip timer → `PipNudge` → render offsets; Alden walks `AldenTargets` → clear | M | EditMode: offsets applied to view transform only; saved poses unchanged after N ticks | DT-11, DT-08 | DB-041 | ⬜ |
| DB-043 | Personality switch transition + voice swap; desk preserved | S | Existing core test + PlayMode: no item view moves on switch | DT-11 | DB-042 | ⬜ |
| DB-044 | Reactions: looked-at, pointed-at, spoken-to | M | EditMode: gaze dwell threshold → reaction event once | DT-11 | DB-041 | ⬜ |

### P4 · Hardening (13 – 15 Nov) — feature freeze end of 13 Nov

| ID | Task | Size | Acceptance | Status |
|---|---|---|---|---|
| DB-050 | Performance: 72 FPS on Quest 3/3S (OVR Metrics); Quest 2 smoke test | M | DT-14 | ⬜ |
| DB-051 | Full device-test pass × 2 (two desks / lighting conditions) | M | All DT pass twice | ⬜ |
| DB-052 | Accessibility pass: subtitle size, **high-contrast subtitle toggle**, hands-only run, keyboard-only note, voice alternative for every hand action | S | DT-12, DT-06, DT-18 | ⬜ |
| DB-054 | Cold start ≤ 10 s and clean pause/resume (headset off/on, Home menu) | M | DT-16, DT-17 | ⬜ |
| DB-053 | Bug bash + fixes (each fix starts with a failing test where possible) | L | No S1/S2 bugs open | ⬜ |

### P5 · Demo + submission (16 – 17 Nov)

| ID | Task | Size | Acceptance | Status |
|---|---|---|---|---|
| DB-060 | Rehearse the storyboard script; seed a realistic desk; record mixed-reality takes | M | Clean take per beat | ⬜ |
| DB-061 | Edit to the storyboard; captions; **≤ 2:45**; device footage only (no AI-generated video); upload public to YouTube | M | Public link plays logged-out | ⬜ |
| DB-062 | Devpost form: name, 140-char tagline, Productivity / New Experience, ≤ 500-word description (inspiration, how built, future plans, target launch date, hand interactions), screenshots; final APK in Competition channel; Invite URL tested from a second account; **submit 17 Nov** | S | Confirmation received; invite installs on a clean headset | ⬜ |

---

## 6. Device test scripts (Quest 3, hands only unless stated)

Run the ones on a task card at the end of that task, and all of them at checkpoints and in DB-051. Log failures as bugs with a failing test where the cause is in core.

| ID | Script | Pass when |
|---|---|---|
| DT-00 | Launch app | Passthrough, hands tracked, no controller prompt, 72 FPS |
| DT-01 | Fresh install → follow the butler | Greeting → look at desk → jump-down → invite, all within 60 s |
| DT-02 | Fresh install, look at the floor/ceiling for 10 s | Manual-placement prompt; two pinches → jump-down |
| DT-03 | Create 3 notes, move one, quit app, relaunch | Same notes, same places (± 1 cm), "welcome back" |
| DT-04 | Dictate a note, say "No, I said …", confirm | Corrected text pinned, case kept |
| DT-05 | Start a note, stay silent | Prompt at ~4.5 s offering retry or keyboard |
| DT-06 | Deny mic permission (or mute), create a note | Keyboard opens; typed note pinned |
| DT-07 | Scatter 5 notes, "clean up the desk", then "undo" | Grouped by tag; undo restores exact positions |
| DT-08 | Lock a note; organise, focus, "delete this" on it | Never moves, stays visible, delete refused |
| DT-09 | Mark 1 important, "focus mode", "exit focus", "undo" | Others fade; return; fade again |
| DT-10 | "Where did I put the pricing notes?" (+ one that doesn't exist) | Highlight + point; "nothing like that" for the missing one |
| DT-11 | Switch Pip ↔ Alden while idle 60 s each | Pip knocks notes crooked; Alden straightens them; saved poses unchanged after restart |
| DT-12 | Whole storyboard **without speaking** | Every beat achievable via hand menus and keyboard |
| DT-13 | Latency overlay during 10 commands | Ack ≤ 200 ms, change ≤ 400 ms, voice ≤ 1.0 s (p90) |
| DT-14 | 5-minute session with 20 items, Pip idle | ≥ 72 FPS sustained on Quest 3/3S; **never below 60** (judged floor) |
| DT-15 | Add an image panel from the folder | Image shows; survives restart |
| DT-16 | Returning user: launch from the app library | Butler on the desk within 10 s |
| DT-17 | Mid-dictation: take headset off 10 s, put back on; then open/close the Home menu | Draft, desk and personality intact; no stuck mic |
| DT-18 | Every hand-menu action, done by voice instead; subtitles in high-contrast mode | All actions have a voice equivalent; subtitles readable on a bright desk |

---

## 7. Risk register

| Risk | Likelihood | Impact | Mitigation | Trigger to act |
|---|---|---|---|---|
| Voice SDK can't do live partials or two voices | M | H | Ports make it swappable; fallbacks in D-009; pre-recorded catalog lines | S-1 fails by 26 Oct |
| Anchors resolve unreliably | M | H | Single desk anchor; "desk moved" path re-attaches items; manual placement | S-2 shows > 10 % failures |
| Character art/animation eats the schedule | H | M | Stylised low-poly; reuse a rig across both characters; placeholders until P3 | DB-040 > 1.5 days |
| Hand keyboard is fiddly | M | M | Large keys, poke-only, short notes | DT-06 takes > 60 s for 5 words |
| Scope creep from "one more command" | M | M | Grammar locked; new commands need a D-entry | Any new command after 6 Nov |
| Demo recording quality | M | H | Checkpoint clips from 30 Oct; rehearse script | Checkpoint clip unusable |
| Not eligible to win from India (banking clause) | L–M | H | Ask the sponsor in writing now (DB-009b, F-004) | No answer by 6 Nov → follow up on Devpost discussion + Start Discord |
| Dashboard/upload problems on submission night | M | H | Competition channel set up in P0; upload at every checkpoint (D-018) | Any checkpoint upload fails |
| Cloud voice down during judging | L | H | Commands work offline; keyboard always available ("take it away" test) | — |

---

## 8. Cut line (if behind at the 6 Nov checkpoint)

These are all Must Haves, so cutting one is a decision (D-entry) and not a silent drop. Cut in this order; nothing above the line is touched.

1. Quest 2 support (already best-effort)
2. LLM flavour (DB-039 adapter; the gate stays)
3. Reactions to gaze/point (DB-044): keep spoken-to only
4. Image panels (DB-037)
5. Tag via hand menu: keep voice tagging

— **Never cut:** hook + fallback, dictation + correction, keyboard, persistence, organise + undo, focus, lock, personalities, hands-only path, subtitles.
