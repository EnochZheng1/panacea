/**
 * Every change to a player-visible number carries a cause, so the Ledger can always explain
 * "why". Each cause needs a `cause.<id>` string in the content string table (checked by tests).
 */
export const CAUSES = {
  salesShop: 'sales.shop',
  salesWalkIn: 'sales.walk_in',
  salesWholesale: 'sales.wholesale',
  salesPost: 'sales.post',
  /** What craving regulars pay over the shelf price (see `cravingPremiumCents`). */
  salesCraving: 'sales.craving',
  rent: 'expense.rent',
  ingredients: 'expense.ingredients',
  bottling: 'expense.bottling',
  advertising: 'expense.advertising',
  bribe: 'expense.bribe',
  retainer: 'expense.retainer',
  investment: 'expense.investment',
  upkeep: 'expense.upkeep',
  mishap: 'investment.mishap',
  wordOfMouth: 'body.word_of_mouth',
  reputationDrift: 'body.reputation_drift',
  complaints: 'body.complaints',
  death: 'scrutiny.death',
  inquest: 'scrutiny.inquest',
  heatDecay: 'scrutiny.decay',
  claims: 'advertising.claims',
  event: 'event',
  timeline: 'timeline',
  unlock: 'unlock',
} as const;

export type Cause = (typeof CAUSES)[keyof typeof CAUSES];
