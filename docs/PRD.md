# Desk Butler – Product Requirements Document (PRD)

**Version:** 1.4  
**Date:** 22 October 2026  
**Status:** Draft for Development  
**Target Competition:** Meta VR Start Developer Competition 2026  
**Primary Track:** Productivity  
**Secondary Focus:** Best Agentic Interaction + Best Accessibility Forward Experience  

---

## 1. Product Vision

**Desk Butler** is a hands-first mixed reality productivity companion that lives on the user’s real desk.

A small AI butler sits on the physical desk (via passthrough) and acts as both a **spatial workspace manager** and a **productive agent**. He helps the user organise information, maintain focus, remember context, and reduce friction in daily knowledge work — while providing a persistent, living presence.

The product is **not** a general-purpose voice assistant with an avatar. It is a spatially grounded productivity agent whose primary domain is the user’s desk and the information placed on it.

Users can choose between two distinct personalities:

- **Alden** — Professional, calm, slightly witty British-style butler
- **Pip** — Cute, energetic, playful helper spirit

Personality exists to create emotional attachment and make the experience memorable. Productivity remains the core value.

---

## 2. Goals

### Primary Goals
- Make spatial organisation of information faster and more natural than traditional 2D screens
- Provide a persistent, context-aware agent that lives on the user’s desk
- Reduce the mental load of remembering where things are and what needs attention
- Deliver a clear “wow” experience within the first 60–90 seconds
- Be fully usable with hands only (no controllers required)
- Support both voice and non-voice input paths for accessibility

### Secondary Goals
- Create emotional connection through strong character personality and idle behaviour
- Demonstrate excellent hands-first interaction design
- Score highly in the Productivity track, Best Agentic Interaction, and Best Accessibility Forward Experience awards

---

## 3. Target User

Solo knowledge workers, freelancers, students, and remote workers who:
- Regularly work from a physical desk
- Juggle multiple notes, references, and tasks
- Prefer spatial organisation over endless browser tabs or floating windows
- Want a lightweight, focused companion rather than a broad general AI assistant
- May need to work in quiet environments or have limited hearing

---

## 4. Core Experience Pillars

1. **Spatial Desk** — The real desk becomes a persistent canvas for information
2. **Productive Agent** — The butler actively helps organise, prioritise, and surface information
3. **Living Presence** — The character feels alive through idle behaviour, reactions, and personality
4. **Hands-First + Accessible Design** — Every core flow can be completed with hands only; voice is primary but not exclusive

---

## 5. Feature Requirements

### 5.1 Spatial Desk System + Data Model (Must Have)

- Detect the user’s real desk surface using Scene Understanding / MR Utility Kit
- Support pinning of the following item types:
  - Sticky Notes (short text)
  - Text Panels (title + body)
  - Image / Reference Panels
- All items use Persistent Spatial Anchors

**Data Model (Required):**

Every desk item is stored locally with the following structure:

```text
Item {
  anchorUUID: string          // Persistent Spatial Anchor ID
  type: "sticky" | "panel" | "image"
  content: string             // Text content or image reference
  title: string?              // Optional title for panels
  locked: boolean             // If true, butler cannot move or delete
  createdAt: timestamp
  updatedAt: timestamp
  position: Vector3           // Local offset relative to anchor (for recovery)
  rotation: Quaternion
  scale: Vector3
}
```

- Content is stored in a local key-value store keyed by `anchorUUID`
- On app launch, the system re-hydrates all items by resolving their anchors and attaching the stored content
- “Remember” and “Status” commands depend entirely on this data model
- Locked flag prevents the butler from moving or deleting the item

**Interactions:**
- Full hand interaction: grab, move, rotate, scale, delete
- Visual feedback when an item is successfully anchored
- Ability to lock/unlock items

### 5.2 Agentic Layer (Must Have – Productivity Focused)

The butler is a productive agent, not just a character.

**Core Capabilities:**

| Capability     | Description                                      | Example Commands                          |
|----------------|--------------------------------------------------|-------------------------------------------|
| Organise       | Tidy and group related items on the desk         | “Clean up the desk”, “Group research notes” |
| Prioritise     | Surface what needs attention                     | “What’s important today?”                 |
| Remember       | Maintain spatial + semantic memory of the desk   | “Where did I put the pricing notes?”      |
| Capture        | Quickly turn spoken or typed input into notes    | “Pin this idea”, “Remember this”          |
| Status         | Give a short overview of the current desk        | “Give me a quick overview”                |
| Focus Mode     | Temporarily hide non-essential items             | “Focus mode”, “Clear distractions”        |

