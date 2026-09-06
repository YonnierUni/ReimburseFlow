# ReimburseFlow

ReimburseFlow is a full stack solution for managing corporate reimbursement requests.

## Features

- Create reimbursements
- List reimbursements
- Filter by status and category
- Approve or reject requests
- Capture rejection reasons
- Idempotent create requests
- Duplicate prevention
- Optimistic concurrency
- Consistent error handling
- Responsive frontend
- Dockerized local stack
- Automated tests

## Tech Stack

Backend:
- .NET 8
- ASP.NET Core Controllers
- EF Core 8
- SQL Server
- xUnit

Frontend:
- React
- TypeScript
- Vite
- Vitest
- React Testing Library

Infrastructure:
- Docker
- Docker Compose
- nginx

## Architecture

The backend follows Clean Architecture with a small, explicit CQRS style.

- Domain contains business rules and entities.
- Application coordinates use cases through commands and queries.
- Infrastructure contains EF Core persistence implementation details.
- API exposes HTTP controllers and maps errors to HTTP responses.

Writes are modeled as commands. Reads are modeled as queries. The project intentionally does not use MediatR or a Generic Repository; handlers and persistence boundaries are kept explicit and small.

```text
Frontend
   |
HTTP
   v
API Controllers
   |
   v
Application Commands / Queries
   |
   v
Domain

Infrastructure / EF Core
   |
   +--> implements Application persistence abstractions
   |
   v
SQL Server
```

## Business Rules

- Amount must be greater than zero.
- A new reimbursement starts as `Pending`.
- A pending reimbursement can become `Approved` or `Rejected`.
- Final states cannot change.
- Rejection requires a reason.
- `EmployeeId` + `ReceiptNumber` is unique.
- Accidental retries must not duplicate reimbursements.
- Concurrent processing must leave one final consistent state.

## Idempotency

`POST /api/reimbursements` requires an `Idempotency-Key` header.

Reusing an existing Idempotency-Key returns the reimbursement associated with that key instead of creating a new record. Clients are expected to use a new key for a different logical request.

ReimburseFlow stores idempotency records in the `IdempotencyRequests` table, where the primary key protects concurrent races for the same key.

The frontend creates keys with `crypto.randomUUID()`. An identical retry reuses the current key; if the user edits the form, the key is discarded so the next submit represents a new logical attempt.

## Concurrency

ReimburseFlow uses SQL Server `rowversion` for optimistic concurrency.

Only one concurrent state transition can win. Stale or invalid state transitions are returned as `409 Conflict`. When the UI receives a conflict during approve or reject, it keeps the backend error visible and refreshes the list so the screen reflects the server truth.

## API

Endpoints:

- `POST /api/reimbursements`
- `GET /api/reimbursements`
- `GET /api/reimbursements/{id}`
- `POST /api/reimbursements/{id}/approve`
- `POST /api/reimbursements/{id}/reject`
- `GET /health`
- Swagger UI: `/swagger`

Main HTTP status codes:

- `201 Created`: reimbursement created.
- `200 OK`: read or state transition succeeded.
- `400 Bad Request`: invalid request contract or domain input.
- `404 Not Found`: reimbursement was not found.
- `409 Conflict`: duplicate reimbursement, idempotency conflict, concurrency conflict, or invalid state transition.
- `500 Internal Server Error`: unexpected server error with internal details hidden.

## Running with Docker

Prerequisites:

- Docker Desktop or Docker Engine
- Docker Compose

The validated local flow is:

```bash
docker compose --env-file .env.example up --build -d
```

Optionally copy `.env.example` to `.env` and adjust values locally. `SA_PASSWORD` must be defined. The value in `.env.example` is a local development example and must be changed in real environments.

URLs:

- Frontend: http://localhost:5173
- API: http://localhost:5087
- Swagger: http://localhost:5087/swagger
- Health: http://localhost:5087/health

## Database migrations

The current migration is:

- `20260906030600_InitialCreate`

Docker enables:

```text
Database__ApplyMigrationsOnStartup=true
```

The API applies migrations with `Database.MigrateAsync()`. It does not use `EnsureCreated`. Automatic migrations are opt-in through configuration, and cold start from an empty Docker volume was validated.

## Environment variables

Variables used by `.env.example` and `docker-compose.yml`:

- `SA_PASSWORD`: SQL Server `sa` password used by the local Docker stack.
- `API_PORT`: host port mapped to the API container. Default/example: `5087`.
- `FRONTEND_PORT`: host port mapped to the nginx frontend container. Default/example: `5173`.
- `DB_PORT`: host port mapped to SQL Server. Default/example: `1433`.
- `VITE_API_BASE_URL`: API base URL compiled into the frontend build. Default/example: `http://localhost:5087/api`.

## Tests

Backend:

```bash
dotnet test backend/ReimburseFlow.sln
```

Current result: 75 tests passing.

Frontend:

```bash
cd frontend
npm test -- --run
```

Current result: 22 tests passing.

Builds:

```bash
dotnet build backend/ReimburseFlow.sln
```

```bash
cd frontend
npm run build
```

These counts reflect the current project state.

## Repository structure

```text
backend/
  src/
    ReimburseFlow.Api
    ReimburseFlow.Application
    ReimburseFlow.Domain
    ReimburseFlow.Infrastructure
  tests/

frontend/
  src/
    api/
    components/
    hooks/
    models/
    pages/
    test/
  Dockerfile
  nginx.conf

docker-compose.yml
docs/
```

## Error handling

The API returns ProblemDetails-style responses with a `traceId` when applicable.

- `400`: validation errors and general domain input errors.
- `404`: missing reimbursement.
- `409`: duplicate, idempotency, concurrency, and state conflicts.
- `500`: unexpected errors, without exposing internal details.

The backend uses structured logging and avoids logging sensitive information.

## Scope / assumptions

See [docs/ASSUMPTIONS_AND_SCOPE.md](docs/ASSUMPTIONS_AND_SCOPE.md).

## Technical decisions

See [docs/TECHNICAL_DECISIONS.md](docs/TECHNICAL_DECISIONS.md).

## AI usage

See [docs/AI_USAGE.md](docs/AI_USAGE.md).
