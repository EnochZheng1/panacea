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
  /** The month of the year is one of these (1–12): a letter that comes at Christmas or in July. */
  | { type: 'season'; months: number[] }
  /** The shop has traded at least this many months (0 in its first month). */
  | { type: 'months'; atLeast: number }
  /**
   * The letter `event` has come, and at least `atLeast` months ago. A letter whose condition
   * waits on another letter this way is a follow-up: it is drawn before fresh letters.
   */
  | { type: 'since'; event: string; atLeast: number }
  /**
   * What the shop's remedy is, as its correspondents know it (the bottle it sold most of last
   * month, else the newest in the formula book): its habit per bottle as assayed, the fluid
   * ounces per bottle of one drug class, or a claim on its label.
   */
  | { type: 'remedyHabit'; atLeast: number }
  | { type: 'remedyDrug'; drugClass: DrugClass; atLeastOz: number }
  | { type: 'remedyClaim'; id: string }
  /** The shop's remedy (as above) is priced at this many cents or more. */
  | { type: 'remedyPrice'; atLeastCents: number }
  /** The shop sold at least this many different remedies last month: enough to mix them up. */
  | { type: 'remedies'; atLeast: number }
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
    z.strictObject({
      type: z.literal('season'),
      months: z.array(z.number().int().min(1).max(12)).min(1),
    }),
    z.strictObject({ type: z.literal('months'), atLeast: z.number().int().min(0) }),
    z.strictObject({
      type: z.literal('since'),
      event: IdSchema,
      atLeast: z.number().int().min(0),
    }),
    z.strictObject({ type: z.literal('remedyHabit'), atLeast: z.number() }),
    z.strictObject({
      type: z.literal('remedyDrug'),
      drugClass: z.enum(DRUG_CLASSES),
      atLeastOz: z.number().min(0),
    }),
    z.strictObject({ type: z.literal('remedyClaim'), id: IdSchema }),
    z.strictObject({ type: z.literal('remedyPrice'), atLeastCents: z.number().int().min(0) }),
    z.strictObject({ type: z.literal('remedies'), atLeast: z.number().int().min(0) }),
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
  /**
   * Adds to a capability; with `months`, only for that many month-ends, then it reverts. A
   * capability `callers_<source>` sends that many callers a month to the counter, under `source`.
   */
  z.strictObject({
    type: z.literal('capability'),
    id: IdSchema,
    delta: z.number().int(),
    months: z.number().int().min(1).optional(),
  }),
  /** Authorities confiscate every bottle on the shelf. */
  z.strictObject({ type: z.literal('seizeStock') }),
  /**
   * Nudges the ward's memory of harm (0–100; see the sim's ward system): a sermon or a kind
   * notice that helps the street forget, a story that reminds it.
   */
  z.strictObject({ type: z.literal('wardMemory'), delta: z.number() }),
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

/**
 * Where a reply to a letter books its money, when that is not simply the cost of answering:
 * `outlay` (laid out on the business: a sign, a portrait, a lease), `supplies` (spirit or drugs
 * bought), `fees` (a consideration to an official), `subscriptions` (a subscription, a donation,
 * dues), `trade` (trade given up to answer it). Untagged money is "letters answered".
 */
export const LEDGER_TAGS = ['outlay', 'supplies', 'fees', 'subscriptions', 'trade'] as const;
export type LedgerTag = (typeof LEDGER_TAGS)[number];

