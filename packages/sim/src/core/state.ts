import type { BuyerProfile, DrugClass, GameDate } from '@panacea/content';
import type { Action } from './actions.ts';
import type { Cause } from './causes.ts';
import type { RngState } from './rng.ts';

/**
 * The complete game state. Plain JSON-compatible data only (no classes, Maps or functions), so
 * it can be cloned, hashed, saved and diffed. Money is always integer cents.
 *
 * Some fields are ground truth the player never sees directly (a customer's exact health, the
 * real cause of a death, a batch's true strength). The machine API's `observe` exposes only
 * what a player could know.
 */

export const STATE_VERSION = 1;

export type Metric = 'cash' | 'reputation' | 'notoriety' | 'heat';

export interface CausalDelta {
  metric: Metric;
  amount: number;
  cause: Cause;
  /** Optional sub-cause, e.g. the event or formula id responsible. */
  detail?: string;
}

/**
 * Why a regular stopped coming. `afraid`: an undependent regular who heard what the ward says of
 * the bottle (see `GameState.ward`) and took their custom elsewhere.
 */
export type LeaveReason = 'recovered' | 'dissatisfied' | 'drifted' | 'bereaved' | 'afraid';
export type InquestOutcome = 'cleared' | 'implicated' | 'bribed';

export type NewsItem =
  | { kind: 'timeline'; id: string }
  | { kind: 'unlock'; id: string }
  | { kind: 'event_resolved'; eventId: string; choiceId: string; auto: boolean }
  | { kind: 'death'; deathId: string }
  | { kind: 'inquest'; deathId: string; outcome: InquestOutcome; verdict: string }
  /** The coroner took the fee, and his clerk talked: the verdict went against you anyway. */
  | { kind: 'bribe_exposed'; deathId: string }
  | { kind: 'customer_joined'; customerId: string }
  | { kind: 'customer_left'; customerId: string; name: string; reason: LeaveReason }
  | { kind: 'bad_batch_caught'; formulaId: string; bottles: number }
  | { kind: 'stock_seized'; bottles: number }
  | { kind: 'ads_lapsed' }
  | { kind: 'shutdown_warning'; monthsLeft: number }
  /** An investment bought, run out (an almanac's year), lost (a contract cancelled), or a mishap. */
  | { kind: 'investment'; id: string; change: InvestmentChange };

export type InvestmentChange = 'bought' | 'lapsed' | 'lost' | 'mishap';

/**
 * Orders from away this month (druggists under contract, or readers by post): what they asked
 * for, what the shelf could give them, and what they paid.
 */
export interface WholesaleMonth {
  ordered: number;
  delivered: number;
  revenueCents: number;
}

/** An investment the shop holds. */
export interface InvestmentHolding {
  boughtTurn: number;
  /** It runs through the month before this one, then lapses; null if it is held for good. */
  endsTurn: number | null;
}

/**
 * One formula's sales this month, with where the bottles went: to regulars at the counter
 * (`shop`), to strangers at the counter (`walkIn`), to the druggists under contract
 * (`wholesale`) and by post (`post`). The four splits sum to `bottles` and `revenueCents`.
 */
export interface SaleRecord {
  formulaId: string;
  bottles: number;
  revenueCents: number;
  shopBottles: number;
  shopRevenueCents: number;
  walkInBottles: number;
  walkInRevenueCents: number;
  wholesaleBottles: number;
  wholesaleRevenueCents: number;
  postBottles: number;
  postRevenueCents: number;
  /**
   * Of `shopRevenueCents`, what regulars who crave the bottle paid over the shelf price (see
   * `cravingPremiumCents`): the counter took `shopBottles` × the price, and this besides.
   */
  cravingPremiumCents: number;
}

/** Where a bottle sold went (see `SaleRecord`). */
export type SaleSource = 'shop' | 'walkIn' | 'wholesale' | 'post';

/** Running totals for the month in progress. Actions and turn phases both write here. */
export interface MonthInProgress {
  startCashCents: number;
  startReputation: number;
  startNotoriety: number;
  startHeat: number;
  startWardMemory: number;
  /** What the stock was worth at cost when the month opened (see `stockValue`). */
  startStockValueCents: number;
  deltas: CausalDelta[];
  news: NewsItem[];
  sales: SaleRecord[];
  /** Grumbles this month (withdrawal, disappointment, families of regulars who are ruined). */
  complaints: number;
  /**
   * Callers who asked the price and left: walk-ins, and regulars who came in for their first
   * bottle, for whom some bottle on the shelf appealed but every one cost more than they would pay;
   * and regulars who found the bottle dearer than they could comfortably spend and made the last
   * one do (see `dearness`).
   */
  turnedAwayByPrice: number;
  /**
   * Callers wary of narcotics (see `Customer.wary`), walk-ins and regulars, who found something
   * on the shelf to their liking but would not take it for the opiate or coca in it.
   */
  refusedNarcotic: number;
  /**
   * Regulars whose bottle this month fell short of what its label promised for their complaint
   * (see `market.promiseShare`): they had been told it cured them.
   */
  disappointed: number;
  /**
   * Callers at the shop this month, by where they came from: `ward` (the neighbourhood, by
   * reputation), `newspaper`, `subscriptions` (societies and the like), and each investment id.
   */
  callers: Record<string, number>;
  /** The druggists under contract. */
  wholesale: WholesaleMonth;
  /** Orders by post (the almanac's readers). */
  post: WholesaleMonth;
}

