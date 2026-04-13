# Testing Implementation Plan — Contacts API

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement unit and E2E tests for the Contacts API using xUnit with NSubstitute for unit tests and WebApplicationFactory with SQL Server for E2E.

**Architecture:** Split existing test project into two: Unit tests using NSubstitute (note: spec mentions Moq but codebase uses NSubstitute) and E2E tests using WebApplicationFactory with SQL Server container.

**Tech Stack:** xUnit, NSubstitute, Microsoft.AspNetCore.Mvc.Testing, Microsoft.EntityFrameworkCore.SqlServer, SQL Server (docker)

---

## File Structure

```
Contacts.Tests/
├── Contacts.Tests.csproj          # Modify: existing, add NSubstitute + Moq per spec
├── Services/
│   └── ContactServiceTests.cs      # Create: unit tests
└── Controllers/
    └── ContactsControllerTests.cs  # Create: E2E tests
```

**Note:** Current test project uses NSubstitute. Spec says Moq but codebase uses NSubstitute. Plan adapts to use NSubstitute per existing pattern.

---

## Task 1: Update Test Project Dependencies

**Files:**
- Modify: `Contacts.Tests/Contacts.Tests.csproj`

- [ ] **Step 1: Add required NuGet packages**

Modify the .csproj to add packages:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="NSubstitute" Version="5.3.0" />
```

Note: Some packages already exist, ensure all are present.

- [ ] **Step 2: Verify build**

Run: `dotnet build Contacts.Tests/Contacts.Tests.csproj`
Expected: BUILD SUCCEEDED

---

## Task 2: Create Unit Test Directory and Base Class

**Files:**
- Create: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Create Services directory**

Create folder `Contacts.Tests/Services/`

- [ ] **Step 2: Write ContactServiceTests base class**

```csharp
using Contacts.API.DTOs;
using Contacts.API.Services;
using Contacts.API.Repositories.Interfaces;
using NSubstitute;
using Xunit;

namespace Contacts.Tests.Services;

public class ContactServiceTests
{
    private readonly IContactRepository _repository;
    private readonly ContactService _service;

