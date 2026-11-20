# Project knowledge base

Everything an agent or a new developer needs beyond the root `AGENTS.md`.

| Path | Purpose | When to read |
|---|---|---|
| [`.opencode/rules/`](../.opencode/rules/) | Normative standards: what you **must** do when changing code | Before editing anything |
| [`docs/`](.) | Descriptive: how the system works **today** | Before designing a change |
| [`docs/adr/`](adr/) | Architecture decision records: why a choice was made | Before revisiting a decision |
| [`docs/plan.md`](plan.md) | The sole shared plan for all harnesses | Only when the user explicitly asks |
| [`.opencode/skills/`](../.opencode/skills/) | Step-by-step playbooks for recurring tasks | When performing that task |
| [`.github/workflows/`](../.github/workflows/) | GitHub Actions CI/CD definitions | When changing build or deploy |

## Rules index

| File | Covers |
|---|---|
| [architecture.md](../.opencode/rules/architecture.md) | Layers, dependency direction, aggregate boundaries, where code belongs |
| [coding-standards.md](../.opencode/rules/coding-standards.md) | C# style, naming, nullability, async, errors, logging, time |
| [testing.md](../.opencode/rules/testing.md) | TDD workflow, test pyramid, what must be tested |
| [api.md](../.opencode/rules/api.md) | Minimal API conventions, status codes, DTOs, OpenAPI, validation |
| [persistence.md](../.opencode/rules/persistence.md) | EF Core conventions, mapping, queries, migrations |
| [security-and-workflow.md](../.opencode/rules/security-and-workflow.md) | Secrets, auth, CORS, branches, commits, PRs |
| [terminal-commands.md](../.opencode/rules/terminal-commands.md) | Which commands are routine and which need approval |

## Docs index

| File | Covers |
|---|---|
| [architecture.md](architecture.md) | Current and target architecture, request flow |
| [technology-stack.md](technology-stack.md) | Every package, version, purpose |
| [domain-model.md](domain-model.md) | Entities, value objects, invariants, aggregate map |
| [api-reference.md](api-reference.md) | Endpoint catalogue with parameters and responses |
| [database.md](database.md) | Schema, relationships, migration history |
| [operations.md](operations.md) | Local run, configuration, Docker, deployment, runbook |
| [known-issues.md](known-issues.md) | Live defect and debt register |
| [glossary.md](glossary.md) | DDD and project vocabulary |

## Conventions for this folder

- **Rules are normative** ("must", "never"). If reality disagrees with a rule,
  either fix the code or change the rule — do not leave them contradicting.
- **Docs are descriptive** ("is", "does"). Keep them current in the same commit
  as the code change.
- Do not create plans, checklists, roadmaps, or other planning artifacts unless
  the user explicitly asks. Do not infer permission to plan from a task request.
- `docs/plan.md` is the only plan location and is shared by every harness.
  Do not create numbered plans or harness-specific copies.
- Read, modify, or execute `docs/plan.md` only when the user explicitly asks.
  The existence of a plan is not authorization to execute it.
- Keep current implementation details in `docs/` and lasting decisions in
  `docs/adr/`.
- **ADRs are immutable** once Accepted. Supersede rather than edit.
- **Skills are playbooks** with YAML frontmatter (`name`, `description`) so they
  can be loaded by an agent runtime.
