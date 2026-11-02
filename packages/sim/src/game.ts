import type { ContentPack } from '@panacea/content';
import type { Action, ActionResult } from './core/actions.ts';
import { addMonths, compareDates } from './core/date.ts';
import { closeMonth, openMonth } from './core/ledger.ts';
import { Rng, seedRng } from './core/rng.ts';
import { STATE_VERSION, type GameState, type LedgerEntry, type LogEntry } from './core/state.ts';
import type { Phase } from './core/turn.ts';
import {
  brewBatch,
  buyIngredient,
  createFormula,
  productionPhase,
  setPrice,
} from './systems/lab.ts';
import {
  autoResolveEvents,
  drawEvents,
  resolveEvent,
  timelinePhase,
  unlocksPhase,
} from './systems/progression.ts';
import { expensesPhase, salesPhase } from './systems/sales.ts';

/**
 * Copy-on-write clone. Ledger and log entries are immutable once appended, so those arrays are
 * shallow-copied (sharing their entries); everything else is deep-cloned. This keeps actions and
 * turns O(live state) instead of O(whole history).
 */
export function cloneForWrite(state: GameState): GameState {
  const { ledger, log, ...live } = state;
  return { ...structuredClone(live), ledger: ledger.slice(), log: log.slice() };
}

export interface NewGameOptions {
  seed: string | number;
  backgroundId?: string;
  difficulty?: 'normal';
}

/** Month-end phases, in order. Each is a pure transformation of the draft state. */
export const PHASES: readonly Phase[] = [
  productionPhase,
  salesPhase,
  expensesPhase,
  timelinePhase,
  unlocksPhase,
];

export function newGame(content: ContentPack, options: NewGameOptions): GameState {
  const backgroundId = options.backgroundId ?? content.config.defaultBackground;
  const background = content.byId.backgrounds.get(backgroundId);
  if (!background) throw new Error(`unknown background "${backgroundId}"`);

  const state: GameState = {
    meta: {
      stateVersion: STATE_VERSION,
      contentVersion: content.config.contentVersion,
      seed: String(options.seed),
      backgroundId,
      difficulty: options.difficulty ?? 'normal',
      rng: seedRng(String(options.seed)),
      turn: 0,
      date: { ...content.config.startDate },
      status: 'running',
      endReason: null,
      nextId: 1,
    },
    player: {
      cashCents: background.startingCashCents,
      reputation: content.config.startingReputation,
      notoriety: 0,
      capabilities: { ...background.capabilities },
      flags: Object.fromEntries(background.flags.map((f) => [f, true])),
      monthsInsolvent: 0,
    },
    lab: { inventory: {}, formulas: [], batches: [], stock: {} },
    regulars: content.regulars.map((r) => ({ id: r.id, purchases: 0, lastPurchaseTurn: null })),
    stats: { bottlesSold: 0, batchesBrewed: 0, revenueCents: 0 },
    unlocks: { unlocked: {} },
    events: { pending: [], lastDrawn: {}, timesDrawn: {} },
    timeline: { fired: {} },
    month: {
      startCashCents: background.startingCashCents,
      startReputation: content.config.startingReputation,
      startNotoriety: 0,
      deltas: [],
      news: [],
      sales: [],
    },
    ledger: [],
    log: [],
  };

  const rng = new Rng(state.meta.rng);
  drawEvents(state, content, rng);
  state.meta.rng = rng.state();
  return state;
}

/**
 * Applies one player action. Never mutates the input; returns a new state (unchanged on
 * failure). Successful actions are appended to the replay log.
 */
export function applyAction(
  state: GameState,
  action: Action,
  content: ContentPack,
): { state: GameState; result: ActionResult } {
  if (state.meta.status !== 'running') {
    return { state, result: { ok: false, error: 'the game has ended' } };
  }
  const draft = cloneForWrite(state);
  let result: ActionResult;
  switch (action.type) {
    case 'BuyIngredient':
      result = buyIngredient(draft, action, content);
      break;
    case 'CreateFormula':
      result = createFormula(draft, action, content);
      break;
    case 'SetPrice':
      result = setPrice(draft, action);
      break;
    case 'BrewBatch':
      result = brewBatch(draft, action, content);
      break;
    case 'ResolveEvent': {
      const error = resolveEvent(draft, content, action.instanceId, action.choiceId, false);
      result = error ? { ok: false, error } : { ok: true };
      break;
    }
    default: {
      const unknown: { type?: unknown } = action;
      result = { ok: false, error: `unknown action type "${String(unknown.type)}"` };
    }
  }
  if (!result.ok) return { state, result };
  draft.log.push({ turn: draft.meta.turn, action: structuredClone(action) });
  return { state: draft, result };
}

/**
 * Ends the month: auto-resolves undecided events, runs every phase, closes the Ledger entry,
 * checks for the end of the game, and opens the next month.
 */
export function endTurn(
  state: GameState,
  content: ContentPack,
): { state: GameState; report: LedgerEntry } {
  if (state.meta.status !== 'running') throw new Error('endTurn: the game has ended');
  const draft = cloneForWrite(state);
  const rng = new Rng(draft.meta.rng);
  const ctx = { state: draft, content, rng };

  draft.log.push({ turn: draft.meta.turn, endTurn: true });
  autoResolveEvents(draft, content);
  for (const phase of PHASES) phase.run(ctx);

  draft.player.monthsInsolvent = draft.player.cashCents < 0 ? draft.player.monthsInsolvent + 1 : 0;
  const report = closeMonth(draft);
  draft.ledger.push(report);

  if (draft.player.monthsInsolvent >= content.config.bankruptcyGraceMonths) {
    draft.meta.status = 'ended';
    draft.meta.endReason = 'bankrupt';
  } else if (compareDates(draft.meta.date, content.config.endDate) >= 0) {
    draft.meta.status = 'ended';
    draft.meta.endReason = 'timeline_end';
  } else {
    draft.meta.turn += 1;
    draft.meta.date = addMonths(draft.meta.date, 1);
    draft.month = openMonth(draft);
    drawEvents(draft, content, rng);
  }

  draft.meta.rng = rng.state();
  return { state: draft, report };
}

/** Rebuilds a game from its options and action log. Used for bug reports and determinism tests. */
export function replay(
  content: ContentPack,
  options: NewGameOptions,
  log: readonly LogEntry[],
): GameState {
  let state = newGame(content, options);
  for (const entry of log) {
    if ('endTurn' in entry) {
      state = endTurn(state, content).state;
    } else {
      const { state: next, result } = applyAction(state, entry.action, content);
      if (!result.ok) {
        throw new Error(`replay: action at turn ${entry.turn} failed: ${result.error}`);
      }
      state = next;
    }
  }
  return state;
}
