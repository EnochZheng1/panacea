import { PROPERTY_KEYS, type ContentPack, type Properties } from '@panacea/content';
import { compareDates } from '../core/date.ts';
import type { FormulaComponent, GameState } from '../core/state.ts';

export interface FormulaAnalysis {
  /** Fluid ounces per bottle. */
  volume: number;
  /** Per-bottle properties after processing. */
  properties: Properties;
  /** Ingredients + bottle + processing, per bottle, in (fractional) cents. */
  unitCostCents: number;
}

export const emptyProperties = (): Properties =>
  Object.fromEntries(PROPERTY_KEYS.map((k) => [k, 0])) as Properties;

/** Computes what a formula does. Assumes the formula is valid (see `validateFormula`). */
export function analyzeFormula(
  components: readonly FormulaComponent[],
  processId: string,
  content: ContentPack,
): FormulaAnalysis {
  const props = emptyProperties();
  let volume = 0;
  let cost = content.config.bottleCostCents;
  for (const c of components) {
    const ing = content.byId.ingredients.get(c.ingredientId);
    if (!ing) continue;
    volume += c.amount;
    cost += c.amount * ing.costPerUnitCents;
    for (const k of PROPERTY_KEYS) props[k] += c.amount * ing.properties[k];
  }
  const process = content.byId.processes.get(processId);
  if (process) {
    cost += process.extraCostPerBottleCents;
    for (const k of PROPERTY_KEYS) props[k] *= process.multipliers[k] ?? 1;
  }
  return { volume, properties: props, unitCostCents: cost };
}

export function isIngredientAvailable(
  ingredientId: string,
  state: GameState,
  content: ContentPack,
): boolean {
  const ing = content.byId.ingredients.get(ingredientId);
  if (!ing) return false;
  return !ing.availableFrom || compareDates(state.meta.date, ing.availableFrom) >= 0;
}

export function isProcessAvailable(processId: string, state: GameState, content: ContentPack) {
  const p = content.byId.processes.get(processId);
  if (!p) return false;
  return !p.requiresUnlock || p.requiresUnlock in state.unlocks.unlocked;
}

/** Returns an error message, or null if the formula is valid right now. */
export function validateFormula(
  components: readonly FormulaComponent[],
  processId: string,
  state: GameState,
  content: ContentPack,
): string | null {
  if (!Array.isArray(components) || components.length === 0) return 'formula has no ingredients';
  const seen = new Set<string>();
  let volume = 0;
  for (const c of components) {
    if (!content.byId.ingredients.has(c.ingredientId)) {
      return `unknown ingredient "${c.ingredientId}"`;
    }
    if (!isIngredientAvailable(c.ingredientId, state, content)) {
      return `ingredient "${c.ingredientId}" is not yet available`;
    }
    if (seen.has(c.ingredientId)) return `ingredient "${c.ingredientId}" listed twice`;
    seen.add(c.ingredientId);
    if (!Number.isFinite(c.amount) || c.amount <= 0) {
      return `amount for "${c.ingredientId}" must be positive`;
    }
    volume += c.amount;
  }
  if (volume > content.config.bottleCapacity + 1e-9) {
    return `formula volume ${volume} oz exceeds bottle capacity ${content.config.bottleCapacity} oz`;
  }
  if (!content.byId.processes.has(processId)) return `unknown process "${processId}"`;
  if (!isProcessAvailable(processId, state, content)) {
    return `process "${processId}" is not unlocked`;
  }
  return null;
}
