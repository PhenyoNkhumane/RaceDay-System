IF DB_ID('RaceDay') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDay SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDay;
END
GO
CREATE DATABASE RaceDay;
GO
USE RaceDay;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================
-- TABLE: Discipline
-- =============================================

CREATE TABLE Discipline (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255)
);
GO
-- =============================================
-- TABLE: User
-- =============================================
CREATE TABLE [User] (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  ROLE VARCHAR(20) NOT NULL,
  id_number_or_passport VARCHAR(512),
  asa_permanent_license_number VARCHAR(50),
  created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_User_Role CHECK (
    ROLE IN ('Organiser', 'Participant')
  )
);

GO
-- =============================================
-- TABLES: Participant and Organiser account types
-- =============================================
CREATE TABLE Participant (
  user_id INT PRIMARY KEY,
  CONSTRAINT FK_Participant_User FOREIGN KEY (user_id) REFERENCES [User] (id)
);
GO
CREATE TABLE Organiser (
  user_id INT PRIMARY KEY,
  provisioned_by VARCHAR(255) NOT NULL,
  provisioned_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_Organiser_User FOREIGN KEY (user_id) REFERENCES [User] (id)
);
GO
-- A user may belong to only one account type. Role checks also prevent a
-- subtype row being created for a user with the other role.
CREATE TRIGGER TR_Participant_EnforceRole ON Participant
AFTER INSERT AS
BEGIN
  IF EXISTS (SELECT 1 FROM inserted i JOIN [User] u ON u.id=i.user_id WHERE u.ROLE <> 'Participant')
    THROW 51001, 'Participant subtype requires Participant role.', 1;
  IF EXISTS (SELECT 1 FROM inserted i JOIN Organiser o ON o.user_id=i.user_id)
    THROW 51002, 'A user cannot be both Participant and Organiser.', 1;
END;
GO
CREATE TRIGGER TR_Organiser_EnforceRole ON Organiser
AFTER INSERT AS
BEGIN
  IF EXISTS (SELECT 1 FROM inserted i JOIN [User] u ON u.id=i.user_id WHERE u.ROLE <> 'Organiser')
    THROW 51003, 'Organiser subtype requires Organiser role.', 1;
  IF EXISTS (SELECT 1 FROM inserted i JOIN Participant p ON p.user_id=i.user_id)
    THROW 51004, 'A user cannot be both Participant and Organiser.', 1;
END;
GO
CREATE TRIGGER TR_User_RoleMatchesSubtype ON [User]
AFTER UPDATE AS
BEGIN
  IF EXISTS (
    SELECT 1 FROM inserted i
    WHERE (EXISTS (SELECT 1 FROM Participant p WHERE p.user_id = i.id) AND i.ROLE <> 'Participant')
       OR (EXISTS (SELECT 1 FROM Organiser o WHERE o.user_id = i.id) AND i.ROLE <> 'Organiser')
  )
    THROW 51005, 'User role must match its account subtype.', 1;
