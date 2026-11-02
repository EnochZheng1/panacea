/**
 * Every change to a player-visible number carries a cause, so the Ledger can always explain
 * "why". Each cause needs a `cause.<id>` string in the content string table (checked by tests).
 */
export const CAUSES = {
  salesShop: 'sales.shop',
  salesWalkIn: 'sales.walk_in',
  rent: 'expense.rent',
  ingredients: 'expense.ingredients',
  bottling: 'expense.bottling',
  event: 'event',
  timeline: 'timeline',
  unlock: 'unlock',
} as const;

export type Cause = (typeof CAUSES)[keyof typeof CAUSES];
