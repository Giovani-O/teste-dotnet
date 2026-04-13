# Milestone 1: Foundation & Domain Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Set up the .NET 10 Web API project with the proper folder structure, domain model, EF Core data layer, and custom exception types as defined in the SDD.

**Architecture:** Flat layered architecture in a single .NET project (Contacts.API) with a separate test project (Contacts.Tests). Layers enforced by convention: Controllers → Services → Repositories → DB. Cross-cutting concerns handled by middleware.

**Tech Stack:** .NET 10, C#, Entity Framework Core, SQL Server, Swashbuckle (Swagger), xUnit, NSubstitute

---

### Task 0: Create .gitignore

**Files:**
- Create: `.gitignore`

- [ ] **Step 1: Create .gitignore with standard .NET and environment file exclusions**

```
# .NET build artifacts
bin/
obj/
*.user
*.suo
.vs/
*.ncrunchproject

# NuGet
*.nupkg
*.nuget.targets
project.lock.json
project.assets.json
packages/

# Rider
.idea/
*.sln.iml

# VS Code
.vscode/
!.vscode/settings.json
!.vscode/tasks.json
!.vscode/launch.json
!.vscode/extensions.json

# Environment / secrets
.env
.env.*
!.env.example
appsettings.Development.json
*.pfx
*.p12
*.key

# OS
.DS_Store
Thumbs.db
desktop.ini

# Test results
TestResults/
*.trx
coverage*.xml
*.coveragexml
*.opencover.xml

# Entity Framework migrations (keep files, but not snapshots from untracked dbs)
# Keep: Migrations/*.cs

# Logs
*.log
logs/
```

- [ ] **Step 2: Verify .gitignore is at workspace root**

Run: `ls /home/murasaki/Documents/projects/cadmus/teste-dotnet/.gitignore`
Expected: file exists

---

### Task 1.1: Create .NET 10 Web API project and folder structure

**Files:**
- Create: `Contacts.API/Contacts.API.csproj`
- Create: `Contacts.API/Program.cs`
- Create: `Contacts.Tests/Contacts.Tests.csproj`
- Create: `ContactsApi.sln`
- Create: `Contacts.API/Controllers/.gitkeep`
- Create: `Contacts.API/Data/.gitkeep`
- Create: `Contacts.API/DTOs/.gitkeep`
- Create: `Contacts.API/Exceptions/.gitkeep`
- Create: `Contacts.API/Mappers/.gitkeep`
- Create: `Contacts.API/Middleware/.gitkeep`
- Create: `Contacts.API/Migrations/.gitkeep`
- Create: `Contacts.API/Models/.gitkeep`
- Create: `Contacts.API/Repositories/Interfaces/.gitkeep`
- Create: `Contacts.API/Services/Interfaces/.gitkeep`
- Create: `Contacts.API/Types/.gitkeep`
- Create: `Contacts.Tests/Unit/Services/.gitkeep`
- Create: `Contacts.Tests/E2E/Controllers/.gitkeep`
- Create: `.env.example`

- [ ] **Step 1: Create the Web API project**

Run:
```bash
cd /home/murasaki/Documents/projects/cadmus/teste-dotnet
dotnet new webapi -n Contacts.API --framework net10.0 --no-openapi
```

- [ ] **Step 2: Create the test project**

Run:
```bash
cd /home/murasaki/Documents/projects/cadmus/teste-dotnet
dotnet new xunit -n Contacts.Tests --framework net10.0
```

- [ ] **Step 3: Create the solution and add projects**

Run:
```bash
cd /home/murasaki/Documents/projects/cadmus/teste-dotnet
dotnet new sln -n ContactsApi
dotnet sln ContactsApi.sln add Contacts.API/Contacts.API.csproj
dotnet sln ContactsApi.sln add Contacts.Tests/Contacts.Tests.csproj
dotnet add Contacts.Tests/Contacts.Tests.csproj reference Contacts.API/Contacts.API.csproj
```

- [ ] **Step 4: Add NuGet packages to API project**

Run:
```bash
cd /home/murasaki/Documents/projects/cadmus/teste-dotnet
dotnet add Contacts.API/Contacts.API.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Contacts.API/Contacts.API.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add Contacts.API/Contacts.API.csproj package Swashbuckle.AspNetCore
```

- [ ] **Step 5: Add NuGet packages to test project**

Run:
```bash
cd /home/murasaki/Documents/projects/cadmus/teste-dotnet
dotnet add Contacts.Tests/Contacts.Tests.csproj package NSubstitute
dotnet add Contacts.Tests/Contacts.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add Contacts.Tests/Contacts.Tests.csproj package Microsoft.EntityFrameworkCore.SqlServer
```

