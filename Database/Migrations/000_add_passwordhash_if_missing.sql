-- Ensure EF model column exists for seeding/login.
-- Your startup seeding writes User.PasswordHash into `public.users.passwordhash`.
ALTER TABLE public.users
  ADD COLUMN IF NOT EXISTS passwordhash VARCHAR(500);

