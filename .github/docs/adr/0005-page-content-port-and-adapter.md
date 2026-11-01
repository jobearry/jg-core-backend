# ADR-0005 — The page-content port lives in Application, the adapter in Presentation

- **Status:** Accepted
- **Date:** 2026-10-23
- **Related:** [KI-045](../known-issues.md), [ADR-0001](0001-five-project-clean-architecture.md), [ADR-0004](0004-dto-boundary-at-api-edge.md)

## Context

`Domain` contained two vendor types that nothing in the model used:

- `Domain.ValueObjects.NotionPage` — a mutable class with public setters, an empty-string
  default on every property, and no validation.
- `Domain.Interfaces.INotionService` — a port returning `List<NotionPage>`.

They were there for one reason. `ExternalService` implements the Notion call and may only
reference `Domain`, so the port had to be in `Domain` for the implementation to reach it.
Keeping the vendor vocabulary in the core was the price of that convenience (KI-045).

`Application` is the layer that actually calls the port (`PortfolioService.GetNotionPageAsync`).
The domain never does. So the port was in the wrong place, and moving it was blocked by
AGENTS §6 rule 2 — "infrastructure projects must never reference `Application`".

## Decision

**The port and its model live in `Application`. The adapter lives in the composition root.**

- `Application/Abstractions/IPageContentReader.cs` — `ReadPageAsync` returning
  `IReadOnlyList<PageContent>`.
- `Application/Models/PageContent.cs` — `sealed record PageContent(string Url, string Date, string Title)`.
- `ExternalService/Notion/Models/NotionPage.cs` — the vendor model, moved here. The client
  implements no interface.
- `Presentation/Adapters/NotionPageContentReader.cs` — implements the port by delegating to
  `NotionClientService` and mapping field by field. It is the only project that references
  both `Application` and `ExternalService`, which is a property of being the composition
  root, not a new coupling.
- `Domain` loses both types and becomes entirely vendor-free.

Rule 2 is **unchanged**: `ExternalService` still references `Domain` only. No csproj
reference was added or removed anywhere.

## Consequences

**Positive**

- The domain model contains no vendor vocabulary and no mutable, unvalidated type.
- `Application` declares the interface it uses, which is the normal direction.
- The wire contract is untouched: same routes, same `NotionPageDto` name, same keys, same
  `notion_unavailable` / `notion_invalid_response` codes.

**Negative / accepted costs**

- One behavioural class sits in the composition root. That is unusual enough to need this
  ADR: a future reader who sees `Adapters/` should not conclude that rule 2 was bent.
- There are two similar shapes (`PageContent` and `NotionPageDto`). They are not redundant:
  one is the internal model, the other the published contract, and they are allowed to
  diverge — the DTO would keep its name even if the source stopped being Notion.
- The adapter is the only untested-by-endpoint-tests seam, because the endpoint tests
  replace the port. `NotionPageContentReaderTests` covers it with a stub
  `HttpMessageHandler`, including that an upstream failure escapes with its original type
  so the 502 classification still works.

## Alternatives considered

| Option | Description | Trade-off |
|---|---|---|
| **Rename in `Domain`** (`IPageContentReader`, `PageContent`) | Keep the port where it is, drop the vendor name | Cheapest, but the core still owns an integration port nothing in the model calls — a neutral name over the same misplaced concept |
| **Let `ExternalService` reference `Application`** | Move the port, change one csproj line | Architecturally textbook, but amends a rule the owner set deliberately; rejected without reopening AGENTS §6 |
| **Keep both as-is** | Status quo | Domain names a vendor, and `NotionPage` stays the only mutable, unvalidated domain type |
