export const STUDENT_ID_LENGTH = 17;

const BANGLA_DIGITS = '০১২৩৪৫৬৭৮৯';

/** Accepts Bangla digits and stray spaces/dashes, since parents type on Bangla keyboards. */
export function normalizeStudentId(raw: string): string {
  return raw
    .replace(/[০-৯]/g, (d) => String(BANGLA_DIGITS.indexOf(d)))
    .replace(/[\s-]/g, '');
}

export function isValidStudentId(value: string): boolean {
  return new RegExp(`^\\d{${STUDENT_ID_LENGTH}}$`).test(value);
}
