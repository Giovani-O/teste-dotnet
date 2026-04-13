# Implementation Plan: ContactService

## Task 4.1: CreateAsync Implementation

### Steps:
1. Create `Services/Interfaces/IContactService.cs` with the interface
2. Create `Services/ContactService.cs` with the implementation class
3. Implement `CreateAsync` method:
   - Validate `Name` is non-empty → `ValidationException`
   - Validate `Birthdate` is not in the future → `ValidationException`
   - Validate computed `Age > 0` → `ValidationException`
   - Validate `Age >= 18` → `ValidationException`
   - Map DTO to entity
   - Set `Id = Guid.NewGuid()`, `IsActive = true`, `CreatedAt = UpdatedAt = DateTime.UtcNow`
   - Call `repository.AddAsync()` and `SaveChangesAsync()`
   - Return response DTO

## Task 4.2: Get and List Implementation

### Steps:
1. Implement `GetAllAsync(ContactQueryDto query)`:
   - Delegate to `repository.GetAllActiveAsync(query)`
   - Map results to response DTOs
   - Return list

2. Implement `GetByIdAsync(Guid id)`:
   - Call `repository.GetActiveByIdAsync(id)`
   - If null → throw `NotFoundException`
   - Return response DTO

## Task 4.3: Update, Activate/Deactivate, Delete Implementation

### Steps:
1. Implement `UpdateAsync(Guid id, UpdateContactDto dto)`:
   - Fetch active contact by ID → `NotFoundException` if missing
   - Validate any provided fields (same rules as creation)
   - Apply partial update via mapper
   - Set `UpdatedAt = DateTime.UtcNow`
   - Persist and return updated response DTO

2. Implement `ActivateAsync(Guid id)`:
   - Fetch contact by ID (any status) → `NotFoundException` if missing
   - If already active → throw `ConflictException`
   - Set `IsActive = true`, `UpdatedAt = DateTime.UtcNow`, persist

3. Implement `DeactivateAsync(Guid id)`:
   - Fetch contact by ID (any status) → `NotFoundException` if missing
   - If already inactive → throw `ConflictException`
   - Set `IsActive = false`, `UpdatedAt = DateTime.UtcNow`, persist

4. Implement `DeleteAsync(Guid id)`:
   - Fetch contact by ID (any status) → `NotFoundException` if missing
   - Call `repository.DeleteAsync()` and `SaveChangesAsync()`

## Validation Helpers (shared logic)

Extract validation into private methods to avoid duplication between Create and Update:
- `ValidateName(string? name)`
- `ValidateBirthdate(DateOnly birthdate)` - checks not in future and age > 0
- `ValidateAge(DateOnly birthdate)` - checks age >= 18