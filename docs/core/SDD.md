# Software Design Document — Contacts API

## 1. Architecture Overview

The application follows a flat layered architecture within a single .NET project. Layers are enforced by convention and code review discipline rather than separate assemblies. Each layer has a single, clearly defined responsibility and communicates with adjacent layers only through interfaces.

```
HTTP Request
     |
     v
[ Controllers ]      — HTTP in/out only; delegates to service
     |
     v
[ Services ]         — Business rules and orchestration
     |
     v
[ Repositories ]     — EF Core data access only
     |
     v
[ SQL Server DB ]
```

Cross-cutting concerns (error handling) are handled by middleware that wraps the entire pipeline.

---

## 2. Project Structure

```
Contacts.API/
├── Controllers/
│   └── ContactsController.cs
├── Data/
│   └── AppDbContext.cs
├── DTOs/
│   ├── CreateContactDto.cs
│   ├── UpdateContactDto.cs
│   ├── ContactResponseDto.cs
│   └── ContactQueryDto.cs
├── Exceptions/
│   ├── ValidationException.cs
│   ├── NotFoundException.cs
│   └── ConflictException.cs
├── Mappers/
│   └── ContactMapper.cs
├── Middleware/
│   └── ErrorHandlingMiddleware.cs
├── Migrations/
├── Models/
│   └── Contact.cs
├── Repositories/
│   ├── Interfaces/
│   │   └── IContactRepository.cs
│   └── ContactRepository.cs
├── Services/
│   ├── Interfaces/
│   │   └── IContactService.cs
│   └── ContactService.cs
├── Types/
│   └── Gender.cs
└── Program.cs

Contacts.Tests/
├── Unit/
│   └── Services/
│       └── ContactServiceTests.cs
└── E2E/
    └── Controllers/
        └── ContactsControllerTests.cs
```

---

## 3. Data Model

### 3.1 Entity

```csharp
// Models/Contact.cs
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

### 3.2 Enum

```csharp
// Types/Gender.cs
public enum Gender
{
    Male,
    Female,
    Other
}
```

### 3.3 EF Configuration

```csharp
// Data/AppDbContext.cs
modelBuilder.Entity<Contact>()
    .Ignore(c => c.Age);

modelBuilder.Entity<Contact>()
    .Property(c => c.Gender)
    .HasConversion<string>();
```

`Age` is explicitly ignored by EF. `Gender` is stored as a string column for readability.

---

## 4. DTOs

### 4.1 CreateContactDto
```csharp
public class CreateContactDto
{
    public string Name { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public Gender Gender { get; set; }
}
```

### 4.2 UpdateContactDto
```csharp
public class UpdateContactDto
{
    public string? Name { get; set; }
    public DateOnly? Birthdate { get; set; }
    public Gender? Gender { get; set; }
}
```

### 4.3 ContactResponseDto
```csharp
public class ContactResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; }
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 4.4 ContactQueryDto
```csharp
public class ContactQueryDto
{
    public string? Name { get; set; }
    public Gender? Gender { get; set; }
    public DateOnly? BirthdateFrom { get; set; }
    public DateOnly? BirthdateTo { get; set; }
}
```

---

## 5. Mapper

`ContactMapper` is a static class with pure mapping methods. No external mapping library is used.

```csharp
// Mappers/ContactMapper.cs
public static class ContactMapper
{
    public static Contact ToEntity(CreateContactDto dto) { ... }
    public static ContactResponseDto ToResponseDto(Contact entity) { ... }
    public static void ApplyUpdate(UpdateContactDto dto, Contact entity) { ... }
}
```

`ApplyUpdate` only writes fields that are non-null in the DTO (partial update semantics).

---

## 6. Repository

### 6.1 Interface

```csharp
// Repositories/Interfaces/IContactRepository.cs
public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id);
    Task<Contact?> GetActiveByIdAsync(Guid id);
    Task<List<Contact>> GetAllActiveAsync(ContactQueryDto query);
    Task AddAsync(Contact contact);
    Task UpdateAsync(Contact contact);
    Task DeleteAsync(Contact contact);
    Task SaveChangesAsync();
}
```

### 6.2 Implementation

`ContactRepository` uses EF Core with LINQ to Entities. All queries use `AsNoTracking()` for reads. Write operations track entities normally.

`GetAllActiveAsync` builds a LINQ query dynamically based on which filter fields are non-null in the `ContactQueryDto`.

---

## 7. Service

### 7.1 Interface

```csharp
// Services/Interfaces/IContactService.cs
public interface IContactService
{
    Task<ContactResponseDto> CreateAsync(CreateContactDto dto);
    Task<List<ContactResponseDto>> GetAllAsync(ContactQueryDto query);
    Task<ContactResponseDto> GetByIdAsync(Guid id);
    Task<ContactResponseDto> UpdateAsync(Guid id, UpdateContactDto dto);
    Task ActivateAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task DeleteAsync(Guid id);
}
```

