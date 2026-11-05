import type { ContentPack } from '@panacea/content';
import type { Action, ActionResult } from './core/actions.ts';
import { addMonths, compareDates } from './core/date.ts';
import { closeMonth, openMonth } from './core/ledger.ts';
import { Rng, seedRng } from './core/rng.ts';
import {
  STATE_VERSION,
  type FormulaComponent,
  type GameState,
  type LedgerEntry,
  type LogEntry,
} from './core/state.ts';
import { newScratch, type Phase, type TurnContext } from './core/turn.ts';
import { bodyPhase } from './systems/body.ts';
import { initialCustomers } from './systems/customers.ts';
import {
  brewBatch,
  buyIngredient,
  createFormula,
  productionPhase,
  reformulate,
  setAdSpend,
  setClaims,
  setPrice,
  stockValue,
} from './systems/lab.ts';
import { invest, investmentsPhase } from './systems/investments.ts';
import { advertisingPhase, expensesPhase, salesPhase } from './systems/market.ts';
import {
  autoResolveEvents,
  drawEvents,
  resolveEvent,
  timelinePhase,
  timersPhase,
  unlocksPhase,
} from './systems/progression.ts';
import { heatDecayPhase } from './systems/scrutiny.ts';

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
  heatDecayPhase,
  productionPhase,
  advertisingPhase,
  salesPhase,
  bodyPhase,
  expensesPhase,
  investmentsPhase,
  timelinePhase,
  unlocksPhase,
  timersPhase,
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
      usedNames: [],
    },
    player: {
      cashCents: background.startingCashCents,
      reputation: content.config.startingReputation,
      notoriety: 0,
      capabilities: { ...background.capabilities },
      timedCapabilities: [],
      flags: Object.fromEntries(background.flags.map((f) => [f, true])),
      monthsInsolvent: 0,
      investments: {},
    },
    lab: { inventory: {}, formulas: [], batches: [], stock: [] },
    marketing: { adSpendCents: 0 },
    customers: [],
    deaths: [],
    scrutiny: {
      heat: 0,
      peakHeat: 0,
      bribesPaid: 0,
      lastBribeCents: 0,
      inquestsHeld: 0,
      timesImplicated: 0,
      monthsAtShutdownHeat: 0,
    },
    stats: {
      bottlesSold: 0,
      batchesBrewed: 0,
      revenueCents: 0,
      customersJoined: 0,
      customersLost: 0,
      complaints: 0,
    },
    unlocks: { unlocked: {} },
    events: { pending: [], lastDrawn: {}, timesDrawn: {} },
    timeline: { fired: {} },
    month: {
      startCashCents: background.startingCashCents,
      startReputation: content.config.startingReputation,
      startNotoriety: 0,
      startHeat: 0,
      startStockValueCents: 0,
      deltas: [],
      news: [],
      sales: [],
      complaints: 0,
      turnedAwayByPrice: 0,
      callers: {},
      wholesale: { ordered: 0, delivered: 0, revenueCents: 0 },
      post: { ordered: 0, delivered: 0, revenueCents: 0 },
    },
    ledger: [],
    log: [],
  };

  const rng = new Rng(state.meta.rng);
  state.customers = initialCustomers(state, content, rng);
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
  const result = applyActionInPlace(draft, action, content);
  return result.ok ? { state: draft, result } : { state, result };
}

/**
 * Like `applyAction`, but mutates `state` directly. For callers that own their state outright
 * (the batch harness, replays), where copying on every action would dominate run time.
 * Every handler validates before it mutates, so a failed action leaves `state` unchanged.
 */
export function applyActionInPlace(
  draft: GameState,
  action: Action,
  content: ContentPack,
): ActionResult {
  if (draft.meta.status !== 'running') return { ok: false, error: 'the game has ended' };
  const rng = new Rng(draft.meta.rng);
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
    case 'Reformulate':
      result = reformulate(draft, action, content);
      break;
    case 'SetClaims':
      result = setClaims(draft, action, content);
      break;
    case 'SetAdSpend':
      result = setAdSpend(draft, action);
      break;
    case 'BrewBatch':
      result = brewBatch(draft, action, content);
      break;
    case 'ResolveEvent': {
      const error = resolveEvent(draft, content, rng, action.instanceId, action.choiceId, false);
      result = error ? { ok: false, error } : { ok: true };
      break;
    }
    case 'Invest':
      result = invest(draft, action, content);
      break;
    default: {
      const unknown: { type?: unknown } = action;
      result = { ok: false, error: `unknown action type "${String(unknown.type)}"` };
    }
  }
  if (!result.ok) return result;
  draft.meta.rng = rng.state();
  draft.log.push({ turn: draft.meta.turn, action: canonicalAction(action) });
  return result;
}

