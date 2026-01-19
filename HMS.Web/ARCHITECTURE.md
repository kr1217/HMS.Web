# Architecture & Coding Standards

## 1. Asynchronous Programming Rules
To ensure scalability and prevent threading deadlocks, strict async rules are enforced:

### ✅ Allowed (Async I/O)
Async/Await is permitted **ONLY** at I/O boundaries:
*   **Database Operations** (SQL Queries, Stored Procedures via `DatabaseHelper`)
*   **File Level Operations** (Reading/Writing files)
*   **External HTTP Calls** (APIs)

```csharp
// Correct Repository Pattern
public async Task<List<Patient>> GetAllPatientsAsync() {
    string sql = "SELECT * FROM Patients";
    return await _db.ExecuteQueryAsync(sql, MapPatient);
}
```

### ❌ Prohibited (Fake Async)
Do **NOT** wrap synchronous code in `Task.Run` or `Task.FromResult` just to make it look async.
Business logic must remain synchronous.

```csharp
// VIOLATION
public async Task<int> CalculateTotal(int a, int b) {
    return await Task.Run(() => a + b); // DO NOT DO THIS
}

// CORRECT
public int CalculateTotal(int a, int b) {
    return a + b;
}
```

### ❌ Prohibited (Blocking)
Do **NOT** block async code.
*   No `.Result`
*   No `.Wait()`
*   No `async void` (except top-level Event Handlers in Blazor/WinForms)

## 2. Layer Separation
*   **Components (.razor)**: Orchestrate calls. Can use `await`.
*   **Repositories (DAL)**: Execute SQL. Must be `async`.
*   **Business Logic (Internal)**: Must be synchronous. (e.g., `MapPatient` methods).

## 3. Database Access
*   All DB access must go through `DatabaseHelper`.
*   `DatabaseHelper` methods are strictly `async` to enforce non-blocking I/O.
*   Mapping functions passed to `DatabaseHelper` must be synchronous delegates.
