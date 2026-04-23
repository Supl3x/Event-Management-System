ALTER TABLE public.competition
ADD COLUMN IF NOT EXISTS availableseats INT NOT NULL DEFAULT 100;

ALTER TABLE public.registration
ADD COLUMN IF NOT EXISTS prioritynumber INT;

DO $$
DECLARE r RECORD;
BEGIN
  FOR r IN
    SELECT conname
    FROM pg_constraint
    WHERE conrelid = 'public.registration'::regclass
      AND contype = 'c'
      AND pg_get_constraintdef(oid) ILIKE '%status%'
  LOOP
    EXECUTE format('ALTER TABLE public.registration DROP CONSTRAINT %I', r.conname);
  END LOOP;
END $$;

ALTER TABLE public.registration
ADD CONSTRAINT chk_registration_status
CHECK (status IN ('Pending','Approved','Rejected','Confirmed','Waitlist','Cancelled'));

DROP TRIGGER IF EXISTS trg_payment_status_sync ON public.payment;
DROP FUNCTION IF EXISTS public.sync_registration_on_payment_approval();
