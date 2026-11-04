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
| `Persistence` | `IRepository<T>` and the MongoDB mapping. |
| `Domain` | `Claim` and `Cover`, free of framework dependencies. |
| `Auditing` | Audit records. |
