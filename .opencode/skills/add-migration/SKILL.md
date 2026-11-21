---
name: add-migration
description: Safely add an EF Core migration to the JDB Portfolio API — update entities and fluent config, generate the migration, review it, apply it, and document it. Use when the database schema must change, such as adding finished_at to experience or a CHECK constraint.
---

# Add a migration

## Preconditions

- `dotnet-ef` available — run `dotnet tool restore` from the repo root. The manifest
  is `dotnet-tools.json` at the root and pins `dotnet-ef` 10.0.10.
- A reachable PostgreSQL instance and a valid `ConnectionStrings:JDBConnection`.
- You have approval: schema changes are on the "ask first" list.

## Steps

### 1. Change the persistence model

Edit `src/Portfolio/Shared/Persistence/Entities/*Entity.cs` and the fluent
configuration in `src/Portfolio/Shared/Persistence/Configurations/`.

Only touch domain-oriented code if a domain concept actually changed. A column
that exists only for persistence does not belong in the domain model.

### 2. Generate

```bash
dotnet ef migrations add <DescriptiveName> \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/Portfolio/Portfolio.csproj
```

Name it for the change: `AddFinishedAtToExperience`, not `UpdateSchema`.

### 3. Review the generated files

Open `<timestamp>_<Name>.cs` and check:

- [ ] `Up` does what you intended — no surprise `DropColumn`.
- [ ] `Down` actually reverses `Up`.
- [ ] A rename was not generated as drop + add (data loss).
- [ ] Default values are set for non-nullable additions to a populated table.
- [ ] `PortfolioContextModelSnapshot.cs` is updated.
- [ ] No secrets in the migration.

If the migration is wrong and **not yet applied anywhere**, delete it with
`dotnet ef migrations remove` and regenerate. Once applied or merged, add a new
migration instead.

### 4. Apply

```bash
dotnet ef database update \
  --project src/Portfolio/Portfolio.csproj \
  --startup-project src/Portfolio/Portfolio.csproj
```

### 5. Update documentation

- `docs/database.md`: table/column reference and migration history.
- `docs/known-issues.md`: tick off any gap it closes.
- Domain docs if the model changed.

## Verification

```bash
dotnet ef migrations list --project src/Portfolio/Portfolio.csproj --startup-project src/Portfolio/Portfolio.csproj
dotnet build JGBackend.sln
```

Then bring the API up and hit an endpoint that reads the changed table.

## Troubleshooting

| Symptom | Cause |
|---|---|
| "The DbContext type was not found" | wrong `--project` / `--startup-project` |
| null connection string at design time | User Secrets are missing or the ID differs from `Portfolio.csproj`; pass `ConnectionStrings__JDBConnection` or `--connection` |
| migration generated but app still fails | the app also calls `Migrate()` at startup; check the connection string it actually uses |
| `relation does not exist` | the migration was generated but not applied |
