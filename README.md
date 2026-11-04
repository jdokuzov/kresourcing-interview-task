# Claims API

Backend for handling insurance claims. The assignment is in [docs/README.md](docs/README.md).

## Running

Docker must be running: SQL Server and MongoDB are started through Testcontainers. The first start pulls the images, which can take several minutes.

```
dotnet run --project Claims      # Swagger UI at https://localhost:7052/swagger
dotnet test                      # unit tests and API tests
```

## Structure

A request passes through these layers, and each one only knows the layer below it through an interface.

| Folder | Responsibility |
| --- | --- |
| `Controllers` | Map HTTP to service calls and results to status codes. |
| `Contracts` | Request models, which is why clients cannot set an id or a premium. |
| `Services` | Use cases, and the premium formula in `PremiumCalculator`. |
| `Validation` | Business rules. A broken rule becomes a 400 response in `RequestValidationExceptionHandler`. |
| `Persistence` | `IRepository<T>` and the MongoDB mapping. |
| `Domain` | `Claim` and `Cover`, free of framework dependencies. |
| `Auditing` | Audit records, the queue and the worker that writes them. |

## Auditing

Requests no longer wait for the audit database. `Auditer` puts a record on a bounded in-memory `Channel`, and `AuditWorker`, a background service, writes the records to SQL Server. On shutdown the worker finishes what is queued.

The queue lives in memory, so records are lost if the process crashes, and a record that fails to write is logged but not retried. Audit data that must never be lost would need a transactional outbox or a durable broker such as Azure Service Bus; only `Auditer` and `AuditWorker` would change.

## Decisions on unclear requirements

- **Dates are UTC.** A date without an offset is read as UTC, and rules compare whole days.
- **A cover may start today.** Only earlier days count as "in the past".
- **"1 year" is a calendar year,** so it also works across a leap day.
- **A claim is valid on the cover's first and last day,** whereas the last day is not charged in the premium, because the number of days is `EndDate - StartDate`.
- **Discounts add up.** "Additional" is read as 5% + 3% = 8% for yachts and 2% + 1% = 3% for other types.
- **`/Covers/compute` is not validated.** It is a quote, and it returns 0 for an empty or negative period.
- **Added rules:** `EndDate` must be after `StartDate`, `DamageCost` cannot be negative, and the cover of a claim must exist.
