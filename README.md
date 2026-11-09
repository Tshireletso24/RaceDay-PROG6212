RaceDay – PROG6212 Portfolio of Evidence
System Description

RaceDay is a full-stack, web-based event management system developed for the South African road running, walking, and cycling community.

The platform allows Event Organisers to create and manage events, categories, participant enrolments, and results. Participants can browse upcoming events, enter events, and track their personal performance history.

This repository contains the Portfolio of Evidence (POE), submitted progressively across three parts:

Part 1 – System Planning: Entity Relationship Diagram (ERD), API endpoint plan, and SQL database script.
Part 2 – RESTful API: ASP.NET Core Web API built in C#, connected to SQL Server, with unit tests and CI/CD.
Part 3 – MVC Web Application: ASP.NET Core MVC application consuming the API, Azure Blob Storage integration, and Docker containerisation.
User Roles

The system supports two distinct user roles:

Organiser
Create, edit, and delete events.
Manage event categories.
View participant enrolments for their events.
Confirm or cancel enrolments.
Capture participant results.
Participant
Register and log in.
Browse upcoming events.
Enter events by selecting a category.
View personal enrolments.
Track personal results and performance history.
Manage their profile.
Part 1 – System Planning and Database

All Part 1 planning documents are located in the /docs folder:

RaceDay_ERD.png – Entity Relationship Diagram.
RaceDay EndPoint.docx – API Endpoint Plan.
RaceDayDB.sql – SQL database creation and seed script.
Running the SQL Script
Open SQL Server Management Studio (SSMS).
Open docs/RaceDayDB.sql.
Execute the CREATE DATABASE RaceDayDB; statement on its own.
Execute the remainder of the script to create all tables and seed sample data.
Part 1 CI/CD

A GitHub Actions workflow validates the repository structure, confirming that the /docs folder exists and contains the required files.

Latest successful build:

!Part 1 CI/CD Green Build

Part 1 Video Demonstration

Unlisted YouTube video:

https://youtube.com/shorts/AEKODOYWIJU?si=fPHDPaaOuJMtye71

The video covers:

The ERD and reasoning behind each entity and relationship.
The API endpoint plan and key design decisions.
Role enforcement and resource nesting.
Execution of the SQL script in SSMS, from database creation to seeded sample data.
Part 1 AI Usage Disclosure

This project used AI assistance (Claude) for planning guidance on database design decisions, structuring the API endpoint plan, and debugging SQL Server errors encountered while writing and testing the database script. All code was typed and tested independently by the student in SSMS.

Part 2 – RESTful API

The RaceDay API is built using ASP.NET Core Web API (.NET 10) and C#.

It uses:

Entity Framework Core (Code-First).
SQL Server.
Server-side sessions for authentication.
BCrypt for password hashing.
Swagger UI for API documentation.
xUnit for automated testing.

The project is located in /Part2-API.

Project Structure
text
Part2-API/
├── RaceDay.slnx
├── RaceDay.Api/
│   ├── Controllers/
│   ├── Models/
│   ├── Dtos/
│   ├── Data/
│   ├── Security/
│   └── Migrations/
└── RaceDay.Api.Tests/

Role-Based Access Control

Role rules are enforced server-side for protected endpoints.

401 Unauthorized – User is not authenticated.
403 Forbidden – User does not have the required role or attempts to manage an event they do not own.
Setup and Run

Prerequisites:

.NET 10 SDK.
SQL Server Express or LocalDB.
Clone the repository.
Open a terminal in Part2-API/RaceDay.Api.
Configure ConnectionStrings:DefaultConnection in appsettings.json to match your SQL Server instance.
Create the database using Entity Framework Core migrations:
powershell
dotnet tool install --global dotnet-ef
dotnet ef database update

Start the API:
powershell
dotnet run --launch-profile http


Swagger documentation:

http://localhost:5261/swagger

The API database is named RaceDayApiDB.

Authentication
POST /api/auth/register – Registers a user and stores a BCrypt password hash.
POST /api/auth/login – Starts a server-side session.
POST /api/auth/logout – Ends the current session.

Swagger maintains the session cookie, allowing authenticated requests to protected endpoints.

Organiser registration requires an invite code configured through OrganiserInviteCode. The demonstration value should be configured securely and must not be treated as a production secret.

API Endpoints
Area	Endpoints
Authentication	POST /api/auth/register, /login, /logout
Profile	GET, PUT /api/users/me
Events	GET /api/events, GET /api/events/{id}, POST, PUT, DELETE
Categories	GET, POST /api/events/{eventId}/categories, PUT, DELETE /api/categories/{id}
Enrolments	POST /api/enrolments, GET /api/enrolments/me, GET /api/events/{eventId}/enrolments, PUT /api/enrolments/{id}/status
Results	POST /api/results, GET /api/results/me
Lookup	GET /api/eventtypes
Unit Tests

