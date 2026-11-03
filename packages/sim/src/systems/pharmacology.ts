import {
  DRUG_CLASSES,
  type BuyerProfile,
  type ContentPack,
  type DrugClass,
} from '@panacea/content';
import type { Body, Customer } from '../core/state.ts';
import type { FormulaAnalysis } from './formula.ts';

/**
 * How a bottle acts on a particular body. Tolerance blunts both the felt effect and (partly) the
 * danger of each drug class separately; non-drug ingredients act the same on everyone.
 */

export const zeroTolerance = (): Record<DrugClass, number> =>
  Object.fromEntries(DRUG_CLASSES.map((c) => [c, 0])) as Record<DrugClass, number>;

/** Felt kick and calm from one bottle, after tolerance. */
export function feltEffect(a: FormulaAnalysis, tolerance: Record<DrugClass, number>) {
  let kick = a.byClass.none.kick;
  let calm = a.byClass.none.calm;
  for (const c of DRUG_CLASSES) {
    const factor = 1 / (1 + tolerance[c]);
    kick += a.byClass[c].kick * factor;
    calm += a.byClass[c].calm * factor;
  }
  return { kick, calm };
}

/** How much relief a buyer feels from one bottle (before comparing with their ailment). */
export function reliefScore(
  a: FormulaAnalysis,
  tolerance: Record<DrugClass, number>,
  profile: BuyerProfile,
): number {
  const felt = feltEffect(a, tolerance);
  return profile.preference.kick * felt.kick + profile.preference.calm * felt.calm;
}

/** How attractive a bottle looks to a buyer, before price. */
export function appeal(
  a: FormulaAnalysis,
  tolerance: Record<DrugClass, number>,
  profile: BuyerProfile,
  dependency: number,
  claimAppeal: number,
): number {
  const bitterPenalty =
    profile.bitternessAversion * Math.max(0, a.properties.bitterness - profile.bitternessTolerance);
  const craving = dependency * Math.min(5, a.properties.habit) * 0.5;
  return reliefScore(a, tolerance, profile) - bitterPenalty + craving + 0.5 * claimAppeal;
}

export interface DoseContext {
  potency: number;
  /** Fraction of a bottle's dose actually given (infants get spoonfuls). */
  doseScale: number;
  vulnerability: number;
}

/** Acute danger of one bottle for this body. Compared against a health-based threshold. */
export function acuteDanger(
  a: FormulaAnalysis,
  tolerance: Record<DrugClass, number>,
  dose: DoseContext,
  content: ContentPack,
): number {
  const protection = content.config.body.toleranceProtection;
  let danger = a.byClass.none.acuteToxicity;
  for (const c of DRUG_CLASSES) {
    danger += a.byClass[c].acuteToxicity / (1 + protection * tolerance[c]);
  }
  return Math.max(0, danger) * dose.potency * dose.doseScale * dose.vulnerability;
}

export const dangerThreshold = (health: number): number => Math.max(1, health / 10);

/** Probability that one bottle kills outright, given its danger and the drinker's health. */
export function overdoseChance(danger: number, health: number): number {
  const risk = danger / dangerThreshold(health);
  const x = Math.min(1, Math.max(0, (risk - 0.7) / 1.3));
  return x * x;
}

export function doseContextFor(
  customer: Pick<Customer, 'consumer' | 'segment'>,
  potency: number,
  content: ContentPack,
): DoseContext {
  const seg = content.byId.segments.get(customer.segment);
  const infant = customer.consumer === 'infant';
  return {
    potency,
    doseScale: infant ? content.config.body.infantDoseScale : 1,
    vulnerability:
      (seg?.vulnerability ?? 1) * (infant ? content.config.body.infantVulnerability : 1),
  };
}

export const newBody = (baseHealth: number): Body => ({
  health: baseHealth,
  baseHealth,
  tolerance: zeroTolerance(),
  dependency: 0,
  load: 0,
});
