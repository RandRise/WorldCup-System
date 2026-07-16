# WorldCup System — Phase 9 Ops Hardening & Tournament Close-Out

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 9 — Ops Hardening & Tournament Close-Out

Opened **17 Jul 2026** from the former “What’s left” ops list after Phases 1–8 product work. [← Back to Changes Review](../changes-review.md) · [Roadmap Phase 9](../roadmap.md#phase-9)

> **Info:** **In progress.** Final scheduled; migration/Feeder repair confirmed; knockout re-advance UX shipped. Remaining: record Final after 19 Jul FT, commit Phase 7–8 when asked.

### Phase 9 Roadmap Mapping

| Task | Status | Notes |
| --- | --- | --- |
| Schedule Final (Argentina vs Spain, 19 Jul) | Done | `add_sf2_and_final.py` — MatchId **116** (Argentina vs Spain, MetLife) |
| Record Final result after FT | Open | After 19 Jul — Admin live console or extend import script |
| Optional ThirdPlace match | Skipped | Not needed for this tournament path |
| Apply `AddMatchStage` + Feeder* repair | Done | Migration recorded; Feeder* columns present; repair runs on API start |
| Commit Phase 7–8 working tree | Open | Ask when ready to commit |
| Knockout re-advance UX | Done | Goal API warning + Schedule **Advance winner** |

### Knockout re-advance UX

- `GoalService.AddGoal` / `DeleteGoal` return `string?` advance-conflict warning (goal still saved)
- `GoalController` Ok body includes `Warning: …` when advance fails
- Live console shows a warning banner for those responses
- Admin Schedule: **Advance winner** on finished knockout matches → `Knockout/AdvanceFromMatch/{id}` (surfaces destination-has-events errors)

### Final after FT (deferred)

1. Admin live console on Match **116**, or
2. Extend `import_wc2026_finished_matches.py` with the FT scoreline and re-run / upsert

### Related

- [WC 2026 live data](wc2026-live-data.md)
- [Phase 7 bugfixes](phase-7-bugfixes.md)
- [Phase 8 dashboards](phase-8-dashboards.md)
