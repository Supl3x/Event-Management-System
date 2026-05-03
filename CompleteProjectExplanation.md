# Complete Project Explanation

## 1. Introduction

I built this event management system as a full-stack ASP.NET Core MVC application for campus events and competitions. The system is designed to support user registration, event and competition creation, team registration, payment proof upload and verification, ticket issuance, notifications, and volunteer/staff assignments.

This report explains the entire project step-by-step, with a heavy focus on database design, DBMS concepts, and the relationships between frontend, backend, and the PostgreSQL data store.

## 2. System Overview

### What the project does

- Provides a portal for students to register for campus events and competitions.
- Allows organizers to create events and competitions.
- Supports team-based and individual competition registration.
- Accepts payment proof uploads and verifies payments.
- Issues tickets once payment is approved.
- Sends notifications to users and supports realtime updates via Supabase.
- Includes role-based access control for students, organizers, volunteers, and admins.

### Main features

- User authentication and registration with BCrypt password hashing.
- Role profiles for `Student`, `Organizer`, `Volunteer`, and `Admin`.
- Event creation and management.
- Competition creation, details, and booking flow.
- Individual and team registration workflows.
- Payment proof upload, verification, approval, and rejection.
- Ticket generation and status tracking.
- Notification creation and listing.
- Supabase realtime broadcasts for seat updates and notifications.

### Technologies used

- HTML/CSS via Razor views and Bootstrap.
- JavaScript for client-side enhancements and realtime updates.
- C# and ASP.NET Core MVC for backend logic and controllers.
- Entity Framework Core for database access and ORM mapping.
- PostgreSQL hosted on Supabase as the relational database.
- Npgsql for PostgreSQL connectivity.
- Cookie-based authentication and authorization.

## 3. Architecture

### How frontend, backend, and database are connected

- The browser requests a page from the ASP.NET Core backend.
- The backend routes the request to a controller action.
- The controller uses `ApplicationDbContext` to query or update PostgreSQL through EF Core.
- The controller prepares a model and returns a Razor view.
- Razor renders dynamic HTML on the server and sends it back to the browser.
- The browser renders the page and optionally runs client-side JavaScript.
- For realtime behavior, JavaScript may connect to Supabase realtime channels.

### Step-by-step data flow

1. User submits an action in the browser, such as clicking "Register" or "Upload Payment".
2. The browser sends an HTTP request to the ASP.NET Core backend.
3. ASP.NET Core routing maps the request to the appropriate controller action.
4. The controller queries or updates the database via `ApplicationDbContext` and EF Core.
5. Database operations generate SQL statements executed against PostgreSQL.
6. The controller constructs a view model and returns a Razor view.
7. The backend renders HTML and sends it to the browser.
8. The browser displays the page and may execute JavaScript for realtime updates.
9. If applicable, the backend also publishes realtime notifications or seat updates via Supabase.

## 4. Frontend Explanation

### HTML, CSS, and JavaScript usage

- Razor views in `Views/` generate HTML pages on the server.
- CSS is provided by Bootstrap and custom styles in `wwwroot/css/site.css`.
- The shared layout is defined in `Views/Shared/_Layout.cshtml`, which includes global scripts and the page shell.
- JavaScript files such as `wwwroot/js/site.js` handle UI interactions and animations.

### Frontend interaction with APIs

- The frontend mostly uses HTML forms and standard POST requests to controller endpoints.
- For example, registration forms post to `Registration/Create`, and payment uploads post to `Payment/Create`.
- The browser does not use a single-page application framework; it uses server-rendered pages and form-based navigation.

### Dynamic UI updates

