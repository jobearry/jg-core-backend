# ADR-0003 — Aggregate boundary for Experience and Project

- **Status:** **Proposed** — needs a decision before plan 3.3/3.4 starts
- **Date:** 2026-10-21
- **Related:** plan 3.4, [`../domain-model.md`](../domain-model.md), [`../database.md`](../database.md)

## Context

The current model has three overlapping boundaries:

1. `Experience` owns `List<Project>` — projects are loaded as part of the
   experience aggregate (`GetAllExperienceWithProjectsAsync`).
2. `Project` is **also** an aggregate root: `IPortfolioProjectRepository`
   returns it independently (`GetAllProjectsWithTechStacksAsync`), and
   `/api/v1/project` exists as a top-level resource.
3. `Project.TechStacks` holds `TechStack` **entity instances**, but `TechStack`
   is shared reference data also returned by `/api/v1/skill` and referenced by
   many projects.

The database reinforces the contradiction: `project.experience_id` is nullable
with `ON DELETE SET NULL`, which treats a project as a thing that can outlive
its experience — the opposite of "owned by".

There are no write endpoints yet, so this decision is cheap now and expensive
later.

## Decision (recommended)

**Experience owns Project for writes. Project remains independently readable.**

Concretely:

- One write entry point: `EfPortfolioExperienceRepository` (or an
  `IExperienceRepository` that replaces both) is the only thing that creates,
  updates, or deletes projects.
- `IPortfolioProjectRepository` becomes **read-only** and exists to serve the
  `/project` endpoint. It must not expose write methods.
- `Project` references `Experience` by value (`ExperienceId`), not by object
  reference. `Experience.Projects` is a controlled collection with
  `AddProject` / `RemoveProject` behaviour.
- `Project.TechStacks` becomes `IReadOnlyCollection<TechStackRef>` (id + name +
  image) or a list of ids resolved by the read model. `TechStack` entities are
  not part of the Project aggregate.
- `project.experience_id` becomes `NOT NULL` with `ON DELETE CASCADE` once the
  data allows it, or the ownership claim is dropped.

## Consequences

**Positive**

- One place to enforce "a project belongs to exactly one experience".
- One transaction boundary: saving an experience saves its projects atomically.
- `TechStack` can be renamed or recategorised without touching projects.

**Negative / accepted costs**

- The `/project` endpoint needs a read model that joins experience → project →
  tech stacks; it can no longer rely on the aggregate.
- `Project` can no longer be created independently, which changes the API shape
  if a write endpoint is added for a standalone project.
- Migrating the FK to `NOT NULL` requires cleaning orphan rows first.

**Follow-up work**

- Decide whether `Project.Period` is modelled (add columns) or deleted.
- Add `finished_at` to `experience` (plan 1.3) — an owned project's period is
  currently meaningless without it.

## Alternatives considered

| Option | Description | Trade-off |
|---|---|---|
| **B. Project is an independent aggregate** | `Project` has its own repository (read + write), `Experience` holds only `ExperienceId` back-references. `Experience.Projects` becomes a read-model projection. | Simpler CRUD and matches the current database FK. But "a project must belong to an experience" becomes unenforceable, and the experience endpoint needs a separate query. |
| **C. Keep both as-is** | No decision. | Two write entry points and no transaction boundary. Rejected: this is the ambiguity that makes the model anemic. |

## Open questions for the decision owner

1. Can a project exist without an experience? (Product answer, not a technical
   one.)
2. Does the frontend ever display a project outside an experience? If never, the
   `/project` endpoint is redundant and option B loses its main advantage.
3. Should `TechStack` be a separate aggregate with its own lifecycle? It is
   currently shared reference data with no per-project state.
