import type { ContentPack } from '@panacea/content';
import {
  MAX_AD_SPEND_CENTS,
  MAX_FORMULA_NAME_LENGTH,
  MAX_PRICE_CENTS,
  type Action,
  type ActionResult,
} from '../core/actions.ts';
import { CAUSES } from '../core/causes.ts';
import { adjust } from '../core/ledger.ts';
import { gaussian } from '../core/rng.ts';
import type { FormulaComponent, GameState, Lot, Phase } from '../core/index.ts';
import { isIngredientAvailable, validateClaims, validateFormula } from './formula.ts';

/** Inventory quantities are rounded to avoid floating-point dust accumulating over a run. */
export const roundQty = (x: number): number => Math.round(x * 1e6) / 1e6;

const fail = (error: string): ActionResult => ({ ok: false, error });

const isValidPrice = (p: number) => Number.isInteger(p) && p >= 0 && p <= MAX_PRICE_CENTS;

/** Bottles of a formula on the shelf. */
export const stockOf = (state: GameState, formulaId: string): number =>
  state.lab.stock.reduce((n, lot) => (lot.formulaId === formulaId ? n + lot.bottles : n), 0);

export const totalStock = (state: GameState): number =>
  state.lab.stock.reduce((n, lot) => n + lot.bottles, 0);

/** Takes one bottle of a formula from its oldest lot. */
export function takeBottle(state: GameState, formulaId: string): Lot | undefined {
  const index = state.lab.stock.findIndex((l) => l.formulaId === formulaId && l.bottles > 0);
  const lot = state.lab.stock[index];
  if (!lot) return undefined;
  lot.bottles -= 1;
  const taken = { ...lot };
  if (lot.bottles === 0) state.lab.stock.splice(index, 1);
  return taken;
}

export function ingredientUnitCost(state: GameState, ingredientId: string, content: ContentPack) {
  const ing = content.byId.ingredients.get(ingredientId);
  if (!ing) return Number.POSITIVE_INFINITY;
  const discount = Math.min(90, state.player.capabilities['ingredient_discount_pct'] ?? 0);
  // a supplier's rise (a shortage, a trust) for one ingredient
  const surcharge = Math.max(0, state.player.capabilities[`surcharge_pct_${ing.id}`] ?? 0);
  // a laboratory works up opium, morphine and coca from the crude drug
  const narcotic =
    ing.drugClass === 'opiate' || ing.drugClass === 'cocaine'
      ? Math.min(90, Math.max(0, state.player.capabilities['narcotic_discount_pct'] ?? 0))
      : 0;
  return ing.costPerUnitCents * (1 - discount / 100) * (1 + surcharge / 100) * (1 - narcotic / 100);
}

export interface StockValue {
  /** Bottles on the shelf or brewing: their ingredients and bottling. */
  shelfCents: number;
  /** Ingredients on hand. */
  ingredientsCents: number;
  totalCents: number;
}

/**
 * What the shop's stock is worth at today's cost (with any discount or surcharge): the
 * ingredients on hand, and every bottle brewing or on the shelf at what its ingredients and
 * bottling would cost now. Money spent on stock is not lost, and the books can say so.
 */
export function stockValue(state: GameState, content: ContentPack): StockValue {
  let ingredients = 0;
  for (const [id, qty] of Object.entries(state.lab.inventory)) {
    if (qty > 0) ingredients += qty * ingredientUnitCost(state, id, content);
  }
  const bottleCost = (components: readonly FormulaComponent[], processId: string) => {
    let cost = bottlingCostPerBottle(processId, content);
    for (const c of components)
      cost += c.amount * ingredientUnitCost(state, c.ingredientId, content);
    return cost;
  };
  let shelf = 0;
  for (const b of state.lab.batches) shelf += b.bottles * bottleCost(b.components, b.processId);
  for (const lot of state.lab.stock)
    shelf += lot.bottles * bottleCost(lot.components, lot.processId);
  const shelfCents = Math.round(shelf);
  const ingredientsCents = Math.round(ingredients);
  return { shelfCents, ingredientsCents, totalCents: shelfCents + ingredientsCents };
}

