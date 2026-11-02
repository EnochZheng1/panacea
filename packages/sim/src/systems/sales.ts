import type { BuyerProfile, Properties } from '@panacea/content';
import { CAUSES, type Cause } from '../core/causes.ts';
import { adjust } from '../core/ledger.ts';
import type { Phase } from '../core/index.ts';
import { analyzeFormula } from './formula.ts';

/**
 * PLACEHOLDER SALES MODEL (M0).
 *
 * Shop regulars and walk-ins each consider the bottles on the shelf and buy at most one per
 * month, judging by felt effect, bitterness and price. M1 replaces this with the body model
 * (tolerance, dependency, toxic load) and M2 adds the aggregate regional market.
 */

interface Offer {
  formulaId: string;
  priceCents: number;
  properties: Properties;
}

export function appeal(props: Properties, profile: BuyerProfile): number {
  return (
    profile.preference.kick * props.kick +
    profile.preference.calm * props.calm -
    profile.bitternessAversion * Math.max(0, props.bitterness - profile.bitternessTolerance)
  );
}

export function willingnessToPay(props: Properties, profile: BuyerProfile): number {
  const a = appeal(props, profile);
  return profile.baseWillingnessCents * Math.min(2, Math.max(0, 0.5 + a / 10));
}

/** The best value-for-money bottle this buyer would accept, if any. */
export function chooseOffer(
  offers: readonly Offer[],
  stock: Record<string, number>,
  profile: BuyerProfile,
): Offer | undefined {
  let best: Offer | undefined;
  let bestScore = Number.NEGATIVE_INFINITY;
  for (const o of offers) {
    if ((stock[o.formulaId] ?? 0) <= 0) continue;
    const a = appeal(o.properties, profile);
    if (a <= 0 || o.priceCents > willingnessToPay(o.properties, profile)) continue;
    const score = a / Math.max(1, o.priceCents);
    if (score > bestScore) {
      best = o;
      bestScore = score;
    }
  }
  return best;
}

export const salesPhase: Phase = {
  id: 'sales',
  run({ state, content, rng }) {
    const offers: Offer[] = state.lab.formulas
      .filter((f) => (state.lab.stock[f.id] ?? 0) > 0)
      .map((f) => ({
        formulaId: f.id,
        priceCents: f.priceCents,
        properties: analyzeFormula(f.components, f.processId, content).properties,
      }));
    if (offers.length === 0) return;

    const revenue = new Map<string, { cause: Cause; formulaId: string; cents: number }>();
    const sell = (offer: Offer, cause: Cause) => {
      state.lab.stock[offer.formulaId] = (state.lab.stock[offer.formulaId] ?? 0) - 1;
      state.stats.bottlesSold += 1;
      state.stats.revenueCents += offer.priceCents;
      const key = `${cause}|${offer.formulaId}`;
      const r = revenue.get(key) ?? { cause, formulaId: offer.formulaId, cents: 0 };
      r.cents += offer.priceCents;
      revenue.set(key, r);
      const sale = state.month.sales.find((s) => s.formulaId === offer.formulaId);
      if (sale) {
        sale.bottles += 1;
        sale.revenueCents += offer.priceCents;
      } else {
        state.month.sales.push({
          formulaId: offer.formulaId,
          bottles: 1,
          revenueCents: offer.priceCents,
        });
      }
    };

    // Regulars, in a fixed order for determinism.
    for (const def of content.regulars) {
      const regular = state.regulars.find((r) => r.id === def.id);
      if (!regular || !rng.chance(def.profile.buyChance)) continue;
      const offer = chooseOffer(offers, state.lab.stock, def.profile);
      if (!offer) continue;
      sell(offer, CAUSES.salesShop);
      regular.purchases += 1;
      regular.lastPurchaseTurn = state.meta.turn;
    }

    // Walk-ins.
    const { walkIns } = content.config;
    const visitors = rng.int(
      0,
      walkIns.base + Math.floor(state.player.reputation * walkIns.perReputation),
    );
    for (let i = 0; i < visitors; i++) {
      if (!rng.chance(walkIns.profile.buyChance)) continue;
      const offer = chooseOffer(offers, state.lab.stock, walkIns.profile);
      if (offer) sell(offer, CAUSES.salesWalkIn);
    }

    for (const r of revenue.values()) adjust(state, 'cash', r.cents, r.cause, r.formulaId);
  },
};

export const expensesPhase: Phase = {
  id: 'expenses',
  run({ state, content }) {
    adjust(state, 'cash', -content.config.rentCents, CAUSES.rent);
  },
};
