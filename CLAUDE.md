# Instructions for AI coding assistants

- Read `docs/DEVELOPMENT_PLAN.md` §2–3 before changing code. Work on one task ID at a time.
- **TDD is mandatory for core:** add or extend the failing NUnit test first, then the code. Run `scripts/test.sh`.
- Product rules live in `packages/com.deskbutler.core/Runtime` (no `UnityEngine`, C# ≤ 9, NUnit 3 classic asserts). Unity code only adapts.
- New voice command: follow DEVELOPMENT_PLAN §3.3 exactly; both personalities need catalog lines.
- Never let an LLM choose actions (D-004). Never persist idle offsets (D-006). Every desk-changing command goes through `DeskAgent.Mutate` so it is undoable (D-005).
- If you make a choice between real options, append a D-entry to `docs/DECISIONS.md`. If something blocks you for > 15 min, append an F-entry to `docs/FRICTION_LOG.md`.
- Commit format: `type(scope): summary [DB-0xx]`.
