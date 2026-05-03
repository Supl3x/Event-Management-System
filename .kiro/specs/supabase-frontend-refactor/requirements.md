# Requirements Document

## Introduction

This document specifies the requirements for refactoring an existing ASP.NET Core MVC Event Management System into a frontend-only application with direct Supabase integration. The refactored system will eliminate all backend C# code (Controllers, Services, Program.cs) and replace it with client-side JavaScript/TypeScript that communicates directly with Supabase using the public anonymous key. The system will rely on Supabase Row Level Security (RLS) for authorization and will be deployable on Vercel as a static or frontend-only application.

The refactored system must preserve all existing functionality including authentication, CRUD operations for events/competitions/registrations/payments/tickets, role management, real-time notifications, and staff assignments while maintaining security through RLS policies.

## Glossary

- **Frontend_Application**: The client-side web application that runs in the user's browser and communicates directly with Supabase
- **Supabase_Client**: The JavaScript/TypeScript library (@supabase/supabase-js) that provides methods to interact with Supabase services
- **RLS_Policy**: Row Level Security policy defined in Supabase PostgreSQL that controls data access based on authenticated user context
- **SUPABASE_URL**: The public URL endpoint for the Supabase project
- **SUPABASE_ANON_KEY**: The public anonymous key used for client-side authentication and API calls
- **Auth_Session**: The authenticated user session managed by Supabase Auth
- **Realtime_Channel**: A Supabase Realtime subscription that pushes database changes to connected clients
- **Service_Layer**: Client-side JavaScript/TypeScript modules that encapsulate Supabase operations
- **Configuration_Module**: A centralized module that initializes and exports the Supabase client instance
- **Vercel_Deployment**: The process of deploying the frontend application to Vercel's hosting platform
- **Static_Asset**: HTML, CSS, JavaScript, and image files served directly without server-side processing
- **User_Role**: One of Admin, Student, Organizer, or Volunteer as defined in the database schema
- **Registration_Status**: One of Pending, Approved, or Rejected for competition registrations
- **Payment_Status**: One of Pending, Approved, or Rejected for payment verification
- **Competition_Type**: Either Individual or Team based on MaxTeamSize
- **Booking_Engine**: Logic that manages competition seat availability and waitlist processing
- **Notification_System**: Real-time notification delivery using Supabase Realtime
- **QR_Code**: Unique ticket identifier generated after payment approval

## Requirements

### Requirement 1: Remove Backend Infrastructure

**User Story:** As a developer, I want to remove all ASP.NET backend code, so that the application can be deployed as a frontend-only solution on Vercel.

#### Acceptance Criteria

1. THE Frontend_Application SHALL NOT include any C# Controllers (AccountController, CompetitionController, DashboardController, EventController, HomeController, NotificationController, PaymentController, RegistrationController, RoleManagementController, RoleRequestController, StaffAssignmentController, TicketController)
2. THE Frontend_Application SHALL NOT include any C# Services (BookingEngine, DashboardService, NotificationService, SupabaseRealtimeService, TicketService)
3. THE Frontend_Application SHALL NOT include Program.cs, ApplicationDbContext.cs, or any Entity Framework dependencies
4. THE Frontend_Application SHALL NOT include the EventManagementPortal.csproj file or any .NET SDK dependencies
5. THE Frontend_Application SHALL consist only of HTML, CSS, JavaScript/TypeScript, and static assets

### Requirement 2: Initialize Supabase Client

**User Story:** As a developer, I want a centralized Supabase client configuration, so that all parts of the application use the same authenticated connection.

#### Acceptance Criteria

1. THE Configuration_Module SHALL initialize the Supabase_Client using SUPABASE_URL and SUPABASE_ANON_KEY
2. THE Configuration_Module SHALL export a single Supabase_Client instance for use across the application
3. THE Configuration_Module SHALL NOT expose or use any service_role key
4. THE Configuration_Module SHALL configure the Supabase_Client with appropriate session persistence options
5. WHEN the application loads, THE Configuration_Module SHALL attempt to restore any existing Auth_Session

### Requirement 3: Implement Authentication

**User Story:** As a user, I want to sign up, log in, and log out using my email and password, so that I can access the system securely.

#### Acceptance Criteria

