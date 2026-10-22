# Desk Butler – Product Requirements Document (PRD)

**Version:** 1.5
**Date:** 22 October 2026
**Status:** Locked for Development (changes go through `docs/DECISIONS.md`)
**Target Competition:** Meta VR Start Developer Competition 2026 (submission deadline **18 Nov 2026**)
**Primary Track:** Productivity
**Secondary Focus:** Best Agentic Interaction + Best Accessibility Forward Experience

---

## 0. Changes from v1.4

v1.4 had gaps and contradictions. All of them are now **Must Have** and resolved as follows.

| # | Gap in v1.4 | Resolution in v1.5 | Decision |
|---|---|---|---|
| 1 | Focus Mode was Should Have but in the core grammar and demo | Focus Mode is **Must Have** | D-008 |
| 2 | "What's important" was Must, but priority marking was Should and the model had no `priority` | Priority field + "mark this important" are **Must Have** | D-003 |
| 3 | "Group research notes" had no field to group by | `tags` field + "tag this as …" are **Must Have** | D-003 |
| 4 | Idle tidying/nudging was unprompted movement that broke "only on explicit command" | Idle behaviour is **cosmetic only**: never saved, never on locked items | D-006 |
| 5 | Undo snapshotted transforms + locked only, so a deleted item couldn't come back | Undo captures **full item state** for every desk-changing command | D-005 |
| 6 | One anchor per item is heavy and drifts | **Single desk anchor**; items stored as desk-local poses | D-002 |
| 7 | LLM / speech-to-text / text-to-speech unspecified | Voice + LLM stack and **latency budget** defined (§5.8) | D-004, D-009 |
| 8 | Quest 2 passthrough weakens the hook; 72 FPS is tight | **Quest 3/3S primary**, Quest 2 best-effort | D-010 |
| 9 | No source for image panels | Images come from a **local device folder** | D-012 |
| 10 | No fallback when no desk is detected | **10 s timeout → manual placement** | D-011 |
| 11 | No defined reply to unrecognised commands | Deterministic **"not understood + help"** reply; LLM small talk only | D-004 |
| 12 | Voice errors said "use the keyboard", but the keyboard was Should | Minimal hand keyboard is **Must Have** | D-014 |

---

## 1. Product Vision

**Desk Butler** is a hands-first mixed reality productivity companion that lives on the user's real desk.

A small AI butler sits on the physical desk (via passthrough). He acts as both a **spatial workspace manager** and a **productive agent**. He helps the user organise information, keep focus, remember context and reduce friction in daily knowledge work, while being a persistent, living presence.

The product is **not** a general-purpose voice assistant with an avatar. It is a spatially grounded productivity agent whose domain is the user's desk and the information placed on it.

Users choose between two personalities:

- **Alden**: professional, calm, slightly witty British-style butler
- **Pip**: cute, energetic, playful helper spirit

Personality creates emotional attachment and makes the experience memorable. Productivity remains the core value.

---

## 2. Goals

### Primary Goals
- Make spatial organisation of information faster and more natural than on 2D screens
- Provide a persistent, context-aware agent that lives on the user's desk
- Reduce the mental load of remembering where things are and what needs attention
- Deliver a clear "wow" within the first 60–90 seconds
- Be fully usable with hands only (no controllers); this is a competition requirement
- Support both voice and non-voice input for accessibility

### Secondary Goals
- Create emotional connection through strong personality and idle behaviour
- Demonstrate excellent hands-first interaction design
- Score highly in Productivity, Best Agentic Interaction and Best Accessibility Forward Experience

---

## 3. Target User

Solo knowledge workers, freelancers, students and remote workers who:
- Regularly work at a physical desk
- Juggle multiple notes, references and tasks
- Prefer spatial organisation over endless tabs or floating windows
- Want a lightweight, focused companion rather than a broad general assistant
- May need to work in quiet environments or have limited hearing

---

## 4. Core Experience Pillars

1. **Spatial Desk**: the real desk becomes a persistent canvas for information
2. **Productive Agent**: the butler helps organise, prioritise and surface information
3. **Living Presence**: idle behaviour, reactions and personality make the character feel alive
4. **Hands-First + Accessible**: every core flow can be completed with hands only; voice is primary but never required

---

## 5. Feature Requirements

### 5.1 Spatial Desk System + Data Model (Must Have)

