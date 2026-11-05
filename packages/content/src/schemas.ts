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
  | { type: 'heat'; atLeast: number }
  | { type: 'deaths'; atLeast: number }
  | { type: 'inquests'; atLeast: number }
  | { type: 'regulars'; atLeast: number }
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
    z.strictObject({ type: z.literal('heat'), atLeast: z.number() }),
    z.strictObject({ type: z.literal('deaths'), atLeast: z.number().int() }),
    z.strictObject({ type: z.literal('inquests'), atLeast: z.number().int() }),
    z.strictObject({ type: z.literal('regulars'), atLeast: z.number().int() }),
    z.strictObject({ type: z.literal('date'), from: GameDateSchema }),
    z.strictObject({ type: z.literal('flag'), id: IdSchema }),
    z.strictObject({ type: z.literal('unlocked'), id: IdSchema }),
    z.strictObject({ type: z.literal('all'), of: z.array(RequirementSchema).min(1) }),
    z.strictObject({ type: z.literal('any'), of: z.array(RequirementSchema).min(1) }),
    z.strictObject({ type: z.literal('not'), requirement: RequirementSchema }),
  ]),
);

/**
 * Flags the simulation itself sets (not content). Content may require them; the linter treats
 * them as always producible.
 */
export const SYSTEM_FLAGS = [
  'survived_inquest',
  'suffered_bad_batch',
  'implicated_at_inquest',
] as const;
export type SystemFlag = (typeof SYSTEM_FLAGS)[number];

// ---------------------------------------------------------------------------
// Effects — what events, unlocks and timeline entries do to the game
// ---------------------------------------------------------------------------