/** A closed month, as shown in the Ledger. */
export interface LedgerEntry {
  turn: number;
  date: GameDate;
  cashStartCents: number;
  cashEndCents: number;
  reputationStart: number;
  reputationEnd: number;
  notorietyStart: number;
  notorietyEnd: number;
  heatStart: number;
  heatEnd: number;
  /** What the ward remembered against the shop when the month opened and closed (0..100). */
  wardMemoryStart: number;
  wardMemoryEnd: number;
  /**
   * Ingredients on hand and bottles brewing or on the shelf, valued at cost, when the month
   * opened and when it closed. Cash spent on stock is not lost: the books show it here.
   */
  stockValueStartCents: number;
  stockValueEndCents: number;
  deltas: CausalDelta[];
  news: NewsItem[];
  sales: SaleRecord[];
  customers: number;
  complaints: number;
  /** Callers who asked the price and left (see `MonthInProgress.turnedAwayByPrice`). */
  turnedAwayByPrice: number;
  /** Wary callers who would not take a narcotic bottle (see `MonthInProgress.refusedNarcotic`). */
  refusedNarcotic: number;
  /** Regulars the label disappointed (see `MonthInProgress.disappointed`). */
  disappointed: number;
  /** Callers by source (see `MonthInProgress.callers`). */
  callers: Record<string, number>;
  wholesale: WholesaleMonth;
  post: WholesaleMonth;
  /** Bottles left on the shelf when the month closed: 0 is a month that sold out. */
  shelfEndBottles: number;
}

export interface FormulaComponent {
  ingredientId: string;
  /** Fluid ounces per bottle. */
  amount: number;
}

export interface Formula {
  id: string;
  name: string;
  components: FormulaComponent[];
  processId: string;
  priceCents: number;
  /** Label and advertising claims (claim ids). */
  claims: string[];
  createdTurn: number;
}

export interface Batch {
  id: string;
  formulaId: string;
  /** The recipe as it was when brewed (formulas can be reformulated later). */
  components: FormulaComponent[];
  processId: string;
  bottles: number;
  startedTurn: number;
  readyTurn: number;
}

/** Finished bottles from one batch. Truth: `potency` is the batch's real strength. */
export interface Lot {
  id: string;
  formulaId: string;
  components: FormulaComponent[];
  processId: string;
  bottles: number;
  potency: number;
  badBatch: boolean;
}

export interface Body {
  health: number;
  baseHealth: number;
  tolerance: Record<DrugClass, number>;
  /** 0..1 */
  dependency: number;
  /** Accumulated toxins that do not clear quickly (heavy metals). */
  load: number;
}

/** A regular customer of the shop. */
export interface Customer {
  id: string;
  name: string;
  segment: string;
  ailment: string;
  /** Who takes the medicine: the customer, or an infant in their care. */
  consumer: 'self' | 'infant';
  profile: BuyerProfile;
  joinedTurn: number;
  /** Turn the complaint clears up by itself, if it ever does. */
  ailmentEndsTurn: number | null;
  body: Body;
  /** -1..1 */
  satisfaction: number;
  purchases: number;
  lastPurchaseTurn: number | null;
  lastFormulaId: string | null;
  bottlesLastMonth: number;
  inWithdrawal: boolean;
  /**
   * Weaning off an old recipe: still dependent (above `body.mildDependencyCap`) on a bottle that
   * had a narcotic in it, while this month's bottles were all mild. The craving fades by
   * `body.weaningDecay` a month.
   */
  weaning: boolean;
  /**
   * What the customer owes for spending beyond their means (the pawnshop, the grocer's slate), in
   * cents. Paid down in months they spend less than they can afford.
   */
  debtCents: number;
  /**
   * Wary of narcotics (see `Segment.wary` in the content): will not take a bottle whose opiates and
   * coca make a habit of `market.waryHabit` or more, and trusts one with none in it.
   */
  wary: boolean;
}

export type DeathCause = 'overdose' | 'poisoning' | 'disease' | 'natural';

/**
 * The cause of a death as the ward and the coroner's register give it, which the player sees:
 *
 * - `overdose`: found insensible after a dose (every overdose; the bottle is the plain suspect).
 * - `wasting`: wasted away, as from a slow poison (a load of metals).
 * - `ailment`: of the complaint they came in with (`DeathRecord.ailment`): the consumption, a
 *   cough gone to the chest, an infant's teething fever.
 * - `old_age`: an elderly customer, in the ordinary way.
 * - `natural`: suddenly, of a fever going about, in the ordinary way.
 *
 * It never says whether the remedy hastened a death that looked natural: that stays hidden.
 */
