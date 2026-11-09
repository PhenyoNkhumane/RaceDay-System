# RaceDay

[![Build and tests](https://github.com/PhenyoNkhumane/prog6212-PhenyoNkhumane/actions/workflows/dotNET.yml/badge.svg)](https://github.com/PhenyoNkhumane/prog6212-PhenyoNkhumane/actions/workflows/dotNET.yml)

RaceDay is an ASP.NET Core API for managing sporting events, categories, participant registrations, clubs, and results. Organisers create and administer events; Participants enrol and view their own records. Authentication uses a server-side session with an HttpOnly `.RaceDay.Session` cookie.

## User roles

### Organiser
An Organiser manages sporting events and their related categories, oversees event administration, and manages event results. Organiser self-registration requires a shared invite code (`Registration:OrganiserCode`).

### Participant
A Participant registers for sporting events, manages their own registrations, and accesses relevant event and result information. Participants can view published event information and use protected operations permitted by their assigned role.

Discipline exists alongside `event_type` because an event’s Run/Walk/Cycle kind is separate from the reusable Discipline catalogue used by categories and club memberships. See [`docs/ERD.md`](docs/ERD.md) and [`docs/ERD.png`](docs/ERD.png).

## Setup

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB or Express)

Development uses a throwaway `Identity:EncryptionKey` in `appsettings.Development.json` (the same base64 key as the tests). Production must supply a real 32-byte key through the `Identity__EncryptionKey` environment variable — do not reuse the development value. Set `Registration:OrganiserCode` for organiser signup (development default: `ChooseYourOwnCode`). Update `ConnectionStrings:DefaultConnection` for your SQL Server instance.

### Create the database

Use either approach:

- **Part 1 / demo seed:** run [`docs/RaceDay.sql`](docs/RaceDay.sql) in SSMS (or `sqlcmd`). It drops and recreates `RaceDay`, applies schema, triggers, check constraints and sample rows.
- **Part 2 / Code-First:** apply the EF migration against an empty database:

```powershell
dotnet ef database update --project RaceDay.Api
```

Deliberate differences: the SQL script uses `varchar`, CHECK constraints and seed data; the migration uses `nvarchar`, omits CHECKs and seed data, and adds the same subtype triggers plus the filtered unique index on active registrations via the model. Prefer the script for Part 1 demos and the migration for the API.

## Run

```powershell
dotnet run --project RaceDay.Api
```

The API listens at `http://localhost:5155` for the `http` profile. In Development, open `http://localhost:5155/swagger`.

### Authentication and sample requests

Registration accepts `Participant` freely, or `Organiser` with a valid `organiserCode`. A successful request creates the matching subtype row and sets the `.RaceDay.Session` cookie:

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

Log in with `POST /api/auth/login` to refresh the session cookie, then call protected endpoints (the browser sends the cookie automatically). Example:

```http
GET /api/registrations/me
```

Log out with `POST /api/auth/logout` (returns `204`). The SQL sample accounts all use the demo password `DemoRaceDay123!`. They and their `example.test` email addresses are for local demonstrations only.

Identity/passport values are encrypted in the database using AES-GCM; the encryption key is supplied through `Identity:EncryptionKey`.

Further design docs:

- [`docs/ERD.md`](docs/ERD.md) / [`docs/ERD.png`](docs/ERD.png)
- [`docs/RaceDay.sql`](docs/RaceDay.sql)
- [`docs/API-Endpoint-Plan.md`](docs/API-Endpoint-Plan.md)
- [`docs/Role-Based-Design.md`](docs/Role-Based-Design.md)
- [`docs/Part3-API-Addendum.md`](docs/Part3-API-Addendum.md)

## Tests

```powershell
dotnet test RaceDay.slnx
```

Controller tests use SQLite in-memory so foreign keys, unique indexes, and transaction behavior are exercised rather than silently ignored.

## Continuous integration

The screenshot below shows the GitHub Actions CI workflow used to restore, build, and test the RaceDay solution.

![Successful CI/CD workflow run](docs/ci-cd-screenshot.png)

GitHub Actions restores, builds, and tests the solution on .NET 10: [`dotNET.yml`](.github/workflows/dotNET.yml).

## Part 1 Video


Unlisted YouTube presentation: https://youtu.be/xuefHGvTkMU?si=qpIO-F2bISGh-el0

This presentation covers the main planning and design components of the RaceDay project: the two user roles, the ERD and database decisions, the API endpoint plan, and a demonstration of the SQL script in SSMS, plus the GitHub repository and its automated validation workflow.

## Part 2 Demonstration Video

The video below demonstrates the RaceDay application, including its key functionalities, API endpoints, authentication, participant registration and unit testing.

**Watch the Part 2 Presentation:**

[RaceDay Presentation – Part 2](https://youtu.be/kTC97r9PPcI?si=13vxoU9B8v5M3D8v)

## Part 3 MVC application

The MVC frontend lives in `RaceDay.Mvc` and uses the Part 2 API for account, event, category, enrolment, result, and profile operations. It does not access the database. The app forwards the API's opaque `.RaceDay.Session` cookie from server-side MVC session storage on each API request.

The Participant area includes upcoming event filters, event details and category entry, personal enrolments, race results, and profile editing. The Organiser area includes an event dashboard, event and category management, enrolment lists, and results capture. Active registrations appear as Confirmed in the UI; the API's separate PaymentStatus remains visible on enrolment pages.

### Run locally

1. Set up SQL Server and apply the API migrations as described above.
2. Configure the API database connection and organiser invite code. Local Development uses Azurite; for real Azure uploads, provide `RaceDayStorage:ConnectionString` through user-secrets or the `RaceDayStorage__ConnectionString` environment variable. The API creates the `raceday-images` container for images.
3. Start Azurite for local blob-storage practice. Development uses `UseDevelopmentStorage=true`; the API creates the `raceday-images` container when an image is uploaded. Azurite evidence is not proof of an Azure Storage Account. Before submission, create a real Azure Storage Account and replace only `RaceDayStorage:ConnectionString` (or `RaceDayStorage__ConnectionString`) with its connection string. Verify the uploaded blob in the Azure portal and its URL in the database.
4. Run the API at `http://localhost:5155`.
5. In another terminal, run `dotnet run --project RaceDay.Mvc`. The MVC app opens at `http://localhost:5186`. Override `ApiSettings:BaseUrl` with `ApiSettings__BaseUrl` when the API uses another address.
6. To start both apps from Visual Studio, open the solution's startup-project settings, select **Multiple startup projects**, and set both `RaceDay.Api` and `RaceDay.Mvc` to **Start**.

### Docker

Start the API so it is reachable from Docker:

```powershell
dotnet run --project RaceDay.Api --urls http://0.0.0.0:5155
```

From the MVC folder, build and run the non-root container:

```powershell
Set-Location RaceDay.Mvc
docker build -t raceday-mvc .
docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5155/ raceday-mvc
```

Open `http://localhost:8080`. The API must be reachable from the container, and its database and Blob configuration must be valid. On Linux Docker hosts, add `--add-host=host.docker.internal:host-gateway` to the run command.

### Part 3 submission items

The Part 3 CI job builds the solution, runs the existing API test suite, and builds the MVC Docker image. Add the Part 3 green workflow screenshot and your unlisted Part 3 video link here before submission. The video must use your own voice and explain the MVC structure, API-backed features, Blob Storage flow, and Docker run.

Part 3 adds nullable `Event.banner_url` and `User.profile_picture_url` columns through the `Part3ImageUploads` migration. This is a documented extension to the Part 1 ERD and SQL script. Apply it with `dotnet ef database update --project RaceDay.Api` before running the updated API.

AI-use disclosure: AI assistance was used to review the Part 3 requirements and scaffold and to assist with implementation. Review, understand, and adapt the code before submission in line with the PoE's academic integrity instructions.