    public ContactServiceTests()
    {
        _repository = Substitute.For<IContactRepository>();
        _service = new ContactService(_repository);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add ContactServiceTests base structure"
```

---

## Task 3: CreateAsync Unit Tests

**Files:**
- Modify: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Add test for happy path**

```csharp
[Fact]
public async Task CreateAsync_ShouldReturnContactResponseDto_WhenInputIsValid()
{
    // Arrange
    var dto = new CreateContactDto
    {
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = Gender.Male
    };
    var contact = new Contact
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        Birthdate = dto.Birthdate,
        Gender = dto.Gender,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    _repository.AddAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
    _repository.When(r => r.AddAsync(Arg.Any<Contact>())).Do(info => 
    {
        var c = info.Arg<Contact>();
        contact.Id = c.Id;
        contact.CreatedAt = c.CreatedAt;
    });

    // Act
    var result = await _service.CreateAsync(dto);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(dto.Name, result.Name);
    Assert.Equal(dto.Birthdate, result.Birthdate);
    Assert.Equal(dto.Gender, result.Gender);
    Assert.True(result.IsActive);
}
```

- [ ] **Step 2: Add test for empty name**

```csharp
[Fact]
public async Task CreateAsync_ShouldThrowValidationException_WhenNameIsEmpty()
{
    // Arrange
    var dto = new CreateContactDto
    {
        Name = "",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = Gender.Male
    };

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.CreateAsync(dto));
    Assert.Contains(ex.Errors, e => e.Contains("Name"));
}
```

- [ ] **Step 3: Add test for whitespace name**

```csharp
[Fact]
public async Task CreateAsync_ShouldThrowValidationException_WhenNameIsWhitespace()
{
    var dto = new CreateContactDto
    {
        Name = "   ",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = Gender.Male
    };

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.CreateAsync(dto));
    Assert.Contains(ex.Errors, e => e.Contains("Name"));
}
```

- [ ] **Step 4: Add test for null name**

```csharp
[Fact]
public async Task CreateAsync_ShouldThrowValidationException_WhenNameIsNull()
{
    var dto = new CreateContactDto
    {
        Name = null!,
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = Gender.Male
    };

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.CreateAsync(dto));
    Assert.Contains(ex.Errors, e => e.Contains("Name"));
}
```

- [ ] **Step 5: Add test for future birthdate**

```csharp
[Fact]
public async Task CreateAsync_ShouldThrowValidationException_WhenBirthdateIsInFuture()
{
    var dto = new CreateContactDto
    {
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
        Gender = Gender.Male
    };

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.CreateAsync(dto));
    Assert.Contains(ex.Errors, e => e.Contains("future"));
}
```

- [ ] **Step 6: Add test for age exactly 0 (birthdate today)**

```csharp
[Fact]
public async Task CreateAsync_ShouldThrowValidationException_WhenAgeIsExactly0()
{
    var dto = new CreateContactDto
    {
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today),
        Gender = Gender.Male
    };

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.CreateAsync(dto));
    Assert.Contains(ex.Errors, e => e.Contains("valid age"));
}
```

- [ ] **Step 7: Add test for age < 18**

```csharp
[Fact]
public async Task CreateAsync_ShouldThrowValidationException_WhenAgeIsUnder18()
{
    var dto = new CreateContactDto
    {
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-17)),
        Gender = Gender.Male
    };

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.CreateAsync(dto));
    Assert.Contains(ex.Errors, e => e.Contains("at least 18"));
}
```

- [ ] **Step 8: Add test for valid age boundary (exactly 18)**

```csharp
[Fact]
public async Task CreateAsync_ShouldSucceed_WhenAgeIsExactly18()
{
    var dto = new CreateContactDto
    {
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Gender = Gender.Male
    };
    _repository.AddAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);

    var result = await _service.CreateAsync(dto);

    Assert.NotNull(result);
    Assert.Equal(dto.Name, result.Name);
}
```

- [ ] **Step 9: Add test for all valid genders**

For each Gender enum value (Male, Female, Other):

```csharp
[Theory]
[InlineData(Gender.Male)]
[InlineData(Gender.Female)]
[InlineData(Gender.Other)]
public async Task CreateAsync_ShouldSucceed_WhenGenderIsValid(Gender gender)
{
    var dto = new CreateContactDto
    {
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = gender
    };
    _repository.AddAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);

    var result = await _service.CreateAsync(dto);

    Assert.NotNull(result);
    Assert.Equal(gender, result.Gender);
}
```

- [ ] **Step 10: Add test for multiple validation errors**

```csharp
[Fact]
public async Task CreateAsync_ShouldThrowValidationException_WithMultipleErrors_WhenInputsAreInvalid()
{
    var dto = new CreateContactDto
    {
        Name = "",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
        Gender = Gender.Male
    };

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.CreateAsync(dto));
    Assert.True(ex.Errors.Count >= 2);
}
```

- [ ] **Step 11: Run tests to verify**

Run: `dotnet test Contacts.Tests --filter "CreateAsync" -v`
Expected: All tests pass

- [ ] **Step 12: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add CreateAsync unit tests"
```

---

## Task 4: GetAllAsync Unit Tests

**Files:**
- Modify: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Add test for happy path (returns active only)**

```csharp
[Fact]
public async Task GetAllAsync_ShouldReturnActiveContactsOnly()
{
    // Arrange
    var activeContacts = new List<Contact>
    {
        new() { Id = Guid.NewGuid(), Name = "Active 1", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "Active 2", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-25)), Gender = Gender.Female, IsActive = true }
    };
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(activeContacts);

    // Act
    var result = await _service.GetAllAsync(new ContactQueryDto());

    // Assert
    Assert.Equal(2, result.Count);
    Assert.All(result, c => Assert.True(c.IsActive));
}
```

- [ ] **Step 2: Add test for filter by name (case-insensitive partial)**

