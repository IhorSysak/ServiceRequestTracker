# ServiceRequestTracker

Tech stack: .NET 10, ASP.NET Core (controllers), EF Core + PostgreSQL, RabbitMQ, FluentValidation, Scalar (OpenAPI), xUnit + FluentAssertions + Testcontainers, Docker Compose.

## How to start

```bash
cp .env.example .env   # first time only: set your own values in .env
docker compose up --build
```

The database schema is created automatically. The configuration is read from `.env`, which is not committed: create it from `.env.example`.

* **API + Scalar:** http://localhost:8080/scalar/
* **RabbitMQ UI:** http://localhost:15672 (`RABBITMQ_USER` / `RABBITMQ_PASSWORD`)
* **pgAdmin:** http://localhost:5050 (`PGADMIN_EMAIL` / `PGADMIN_PASSWORD`)

To view tables in pgAdmin, go to **Add new server**:
* **Name:** Any name
* **Host name/address:** `postgres`
* **Port:** `5432`
* **Maintenance database:** `service_requests`
* **Username:** From `.env` file (`POSTGRES_USER`)
* **Password:** From `.env` file (`POSTGRES_PASSWORD`)

## Testing

I covered only the part that I considered most important. The coverage percentage can be increased.

* **Unit:** Status logic (`StatusTransitions`, `ServiceRequest.ChangeStatus`).
* **Integration:** Information regarding the creation and changes to the status is provided to the consumer; duplicate messages are stored only once.

## Key Decisions

* **Outbox Pattern:** The request and event are stored in a single transaction. A background process polls the table every 5 seconds.
* **Idempotency:** Ensured using `MessageId`.
* **Resilience:** Retry logic for reconnecting to components.
* **Dead Letter Queue (DLQ):** Implemented for failing messages.
* **Concurrency:** Optimistic concurrency via `xmin`.
* **Database Management:** Auto-migration feature enabled.

## What I Would Implement Next

* Cleaning up processed outbox entries.
* Health checks for services.
* Metrics (OpenTelemetry).
* Circuit breaker pattern (Polly).
* Retry queue with TTL instead of retries within the process; a rollback tool using DLQ.
* Expand integration tests for the HTTP layer.