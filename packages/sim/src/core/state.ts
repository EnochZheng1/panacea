import type { GameDate } from '@panacea/content';
import type { Action } from './actions.ts';
import type { Cause } from './causes.ts';
import type { RngState } from './rng.ts';

/**
 * The complete game state. Plain JSON-compatible data only (no classes, Maps or functions), so
 * it can be cloned, hashed, saved and diffed. Money is always integer cents.
 */

export const STATE_VERSION = 1;

export type Metric = 'cash' | 'reputation' | 'notoriety';

export interface CausalDelta {
  metric: Metric;
  amount: number;
  cause: Cause;
  /** Optional sub-cause, e.g. the event or formula id responsible. */
  detail?: string;
}

export type NewsItem =
  | { kind: 'timeline'; id: string }
  | { kind: 'unlock'; id: string }
  | { kind: 'event_resolved'; eventId: string; choiceId: string; auto: boolean };

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
  deltas: CausalDelta[];
  news: NewsItem[];
  sales: SaleRecord[];
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
  deltas: CausalDelta[];
  news: NewsItem[];
  sales: SaleRecord[];
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
  createdTurn: number;
}

export interface Batch {
  id: string;
  formulaId: string;
  bottles: number;
  startedTurn: number;
  readyTurn: number;
}

export interface RegularState {
  id: string;
  purchases: number;
  lastPurchaseTurn: number | null;
}

export interface PendingEvent {
  instanceId: string;
  eventId: string;
  drawnTurn: number;
}

export type EndReason = 'timeline_end' | 'bankrupt';

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
    /** Counter for generated ids (formulas, batches, event instances). */
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
    /** Finished bottles on the shelf, by formula id. */
    stock: Record<string, number>;
  };
  regulars: RegularState[];
  stats: {
    bottlesSold: number;
    batchesBrewed: number;
    revenueCents: number;
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
