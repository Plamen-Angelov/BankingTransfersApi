# Banking Transfers API

Backend API for processing bank transfer requests, built with .NET 10 and Clean Architecture.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (local instance or remote)

## Configuration

Open `src/BankingTransfers.API/appsettings.json` and update the connection string if needed:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=BankingTransfersDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

The default targets a local SQL Server instance using Windows Authentication.

## Database Setup

Run the provided SQL script to create the tables and seed test data:

```
sql/create_and_seed.sql
```

Alternatively, apply EF Core migrations directly:

```bash
cd src/BankingTransfers.API
dotnet ef database update
```

The application also seeds test data automatically on startup if the database is empty.

## Running the Application

```bash
cd src/BankingTransfers.API
dotnet run
```

Swagger UI is available at: `https://localhost:{port}/swagger`

## Running the Tests

```bash
dotnet test tests/BankingTransfers.Tests
```

## Seeded Test Data

Two user profiles are available out of the box:

| Username | UserProfileUID | IBAN | Can Create Transfer |
|---|---|---|---|
| john.doe | `8d5fa53a-fe3b-4f74-b5c4-7b43dc4e2187` | `BG12TEST1234567890` | Yes |
| john.doe | `8d5fa53a-fe3b-4f74-b5c4-7b43dc4e2187` | `BG99TEST0000000001` | No |
| jane.smith | `78f90a74-c792-45ca-a6b1-4dac75f4604d` | `BG34TEST9876543210` | Yes |
| jane.smith | `78f90a74-c792-45ca-a6b1-4dac75f4604d` | `BG99TEST0000000002` | No |

## API Endpoints

All endpoints require the `UserProfileUID` header (GUID of the acting user).

### POST /api/transfers/validate
Validates transfer data without creating a transfer. Returns 200 if valid, 400 if validation fails, 404 if the user profile is not found.

### POST /api/transfers
Creates a transfer request with status `Pending`. Returns 200 with the transfer ID on success.
Supports idempotency — submitting the same `idempotencyKey` twice returns the existing transfer without creating a duplicate.

### GET /api/transfers/{transferUId}
Returns the full details and current status of a transfer. Returns 404 if the transfer does not exist or belongs to a different user.

## Background Processing

A background service polls every 30 seconds and processes all `Pending` transfers whose `ExecutionDate` has been reached. Processing outcome depends on the transfer amount:

| Amount | Outcome |
|---|---|
| Below 10,000 | Marked as **Processed** immediately |
| Between 10,000 and 20,000 | CoreSystem returns a temporary error — the service retries up to 3 times with a 3-second delay between attempts. To simulate a real-life scenario where a transient error may or may not resolve on retry, each attempt has a randomised 30% chance of success. If all 3 retries are exhausted without success, the transfer is marked as **Failed**. |
| Above 20,000 | Marked as **Failed** immediately (CoreSystem rejects the amount) |

The background service uses an atomic SQL `UPDATE ... OUTPUT` statement to claim transfers, which makes it safe to run multiple instances in parallel without double-processing.

## Technologies

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQL Server
- MediatR
- FluentValidation
- Polly (retry policy)
- xUnit + Moq + FluentAssertions (tests)
- Swagger / OpenAPI
