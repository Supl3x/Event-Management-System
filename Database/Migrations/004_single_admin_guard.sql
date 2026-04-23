DO $$
DECLARE keep_userid INT;
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'admin'
  ) THEN
    SELECT a.userid
    INTO keep_userid
    FROM public.admin a
    ORDER BY a.userid
    LIMIT 1;

    IF keep_userid IS NOT NULL THEN
      DELETE FROM public.admin
      WHERE userid <> keep_userid;
    END IF;

    CREATE UNIQUE INDEX IF NOT EXISTS ux_admin_singleton
      ON public.admin ((1));
  END IF;
END $$;
