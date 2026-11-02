import type { ContentPack } from '@panacea/content';
import {
  MAX_FORMULA_NAME_LENGTH,
  MAX_PRICE_CENTS,
  type Action,
  type ActionResult,
} from '../core/actions.ts';
import { CAUSES } from '../core/causes.ts';
import { adjust } from '../core/ledger.ts';
import type { GameState, Phase } from '../core/index.ts';
import { isIngredientAvailable, validateFormula } from './formula.ts';

/** Inventory quantities are rounded to avoid floating-point dust accumulating over a run. */
export const roundQty = (x: number): number => Math.round(x * 1e6) / 1e6;

const fail = (error: string): ActionResult => ({ ok: false, error });

const isValidPrice = (p: number) => Number.isInteger(p) && p >= 0 && p <= MAX_PRICE_CENTS;

export function buyIngredient(
  state: GameState,
  action: Extract<Action, { type: 'BuyIngredient' }>,
  content: ContentPack,
): ActionResult {
  const ing = content.byId.ingredients.get(action.ingredientId);
  if (!ing) return fail(`unknown ingredient "${action.ingredientId}"`);
  if (!isIngredientAvailable(ing.id, state, content))
    return fail(`"${ing.id}" is not yet available`);
  if (!Number.isInteger(action.units) || action.units < 1)
    return fail('units must be a positive integer');
  const cost = Math.round(action.units * ing.costPerUnitCents);
  if (cost > state.player.cashCents) return fail('not enough cash');
  adjust(state, 'cash', -cost, CAUSES.ingredients, ing.id);
  state.lab.inventory[ing.id] = roundQty((state.lab.inventory[ing.id] ?? 0) + action.units);
  return { ok: true };
}

export function createFormula(
  state: GameState,
  action: Extract<Action, { type: 'CreateFormula' }>,
  content: ContentPack,
): ActionResult {
  if (state.lab.formulas.length >= content.config.maxFormulas) return fail('formula book is full');
  const name = typeof action.name === 'string' ? action.name.trim() : '';
  if (name.length === 0 || name.length > MAX_FORMULA_NAME_LENGTH) {
    return fail(`name must be 1-${MAX_FORMULA_NAME_LENGTH} characters`);
  }
  if (!isValidPrice(action.priceCents)) return fail('invalid price');
  const error = validateFormula(action.components, action.processId, state, content);
  if (error) return fail(error);
  const id = `formula_${state.meta.nextId++}`;
  state.lab.formulas.push({
    id,
    name,
    components: action.components.map((c) => ({ ingredientId: c.ingredientId, amount: c.amount })),
    processId: action.processId,
    priceCents: action.priceCents,
    createdTurn: state.meta.turn,
  });
  state.lab.stock[id] = 0;
  return { ok: true, createdId: id };
}

export function setPrice(
  state: GameState,
  action: Extract<Action, { type: 'SetPrice' }>,
): ActionResult {
  const formula = state.lab.formulas.find((f) => f.id === action.formulaId);
  if (!formula) return fail(`unknown formula "${action.formulaId}"`);
  if (!isValidPrice(action.priceCents)) return fail('invalid price');
  formula.priceCents = action.priceCents;
  return { ok: true };
}

export const stillsFree = (state: GameState): number =>
  Math.max(
    0,
    (state.player.capabilities['stills'] ?? 0) -
      state.lab.batches.filter((b) => b.startedTurn === state.meta.turn).length,
  );

export function bottlingCostPerBottle(processId: string, content: ContentPack): number {
  return (
    content.config.bottleCostCents +
    (content.byId.processes.get(processId)?.extraCostPerBottleCents ?? 0)
  );
}

/** The largest batch the player could brew right now (ingredients, cash, and a free still). */
export function maxBrewable(state: GameState, formulaId: string, content: ContentPack): number {
  const formula = state.lab.formulas.find((f) => f.id === formulaId);
  if (!formula || stillsFree(state) === 0) return 0;
  let max = Number.POSITIVE_INFINITY;
  for (const c of formula.components) {
    max = Math.min(
      max,
      Math.floor(roundQty((state.lab.inventory[c.ingredientId] ?? 0) / c.amount)),
    );
  }
  const perBottle = bottlingCostPerBottle(formula.processId, content);
  if (perBottle > 0) max = Math.min(max, Math.floor(state.player.cashCents / perBottle));
  return Number.isFinite(max) ? Math.max(0, max) : 0;
}

export function brewBatch(
  state: GameState,
  action: Extract<Action, { type: 'BrewBatch' }>,
  content: ContentPack,
): ActionResult {
  const formula = state.lab.formulas.find((f) => f.id === action.formulaId);
  if (!formula) return fail(`unknown formula "${action.formulaId}"`);
  if (!Number.isInteger(action.bottles) || action.bottles < 1) {
    return fail('bottles must be a positive integer');
  }
  if (stillsFree(state) === 0) return fail('no free still this month');
  for (const c of formula.components) {
    const need = roundQty(c.amount * action.bottles);
    if ((state.lab.inventory[c.ingredientId] ?? 0) + 1e-9 < need) {
      return fail(`not enough "${c.ingredientId}" (need ${need} oz)`);
    }
  }
  const cost = action.bottles * bottlingCostPerBottle(formula.processId, content);
  if (cost > state.player.cashCents) return fail('not enough cash for bottles');

  for (const c of formula.components) {
    const left = roundQty((state.lab.inventory[c.ingredientId] ?? 0) - c.amount * action.bottles);
    state.lab.inventory[c.ingredientId] = Math.max(0, left);
  }
  adjust(state, 'cash', -cost, CAUSES.bottling, formula.id);
  const id = `batch_${state.meta.nextId++}`;
  state.lab.batches.push({
    id,
    formulaId: formula.id,
    bottles: action.bottles,
    startedTurn: state.meta.turn,
    readyTurn: state.meta.turn,
  });
  state.stats.batchesBrewed += 1;
  return { ok: true, createdId: id };
}

/** Turn phase: finished batches go onto the shelf. */
export const productionPhase: Phase = {
  id: 'production',
  run({ state }) {
    const remaining = [];
    for (const b of state.lab.batches) {
      if (b.readyTurn <= state.meta.turn) {
        state.lab.stock[b.formulaId] = (state.lab.stock[b.formulaId] ?? 0) + b.bottles;
      } else {
        remaining.push(b);
      }
    }
    state.lab.batches = remaining;
  },
};
