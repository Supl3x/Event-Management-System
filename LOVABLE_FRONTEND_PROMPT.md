# Lovable prompt (frontend-only Supabase; no repo access)

You do NOT have access to my repository while generating code. Build the frontend using this spec only.

## Goal
Refactor the existing “EventHub — NED” system into a **frontend-only** React app that connects **directly to Supabase** from the browser.

- **No backend deployment** (Railway) is required after this.
- The frontend must be deployable to **Vercel** as a static/frontend application.
- Use **Supabase Auth** (email/password) for login/signup/logout.
- Use **Supabase RLS** for authorization (client-side guards only improve UX; real security must be via RLS).
- Preserve the existing **neon theme** and UI behavior.

## Tech stack
- React + TypeScript
- Prefer **Next.js (App Router)** because it works well on Vercel
- Supabase client: `@supabase/supabase-js`

## Environment variables (Vercel)
The frontend must read:
- `NEXT_PUBLIC_SUPABASE_URL`
- `NEXT_PUBLIC_SUPABASE_ANON_KEY`

## Theme requirements (must match current UI)
Recreate the neon look using the following:

### Fonts + icons
- Load Google Fonts:
  - `Orbitron` (wght 500, 700)
  - `Space Grotesk` (wght 400, 500, 600)
- Use Font Awesome (for icons).

### Base CSS (include in your global stylesheet)
Implement (at minimum) the CSS below so the theme intensity selector works:

```css
@import url("https://fonts.googleapis.com/css2?family=Orbitron:wght@500;700&family=Space+Grotesk:wght@400;500;600&display=swap");

:root{
  --bg-900:#060814;
  --bg-850:#0b1020;
  --bg-800:#10182a;
  --surface: rgba(18, 27, 46, 0.76);
  --surface-strong: rgba(17, 25, 44, 0.95);
  --border: rgba(120, 160, 255, 0.24);
  --text:#d7e4ff;
  --muted:#8fa2c9;
  --primary:#00d9ff;
  --secondary:#9b5cff;
  --success:#00ff9f;
  --danger:#ff477e;
  --warning:#ffbe0b;
  --glow-primary: 0 0 16px rgba(0, 217, 255, 0.45);
  --glow-secondary: 0 0 18px rgba(155, 92, 255, 0.45);
  --shadow-soft: 0 18px 40px rgba(0, 0, 0, 0.45);
}

body.neon-app{
  font-family:"Space Grotesk",sans-serif;
  color:var(--text);
  background: radial-gradient(circle at 15% 18%, rgba(0, 217, 255, 0.12), transparent 40%),
              radial-gradient(circle at 80% 10%, rgba(155, 92, 255, 0.14), transparent 42%),
              linear-gradient(180deg, var(--bg-850), var(--bg-900));
  overflow-x:hidden;
}

h1,h2,h3,h4,h5,h6,.navbar-brand{
  font-family:"Orbitron",sans-serif;
  letter-spacing:0.03em;
  color:#eef4ff;
}

a{ color: var(--primary); }
a:hover{ color:#7eeeff; }

.neon-bg-orb{
  position:fixed;
  z-index:0;
  width:28rem;
  height:28rem;
  border-radius:50%;
  filter:blur(70px);
  opacity:0.4;
  pointer-events:none;
}
.neon-bg-orb--left{
  left:-10rem; top:20%;
  background: rgba(0,217,255,0.55);
  animation: orb-float-left 16s ease-in-out infinite alternate;
}
.neon-bg-orb--right{
  right:-8rem; top:52%;
  background: rgba(155,92,255,0.55);
  animation: orb-float-right 20s ease-in-out infinite alternate;
}
.neon-grid-overlay{
  position:fixed; inset:0; z-index:0; pointer-events:none; opacity:0.18;
  background-image: linear-gradient(rgba(90,110,180,0.18) 1px, transparent 1px),
                    linear-gradient(90deg, rgba(90,110,180,0.18) 1px, transparent 1px);
  background-size:34px 34px;
  mask-image: radial-gradient(circle at center, black 42%, transparent 96%);
  animation: grid-breathe 10s ease-in-out infinite;
}

.navbar-emp{
  background: linear-gradient(180deg, rgba(10, 16, 34, 0.97), rgba(10, 16, 34, 0.78)) !important;
  border-bottom: 1px solid rgba(0,217,255,0.2);
  backdrop-filter: blur(12px);
  box-shadow: 0 6px 22px rgba(0,0,0,0.35);
}
.navbar-emp .brand-mark{
  border:1px solid rgba(0,217,255,0.55);
  background: linear-gradient(135deg, rgba(0,217,255,0.9), rgba(155,92,255,0.75));
  box-shadow: var(--glow-primary);
  animation: brand-glow 3.2s ease-in-out infinite;
}
.navbar-emp .nav-link{
  color:#c6d2ef !important;
  border:1px solid transparent;
  border-radius:10px;
  padding:0.42rem 0.85rem !important;
  transition: all 0.2s ease;
}
.navbar-emp .nav-link:hover,
.navbar-emp .nav-link.active{
  color:#fff !important;
  background: rgba(0,217,255,0.1);
  border-color: rgba(0,217,255,0.28);
  box-shadow: inset 0 0 0 1px rgba(0,217,255,0.16);
}

.card,.saas-card,.table,.list-group-item,.modal-content{
  background: var(--surface) !important;
  border: 1px solid var(--border) !important;
  border-radius:14px !important;
  box-shadow: var(--shadow-soft);
  backdrop-filter: blur(8px);
}
.card:hover,.saas-card:hover{ transform: translateY(-3px); border-color: rgba(0,217,255,0.42) !important; }

.form-control,.form-select,.input-group-text,textarea{
  background: var(--surface-strong) !important;
  border: 1px solid rgba(124, 151, 224, 0.36) !important;
  color:#e8f0ff !important;
  border-radius:10px !important;
}
.form-control:focus,.form-select:focus,textarea:focus{
  border-color: rgba(0,217,255,0.76) !important;
  box-shadow: 0 0 0 0.2rem rgba(0,217,255,0.18), var(--glow-primary) !important;
}

.btn{
  border-radius:10px;
  font-weight:600;
  letter-spacing:0.02em;
  transition:0.2s ease;
}
.btn-primary,.btn-cyan{
  border:1px solid rgba(0,217,255,0.55);
  background: linear-gradient(120deg, #00b7ff, #00d9ff 50%, #9b5cff);
  color:#040714;
  box-shadow: var(--glow-primary);
}
.btn-primary:hover,.btn-cyan:hover{ color:#03050d; transform: translateY(-1px); filter: brightness(1.08); }

/* Theme intensity modes */
body.neon-app.intensity-high{ --shadow-soft:0 22px 44px rgba(0,0,0,0.52); --glow-primary:0 0 20px rgba(0,217,255,0.58); }
body.neon-app.intensity-cinematic{ --shadow-soft:0 20px 42px rgba(0,0,0,0.5); --glow-primary:0 0 14px rgba(0,217,255,0.4); --glow-secondary:0 0 14px rgba(155,92,255,0.36); }
body.neon-app.intensity-balanced{ --shadow-soft:0 18px 40px rgba(0,0,0,0.45); }
body.neon-app.intensity-light{ --shadow-soft:0 10px 20px rgba(0,0,0,0.26); --glow-primary:0 0 8px rgba(0,217,255,0.2); --glow-secondary:0 0 8px rgba(155,92,255,0.2); }
body.neon-app.intensity-light .neon-bg-orb{ opacity:0.15; filter:blur(32px); }
body.neon-app.intensity-light .neon-grid-overlay{ opacity:0.06; }

@keyframes orb-float-left { 0%{transform:translate3d(0,0,0) scale(1);} 100%{transform:translate3d(30px,-25px,0) scale(1.06);} }
@keyframes orb-float-right { 0%{transform:translate3d(0,0,0) scale(1);} 100%{transform:translate3d(-34px,24px,0) scale(1.08);} }
@keyframes grid-breathe { 0%,100%{opacity:0.14;} 50%{opacity:0.23;} }
@keyframes brand-glow { 0%,100%{ box-shadow:0 0 10px rgba(0,217,255,0.35);} 50%{ box-shadow:0 0 24px rgba(0,217,255,0.65);} }
```