- Pages are rendered on the server with the latest data, so each request produces dynamic content.
- There is also a realtime JavaScript component in `wwwroot/js/realtime.js`.
- That script expects a Supabase realtime configuration and uses `window.supabase.createClient` to subscribe to broadcast channels.
- It looks for DOM elements with `data-competition-id` and updates seat count elements that match `data-seat-count="<competitionId>"`.
- It also listens for notification broadcasts on a `notifications` topic and displays a Bootstrap toast.
- In this workspace, the realtime publishing service is implemented, but the view-level client configuration is not currently emitted into pages, so the realtime script is present but would need config injection to be fully functional.

## 5. Backend Explanation

### API endpoints

Key controllers and endpoints include:

- `AccountController`: login, logout, registration, and role resolution.
- `EventController`: list events, event details, create events, delete events.
- `CompetitionController`: list competitions, competition details, create competitions.
- `RegistrationController`: registration pages, individual and team registration, user registrations listing.
- `PaymentController`: upload payment proof, list payments, verify payments, reject payments.
- `NotificationController`: list notifications, mark notifications as read.

### Request/response handling

- Each controller action receives request data from URL parameters, query strings, or form POST models.
- Model validation is enforced with `ModelState.IsValid`.
- Controllers use `TempData` to pass success and error messages to views.
- Actions return `View(model)` for rendered pages or `RedirectToAction()` for post/redirect/get patterns.
- Authorization is enforced with `[Authorize]`, role-based policies, and `User.IsInRole(...)`.

### Connection string explanation

- The connection string is loaded from `appsettings.json` or environment variables.
- `Program.cs` uses `builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")`.
- The connection string points to Supabase PostgreSQL with host, port, database, username, password, SSL mode, and pooling properties.
- `AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.CommandTimeout(120)))` configures EF Core and sets a 120-second command timeout.
- The code intentionally avoids automatic retry on failure because retrying after a partial commit can cause duplicate key errors.

### Backend communication with database

- `ApplicationDbContext` maps domain models to PostgreSQL tables using EF Core.
- Controllers use LINQ queries and EF methods such as `.Where()`, `.Include()`, `.FirstOrDefaultAsync()`, `.AnyAsync()`, `.ToListAsync()`, `.Add()`, and `.SaveChangesAsync()`.
- In critical sections, the project uses transactions and explicit row-level locks with `SELECT 1 FROM competition WHERE competitionid = {id} FOR UPDATE`.
- Raw SQL is also used in startup migration code and for database health checks.
- The backend also publishes realtime events through `SupabaseRealtimeService` after key operations.

## 6. Database Design (DETAILED)

### Tables

The database schema includes the following main tables:

- `users`
- `admin`
- `organizer`
- `volunteer`
- `student`
- `event`
- `competition`
- `registration`
- `payment`
- `ticket`
- `team`
- `teammember`
- `eventstaff`
- `competitionstaff`
- `notification`
- `organizerrolerequest`
- `volunteereventrequest`

### Table details

#### `users`
- PK: `userid`
- Columns: `name`, `email`, `phone`, `passwordhash`
- Unique index: `email`
- Relationships: one-to-one with `student`, `organizer`, `volunteer`, and `admin`

#### `admin`
- PK/FK: `userid`
- Relationship: references `users.userid`
- Delete behavior: cascade when user deleted

#### `organizer`
- PK/FK: `userid`
- Relationship: references `users.userid`
- Delete behavior: cascade

#### `volunteer`
- PK/FK: `userid`
- Relationship: references `users.userid`
- Delete behavior: cascade

#### `student`
- PK/FK: `userid`
- Columns: `rollnumber`, `department`
- Unique index: `rollnumber`
- Relationship: references `users.userid`
- Delete behavior: cascade

#### `event`
- PK: `eventid`
- FK: `createdby` references `organizer.userid`
- Delete behavior: restrict on event creator deletion
- Columns: `name`, `department`, `location`, `startdate`, `enddate`, `createdat`, `updatedat`, `updatedby`

#### `competition`
- PK: `competitionid`
- FK: `eventid` references `event.eventid`
- Delete behavior: restrict on parent event deletion
- Columns: `name`, `description`, `location`, `startdate`, `enddate`, `maxteamsize`, `entryfee`, `availableseats`, `createdat`, `updatedat`, `updatedby`