1. WHEN a user submits valid registration credentials (name, email, phone, password, role), THE Frontend_Application SHALL call Supabase Auth signUp and insert user profile data into the Users table
2. WHEN a user submits valid login credentials (email, password), THE Frontend_Application SHALL call Supabase Auth signInWithPassword and establish an Auth_Session
3. WHEN a user clicks logout, THE Frontend_Application SHALL call Supabase Auth signOut and clear the Auth_Session
4. WHEN an Auth_Session is established, THE Frontend_Application SHALL retrieve the authenticated user's profile and role information from the database
5. IF authentication fails, THEN THE Frontend_Application SHALL display a descriptive error message to the user
6. THE Frontend_Application SHALL store the Auth_Session in browser storage for persistence across page reloads

### Requirement 4: Implement Role-Based Access Control

**User Story:** As a user, I want the application to show me only the features and data appropriate for my role, so that I can perform my tasks without confusion.

#### Acceptance Criteria

1. WHEN a user is authenticated, THE Frontend_Application SHALL determine the user's User_Role by querying the Admin, Student, Organizer, and Volunteer tables
2. THE Frontend_Application SHALL display navigation and UI elements appropriate for the authenticated user's User_Role
3. THE Frontend_Application SHALL enforce client-side route protection based on User_Role
4. THE RLS_Policy on each database table SHALL enforce server-side authorization based on the authenticated user's User_Role
5. IF a user attempts to access a resource without proper authorization, THEN THE Frontend_Application SHALL display an access denied message

### Requirement 5: Implement Event Management

**User Story:** As an Organizer, I want to create, update, and delete events, so that I can manage campus events effectively.

#### Acceptance Criteria

1. WHEN an Organizer submits a new event form, THE Service_Layer SHALL insert a row into the Event table with the authenticated user as CreatedBy
2. WHEN an Organizer updates an event, THE Service_Layer SHALL update the corresponding Event row and set UpdatedBy and UpdatedAt fields
3. WHEN an Organizer deletes an event, THE Service_Layer SHALL delete the Event row (cascading to related competitions)
4. THE Service_Layer SHALL retrieve events using Supabase_Client select queries with appropriate filters
5. THE RLS_Policy on the Event table SHALL allow Organizers to modify only events they created (CreatedBy = auth.uid())

### Requirement 6: Implement Competition Management

**User Story:** As an Organizer, I want to create and manage competitions within events, so that students can register for specific activities.

#### Acceptance Criteria

1. WHEN an Organizer submits a new competition form, THE Service_Layer SHALL insert a row into the Competition table linked to the parent Event
2. WHEN an Organizer updates a competition, THE Service_Layer SHALL update the Competition row and set UpdatedBy and UpdatedAt fields
3. WHEN an Organizer deletes a competition, THE Service_Layer SHALL delete the Competition row (cascading to registrations)
4. THE Service_Layer SHALL retrieve competitions using Supabase_Client select queries with joins to Event data
5. THE RLS_Policy on the Competition table SHALL allow Organizers to modify only competitions belonging to events they created

### Requirement 7: Implement Registration Management

**User Story:** As a Student, I want to register for competitions individually or as part of a team, so that I can participate in campus events.

#### Acceptance Criteria

1. WHEN a Student submits an individual registration, THE Service_Layer SHALL insert a Registration row with Type='Individual' and TeamID=NULL
2. WHEN a Student submits a team registration, THE Service_Layer SHALL first insert a Team row, then insert TeamMember rows, then insert a Registration row with Type='Team'
3. WHEN a registration is submitted, THE Service_Layer SHALL check AvailableSeats and set Registration_Status to 'Pending' or 'Waitlisted' accordingly
4. THE Service_Layer SHALL enforce the constraint that one user can register only once per competition (uq_user_competition)
5. THE RLS_Policy on the Registration table SHALL allow Students to view their own registrations and Organizers to view registrations for their events

### Requirement 8: Implement Payment Processing

**User Story:** As a Student, I want to upload payment proof for my registration, so that my registration can be verified and approved.

#### Acceptance Criteria

1. WHEN a Student uploads a payment screenshot, THE Service_Layer SHALL insert a Payment row linked to the Registration with Status='Pending'
2. WHEN an Organizer or Volunteer approves a payment, THE Service_Layer SHALL update the Payment row with Status='Approved', VerifiedBy, and VerifiedAt
3. WHEN a payment is approved, THE database trigger SHALL automatically update the Registration_Status to 'Approved' and generate a Ticket
4. THE Service_Layer SHALL retrieve pending payments for Organizers and Volunteers to review
5. THE RLS_Policy on the Payment table SHALL allow Students to view their own payments and Organizers/Volunteers to view payments for their events

### Requirement 9: Implement Ticket Generation

**User Story:** As a Student, I want to receive a ticket with a QR code after my payment is approved, so that I can gain entry to the competition.