export const EffectSchema = z.discriminatedUnion('type', [
  z.strictObject({ type: z.literal('cash'), deltaCents: z.number().int() }),
  z.strictObject({ type: z.literal('reputation'), delta: z.number() }),
  z.strictObject({ type: z.literal('notoriety'), delta: z.number() }),
  z.strictObject({ type: z.literal('heat'), delta: z.number() }),
  z.strictObject({ type: z.literal('setFlag'), id: IdSchema }),
  z.strictObject({ type: z.literal('clearFlag'), id: IdSchema }),
  /** Adds to a capability; with `months`, only for that many month-ends, then it reverts. */
  z.strictObject({
    type: z.literal('capability'),
    id: IdSchema,
    delta: z.number().int(),
    months: z.number().int().min(1).optional(),
  }),
  /** Authorities confiscate every bottle on the shelf. */
  z.strictObject({ type: z.literal('seizeStock') }),
  /** Resolves the coroner's inquest attached to a system event (see the sim's scrutiny system). */
  z.strictObject({
    type: z.literal('inquest'),
    approach: z.enum(['testify', 'silent', 'bribe']),
  }),
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

/** Drugs the body builds tolerance and dependency to, separately per class. */
export const DRUG_CLASSES = ['alcohol', 'opiate', 'cocaine'] as const;
export type DrugClass = (typeof DRUG_CLASSES)[number];

export const IngredientSchema = z.strictObject({
  id: IdSchema,
  substanceClass: z.enum(SUBSTANCE_CLASSES),
  drugClass: z.enum(DRUG_CLASSES).optional(),
  /** Property contribution per fluid ounce. */
  properties: PropertiesSchema,
  /** Price per fluid ounce, in cents (may be fractional; rounded per transaction). */
  costPerUnitCents: z.number().min(0),
  availableFrom: GameDateSchema.optional(),
  tags: z.array(IdSchema).default([]),
  /** How the liquid looks in glass: the only saturated colour in the game's world. */
  appearance: z
    .strictObject({
      color: z.string().regex(/^#[0-9a-f]{6}$/i, 'colour must be #rrggbb'),
      /** 0 = clear as water, 1 = opaque. Also weights the colour when liquids are mixed. */
      opacity: z.number().min(0).max(1),
    })
    .default({ color: '#d8d0c0', opacity: 0.2 }),
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
  /** The choice is only offered when this holds. The default choice must not have one. */
  requires: RequirementSchema.optional(),
  /** Chosen automatically if the month ends without a decision. */
  default: z.boolean().default(false),
});
export type EventChoice = z.infer<typeof EventChoiceSchema>;

export const GameEventSchema = z.strictObject({
  id: IdSchema,
  /**
   * `random` events are drawn into The Post when their condition holds; `system` events are
   * raised by the simulation itself (e.g. an inquest after a death) and carry context.
   */
  trigger: z.enum(['random', 'system']).default('random'),
  condition: RequirementSchema.optional(),
  /** Probability of being drawn in a month where the condition holds (random events only). */
  chance: z.number().min(0).max(1).default(0),
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

export const AilmentSchema = z.strictObject({
  id: IdSchema,
  /** How much felt relief the sufferer needs to be satisfied. */
  severity: z.number().min(0),
  /** Health change per month from the illness itself (negative = worsening). */
  healthDriftPerMonth: z.number(),
  /** Months after which the complaint resolves by itself, as [min, max]. */
  resolvesAfterMonths: z.tuple([z.number().int().min(1), z.number().int().min(1)]).optional(),
  /** Who actually takes the medicine: the buyer, or an infant in their care. */
  consumer: z.enum(['self', 'infant']).default('self'),
  /** Multiplier on natural mortality. */
  mortalityMultiplier: z.number().min(0).default(1),
});
export type Ailment = z.infer<typeof AilmentSchema>;

export const SegmentSchema = z.strictObject({
  id: IdSchema,
  /** Relative share of walk-in customers. */
  weight: z.number().min(0),
  /** Multiplier on how hard each dose hits (toxicity and harm). */
  vulnerability: z.number().positive(),
  baseHealth: z.number().min(1).max(100),
  /** Monthly probability of dying of unrelated causes at full health. */
  naturalHazard: z.number().min(0).max(1),
  gender: z.enum(['female', 'male', 'any']),
  /** Forms of address for generated names, e.g. ["Mrs."]. */
  titles: z.array(z.string().min(1)).min(1),
  ailments: z.array(z.strictObject({ id: IdSchema, weight: z.number().positive() })).min(1),
  profile: BuyerProfileSchema,
});
export type Segment = z.infer<typeof SegmentSchema>;

/** One stock of names (Irish, German, Yankee…): given names are only paired with its own surnames. */
export const NameOriginSchema = z.strictObject({
  id: IdSchema,
  /** How common the stock is in the ward. */
  weight: z.number().positive(),
  female: z.array(z.string().min(1)).min(1),
  male: z.array(z.string().min(1)).min(1),
  surnames: z.array(z.string().min(1)).min(1),
});
export type NameOrigin = z.infer<typeof NameOriginSchema>;

export const NamesSchema = z.strictObject({
  origins: z.array(NameOriginSchema).min(1),
});
export type Names = z.infer<typeof NamesSchema>;

export const ClaimSchema = z.strictObject({
  id: IdSchema,
  /** Ailments the claim speaks to. Empty means it appeals to everyone. */
  ailments: z.array(IdSchema).default([]),
  /** How much the claim draws customers. */
  appeal: z.number().min(0),
  /** How much the claim draws scrutiny. */
  boldness: z.number().min(0),
  requiresUnlock: IdSchema.optional(),
});
export type Claim = z.infer<typeof ClaimSchema>;

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

/**
 * Bottles that leave the shop every month for buyers elsewhere: `bottlesPerMonth` × (1 +
 * reputation × `perReputation`) × the price factor, at `pricePct`% of the shelf price but at most
 * `maxPriceCents` a bottle. The price factor is 1 + `priceElasticity` × (`refPriceCents` − shelf
 * price) / `refPriceCents`, between 0 and `maxOrderBoost`: they sell the bottle on at your shelf
 * price, so a dear bottle sells fewer out there. The orders are filled after the regulars have had
 * theirs (see `market.ordersShelfShare`), from the bottle the shop has most of. They go to
 * strangers of `strangerHealth` with no tolerance, and a strong enough bottle kills some of them.
 */
export const OrdersSchema = z.strictObject({
  bottlesPerMonth: z.number().int().min(0),
  perReputation: z.number().min(0),
  pricePct: z.number().min(0).max(100),
  maxPriceCents: z.number().int().min(0),
  strangerHealth: z.number().min(1).max(100),
  refPriceCents: z.number().int().min(1).default(60),
  priceElasticity: z.number().min(0).default(0),
  maxOrderBoost: z.number().min(1).default(1),
});
export type Orders = z.infer<typeof OrdersSchema>;

/**
 * Something the proprietor can buy that pays back over months or years: a branch shop, an
 * almanac, a wholesale contract, a laboratory. Bought with the `Invest` action; held until it
 * runs out (`months`), is lost (`lostWhen`), or for good.
 */
export const InvestmentSchema = z.strictObject({
  id: IdSchema,
  /** Paid once, when bought (and again each time a lapsed one is renewed). */
  costCents: z.number().int().min(0),
  /** Paid at every month-end while it is held (rent, wages). */
  upkeepCents: z.number().int().min(0).default(0),
  /** Runs this many month-ends from the month it is bought, then lapses and may be bought again. */
  months: z.number().int().min(1).optional(),
  /** Offered only while this holds. */
  requires: RequirementSchema.optional(),
  /** Checked at every month-end: if it holds, the investment is lost (and may be bought again). */
  lostWhen: RequirementSchema.optional(),
  /** Extra callers at the shop every month while it is held. */
  walkInsPerMonth: z.number().int().min(0).default(0),
  /** Heat each month per point of label boldness, while held (claims printed where all can read). */
  heatPerBoldness: z.number().min(0).default(0),
  /**
   * Druggists out of town who take bottles every month at a wholesale price (see `OrdersSchema`).
   * They read the papers, and cancel (`lostWhen`) once the talk turns ugly.
   */
  wholesale: OrdersSchema.optional(),
  /** Readers who send for a bottle by post (an almanac's): orders at the shelf price, by mail. */
  postOrders: OrdersSchema.optional(),
  /** A monthly chance of an accident while held (a laboratory fire), with its effects. */
  mishap: z
    .strictObject({
      chancePerMonth: z.number().min(0).max(1),
      effects: z.array(EffectSchema).min(1),
    })
    .optional(),
  /**
   * Applied when bought. Capability changes (which must not be timed) are undone when the
   * investment lapses or is lost.
   */
  effects: z.array(EffectSchema).default([]),
});
export type Investment = z.infer<typeof InvestmentSchema>;

export const ConfigSchema = z.strictObject({
  contentVersion: z.string().min(1),
  startDate: GameDateSchema,
  endDate: GameDateSchema,
  defaultBackground: IdSchema,
  startingReputation: z.number().min(0).max(100),
  /** Reputation drifts toward this baseline when nothing happens… */
  reputationBaseline: z.number().min(0).max(100),
  /** …by this fraction of the gap each month. */
  reputationDrift: z.number().min(0).max(1),
  rentCents: z.number().int().min(0),
  bottleCostCents: z.number().int().min(0),
  /** Fluid ounces per bottle. */
  bottleCapacity: z.number().positive(),
  /**
   * The most bottles one still makes in a batch. A capability `still_bottles` (a larger copper)
   * adds to it, for every still.
   */
  stillBottles: z.number().int().min(1),
  maxFormulas: z.number().int().min(1),
  maxEventsPerMonth: z.number().int().min(0),
  /** Consecutive months with negative cash before the business fails. */
  bankruptcyGraceMonths: z.number().int().min(1),
  maxRegulars: z.number().int().min(0),
  maxClaimsPerFormula: z.number().int().min(0),
  walkIns: z.strictObject({
    base: z.number().int().min(0),
    perReputation: z.number().min(0),
    /** Chance a satisfied walk-in becomes a regular. */
    conversionChance: z.number().min(0).max(1),
  }),
  market: z.strictObject({
    /**
     * Willingness to pay = usual price × clamp(willingnessBase + appeal / appealPerWillingness,
     * 0, willingnessMax) × (1 + dependency).
     */
    willingnessBase: z.number().min(0),
    appealPerWillingness: z.number().positive(),
    willingnessMax: z.number().min(0),
    /**
     * A regular's attachment is max(dependency, satisfaction). They want
     * 1 + ⌊attachment × bottlesPerAttachment⌋ bottles, and will spend up to usual price ×
     * (budgetBase + budgetPerAttachment × attachment) × (1 + dependency) on the extra ones.
     */
    bottlesPerAttachment: z.number().min(0),
    budgetBase: z.number().min(0),
    budgetPerAttachment: z.number().min(0),
    /**
     * A regular can afford usual price × meansPerMonth a month. Spending more runs up a debt,
     * paid down in leaner months; while a regular owes more than a month's means they are
     * ruined: they buy one bottle at most, and their family complains.
     */
    meansPerMonth: z.number().positive(),
    /**
     * The habit eats the means: a regular's means are multiplied by
     * (1 − meansLostToHabit × dependency).
     */
    meansLostToHabit: z.number().min(0).max(1),
    /**
     * A walk-in hesitates over a dear bottle: their chance to buy is multiplied by
     * 1 − priceHesitation × clamp(1 − headroom / hesitationMargin, 0, 1), where headroom is
     * 1 − price / willingness to pay.
     */
    priceHesitation: z.number().min(0).max(1),
    hesitationMargin: z.number().positive(),
    /**
     * Orders from away (druggists, the post) are filled after the regulars have had theirs, from
     * at most this share of what is left on the shelf; the walk-ins get the rest.
     */
    ordersShelfShare: z.number().min(0).max(1),
  }),
  advertising: z.strictObject({
    /** Extra walk-ins ≈ visitorsPerSqrtCent × √(monthly spend in cents). */
    visitorsPerSqrtCent: z.number().min(0),
    /** Heat per month per point of claim boldness while advertising. */
    heatPerBoldness: z.number().min(0),
    /** Heat per month per point of boldness printed on labels of bottles on sale. */
    labelHeatPerBoldness: z.number().min(0),
  }),
  batches: z.strictObject({
    /** Standard deviation of batch potency around 1.0. */
    potencySpread: z.number().min(0),
    badBatchChance: z.number().min(0).max(1),
    badBatchPotency: z.tuple([z.number().positive(), z.number().positive()]),
  }),
  body: z.strictObject({
    toleranceGain: z.number().min(0),
    toleranceDecay: z.number().min(0).max(1),
    /** How much tolerance protects against a drug class's toxicity. */
    toleranceProtection: z.number().min(0),
    dependencyGain: z.number().min(0),
    dependencyDecay: z.number().min(0).max(1),
    /** Dependency above which missing doses cause withdrawal. */
    withdrawalThreshold: z.number().min(0).max(1),
    withdrawalHarm: z.number().min(0),
    loadDecay: z.number().min(0).max(1),
    loadSafe: z.number().min(0),
    loadHarm: z.number().min(0),
    acuteHarm: z.number().min(0),
    recovery: z.number().min(0).max(1),
    /** Monthly chance an unsatisfied, undependent regular stops coming. */
    lapseChance: z.number().min(0).max(1),
    /** Infants are far more vulnerable, but are given smaller doses. */
    infantVulnerability: z.number().positive(),
    infantDoseScale: z.number().positive(),
  }),
  scrutiny: z.strictObject({
    heatDecayFlat: z.number().min(0),
    heatDecayRate: z.number().min(0).max(1),
    /** Extra decay at high heat: heatDecayHigh × heat × heat / 100 a month. */
    heatDecayHigh: z.number().min(0).max(1),
    heatPerDeath: z.number().min(0),
    heatPerInfantDeath: z.number().min(0),
    heatPerAnonymousDeath: z.number().min(0),
    heatPerComplaint: z.number().min(0),
    /** At this heat the authorities move against you… */
    shutdownHeat: z.number().positive(),
    /** …and close the shop if it stays there this many consecutive months. */
    shutdownGraceMonths: z.number().int().min(1),
    inquestBaseChance: z.number().min(0).max(1),
    bribeBaseCents: z.number().int().min(0),
    /** Each bribe already paid makes the next dearer by this share of the base fee. */
    bribeRisePerBribe: z.number().min(0),
    /**
     * Whatever the heat, every fee is at least this much more than the last one paid: the fee
     * rises with every envelope.
     */
    bribeMinRiseCents: z.number().int().min(0),
    /** The fee is multiplied by 1 + heat / bribeHeatDoubling: a notorious case costs more. */
    bribeHeatDoubling: z.number().positive(),
    /**
     * Chance a bribe comes out (the coroner's clerk talks): per bribe already paid, plus per
     * point of notoriety, up to the max. An exposed bribe is a verdict against you, and worse.
     */
    bribeExposurePerBribe: z.number().min(0),
    bribeExposurePerNotoriety: z.number().min(0),
    bribeExposureMax: z.number().min(0).max(1),
    benignVerdicts: z.array(IdSchema).min(1),
  }),
});
export type Config = z.infer<typeof ConfigSchema>;
