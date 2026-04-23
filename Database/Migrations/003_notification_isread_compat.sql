DO $$
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'notification'
  ) THEN
    ALTER TABLE public.notification
      ADD COLUMN IF NOT EXISTS isread BOOLEAN;

    IF EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = 'public' AND table_name = 'notification' AND column_name = 'is_read'
    ) THEN
      EXECUTE 'UPDATE public.notification SET isread = COALESCE(isread, is_read)';
    END IF;

    IF EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = 'public' AND table_name = 'notification' AND column_name = 'IsRead'
    ) THEN
      EXECUTE 'UPDATE public.notification SET isread = COALESCE(isread, "IsRead")';
    END IF;

    UPDATE public.notification
    SET isread = FALSE
    WHERE isread IS NULL;

    ALTER TABLE public.notification
      ALTER COLUMN isread SET DEFAULT FALSE,
      ALTER COLUMN isread SET NOT NULL;
  END IF;
END $$;