#### Acceptance Criteria

1. WHEN a Payment is approved, THE database trigger SHALL automatically insert a Ticket row with a unique QRCode and UniqueCode
2. THE Service_Layer SHALL retrieve ticket information for display to the user
3. THE Frontend_Application SHALL display the QR code and unique code to the user
4. THE RLS_Policy on the Ticket table SHALL allow Students to view their own tickets and Organizers/Volunteers to view tickets for their events
5. THE Ticket SHALL be generated exactly once per approved registration (1:1 relationship enforced by unique constraint)

### Requirement 10: Implement Dashboard Views

**User Story:** As a user, I want to see a dashboard with relevant statistics and information for my role, so that I can quickly understand the current state of events and registrations.

#### Acceptance Criteria

1. WHEN an Admin views the dashboard, THE Service_Layer SHALL retrieve counts of total users, events, competitions, and pending role requests
2. WHEN an Organizer views the dashboard, THE Service_Layer SHALL retrieve counts of events created, competitions managed, and pending payments for their events
3. WHEN a Student views the dashboard, THE Service_Layer SHALL retrieve counts of registrations, approved registrations, and pending payments
4. WHEN a Volunteer views the dashboard, THE Service_Layer SHALL retrieve counts of events and competitions they are assigned to
5. THE Service_Layer SHALL use Supabase_Client select queries with count aggregations and filters based on User_Role

### Requirement 11: Implement Real-Time Notifications

**User Story:** As a user, I want to receive real-time notifications when important events occur, so that I can stay informed without refreshing the page.

#### Acceptance Criteria

1. WHEN the Frontend_Application loads, THE Service_Layer SHALL subscribe to a Realtime_Channel for the Notification table filtered by the authenticated user's UserID
2. WHEN a new notification is inserted into the database, THE Realtime_Channel SHALL push the notification to the connected client
3. THE Frontend_Application SHALL display new notifications in the UI without requiring a page refresh
4. WHEN a user marks a notification as read, THE Service_Layer SHALL update the Notification row with IsRead=true
5. THE RLS_Policy on the Notification table SHALL allow users to view and update only their own notifications

### Requirement 12: Implement Staff Assignment

**User Story:** As an Organizer, I want to assign Volunteers to events and competitions, so that I can manage event staffing effectively.

#### Acceptance Criteria

1. WHEN an Organizer assigns a Volunteer to an event, THE Service_Layer SHALL insert a row into the EventStaff table
2. WHEN an Organizer assigns a Volunteer to a competition, THE Service_Layer SHALL insert a row into the CompetitionStaff table
3. WHEN an Organizer removes a staff assignment, THE Service_Layer SHALL delete the corresponding EventStaff or CompetitionStaff row
4. THE Service_Layer SHALL retrieve staff assignments using Supabase_Client select queries with joins to User data
5. THE RLS_Policy on EventStaff and CompetitionStaff tables SHALL allow Organizers to manage assignments for their events and competitions

### Requirement 13: Implement Role Request Management

**User Story:** As a Student, I want to request the Organizer role, so that I can create and manage events after approval.

#### Acceptance Criteria

1. WHEN a Student submits a role request, THE Service_Layer SHALL insert an OrganizerRoleRequest row with Status='Pending'
2. WHEN an Admin approves a role request, THE Service_Layer SHALL update the OrganizerRoleRequest with Status='Approved', ApprovedBy, and ReviewedAt, and insert a row into the Organizer table
3. WHEN an Admin rejects a role request, THE Service_Layer SHALL update the OrganizerRoleRequest with Status='Rejected', ApprovedBy, and ReviewedAt
4. THE Service_Layer SHALL retrieve pending role requests for Admin review
5. THE RLS_Policy on OrganizerRoleRequest SHALL allow Students to view their own requests and Admins to view all requests

### Requirement 14: Implement Booking Engine Logic

**User Story:** As a Student, I want the system to automatically manage seat availability and waitlists, so that registrations are processed fairly and efficiently.

#### Acceptance Criteria

1. WHEN a registration is submitted, THE Service_Layer SHALL check the Competition AvailableSeats value
2. IF AvailableSeats > 0, THEN THE Service_Layer SHALL decrement AvailableSeats and set Registration_Status='Pending'
3. IF AvailableSeats = 0, THEN THE Service_Layer SHALL set Registration_Status='Waitlisted' and assign a PriorityNumber
4. WHEN a registration is cancelled or rejected, THE Service_Layer SHALL increment AvailableSeats and promote the next waitlisted registration
5. THE Service_Layer SHALL use Supabase transactions to ensure atomic seat allocation and prevent race conditions