- Detect the user's desk surface using MR Utility Kit / Scene Understanding
- Item types: **Sticky Notes** (short text), **Text Panels** (title + body), **Image / Reference Panels**
- **One persistent spatial anchor for the desk** (D-002). Every item's pose is stored relative to it. Re-anchoring the desk moves all items together and coherently.

**Data Model (implemented in `packages/com.deskbutler.core/Runtime`):**

```text
DeskSnapshot {
  schemaVersion: int              // 1; newer-than-app snapshots are rejected, never silently read
  deskAnchorId: string            // persistent spatial anchor UUID of the desk
  bounds: { minX, maxX, minZ, maxZ }   // usable desk rectangle, desk-local metres
  items: Item[]
}

Item {
  id: string                      // stable GUID; key for views, undo, persistence
  type: "sticky" | "panel" | "image"
  title: string?                  // panels
  content: string                 // text, or local file path for images
  tags: string[]                  // first tag = grouping tag for Organise
  priority: "normal" | "high"     // drives "What's important" and Focus Mode
  locked: boolean                 // agent cannot move, hide, or delete
  hiddenByFocus: boolean          // set only by Focus Mode
  localPose: { position, rotation, scale }   // relative to the desk anchor
  createdAtMs, updatedAtMs: long  // unix ms
}
```

- Stored locally as one JSON file per desk, saved after every desk-changing result (`AgentResult.DeskChanged`)
- On launch: load snapshot → resolve desk anchor → spawn item views at `anchor × localPose`
- If the anchor fails to resolve, the butler says the desk moved and re-runs desk detection; items re-attach to the new anchor
- **Image panels** take images from a local device folder (`Pictures/DeskButler/`) via a hand-friendly picker. Camera capture is out of scope (D-012).

**Interactions:**
- Full hand interaction: grab, move, rotate, scale, delete
- Visual feedback when an item is placed on the desk
- Lock/unlock, mark important, and tag via voice **or** the item's hand menu

### 5.2 Agentic Layer (Must Have)

| Capability | Description | Example Commands |
|---|---|---|
| Organise | Group by tag into columns, important first, locked items untouched | "Clean up the desk", "Group research notes" |
| Prioritise | Surface items marked important | "What's important today?" |
| Remember | Find items by title, content or tag; highlight them | "Where did I put the pricing notes?" |
| Capture | Turn speech or typing into a note | "New note", "Pin this idea: call the vendor" |
| Status | Short overview from desk data | "Give me a quick overview" |
| Focus Mode | Hide everything not marked important | "Focus mode", "Exit focus" |
| Mark / Tag | Set priority and tags on the pointed item | "Mark this important", "Tag this as research" |
| Delete | Remove the pointed item, after confirmation | "Delete this" → "Yes" |

**Agent Rules:**
- Acts only on items that exist on the desk; **never invents** an item, location or content
- "This/it" means the item the user is pointing at or gazing at. With no target, the butler asks "Which one?"
- **Delete always asks for confirmation.** An explicit organise command counts as consent; undo is the safety net (D-007).
- Locked items are never moved, hidden by Focus Mode, or deleted
- Focus Mode hides `priority = normal` items that are not locked. If nothing is important, the butler asks the user to mark something first.
- **Every desk-changing command is undoable** (§5.3)

**Command Grammar (locked, implemented in `CommandParser.cs`):**

- New note: "New note" / "Pin this (idea)" / "Remember this" / "Take a note" (+ optional inline text)
- Organise: "Clean up the desk" / "Organise the desk" / "Tidy up" / "Group *tag* notes"
- Find: "Where is …" / "Where did I put …" / "Find …"
- Status: "Status" / "Overview" / "Give me a quick overview"
- Important: "What's important (today)" / "What needs attention"
- Focus: "Focus mode" / "Clear distractions" / "Exit focus" / "Show everything"
- Lock: "Lock this" / "Unlock this"
- Priority: "Mark this important" / "This is important" / "Mark this as not important"
- Tag: "Tag this as *x*"
- Delete: "Delete this" / "Remove this"
- Undo: "Undo" / "Take that back"
- Personality: "Switch to Alden" / "Switch to Pip"
- Dialogue: "Yes" / "No" / "Cancel" / "Never mind" / "No, I said …" / "Change that to …" / "Help"

Wake words ("Alden,", "hey Pip") and politeness ("please", "could you") are ignored.

**Partial phrases** ("Where is", "Switch to", "Tag this as") get a clarification question. **Unrecognised speech** gets the deterministic "not understood, say 'help'" line. The LLM may add small talk but never takes actions (§5.8).