export const EventChoiceSchema = z.strictObject({
  id: IdSchema,
  effects: z.array(EffectSchema).default([]),
  /** The choice is only offered when this holds. The default choice must not have one. */
  requires: RequirementSchema.optional(),
  /** Chosen automatically if the month ends without a decision. */
  default: z.boolean().default(false),
  /** Where the reply's cash effects are booked (see `LEDGER_TAGS`). */
  ledger: z.enum(LEDGER_TAGS).optional(),
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
  /**
   * The share of these households wary of a remedy with a narcotic in it: the temperance and
   * chapel households, the mothers who have read of soothing syrups, the respectable trade. A wary
   * buyer will not take a bottle whose opiates and cocaine make a habit of `market.waryHabit` or
   * more, and takes a bottle with none at all on its own recommendation (`market.cleanAppeal`).
   */
  wary: z.number().min(0).max(1).default(0),
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
  /** Wary of narcotics (see `Segment.wary`). */
  wary: z.boolean().default(false),
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
    /**
     * Word of a strong bottle brings callers of its own (`callers.word`): ⌊wordPerHabit ×
     * clamp(habit − wordHabitFloor, 0, wordHabitCap)⌋ a month, by the most habit-forming bottle on
     * the shelf. A mild tonic is not talked about; a narcotic one is.
     */
    wordPerHabit: z.number().min(0),
    wordHabitFloor: z.number().min(0),
    wordHabitCap: z.number().min(0),
    /**
     * The ward is so many households of each kind: a stranger becomes a regular only while the shop
     * has fewer than wardShareSlack × its capacity × the kind's share of the ward (by segment
     * weight) regulars of that kind. 0: no limit.
     */
    wardShareSlack: z.number().min(0),
    /**
     * While the shelf holds a bottle the careful households trust (no narcotic, a modest label),
     * each wary regular pleased with the shop sends carefulPerWary callers a month (rounded down),
     * all wary (`callers.careful`).
     */
    carefulPerWary: z.number().min(0),
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
    /**
     * A craving regular needs more as the dose stops working ("two spoons and then four"): their
     * attachment for the extra bottles is dependency × (bottlesPerAttachment + bottlesPerTolerance
     * × their total tolerance above toleranceFloor), at most habitBottlesMax extra bottles. A mild
     * tonic's regulars never build tolerance past the floor; a strong bottle's do within months.
     */
    bottlesPerTolerance: z.number().min(0),
    toleranceFloor: z.number().min(0),
    /**
     * A regular who craves a strong bottle pays over its price, up to cravingPremium × dependency
     * of it, while they owe less than premiumDebtMonths months' means: in full for a bottle whose
     * habit is premiumHabitFloor + premiumHabitSpan or more, nothing at premiumHabitFloor or less.
     */
    cravingPremium: z.number().min(0),
    premiumDebtMonths: z.number().min(0),
    premiumHabitFloor: z.number().min(0),
    premiumHabitSpan: z.number().positive(),
    habitBottlesMax: z.number().int().min(0),
    budgetBase: z.number().min(0),
    budgetPerAttachment: z.number().min(0),
    /**
     * A regular can afford usual price × meansPerMonth a month. Spending more runs up a debt,
     * paid down in leaner months; while a regular owes more than `ruinMonths` months' means
     * they are ruined: they buy one bottle at most, and their family complains.
     */
    meansPerMonth: z.number().positive(),
    ruinMonths: z.number().positive(),
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
     * A regular makes the last bottle do when the price is past what they can comfortably spend on
     * one, dearAbove × their usual price: they buy that month with chance
     * (comfortable price / price) ^ dearElasticity (1 at or below it). Spirits are sold in every
     * saloon, a narcotic is not: the comfortable price is multiplied by 1 + min(1, the habit the
     * bottle's opiates and cocaine make / dearHabitHold).
     */
    dearAbove: z.number().positive(),
    dearElasticity: z.number().min(0),
    dearHabitHold: z.number().positive(),
    /**
     * A buyer wary of narcotics (`Segment.wary`) will not take a bottle whose opiates and cocaine
     * make a habit of waryHabit or more (a third of an ounce of laudanum at 2), and finds a bottle
     * with no opiate or coca in it and a modest label worth cleanAppeal more, as a claim on its
     * label would be.
     */
    waryHabit: z.number().positive(),
    cleanAppeal: z.number().min(0),
    /** A label bolder than this (its claims' total boldness) the careful households do not trust. */
    waryBoldness: z.number().min(0),
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
    /**
     * A bottle whose habit is under habitFloor (the assay's "none") makes a taste for it, not a
     * craving: a month of such bottles builds dependency up to mildDependencyCap (under
     * withdrawalThreshold) and no further. 0: no floor.
     */
    habitFloor: z.number().min(0),
    mildDependencyCap: z.number().min(0).max(1),
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
    /**
     * A letter's correspondent sends this much fewer callers at the heat that shuts the shop (more
     * than 1: nobody, some way before it). Also scales the standing a reply to a letter earns.
     */
    letterCallerLoss: z.number().min(0),
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
     * Chance a bribe comes out (the coroner's clerk talks): a base chance, plus per bribe already
     * paid, plus per point of notoriety, up to the max. An exposed bribe is a verdict against you, and worse.
     */
    bribeExposureBase: z.number().min(0),
    bribeExposurePerBribe: z.number().min(0),
    bribeExposurePerNotoriety: z.number().min(0),
    bribeExposureMax: z.number().min(0).max(1),
    benignVerdicts: z.array(IdSchema).min(1),
    /**
     * What a bought verdict does to the heat (0: the envelope stops the talk rising, no more). The
     * death that called the inquest has already raised it.
     */
    bribedVerdictHeat: z.number(),
  }),
  /**
   * The ward's memory of harm (0..100), apart from the heat: what the neighbours say. Each
   * month-end it fades to (1 − memoryDecay) of itself, less memoryDecayFlat, and then the month's
   * harms are added to it.
   */
  ward: z.strictObject({
    memoryDecay: z.number().min(0).max(1),
    memoryDecayFlat: z.number().min(0),
    /** A regular's death the remedy caused, that looked it (an overdose or a wasting). */
    perDeath: z.number().min(0),
    perInfantDeath: z.number().min(0),
    /** A stranger found dead with the bottle (a walk-in, or a buyer by post). */
    perStrangerDeath: z.number().min(0),
    /** A death that looked natural, but the remedy hastened. */
    perHastenedDeath: z.number().min(0),
    /** Added for a death from a mis-measured batch: a bottle that killed on the spot. */
    perBadBatchDeath: z.number().min(0),
    /** An inquest whose jury named the remedy. */
    perVerdict: z.number().min(0),
    /** Added when the verdict came of a bribe found out. */
    perExposedBribe: z.number().min(0),
    /** The Board of Health carried the bottles away. */
    perSeizure: z.number().min(0),
    /** At a memory of 100, strangers at the counter buy this much less often… */
    walkInLoss: z.number().min(0).max(1),
    /** …come back as regulars this much less often… */
    conversionLoss: z.number().min(0).max(1),
    /** …and an undependent regular leaves with this chance a month. Scales with the memory. */
    leaveChance: z.number().min(0).max(1),
    /**
     * At a memory of 100, a letter's correspondent sends this much fewer callers (`callers_*`), and
     * a reply to a letter earns this much less standing.
     */
    letterCallerLoss: z.number().min(0).max(1),
    /** The months the observation counts what the ward remembers over (`wardRemembers`). */
    rememberMonths: z.number().int().min(1),
  }),
});
export type Config = z.infer<typeof ConfigSchema>;