### Requirement 15: Implement Error Handling

**User Story:** As a user, I want to see clear error messages when something goes wrong, so that I can understand and resolve issues.

#### Acceptance Criteria

1. WHEN a Supabase operation fails, THE Service_Layer SHALL catch the error and return a descriptive error message
2. THE Frontend_Application SHALL display error messages to the user in a user-friendly format
3. IF a network error occurs, THEN THE Frontend_Application SHALL display a message indicating connectivity issues
4. IF an RLS_Policy denies access, THEN THE Frontend_Application SHALL display an authorization error message
5. THE Service_Layer SHALL log errors to the browser console for debugging purposes

### Requirement 16: Create Deployment Documentation

**User Story:** As a developer, I want clear deployment instructions, so that I can deploy the application to Vercel successfully.

#### Acceptance Criteria

1. THE VERCEL_DEPLOYMENT.md document SHALL provide step-by-step instructions for deploying the Frontend_Application to Vercel
2. THE VERCEL_DEPLOYMENT.md document SHALL specify required environment variables (SUPABASE_URL, SUPABASE_ANON_KEY)
3. THE VERCEL_DEPLOYMENT.md document SHALL explain how to configure Vercel build settings for the project
4. THE VERCEL_DEPLOYMENT.md document SHALL include instructions for setting up custom domains if needed
5. THE VERCEL_DEPLOYMENT.md document SHALL include troubleshooting tips for common deployment issues

### Requirement 17: Create Implementation Documentation

**User Story:** As a developer, I want detailed implementation guidance, so that I can refactor the codebase correctly and completely.

#### Acceptance Criteria

1. THE REFACTOR_IMPLEMENTATION.md document SHALL provide a complete file structure for the refactored Frontend_Application
2. THE REFACTOR_IMPLEMENTATION.md document SHALL specify the Configuration_Module structure with Supabase_Client initialization
3. THE REFACTOR_IMPLEMENTATION.md document SHALL specify the Service_Layer structure with modules for each domain (auth, events, competitions, registrations, payments, tickets, notifications, staff)
4. THE REFACTOR_IMPLEMENTATION.md document SHALL provide code examples for common operations (authentication, CRUD, real-time subscriptions)
5. THE REFACTOR_IMPLEMENTATION.md document SHALL explain the separation of concerns between configuration, services, and UI layers

### Requirement 18: Preserve Database Schema

**User Story:** As a developer, I want to keep the existing Supabase database schema unchanged, so that I can focus on frontend refactoring without database migrations.

#### Acceptance Criteria

1. THE Frontend_Application SHALL use the existing database schema defined in Database/Schema.txt
2. THE Frontend_Application SHALL NOT require any changes to table structures, columns, or constraints
3. THE Frontend_Application SHALL rely on existing database triggers (auto_generate_ticket, sync_registration_on_payment_approval, check_team_size, check_registration_type)
4. THE Frontend_Application SHALL assume RLS policies are enabled on all tables
5. THE implementation documentation SHALL reference the existing schema and explain which tables are used by each Service_Layer module

### Requirement 19: Implement Security Best Practices

**User Story:** As a developer, I want the application to follow security best practices, so that user data is protected and the system is not vulnerable to common attacks.

#### Acceptance Criteria

1. THE Frontend_Application SHALL use only SUPABASE_ANON_KEY and SHALL NOT expose any service_role key
2. THE Frontend_Application SHALL rely on RLS_Policy for all authorization decisions
3. THE Frontend_Application SHALL validate user input on the client side before sending to Supabase
4. THE Frontend_Application SHALL sanitize user-generated content before displaying in the UI to prevent XSS attacks
5. THE Frontend_Application SHALL use HTTPS for all communication with Supabase (enforced by Supabase and Vercel)

### Requirement 20: Maintain Clean Code Structure

**User Story:** As a developer, I want the codebase to be well-organized and maintainable, so that future changes are easy to implement.

#### Acceptance Criteria

1. THE Frontend_Application SHALL separate concerns into Configuration_Module, Service_Layer, and UI layers
2. THE Service_Layer SHALL encapsulate all Supabase operations and expose clean interfaces to the UI layer
3. THE Frontend_Application SHALL use consistent naming conventions across all modules
4. THE Frontend_Application SHALL include comments explaining complex logic and business rules
5. THE Frontend_Application SHALL avoid code duplication by extracting common patterns into reusable functions
