ALTER TABLE public.student
  DROP CONSTRAINT IF EXISTS chk_student_rollnumber_format;

ALTER TABLE public.teammember
  DROP CONSTRAINT IF EXISTS chk_teammember_rollnumber_format;
