# RaceDay Role-Based Design

## Account roles

RaceDay has two account roles: `Participant` and `Organiser`. Each account has exactly one role. `User` stores shared identity and login data, while the matching `Participant` or `Organiser` subtype row represents the account's role in the data model.

Participants may self-register freely. Organiser registration also requires a shared invite code configured as `Registration:OrganiserCode` (development default: `ChooseYourOwnCode`). The API validates the role against the fixed allow-list and atomically creates the `User` and matching subtype. The role cannot be changed through profile updates. Successful register/login starts a server-side session; the browser holds only the opaque HttpOnly `.RaceDay.Session` cookie.

Passwords must be at least 12 characters and include upper-case, lower-case, numeric, and non-alphanumeric characters. Login attempts are rate-limited.

## Permissions

| Capability | Participant | Organiser |
|---|---|---|
| Register an account | Yes, self-registration | Yes, with a valid organiser invite code |
| View public events, categories, clubs and results | Yes | Yes |
| Enter a category and manage own registration | Yes | No |
| View own registrations, club memberships and results | Yes | No |
| Create and manage own events and categories | No | Yes |
| List own events including Draft and Cancelled (`GET /api/events/mine`) | No | Yes |
| View registrations and record results for own events | No | Yes |
| Add disciplines or clubs | No | Yes |
| Manage another organiser's event | No | No |

Every protected request must have a valid session and check its role and, for event operations, ownership of the relevant event. Knowing an identifier does not grant access to another user's records.

## Database enforcement

`Event.organiser_id` references `Organiser.user_id`. `Registration.participant_id` and `ClubMembership.participant_id` reference `Participant.user_id`. These foreign keys prevent an account without the appropriate subtype from acting in that capacity. Subtype triggers validate that the subtype matches the role and reject a user appearing in both subtype tables. SQL also guards role changes against an existing subtype mismatch.
