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

export type LeaveReason = 'recovered' | 'dissatisfied' | 'drifted' | 'bereaved';
export type InquestOutcome = 'cleared' | 'implicated' | 'bribed';

export type NewsItem =
  | { kind: 'timeline'; id: string }
  | { kind: 'unlock'; id: string }
  | { kind: 'event_resolved'; eventId: string; choiceId: string; auto: boolean }
  | { kind: 'death'; deathId: string }
  | { kind: 'inquest'; deathId: string; outcome: InquestOutcome; verdict: string }
  | { kind: 'customer_joined'; customerId: string }
  | { kind: 'customer_left'; customerId: string; name: string; reason: LeaveReason }
  | { kind: 'bad_batch_caught'; formulaId: string; bottles: number }
  | { kind: 'stock_seized'; bottles: number }
  | { kind: 'ads_lapsed' }
  | { kind: 'shutdown_warning'; monthsLeft: number };

export interface SaleRecord {
  formulaId: string;
  bottles: number;
  revenueCents: number;
}

/** Running totals for the month in progress. Actions and turn phases both write here. */
export interface MonthInProgress {
  startCashCents: number;
  startReputation: number;
  startNotoriety: number;
  startHeat: number;
  deltas: CausalDelta[];
  news: NewsItem[];
  sales: SaleRecord[];
  /** Grumbles this month (withdrawal, disappointment). */
  complaints: number;
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
  deltas: CausalDelta[];
  news: NewsItem[];
  sales: SaleRecord[];
  customers: number;
  complaints: number;
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
}

export type DeathCause = 'overdose' | 'poisoning' | 'disease' | 'natural';

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
  /** Truth. */
  cause: DeathCause;
  /** Truth: did your product cause or hasten this death? */
  productImplicated: boolean;
  formulaId: string | null;
  badBatch: boolean;
  /** How easily a chemist would find the drug in the bottle (per-bottle detectability). */
  detectability: number;
  /** Boldness of the label claims on the bottle. */
  boldness: number;
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
  };
  player: {
    cashCents: number;
    reputation: number;
    notoriety: number;
    capabilities: Record<string, number>;
    flags: Record<string, boolean>;
    monthsInsolvent: number;
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
  scrutiny: {
    /** 0..100: how much the neighbourhood, the press and the authorities are talking. */
    heat: number;
    peakHeat: number;
    bribesPaid: number;
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