**Agent Rules:**
- Only acts on items that exist on the desk
- Always confirms before deleting or making major rearrangements
- Respects locked items
- **All destructive or organisational actions are reversible** (see Undo below)

**Command Grammar (Locked Early):**

A small, explicit set of commands is recognised deterministically. Everything else is treated as conversational filler and handled by the LLM only for tone/personality.

**Core Command Vocabulary:**
- “New note” / “Pin this” / “Remember this”
- “Clean up the desk” / “Organise the desk”
- “Where is …” / “Find …”
- “What’s important” / “Give me a status” / “Overview”
- “Focus mode” / “Clear distractions” / “Exit focus”
- “Lock this” / “Unlock this”
- “Undo”
- “Switch to Alden” / “Switch to Pip”

Ambiguous or partial phrases should be clarified by the butler rather than guessed.

### 5.3 Undo System (Must Have)

Because the agent is allowed to reorganise and (with confirmation) delete items, **Undo is a Must Have**.

**Implementation (Cheap & Reliable):**
- Before any agent-driven organisational or destructive action, snapshot the relevant items’ transforms and locked state
- Support **one level of undo** only
- Voice command: “Undo”
- Hand alternative: simple undo gesture or button near the butler
- After undo is used, the snapshot is cleared

This satisfies the rule “All destructive or organisational actions are reversible” without building a full history stack.

### 5.4 Character System

**Shared Requirements:**
- Small character approximately 15–20 cm tall
- Primarily sits on the corner or edge of the desk
- Can walk and move across the desk surface
- Reacts to being looked at, pointed at, or spoken to
- Simple facial expressions and body language
- Spatial voice output

#### Personality: Alden (Professional)
- **Tone:** Calm, precise, mildly dry humour
- **Idle Behaviour:** Acts as a housekeeper of the desk
  - Straightens notes and panels
  - Aligns items neatly
  - Performs light “dusting” gestures
  - Stands still with composed posture when nothing needs doing
- **Voice Style:** Measured, slightly formal

#### Personality: Pip (Energetic)
- **Tone:** Cheerful, eager, playful
- **Idle Behaviour:** Plays and messes around
  - Bounces and hops across the desk
  - Picks up notes and places them back slightly crooked
  - Does small dances or stretches
  - Occasionally nudges items out of perfect alignment
- **Voice Style:** Higher energy, warmer, faster speech

**Personality Switching:**
- Available at any time via voice command or simple hand menu
- Switch is instant and preserves the current desk state

### 5.5 Onboarding Experience (Critical – 60 Second Hook)

The first experience must deliver a clear wow moment and hide technical setup behind narrative.

**Exact Flow:**

1. App launches and passthrough activates.
2. The Butler **immediately spawns floating in front of the user’s face** (not on the desk).
3. He delivers a short greeting according to the selected personality.
4. He then prompts the user to look at their desk:
   - **Alden:** “Ah, a new workspace. Kindly look toward your desk so I may take my proper place.”
   - **Pip:** “Hi! Look at your desk — I wanna jump down!”
5. While the user looks at the desk, Scene Understanding / plane detection runs in the background.
6. Once a suitable horizontal surface is detected, the Butler performs a short, delightful **jump-down animation** onto the desk.
7. He lands, performs a personality-appropriate gesture, and invites the user to create their first note.

### 5.6 Text Input Mechanics

**Primary method: Voice-to-Text Dictation**

**Secondary / Accessibility method: Minimal Hand Keyboard** (elevated to Should Have)

Hand-tracked floating keyboards are frustrating as a primary method, but a minimal keyboard is required for:
- Deaf / hard-of-hearing users
- Quiet environments where speaking is inappropriate
- Microphone failure or permission denial

**Voice Note Creation Flow:**
1. User triggers note creation via voice or hand gesture
2. Butler acknowledges and begins listening
3. User speaks
4. Near real-time speech-to-text appears
5. User confirms or corrects
6. Note is pinned

**Voice Error Handling (Required):**

