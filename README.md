# RaceDay
## User roles

### Organiser
An Organiser manages sporting events and their related categories, oversees event administration, and manages event results. Organisers have access to protected operations according to their assigned role and permissions.

### Participant
A Participant registers for sporting events, manages their own registrations, and accesses relevant event and result information. Participants can view published event information and use protected operations permitted by their assigned role.
[![Build and tests](https://github.com/PhenyoNkhumane/prog6212-PhenyoNkhumane/actions/workflows/dotNET.yml/badge.svg)](https://github.com/PhenyoNkhumane/prog6212-PhenyoNkhumane/actions/workflows/dotNET.yml)

RaceDay is an ASP.NET Core API for managing sporting events, categories, participant registrations, clubs, and results.

## Quick start

### Prerequisites

- .NET 10 SDK
- SQL Server or SQL Server LocalDB

Configure a local JWT signing key (at least 32 bytes) and database connection string. Do not commit the key:

```powershell
$env:Jwt__SigningKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:Identity__EncryptionKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Keep both keys stable and secret; the identity encryption key must remain available to decrypt existing participant records. Update `ConnectionStrings:DefaultConnection` in user secrets or `appsettings.Development.json` for your SQL Server instance, then create a fresh database by running `docs/RaceDay.sql`. Existing databases from earlier versions need a data migration that encrypts their plaintext identity values before they can be used; this repository does not yet provide that upgrade migration. Start the API:

```powershell
dotnet run --project RaceDay.Api
```

The API listens at `http://localhost:5155` for the `http` profile. In Development, open `http://localhost:5155/swagger`.

### Authentication and sample requests

Registration accepts either `Participant` or `Organiser`. A request creates the matching subtype row and returns a JWT:

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "runner@example.test",
  "password": "RaceDayDemo123!",
  "firstName": "Taylor",
  "lastName": "Runner",
  "idNumberOrPassport": "TEST-ID-001",
  "role": "Participant"
}
```

Log in with `POST /api/auth/login` to get `{ "user": ..., "token": "..." }`, then send `Authorization: Bearer <token>` to protected endpoints. For example:

```http
GET /api/registrations/me
Authorization: Bearer <token>
```

The SQL sample accounts all use the demo password `DemoRaceDay123!`. They and their `example.test` email addresses are for local demonstrations only.

## Design and data

- [`docs/ERD.md`](docs/ERD.md) is the authoritative entity relationship diagram source; [`docs/ERD.png`](docs/ERD.png) is its generated image.
- [`docs/RaceDay.sql`](docs/RaceDay.sql) creates the SQL Server schema and sample data.
- [`docs/API-Endpoint-Plan.md`](docs/API-Endpoint-Plan.md) documents routes and response contracts.
- [`docs/Role-Based-Design.md`](docs/Role-Based-Design.md) describes account roles and authorization.

Public event endpoints return published events only. Public results use dedicated response DTOs and do not include registration entities, participant IDs, or medical-aid details.
Identity/passport values are encrypted in the database using AES-GCM; the encryption key is supplied through `Identity:EncryptionKey`.

## Tests

Run the test suite from the repository root:

```powershell
dotnet test RaceDay.slnx
```

Controller tests use SQLite in-memory so foreign keys, unique indexes, and transaction behavior are exercised rather than silently ignored.

## Continuous integration
### CI/CD Workflow Evidence

The screenshot below shows the GitHub Actions CI workflow used to restore, build, and test the RaceDay solution.

![Successful CI/CD workflow run](docs/ci-cd-screenshot.png)


GitHub Actions restores, builds, and tests the solution on .NET 10: [`dotNET.yml`](.github/workflows/dotNET.yml).
## Unlisted YouTube Presentation
https://youtu.be/xuefHGvTkMU?si=qpIO-F2bISGh-el0
This presentation has covered the main planning and design components of my RaceDay project.

I introduced the two user roles, explained the Entity Relationship Diagram and the database-design decisions, discussed the API Endpoint Plan, and demonstrated the SQL script using SQL Server Management Studio.

I also showed the GitHub repository and its automated validation workflow.

These components work together to document the system's structure, define how its data is organised, plan how the application communicates, and support the development process.