Run the automated tests:

powershell
cd Part2-API
dotnet test


The project contains 64 unit tests covering:

Registration and login.
Event management by Organisers.
Role-based access restrictions.
Enrolment processing.
Results management.

Tests use an in-memory database and do not modify the actual SQL Server database.

Part 2 CI/CD

The GitHub Actions workflow .github/workflows/dotnet-ci.yml builds the solution and executes the unit tests on every push.

Latest successful build:

!Part 2 CI

Deviations from the Part 1 Plan
Change	Reason
Status column added to Enrolments	Supports Pending, Confirmed, and Cancelled statuses.
PUT /api/enrolments/{id}/status	Allows Organisers to confirm or cancel enrolments.
organiserCode added to registration	Prevents unrestricted Organiser registration.
Additional event query parameters	Supports event filtering and Organiser dashboards.
Additional HTTP status codes	Provides safer conflict handling.
Database named RaceDayApiDB	Keeps the original Part 1 RaceDayDB script intact.
Part 2 Video Demonstration

Unlisted YouTube video:

[Insert Part 2 YouTube link here]

Part 2 AI Usage Disclosure

AI assistance (Claude) was used for planning guidance, debugging, and development support. Implementation and testing were carried out as part of the student's project work.

Part 3 – MVC Web Application and Docker

Part 3 introduces the ASP.NET Core MVC web application, which consumes the RaceDay RESTful API.

The MVC application provides the user-facing interface through which Organisers and Participants interact with the system.

The project is located in /Part3-Web.

Technologies Used
ASP.NET Core MVC.
ASP.NET Core Web API.
C#.
HTML, CSS and JavaScript.
Azure Blob Storage integration.
Docker.
Git and GitHub.
Project Structure
text
Part3-Web/
└── RaceDay.Web/
    ├── Controllers/
    ├── Models/
    ├── Views/
    ├── wwwroot/
    ├── Dockerfile
    └── Program.cs

Running the Application Locally

Prerequisites:

.NET 10 SDK.
SQL Server.
Docker Desktop (for containerised deployment).
1. Start the API

Navigate to the API project:

powershell
cd Part2-API/RaceDay.Api


Start the API using:

powershell
dotnet run --no-launch-profile --urls "http://0.0.0.0:5261"


The API listens on port 5261.

2. Build the Docker Image

Open another PowerShell terminal and navigate to:

powershell
cd Part3-Web/RaceDay.Web


Build the image:

powershell
docker build -t raceday-web .

3. Run the Docker Container
powershell
docker run -d --name raceday-web-container -p 8080:8080 raceday-web


The MVC application is accessible at:

http://localhost:8080

Docker Configuration

The RaceDay MVC application is containerised using Docker.

The container exposes port 8080, while the backend API runs separately on port 5261.

When the MVC application runs inside Docker Desktop on Windows, its API base URL should use:

text
http://host.docker.internal:5261


The API must listen on an interface accessible from Docker rather than being restricted to 127.0.0.1.

This configuration allows the MVC container to communicate with the API running on the Windows host.

Docker Management Commands

View running containers:

powershell
docker ps


View application logs:

powershell
docker logs raceday-web-container


Check port mappings:

powershell
docker port raceday-web-container


Stop the container:

powershell
docker stop raceday-web-container


Remove the container:

powershell
docker rm raceday-web-container

Docker Verification

The following checks were successfully performed:

Verification	Result
Docker image build	Successful
MVC container startup	Successful
Application listening on port 8080	Confirmed
Docker port mapping	Confirmed
Windows connection to localhost:8080	Successful
MVC home page HTTP response	HTTP 200 OK
API port reachable from Windows	Successful

The container logs confirmed:

text
Now listening on: http://[::]:8080
Application started.
Hosting environment: Production
Content root path: /app


The MVC home page returned:

text
HTTP/1.1 200 OK


Note: Successful API communication from inside the Docker container, including login and event retrieval, must be verified separately before claiming complete end-to-end Docker functionality.

Part 3 CI/CD

Latest successful build:

[Insert Part 3 GitHub Actions build evidence here.]

Part 3 Video Demonstration

Unlisted YouTube video:

[Insert Part 3 YouTube link here]

Part 3 AI Usage Disclosure

AI assistance was used for troubleshooting Docker configuration, understanding container networking, and improving project documentation. Application configuration and testing were performed as part of the student's development work.

Repository and Submission Information

This repository contains the complete RaceDay Portfolio of Evidence for PROG6212.

It includes:

Part 1 system planning and database documentation.
Part 2 RESTful API and automated tests.
Part 3 MVC web application and Docker configuration.
GitHub Actions CI/CD workflows.
Supporting documentation and demonstration evidence.
Author

Developed as part of the PROG6212 academic project.