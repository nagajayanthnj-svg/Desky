# Desk Butler

A hands-first mixed reality productivity companion for Meta Quest. A small AI butler (Alden or Pip) lives on your real desk, organises notes, finds things, and keeps you focused.

Entry for the **Meta VR Start Developer Competition 2026**, Productivity track. Submission deadline is 18 Nov 2026; we submit on 17 Nov.

## Where things are

| Path | What |
|---|---|
| `docs/PRD.md` | Product requirements v1.5 (locked; change via a decision) |
| `docs/DEVELOPMENT_PLAN.md` | Timeline, TDD methodology, task board, device test scripts, risks, cut line |
| `docs/DECISIONS.md` | Decision log (D-xxx) |
| `docs/FRICTION_LOG.md` | Friction log (F-xxx) |
| `packages/com.deskbutler.core/` | Pure C# core as a Unity package: all product rules + NUnit tests |
| `dotnet/` | Project files to run core tests outside Unity |
| `unity/DeskButler/` | Unity project (created in task DB-003) |

## Run the tests

```bash
scripts/test.sh                       # all core tests (needs .NET 8 SDK)
scripts/test.sh --filter DeskAgent    # one area
```

CI runs the same thing on every push (`.github/workflows/core-tests.yml`).

In Unity, after DB-003: **Window → General → Test Runner → EditMode**. The core package is listed under `testables` in `Packages/manifest.json`.

## How we work (short version)

1. Pick the top unblocked task in the plan's task board, then branch `task/DB-0xx-slug`.
2. Write the card's tests first, make them pass, refactor.
3. Product rules go in core, never in MonoBehaviours.
4. Log decisions (D-) and friction over 15 minutes (F-) as they happen.
5. Commit with `type(scope): summary [DB-0xx]`, merge the same day, tick the board.

The full rules, Definition of Done and the device checklist are in `docs/DEVELOPMENT_PLAN.md`.
