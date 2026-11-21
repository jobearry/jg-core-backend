# Persistence rules (EF Core + PostgreSQL)

## 1. Entity ≠ domain, always

| | Domain entity | Persistence entity |
|---|---|---|
| Lives in | `Domain/Entities` | `Persistence/Entities` |
| Represents | a concept with invariants | a table row |
| Has | private setters, behaviour | public setters, `= null!` |
| Knows about | nothing external | EF conventions, column names |
| Naming | `Project` | `ProjectEntity` |

Rules:

- Never let a `Persistence.Entities` type cross into Application or Presentation.
- Never let EF materialise a Domain type. Domain types are constructed by the
  mapping profile (or a factory), so their invariants always run.
- No EF attributes in Domain. Fluent configuration only, in
  `Persistence/Configurations` as `IEntityTypeConfiguration<T>` classes.

## 2. Schema conventions

Observed and to be preserved:

- Table names: **singular**, `snake_case` (`experience`, `project`,
  `recommendation`, `tech_stack`, `tech_stack_category`, `project_tech_stack`).
- Column names: `snake_case`.
- Primary keys: `id`, `UseIdentityAlwaysColumn()`.
- Constraint names: `<table>_pkey`, `FK_<table>_<column>`.
- Join table: `project_tech_stack` with a composite key
  `(project_id, tech_stack_id)`.
- Timestamps: prefer `timestamptz` for new columns. Existing columns are
  `timestamp without time zone` — a known hazard (see `docs/known-issues.md`).

## 3. Queries

- **Reads** use `.AsNoTracking()`. Nothing in this API writes.
- Multiple sibling collections use `.AsSplitQuery()` to avoid cartesian
  explosion (the experience query includes 4 levels).
- Prefer projecting straight to a DTO (`Select(...)`) for read models; it avoids
  loading columns you discard and sidesteps the mapping profile entirely.
- No client-side evaluation: everything before `ToListAsync` must translate to
  SQL.
- Every query accepts and passes `CancellationToken`.

## 4. Mapping

Mappers in `src/Portfolio/Shared/Persistence/Mappers/` are the riskiest code in
the solution.

- Map value objects **explicitly** (`Role.Value`, `Period.Started`, etc.). Never
  rely on `ToString()` or an implicit conversion.
- Every used direction needs a runtime mapping test.
- A domain → entity mapping that is never used should be **deleted**, not left
  broken.
- AutoMapper is gone. Do not reintroduce it without an ADR.

## 5. Migrations

```bash
dotnet ef migrations add <DescriptiveName> \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/Portfolio/Portfolio.csproj

dotnet ef database update \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/Portfolio/Portfolio.csproj
```

Rules:

- **Never edit an applied migration.** Add a new one. (The one exception:
  migrations not yet merged to `main`.)
- One logical change per migration. Name it for the change, not the date:
  `AddFinishedAtToExperience`, not `UpdateSchema` (there is already an
  `UpdateSchema` in history).
- Always read the generated `Up`/`Down` before committing. EF guesses wrong on
  renames (it drops + adds, losing data).
- Never commit a migration without the matching model snapshot.
- `Program.cs` calls `Database.Migrate()` at startup when
  `Database:AutoMigrate` is enabled. That is fine for one instance and wrong for
  many; disable it when migrations are applied out of band.
- The design-time factory uses the Portfolio project's `UserSecretsId` to load
  the same User Secrets store; keep the ID in `PortfolioContextFactory`
  synchronized with `src/Portfolio/Portfolio.csproj`.

## 6. Configuration

- Connection string key: `ConnectionStrings:JDBConnection`.
- Development: User Secrets on `src/Portfolio/Portfolio.csproj`; the shared
  project ID is used by the design-time factory.
- Production: environment variable
  `ConnectionStrings__JDBConnection` (double underscore).
- Never commit a connection string, even a localhost one.

## 7. Transactions and writes

- There is no write path today and no `IUnitOfWork`. Before adding one, read
  ADR-0003 — the aggregate boundary decides where the transaction lives.
- A write must save exactly one aggregate per transaction. If you need two,
  that is a domain event or a saga, not a bigger transaction.

## 8. Testing persistence

- Use **Testcontainers PostgreSQL**, not the EF in-memory provider: in-memory
  does not enforce `NOT NULL`, `CHECK`, or PostgreSQL type semantics.
- Assert on behaviour that only a real database can prove: constraints, defaults,
  `ON DELETE SET NULL`, identity columns.