### Theme intensity selector behavior (must match)
Implement the same logic as the existing theme script:
```js
(function () {
  const THEME_KEY = "eventhub_theme_intensity";
  const allowedIntensities = ["cinematic", "high", "balanced", "light"];
  const getSavedIntensity = () => {
    const raw = localStorage.getItem(THEME_KEY);
    return allowedIntensities.includes(raw) ? raw : "balanced";
  };
  const applyIntensity = (intensity) => {
    const body = document.body;
    if (!body) return;
    body.classList.remove("intensity-cinematic","intensity-high","intensity-balanced","intensity-light");
    body.classList.add(`intensity-${intensity}`);
  };
  const initIntensitySelector = () => {
    const selector = document.getElementById("themeIntensitySelect");
    if (!selector) return;
    const initial = getSavedIntensity();
    selector.value = initial;
    applyIntensity(initial);
    selector.addEventListener("change",(event)=>{
      const value = event.target.value;
      if (!allowedIntensities.includes(value)) return;
      localStorage.setItem(THEME_KEY,value);
      applyIntensity(value);
    });
  };
  applyIntensity(getSavedIntensity());
  initIntensitySelector();
})();
```

Also implement the “tilt on hover/mouse move” behavior for cards (optional, but recommended for visual parity).

## App layout / navbar
Recreate the navbar conceptually:
- Always show:
  - `Home` (route `/`)
  - `Events` (route `/events`)
- When authenticated show:
  - `Dashboard` (route `/dashboard`)
  - `Notifications` (route `/notifications`) + unread indicator dot if you have unread notifications
- Role-based extra items:
  - Student: `My Tickets` (route `/my/tickets`)
  - Organizer: `Create Event`, `Create Competition`
  - Admin: `Role Management`
  - Volunteer: `Verify Payments`

Navbar also includes the theme intensity `<select id="themeIntensitySelect">` with options:
`cinematic`, `high`, `balanced`, `light`.

## Supabase data access contract
The DB schema includes these tables:
`users, student, admin, organizer, volunteer, organizerrolerequest, event, competition, eventstaff, competitionstaff, team, teammember, registration, payment, ticket, notification`.

### IMPORTANT: mapping Supabase Auth user -> `users.UserID`
Because the schema uses integer `users.UserID`, the frontend must determine the current app user row.

Implement a function:
`getAppUser()` that returns `{ userId, email }`.

Default strategy (try this first):
1) Read `session.user.email` from Supabase Auth
2) Query `public.users` by email: `select userid from users where email = ...`
3) Use that `userid` for:
   - role checks (admin/student/organizer/volunteer tables)
   - all RLS-sensitive queries/updates/reads.

If this fails due to your RLS policy design, adjust this mapping strategy to match how your policies identify the user.

## Auth flows
### Signup
1) Supabase Auth signUp with email/password
2) After signup, insert into:
   - `users` row (Name, Email, Phone, PasswordHash optional/nullable)
   - `student` row if role is Student (RollNumber, Department)
   - if role selection exists (or default), create role rows accordingly.

Validation to match existing behavior:
- Students must use NED cloud email: `@cloud.neduet.edu.pk`
- Students require `RollNumber` and `Department`.
- Default role should be Student unless UI asks for something else.

### Login
- supabase Auth signInWithPassword
- then call `getAppUser()` and fetch roles.

### Logout
- supabase Auth signOut
- clear local session state.

## Role-based behavior
After login, fetch:
- isAdmin: exists in `public.admin` for this `UserID`
- isOrganizer: exists in `public.organizer`
- isVolunteer: exists in `public.volunteer`
- isStudent: exists in `public.student`