export type ReportedCause = 'overdose' | 'wasting' | 'ailment' | 'old_age' | 'natural';

/** The human ledger. Truth fields are hidden from the player. */
export interface DeathRecord {
  id: string;
  turn: number;
  date: GameDate;
  customerId: string | null;
  name: string;
  segment: string;
  consumer: 'self' | 'infant';
  anonymous: boolean;
  /** The complaint they took the remedy for, if known (a stranger by post: null). */
  ailment: string | null;
  /** Truth. */
  cause: DeathCause;
  /** The cause as the ward gives it (see `ReportedCause`): visible to the player. */
  reportedCause: ReportedCause;
  /** Truth: did your product cause or hasten this death? */
  productImplicated: boolean;
  formulaId: string | null;
  badBatch: boolean;
  /** How easily a chemist would find the drug in the bottle (per-bottle detectability). */
  detectability: number;
  /** Boldness of the label claims on the bottle. */
  boldness: number;
  /**
   * The label on their bottle claimed to cure their complaint (a `cures` claim that names it): a
   * broken promise, which the papers print beside the notice.
   */
  promisedCure: boolean;
  /** What the customer owed for spending beyond their means on the bottle when they died. */
  owedCents: number;
  inquest: 'none' | 'pending' | InquestOutcome;
  verdict: string | null;
}

export interface PendingEvent {
  instanceId: string;
  eventId: string;
  drawnTurn: number;
  /** For system events: the death an inquest concerns. */
  deathId?: string;
}

/** A capability change that holds until month `expiresTurn` begins. */
export interface TimedCapability {
  id: string;
  delta: number;
  expiresTurn: number;
}

export type EndReason = 'timeline_end' | 'bankrupt' | 'shut_down';

export type LogEntry = { turn: number; action: Action } | { turn: number; endTurn: true };

export interface GameState {
  meta: {
    stateVersion: number;
    contentVersion: string;
    seed: string;
    backgroundId: string;
    difficulty: 'normal';
    rng: RngState;
    /** 0-based month counter. */
    turn: number;
    date: GameDate;
    status: 'running' | 'ended';
    endReason: EndReason | null;
    /** Counter for generated ids (formulas, batches, customers, events…). */
    nextId: number;
    /**
     * Every given name and surname ("Elias Whitlock") a customer has had in this game, living,
     * dead or gone, so that no two people share one.
     */
    usedNames: string[];
  };
  player: {
    cashCents: number;
    reputation: number;
    notoriety: number;
    capabilities: Record<string, number>;
    /** Capability changes that last a while (a rival's sale, a dearer supplier), oldest first. */
    timedCapabilities: TimedCapability[];
    flags: Record<string, boolean>;
    monthsInsolvent: number;
    /** Investment id → the holding, for investments the shop holds now. */
    investments: Record<string, InvestmentHolding>;
  };
  lab: {
    /** Fluid ounces on hand, by ingredient id. */
    inventory: Record<string, number>;
    formulas: Formula[];
    batches: Batch[];
    /** Finished bottles on the shelf, oldest first. */
    stock: Lot[];
  };
  marketing: {
    adSpendCents: number;
  };
  customers: Customer[];
  deaths: DeathRecord[];
  /**
   * The ward's memory of harm: what the neighbours say of the shop, apart from the authorities'
   * suspicion (`scrutiny.heat`). Deaths the remedy caused or hastened, verdicts that named it and
   * bottles the Board of Health carried away add to it; it fades by a share each month, so a bad
   * month weighs for most of a year. It puts off strangers at the counter, keeps them from coming
   * back as regulars, and sends some regulars elsewhere.
   */
  ward: {
    /** 0..100. */
    memory: number;
    peakMemory: number;
  };
  scrutiny: {
    /** 0..100: how much the neighbourhood, the press and the authorities are talking. */
    heat: number;
    peakHeat: number;
    bribesPaid: number;
    /** The last fee the coroner was paid, in cents (0 before the first). */
    lastBribeCents: number;
    inquestsHeld: number;
    timesImplicated: number;
    /** Consecutive month-ends at or above the shutdown heat. */
    monthsAtShutdownHeat: number;
  };
  stats: {
    bottlesSold: number;
    batchesBrewed: number;
    revenueCents: number;
    customersJoined: number;
    customersLost: number;
    complaints: number;
  };
  unlocks: {
    /** Unlock id → turn it was gained. */
    unlocked: Record<string, number>;
  };
  events: {
    pending: PendingEvent[];
    /** Event id → last turn it was drawn. */
    lastDrawn: Record<string, number>;
    /** Event id → times drawn. */
    timesDrawn: Record<string, number>;
  };
  timeline: {
    /** Timeline entry id → turn it fired. */
    fired: Record<string, number>;
  };
  month: MonthInProgress;
  /** Closed months. Entries are immutable once appended (copies share them). */
  ledger: LedgerEntry[];
  /** Replay log. Entries are immutable once appended (copies share them). */
  log: LogEntry[];
}
