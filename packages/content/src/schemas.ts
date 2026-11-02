import { z } from 'zod';

/**
 * Content schemas. All game content (ingredients, unlocks, events, …) is data validated
 * against these schemas. Player-facing text never lives in the data: every entity's text is
 * looked up in the string tables by key (see `stringKeys.ts`), so the game is
 * localisation-ready from day one.
 */

export const IdSchema = z
  .string()
  .regex(/^[a-z][a-z0-9_]*$/, 'ids must be lower_snake_case and start with a letter');

export const GameDateSchema = z.strictObject({
  year: z.number().int().min(1800).max(1950),
  month: z.number().int().min(1).max(12),
});
export type GameDate = z.infer<typeof GameDateSchema>;

// ---------------------------------------------------------------------------
// Formula properties
// ---------------------------------------------------------------------------

export const PROPERTY_KEYS = [
  'kick', // felt effect: warmth, buzz, numbness, purging
  'calm', // sedation / pain relief
  'habit', // dependency formation
  'acuteToxicity', // per-dose danger, compared against tolerance and vulnerability
  'accumulation', // chronic load that does not clear (heavy metals)
  'bitterness', // perceived strength vs palatability
  'detectability', // how easily a state chemist identifies it
] as const;
export type PropertyKey = (typeof PROPERTY_KEYS)[number];
export type Properties = Record<PropertyKey, number>;

export const PropertiesSchema = z.strictObject({
  kick: z.number(),
  calm: z.number(),
  habit: z.number(),
  acuteToxicity: z.number(),
  accumulation: z.number(),
  bitterness: z.number(),
  detectability: z.number(),
});

// ---------------------------------------------------------------------------
// Requirements (unlock and event conditions) — a small recursive DSL
// ---------------------------------------------------------------------------

export type Requirement =
  | { type: 'cash'; atLeastCents: number }
  | { type: 'reputation'; atLeast: number }
  | { type: 'notoriety'; atLeast: number }
  | { type: 'bottlesSold'; atLeast: number }
  | { type: 'batchesBrewed'; atLeast: number }
  | { type: 'date'; from: GameDate }
  | { type: 'flag'; id: string }
  | { type: 'unlocked'; id: string }
  | { type: 'all'; of: Requirement[] }
  | { type: 'any'; of: Requirement[] }
  | { type: 'not'; requirement: Requirement };

export const RequirementSchema: z.ZodType<Requirement> = z.lazy(() =>
  z.union([
    z.strictObject({ type: z.literal('cash'), atLeastCents: z.number().int() }),
    z.strictObject({ type: z.literal('reputation'), atLeast: z.number() }),
    z.strictObject({ type: z.literal('notoriety'), atLeast: z.number() }),
    z.strictObject({ type: z.literal('bottlesSold'), atLeast: z.number().int() }),
    z.strictObject({ type: z.literal('batchesBrewed'), atLeast: z.number().int() }),
    z.strictObject({ type: z.literal('date'), from: GameDateSchema }),
    z.strictObject({ type: z.literal('flag'), id: IdSchema }),
    z.strictObject({ type: z.literal('unlocked'), id: IdSchema }),
    z.strictObject({ type: z.literal('all'), of: z.array(RequirementSchema).min(1) }),
    z.strictObject({ type: z.literal('any'), of: z.array(RequirementSchema).min(1) }),
    z.strictObject({ type: z.literal('not'), requirement: RequirementSchema }),
  ]),
);

// ---------------------------------------------------------------------------
// Effects — what events, unlocks and timeline entries do to the game
// ---------------------------------------------------------------------------

export const EffectSchema = z.discriminatedUnion('type', [
  z.strictObject({ type: z.literal('cash'), deltaCents: z.number().int() }),
  z.strictObject({ type: z.literal('reputation'), delta: z.number() }),
  z.strictObject({ type: z.literal('notoriety'), delta: z.number() }),
  z.strictObject({ type: z.literal('setFlag'), id: IdSchema }),
  z.strictObject({ type: z.literal('clearFlag'), id: IdSchema }),
  z.strictObject({ type: z.literal('capability'), id: IdSchema, delta: z.number().int() }),
]);
export type Effect = z.infer<typeof EffectSchema>;

// ---------------------------------------------------------------------------
// Entities
// ---------------------------------------------------------------------------

export const SUBSTANCE_CLASSES = [
  'base',
  'narcotic',
  'stimulant',
  'sedative',
  'metal',
  'poison',
  'purgative',
  'filler',
  'flavour',
  'botanical',
  'analgesic',
] as const;