export function buyIngredient(
  state: GameState,
  action: Extract<Action, { type: 'BuyIngredient' }>,
  content: ContentPack,
): ActionResult {
  const ing = content.byId.ingredients.get(action.ingredientId);
  if (!ing) return fail(`unknown ingredient "${action.ingredientId}"`);
  if (!isIngredientAvailable(ing.id, state, content)) {
    return fail(`"${ing.id}" is not yet available`);
  }
  if (!Number.isInteger(action.units) || action.units < 1) {
    return fail('units must be a positive integer');
  }
  const cost = Math.round(action.units * ingredientUnitCost(state, ing.id, content));
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
  const claims = action.claims ?? [];
  const claimError = validateClaims(claims, state, content);
  if (claimError) return fail(claimError);
  const id = `formula_${state.meta.nextId++}`;
  state.lab.formulas.push({
    id,
    name,
    components: action.components.map((c) => ({ ingredientId: c.ingredientId, amount: c.amount })),
    processId: action.processId,
    priceCents: action.priceCents,
    claims: [...claims],
    createdTurn: state.meta.turn,
  });
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

export function reformulate(
  state: GameState,
  action: Extract<Action, { type: 'Reformulate' }>,
  content: ContentPack,
): ActionResult {
  const formula = state.lab.formulas.find((f) => f.id === action.formulaId);
  if (!formula) return fail(`unknown formula "${action.formulaId}"`);
  const error = validateFormula(action.components, action.processId, state, content);
  if (error) return fail(error);
  formula.components = action.components.map((c) => ({
    ingredientId: c.ingredientId,
    amount: c.amount,
  }));
  formula.processId = action.processId;
  return { ok: true };
}

export function setClaims(
  state: GameState,
  action: Extract<Action, { type: 'SetClaims' }>,
  content: ContentPack,
): ActionResult {
  const formula = state.lab.formulas.find((f) => f.id === action.formulaId);
  if (!formula) return fail(`unknown formula "${action.formulaId}"`);
  const error = validateClaims(action.claims, state, content);
  if (error) return fail(error);
  formula.claims = [...action.claims];
  return { ok: true };
}

export function setAdSpend(
  state: GameState,
  action: Extract<Action, { type: 'SetAdSpend' }>,
): ActionResult {
  if (!('newspaper_ads' in state.unlocks.unlocked))
    return fail('newspaper advertising is not unlocked');
  if (!Number.isInteger(action.cents) || action.cents < 0 || action.cents > MAX_AD_SPEND_CENTS) {
    return fail('invalid advertising budget');
  }
  state.marketing.adSpendCents = action.cents;
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
    components: structuredClone(formula.components),
    processId: formula.processId,
    bottles: action.bottles,
    startedTurn: state.meta.turn,
    readyTurn: state.meta.turn,
  });
  state.stats.batchesBrewed += 1;
  return { ok: true, createdId: id };
}

/**
 * Turn phase: finished batches go onto the shelf as lots. Every batch's true strength varies a
 * little; occasionally one is badly mis-measured. With quality control, bad batches are caught
 * and poured away.
 */
export const productionPhase: Phase = {
  id: 'production',
  run({ state, content, rng }) {
    const { potencySpread, badBatchChance, badBatchPotency } = content.config.batches;
    const qc = (state.player.capabilities['quality_control'] ?? 0) > 0;
    const remaining = [];
    for (const b of state.lab.batches) {
      if (b.readyTurn > state.meta.turn) {
        remaining.push(b);
        continue;
      }
      const spread = qc ? potencySpread / 2 : potencySpread;
      let potency = Math.min(1.5, Math.max(0.5, 1 + spread * gaussian(rng)));
      let badBatch = false;
      if (rng.chance(badBatchChance)) {
        badBatch = true;
        potency = badBatchPotency[0] + rng.float() * (badBatchPotency[1] - badBatchPotency[0]);
      }
      if (badBatch && qc) {
        state.month.news.push({
          kind: 'bad_batch_caught',
          formulaId: b.formulaId,
          bottles: b.bottles,
        });
        continue;
      }
      state.lab.stock.push({
        id: b.id,
        formulaId: b.formulaId,
        components: b.components,
        processId: b.processId,
        bottles: b.bottles,
        potency,
        badBatch,
      });
    }
    state.lab.batches = remaining;
  },
};
