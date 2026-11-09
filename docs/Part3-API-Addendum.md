# Part 3 API and schema addendum

The API keeps the Part 2 session and role enforcement. MVC sends requests to the API and forwards the API's `.RaceDay.Session` cookie from its own server-side session.

## Added endpoints

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/events/dashboard` | Organiser | Lists the signed-in organiser's events, active enrolment totals, and upcoming flags. |
| POST | `/api/events/{eventId}/banner` | Owning Organiser | Uploads a PNG, JPEG, or WebP event banner to the configured Azure Blob container and stores its URL. |
| POST | `/api/users/me/profile-picture` | Signed-in user | Uploads a PNG, JPEG, or WebP profile image to Azure Blob Storage and stores its URL. |
| GET | `/api/events/{eventId}/results/manage` | Owning Organiser | Returns results for organiser result review, including registration IDs needed to avoid duplicate entry. |

Image uploads are limited to 5 MB. The API configuration key is `RaceDayStorage:ConnectionString` (environment variable `RaceDayStorage__ConnectionString`); the container defaults to `raceday-images`. The container serves images through their returned URLs. MVC does not use Azure credentials and does not communicate with Azure directly.

## Response additions

Event responses include `bannerUrl`. User profile responses include `profilePictureUrl`. Enrolment responses include event/category labels and a participant display name where relevant. Participant result responses include event name, event date, category name, finish time, and total finishers. Public event-result responses do not expose registration IDs.

The API's registration status remains `Active` or `Cancelled`, separate from payment status. Since entries are recorded immediately, MVC presents `Active` as “Confirmed”; it does not add a new Pending/Confirmed database state.

## Schema extension

The Part 3 migration adds nullable `Event.banner_url` and `User.profile_picture_url` columns (`nvarchar(2048)`). These are deliberate additions beyond the Part 1 ERD and `docs/RaceDay.sql`; the original Part 1 artifacts remain unchanged. Apply the EF migration with `dotnet ef database update --project RaceDay.Api`.
