# .github — project knowledge base

Everything an agent (or a new developer) needs beyond the root `AGENTS.md`.

| Folder | Purpose | When to read |
|---|---|---|
| [`rules/`](rules/) | Normative standards: what you **must** do when changing code | Before editing anything |
| [`docs/`](docs/) | Descriptive: how the system works **today** | Before designing a change |
| [`docs/adr/`](docs/adr/) | Architecture decision records: why a choice was made | Before revisiting a decision |
| [`plans/`](plans/) | Active implementation plans with acceptance criteria | Before starting planned work |
| [`skills/`](skills/) | Step-by-step playbooks for recurring tasks | When performing that task |
| [`workflows/`](workflows/) | GitHub Actions CI/CD definitions | When changing build or deploy |

## Rules index

| File | Covers |
|---|---|
| [rules/architecture.md](rules/architecture.md) | Layers, dependency direction, aggregate boundaries, where code belongs |
| [rules/coding-standards.md](rules/coding-standards.md) | C# style, naming, nullability, async, errors, logging, time |
| [rules/testing.md](rules/testing.md) | TDD workflow, test pyramid, what must be tested |
| [rules/api.md](rules/api.md) | Minimal API conventions, status codes, DTOs, OpenAPI, validation |
| [rules/persistence.md](rules/persistence.md) | EF Core conventions, mapping, queries, migrations |
| [rules/security-and-workflow.md](rules/security-and-workflow.md) | Secrets, auth, CORS, branches, commits, PRs |

## Docs index

| File | Covers |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Current and target architecture, request flow |
| [docs/technology-stack.md](docs/technology-stack.md) | Every package, version, purpose |
| [docs/domain-model.md](docs/domain-model.md) | Entities, value objects, invariants, aggregate map |
| [docs/api-reference.md](docs/api-reference.md) | Endpoint catalogue with parameters and responses |
| [docs/database.md](docs/database.md) | Schema, relationships, migration history |
| [docs/operations.md](docs/operations.md) | Local run, configuration, Docker, deployment, runbook |
| [docs/known-issues.md](docs/known-issues.md) | Live defect and debt register |
| [docs/glossary.md](docs/glossary.md) | DDD and project vocabulary |

## Conventions for this folder

- **Rules are normative** ("must", "never"). If reality disagrees with a rule,
  either fix the code or change the rule — do not leave them contradicting.
- **Docs are descriptive** ("is", "does"). Keep them current in the same commit
  as the code change.
- **Plans cover unimplemented work only.** Keep current implementation details
  in `docs/`, and decisions in `docs/adr/`. Remove a plan once its work is
  complete and its resulting state is documented.
- **Active plans are numbered** `NNN-kebab-title.md`, newest number wins, and
  each has a status line. Update the plan's checkboxes as work lands.
- **ADRs are immutable** once Accepted. Supersede rather than edit.
- **Skills are playbooks** with YAML frontmatter (`name`, `description`) so they
  can be loaded by an agent runtime.
