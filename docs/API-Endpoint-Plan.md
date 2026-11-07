# API Endpoint Plan

**Project:** Race/Event Management System
**Part:** 1 — Endpoint Planning (based on ERD: User, Participant, Organiser, Club, Event, Category, Discipline, Registration, Result, ClubMembership)

Each account has exactly one subtype: Participant or Organiser. Public self-registration accepts either role (Organiser also requires a valid `organiserCode`), creates the matching subtype in the same transaction, and starts a server-side session (`.RaceDay.Session` cookie). Only those two account roles call protected endpoints.

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Register a Participant or Organiser account and create the matching subtype | Public | `{ email, password, firstName, lastName, idNumberOrPassport, role, asaPermanentLicenseNumber?, organiserCode? }` | `201 Created` — user profile; sets `.RaceDay.Session` cookie. Organiser registration without a valid `organiserCode` returns `403 Forbidden`. |
| POST | `/api/auth/login` | Authenticate an existing user and start a server-side session | Public | `{ email, password }` | `200 OK` — user profile; sets `.RaceDay.Session` cookie |
| POST | `/api/auth/logout` | Clear the server-side session and expire the session cookie | Authenticated | — | `204 No Content` |
| PUT | `/api/auth/password` | Set a new password for the signed-in user | Authenticated user | `{ currentPassword, newPassword }` | `204 No Content` |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | View the logged-in user's own profile | Participant / Organiser | — | `200 OK` — user object |
| PUT | `/api/users/me` | Update the logged-in user's own profile | Participant / Organiser | `{ firstName?, lastName?, email?, idNumberOrPassport?, asaPermanentLicenseNumber? }` | `200 OK` — updated user object |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | List published events only | Public | — | `200 OK` — array of event response DTOs |
| GET | `/api/events/mine` | List every event the signed-in Organiser owns, including Draft and Cancelled | Organiser | — | `200 OK` — array of event response DTOs |
| GET | `/api/events/{eventId}` | Get a published event; drafts and cancelled events are hidden | Public | — | `200 OK` — event response DTO |
| POST | `/api/events` | Create a new event | Organiser | `{ title, description, date, venueName, city, province, distanceKm, eventType, status }` | `201 Created` — event object; `400 Bad Request` for invalid type or distance |
| PUT | `/api/events/{eventId}` | Update an existing event | Organiser (owner) | `{ title?, description?, date?, venueName?, city?, province?, distanceKm?, eventType?, status? }` | `200 OK` — updated event object; `404 Not Found` if absent |
| DELETE | `/api/events/{eventId}` | Delete an event | Organiser (owner) | — | `204 No Content` |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | List all categories for a given event | Public | — | `200 OK` — array of categories |
| GET | `/api/categories/{categoryId}` | Get details of a single category | Public | — | `200 OK` — category object |
| POST | `/api/events/{eventId}/categories` | Create a category by age range or distance, with discipline, entry fee, capacity and start time | Organiser (owner) | `{ disciplineId, name, categoryType, distanceKm?, minAge?, maxAge?, entryFeeZar, maxSlots, startTime }` | `201 Created` — category object; `400 Bad Request` for invalid bounds; `404 Not Found` if event absent |
| PUT | `/api/categories/{categoryId}` | Update a category | Organiser (owner) | `{ name?, categoryType?, distanceKm?, minAge?, maxAge?, entryFeeZar?, maxSlots?, startTime? }` | `200 OK` — updated category object; `404 Not Found` if absent |
| DELETE | `/api/categories/{categoryId}` | Delete a category | Organiser (owner) | — | `204 No Content` |

## 5. Event Enrolments (Registrations)

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/registrations` | Enter a future published event by selecting an eligible category | Participant | `{ categoryId, requiresTempAsaLicense?, medicalAidName?, medicalAidNumber? }` | `201 Created` — registration response DTO; `409 Conflict` for duplicate entry or full category; `404 Not Found` if event/category absent |
| GET | `/api/registrations/me` | View the logged-in Participant's own enrolments | Participant | — | `200 OK` — array of registrations |
| GET | `/api/events/{eventId}/registrations` | View all enrolments for an organiser's event | Organiser (owner) | — | `200 OK` — array of registrations |
| GET | `/api/registrations/{registrationId}` | View a single registration's detail | Participant (owner) / Organiser (owner event) | — | `200 OK` — registration object |
| PUT | `/api/registrations/{registrationId}` | Update a registration | Organiser (owner); Participant (owner) limited to their own medical-aid details | Organiser: `{ paymentStatus?, amountPaid?, assignedBibNumber?, status? }` / Participant: `{ medicalAidName?, medicalAidNumber? }` | `200 OK` — updated registration object |
| DELETE | `/api/registrations/{registrationId}` | Cancel a registration or withdraw from an event | Participant (owner) / Organiser (owner event) | — | `204 No Content`; `404 Not Found` if absent |

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/results` | Record the official race result for a participant's registration, including times, positions and race status | Organiser (owner) | `{ registrationId, gunTime, netTime, overallPosition, categoryPosition, status }` | `201 Created` — result object |
| PUT | `/api/results/{resultId}` | Correct/update a captured result | Organiser (owner) | `{ gunTime?, netTime?, overallPosition?, categoryPosition?, status? }` | `200 OK` — updated result object |
| GET | `/api/events/{eventId}/results` | View safe race-result fields only; omit participant identifiers and registration/medical-aid details | Public | — | `200 OK` — array of result response DTOs |
| GET | `/api/results/me` | View the logged-in Participant's own results | Participant | — | `200 OK` — array of results |
| GET | `/api/results/{resultId}` | View a single result | Participant (owner) / Organiser (owner event) | — | `200 OK` — result object |

## 7. Additional Supporting Endpoints

These endpoints support the `Discipline`, `Club`, and `ClubMembership` entities in the ERD. Organisers create disciplines and register clubs; these are Organiser permissions and do not introduce a third account role.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/disciplines` | List all available sporting disciplines | Public | — | `200 OK` — array of disciplines |
| POST | `/api/disciplines` | Add a sporting discipline | Organiser | `{ name, description }` | `201 Created` — discipline object; `409 Conflict` if name already exists |
| GET | `/api/clubs` | List all registered clubs | Public | — | `200 OK` — array of clubs |
| POST | `/api/clubs` | Register a new club | Organiser | `{ name, registrationNumber, contactEmail, contactNumber }` | `201 Created` — club object; `409 Conflict` if name or registration number already exists |
| GET | `/api/clubs/{clubId}` | Get details of a specific club | Public | — | `200 OK` — club object |
| POST | `/api/clubs/{clubId}/memberships` | Join a club under a specific sporting discipline | Participant | `{ disciplineId, membershipNumber, joinedAt }` | `201 Created` — membership response DTO; `409 Conflict` if already a member for that discipline; `404 Not Found` if club absent |
| GET | `/api/users/me/memberships` | View the logged-in Participant's club memberships | Participant | — | `200 OK` — array of memberships |

## Notes on error responses

All endpoints additionally return, where applicable: `400 Bad Request` (validation failure), `401 Unauthorized` (missing/invalid session), `403 Forbidden` (wrong role, not the resource owner, or invalid organiser code), `404 Not Found` (resource does not exist), and `409 Conflict` (uniqueness or capacity conflict). A cancelled registration may be entered again; the participant/category uniqueness rule applies only while a registration is active.
