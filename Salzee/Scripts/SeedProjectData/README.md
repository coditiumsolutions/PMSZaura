# Seed Project Data

Seeds **100 Customers** and **100 Properties** per project for dashboard reports and testing.

## What it does

1. **Lists all projects** in the database (ProjectID, Name, Prefix, current Customer count, Property count).
2. For **each project**:
   - Ensures at least one Payment Plan exists (creates a default plan if none).
   - Adds **100 Customers** (with that project’s PlanID and ProjectID).
   - Adds **100 Properties** (with that project’s ProjectID, Status = Available).

## Usage

From this folder:

```powershell
dotnet run
```

Or with a custom connection string (e.g. different server or database name):

```powershell
dotnet run "Server=172.20.229.3;Database=PMS;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
```

- Default connection string if no argument:  
  `Server=localhost;Database=PMS;User Id=sa;Password=Pakistan@786;Encrypt=Mandatory;TrustServerCertificate=true;`
- Use your actual server, database, and password in the connection string as needed.

## Requirements

- Database schema must match the application (e.g. tables **Projects**, **Customers**, **Property**, **PaymentPlan** with columns as in `db.txt`).
- If you see errors like **Invalid column name 'ProjectID'** (or similar), run Entity Framework migrations against this database so the schema matches the models, or align the database with the schema described in `db.txt`.

## Output

- Prints all projects with current counts.
- For each project, prints how many customers and properties were added.
- Final totals: total customers added and total properties added.