END;
GO
-- =============================================
-- TABLE: Club
-- =============================================
CREATE TABLE Club (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  name VARCHAR(200) NOT NULL UNIQUE,
  registration_number VARCHAR(100) UNIQUE,
  contact_email VARCHAR(255),
  contact_number VARCHAR(30),
  created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

GO
-- =============================================
-- TABLE: Event
-- =============================================
CREATE TABLE Event (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  organiser_id INT NOT NULL,
  title VARCHAR(200) NOT NULL,
  description VARCHAR(1000),
  DATE DATE NOT NULL,
  distance_km DECIMAL(6, 2) NOT NULL,
  event_type VARCHAR(10) NOT NULL,
  venue_name VARCHAR(200) NOT NULL,
  city VARCHAR(100) NOT NULL,
  province VARCHAR(100) NOT NULL,
  status VARCHAR(20) NOT NULL,
  CONSTRAINT FK_Event_Organiser FOREIGN KEY (organiser_id) REFERENCES Organiser (user_id),
  CONSTRAINT CK_Event_Distance CHECK (distance_km > 0),
  CONSTRAINT CK_Event_Type CHECK (event_type IN ('Run', 'Walk', 'Cycle')),
  CONSTRAINT CK_Event_Status CHECK (status IN ('Draft', 'Published', 'Cancelled'))
);

GO
-- =============================================
-- TABLE: Category
-- =============================================
CREATE TABLE Category (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  event_id INT NOT NULL,
  discipline_id INT NOT NULL,
  name VARCHAR(150) NOT NULL,
  category_type VARCHAR(10) NOT NULL,
  distance_km DECIMAL(6, 2),
  min_age INT,
  max_age INT,
  entry_fee_zar DECIMAL(10, 2) NOT NULL,
  max_slots INT NOT NULL,
  start_time TIME NOT NULL,
  CONSTRAINT FK_Category_Event FOREIGN KEY (event_id) REFERENCES Event (id),
  CONSTRAINT FK_Category_Discipline FOREIGN KEY (discipline_id) REFERENCES Discipline (id),
  CONSTRAINT CK_Category_Distance CHECK (distance_km > 0),
  CONSTRAINT CK_Category_Type CHECK (
    (category_type = 'Distance' AND distance_km IS NOT NULL AND min_age IS NULL AND max_age IS NULL)
    OR (category_type = 'Age' AND distance_km IS NULL AND min_age IS NOT NULL AND max_age IS NOT NULL AND min_age >= 0 AND max_age >= min_age)
  ),
  CONSTRAINT CK_Category_EntryFee CHECK (entry_fee_zar >= 0),
  CONSTRAINT CK_Category_MaxSlots CHECK (max_slots > 0)
);

GO
-- =============================================
-- TABLE: Registration
-- =============================================
CREATE TABLE Registration (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  participant_id INT NOT NULL,
  category_id INT NOT NULL,
  registration_date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  payment_status VARCHAR(20) NOT NULL,
  amount_paid DECIMAL(10, 2) NOT NULL,
  assigned_bib_number VARCHAR(20),
  requires_temp_asa_license BIT NOT NULL DEFAULT 0,
  medical_aid_name VARCHAR(150),
  medical_aid_number VARCHAR(50),
  status VARCHAR(20) NOT NULL DEFAULT 'Active',
  CONSTRAINT FK_Registration_Participant FOREIGN KEY (participant_id) REFERENCES Participant (user_id),
  CONSTRAINT FK_Registration_Category FOREIGN KEY (category_id) REFERENCES Category (id),
  CONSTRAINT CK_Registration_PaymentStatus CHECK (payment_status IN ('Pending', 'Paid', 'Refunded')),
  CONSTRAINT CK_Registration_AmountPaid CHECK (amount_paid >= 0),
  CONSTRAINT CK_Registration_Status CHECK (status IN ('Active', 'Cancelled'))
);
CREATE UNIQUE INDEX UX_Registration_Active_Participant_Category
  ON Registration (participant_id, category_id)
  WHERE status = 'Active';
CREATE INDEX IX_Registration_Category ON Registration (category_id);
CREATE INDEX IX_Registration_Participant ON Registration (participant_id);

GO
-- =============================================
-- TABLE: ClubMembership
-- =============================================
CREATE TABLE ClubMembership (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  participant_id INT NOT NULL,
  club_id INT NOT NULL,
  discipline_id INT NOT NULL,
  membership_number VARCHAR(100),
  joined_at DATE NOT NULL DEFAULT CONVERT(DATE, SYSUTCDATETIME()),
  status VARCHAR(20) NOT NULL DEFAULT 'Active',
  CONSTRAINT FK_ClubMembership_Participant FOREIGN KEY (participant_id) REFERENCES Participant (user_id),
  CONSTRAINT FK_ClubMembership_Club FOREIGN KEY (club_id) REFERENCES Club (id),
  CONSTRAINT FK_ClubMembership_Discipline FOREIGN KEY (discipline_id) REFERENCES Discipline (id),
  CONSTRAINT CK_ClubMembership_Status CHECK (status IN ('Active', 'Inactive')),
  CONSTRAINT UQ_ClubMembership_Participant_Discipline UNIQUE (participant_id, discipline_id)
);

GO
-- =============================================
-- TABLE: Result
-- =============================================

CREATE TABLE Result (
    id INT IDENTITY(1,1) PRIMARY KEY,
    registration_id INT NOT NULL,
    gun_time TIME,
    net_time TIME,
    overall_position INT,
    category_position INT,
    status VARCHAR(20) NOT NULL,

    CONSTRAINT FK_Result_Registration
        FOREIGN KEY (registration_id)
        REFERENCES Registration(id),

    CONSTRAINT CK_Result_Status
        CHECK (status IN ('Finished', 'DNF', 'DNS', 'DQ')),

    CONSTRAINT CK_Result_OverallPosition
        CHECK (overall_position IS NULL OR overall_position > 0),

    CONSTRAINT CK_Result_CategoryPosition
        CHECK (category_position IS NULL OR category_position > 0),

    CONSTRAINT UQ_Result_Registration
        UNIQUE (registration_id)
);
GO
CREATE INDEX IX_Event_Organiser ON Event (organiser_id);
CREATE INDEX IX_Category_Event ON Category (event_id);
CREATE INDEX IX_Category_Discipline ON Category (discipline_id);
CREATE INDEX IX_ClubMembership_Club ON ClubMembership (club_id);
CREATE INDEX IX_ClubMembership_Discipline ON ClubMembership (discipline_id);
GO
-- =============================================
-- SAMPLE DATA
-- =============================================

-- DISCIPLINES
INSERT INTO Discipline (name, description)
VALUES
('Running', 'Road running and marathon events'),
('Walking', 'Competitive and recreational walking events'),
('Cycling', 'Road cycling events');
GO

-- USERS
-- 2 Organisers
INSERT INTO [User]
    (email, password_hash, first_name, last_name, role,
     id_number_or_passport, asa_permanent_license_number)
VALUES
('organiser1@example.test', 'AQAAAAIAAYagAAAAEJDB04h6MBZAsPwLb8OKoq20gEB8GFY2KE0j+I5bLnTDqp13U3W3ln7KJEzNpsYmYg==',
 'Alex', 'Example', 'Organiser',
 NULL, NULL),

('organiser2@example.test', 'AQAAAAIAAYagAAAAEJDB04h6MBZAsPwLb8OKoq20gEB8GFY2KE0j+I5bLnTDqp13U3W3ln7KJEzNpsYmYg==',
 'Sam', 'Sample', 'Organiser',
 NULL, NULL);
INSERT INTO Organiser (user_id, provisioned_by)
SELECT id, 'Demo seed' FROM [User]
WHERE email IN ('organiser1@example.test', 'organiser2@example.test');

-- 2 Participants
INSERT INTO [User]
    (email, password_hash, first_name, last_name, role,
     id_number_or_passport, asa_permanent_license_number)
VALUES
('participant1@example.test', 'AQAAAAIAAYagAAAAEJDB04h6MBZAsPwLb8OKoq20gEB8GFY2KE0j+I5bLnTDqp13U3W3ln7KJEzNpsYmYg==',
 'Taylor', 'Runner', 'Participant',
 NULL, 'DEMO-ASA-001'),

('participant2@example.test', 'AQAAAAIAAYagAAAAEJDB04h6MBZAsPwLb8OKoq20gEB8GFY2KE0j+I5bLnTDqp13U3W3ln7KJEzNpsYmYg==',
 'Jordan', 'Racer', 'Participant',
 NULL, 'DEMO-ASA-002');
INSERT INTO Participant (user_id)
SELECT id FROM [User] WHERE email IN ('participant1@example.test', 'participant2@example.test');
GO

-- CLUBS
INSERT INTO Club
    (name, registration_number, contact_email, contact_number)
VALUES
('Johannesburg Running Club', 'JRC001',
 'info@jrc.co.za', '0115551001'),

('Gauteng Cycling Club', 'GCC001',
 'info@gcc.co.za', '0115551002');
GO

-- EVENTS
DECLARE @organiser1_id INT = (SELECT id FROM [User] WHERE email = 'organiser1@example.test');
DECLARE @organiser2_id INT = (SELECT id FROM [User] WHERE email = 'organiser2@example.test');

INSERT INTO Event
    (organiser_id, title, description, date, distance_km, event_type,
     venue_name, city, province, status)
VALUES
(@organiser1_id,
 'Johannesburg Road Race',
 'Annual road running event.',
 '2026-10-10',
 21.00, 'Run',
 'Zoo Lake Sports Grounds',
 'Johannesburg',
 'Gauteng',
 'Published'),

(@organiser1_id,
 'Soweto Community Run',
 'Community-focused running event.',
 '2026-11-15',
 10.00, 'Run',
 'Orlando Stadium',
 'Soweto',
 'Gauteng',
 'Published'),

(@organiser2_id,
 'Gauteng Cycling Challenge',
 'Competitive road cycling event.',
 '2026-12-05',
 40.00, 'Cycle',
 'Midrand Sports Centre',
 'Midrand',
 'Gauteng',
 'Draft');
GO

-- CATEGORIES
INSERT INTO Category
    (event_id, discipline_id, name, category_type, distance_km, min_age, max_age,
     entry_fee_zar, max_slots, start_time)
VALUES
(1, 1, 'Under 20', 'Age', NULL, 0, 19, 120.00, 200, '07:00'),
(1, 1, 'Senior', 'Age', NULL, 60, 120, 120.00, 200, '07:00'),
(1, 1, '10km Road Race', 'Distance', 10.00, NULL, NULL, 150.00, 1000, '07:00'),
(1, 1, '21km Half Marathon', 'Distance', 21.00, NULL, NULL, 250.00, 800, '06:30'),
(1, 1, '42km Marathon', 'Distance', 42.00, NULL, NULL, 400.00, 500, '06:00'),

(2, 1, '5km Fun Run', 'Distance', 5.00, NULL, NULL, 80.00, 1500, '08:00'),
(2, 2, '10km Walk', 'Distance', 10.00, NULL, NULL, 120.00, 500, '07:30'),

(3, 3, '50km Cycling Race', 'Distance', 50.00, NULL, NULL, 300.00, 500, '06:30'),
(3, 3, '100km Cycling Race', 'Distance', 100.00, NULL, NULL, 500.00, 300, '06:00');
GO

-- CLUB MEMBERSHIPS
DECLARE @participant1_id INT = (SELECT id FROM [User] WHERE email = 'participant1@example.test');
DECLARE @participant2_id INT = (SELECT id FROM [User] WHERE email = 'participant2@example.test');

INSERT INTO ClubMembership
    (participant_id, club_id, discipline_id,
     membership_number, joined_at, status)
VALUES
(@participant1_id, 1, 1, 'DEMO-JRC-001', '2026-01-15', 'Active'),
(@participant2_id, 1, 1, 'DEMO-JRC-002', '2026-02-01', 'Active'),
(@participant1_id, 2, 3, 'DEMO-GCC-001', '2026-03-10', 'Active');
GO

-- REGISTRATIONS
DECLARE @participant1_id INT = (SELECT id FROM [User] WHERE email = 'participant1@example.test');
DECLARE @participant2_id INT = (SELECT id FROM [User] WHERE email = 'participant2@example.test');

INSERT INTO Registration
    (participant_id, category_id, registration_date,
     payment_status, amount_paid, assigned_bib_number,
     requires_temp_asa_license, medical_aid_name,
     medical_aid_number)
VALUES
( @participant1_id, 4, '2026-08-20', 'Paid', 250.00, 'DEMO-JR001',
 0, 'Discovery Health', 'DH100001'),

( @participant2_id, 3, '2026-08-21', 'Paid', 150.00, 'DEMO-JR002',
 0, 'Bonitas', 'BO100002'),

( @participant1_id, 6, '2026-08-25', 'Pending', 80.00, NULL,
 1, 'Discovery Health', 'DH100001');
GO

-- RESULTS
INSERT INTO Result
    (registration_id, gun_time, net_time,
     overall_position, category_position, status)
VALUES
(1, '01:02:35', '01:01:50', 25, 8, 'Finished'),

(2, '02:15:20', '02:13:45', 47, 12, 'Finished');
GO
