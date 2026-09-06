# Technical Decisions

This document summarizes the main technical choices made in ReimburseFlow, the alternatives considered, and the reasons behind the selected approach.

## 1. Clean Architecture with lightweight CQRS

Context:
The backend needs separation between HTTP concerns, use cases, business rules, and persistence.

Options considered:
- Traditional layered MVC
- Vertical Slice Architecture
- Clean Architecture with MediatR
- Clean Architecture with explicit handlers

Decision:
Use Clean Architecture with explicit command/query handlers and a lightweight CQRS approach.

Why this option:
- Domain rules remain independent from HTTP and EF Core.
- Application use cases stay explicit and independently testable.
- Commands and queries communicate intent clearly.
- Dependency direction remains toward the core.

Why the alternatives were not selected:
- Traditional MVC could concentrate too much business behavior in controllers/services.
- Full Vertical Slice Architecture was not necessary for the current size and existing domain separation.
- MediatR would add indirection and dependency setup without a current need for pipelines or mediator behaviors.

Validation:
- Domain, Application, API, and persistence-focused tests cover the responsibilities separately.
- Full-stack manual validation confirmed the complete flow.

## 2. EF Core as the persistence technology

Context:
The application needs relational persistence, migrations, constraints, transactions, and optimistic concurrency.

Options considered:
- EF Core
- Dapper
- EF Core + Dapper
- Raw ADO.NET

Decision:
Use EF Core 8 as the only data-access technology.

Why this option:
- Provides migrations.
- Supports change tracking.
- Enables LINQ-based queries.
- `DbContext` provides transaction coordination and unit-of-work behavior for the current persistence model.
- Supports concurrency through SQL Server `rowversion`.
- Fits the current read/write complexity.

Why the alternatives were not selected:
- Dapper would require more manual SQL and does not add enough value for the current simple queries.
- EF Core + Dapper would introduce two persistence approaches without a concrete need.
- Raw ADO.NET would add unnecessary boilerplate and manual mapping.

Validation:
- Automated tests cover persistence-related behavior.
- Docker-based validation confirmed migrations, constraints, persistence, and database behavior.

## 3. SQL Server

Context:
The system needs relational integrity, unique constraints, transactional persistence, and optimistic concurrency.

Options considered:
- SQL Server
- PostgreSQL
- SQLite
- In-memory persistence

Decision:
Use SQL Server.

Why this option:
- Native `rowversion` support works directly with the selected optimistic concurrency strategy.
- Supports the required relational constraints and transactions.
- Integrates cleanly with EF Core.
- Available as a Docker container for a repeatable local environment.

Why the alternatives were not selected:
- PostgreSQL could satisfy most relational requirements, but would require a different concurrency-token strategy.
- SQLite is useful for lightweight local environments but does not represent the selected concurrency behavior as closely.
- In-memory persistence cannot validate real database constraints or concurrency behavior.

Validation:
- Database creation from an empty volume.
- Migration application.
- Persistence across restart.
- Unique constraint and concurrency scenarios.

## 4. Database-backed idempotency instead of cache-based idempotency

Context:
Create requests must tolerate retries and simultaneous submissions without generating duplicate reimbursements.

Options considered:
- SQL Server table
- Redis
- In-memory cache
- Application-only duplicate checks

Decision:
Persist idempotency keys in the `IdempotencyRequests` SQL Server table.

Why this option:
- Idempotency state survives process restarts.
- The idempotency record and reimbursement are persisted together through the same EF Core SaveChanges operation.
- SQL Server is already the system of record.
- A primary key provides race-safe protection for simultaneous use of the same key.

Why the alternatives were not selected:
- In-memory cache would lose state on restart and would not work reliably across multiple application instances.
- Redis would be a valid distributed option but would add infrastructure not currently needed.
- Application-only checks are vulnerable to concurrent races.

Validation:
- Repeated same-key requests return the same reimbursement.
- Simultaneous same-key requests produce one stored reimbursement.
- State persists across container restarts.

## 5. Business duplicate protection with application check + database unique constraint

Context:
The same `EmployeeId` and `ReceiptNumber` must not create multiple reimbursements.