### 5.3 Undo System (Must Have)

- Applies to **every desk-changing voice/agent command**: organise, focus on/off, lock/unlock, mark, tag, delete, pin note
- Before the command, the desk is snapshotted. After it, only the items the command **actually changed** are kept, with their **full prior state** (content, tags, priority, lock, visibility, pose). Created items are recorded so undo removes them.
- **One level.** A command that changes nothing does not overwrite the previous undo.
- Undo restores state **into the existing item objects**, so views holding references stay valid
- Manual hand moves are not undoable. Hand edits to items the last command didn't touch survive an undo.
- Voice: "Undo". Hands: undo button on the butler's hand menu.

### 5.4 Character System

**Shared:**
- About 15–20 cm tall, sits at the desk corner, walks across the surface
- Reacts to being looked at, pointed at or spoken to
- Simple facial expressions and body language; spatialised voice
- Every reply is also shown as a **subtitle bubble** (accessibility)

**Idle behaviour is cosmetic (D-006).** Idle animations apply render-only offsets on top of saved poses. They never change the saved desk, never touch locked or hidden items, and are cleared on restart.

#### Alden (Professional)
- Tone: calm, precise, mildly dry humour; measured, slightly formal voice
- Idle: walks to any crooked item and straightens it (clears visual offsets), dusting gestures, composed stillness

#### Pip (Energetic)
- Tone: cheerful, eager, playful; higher-energy, faster voice
- Idle: hops, dances, stretches. Occasionally knocks one unlocked item slightly crooked (≤ 1.5 cm, ≤ 5°).

**Switching:** voice or hand menu, instant, desk state preserved.

### 5.5 Onboarding (Must Have, 60-second hook)

**First run:**
1. App launches, passthrough on
2. Butler **spawns floating in front of the user's face** and greets them in personality
3. Prompts the user to look at the desk:
   - **Alden:** "Ah, a new workspace. Kindly look toward your desk so I may take my proper place."
   - **Pip:** "Hi! Look at your desk — I wanna jump down!"
4. Plane detection runs in the background
5. On a suitable horizontal surface: **jump-down animation**, landing gesture, invitation to create the first note
6. **No surface after 10 s (D-011):** "Pinch where your desk is." The user pinches two corners at desk height to define the plane, then the butler jumps down. A late automatic detection is still accepted.

**Returning user:** the saved desk anchor resolves, and the butler is already on the desk with a "welcome back" (no greeting). If the anchor fails, "Looks like the desk moved" and desk detection runs again.

### 5.6 Text Input (Must Have)

**Primary:** voice dictation. **Secondary (Must Have, D-014):** a minimal hand keyboard for deaf/hard-of-hearing users, quiet rooms, and mic failure or denied permission.

**Voice note flow:** trigger → butler acknowledges → user speaks → live text appears → user confirms or corrects → pinned. Inline text ("Pin this idea: …") goes straight to confirmation.

| Situation | Behaviour |
|---|---|
| Mis-hearing | "No, I said …" / "Change that to …" replaces the text (case and punctuation kept) and re-confirms |
| Bare "No" | Clears the draft and listens again |
| Silence | After 4.5 s with nothing heard: "I didn't catch that. Try again or use the keyboard." |
| Long dictation | Ends after 30 s; any text heard goes to confirmation |
| Cancel | "Cancel", "Never mind", or the open-hand gesture |
| Mic unavailable | Keyboard opens automatically |
| Keyboard "Done" | Saves directly (explicit confirmation) |

The butler never fails silently and never fills in text the user didn't say or type.

### 5.7 Interaction Methods

- Primary: hand tracking (grab, pinch, point, poke)
- Text: voice dictation; minimal hand keyboard
- Targeting "this": pointed item first, then gazed item
- Every core flow is completable with hands only: item hand menu (lock, important, tag from recent tags, delete, undo) plus a butler hand menu (new note, organise, focus, status, switch personality)

### 5.8 Voice, LLM and Latency (Must Have)

- **Speech-to-text, text-to-speech:** behind `ISpeechToText` / `ITextToSpeech`. The candidate stack is Meta Voice SDK dictation and TTS with two distinct voices; it is validated in spike S-1 (D-009). It is swappable without touching the core.
- **Commands are deterministic.** The grammar picks the action and the response catalog picks the line.
- **The LLM is optional flavour** (`IFlavourModel`, D-004). It may re-word a line the core already chose, or answer small talk. It never chooses actions and is never the only source of a reply. If it doesn't answer within **700 ms**, the baseline line is used.

