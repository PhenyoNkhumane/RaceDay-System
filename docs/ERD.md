# RaceDay Entity Relationship Diagram

`User` stores shared login and identity details. Every account has one role (`NOT NULL`) and one matching subtype row. Public registration may create a Participant freely, or an Organiser when a valid invite code is supplied. Events reference Organiser, and Registrations and ClubMemberships reference Participant.

`event_type` and `category_type` are `varchar(10)`. `id_number_or_passport` is `varchar(512)` so encrypted ciphertext fits. Discipline exists alongside `event_type` because an event’s sport kind (Run/Walk/Cycle) is separate from the reusable Discipline catalogue that categories and club memberships classify against.

```mermaid
erDiagram
    USER ||--o| PARTICIPANT : "has subtype"
    USER ||--o| ORGANISER : "has subtype"
    ORGANISER ||--o{ EVENT : creates
    EVENT ||--o{ CATEGORY : contains
    DISCIPLINE ||--o{ CATEGORY : classifies
    PARTICIPANT ||--o{ REGISTRATION : enters
    CATEGORY ||--o{ REGISTRATION : receives
    REGISTRATION ||--o| RESULT : records
    PARTICIPANT ||--o{ CLUB_MEMBERSHIP : holds
    CLUB ||--o{ CLUB_MEMBERSHIP : has
    DISCIPLINE ||--o{ CLUB_MEMBERSHIP : classifies

    USER {
        int id PK
        string email UK
        string password_hash
        string first_name
        string last_name
        string role "varchar(20) NOT NULL"
        encrypted string id_number_or_passport "varchar(512)"
        string asa_permanent_license_number
        datetime created_at
    }
    PARTICIPANT {
        int user_id PK,FK
    }
    ORGANISER {
        int user_id PK,FK
        string provisioned_by
        datetime provisioned_at
    }
    CLUB {
        int id PK
        string name UK
        string registration_number UK
        string contact_email
        string contact_number
        datetime created_at
    }
    EVENT {
        int id PK
        int organiser_id FK
        string title
        string description
        date date
        decimal distance_km
        string event_type "varchar(10)"
        string venue_name
        string city
        string province
        string status
    }
    CATEGORY {
        int id PK
        int event_id FK
        int discipline_id FK
        string name
        string category_type "varchar(10)"
        decimal distance_km
        int min_age
        int max_age
        decimal entry_fee_zar
        int max_slots
        time start_time
    }
    DISCIPLINE {
        int id PK
        string name UK
        string description
    }
    REGISTRATION {
        int id PK
        int participant_id FK
        int category_id FK
        datetime registration_date
        string payment_status
        decimal amount_paid
        string assigned_bib_number
        boolean requires_temp_asa_license
        string medical_aid_name
        string medical_aid_number
        string status
    }
    RESULT {
        int id PK
        int registration_id FK,UK
        time gun_time
        time net_time
        int overall_position
        int category_position
        string status
    }
    CLUB_MEMBERSHIP {
        int id PK
        int participant_id FK
        int club_id FK
        int discipline_id FK
        string membership_number
        date joined_at
        string status
    }
```

The database foreign keys bind event ownership to `Organiser` and participant activity to `Participant`. Constraints and subtype checks prevent a single account from occupying both roles.

![RaceDay ERD](ERD.png)