Options considered:
- Application-only duplicate check
- Database-only unique constraint
- Both

Decision:
Use both an application-level check and a database unique constraint on `EmployeeId + ReceiptNumber`.

Why this option:
- The application check provides a clearer normal error path.
- The database constraint is the final race-safe guarantee.
- Together they cover both normal requests and simultaneous races.

Why the alternatives were not selected:
- Application-only validation cannot fully prevent concurrent duplicates.
- Database-only validation would be safe but would rely on constraint failures for normal duplicate detection.

Validation:
- Duplicate create returns `409 Conflict`.
- The same receipt number for a different employee is allowed.
- The database unique constraint protects against concurrent duplicate creation.

## 6. Optimistic concurrency with SQL Server rowversion

Context:
Two users may attempt to approve or reject the same pending reimbursement at the same time.

Options considered:
- Application-only state checks
- Pessimistic locking
- Optimistic concurrency using `rowversion`

Decision:
Use optimistic concurrency with SQL Server `rowversion`.

Why this option:
- Detects concurrent updates at persistence time.
- Does not hold database locks during user interaction.
- Fits a short state-transition workflow.
- Integrates directly with EF Core.

Why the alternatives were not selected:
- Application-only checks can race between read and write.
- Pessimistic locking would introduce unnecessary locking complexity for short operations.

Validation:
- Simultaneous approve/reject scenarios leave one consistent final state.
- The losing request receives `409 Conflict`.

## 7. ASP.NET Core Controllers instead of Minimal APIs

Context:
The HTTP layer needs request models, validation attributes, Swagger, centralized API behavior, and multiple related endpoints.

Options considered:
- ASP.NET Core Controllers
- Minimal APIs

Decision:
Use ASP.NET Core Controllers.

Why this option:
- Natural fit for grouped reimbursement endpoints.
- Works cleanly with request DTOs and DataAnnotations.
- Integrates with automatic model validation and Swagger.
- Keeps HTTP concerns organized.

Why the alternatives were not selected:
- Minimal APIs would also work, but would not provide a meaningful simplification for the current endpoint set and validation style.

Validation:
- API tests cover request validation, endpoint behavior, and HTTP response mapping.

## 8. Centralized ProblemDetails error handling

Context:
Clients need consistent and understandable HTTP errors without internal exception details leaking.

Options considered:
- try/catch in each controller
- raw string error responses
- centralized global exception handling with ProblemDetails

Decision:
Use a global exception handler and ProblemDetails responses.

Why this option:
- Centralizes exception-to-status mapping.
- Keeps controllers focused on HTTP orchestration.
- Provides consistent error format and `traceId`.
- Allows different domain/application conflicts to map predictably to `400`, `404`, `409`, and `500`.

Why the alternatives were not selected:
- Per-controller try/catch duplicates error-handling logic.
- Raw error strings are harder for clients to consume consistently.

Validation:
- API tests cover validation errors, not found, duplicate conflicts, concurrency conflicts, and invalid state transitions.

## 9. Separate API validation from Domain invariants

Context:
Some validations are technical HTTP/storage constraints while others are actual business invariants.

Options considered:
- Put all validation in Domain
- Put all validation in API
- Split validation by responsibility

Decision:
Use request model validation for API/storage constraints and Domain validation for business invariants.

Why this option:
- Max-length limits such as 100/1000 belong to the request/persistence contract.
- Rules such as `Amount > 0`, required business values, and valid state transitions remain protected by the Domain.
- Domain rules remain valid even when invoked outside HTTP.

Why the alternatives were not selected:
- Putting technical storage lengths in Domain would turn persistence details into business rules.
- API-only validation could allow invalid domain states when use cases are invoked from another entry point.

Validation:
- Max-length violations return `400` before reaching SQL Server.
- Domain invariants remain independently tested.

## 10. `409 Conflict` for invalid state transitions

Context:
A client may hold stale state and try to approve/reject a reimbursement that is no longer Pending.

Options considered:
- Return `400 Bad Request`
- Return `409 Conflict`
- Detect frontend behavior using error-message text

Decision:
Map `InvalidStateTransitionException` to `409 Conflict`.