```csharp
[Fact]
public async Task GetAllAsync_ShouldFilterByName_CaseInsensitivePartialMatch()
{
    // Arrange
    var contacts = new List<Contact>
    {
        new() { Id = Guid.NewGuid(), Name = "John Doe", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "Jane Doe", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-22)), Gender = Gender.Female, IsActive = true }
    };
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(contacts);

    // Act
    var result = await _service.GetAllAsync(new ContactQueryDto { Name = "john" });

    // Assert
    Assert.Single(result);
    Assert.Contains("John", result[0].Name, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 3: Add test for filter by gender (exact match)**

```csharp
[Fact]
public async Task GetAllAsync_ShouldFilterByGender_ExactMatch()
{
    // Arrange
    var contacts = new List<Contact>
    {
        new() { Id = Guid.NewGuid(), Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "Jane", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-22)), Gender = Gender.Female, IsActive = true }
    };
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(contacts);

    // Act
    var result = await _service.GetAllAsync(new ContactQueryDto { Gender = Gender.Male });

    // Assert
    Assert.Single(result);
    Assert.Equal(Gender.Male, result[0].Gender);
}
```

- [ ] **Step 4: Add test for filter by birthdateFrom**

```csharp
[Fact]
public async Task GetAllAsync_ShouldFilterByBirthdateFrom()
{
    var contacts = new List<Contact>
    {
        new() { Id = Guid.NewGuid(), Name = "Old", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), Gender = Gender.Male, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "Young", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true }
    };
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(contacts);

    var result = await _service.GetAllAsync(new ContactQueryDto { BirthdateFrom = DateOnly.FromDateTime(DateTime.Today.AddYears(-25)) });

    Assert.Single(result);
    Assert.Equal("Old", result[0].Name);
}
```

- [ ] **Step 5: Add test for filter by birthdateTo**

```csharp
[Fact]
public async Task GetAllAsync_ShouldFilterByBirthdateTo()
{
    var contacts = new List<Contact>
    {
        new() { Id = Guid.NewGuid(), Name = "Old", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), Gender = Gender.Male, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "Young", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true }
    };
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(contacts);

    var result = await _service.GetAllAsync(new ContactQueryDto { BirthdateTo = DateOnly.FromDateTime(DateTime.Today.AddYears(-25)) });

    Assert.Single(result);
    Assert.Equal("Young", result[0].Name);
}
```

- [ ] **Step 6: Add test for filter by birthdate range (from + to)**

```csharp
[Fact]
public async Task GetAllAsync_ShouldFilterByBirthdateRange()
{
    var contacts = new List<Contact>
    {
        new() { Id = Guid.NewGuid(), Name = "A", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), Gender = Gender.Male, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "B", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-25)), Gender = Gender.Male, IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "C", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true }
    };
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(contacts);

    var result = await _service.GetAllAsync(new ContactQueryDto 
    { 
        BirthdateFrom = DateOnly.FromDateTime(DateTime.Today.AddYears(-28)),
        BirthdateTo = DateOnly.FromDateTime(DateTime.Today.AddYears(-22))
    });

    Assert.Single(result);
    Assert.Equal("B", result[0].Name);
}
```

- [ ] **Step 7: Add test for empty result (no matches)**

```csharp
[Fact]
public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoContactsMatch()
{
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(new List<Contact>());

    var result = await _service.GetAllAsync(new ContactQueryDto { Name = "Nonexistent" });

    Assert.Empty(result);
}
```

- [ ] **Step 8: Add test for all filters combined**

```csharp
[Fact]
public async Task GetAllAsync_ShouldApplyAllFilters_Combined()
{
    var contacts = new List<Contact>
    {
        new() { Id = Guid.NewGuid(), Name = "John Male Old", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-25)), Gender = Gender.Male, IsActive = true }
    };
    _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(contacts);

    var result = await _service.GetAllAsync(new ContactQueryDto 
    { 
        Name = "john",
        Gender = Gender.Male,
        BirthdateFrom = DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
        BirthdateTo = DateOnly.FromDateTime(DateTime.Today.AddYears(-20))
    });

    Assert.Single(result);
}
```

- [ ] **Step 9: Run tests to verify**

Run: `dotnet test Contacts.Tests --filter "GetAllAsync" -v`
Expected: All tests pass

- [ ] **Step 10: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add GetAllAsync unit tests"
```

---

## Task 5: GetByIdAsync Unit Tests

**Files:**
- Modify: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Add test for happy path (active contact)**