**Latency budget (end of user speech →):**

| Event | Budget |
|---|---|
| Visual acknowledgement (butler turns / ear twitch) | ≤ 200 ms |
| Desk change visible (highlight, move, hide) | ≤ 400 ms |
| Spoken reply starts | ≤ 1.0 s |

---

## 6. Demo Video Storyboard (Judged Artifact)

Target **3:00**. The Meta announcement doesn't state a length, so verify it against the Devpost rules (F-002). This sequence drives development priority and is encoded as the acceptance test `DemoStoryboardTests`.

| Time | Beat | What is shown |
|---|---|---|
| 0:00 | Hook: Jump Down | Butler spawns floating, asks user to look at desk, jumps down |
| 0:25 | First Note (Dictation) | Voice note, one correction, pinned |
| 0:50 | Agentic Organise | "Clean up the desk" groups notes; "Undo" shown briefly |
| 1:15 | Personality Contrast | Pip knocks a note crooked; switch to Alden, who straightens it |
| 1:45 | Focus Mode + Persistence | Focus hides non-important notes; app restarted, everything still there |
| 2:15 | Accessibility | Hand keyboard note in a "quiet room"; subtitles visible |
| 2:30–3:00 | Closing | "Where did I put the pricing notes?" highlight + status + flourish |

---

## 7. Out of Scope (MVP)

- Multiplayer or shared desks
- Rich text editing or complex documents
- External integrations (Notion, Google Drive, Calendar, etc.)
- Multi-step autonomous planning
- Custom characters or clothing
- Physics beyond cosmetic nudging
- Multi-level undo history
- Camera capture for image panels
- Wall / vertical surfaces

---

## 8. Technical Requirements

| Area | Requirement |
|---|---|
| Engine | Unity (version pinned in spike S-0 to what the Meta XR SDK supports) + Meta XR All-in-One SDK v81+ |
| Architecture | Pure C# core package (no `UnityEngine`) + thin Unity adapters (D-001) |
| Interaction | Meta Interaction SDK (hands, gaze, poke, pinch) |
| Scene | MR Utility Kit (plane detection) |
| Anchors | One persistent spatial anchor per desk (D-002) |
| Storage | Local JSON `DeskSnapshot`, schema-versioned |
| Input | Hands-first + voice + minimal hand keyboard |
| Performance | Stable 72 FPS on Quest 3/3S; Quest 2 best-effort (D-010) |
| Passthrough | Required |
| Audio | Spatialised butler voice + subtitles |
| Target Devices | **Quest 3, Quest 3S** (primary); Quest 2 (best-effort) |
| Testing | Core: NUnit TDD, CI on every push. Unity: EditMode/PlayMode. Device: scripted checklist. |

---

## 9. Priority Backlog

### Must Have (MVP)
- Floating spawn + jump-down onboarding, **with the no-desk fallback and returning-user path**
- Voice dictation note creation with the full error handling and correction loop
- **Minimal hand keyboard**
- Desk data model with **tags, priority, hiddenByFocus**, stored against a **single desk anchor**
- Desk detection + persistence + rehydration
- Sticky notes, text panels, **image panels from a local folder**
- Two personalities with **cosmetic** idle behaviours
- Locked command grammar, including **mark important, tag, delete-with-confirm**
- Deterministic reply for **unrecognised and partial** commands
- **Focus Mode**
- **One-level undo with full item state** for every desk-changing command
- **Voice/LLM adapter layer within the latency budget**
- Full hands-only interaction (item + butler hand menus)
- Personality switching
- Subtitles for every butler reply

### Should Have
- Improved listening / confirmation UI polish
- Better visual polish
- More idle animations

### Could Have
- Additional panel types
- Wall surface support (future)

---

## 10. Design Principles

- Productivity first, personality second
- Never invent information that isn't on the desk
- Prefer spatial actions over abstract menus
- Present without being distracting; idle behaviour is cosmetic
- Hide technical complexity behind narrative and delight
- Every flow works with voice **and** without voice
- Every agent desk change is reversible (one-level undo)
- The grammar decides actions; the LLM only decides wording

---

## 11. Open Questions

- Character visual style (stylised vs semi-realistic). Decide by 28 Oct (D-015, pending).
- Official demo video length on Devpost (F-002)

---

**End of PRD v1.5**
