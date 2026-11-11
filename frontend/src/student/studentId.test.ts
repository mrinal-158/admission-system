import { describe, expect, it } from 'vitest';
import { isValidStudentId, normalizeStudentId } from './studentId';

describe('student id', () => {
  it('converts Bangla digits and strips spaces and dashes', () => {
    expect(normalizeStudentId('২০১৫ ২৬৯১-২৩৪৫৬৭৮৯০')).toBe('20152691234567890');
  });

  it('accepts exactly 17 ASCII digits', () => {
    expect(isValidStudentId('20152691234567890')).toBe(true);
    expect(isValidStudentId('2015269123456789')).toBe(false);
    expect(isValidStudentId('201526912345678901')).toBe(false);
    expect(isValidStudentId('2015269123456789a')).toBe(false);
  });
});