```csharp
[Fact]
public async Task GetByIdAsync_ShouldReturnContact_WhenContactIsActive()
{
    // Arrange
    var id = Guid.NewGuid();
    var contact = new Contact
    {
        Id = id,
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = Gender.Male,
        IsActive = true
    };
    _repository.GetByIdAsync(id).Returns(contact);

    // Act
    var result = await _service.GetByIdAsync(id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(id, result.Id);
    Assert.Equal("John Doe", result.Name);
}
```

- [ ] **Step 2: Add test for contact not found (never existed)**

```csharp
[Fact]
public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
{
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id).Returns((Contact?)null);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.NotFoundException>(
        () => _service.GetByIdAsync(id));
}
```

- [ ] **Step 3: Add test for contact exists but inactive (404)**

```csharp
[Fact]
public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenContactIsInactive()
{
    var id = Guid.NewGuid();
    var contact = new Contact
    {
        Id = id,
        Name = "John Doe",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = Gender.Male,
        IsActive = false
    };
    _repository.GetByIdAsync(id).Returns(contact);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.NotFoundException>(
        () => _service.GetByIdAsync(id));
}
```

- [ ] **Step 4: Run tests to verify**

Run: `dotnet test Contacts.Tests --filter "GetByIdAsync" -v`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add GetByIdAsync unit tests"
```

---

## Task 6: UpdateAsync Unit Tests

**Files:**
- Modify: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Add test for happy path**

```csharp
[Fact]
public async Task UpdateAsync_ShouldReturnUpdatedContact_WhenInputIsValid()
{
    var id = Guid.NewGuid();
    var contact = new Contact
    {
        Id = id,
        Name = "Old Name",
        Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
        Gender = Gender.Male,
        IsActive = true
    };
    var dto = new UpdateContactDto { Name = "New Name" };
    _repository.GetActiveByIdAsync(id).Returns(contact);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    var result = await _service.UpdateAsync(id, dto);

    Assert.Equal("New Name", result.Name);
}
```

- [ ] **Step 2: Add test for contact not found**

```csharp
[Fact]
public async Task UpdateAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
{
    var id = Guid.NewGuid();
    _repository.GetActiveByIdAsync(id).Returns((Contact?)null);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.NotFoundException>(
        () => _service.UpdateAsync(id, new UpdateContactDto()));
}
```

- [ ] **Step 3: Add test for name empty/whitespace**

```csharp
[Fact]
public async Task UpdateAsync_ShouldThrowValidationException_WhenNameIsEmpty()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.UpdateAsync(id, new UpdateContactDto { Name = "" }));
    Assert.Contains(ex.Errors, e => e.Contains("Name"));
}
```

- [ ] **Step 4: Add test for birthdate in future**

```csharp
[Fact]
public async Task UpdateAsync_ShouldThrowValidationException_WhenBirthdateIsInFuture()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.UpdateAsync(id, new UpdateContactDto { Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)) }));
    Assert.Contains(ex.Errors, e => e.Contains("future"));
}
```

- [ ] **Step 5: Add test for age exactly 0**

```csharp
[Fact]
public async Task UpdateAsync_ShouldThrowValidationException_WhenAgeIsExactly0()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.UpdateAsync(id, new UpdateContactDto { Birthdate = DateOnly.FromDateTime(DateTime.Today) }));
    Assert.Contains(ex.Errors, e => e.Contains("valid age"));
}
```

- [ ] **Step 6: Add test for age < 18**

```csharp
[Fact]
public async Task UpdateAsync_ShouldThrowValidationException_WhenAgeIsUnder18()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.UpdateAsync(id, new UpdateContactDto { Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-17)) }));
    Assert.Contains(ex.Errors, e => e.Contains("at least 18"));
}
```

- [ ] **Step 7: Add test for partial update (only name)**

```csharp
[Fact]
public async Task UpdateAsync_ShouldUpdateOnlyName_WhenOnlyNameProvided()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "Old", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    var result = await _service.UpdateAsync(id, new UpdateContactDto { Name = "New" });

    Assert.Equal("New", result.Name);
    Assert.Equal(contact.Birthdate, result.Birthdate);
}
```

- [ ] **Step 8: Add test for partial update (only birthdate)**

```csharp
[Fact]
public async Task UpdateAsync_ShouldUpdateOnlyBirthdate_WhenOnlyBirthdateProvided()
{
    var id = Guid.NewGuid();
    var newBirthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-25));
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    var result = await _service.UpdateAsync(id, new UpdateContactDto { Birthdate = newBirthdate });

    Assert.Equal("John", result.Name);
    Assert.Equal(newBirthdate, result.Birthdate);
}
```

- [ ] **Step 9: Add test for partial update (only gender)**

```csharp
[Fact]
public async Task UpdateAsync_ShouldUpdateOnlyGender_WhenOnlyGenderProvided()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    var result = await _service.UpdateAsync(id, new UpdateContactDto { Gender = Gender.Female });

    Assert.Equal(Gender.Female, result.Gender);
}
```

- [ ] **Step 10: Add test for no fields provided (no-op)**

```csharp
[Fact]
public async Task UpdateAsync_ShouldReturnUnchangedContact_WhenNoFieldsProvided()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    var result = await _service.UpdateAsync(id, new UpdateContactDto());

    Assert.Equal("John", result.Name);
}
```

- [ ] **Step 11: Add test for multiple validation errors**

```csharp
[Fact]
public async Task UpdateAsync_ShouldThrowValidationException_WithMultipleErrors()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, Name = "John", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), Gender = Gender.Male, IsActive = true };
    _repository.GetActiveByIdAsync(id).Returns(contact);

    var ex = await Assert.ThrowsAsync<Contacts.API.Exceptions.ValidationException>(
        () => _service.UpdateAsync(id, new UpdateContactDto { Name = "", Birthdate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)) }));
    Assert.True(ex.Errors.Count >= 2);
}
```

- [ ] **Step 12: Run tests to verify**

Run: `dotnet test Contacts.Tests --filter "UpdateAsync" -v`
Expected: All tests pass

- [ ] **Step 13: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add UpdateAsync unit tests"
```

