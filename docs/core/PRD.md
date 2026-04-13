# Product Requirements Document — Contacts API

## 1. Overview

A RESTful API for managing contacts. Supports full CRUD operations with business rules around contact age, active status, and data integrity. Intended as a clean, well-structured .NET reference implementation.

---

## 2. Functional Requirements

### 2.1 Contact Entity

A contact has the following fields:

| Field | Type | Description |
|---|---|---|
| `id` | UUID | Primary key, generated server-side |
| `name` | string | Full name of the contact |
| `birthdate` | DateOnly | Date of birth |
| `gender` | enum | One of: `Male`, `Female`, `Other` |
| `isActive` | bool | Active status; defaults to `true` on creation |
| `age` | int | Calculated from `birthdate`; never stored in the database |
| `createdAt` | DateTime | Timestamp of when the record was created; set server-side |
| `updatedAt` | DateTime | Timestamp of the last update; set server-side on every write |

### 2.2 Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/contacts` | Create a new contact |
| `GET` | `/api/contacts` | List all active contacts (with optional filters) |
| `GET` | `/api/contacts/{id}` | Get an active contact by ID |
| `PATCH` | `/api/contacts/{id}` | Partially edit an active contact |
| `PATCH` | `/api/contacts/{id}/activate` | Activate an inactive contact |
| `PATCH` | `/api/contacts/{id}/deactivate` | Deactivate an active contact |
| `DELETE` | `/api/contacts/{id}` | Permanently delete a contact |

### 2.3 Filtering (GET /api/contacts)

The list endpoint accepts the following optional query parameters:

| Parameter | Type | Behavior |
|---|---|---|
| `name` | string | Case-insensitive partial/contains match |
| `gender` | string | Exact match (`Male`, `Female`, `Other`) |
| `birthdateFrom` | DateOnly | Inclusive lower bound on birthdate |
| `birthdateTo` | DateOnly | Inclusive upper bound on birthdate |

### 2.4 Business Rules

**Creation:**
- `name` is required and must be non-empty.
- `birthdate` is required.
- `birthdate` must not be in the future.
- The contact's age (derived from `birthdate`) must be greater than 0.
- The contact's age must be at least 18 years at the time of creation.
- `gender` is required and must be a valid enum value.
- `isActive` defaults to `true`.
- `createdAt` and `updatedAt` are set server-side; clients cannot provide them.

**Edit (PATCH):**
- All fields are optional; only provided fields are updated.
- Any provided field is subject to the same validation rules as on creation.
- The contact must be active; an inactive contact returns `404 Not Found`.

**List / Get by ID:**
- Only active contacts (`isActive = true`) are returned.
- An inactive contact requested by ID returns `404 Not Found`.

**Activate / Deactivate:**
- Activating an already-active contact returns `409 Conflict`.
- Deactivating an already-inactive contact returns `409 Conflict`.

**Delete:**
- Hard delete — the record is permanently removed from the database.
- Can be applied to any contact regardless of active status.

### 2.5 Age Calculation

Age is computed at runtime using the following logic:

```
age = today.Year - birthdate.Year
if (today < birthdate this year) age--
```

This value is never persisted. EF is configured to ignore the `Age` property.

---

## 3. HTTP Status Codes

| Code | Meaning |
|---|---|
| `201 Created` | Contact successfully created (includes `Location` header) |
| `200 OK` | Successful read or update |
| `204 No Content` | Successful delete |
| `400 Bad Request` | Validation failure |
| `404 Not Found` | Contact not found or inactive |
| `409 Conflict` | Activation state conflict |
| `500 Internal Server Error` | Unexpected server error |

---

## 4. Error Response Shape

All error responses follow a consistent JSON structure:

```json
{
  "statusCode": 400,
  "message": "Validation failed.",
  "errors": ["Birthdate cannot be in the future.", "Contact must be at least 18 years old."]
}
```

- `errors` is always an array to support multiple messages in a single response.
- `500` responses return a generic message — no stack traces or internal details are exposed.

---

## 5. API Documentation

Swagger UI is exposed via Swashbuckle for interactive API documentation and manual testing.

---

## 6. Non-Functional Requirements

- **SOLID principles** — applied throughout, especially Single Responsibility and Dependency Inversion.
- **Clean Code** — meaningful names, small focused methods, no dead code.
- **Clean Architecture** — strict layering: Controllers, Services, Repositories, DTOs, Mappers, Types.
- **Testability** — all business logic is unit-testable in isolation via interface-based dependencies.

---

## 7. Tech Stack

| Concern | Technology |
|---|---|
| Framework | .NET 10 (C#) |
| Database | SQL Server (Docker container) |
| ORM | Entity Framework Core with LINQ to Entities |
| API Docs | Swashbuckle (Swagger) |
| Testing | xUnit |
| Test DB | SQL Server in a dedicated Docker container |
