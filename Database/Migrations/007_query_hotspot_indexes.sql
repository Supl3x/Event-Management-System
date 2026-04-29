CREATE INDEX IF NOT EXISTS idx_registration_user_registeredat
  ON public.registration(userid, registeredat DESC);

CREATE INDEX IF NOT EXISTS idx_registration_competition_status_priority
  ON public.registration(competitionid, status, prioritynumber);

CREATE INDEX IF NOT EXISTS idx_registration_competition_user
  ON public.registration(competitionid, userid);

CREATE INDEX IF NOT EXISTS idx_payment_status_submittedat
  ON public.payment(status, submittedat DESC);

CREATE INDEX IF NOT EXISTS idx_payment_verifiedby
  ON public.payment(verifiedby);

CREATE INDEX IF NOT EXISTS idx_notification_user_createdat
  ON public.notification(userid, createdat DESC);

CREATE INDEX IF NOT EXISTS idx_notification_user_isread_createdat
  ON public.notification(userid, isread, createdat DESC);