---

## Task 7: ActivateAsync Unit Tests

**Files:**
- Modify: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Add test for happy path (inactive → active)**

```csharp
[Fact]
public async Task ActivateAsync_ShouldActivateContact_WhenContactIsInactive()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = false };
    _repository.GetByIdAsync(id).Returns(contact);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    await _service.ActivateAsync(id);

    Assert.True(contact.IsActive);
}
```

- [ ] **Step 2: Add test for contact not found (never existed)**

```csharp
[Fact]
public async Task ActivateAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
{
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id).Returns((Contact?)null);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.NotFoundException>(
        () => _service.ActivateAsync(id));
}
```

- [ ] **Step 3: Add test for already active (409)**

```csharp
[Fact]
public async Task ActivateAsync_ShouldThrowConflictException_WhenContactIsAlreadyActive()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = true };
    _repository.GetByIdAsync(id).Returns(contact);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.ConflictException>(
        () => _service.ActivateAsync(id));
}
```

- [ ] **Step 4: Add test for can reactivate after deactivate**

```csharp
[Fact]
public async Task ActivateAsync_ShouldReactivate_WhenContactWasPreviouslyDeactivated()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = false };
    _repository.GetByIdAsync(id).Returns(contact);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    await _service.ActivateAsync(id);
    Assert.True(contact.IsActive);
}
```

- [ ] **Step 5: Run tests to verify**

