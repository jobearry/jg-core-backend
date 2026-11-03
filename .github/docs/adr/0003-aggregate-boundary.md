# ADR-0003 — Aggregate boundary for Experience and Project

- **Status:** Accepted · **implemented 2026-10-23**
- **Date:** 2026-10-21 (proposed) · 2026-10-22 (decided) · 2026-10-23 (enforced in code)
- **Related:** [`../domain-model.md`](../domain-model.md), [`../database.md`](../database.md)

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

## Decision

**Project is an independent aggregate. The link to an Experience is optional.**

The product requirement that settles the shape is simple and was confirmed by
the decision owner:

- An experience may have **no** projects.
- A project may exist with **no** experience.

So "a project belongs to exactly one experience" is **not** an invariant of this
domain, and enforcing it would be enforcing a rule the business does not have.

Concretely:

- `Project` is the aggregate root. `IPortfolioProjectRepository` is its write
  entry point; `IPortfolioExperienceRepository` is not.
- `ProjectEntity.ExperienceId` stays **nullable**. A standalone project has
  `ExperienceId == null`. This matches the existing schema, so no migration is
  required.
- `Experience.Projects` is a **read-only projection** — the projects that point at
  this experience — not an owned collection. It is `IReadOnlyList<Project>`,
  populated by the mapping and replaced only through `ReplaceProjects`, so no
  caller can swap or mutate it while the aggregate believes nothing changed.
- Attaching and detaching a project to and from an experience is one explicit
  operation on the `Project` aggregate (`AttachToExperience`), so exactly one
  place owns the relationship. Without it, the two repositories could disagree
  about who a project belongs to.
- `Project.TechStacks` is `IReadOnlyList<TechStackRef>`. A `TechStackRef` is a
  `record` carrying the id, name, image and category: an immutable snapshot,
  identified by id. The category is included because the wire already publishes it
  for a nested stack — dropping it would have been a contract change smuggled into
  a refactor.

**What "enforced" means while there is no write path.** Every repository derives from
`IRepository<T>`, whose only member is `GetAllAsync`; no `Add`/`Update`/`Delete` method
exists anywhere in `src/`. So the boundary is currently enforced by *shape*, not by a
transaction:

- `Experience.Projects` cannot be assigned, and `Project.ExperienceId` changes only
  through `AttachToExperience`.
- `Project.TechStacks` cannot be assigned, and its elements are immutable records.
- `IPortfolioProjectRepository` is named as the future write entry point for `Project`;
  `IPortfolioExperienceRepository` is not. Adding a second one is the thing to refuse in
  review — that is the ambiguity this ADR exists to prevent.

## Consequences

**Positive**

- No migration: the schema already allows a project without an experience.
- Each aggregate has exactly one write entry point, so the "two ways to create a
  Project" ambiguity is gone.
- `TechStack` can be renamed or recategorised without touching projects.
- A project is created the same way whether or not it is later linked to a role.

**Negative / accepted costs**

- "A project belongs to at most one experience" is enforced by the
  application, not by the database. Nothing structurally prevents a second
  writer from setting a different `ExperienceId`.
- The `/experience` endpoint cannot show standalone projects. A standalone
  project appears in `/api/v1/project` only. That is intended, not a defect.
- A project that *is* linked to an experience is loaded twice — once through
  the experience include and once through the project query. The read model
  has to join when it needs both.

**Follow-up work**

- ~~Decide whether `Project.Period` is modelled or deleted~~ — **deleted
  2026-10-22**: it had no column and no mapping, so it was always `null`.
- ~~Add `finished_at` to `experience`~~ — **added and applied**
  `20261022064321_AddExperienceFinishedAt`). The **data backfill is still
  outstanding**: past roles currently report an ongoing duration.
- ~~Replace `Project.TechStacks` with ids or a `TechStackRef` value object~~ —
  **done 2026-10-23**.

## Alternatives considered

| Option | Description | Trade-off |
|---|---|---|
| **A. Experience owns Project for writes** | One write entry point on the experience repository; the project repository is read-only; `project.experience_id` becomes `NOT NULL` with `ON DELETE CASCADE`. | One transaction boundary and "belongs to exactly one experience" enforced by the schema. **Rejected** — it forbids a standalone project, which is a valid thing in this domain, and it requires cleaning orphan rows and a migration to change the FK. |
| **C. Keep both as-is** | No decision. | Two write entry points and no transaction boundary. Rejected: this is the ambiguity that makes the model anemic. |

## Open questions

1. ~~Can a project exist without an experience?~~ **Answered 2026-10-22: yes.**
   That is the basis of this decision.
2. ~~Does the frontend ever display a project outside an experience?~~ **Answered
   2026-10-22: yes** — standalone projects are intended, and `/api/v1/project`
   is the endpoint that serves them.
3. ~~Should `TechStack` be a separate aggregate with its own lifecycle?~~
   **Answered 2026-10-22: yes** — it is shared reference data with no
   per-project state. See plan item 3.4.
