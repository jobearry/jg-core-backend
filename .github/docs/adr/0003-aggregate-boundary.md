# ADR-0003 — Aggregate boundary for Experience and Project

- **Status:** Accepted
- **Date:** 2026-10-21 (proposed) · 2026-10-22 (decided — plan 001 decision D3)
- **Related:** plan 001 items 3.4 and D3, [`../domain-model.md`](../domain-model.md), [`../database.md`](../database.md)

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
- `Experience.Projects` becomes a **read-only projection** — the projects that
  point at this experience — not an owned collection. It is populated by the
  mapping and never mutated by callers. It is currently
  `List<Project>?` with a public setter (`Domain/Entities/Experience.cs:22`).
- Attaching and detaching a project to and from an experience is one explicit
  operation on the `Project` aggregate, so exactly one place owns the
  relationship. Without it, the two repositories could disagree about who a
  project belongs to.
- `Project.TechStacks` becomes `IReadOnlyCollection<TechStackRef>` (id + name +
  image) or a list of ids resolved by the read model. `TechStack` is shared
  reference data with no per-project state, so it is not part of any aggregate
  that contains a project.

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

- Decide whether `Project.Period` is modelled (add columns) or deleted — plan
  item 1.4.
- Add `finished_at` to `experience` (plan item 1.3) — an owned project's period
  is currently meaningless without it.
- Replace `Project.TechStacks` with ids or a `TechStackRef` value object — plan
  item 3.4.

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