Run: `dotnet test Contacts.Tests --filter "ActivateAsync" -v`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add ActivateAsync unit tests"
```

---

## Task 8: DeactivateAsync Unit Tests

**Files:**
- Modify: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Add test for happy path (active → inactive)**

```csharp
[Fact]
public async Task DeactivateAsync_ShouldDeactivateContact_WhenContactIsActive()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = true };
    _repository.GetByIdAsync(id).Returns(contact);
    _repository.UpdateAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    await _service.DeactivateAsync(id);

    Assert.False(contact.IsActive);
}
```

- [ ] **Step 2: Add test for contact not found (never existed)**

```csharp
[Fact]
public async Task DeactivateAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
{
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id).Returns((Contact?)null);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.NotFoundException>(
        () => _service.DeactivateAsync(id));
}
```

- [ ] **Step 3: Add test for already inactive (409)**

```csharp
[Fact]
public async Task DeactivateAsync_ShouldThrowConflictException_WhenContactIsAlreadyInactive()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = false };
    _repository.GetByIdAsync(id).Returns(contact);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.ConflictException>(
        () => _service.DeactivateAsync(id));
}
```

- [ ] **Step 4: Add test for can deactivate after activate**

```csharp
[Fact]
public async Task DeactivateAsync_ShouldDeactivate_WhenContactWasPreviouslyActivated()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = true };
    _repository.GetByIdAsync(id).Returns(contact);
    _repository.UpdateAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    await _service.DeactivateAsync(id);
    Assert.False(contact.IsActive);
}
```

- [ ] **Step 5: Run tests to verify**

Run: `dotnet test Contacts.Tests --filter "DeactivateAsync" -v`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add DeactivateAsync unit tests"
```

---

## Task 9: DeleteAsync Unit Tests

**Files:**
- Modify: `Contacts.Tests/Services/ContactServiceTests.cs`

- [ ] **Step 1: Add test for happy path (active deleted)**

```csharp
[Fact]
public async Task DeleteAsync_ShouldDeleteContact_WhenContactIsActive()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = true };
    _repository.GetByIdAsync(id).Returns(contact);
    _repository.DeleteAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    await _service.DeleteAsync(id);

    _repository.Received(1).DeleteAsync(contact);
}
```

- [ ] **Step 2: Add test for happy path (inactive deleted)**

```csharp
[Fact]
public async Task DeleteAsync_ShouldDeleteContact_WhenContactIsInactive()
{
    var id = Guid.NewGuid();
    var contact = new Contact { Id = id, IsActive = false };
    _repository.GetByIdAsync(id).Returns(contact);
    _repository.DeleteAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
    _repository.SaveChangesAsync().Returns(Task.CompletedTask);

    await _service.DeleteAsync(id);

    _repository.Received(1).DeleteAsync(contact);
}
```

- [ ] **Step 3: Add test for contact not found (never existed)**

```csharp
[Fact]
public async Task DeleteAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
{
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id).Returns((Contact?)null);

    await Assert.ThrowsAsync<Contacts.API.Exceptions.NotFoundException>(
        () => _service.DeleteAsync(id));
}
```

- [ ] **Step 4: Run tests to verify**

Run: `dotnet test Contacts.Tests --filter "DeleteAsync" -v`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add Contacts.Tests/Services/ContactServiceTests.cs
git commit -m "test: add DeleteAsync unit tests"
```

---

## Task 10: E2E Test Setup and Controller Tests

**Files:**
- Create: `Contacts.Tests/Controllers/ContactsControllerTests.cs`

- [ ] **Step 1: Create Controllers directory**

Create folder `Contacts.Tests/Controllers/`

- [ ] **Step 2: Start test database**

Run: `docker-compose -f docker-compose.test.yml up -d`
Expected: Container started successfully

- [ ] **Step 3: Write E2E test base class**

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using Xunit;

namespace Contacts.Tests.Controllers;

[Trait("Category", "E2E")]
public class ContactsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _connectionString = "Server=localhost,1434;Database=ContactsDb_Test;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True";

    public ContactsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private async Task CleanupDatabase()
    {
        var password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD") ?? "YourStrong@Passw0rd";
        var connString = $"Server=localhost,1434;Database=ContactsDb_Test;User Id=sa;Password={password};TrustServerCertificate=True";
        using var connection = new SqlConnection(connString);
        await connection.OpenAsync();
        using var command = new SqlCommand("DELETE FROM Contacts", connection);
        await command.ExecuteNonQueryAsync();
    }
}
```

- [ ] **Step 4: Add POST /api/contacts test (201 + Location header)**

```csharp
[Fact]
public async Task PostContacts_ShouldReturn201_WhenInputIsValid()
{
    await CleanupDatabase();
    
    var dto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };

    var response = await _client.PostAsJsonAsync("/api/contacts", dto);

    Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    Assert.NotNull(response.Headers.Location);
}
```

