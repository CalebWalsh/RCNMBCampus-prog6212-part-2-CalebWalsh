## Part 2 - RESTful API

The API is built with ASP.NET Core Web API (.NET 8, C#) and Entity Framework Core (Code-First) on SQL Server. It implements the endpoint plan from Part 1 in `docs/RaceDay_API_Endpoint_Plan.pdf`.

### User roles

**Organiser** - can create, edit and delete their own events, define categories for them, view all enrolments for their events, and capture finish times and positions.

**Participant** - can browse events and categories, enter an event by choosing a category, view and cancel their own enrolments, and view their own results.

Both roles can register, log in, and view and update their own profile.

### Structure

```
RaceDay/
  RaceDay.sln
  RaceDay.Api/             ASP.NET Core Web API
    Auth/                  session keys, roles, SessionAuthorize filter
    Controllers/           Auth, Users, Events, Categories, Enrolments, Results
    Data/                  RaceDayContext (EF Core model matching the Part 1 ERD and SQL)
    Dtos/                  request and response models
    Models/                Club, User, Event, Category, Enrolment, Result
  RaceDay.Tests/           xUnit unit and pipeline tests
.github/workflows/ci.yml
```

### Setup

1. Install the .NET 8 SDK and have SQL Server (or LocalDB) available.
2. Set the connection string in `RaceDay.Api/appsettings.json` (`ConnectionStrings:RaceDayDb`).
3. If the Part 1 `RaceDayDB` database already exists on that server, drop it or change the database name, because EF Core creates the schema itself.
4. In Visual Studio Package Manager Console (Default project: `RaceDay.Api`):
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
   Or from a terminal in the `RaceDay` folder:
   ```
   dotnet ef migrations add InitialCreate --project RaceDay.Api
   dotnet ef database update --project RaceDay.Api
   ```
5. Run the API (`dotnet run --project RaceDay.Api` from the `RaceDay` folder) and open `http://localhost:5080/swagger`.

### Swagger

With the API running, open `http://localhost:5080/swagger`. Every endpoint lists its purpose, the data it expects and the responses it returns. To test protected endpoints, call `POST /api/auth/login` first. Swagger reuses the session cookie it sets, so no extra tooling is needed.

### Authentication and roles

- `POST /api/auth/register` creates an account. The role must be exactly `Organiser` or `Participant`; any other value returns 400.
- Passwords are hashed with BCrypt before they are stored. The plain password is never saved or returned.
- `POST /api/auth/login` starts an ASP.NET Core session and stores the user id and role in it. The browser or client keeps the session cookie `.RaceDay.Session` and sends it on later requests.
- Every non-public endpoint uses the `SessionAuthorize` filter: no session returns 401, a session with the wrong role returns 403.
- Organiser endpoints are also limited to the Organiser who owns the event. Participant endpoints are limited to the Participant's own enrolments and results.

### Running the tests

```
dotnet test
```

The 57 tests use the EF Core in-memory provider and cover the controllers, the role filter, and the full request pipeline (login, session cookie, 401 and 403 responses).

### Deviations from the Part 1 endpoint plan

| Plan | Implementation | Reason |
|---|---|---|
| Login returns a JWT token | Login starts a server-side session and returns the user's details | The Part 2 brief requires session management |
| No logout endpoint | `POST /api/auth/logout` ends the session | A session needs a way to end |
| `DELETE /api/events/{id}` | Also removes the event's categories, enrolments and results | The SQL schema uses no cascading deletes, so they are removed explicitly |
| `POST /api/events/{eventId}/enrolments` 409 for already enrolled | Also returns 409 when the category has reached `MaxParticipants` | Uses the existing `MaxParticipants` column |

### CI/CD

The workflow in `.github/workflows/ci.yml` runs on every push and pull request. It checks that the required files exist (README, solution, projects and the Part 1 files in `docs/`), then restores, builds and runs all unit tests from the `RaceDay` folder.

![CI green build](ci-green-build-part2.png)

### Video presentation

YouTube (unlisted): ADD LINK HERE