Why this option:
- The request itself is structurally valid.
- The operation conflicts with the current resource state.
- The frontend can react to an HTTP status instead of parsing exception text.
- It aligns stale-state handling with concurrency semantics.

Why the alternatives were not selected:
- `400` does not express the resource-state conflict as clearly.
- Message-text matching would tightly couple frontend behavior to backend error wording.

Validation:
- Backend tests cover invalid transitions as `409`.
- Frontend tests confirm refresh after conflict.
- Manual stale-window testing confirmed the UI reflects the server state.

## 11. React hooks + Fetch API

Context:
The frontend needs create, list, filters, approve/reject, loading, errors, and refresh behavior.

Options considered:
- React hooks + Fetch API
- React Query / TanStack Query
- Redux
- Axios

Decision:
Use React hooks with a small centralized Fetch API wrapper.

Why this option:
- Current data flow is small and localized.
- Native Fetch is sufficient for the HTTP contract.
- Custom hooks provide enough separation for loading/error/data behavior.
- Avoids adding state-management dependencies without a current need.

Why the alternatives were not selected:
- React Query would provide caching/retry features that are not currently necessary for a single-screen workflow.
- Redux would add global-state infrastructure for state that is currently local.
- Axios would add a dependency without providing necessary functionality beyond the current Fetch wrapper.

Validation:
- Frontend tests cover creation, filters, errors, conflict refresh, and double-submit protection.
- Manual testing confirmed API outage/recovery and stale-state behavior.

## 12. Docker Compose for local execution

Context:
The complete application requires frontend, API, and SQL Server to run together consistently.

Options considered:
- Manual local setup
- Custom shell/PowerShell scripts
- Docker Compose
- Hosted development dependencies

Decision:
Use Docker Compose.

Why this option:
- Starts the complete stack with one command.
- Keeps SQL Server and runtime dependencies isolated.
- Provides consistent ports and environment configuration.
- Allows database persistence through a named volume.

Why the alternatives were not selected:
- Manual setup depends more heavily on machine-specific configuration.
- Custom scripts would still require each dependency to be installed locally.
- Hosted dependencies would add external availability and configuration requirements.

Validation:
- Full build.
- Health endpoint.
- Frontend access.
- API access.
- Clean database startup.
- Persistence after restart.

## 13. Opt-in automatic EF Core migrations

Context:
The Docker stack should be able to initialize an empty database automatically, while migration execution should not be forced in every environment.

Options considered:
- `EnsureCreated`
- Unconditional migrations on startup
- Manual migrations only
- Configuration-controlled migrations

Decision:
Use `Database.MigrateAsync()` controlled by `Database:ApplyMigrationsOnStartup`.

Why this option:
- Docker startup can initialize the schema automatically.
- Migration history remains managed by EF Core.
- Other environments can disable startup migrations.
- Avoids `EnsureCreated`, which bypasses the migration workflow.

Why the alternatives were not selected:
- `EnsureCreated` is not appropriate for a migration-based relational schema.
- Always running migrations removes deployment control.
- Manual-only migrations would make the local Docker startup less self-contained.

Validation:
- Empty-volume startup successfully applies `20260906030600_InitialCreate`.
- The application starts and operates normally after migration.

## Decision evolution

| Decision | Current rationale | Revisit if |
| --- | --- | --- |
| EF Core only | Current reads and writes are simple and benefit from a single persistence model. | Reporting or query-performance requirements justify SQL-first read models. |
| SQL-backed idempotency | Keeps idempotency durable and close to the current system of record. | Idempotency must coordinate across multiple services or distributed workloads. |
| SQL Server `rowversion` | Provides optimistic concurrency for short state transitions. | The workflow requires explicit locking or long-running coordination. |
| React hooks + Fetch | The frontend state and server interactions are currently small and localized. | The application grows into multiple screens requiring shared caching or server-state management. |
| Docker Compose | Provides a repeatable local full-stack environment. | Deployment or orchestration requirements require a different runtime model. |
| Startup migrations | Makes local Docker startup self-contained. | A deployment pipeline takes ownership of database migrations. |
