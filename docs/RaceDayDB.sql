--Creating a Database--

create Database RaceDayDB;

use RaceDayDB;

--creating the tables based on the ERD --
--This stores both the Organizers and the Participants, distinguished by the role--
CREATE TABLE Users( 
UserID int Identity(1,1) primary key,
FullName varchar(150) not null,
Email varchar(255) not null unique,
PasswordHash varchar(500) not null,
Role varchar(20) not null  CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant')),
PhoneNumber varchar(20) null,
ProfilePictureUrl varchar(500) null,
CreatedAt DateTime2 not null default getdate()
);

--creating the event type table--
--lookup table for the event type: Run, Walk , Cycle--

create Table EventTypes (
EventTypeID int identity(1,1) primary key,
TypeName varchar(20) not null unique
);

--creating the Event table--
--created by an organizer/ user. linked to an EventType--

create Table Events(
  EventId       INT IDENTITY(1,1) PRIMARY KEY,
    OrganizerId   INT             NOT NULL,
    EventTypeId   INT             NOT NULL,
    Name          NVARCHAR(150)   NOT NULL,
    Description   NVARCHAR(1000)  NULL,
    EventDate     DATETIME2       NOT NULL,
    Location      NVARCHAR(200)   NOT NULL,
    DistanceKm    DECIMAL(6,2)    NOT NULL,
    ImageUrl      NVARCHAR(500)   NULL,
    CreatedAt     DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganizerId) REFERENCES Users(UserId),
    CONSTRAINT FK_Events_EventType FOREIGN KEY (EventTypeId) REFERENCES EventTypes(EventTypeId)
);

USE RaceDayDB;
SELECT * FROM Events;

 --creating the categories table--
 --Age/Distance categories belonging to each event--

 create table Categories(
 CategoryID INT IDENTITY(1,1) PRIMARY KEY,
 EventID INT NOT NULL,
 CategoryName VARCHAR(100) not null,
 MinAge INT NULL,
 MaxAge INT Null,
 CONSTRAINT FK_Categories_Event FOREIGN KEY (EventId) REFERENCES Events(EventId)
 );


--creating the Enrolments table--
--link a user to an event and a category--

 create table Enrolments(
 EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
 ParticipantID int not null,
 EventID INT NOT NULL,
 CategoryID int not null,
 EnrolmentDate DATETIME2 NOT NULL DEFAULT GETDATE(),
 CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
 CONSTRAINT FK_Enrolments_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
 CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
 CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantId, EventId)
 );

 --CREATING THE RESULTS TABLE--
 --  One-to-one with an Enrolment. Captured by the Organiser after the event concludes --

 CREATE TABLE Results( 
 ResultID INT IDENTITY(1,1) PRIMARY KEY,
 EnrolmentID INT NOT NULL UNIQUE,
 FinishTime varchar(20) not null,
 FinishPosition INT NOT NULL,
 CapturedAt INT NOT NULL,
 CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId)
 );
  
  --Populating the tables with the actual data--

  --Populating the EventType table--
  insert into EventTypes values
     ('Run'),
    ('Walk'),
    ('Cycle');


  -- Users: 2 Organisers, 2 Participants--
-- NOTE: PasswordHash values below are placeholders for seed purposes only.--
-- The API in Part 2 must hash real passwords before insert (e.g. BCrypt).--
INSERT INTO Users (FullName, Email, PasswordHash, Role, PhoneNumber) VALUES
    ('Thabo Nkosi',    'thabo.nkosi@raceday.co.za',    'HASHED_PASSWORD_1', 'Organiser',   '0821234567'),
    ('Lindiwe Dube',   'lindiwe.dube@raceday.co.za',   'HASHED_PASSWORD_2', 'Organiser',   '0827654321'),
    ('Sarah van Wyk',  'sarah.vanwyk@example.com',     'HASHED_PASSWORD_3', 'Participant', '0731112222'),
    ('Jarred Peters',  'jarred.peters@example.com',    'HASHED_PASSWORD_4', 'Participant', '0733334444');

    -- Events: 3 events, one per Organiser/type--

INSERT INTO Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm, ImageUrl) VALUES
    (1, 1, 'Joburg City Marathon',
        'An iconic road running event through the streets of Johannesburg.',
        '2026-11-08 06:00:00', 'Johannesburg, Gauteng', 42.20, NULL),
    (1, 2, 'Sandton Charity Walk',
        'A community fun walk raising funds for local schools.',
        '2026-10-04 07:30:00', 'Sandton, Gauteng', 5.00, NULL),
    (2, 3, 'Cape Winelands Cycle Tour',
        'A scenic cycling event through the Cape Winelands.',
        '2026-09-20 06:30:00', 'Stellenbosch, Western Cape', 94.70, NULL);

      -- Categories: at least one per event
INSERT INTO Categories (EventId, CategoryName, MinAge, MaxAge) VALUES
    (1, '10km',        NULL, NULL),
    (1, '21km',        NULL, NULL),
    (1, '42km',        NULL, NULL),
    (2, 'Under 20',    12,   19),
    (2, 'Senior',      20,   59),
    (3, '94.7km Open', NULL, NULL);

    -- Enrolments: sample enrolments linking Participants to Events/Categories
INSERT INTO Enrolments (ParticipantId, EventId, CategoryId) VALUES
    (3, 1, 2),  -- Sarah enrols in the Joburg City Marathon, 21km category
    (4, 1, 3),  -- Jarred enrols in the Joburg City Marathon, 42km category
    (3, 2, 5);  -- Sarah enrols in the Sandton Charity Walk, Senior category



    DROP TABLE IF EXISTS Results;


CREATE TABLE Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT             NOT NULL UNIQUE,
    FinishTime      NVARCHAR(20)    NOT NULL,
    FinishPosition  INT             NOT NULL,
    CapturedAt      DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId)
);

USE RaceDayDB;

SELECT * FROM Users;
SELECT * FROM EventTypes;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;

USE RaceDayDB;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;

USE master;
GO
ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE RaceDayDB;