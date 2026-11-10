import {
  DRUG_CLASSES,
  PROPERTY_KEYS,
  type ContentPack,
  type DrugClass,
  type Properties,
} from '@panacea/content';
import { compareDates } from '../core/date.ts';
import type { FormulaComponent, GameState } from '../core/state.ts';

export interface FormulaAnalysis {
  /** Fluid ounces per bottle. */
  volume: number;
  /** Per-bottle properties after processing. */
  properties: Properties;
  /**
   * The same properties split by drug class ('none' = everything without a drug class), so the
   * body model can apply each class's tolerance separately. Sums to `properties`.
   */
  byClass: Record<DrugClass | 'none', Properties>;
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
  const byClass = Object.fromEntries(
    [...DRUG_CLASSES, 'none'].map((c) => [c, emptyProperties()]),
  ) as Record<DrugClass | 'none', Properties>;
  const process = content.byId.processes.get(processId);
  let volume = 0;
  let cost = content.config.bottleCostCents + (process?.extraCostPerBottleCents ?? 0);
  for (const c of components) {
    const ing = content.byId.ingredients.get(c.ingredientId);
    if (!ing) continue;
    volume += c.amount;
    cost += c.amount * ing.costPerUnitCents;
    const bucket = byClass[ing.drugClass ?? 'none'];
    for (const k of PROPERTY_KEYS) {
      const v = c.amount * ing.properties[k] * (process?.multipliers[k] ?? 1);
      props[k] += v;
      bucket[k] += v;
    }
  }
  return { volume, properties: props, byClass, unitCostCents: cost };
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

/** Total boldness of a formula's claims (drives scrutiny). */
export function claimBoldness(claims: readonly string[], content: ContentPack): number {
  return claims.reduce((sum, id) => sum + (content.byId.claims.get(id)?.boldness ?? 0), 0);
}

/** How strongly a formula's claims appeal to someone with this ailment. */
export function claimAppeal(
  claims: readonly string[],
  ailmentId: string,
  content: ContentPack,
): number {
  let total = 0;
  for (const id of claims) {
    const claim = content.byId.claims.get(id);
    if (!claim) continue;
    if (claim.ailments.length === 0 || claim.ailments.includes(ailmentId)) total += claim.appeal;
  }
  return total;
}

/**
 * How far a bottle fell short of its label for a buyer with this complaint, who felt `relief`
 * beyond the complaint itself (round-10 balance). Each claim that speaks to the complaint promised
 * relief: `market.promiseShare` × its appeal for an ordinary claim, which the bottle makes good if
 * it gives that much relief (the shortfall is at most the promise), and `market.cureShare` × its
 * appeal for a claim to cure a deadly disease (`Claim.cures`), which no bottle makes good: the
 * buyer still has the complaint. A cure is given time (a course of bottles): its part grows with
 * the months they have taken it, `months` / `market.curePatience`, to the whole after that.
 */
export function promiseShortfall(
  claims: readonly string[],
  ailmentId: string,
  relief: number,
  months: number,
  content: ContentPack,
): number {
  const { promiseShare, cureShare, curePatience } = content.config.market;
  let promised = 0;
  let cure = 0;
  for (const id of claims) {
    const claim = content.byId.claims.get(id);
    if (!claim) continue;
    if (claim.ailments.length > 0 && !claim.ailments.includes(ailmentId)) continue;
    if (claim.cures) cure += cureShare * claim.appeal;
    else promised += promiseShare * claim.appeal;
  }
  const patience = curePatience > 0 ? Math.min(1, Math.max(0, months) / curePatience) : 1;
  return Math.min(promised, Math.max(0, promised - relief)) + cure * patience;
}

/** A complaint that kills (its `mortalityMultiplier` is over 1): the consumption, a teething fever. */
const isDeadly = (ailmentId: string, content: ContentPack): boolean =>
  (content.byId.ailments.get(ailmentId)?.mortalityMultiplier ?? 1) > 1;

/**
 * Whether a label claims to cure this complaint outright, and the complaint is one that kills (a
 * `cures` claim that names a deadly complaint, or every complaint): a customer who dies of it is
 * a broken promise.
 */
export function promisesCure(
  claims: readonly string[],
  ailmentId: string | null,
  content: ContentPack,
): boolean {
  if (ailmentId === null || !isDeadly(ailmentId, content)) return false;
  for (const id of claims) {
    const claim = content.byId.claims.get(id);
    if (!claim?.cures) continue;
    if (claim.ailments.length === 0 || claim.ailments.includes(ailmentId)) return true;
  }
  return false;
}

/**
 * Claims on a label to cure a deadly disease: `cures` claims that name a complaint that kills, or
 * every complaint (the Board and the press read these: `advertising.curesHeat`).
 */
export const cureClaims = (claims: readonly string[], content: ContentPack): number =>
  claims.filter((id) => {
    const claim = content.byId.claims.get(id);
    if (!claim?.cures) return false;
    return claim.ailments.length === 0 || claim.ailments.some((a) => isDeadly(a, content));
  }).length;

/** Returns an error message, or null if this set of claims may be printed right now. */
export function validateClaims(
  claims: readonly string[],
  state: GameState,
  content: ContentPack,
): string | null {
  if (!Array.isArray(claims)) return 'claims must be a list';
  if (claims.length > content.config.maxClaimsPerFormula) {
    return `at most ${content.config.maxClaimsPerFormula} claims per label`;
  }
  if (new Set(claims).size !== claims.length) return 'claims must not repeat';
  for (const id of claims) {
    const claim = content.byId.claims.get(id);
    if (!claim) return `unknown claim "${id}"`;
    if (claim.requiresUnlock && !(claim.requiresUnlock in state.unlocks.unlocked)) {
      return `claim "${id}" is not unlocked`;
    }
  }
  return null;
}