/**
 * The action as recorded in the replay log: a fresh copy holding exactly the fields the rules
 * read. Callers often build actions by spreading other objects (e.g. an `ActionSpec`), and
 * stray fields would otherwise end up in the log and the state hash, so that a replay of the
 * same game through another implementation (the C# port) could not reproduce it.
 */
export function canonicalAction(action: Action): Action {
  const components = (cs: readonly FormulaComponent[]) =>
    cs.map((c) => ({ ingredientId: c.ingredientId, amount: c.amount }));
  switch (action.type) {
    case 'BuyIngredient':
      return { type: action.type, ingredientId: action.ingredientId, units: action.units };
    case 'CreateFormula':
      return {
        type: action.type,
        name: action.name,
        components: components(action.components),
        processId: action.processId,
        priceCents: action.priceCents,
        ...(action.claims === undefined ? {} : { claims: [...action.claims] }),
      };
    case 'SetPrice':
      return { type: action.type, formulaId: action.formulaId, priceCents: action.priceCents };
    case 'Reformulate':
      return {
        type: action.type,
        formulaId: action.formulaId,
        components: components(action.components),
        processId: action.processId,
      };
    case 'SetClaims':
      return { type: action.type, formulaId: action.formulaId, claims: [...action.claims] };
    case 'SetAdSpend':
      return { type: action.type, cents: action.cents };
    case 'BrewBatch':
      return { type: action.type, formulaId: action.formulaId, bottles: action.bottles };
    case 'ResolveEvent':
      return { type: action.type, instanceId: action.instanceId, choiceId: action.choiceId };
    case 'Invest':
      return { type: action.type, investmentId: action.investmentId };
  }
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
  return { state: draft, report: endTurnInPlace(draft, content) };
}

/** Like `endTurn`, but mutates `state` directly (see `applyActionInPlace`). */
export function endTurnInPlace(draft: GameState, content: ContentPack): LedgerEntry {
  if (draft.meta.status !== 'running') throw new Error('endTurn: the game has ended');
  const rng = new Rng(draft.meta.rng);
  const ctx: TurnContext = { state: draft, content, rng, scratch: newScratch() };

  draft.log.push({ turn: draft.meta.turn, endTurn: true });
  autoResolveEvents(draft, content, rng);
  for (const phase of PHASES) phase.run(ctx);

  draft.player.monthsInsolvent = draft.player.cashCents < 0 ? draft.player.monthsInsolvent + 1 : 0;
  const { shutdownHeat, shutdownGraceMonths } = content.config.scrutiny;
  const scrutiny = draft.scrutiny;
  scrutiny.monthsAtShutdownHeat =
    scrutiny.heat >= shutdownHeat ? scrutiny.monthsAtShutdownHeat + 1 : 0;
  if (scrutiny.monthsAtShutdownHeat > 0 && scrutiny.monthsAtShutdownHeat < shutdownGraceMonths) {
    draft.month.news.push({
      kind: 'shutdown_warning',
      monthsLeft: shutdownGraceMonths - scrutiny.monthsAtShutdownHeat,
    });
  }
  const report = closeMonth(draft, stockValue(draft, content).totalCents);
  draft.ledger.push(report);

  if (draft.player.monthsInsolvent >= content.config.bankruptcyGraceMonths) {
    draft.meta.status = 'ended';
    draft.meta.endReason = 'bankrupt';
  } else if (scrutiny.monthsAtShutdownHeat >= shutdownGraceMonths) {
    draft.meta.status = 'ended';
    draft.meta.endReason = 'shut_down';
  } else if (compareDates(draft.meta.date, content.config.endDate) >= 0) {
    draft.meta.status = 'ended';
    draft.meta.endReason = 'timeline_end';
  } else {
    draft.meta.turn += 1;
    draft.meta.date = addMonths(draft.meta.date, 1);
    draft.month = openMonth(draft, stockValue(draft, content).totalCents);
    drawEvents(draft, content, rng);
  }

  draft.meta.rng = rng.state();
  return report;
}

/** Rebuilds a game from its options and action log. Used for bug reports and determinism tests. */
export function replay(
  content: ContentPack,
  options: NewGameOptions,
  log: readonly LogEntry[],
): GameState {
  const state = newGame(content, options);
  for (const entry of log) {
    if ('endTurn' in entry) {
      endTurnInPlace(state, content);
    } else {
      const result = applyActionInPlace(state, entry.action, content);
      if (!result.ok) {
        throw new Error(`replay: action at turn ${entry.turn} failed: ${result.error}`);
      }
    }
  }
  return state;
}