| Situation                        | Behaviour |
|----------------------------------|---------|
| Mis-hearing                      | User can say “No, I said [correction]” or “Change that to …” |
| Correction loop                  | Butler updates the text and re-confirms |
| Silence / nothing heard          | After ~4–5 seconds: “I didn’t catch that. Try again or use the keyboard.” |
| Timeout                          | Listening ends cleanly; user can restart |
| Cancellation                     | “Cancel”, “Never mind”, or open-hand gesture aborts |

The butler should never silently fail or invent content.

### 5.7 Interaction Methods

- **Primary:** Hand tracking (grab, pinch, point, poke)
- **Primary for text:** Voice-to-Text
- **Secondary for text:** Minimal hand keyboard (accessibility + quiet environments)
- **Supporting:** Gaze + pinch
- All core flows must remain completable with hands only

---

## 6. Demo Video Storyboard (Judged Artifact)

The demo video is capped at **3 minutes**. Feature priority should be driven by this storyboard.

| Time     | Beat                                      | What is shown |
|----------|-------------------------------------------|------------- |
| 0:00     | Hook – Jump Down                          | Butler spawns floating, asks user to look at desk, jumps down |
| 0:25     | First Note (Dictation)                    | User creates a note via voice; text appears and is pinned |
| 0:50     | Agentic Organise                          | Butler cleans up / groups notes on command |
| 1:15     | Personality Contrast                      | Quick switch between Alden (tidy) and Pip (playful idle) |
| 1:45     | Focus Mode + Persistence                  | Focus mode activates; app is restarted to show items still there |
| 2:15     | Accessibility / Keyboard (optional)       | Brief show of hand keyboard as alternative input |
| 2:30–3:00| Closing                                   | Butler gives a short status + personality flourish |

This sequence should drive development priority.

---

## 7. Out of Scope (MVP)

- Multiplayer or shared desks
- Rich text editing or complex document handling
- External service integrations (Notion, Google Drive, Calendar, etc.)
- Advanced multi-step autonomous planning
- Custom character creation or clothing systems
- Complex physics beyond light nudging of items
- Full multi-level undo history (only one-level snapshot undo)

---

## 8. Technical Requirements

| Area              | Requirement                                      |
|-------------------|--------------------------------------------------|
| Engine            | Unity + Meta XR All-in-One SDK (v81 or newer)   |
| Interaction       | Meta Interaction SDK (Hands + Gaze)             |
| Scene             | MR Utility Kit / Scene Understanding            |
| Anchors           | Persistent Spatial Anchors                      |
| Data Storage      | Local store keyed by Anchor UUID                |
| Input             | Hands-first + Voice + Minimal Keyboard          |
| Performance       | Stable 72 FPS on Quest 2                        |
| Passthrough       | Required                                        |
| Audio             | Spatialised butler voice                        |
| Target Devices    | Quest 2, Quest 3, Quest 3S                       |

---

## 9. Priority Backlog

### Must Have (MVP)
- Instant floating Butler spawn + narrative jump-down onboarding
- Voice-to-Text note creation with proper error handling & correction loop
- Local data model keyed by Anchor UUID (content + type + locked + timestamps)
- Desk detection + persistent spatial anchors
- Sticky notes and basic text panels
- Two personalities with correct idle behaviours
- Core agentic commands using locked command grammar
- **One-level Undo** for agent actions
- Full hands-only interaction
- Personality switching

### Should Have
- Minimal hand keyboard (accessibility + quiet environment fallback)
- Focus Mode
- Improved listening / confirmation UI
- Simple importance / priority marking on notes
- Better visual polish

### Could Have
- Additional panel types
- More varied idle animations
- Desk overview with light prioritisation
- Wall surface support (future)

---

## 10. Design Principles

- Productivity first, personality second
- Never invent information that does not exist on the desk
- Prefer spatial actions over abstract menus
- Make the butler feel present without becoming distracting
- Hide technical complexity behind narrative and delight
- Support both voice and non-voice input paths
- All agent organisational actions must be reversible (one-level undo)
- Lock a clear command grammar early; treat everything else as conversational filler

---

## 11. Open Questions

- Exact visual style direction for both characters (stylised vs semi-realistic)
- Fallback behaviour when no suitable desk surface is detected after a timeout
- Whether to support vertical surfaces (walls) in a later version
- How aggressively the butler should auto-organise by default (currently: only on explicit command)

---

**End of PRD v1.4**