#### `eventstaff`
- Composite PK: `(eventid, userid)`
- FKs: `eventid` references `event.eventid`; `userid` references `volunteer.userid`
- Delete behavior: cascade on event or volunteer removal
- Column: `role`

#### `competitionstaff`
- Composite PK: `(competitionid, userid)`
- FKs: `competitionid` references `competition.competitionid`; `userid` references `volunteer.userid`
- Delete behavior: cascade on competition or volunteer removal
- Column: `role`

#### `registration`
- PK: `registrationid`
- FKs: `competitionid` references `competition.competitionid`; `userid` references `users.userid`; `teamid` references `team.teamid`
- Delete behavior: `teamid` is `SetNull` (team deletion does not delete registration)
- Columns: `type`, `status`, `prioritynumber`, `registeredat`, `updatedat`, `updatedby`
- Indexes: `(userid, registeredat)`, `(competitionid, status, prioritynumber)`, `(competitionid, userid)`

#### `payment`
- PK: `paymentid`
- FK: `registrationid` references `registration.registrationid`
- Unique index: `registrationid` (one payment per registration)
- Columns: `screenshot`, `status`, `verifiedby`, `submittedat`, `verifiedat`, `updatedat`, `updatedby`
- Delete behavior: cascade when registration deleted
- `verifiedby` references `users.userid`, delete behavior: set null

#### `ticket`
- PK: `ticketid`
- FK: `registrationid` references `registration.registrationid`
- Unique index: `registrationid` (one ticket per registration)
- Columns: `qrcode`, `uniquecode`, `generatedat`, `updatedat`, `updatedby`
- Delete behavior: cascade when registration deleted

#### `team`
- PK: `teamid`
- FKs: `leaderuserid` references `users.userid`; `competitionid` references `competition.competitionid`
- Delete behavior: restrict on leader deletion, cascade on competition deletion
- Column: `teamname`

#### `teammember`
- PK: `memberid`
- FK: `teamid` references `team.teamid`
- Delete behavior: cascade when team deleted
- Columns: `name`, `rollnumber`, `department`, `email`

#### `notification`
- PK: `notificationid`
- FK: `userid` references `users.userid`
- Delete behavior: cascade when user deleted
- Columns: `message`, `createdat`, `isread`
- Indexes: `(userid, createdat)`, `(userid, isread, createdat)`

#### `organizerrolerequest`
- PK: `requestid`
- FK: `studentid` references `student.userid`; `approvedby` references `admin.userid`
- Delete behavior: cascade on student deletion, set null on reviewer deletion
- Columns: `status`, `requestedat`, `reviewedat`

#### `volunteereventrequest`
- PK: `requestid`
- FK: `eventid` references `event.eventid`; `studentid` references `student.userid`
- Delete behavior: cascade on event or student deletion
- Columns: `organizerreviewedby`, `adminreviewedby`, `organizerdecision`, `admindecision`, `status`, `requestedat`, `organizerreviewedat`, `adminreviewedat`

### Primary Keys

- Each table has a single primary key except `eventstaff` and `competitionstaff`, which use composite primary keys.
- Primary keys uniquely identify rows and enable relationships.

### Foreign Keys

- Foreign keys enforce referential integrity between tables.
- Examples:
  - `competition.eventid -> event.eventid`
  - `registration.userid -> users.userid`
  - `payment.registrationid -> registration.registrationid`
  - `ticket.registrationid -> registration.registrationid`

### Unique Keys (non-primary)

- `users.email` is unique.
- `student.rollnumber` is unique.
- `payment.registrationid` is unique.
- `ticket.registrationid` is unique.

### Junction Tables

- `eventstaff` is a junction table representing the many-to-many relationship between volunteers and events.
- `competitionstaff` is a junction table representing the many-to-many relationship between volunteers and competitions.
- These junction tables also carry a `role` attribute.

