# Claims API

Backend for handling insurance claims. The assignment is in [docs/README.md](docs/README.md).

## Running

Docker must be running: SQL Server, MongoDB and the Service Bus emulator are started through Testcontainers. The first start pulls the images, which can take several minutes.

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
| `Persistence` | `IRepository<T>`, `IUnitOfWork` and the MongoDB mapping. |
| `Domain` | `Claim` and `Cover`, free of framework dependencies. |
| `Auditing` | Audit records and the path they take from the outbox to the audit database. |

## Auditing

Requests do not wait for the audit database, and an audit record cannot get lost once a change is saved. It takes three steps:

1. **Outbox.** `Auditer` adds an `AuditEvent` to the `outbox` collection, and `SaveChangesAsync` saves it in one MongoDB transaction with the claim or cover it describes.
2. **Relay.** `OutboxRelay` publishes events to the `audit-events` Service Bus queue and deletes each one once it is sent.
3. **Consumer.** `AuditConsumer` receives the messages and `AuditWriter` writes them to SQL Server.

A broker alone would not be enough: saving the claim and sending the message would still be two separate writes, and a crash between them would lose the record. If Service Bus is down, events wait in the outbox, and if SQL Server is down, they wait on the queue.

Delivery is at least once, so an event can arrive twice. The audit tables have a unique index on `MessageId`, the id of the event, and `AuditWriter` ignores events it has already written. A message that keeps failing ends up in the dead-letter queue, which needs monitoring.

Running several instances is safe but wasteful, because their relays can publish the same event. An event that the relay can never send would hold up the ones behind it. Audit records arrive with a delay of about a second, and their order is only guaranteed by `Created`.

MongoDB runs as a replica set, because transactions require one. Locally, Service Bus is the emulator, whose license `Program.cs` accepts through `WithAcceptLicenseAgreement`. A real deployment needs a real namespace with a queue like the one in `servicebus-emulator.json`, and a longer message time to live than the emulator's maximum of one hour.

## Decisions on unclear requirements

- **Dates are UTC.** A date without an offset is read as UTC, and rules compare whole days.
- **A cover may start today.** Only earlier days count as "in the past".
- **"1 year" is a calendar year,** so it also works across a leap day.
- **A claim is valid on the cover's first and last day,** whereas the last day is not charged in the premium, because the number of days is `EndDate - StartDate`.
- **Discounts add up.** "Additional" is read as 5% + 3% = 8% for yachts and 2% + 1% = 3% for other types.
- **`/Covers/compute` is not validated.** It is a quote, and it returns 0 for an empty or negative period.
- **Added rules:** `EndDate` must be after `StartDate`, `DamageCost` cannot be negative, and the cover of a claim must exist.
