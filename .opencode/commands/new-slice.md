---
description: Add a new Portfolio feature end to end
---

Add a portfolio feature named $ARGUMENTS.

Follow the nearest `AGENTS.md` and `.opencode/rules/`. Place the feature under
`src/Portfolio/Features/<Name>/`; only genuinely shared code belongs in
`src/Portfolio/Shared/`. Register it from `Features/DependencyInjection.cs`.

Requirements:

- Endpoint returns a DTO, never a domain entity.
- Add or extend a repository port only if the query needs new data.
- Use typed errors (`Result<T>`), not strings.
- Pass `CancellationToken` through every I/O call.
- Add a test in `tests/Portfolio/` that failed before the change.
- Update `docs/api-reference.md` and Swagger metadata.

Do not add packages, change the schema, or apply migrations without asking.