- [ ] **Step 5: Add GET /api/contacts test (200 + list)**

```csharp
[Fact]
public async Task GetContacts_ShouldReturn200_WithList()
{
    await CleanupDatabase();
    
    var dto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    await _client.PostAsJsonAsync("/api/contacts", dto);

    var response = await _client.GetAsync("/api/contacts");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("John Doe", content);
}
```

- [ ] **Step 6: Add GET /api/contacts/{id} test (200 for active, 404 for inactive)**

```csharp
[Fact]
public async Task GetContactById_ShouldReturn200_WhenContactIsActive()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;

    var response = await _client.GetAsync($"/api/contacts/{id}");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task GetContactById_ShouldReturn404_WhenContactIsInactive()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;
    
    await _client.PatchAsync($"/api/contacts/{id}/deactivate", null);

    var response = await _client.GetAsync($"/api/contacts/{id}");

    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
}
```

- [ ] **Step 7: Add PATCH /api/contacts/{id} test (200, 404 for inactive)**

```csharp
[Fact]
public async Task UpdateContact_ShouldReturn200_WhenInputIsValid()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;

    var updateDto = new { name = "Jane Doe" };
    var response = await _client.PatchAsync($"/api/contacts/{id}", JsonContent.Create(updateDto));

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
}
```

- [ ] **Step 8: Add PATCH /api/contacts/{id}/activate test (200, 409 if already active)**

```csharp
[Fact]
public async Task ActivateContact_ShouldReturn200_WhenContactIsInactive()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;
    await _client.PatchAsync($"/api/contacts/{id}/deactivate", null);

    var response = await _client.PatchAsync($"/api/contacts/{id}/activate", null);

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task ActivateContact_ShouldReturn409_WhenContactIsAlreadyActive()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;

    var response = await _client.PatchAsync($"/api/contacts/{id}/activate", null);

    Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
}
```

- [ ] **Step 9: Add PATCH /api/contacts/{id}/deactivate test (200, 409 if already inactive)**

```csharp
[Fact]
public async Task DeactivateContact_ShouldReturn200_WhenContactIsActive()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;

    var response = await _client.PatchAsync($"/api/contacts/{id}/deactivate", null);

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task DeactivateContact_ShouldReturn409_WhenContactIsAlreadyInactive()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;
    await _client.PatchAsync($"/api/contacts/{id}/deactivate", null);

    var response = await _client.PatchAsync($"/api/contacts/{id}/deactivate", null);

    Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
}
```

- [ ] **Step 10: Add DELETE /api/contacts/{id} test (204)**

```csharp
[Fact]
public async Task DeleteContact_ShouldReturn204_WhenContactExists()
{
    await CleanupDatabase();
    
    var createDto = new
    {
        name = "John Doe",
        birthdate = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd"),
        gender = "Male"
    };
    var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
    var created = await createResponse.Content.ReadAsJsonAsync<dynamic>();
    var id = (string)created.id;

    var response = await _client.DeleteAsync($"/api/contacts/{id}");

    Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
}
```

- [ ] **Step 11: Run E2E tests to verify**

Run: `dotnet test Contacts.Tests --filter "Category=E2E" -v`
Expected: All tests pass

- [ ] **Step 12: Commit**

```bash
git add Contacts.Tests/Controllers/ContactsControllerTests.cs
git commit -m "test: add E2E controller tests"
```

---

## Task 11: Final Verification

**Files:**
- All test files

- [ ] **Step 1: Run all unit tests**

Run: `dotnet test Contacts.Tests --filter "Category=Unit" -v`
Expected: All unit tests pass

- [ ] **Step 2: Run all E2E tests**

Run: `dotnet test Contacts.Tests --filter "Category=E2E" -v`
Expected: All E2E tests pass

- [ ] **Step 3: Check for build warnings**

Run: `dotnet build Contacts.Tests/Contacts.Tests.csproj`
Expected: No warnings

- [ ] **Step 4: Commit final changes**

```bash
git add .
git commit -m "test: complete testing implementation"
```

---

## Plan Complete

**Plan complete and saved to `docs/superpowers/plans/2026-04-13-testing-design.md`. Two execution options:**

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
