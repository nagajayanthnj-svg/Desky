# Friction Log

Anything that slows you down by **more than 15 minutes** or will hit again goes here: build pain, SDK surprises, unclear requirements, tooling. Log it **while it's happening**, not at the end of the day.

Review every Friday checkpoint. Each open item gets an owner action or is consciously accepted. Recurring friction becomes a task in `DEVELOPMENT_PLAN.md` or a decision in `DECISIONS.md`.

**Severity:** `S1` blocks progress · `S2` costs > 1 h/week · `S3` annoyance

| ID | Date | Task | Friction | Sev | Time lost | Root cause | Action | Status |
|---|---|---|---|---|---|---|---|---|
| F-001 | 2026-10-22 | Repo setup | No .NET SDK / NuGet in the sandbox that generated the repo; `dotnet test` couldn't run there | S3 | 15 min | Restricted build environment | Core verified with Mono `mcs` + an NUnit-compatible shim (138/138 passing). **First thing on your machine: run `scripts/test.sh` to confirm with real NUnit.** | Open → DB-001 |
| F-002 | 2026-10-22 | Planning | The Meta announcement gives dates and the hand-tracking rule but not the demo video length; PRD assumes 3:00 | S2 | — | Rules live on Devpost | Done: under 3:00 is required; storyboard re-timed to 2:45 (PRD v1.6, D-018) | Closed |
| F-003 | 2026-10-22 | DB-012 | Undo replaced item objects, so references held by the demo test (and future Unity views) went stale | S2 | 10 min | `Desk.Put` swapped instances | Fixed: restore state in place (`CopyStateFrom`); regression assert added (D-005) | Closed |
| F-004 | 2026-10-22 | DB-002 | Rules exclude "countries where restrictions apply to banking and/or wire transfers" without naming them; India isn't listed either way | S1 | — | Vague eligibility clause | Email the sponsor via the Devpost page **this week** and get written confirmation that an India-based individual can win a cash prize; also confirm VR Start membership is active | Open → DB-009b |

---

## Template

```
| F-0xx | YYYY-MM-DD | DB-xxx | What happened, in one sentence | S1/S2/S3 | 45 min | Why it happened | What we'll do about it | Open/Closed |
```
