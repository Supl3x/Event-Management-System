# Booking and Waitlist Test Cases

## TC-01: Booking when seats are available

- **Objective:** Verify registration is confirmed when seats are available.
- **Preconditions:**
  - Event exists with `AvailableSeats > 0`.
  - Logged-in user exists and is not already registered for the event.
- **Steps:**
  1. Open event details page.
  2. Click `Register / Book Ticket`.
  3. Open `My Registrations`.
- **Expected Results:**
  - Registration is created for the user.
  - Registration status is `Confirmed`.
  - Event `AvailableSeats` decreases by `1`.
  - Notification record is created with registration success message.

---

## TC-02: Booking when event is full (waitlist)

- **Objective:** Verify user is waitlisted when no seats are available.
- **Preconditions:**
  - Event exists with `AvailableSeats = 0`.
  - Logged-in user exists and is not already registered for the event.
- **Steps:**
  1. Open event details page.
  2. Click `Register / Book Ticket`.
  3. Open `My Registrations`.
- **Expected Results:**
  - Registration is created for the user.
  - Registration status is `Waitlist`.
  - Event `AvailableSeats` remains `0`.
  - Notification record is created indicating registration with current status.

---

## TC-03: Cancel ticket and promote next waitlist user

- **Objective:** Verify cancellation frees a seat and promotes next waitlisted user.
- **Preconditions:**
  - Event has at least one confirmed ticket and one waitlisted registration.
  - Waitlist has priority values set (lower `PriorityNumber` = higher priority).
  - Admin or Organizer is logged in.
- **Steps:**
  1. Cancel a confirmed ticket (`hardDelete = false` or `true`).
  2. Check event seat count.
  3. Check waitlisted user registration.
  4. Check notifications table.
- **Expected Results:**
  - Ticket becomes `Cancelled` (or is removed if hard delete).
  - Event `AvailableSeats` is increased, then consumed by promoted user if waitlist exists.
  - Highest-priority waitlisted registration changes to `Confirmed`.
  - Promoted user receives a notification: moved from waitlist to confirmed.

---

## TC-04: Duplicate booking prevention

- **Objective:** Verify same user cannot register twice for same event.
- **Preconditions:**
  - Logged-in user already has a registration for target event.
- **Steps:**
  1. Try to register the same event again.
- **Expected Results:**
  - No new registration row is added.
  - Existing registration remains unchanged.
  - User sees an error message: already registered.

---

## Suggested Edge Validations

- Attempt duplicate booking concurrently from two browser sessions.
- Cancel already-cancelled ticket and verify no double seat increment.
- Confirm waitlist promotion order by `PriorityNumber`, then `Timestamp`.
- Validate realtime seat update reaches pages with live seat counters.
