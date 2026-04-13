# Testing Design — Contacts API

## Overview

This spec defines unit and E2E tests for the Contacts API using xUnit with Moq for unit tests and WebApplicationFactory with SQL Server for E2E.

## Test Projects Structure

```
Contacts.Tests/
├── Contacts.Tests.Unit/          # Unit tests - ContactService with Moq
│   └── Services/
│       └── ContactServiceTests.cs
└── Contacts.Tests.E2E/         # E2E tests - full app via WebApplicationFactory
    └── Controllers/
        └── ContactsControllerTests.cs
```

## Dependencies

### Unit Tests (.csproj)
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Moq" />
```

### E2E Tests (.csproj)
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
```

## Unit Tests

### Target
`ContactService` via `IContactRepository` mock (Moq)

### Test Class Structure
```csharp
public class ContactServiceTests
{
    private readonly Mock<IContactRepository> _repositoryMock;
    private readonly ContactService _service;

    public ContactServiceTests()
    {
        _repositoryMock = new Mock<IContactRepository>();
        _service = new ContactService(_repositoryMock.Object);
    }
}
```

### Coverage

| Method | Test Cases |
|--------|-----------|
| `CreateAsync` | Happy path, name empty/whitespace, name null, birthdate in future, age exactly 0, age < 18, valid age boundary (18+), all valid genders, valid gender enum value, gender not provided (required), multiple validation errors |
| `GetAllAsync` | Happy path (returns active only), filter by name (case-insensitive partial), filter by gender (exact match), filter by birthdateFrom, filter by birthdateTo, filter by birthdate range (from + to), empty result (no matches), all filters combined |
| `GetByIdAsync` | Happy path (active contact), contact not found (never existed), contact exists but inactive (404) |
| `UpdateAsync` | Happy path, contact not found, name empty/whitespace, birthdate in future, age exactly 0, age < 18, partial update (only name), partial update (only birthdate), partial update (only gender), no fields provided (no-op), multiple validation errors |
| `ActivateAsync` | Happy path (inactive → active), contact not found (never existed), already active (409), can reactivate after deactivate |
| `DeactivateAsync` | Happy path (active → inactive), contact not found (never existed), already inactive (409), can deactivate after activate |
| `DeleteAsync` | Happy path (active deleted), happy path (inactive deleted), contact not found (never existed) |

### Naming Convention
`MethodName_ShouldExpectedBehavior_WhenCondition`

### Example Test
```csharp
[Fact]
public void CreateAsync_ShouldThrowValidationException_WhenNameIsEmpty()
{
    var dto = new CreateContactDto { Name = "", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male };
    _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contact>())).Returns(Task.CompletedTask);

    var act = () => _service.CreateAsync(dto);

    await Assert.ThrowsAsync<ValidationException>(() => act);
}
```

## E2E Tests

### Setup
```csharp
public class ContactsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ContactsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
}
```

### Configuration
- Uses docker-compose.test.yml SQL Server (port 1434)
- Connection: `Server=localhost,1434;Database=ContactsDb_Test;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True`
- Database cleaned between tests via table truncation

### Coverage (1 test per endpoint)

| Endpoint | Tests |
|----------|-------|
| POST /api/contacts | Returns 201 + Location header |
| GET /api/contacts | Returns 200 + list |
| GET /api/contacts/{id} | Returns 200 for active, Returns 404 for inactive |
| PATCH /api/contacts/{id} | Returns 200, Returns 404 for inactive |
| PATCH /api/contacts/{id}/activate | Returns 200, Returns 409 if already active |
| PATCH /api/contacts/{id}/deactivate | Returns 200, Returns 409 if already inactive |
| DELETE /api/contacts/{id} | Returns 204 |

### Database Cleanup
```csharp
private async Task CleanupDatabase()
{
    using var connection = new SqlConnection(_connectionString);
    await connection.ExecuteAsync("DELETE FROM Contacts");
}
```

## Running Tests

```bash
# Start test DB
docker-compose -f docker-compose.test.yml up -d

# Run all tests
dotnet test

# Run unit only
dotnet test --filter "Category=Unit"

# Run E2E only
dotnet test --filter "Category=E2E"
```

## Build Validation

Before claiming completion:
1. All unit tests pass
2. All E2E tests pass
3. No build warnings
4. Code coverage verified