Store roles in React state.

## Pages / routes to implement (functional parity)
Implement routes equivalent to the existing MVC views and controllers:

Public
- `/` : Home page (list events)
- `/events` : list events + summaries
- `/events/[id]` : event details + competitions + status indicators
- `/account/login`
- `/account/register`
- `/account/access-denied`

Organizer / Admin
- `/events/create`
- `/events/[id]/delete`
- `/competitions/create`
- `/competitions/[id]` (details)

Student
- `/registrations/competition/[id]/create` (individual registration)
- `/registrations/competition/[id]/create-team` (team registration)
- `/my/registrations`
- `/my/tickets`

Volunteer / Organizer (verification)
- `/payments/verify`
- `/payments/my`
- `/payments/competition/[competitionId]/register` (optional, depends on how you model existing flows)

All authenticated
- `/notifications` list + mark read
- `/dashboard` + role-specific dashboard subpages
- `/role-management` (admin only)
- `/staff-assignment/manage` (organizer/admin)
- `/role-request` (student -> organizer role request)

## Booking engine logic (must be race-safe)
The backend system does seat allocation atomically:
- If `competition.AvailableSeats > 0`:
  - decrement AvailableSeats
  - Registration.Status becomes Confirmed
- Else:
  - Registration.Status becomes Waitlisted
  - PriorityNumber becomes maxPriority+1

Frontend must ensure race safety.

RECOMMENDED approach:
- Create a **Supabase SQL function (RPC)** that performs the seat allocation and inserts registration in one transaction.
- Then the frontend calls that RPC with:
  - competitionId
  - userId (or uses session user mapping inside RPC)
  - type: Individual/Team
  - team details (insert team + teammember rows)

## Registration cancellation / waitlist promotion
Replicate backend “ticket cancel” behavior:
- Cancel removes ticket and marks registration Cancelled
- Increases AvailableSeats
- Promotes earliest waitlisted registration (by PriorityNumber then RegisteredAt)

Again, implement via an RPC to guarantee atomicity.

## Payments & tickets
### Upload payment proof
Use Supabase Storage:
- Create/select a storage bucket named `payments`
- Implement client upload of allowed file types: jpg/jpeg/png/webp
- Save uploaded object path into `payment.Screenshot` as a URL or path that your UI can display.

### Verification
When verifying:
- Update `payment.Status` to `Approved` (or `Rejected`)
- Set `VerifiedBy`, `VerifiedAt`

Your existing DB triggers/functions should update:
- `registration.Status`
- insert into `ticket` (UniqueCode/QRCode generation)

The frontend should then show ticket info by selecting from `ticket` joined to registration/competition/event.

## Real-time notifications + seat updates
Implement Supabase Realtime subscriptions:
1) Notifications:
   - subscribe to `notification` changes filtered by `userid` (use `getAppUser().userId`)
   - when new notification arrives:
     - update local notification list
     - show toast
2) Seat counts:
   - subscribe to `competition` UPDATE events for competitions currently displayed
   - update displayed `AvailableSeats`

Do NOT use service_role broadcasts from frontend.

## RLS policy assumptions
Assume RLS is enabled on all tables.
Frontend must only use anon key + authenticated user context.

If RLS blocks any required write operations, you must adjust Supabase policies or implement RPC functions that can bypass strict client write restrictions (but still enforce ownership/role checks inside SQL).

## Error handling
All Supabase operations must:
- catch errors
- show user-friendly messages
- log the full error to console for debugging

## Deliverables
1) Produce the full Next.js React app code.
2) Include:
   - supabase client initialization
   - auth/session management
   - role resolution
   - service modules for each domain
   - pages matching the routes above
3) Include `VERCEL_DEPLOYMENT.md` instructions with env vars and Supabase setup checklist.

## What to ask if something is ambiguous
If the mapping from Supabase Auth user to `users.UserID` is unclear, stop and ask me what RLS policy uses (email-based? user id-based? custom claim?).

