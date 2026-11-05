import type { ContentPack, Investment, Orders } from '@panacea/content';
import type { Action, ActionResult } from '../core/actions.ts';
import { CAUSES } from '../core/causes.ts';
import { adjust } from '../core/ledger.ts';
import type { GameState, InvestmentChange, Phase } from '../core/index.ts';
import { applyEffects } from './effects.ts';
import {
  evaluateRequirement,
  requirementProgress,
  type ConditionProgress,
} from './requirements.ts';

/**
 * Investments: things the proprietor buys that pay back over months or years. A branch shop
 * in another ward (more regulars, more callers, its own rent), an almanac (a year of callers,
 * and the label's claims in print), a wholesale contract (druggists out of town who buy every
 * month, and cancel when the talk turns ugly), a laboratory (narcotics at half price, wages, and
 * the odd fire). What each pays depends on the bottle and how the shop is run.
 */

export const isHeld = (state: GameState, id: string): boolean => id in state.player.investments;

/** Why an investment cannot be bought right now, or null if it can. */
export function investRefusal(
  state: GameState,
  inv: Investment,
  content: ContentPack,
): string | null {
  if (isHeld(state, inv.id)) return 'already held';
  if (inv.requires && !evaluateRequirement(inv.requires, state, content).met) {
    return 'not yet offered';
  }
  if (state.player.cashCents < inv.costCents) return 'not enough cash';
  return null;
}

/**
 * What a venture not held waits for, as numbers: every condition of its requirement (met or not),
 * then its price against the cash in hand (`kind: 'cost'`).
 */
export function investmentProgress(
  state: GameState,
  inv: Investment,
  content: ContentPack,
): ConditionProgress[] {
  const progress = inv.requires ? requirementProgress(inv.requires, state, content) : [];
  progress.push({
    kind: 'cost',
    id: null,
    sense: 'atLeast',
    have: state.player.cashCents,
    need: inv.costCents,
    met: state.player.cashCents >= inv.costCents,
    via: null,
  });
  return progress;
}

export function invest(
  state: GameState,
  action: Extract<Action, { type: 'Invest' }>,
  content: ContentPack,
): ActionResult {
  const inv = content.byId.investments.get(action.investmentId);
  if (!inv) return { ok: false, error: `unknown investment "${action.investmentId}"` };
  const refusal = investRefusal(state, inv, content);
  if (refusal) return { ok: false, error: refusal };
  adjust(state, 'cash', -inv.costCents, CAUSES.investment, inv.id);
  state.player.investments[inv.id] = {
    boughtTurn: state.meta.turn,
    endsTurn: inv.months === undefined ? null : state.meta.turn + inv.months,
  };
  applyEffects(state, inv.effects, CAUSES.investment, inv.id);
  state.month.news.push({ kind: 'investment', id: inv.id, change: 'bought' });
  return { ok: true };
}

/** Gives an investment up: its capability changes are undone. */
function endInvestment(state: GameState, inv: Investment, change: InvestmentChange): void {
  for (const e of inv.effects) {
    if (e.type === 'capability') {
      state.player.capabilities[e.id] = (state.player.capabilities[e.id] ?? 0) - e.delta;
    }
  }
  delete state.player.investments[inv.id];
  state.month.news.push({ kind: 'investment', id: inv.id, change });
}

/** The investments the shop holds, in content order (so every engine walks them alike). */
export const heldInvestments = (state: GameState, content: ContentPack): Investment[] =>
  content.investments.filter((inv) => isHeld(state, inv.id));

/** Extra callers a month from what the shop holds, by investment id. */
export function investmentWalkIns(state: GameState, content: ContentPack): [string, number][] {
  return heldInvestments(state, content)
    .filter((inv) => inv.walkInsPerMonth > 0)
    .map((inv) => [inv.id, inv.walkInsPerMonth]);
}

/**
 * How a shelf price changes an order from away: 1 at the reference price, more for a cheaper
 * bottle (at most `maxOrderBoost`), fewer for a dearer one, and none once it is far too dear.
 */
export function orderPriceFactor(spec: Orders, priceCents: number | null): number {
  if (priceCents === null || spec.priceElasticity <= 0) return 1;
  const factor =
    1 + (spec.priceElasticity * (spec.refPriceCents - priceCents)) / spec.refPriceCents;
  return Math.max(0, Math.min(spec.maxOrderBoost, factor));
}

/**
 * The bottles an order from away asks for this month: more, the better the proprietor's name,
 * and fewer, the dearer the bottle they would be sent (`priceCents`; null when the shelf is bare,
 * which leaves the order as it stands at the reference price).
 */
export const wholesaleOrder = (state: GameState, spec: Orders, priceCents: number | null): number =>
  Math.floor(
    spec.bottlesPerMonth *
      (1 + state.player.reputation * spec.perReputation) *
      orderPriceFactor(spec, priceCents),
  );

/** What buyers from away pay for a bottle sold on the shelf at `priceCents`. */
export const wholesalePrice = (spec: Orders, priceCents: number): number =>
  Math.min(spec.maxPriceCents, Math.round((priceCents * spec.pricePct) / 100));

/**
 * Turn phase, after expenses: upkeep is paid, accidents happen, contracts are cancelled when
 * their condition holds, and investments that ran their months lapse at the end of the last one.
 */
export const investmentsPhase: Phase = {
  id: 'investments',
  run({ state, content, rng }) {
    for (const inv of heldInvestments(state, content)) {
      const holding = state.player.investments[inv.id];
      if (!holding) continue;
      if (inv.upkeepCents > 0) adjust(state, 'cash', -inv.upkeepCents, CAUSES.upkeep, inv.id);
      if (inv.mishap && rng.chance(inv.mishap.chancePerMonth)) {
        applyEffects(state, inv.mishap.effects, CAUSES.mishap, inv.id);
        state.month.news.push({ kind: 'investment', id: inv.id, change: 'mishap' });
      }
      if (inv.lostWhen && evaluateRequirement(inv.lostWhen, state, content).met) {
        endInvestment(state, inv, 'lost');
      } else if (holding.endsTurn !== null && holding.endsTurn <= state.meta.turn + 1) {
        endInvestment(state, inv, 'lapsed');
      }
    }
  },
};