### ON DELETE CASCADE usage

- Cascade delete is used for dependent tables where child rows should be removed when a parent row is removed, such as:
  - `notification` when `users` is deleted
  - `payment` and `ticket` when `registration` is deleted
  - `teammember` when `team` is deleted
  - `competitionstaff` when `competition` or `volunteer` is deleted
- Some relationships use `SetNull`, such as `payment.verifiedby`, to preserve the payment record when the verifier account is removed.

## 7. Relationships

### One-to-One

- `User` to `Student` is one-to-one.
- `User` to `OrganizerProfile` is one-to-one.
- `User` to `Volunteer` is one-to-one.
- `User` to `Admin` is one-to-one.
- `Registration` to `Payment` is one-to-one.
- `Registration` to `Ticket` is one-to-one.

### One-to-Many

- `OrganizerProfile` to `Event` is one-to-many.
- `Event` to `Competition` is one-to-many.
- `Competition` to `Registration` is one-to-many.
- `User` to `Registration` is one-to-many.
- `Team` to `TeamMember` is one-to-many.
- `Competition` to `Team` is one-to-many.
- `User` to `Notification` is one-to-many.

### Many-to-Many

- Volunteers and events are connected through `eventstaff`.
- Volunteers and competitions are connected through `competitionstaff`.
- These are many-to-many relationships implemented as junction tables with composite keys.

## 8. SQL Operations

### SELECT

- Used to read data from tables.
- Examples from project code:
  - `await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == model.Email)`
  - `await _context.Events.AsNoTracking().Include(e => e.Creator).ThenInclude(c => c.User).FirstOrDefaultAsync(e => e.EventID == id)`
  - `await _context.Registrations.AsNoTracking().Where(r => r.UserID == userId.Value).Include(r => r.Competition).ThenInclude(c => c.Event).ToListAsync()`
- Many of these queries include navigation properties with `.Include(...)`.

### INSERT

- New rows are added with `_context.Add(...)` and `_context.SaveChangesAsync()`.
- Examples:
  - `Users.Add(user)` during registration.
  - `Registrations.Add(new Registration { ... })` during competition booking.
  - `Payments.Add(payment)` when payment proof is uploaded.
  - `Tickets.Add(new Ticket { ... })` after payment approval.

### UPDATE

- Existing rows are updated by modifying entity properties and calling `SaveChangesAsync()`.
- Examples:
  - `lockedCompetition.AvailableSeats = decision.UpdatedAvailableSeats`
  - `payment.Status = PaymentStatuses.Approved`
  - `payment.VerifiedBy = verifierId.Value`
  - `item.IsRead = true` for notifications

### DELETE

- Rows are removed using `_context.Remove(...)` or `_context.RemoveRange(...)`.
- Examples:
  - Deleting registrations and related team members when an event is deleted.
  - Removing event staff or competition staff assignments.

### Raw SQL

- The startup migration runner uses raw SQL from `Database/Migrations/*.sql`.
- The app uses `SELECT 1 FROM competition WHERE competitionid = {id} FOR UPDATE` inside registration transactions to lock the row.
- That lock prevents concurrent registration writes from causing oversubscription.

## 9. Joins

### Inner joins

- EF Core `.Include(...)` produces SQL inner joins when loading related entities.
- For example, `Include(r => r.Competition).ThenInclude(c => c.Event)` loads a registration with its competition and event in one query.
- `Include(e => e.Creator).ThenInclude(c => c.User)` loads event creators through the organizer profile.

### LEFT JOIN / RIGHT JOIN

- The codebase does not explicitly use raw left or right joins.
- However, EF Core can produce left joins implicitly for optional navigation properties such as `r.Team` or `registration.Payment` depending on the relationship.
- There is no explicit right join in the current project.

### Why joins are used

