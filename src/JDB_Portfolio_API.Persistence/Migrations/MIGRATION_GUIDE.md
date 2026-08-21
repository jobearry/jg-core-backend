# Database Migrations Guide

This guide provides instructions for managing database migrations for the JDB Portfolio API using Entity Framework Core with PostgreSQL.

## Prerequisites

Ensure you have the **Entity Framework Core CLI tools** installed:

```powershell
dotnet tool install --global dotnet-ef
```

To update the global tool:

```powershell
dotnet tool update --global dotnet-ef
```

---

## Creating a New Migration

When you make changes to your Entity Framework models, you need to create a new migration to capture those changes.

### Command

```powershell
dotnet ef migrations add <MigrationName> --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
```

### Example

```powershell
dotnet ef migrations add AddUserTable --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
```

This command will:
- Analyze the changes in your DbContext models
- Generate a new migration file in the `Migrations` folder
- Create `<Timestamp>_<MigrationName>.cs` and `<Timestamp>_<MigrationName>.Designer.cs` files

### Best Practices

- Use clear, descriptive migration names (e.g., `AddUserAuthenticationFields`, `RemoveDeprecatedColumn`)
- Keep migrations focused on a single logical change
- Review the generated migration files before applying them

---

## Updating the Database

Apply pending migrations to your database.

### Command

```powershell
dotnet ef database update --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
```

This command will:
- Execute all pending migrations in chronological order
- Update the database schema to match your models
- Apply changes only if the database is running and connection string is valid

---

## Reverting to a Previous Migration

If you need to roll back to a specific migration (useful if something goes wrong).

### Command

```powershell
dotnet ef database update <TargetMigrationName> --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
```

### Example

```powershell
dotnet ef database update 20260808053318_InitialCreate --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
```

### To Remove the Latest Migration

```powershell
dotnet ef migrations remove --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
```

---

## Viewing Migration History

View all applied migrations in the database.

### Command

```powershell
dotnet ef migrations list --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
```

This will display:
- ✔ Applied migrations
- ○ Pending migrations

---

## Common Scenarios

### Scenario 1: Added a New Property to an Entity

1. Add the property to your Entity class
2. Create a migration:
   ```powershell
   dotnet ef migrations add AddPropertyNameToEntityName --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
   ```
3. Review the generated migration file
4. Update the database:
   ```powershell
   dotnet ef database update --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
   ```

### Scenario 2: Changed a Property Type

1. Update the property type in your Entity class
2. Create a migration:
   ```powershell
   dotnet ef migrations add ChangePropertyTypeInEntity --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
   ```
3. Review and adjust the migration if needed (some type changes may require manual data conversion)
4. Update the database:
   ```powershell
   dotnet ef database update --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
   ```

### Scenario 3: Removed a Property

1. Remove the property from your Entity class
2. Create a migration:
   ```powershell
   dotnet ef migrations add RemovePropertyFromEntity --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
   ```
3. Update the database:
   ```powershell
   dotnet ef database update --project JDB_Portfolio_API.Persistence --startup-project JDB_Portfolio_API
   ```

---

## Troubleshooting

### Issue: "No migrations were applied"

**Cause:** There are no pending migrations.

**Solution:** Create a new migration if you've made changes to your models.

### Issue: "Cannot connect to database"

**Cause:** The connection string in `appsettings.json` or environment variables is invalid.

**Solution:** Verify your `JDBConnection` connection string in your configuration.

### Issue: "The DbContext type was not found"

**Cause:** Incorrect project names in the command.

**Solution:** Verify the `--project` and `--startup-project` values match your actual project names.

### Issue: "A migration with name 'X' already exists"

**Cause:** You're trying to create a migration with a name that already exists.

**Solution:** Use a different migration name.

---

## Database Connection Strings

Ensure your `appsettings.json` contains the correct PostgreSQL connection string:

```json
{
  "ConnectionStrings": {
	"JDBConnection": "Host=localhost;Port=5432;Database=jdb_portfolio;Username=postgres;Password=your_password"
  }
}
```

For Azure deployments, use environment variables to set the connection string securely.

---

## Additional Resources

- [Entity Framework Core Migrations Documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Connection Strings](https://www.npgsql.org/doc/connection-string-parameters.html)
- [Entity Framework Core Tools](https://docs.microsoft.com/en-us/ef/core/cli/)
