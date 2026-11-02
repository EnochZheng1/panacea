import type { GameDate } from '@panacea/content';

/** Months since year 0; makes date arithmetic and comparison trivial. */
export const dateIndex = (d: GameDate): number => d.year * 12 + (d.month - 1);

export const fromIndex = (i: number): GameDate => ({
  year: Math.floor(i / 12),
  month: (i % 12) + 1,
});

export const addMonths = (d: GameDate, months: number): GameDate =>
  fromIndex(dateIndex(d) + months);

export const compareDates = (a: GameDate, b: GameDate): number => dateIndex(a) - dateIndex(b);

export const monthsBetween = (from: GameDate, to: GameDate): number =>
  dateIndex(to) - dateIndex(from);

export const formatDate = (d: GameDate): string => `${d.year}-${String(d.month).padStart(2, '0')}`;