- Joins are used to retrieve related data in a single query.
- For example, event details need the creator’s name and related competitions.
- Registration history needs competition and ticket/payment state.

## 10. Subqueries

### Subqueries in the project

- `.AnyAsync(...)` is translated into an SQL `EXISTS` clause.
- `Where(r => competitionIds.Contains(r.CompetitionID))` becomes an `IN` subquery or list membership check.
- The `Verify` action uses `.AnyAsync(...)` to check event and competition staff assignments.

### JOIN vs SUBQUERY

- `JOIN` combines rows from multiple tables into a single result set, which is useful when you need related objects together.
- `SUBQUERY` checks existence or filters by a set and is often more efficient for boolean membership tests.
- In this project, `JOIN` is used for loading related entities, and `SUBQUERY` is used for authorization checks and existence tests.

## 11. Normalization & Anomalies

### 1NF (First Normal Form)

- Each table stores atomic values in each column.
- There are no repeating groups or arrays in a single column.
- Example: `teams` stores one `teamname` per row, not multiple names in a single field.

### 2NF (Second Normal Form)

- Every non-key attribute depends on the whole primary key.
- Composite keys are used only in junction tables like `eventstaff` and `competitionstaff`, and their attributes depend on both keys.
- Example: the `role` on `eventstaff` depends on the combination `(eventid, userid)`.

### 3NF (Third Normal Form)

- There are no transitive dependencies among non-key attributes.
- Contact details are stored once in `users`, not repeated in role-specific tables.
- Role-specific tables like `student`, `organizer`, and `volunteer` store only role-specific fields.

### Why the tables are in that form

- The schema separates entities by their core purpose.
- `users` holds identity and contact data.
- Role tables hold role-specific profile metadata.
- Event and competition data are in separate tables.
- Registration, payment, and ticket data are separated to avoid redundancy and to reflect distinct lifecycle states.

### Anomalies

#### Insertion anomaly

- A user can exist without a role-specific profile, which means incomplete data is possible if role creation fails.
- The registration process mitigates this by creating a `Student` profile during signup.

#### Update anomaly

- The design avoids update anomalies by not duplicating user contact information across tables.
- If email or phone were duplicated in both `users` and a role table, updates could become inconsistent.

#### Deletion anomaly

- Deleting a parent row can remove dependent child rows through cascading deletes.
- The code also uses manual cleanup for event deletions to avoid orphaned registrations, teams, and staff assignments.

## 12. ER Diagram Explanation

### Entities

- `User`: central identity record.
- `Student`: student profile linked to `User`.
- `OrganizerProfile`: organizer profile linked to `User`.
- `Volunteer`: volunteer profile linked to `User`.
- `Admin`: admin profile linked to `User`.
- `Event`: campus event created by an organizer.
- `Competition`: competition inside an event.
- `Registration`: booking for a competition by a user.
- `Payment`: proof upload for a registration.
- `Ticket`: ticket issued for a registration.
- `Team`: team grouping for team competitions.
- `TeamMember`: member detail for a team.
- `EventStaff`: volunteer assignment to an event.
- `CompetitionStaff`: volunteer assignment to a competition.
- `Notification`: user message record.
- `OrganizerRoleRequest`: request from a student to become organizer.
- `VolunteerEventRequest`: request from a student to volunteer for an event.

### Attributes and relationships

- `User` has one-to-one relationships with `Student`, `OrganizerProfile`, `Volunteer`, and `Admin`.
- `OrganizerProfile` is linked to `Event` by `CreatedBy`.
- `Event` has many `Competition` rows.
- `Competition` has many `Registration` rows.
- `Registration` optionally links to `Team`, `Payment`, and `Ticket`.
- `Team` has many `TeamMember` rows and may have many `Registration` rows.
- `EventStaff` and `CompetitionStaff` link `Volunteer` to `Event` and `Competition` respectively.
- `Notification` belongs to one `User`.