### 7.2 Business Logic

**CreateAsync:**
1. Validate `Name` is non-empty.
2. Validate `Birthdate` is not in the future.
3. Validate computed `Age > 0`.
4. Validate `Age >= 18`.
5. Map DTO to entity, set `IsActive = true`, generate `Id = Guid.NewGuid()`, set `CreatedAt = UpdatedAt = DateTime.UtcNow`.
6. Persist and return response DTO.

**GetAllAsync:**
1. Delegate to repository with query filters.
2. Map results to response DTOs.

**GetByIdAsync:**
1. Fetch active contact by ID.
2. If null → throw `NotFoundException`.
3. Return response DTO.

**UpdateAsync:**
1. Fetch active contact by ID → `NotFoundException` if missing.
2. Validate any provided fields using the same rules as creation.
3. Apply partial update via mapper.
4. Set `UpdatedAt = DateTime.UtcNow`.
5. Persist and return updated response DTO.

**ActivateAsync:**
1. Fetch contact by ID (any status) → `NotFoundException` if missing.
2. If already active → throw `ConflictException`.
3. Set `IsActive = true`, set `UpdatedAt = DateTime.UtcNow`, persist.

**DeactivateAsync:**
1. Fetch contact by ID (any status) → `NotFoundException` if missing.
2. If already inactive → throw `ConflictException`.
3. Set `IsActive = false`, set `UpdatedAt = DateTime.UtcNow`, persist.

**DeleteAsync:**
1. Fetch contact by ID (any status) → `NotFoundException` if missing.
2. Hard delete, persist.

---

## 8. Controller

`ContactsController` is decorated with `[ApiController]` and `[Route("api/contacts")]`. It has no business logic — every method delegates to the service and maps the result to the appropriate HTTP response.

```
POST   /api/contacts              → 201 Created + Location header
GET    /api/contacts              → 200 OK + list
GET    /api/contacts/{id}         → 200 OK + contact
PATCH  /api/contacts/{id}         → 200 OK + updated contact
PATCH  /api/contacts/{id}/activate   → 200 OK
PATCH  /api/contacts/{id}/deactivate → 200 OK
DELETE /api/contacts/{id}         → 204 No Content
```

---

## 9. Error Handling Middleware

`ErrorHandlingMiddleware` is registered in `Program.cs` before all other middleware. It catches all unhandled exceptions from the pipeline.

**Exception → HTTP status mapping:**

| Exception Type | HTTP Status |
|---|---|
| `ValidationException` | `400 Bad Request` |
| `NotFoundException` | `404 Not Found` |
| `ConflictException` | `409 Conflict` |
| Any other `Exception` | `500 Internal Server Error` |

**Error response shape:**
```json
{
  "statusCode": 400,
  "message": "Validation failed.",
  "errors": ["Contact must be at least 18 years old."]
}
```

For `500` responses, the message is always `"An unexpected error occurred."` — no internal details are exposed.

---

## 10. Custom Exceptions

```csharp
// Exceptions/ValidationException.cs
public class ValidationException : Exception
{
    public List<string> Errors { get; }
    public ValidationException(List<string> errors) : base("Validation failed.")
        => Errors = errors;
}

// Exceptions/NotFoundException.cs
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Exceptions/ConflictException.cs
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
```

---

## 11. Dependency Injection

All services and repositories are registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

`Scoped` lifetime is used for both service and repository to align with the EF `DbContext` lifetime.

---

## 12. Testing Strategy

### 12.1 Unit Tests (Contacts.Tests/Unit/)

- Test target: `ContactService`
- Dependencies: `IContactRepository` mocked via `NSubstitute` or `Moq`
- One test class per service method
- Covers: happy path, all validation branches, 404 and 409 cases
- Naming convention: `MethodName_ShouldExpectedBehavior_WhenCondition`

### 12.2 E2E / Integration Tests (Contacts.Tests/E2E/)

- Uses `WebApplicationFactory<Program>` to spin up the full application in-memory
- Targets a dedicated SQL Server Docker container (separate from the dev DB)
- EF migrations run automatically at test suite startup via `dbContext.Database.MigrateAsync()`
- Database is cleaned between tests using per-test table truncation to ensure isolation
- Covers: correct HTTP status codes, response body shapes, filter behavior, error response format

### 12.3 Coverage Scope

| Layer | Tested by |
|---|---|
| Services | Unit tests |
| Controllers | E2E tests |
| Repositories | E2E tests (transitively) |
| Mappers | E2E tests (transitively) |
| Middleware | E2E tests (error response tests) |

---

## 13. Configuration

`appsettings.json` holds the dev DB connection string. `appsettings.Development.json` can override for local development. The test project uses its own connection string pointing to the Docker test container, injected via `WebApplicationFactory` configuration override.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ContactsDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```
