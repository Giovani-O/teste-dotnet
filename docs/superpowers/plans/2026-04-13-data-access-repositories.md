# Data Access & Repositories Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `IContactRepository` interface and its EF Core `ContactRepository` implementation, bridging the domain model to SQL Server.

**Architecture:** A single interface (`IContactRepository`) defines the contract; `ContactRepository` satisfies it using `AppDbContext`. All read operations use `AsNoTracking()`. `GetAllActiveAsync` builds the LINQ query incrementally based on whichever filter fields in `ContactQueryDto` are non-null.

**Tech Stack:** .NET 10, C#, EF Core 10 (`Microsoft.EntityFrameworkCore`), SQL Server, xUnit (no unit tests for repository — integration/E2E tests cover it; the plan notes where unit tests would be redundant).

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Contacts.API/Repositories/Interfaces/IContactRepository.cs` | Contract — all 7 method signatures |
| Create | `Contacts.API/Repositories/ContactRepository.cs` | EF Core implementation |

---

### Task 1: Define `IContactRepository`

**Files:**
- Create: `Contacts.API/Repositories/Interfaces/IContactRepository.cs`

- [ ] **Step 1.1: Create the interface file**

  ```csharp
  using Contacts.API.DTOs;
  using Contacts.API.Models;

  namespace Contacts.API.Repositories.Interfaces;

  public interface IContactRepository
  {
      /// <summary>Returns ANY contact by Id regardless of active status. Null if not found.</summary>
      Task<Contact?> GetByIdAsync(Guid id);

      /// <summary>Returns only an ACTIVE contact by Id. Null if not found or inactive.</summary>
      Task<Contact?> GetActiveByIdAsync(Guid id);

      /// <summary>Returns all active contacts, filtered by non-null fields in query.</summary>
      Task<List<Contact>> GetAllActiveAsync(ContactQueryDto query);

      /// <summary>Persists a new contact (not yet saved — caller must call SaveChangesAsync).</summary>
      Task AddAsync(Contact contact);

      /// <summary>Marks a tracked contact as modified (not yet saved — caller must call SaveChangesAsync).</summary>
      Task UpdateAsync(Contact contact);

      /// <summary>Removes a contact (not yet saved — caller must call SaveChangesAsync).</summary>
      Task DeleteAsync(Contact contact);

      /// <summary>Flushes pending changes to the database.</summary>
      Task SaveChangesAsync();
  }
  ```

- [ ] **Step 1.2: Verify the project compiles**

  ```bash
  dotnet build Contacts.API/Contacts.API.csproj
  ```

  Expected: `Build succeeded.` with 0 errors.

---

### Task 2: Implement `ContactRepository`

**Files:**
- Create: `Contacts.API/Repositories/ContactRepository.cs`
- Depends on: `AppDbContext`, `IContactRepository`, `Contact`, `ContactQueryDto`, `Gender`

- [ ] **Step 2.1: Create the repository file**

  ```csharp
  using Contacts.API.Data;
  using Contacts.API.DTOs;
  using Contacts.API.Models;
  using Contacts.API.Repositories.Interfaces;
  using Microsoft.EntityFrameworkCore;

  namespace Contacts.API.Repositories;

  public class ContactRepository : IContactRepository
  {
      private readonly AppDbContext _db;

      public ContactRepository(AppDbContext db)
      {
          _db = db;
      }

      /// <inheritdoc/>
      public async Task<Contact?> GetByIdAsync(Guid id)
      {
          return await _db.Contacts
              .AsNoTracking()
              .FirstOrDefaultAsync(c => c.Id == id);
      }

      /// <inheritdoc/>
      public async Task<Contact?> GetActiveByIdAsync(Guid id)
      {
          return await _db.Contacts
              .AsNoTracking()
              .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
      }

      /// <inheritdoc/>
      public async Task<List<Contact>> GetAllActiveAsync(ContactQueryDto query)
      {
          IQueryable<Contact> q = _db.Contacts
              .AsNoTracking()
              .Where(c => c.IsActive);

          if (!string.IsNullOrWhiteSpace(query.Name))
              q = q.Where(c => c.Name.Contains(query.Name));

          if (query.Gender.HasValue)
              q = q.Where(c => c.Gender == query.Gender.Value);

          if (query.BirthdateFrom.HasValue)
              q = q.Where(c => c.Birthdate >= query.BirthdateFrom.Value);

          if (query.BirthdateTo.HasValue)
              q = q.Where(c => c.Birthdate <= query.BirthdateTo.Value);

          return await q.ToListAsync();
      }

      /// <inheritdoc/>
      public async Task AddAsync(Contact contact)
      {
          await _db.Contacts.AddAsync(contact);
      }

      /// <inheritdoc/>
      public Task UpdateAsync(Contact contact)
      {
          _db.Contacts.Update(contact);
          return Task.CompletedTask;
      }

      /// <inheritdoc/>
      public Task DeleteAsync(Contact contact)
      {
          _db.Contacts.Remove(contact);
          return Task.CompletedTask;
      }

      /// <inheritdoc/>
      public async Task SaveChangesAsync()
      {
          await _db.SaveChangesAsync();
      }
  }
  ```

  **Design notes:**
  - `GetByIdAsync` — no `IsActive` filter; used by Activate/Deactivate/Delete which must work on any status.
  - `GetActiveByIdAsync` — adds `&& c.IsActive`; used by GetById and Update which only operate on active contacts.
  - `GetAllActiveAsync` — chains `.Where()` calls only for non-null filter fields. `Name` uses `.Contains()` for substring match. All four filters compose correctly with the base `IsActive` filter.
  - `UpdateAsync` / `DeleteAsync` — synchronous internally; return `Task.CompletedTask` to satisfy the interface contract without allocating a state machine.
  - `AddAsync` is truly async because `DbSet.AddAsync` is async (value-generator may need to run).
  - `SaveChangesAsync` delegates to `DbContext.SaveChangesAsync()` — the service layer calls this after every mutation.

- [ ] **Step 2.2: Verify the project compiles**

  ```bash
  dotnet build Contacts.API/Contacts.API.csproj
  ```

  Expected: `Build succeeded.` with 0 errors.

---

### Task 3: Register `IContactRepository` in DI

**Files:**
- Modify: `Contacts.API/Program.cs`

The SDD specifies:
```csharp
builder.Services.AddScoped<IContactRepository, ContactRepository>();
```

`IContactService` / `ContactService` are registered in a later milestone. If a TODO comment for them is present, leave it — only add the repository registration now.

- [ ] **Step 3.1: Open `Program.cs` and locate the DI section**

  Find the block that registers `AppDbContext`. The repository registration goes immediately after it.

- [ ] **Step 3.2: Add the repository DI registration**

  Locate:
  ```csharp
  // TODO: Register IContactService and IContactRepository
  ```
  Replace with:
  ```csharp
  builder.Services.AddScoped<IContactRepository, ContactRepository>();
  // TODO: Register IContactService
  ```

  Also add the required `using` at the top of `Program.cs` (if not already present):
  ```csharp
  using Contacts.API.Repositories;
  using Contacts.API.Repositories.Interfaces;
  ```

- [ ] **Step 3.3: Verify the project compiles**

  ```bash
  dotnet build Contacts.API/Contacts.API.csproj
  ```

  Expected: `Build succeeded.` with 0 errors.

---

### Task 4: Verify the test project still compiles

**Files:**
- No changes — confirm nothing is broken.

- [ ] **Step 4.1: Build the test project**

  ```bash
  dotnet build Contacts.Tests/Contacts.Tests.csproj
  ```

  Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 4.2: Build the entire solution**

  ```bash
  dotnet build ContactsApi.slnx
  ```

  Expected: `Build succeeded.` with 0 errors.

---

## Self-Review Notes

- **Spec coverage:** All 7 methods from the SDD interface are present. `AsNoTracking()` applied to all three read methods. Dynamic LINQ filter covers all four `ContactQueryDto` fields. `SaveChangesAsync` isolated per SDD pattern.
- **No placeholders:** All steps contain complete, ready-to-paste code.
- **Type consistency:** `ContactQueryDto`, `Contact`, `Gender`, `AppDbContext` — names match existing files exactly.
- **Out of scope for this plan:** DI registration of `IContactService`, service layer, controller, migrations, tests — all deferred to subsequent milestones.
