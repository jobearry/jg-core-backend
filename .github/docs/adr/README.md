# Architecture Decision Records

An ADR records **why** a structural choice was made, so it is not re-litigated
without context.

| # | Decision | Status |
|---|---|---|
| [0001](0001-five-project-clean-architecture.md) | Five-project clean-architecture layout | Accepted |
| [0002](0002-separate-persistence-and-domain-models.md) | Persistence entities separate from domain model | Accepted |
| [0003](0003-aggregate-boundary.md) | Aggregate boundary for Experience and Project | **Proposed** |
| [0004](0004-dto-boundary-at-api-edge.md) | DTO boundary at the API edge | **Proposed** |

## Statuses

- **Proposed** — under discussion; do not treat as settled.
- **Accepted** — implemented, or being implemented; changes require a new ADR.
- **Superseded by NNNN** — kept for history, no longer in force.
- **Deprecated** — no longer relevant.

## Writing one

1. Copy [`template.md`](template.md) to `NNNN-kebab-title.md`.
2. Fill in Context (the forces), Decision (the choice), Consequences (what it
   costs), Alternatives (what was rejected and why).
3. Open a PR. An ADR that nobody reviewed is just a note.

Rules:

- An accepted ADR is **immutable**. Supersede it with a new one; do not rewrite
  history.
- If the code contradicts the ADR, one of them is wrong — fix it in the same PR.
- Keep it short. If it needs more than two pages, it is probably two decisions.