## 13. Supabase & RLS

### How Supabase is used in this project

- Supabase hosts the PostgreSQL database and the realtime broadcast endpoint.
- The application uses Npgsql to connect to the Supabase database using the configured connection string.
- The `SupabaseRealtimeService` uses the Supabase realtime broadcast API to publish events.
- Realtime messages are sent for seat updates and notifications.

### How data is stored and retrieved

- Data is stored in PostgreSQL tables via EF Core.
- Realtime notifications are stored in the `notification` table and also published via Supabase.
- Realtime seat updates are published after registration changes to `competition.AvailableSeats`.

### API interaction with Supabase

- `SupabaseRealtimeService.PublishAsync(...)` sends POST requests to `${url}/realtime/v1/api/broadcast`.
- It includes `Authorization: Bearer <ServiceRoleKey>` and `apikey: <ServiceRoleKey>`.
- The payload contains `topic`, `event`, and `payload` fields.

### Row Level Security (RLS)

- There is no RLS policy code inside the ASP.NET application.
- The app does not configure database-side row level security policies.
- Suggested policies if implemented:
  - Allow users to read and update only their own notifications.
  - Allow students to read only their own registrations and payments.
  - Allow organizers to manage only events and competitions they created.
  - Allow volunteers to view payments only for assigned events or competitions.

### Failure case: Supabase/server down

- If the database is unavailable, controller actions that query or save data will fail and may return error pages or validation errors.
- The startup ping `SELECT 1` is swallowed, so the app may still start even if the database is down.
- The realtime publish method catches exceptions and ignores them so broadcast failures do not break the main business flow.
- The current frontend is not resilient to offline data; if Supabase realtime is unavailable, live updates simply do not arrive.

### Improvements

- Add explicit retry and circuit breaker logic for transient database failures.
- Surface a user-friendly error page for service unavailability.
- Emit `window.supabaseRealtimeConfig` and ensure the Supabase JS client is loaded so realtime updates work in the browser.
- Add proper RLS policies if direct Supabase access is exposed.

## 14. API & Integration

### How frontend calls backend APIs

- The frontend uses Razor-generated forms and standard HTTP POST requests.
- Example endpoints:
  - `POST /Registration/Create`
  - `POST /Registration/RegisterTeam`
  - `POST /Payment/Create`
  - `POST /Payment/VerifyPayment`
  - `POST /Notification/MarkRead`
- Data is passed as view models in the request body.

### JSON format usage

- The primary UI flow is not JSON-based.
- JSON is used in the realtime broadcast payloads sent to Supabase.
- Example payloads:
  - Seat update: `{ "competitionId": 5, "availableSeats": 12 }`
  - Notification: `{ "userId": 3, "message": "Your payment was approved.", "createdAt": "..." }`

### Full integration explanation

- The frontend renders server-side HTML and posts form data to controllers.
- The backend validates input, executes business logic, and updates the database.
- The database persists records via EF Core.
- After database changes, the backend may publish realtime events and create notifications.
- The browser receives the next rendered page and also may receive live updates via JavaScript.

### Frontend dynamic updates

- The system is primarily server-rendered.
- Browser pages are initially static HTML from Razor, but they reflect the latest server state.
- The JavaScript realtime engine can update DOM elements in response to broadcast events, making the UI partially dynamic without full reloads.

## 15. Error Handling

- Controllers validate models and add errors to `ModelState`.
- The app returns the same view with validation messages if input is invalid.
- Some operations use `try/catch` around database updates to detect PostgreSQL constraint violations.
- `DbUpdateException` is handled for unique violations and check constraint violations.
- The startup migration runner logs exceptions and continues best-effort.
- Realtime delivery failures are deliberately ignored so the main user flow is not disrupted.

## 16. Conclusion

