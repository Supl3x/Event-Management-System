CREATE TABLE IF NOT EXISTS public.volunteereventrequest (
  requestid SERIAL PRIMARY KEY,
  eventid INT NOT NULL REFERENCES public.event(eventid) ON DELETE CASCADE,
  studentid INT NOT NULL REFERENCES public.student(userid) ON DELETE CASCADE,
  organizerreviewedby INT NULL REFERENCES public.organizer(userid) ON DELETE SET NULL,
  adminreviewedby INT NULL REFERENCES public.admin(userid) ON DELETE SET NULL,
  organizerdecision VARCHAR(20) NOT NULL DEFAULT 'Pending'
    CHECK (organizerdecision IN ('Pending', 'Approved', 'Rejected')),
  admindecision VARCHAR(20) NOT NULL DEFAULT 'Pending'
    CHECK (admindecision IN ('Pending', 'Approved', 'Rejected')),
  status VARCHAR(20) NOT NULL DEFAULT 'Pending'
    CHECK (status IN ('Pending', 'Approved', 'Rejected')),
  requestedat TIMESTAMP NOT NULL DEFAULT NOW(),
  organizerreviewedat TIMESTAMP NULL,
  adminreviewedat TIMESTAMP NULL
);

CREATE INDEX IF NOT EXISTS idx_volunteereventrequest_event
  ON public.volunteereventrequest(eventid);
CREATE INDEX IF NOT EXISTS idx_volunteereventrequest_student
  ON public.volunteereventrequest(studentid);
CREATE INDEX IF NOT EXISTS idx_volunteereventrequest_status
  ON public.volunteereventrequest(status);

CREATE UNIQUE INDEX IF NOT EXISTS ux_volunteer_pending_one_per_event_student
  ON public.volunteereventrequest(eventid, studentid)
  WHERE status = 'Pending';

ALTER TABLE public.student
  DROP CONSTRAINT IF EXISTS chk_student_rollnumber_format;
ALTER TABLE public.student
  ADD CONSTRAINT chk_student_rollnumber_format
  CHECK (rollnumber ~ '^[0-9]{2}-[0-9]{4}$');

ALTER TABLE public.teammember
  DROP CONSTRAINT IF EXISTS chk_teammember_rollnumber_format;
ALTER TABLE public.teammember
  ADD CONSTRAINT chk_teammember_rollnumber_format
  CHECK (rollnumber IS NULL OR rollnumber ~ '^[0-9]{2}-[0-9]{4}$');