- [ ] **Step 6: Create folder structure and .env.example**

```
CONTACTS_DB_CONNECTION=Server=localhost,1433;Database=ContactsDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
CONTACTS_TEST_DB_CONNECTION=Server=localhost,1434;Database=ContactsTestDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
```

Create directories:
- `Contacts.API/Controllers/`
- `Contacts.API/Data/`
- `Contacts.API/DTOs/`
- `Contacts.API/Exceptions/`
- `Contacts.API/Mappers/`
- `Contacts.API/Middleware/`
- `Contacts.API/Migrations/`
- `Contacts.API/Models/`
- `Contacts.API/Repositories/Interfaces/`
- `Contacts.API/Services/Interfaces/`
- `Contacts.API/Types/`
- `Contacts.Tests/Unit/Services/`
- `Contacts.Tests/E2E/Controllers/`

- [ ] **Step 7: Verify project builds**

Run: `dotnet build /home/murasaki/Documents/projects/cadmus/teste-dotnet/ContactsApi.sln`
Expected: Build succeeded, 0 errors

---

### Task 1.2: Implement Gender enum and Contact model

**Files:**
- Create: `Contacts.API/Types/Gender.cs`
- Create: `Contacts.API/Models/Contact.cs`

- [ ] **Step 1: Create Gender enum**

```csharp
// Contacts.API/Types/Gender.cs
namespace Contacts.API.Types;

public enum Gender
{
    Male,
    Female,
    Other
}
```

- [ ] **Step 2: Create Contact model**

```csharp
// Contacts.API/Models/Contact.cs
using Contacts.API.Types;

namespace Contacts.API.Models;

public class Contact
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Calculated — not persisted
    public int Age =>
        DateTime.Today.Year - Birthdate.Year -
        (DateTime.Today < new DateTime(DateTime.Today.Year, Birthdate.Month, Birthdate.Day) ? 1 : 0);
}
```

- [ ] **Step 3: Verify build still passes**

Run: `dotnet build /home/murasaki/Documents/projects/cadmus/teste-dotnet/ContactsApi.sln`
Expected: Build succeeded, 0 errors

---

### Task 1.3: Configure AppDbContext

**Files:**
- Create: `Contacts.API/Data/AppDbContext.cs`
- Modify: `Contacts.API/Program.cs` — register DbContext with connection string from environment

- [ ] **Step 1: Create AppDbContext**

```csharp
// Contacts.API/Data/AppDbContext.cs
using Contacts.API.Models;
using Contacts.API.Types;
using Microsoft.EntityFrameworkCore;

namespace Contacts.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>()
            .Ignore(c => c.Age);

        modelBuilder.Entity<Contact>()
            .Property(c => c.Gender)
            .HasConversion<string>();
    }
}
```

- [ ] **Step 2: Update Program.cs to register DbContext using environment variable / connection string**

```csharp
// Contacts.API/Program.cs
using Contacts.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load connection string from environment variable, fallback to appsettings
var connectionString = Environment.GetEnvironmentVariable("CONTACTS_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No database connection string configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 3: Create appsettings.json with placeholder (not real credentials)**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

- [ ] **Step 4: Verify build still passes**

Run: `dotnet build /home/murasaki/Documents/projects/cadmus/teste-dotnet/ContactsApi.sln`
Expected: Build succeeded, 0 errors

---

### Task 1.4: Define Custom Exceptions

**Files:**
- Create: `Contacts.API/Exceptions/ValidationException.cs`
- Create: `Contacts.API/Exceptions/NotFoundException.cs`
- Create: `Contacts.API/Exceptions/ConflictException.cs`

- [ ] **Step 1: Create ValidationException**

```csharp
// Contacts.API/Exceptions/ValidationException.cs
namespace Contacts.API.Exceptions;

public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(List<string> errors) : base("Validation failed.")
        => Errors = errors;
}
```

- [ ] **Step 2: Create NotFoundException**

```csharp
// Contacts.API/Exceptions/NotFoundException.cs
namespace Contacts.API.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

- [ ] **Step 3: Create ConflictException**

```csharp
// Contacts.API/Exceptions/ConflictException.cs
namespace Contacts.API.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
```

- [ ] **Step 4: Final build verification**

Run: `dotnet build /home/murasaki/Documents/projects/cadmus/teste-dotnet/ContactsApi.sln`
Expected: Build succeeded, 0 errors