export const IngredientSchema = z.strictObject({
  id: IdSchema,
  substanceClass: z.enum(SUBSTANCE_CLASSES),
  /** Property contribution per fluid ounce. */
  properties: PropertiesSchema,
  /** Price per fluid ounce, in cents (may be fractional; rounded per transaction). */
  costPerUnitCents: z.number().min(0),
  availableFrom: GameDateSchema.optional(),
  tags: z.array(IdSchema).default([]),
  /** Designer-facing historical note. Not shown to players. */
  note: z.string().optional(),
});
export type Ingredient = z.infer<typeof IngredientSchema>;

export const ProcessSchema = z.strictObject({
  id: IdSchema,
  multipliers: PropertiesSchema.partial().default({}),
  extraCostPerBottleCents: z.number().int().min(0).default(0),
  requiresUnlock: IdSchema.optional(),
});
export type Process = z.infer<typeof ProcessSchema>;

export const UNLOCK_CATEGORIES = [
  'facility',
  'channel',
  'marketing',
  'connection',
  'dark_art',
  'product',
  'automation',
] as const;

export const UnlockSchema = z.strictObject({
  id: IdSchema,
  category: z.enum(UNLOCK_CATEGORIES),
  requirement: RequirementSchema,
  effects: z.array(EffectSchema).default([]),
});
export type Unlock = z.infer<typeof UnlockSchema>;

export const EventChoiceSchema = z.strictObject({
  id: IdSchema,
  effects: z.array(EffectSchema).default([]),
  /** Chosen automatically if the month ends without a decision. */
  default: z.boolean().default(false),
});
export type EventChoice = z.infer<typeof EventChoiceSchema>;

export const GameEventSchema = z.strictObject({
  id: IdSchema,
  condition: RequirementSchema,
  /** Probability of being drawn in a month where the condition holds. */
  chance: z.number().min(0).max(1),
  once: z.boolean().default(false),
  cooldownMonths: z.number().int().min(0).default(0),
  choices: z.array(EventChoiceSchema).min(1),
});
export type GameEvent = z.infer<typeof GameEventSchema>;

export const TimelineEntrySchema = z.strictObject({
  id: IdSchema,
  date: GameDateSchema,
  /** True when the historical month is uncertain and has been placed approximately. */
  approximateDate: z.boolean().default(false),
  effects: z.array(EffectSchema).default([]),
  note: z.string().optional(),
});
export type TimelineEntry = z.infer<typeof TimelineEntrySchema>;

export const BuyerProfileSchema = z.strictObject({
  baseWillingnessCents: z.number().int().min(0),
  buyChance: z.number().min(0).max(1),
  preference: z.strictObject({ kick: z.number(), calm: z.number() }),
  bitternessTolerance: z.number(),
  bitternessAversion: z.number().min(0),
});
export type BuyerProfile = z.infer<typeof BuyerProfileSchema>;

export const RegularSchema = z.strictObject({
  id: IdSchema,
  /** Proper names are not translated. */
  displayName: z.string().min(1),
  segment: IdSchema,
  ailment: IdSchema,
  profile: BuyerProfileSchema,
});
export type Regular = z.infer<typeof RegularSchema>;

export const BackgroundSchema = z.strictObject({
  id: IdSchema,
  startingCashCents: z.number().int().min(0),
  capabilities: z.record(IdSchema, z.number().int()).default({}),
  flags: z.array(IdSchema).default([]),
});
export type Background = z.infer<typeof BackgroundSchema>;

export const ConfigSchema = z.strictObject({
  contentVersion: z.string().min(1),
  startDate: GameDateSchema,
  endDate: GameDateSchema,
  defaultBackground: IdSchema,
  startingReputation: z.number().min(0).max(100),
  rentCents: z.number().int().min(0),
  bottleCostCents: z.number().int().min(0),
  /** Fluid ounces per bottle. */
  bottleCapacity: z.number().positive(),
  maxFormulas: z.number().int().min(1),
  maxEventsPerMonth: z.number().int().min(0),
  /** Consecutive months with negative cash before the business fails. */
  bankruptcyGraceMonths: z.number().int().min(1),
  walkIns: z.strictObject({
    base: z.number().int().min(0),
    perReputation: z.number().min(0),
    profile: BuyerProfileSchema,
  }),
});
export type Config = z.infer<typeof ConfigSchema>;
