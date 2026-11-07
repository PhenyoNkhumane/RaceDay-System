# API Endpoint Plan

**Project:** Race/Event Management System
**Part:** 1 — Endpoint Planning (based on ERD: User, Participant, Organiser, Club, Event, Category, Discipline, Registration, Result, ClubMembership)

Each account has exactly one subtype: Participant or Organiser. Public self-registration accepts either role, creates the matching subtype in the same transaction, and returns a JWT. Only those two account roles call protected endpoints.

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Register a Participant or Organiser account and create the matching subtype | Public | `{ email, password, first_name, last_name, id_number_or_passport, role, asa_permanent_license_number? }` | `201 Created` — `{ user: {...}, token }` |
| POST | `/api/auth/login` | Authenticate an existing user and issue a JWT | Public | `{ email, password }` | `200 OK` — `{ user: {...}, token }` |
| PUT | `/api/auth/password` | Set a new password; temporary-password accounts can access no other protected operation before changing it | Authenticated user | `{ current_password, new_password }` | `204 No Content` |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | View the logged-in user's own profile | Participant / Organiser | — | `200 OK` — user object |
| PUT | `/api/users/me` | Update the logged-in user's own profile | Participant / Organiser | `{ first_name?, last_name?, email?, id_number_or_passport?, asa_permanent_license_number? }` | `200 OK` — updated user object |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | List published events only | Public | — | `200 OK` — array of event response DTOs |
| GET | `/api/events/mine` | List every event the signed-in Organiser owns, including Draft and Cancelled | Organiser | — | `200 OK` — array of event response DTOs |
| GET | `/api/events/{eventId}` | Get a published event; drafts and cancelled events are hidden | Public | — | `200 OK` — event response DTO |
| POST | `/api/events` | Create a new event | Organiser | `{ title, description, date, venue_name, city, province, distance_km, event_type, status }` | `201 Created` — event object; `400 Bad Request` for invalid type or distance |
| PUT | `/api/events/{eventId}` | Update an existing event | Organiser (owner) | `{ title?, description?, date?, venue_name?, city?, province?, distance_km?, event_type?, status? }` | `200 OK` — updated event object; `404 Not Found` if absent |
| DELETE | `/api/events/{eventId}` | Delete an event | Organiser (owner) | — | `204 No Content` |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | List all categories for a given event | Public | — | `200 OK` — array of categories |
| GET | `/api/categories/{categoryId}` | Get details of a single category | Public | — | `200 OK` — category object |
| POST | `/api/events/{eventId}/categories` | Create a category by age range or distance, with discipline, entry fee, capacity and start time | Organiser (owner) | `{ discipline_id, name, category_type, distance_km?, min_age?, max_age?, entry_fee_zar, max_slots, start_time }` | `201 Created` — category object; `400 Bad Request` for invalid bounds; `404 Not Found` if event absent |
| PUT | `/api/categories/{categoryId}` | Update a category | Organiser (owner) | `{ name?, category_type?, distance_km?, min_age?, max_age?, entry_fee_zar?, max_slots?, start_time? }` | `200 OK` — updated category object; `404 Not Found` if absent |
| DELETE | `/api/categories/{categoryId}` | Delete a category | Organiser (owner) | — | `204 No Content` |

## 5. Event Enrolments (Registrations)

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/registrations` | Enter a future published event by selecting an eligible category | Participant | `{ category_id, requires_temp_asa_license?, medical_aid_name?, medical_aid_number? }` | `201 Created` — registration response DTO; `409 Conflict` for duplicate entry or full category; `404 Not Found` if event/category absent |
| GET | `/api/registrations/me` | View the logged-in Participant's own enrolments | Participant | — | `200 OK` — array of registrations |
| GET | `/api/events/{eventId}/registrations` | View all enrolments for an organiser's event | Organiser (owner) | — | `200 OK` — array of registrations |
| GET | `/api/registrations/{registrationId}` | View a single registration's detail | Participant (owner) / Organiser (owner event) | — | `200 OK` — registration object |
| PUT | `/api/registrations/{registrationId}` | Update a registration | Organiser (owner); Participant (owner) limited to their own medical-aid details | Organiser: `{ payment_status?, amount_paid?, assigned_bib_number?, status? }` / Participant: `{ medical_aid_name?, medical_aid_number? }` | `200 OK` — updated registration object |
| DELETE | `/api/registrations/{registrationId}` | Cancel a registration or withdraw from an event | Participant (owner) / Organiser (owner event) | — | `204 No Content`; `404 Not Found` if absent |

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/results` | Record the official race result for a participant's registration, including times, positions and race status | Organiser (owner) | `{ registration_id, gun_time, net_time, overall_position, category_position, status }` | `201 Created` — result object |
| PUT | `/api/results/{resultId}` | Correct/update a captured result | Organiser (owner) | `{ gun_time?, net_time?, overall_position?, category_position?, status? }` | `200 OK` — updated result object |
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
| POST | `/api/clubs` | Register a new club | Organiser | `{ name, registration_number, contact_email, contact_number }` | `201 Created` — club object; `409 Conflict` if name or registration number already exists |
| GET | `/api/clubs/{clubId}` | Get details of a specific club | Public | — | `200 OK` — club object |
| POST | `/api/clubs/{clubId}/memberships` | Join a club under a specific sporting discipline | Participant | `{ discipline_id, membership_number, joined_at }` | `201 Created` — membership response DTO; `409 Conflict` if already a member for that discipline; `404 Not Found` if club absent |
| GET | `/api/users/me/memberships` | View the logged-in Participant's club memberships | Participant | — | `200 OK` — array of memberships |

## Notes on error responses

All endpoints additionally return, where applicable: `400 Bad Request` (validation failure), `401 Unauthorized` (missing/invalid bearer token), `403 Forbidden` (wrong role or not the resource owner), `404 Not Found` (resource does not exist), and `409 Conflict` (uniqueness or capacity conflict). A cancelled registration may be entered again; the participant/category uniqueness rule applies only while a registration is active.