I designed this system as a server-rendered ASP.NET Core MVC application with a PostgreSQL backend and a realtime overlay using Supabase. The project balances relational data integrity, role-based access, and event-driven updates. The database schema models users, events, competitions, registrations, payments, tickets, and staff assignments in a normalized, normalized structure.

Even though the UI appears static at a glance, the backend produces fresh pages on every request, and the realtime layer is intended to update specific page elements without a full reload.

## 17. Viva Questions & Answers

### Q: What is a Primary Key?
A: A primary key uniquely identifies each row in a table. In this project, `users.userid`, `event.eventid`, and `competition.competitionid` are examples of primary keys.

### Q: What is a Foreign Key?
A: A foreign key references a primary key in another table and enforces referential integrity. For example, `competition.eventid` references `event.eventid`.

### Q: What is a Unique Key (non-primary)?
A: A unique key enforces uniqueness without being the primary key. In this project, `users.email` and `student.rollnumber` are unique keys.

### Q: What is a Junction Table?
A: A junction table implements a many-to-many relationship. In this project, `eventstaff` and `competitionstaff` are junction tables linking volunteers to events and competitions.

### Q: What are Joins and their types?
A: Joins combine rows from multiple tables. This project uses inner joins via EF Core `.Include(...)`. A left join would be used for optional related data, but the code does not explicitly use raw left joins.

### Q: What is the difference between JOIN and SUBQUERY?
A: JOIN combines tables into a single result set, while SUBQUERY runs a nested query. In this project, `.Include(...)` uses joins for related entity loading, and `.AnyAsync(...)` uses subqueries to test existence.

### Q: What are anomalies?
A: Anomalies are problems caused by poor schema design. Insertion anomalies happen when valid data cannot be added without unrelated fields. Update anomalies occur when duplicate data must be updated in multiple places. Deletion anomalies happen when deleting one row removes needed data.

### Q: What is normalization and why is it important?
A: Normalization organizes data to reduce redundancy and dependency. It improves data integrity and avoids anomalies. This project uses normalized tables to separate users, profiles, events, competitions, registrations, payments, and tickets.

### Q: Explain 1NF, 2NF, 3NF using this project.
A: 1NF means each column is atomic, e.g. `users.email` stores a single value. 2NF means non-key attributes depend on the whole primary key; composite keys appear only in junction tables like `eventstaff`. 3NF means no non-key attribute depends on another non-key attribute; contact data lives in `users` only, not repeated in `student` or `organizer`.

### Q: How do you retrieve data from tables?
A: The backend uses EF Core queries such as `.Where()`, `.Include()`, `.FirstOrDefaultAsync()`, `.ToListAsync()`, and `.AnyAsync()`. These translate into SQL `SELECT` statements.

### Q: How are SQL queries executed in the project?
A: EF Core generates SQL for CRUD operations. Controllers call `_context.SaveChangesAsync()` for inserts and updates, and use query methods for selects. Raw SQL is used for row locking and migrations.

### Q: What is RLS?
A: Row Level Security restricts row access at the database layer. This project does not implement RLS policies in the application code.

### Q: How does Supabase work here?
A: Supabase hosts PostgreSQL and provides a realtime broadcast API. The backend publishes seat updates and notifications using `SupabaseRealtimeService`.

### Q: What happens if database/server fails?
A: If PostgreSQL is unavailable, requests fail and the user may see errors. Realtime publishes are caught and ignored, so broadcast failures do not block the main workflow.

### Q: How does the frontend update dynamically?
A: The page is server-rendered on each request, but JavaScript can subscribe to Supabase broadcast channels and update page elements without a full reload.

### Q: What is the difference between SQL and NoSQL?
A: SQL uses structured tables, relationships, and schema, while NoSQL uses flexible document or key-value stores. This project is SQL-based and relies on relational constraints and normalization.

### Q: What is JSON format?
A: JSON is a text format for structured data. In this project, JSON is used in realtime broadcast payloads sent to Supabase, not as the primary form submission format.

---

*End of report.*
