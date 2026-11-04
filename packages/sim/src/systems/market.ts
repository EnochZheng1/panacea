import type { BuyerProfile, ContentPack, DrugClass } from '@panacea/content';
import { CAUSES, type Cause } from '../core/causes.ts';
import { adjust } from '../core/ledger.ts';
import type { Bought, Customer, Formula, GameState, Phase, TurnContext } from '../core/index.ts';
import { pickAilment, pickSegment, customerCapacity, generateCustomer } from './customers.ts';
import { analyzeFormula, claimAppeal, claimBoldness, type FormulaAnalysis } from './formula.ts';
import { stockOf, takeBottle } from './lab.ts';
import { appeal, reliefScore, zeroTolerance } from './pharmacology.ts';

/**
 * The shop's market (M1: one neighbourhood).
 *
 * Regulars come back each month; dependent regulars buy more bottles and tolerate higher
 * prices. Walk-ins arrive from reputation and advertising, buy at most once, and sometimes
 * become regulars. M2 generalises this to regional cohorts.
 */

export interface Offer {
  formula: Formula;
  analysis: FormulaAnalysis;
}

/** What is on the shelf. Buyers judge each formula by the lot they would be sold (the oldest). */
export function currentOffers(state: GameState, content: ContentPack): Offer[] {
  const offers: Offer[] = [];
  for (const f of state.lab.formulas) {
    const lot = state.lab.stock.find((l) => l.formulaId === f.id && l.bottles > 0);
    if (lot)
      offers.push({ formula: f, analysis: analyzeFormula(lot.components, lot.processId, content) });
  }
  return offers;
}

export interface Buyer {
  profile: BuyerProfile;
  ailment: string;
  tolerance: Record<DrugClass, number>;
  dependency: number;
}

/**
 * The most a buyer would pay for a bottle of this appeal. A stronger-feeling bottle commands a
 * premium (up to `willingnessMax` × their usual price), and dependency raises it further.
 */
export function willingnessToPay(buyer: Buyer, appealScore: number, content: ContentPack): number {
  const m = content.config.market;
  return (
    buyer.profile.baseWillingnessCents *
    Math.min(
      m.willingnessMax,
      Math.max(0, m.willingnessBase + appealScore / m.appealPerWillingness),
    ) *
    (1 + buyer.dependency)
  );
}

/** The best value-for-money bottle this buyer would accept, if any. */
export function chooseOffer(
  state: GameState,
  offers: readonly Offer[],
  buyer: Buyer,
  content: ContentPack,
  maxPriceCents = Number.POSITIVE_INFINITY,
): Offer | undefined {
  let best: Offer | undefined;
  let bestScore = Number.NEGATIVE_INFINITY;
  for (const o of offers) {
    if (o.formula.priceCents > maxPriceCents || stockOf(state, o.formula.id) <= 0) continue;
    const a = appeal(
      o.analysis,
      buyer.tolerance,
      buyer.profile,
      buyer.dependency,
      claimAppeal(o.formula.claims, buyer.ailment, content),
    );
    if (a <= 0 || o.formula.priceCents > willingnessToPay(buyer, a, content)) continue;
    const score = a / Math.max(1, o.formula.priceCents);
    if (score > bestScore) {
      best = o;
      bestScore = score;
    }
  }
  return best;
}

/**
 * Whether a buyer who left empty-handed was put off by the price: some bottle on the shelf
 * appealed to them, but every one that did cost more than they would pay. (Call it only when
 * `chooseOffer` with no price cap found nothing.)
 */
export function turnedAwayByPrice(
  state: GameState,
  offers: readonly Offer[],
  buyer: Buyer,
  content: ContentPack,
): boolean {
  for (const o of offers) {
    if (stockOf(state, o.formula.id) <= 0) continue;
    const a = appeal(
      o.analysis,
      buyer.tolerance,
      buyer.profile,
      buyer.dependency,
      claimAppeal(o.formula.claims, buyer.ailment, content),
    );
    if (a > 0) return true;
  }
  return false;
}

/**
 * How much a price near a stranger's limit puts them off: 1 when the bottle costs at most
 * (1 − hesitationMargin) of what they would pay, falling to 1 − priceHesitation at their limit.
 */
export function priceHesitation(
  priceCents: number,
  wtpCents: number,
  content: ContentPack,
): number {
  const { priceHesitation: h, hesitationMargin } = content.config.market;
  if (h <= 0 || wtpCents <= 0) return 1;
  const headroom = 1 - priceCents / wtpCents;
  return 1 - h * Math.max(0, Math.min(1, 1 - headroom / hesitationMargin));
}

/**
 * What a regular can afford to spend in a month, in cents. A habit eats into it: a craving
 * regular loses work and wages to the bottle, so the more dependent they are, the less they have.
 */
export const meansCents = (c: Customer, content: ContentPack): number =>
  c.profile.baseWillingnessCents *
  content.config.market.meansPerMonth *
  (1 - content.config.market.meansLostToHabit * c.body.dependency);

/** A regular who owes more than a month's means: one bottle at most, and a family that complains. */
export const isRuined = (c: Customer, content: ContentPack): boolean =>
  c.debtCents > meansCents(c, content);

const customerAsBuyer = (c: Customer): Buyer => ({
  profile: c.profile,
  ailment: c.ailment,
  tolerance: c.body.tolerance,
  dependency: c.body.dependency,
});

/** Turn phase: claims on labels draw notice; newspaper advertising amplifies them. */
export const advertisingPhase: Phase = {
  id: 'advertising',
  run({ state, content, scratch }) {
    const { labelHeatPerBoldness } = content.config.advertising;
    const onSale = state.lab.formulas.filter((f) => stockOf(state, f.id) > 0);
    const labelBoldness = Math.max(0, ...onSale.map((f) => claimBoldness(f.claims, content)));
    if (labelBoldness > 0)
      adjust(state, 'heat', labelBoldness * labelHeatPerBoldness, CAUSES.claims);

    const spend = state.marketing.adSpendCents;
    if (spend <= 0) return;
    if (spend > state.player.cashCents) {
      state.marketing.adSpendCents = 0;
      state.month.news.push({ kind: 'ads_lapsed' });
      return;
    }
    adjust(state, 'cash', -spend, CAUSES.advertising);
    const { visitorsPerSqrtCent, heatPerBoldness } = content.config.advertising;
    scratch.adVisitors = Math.floor(visitorsPerSqrtCent * Math.sqrt(spend));
    const boldness = Math.max(
      0,
      ...state.lab.formulas.map((f) => claimBoldness(f.claims, content)),
    );
    if (boldness > 0) adjust(state, 'heat', boldness * heatPerBoldness, CAUSES.claims);
  },
};

/** Turn phase: regulars and walk-ins buy. */
export const salesPhase: Phase = {
  id: 'sales',
  run(ctx) {
    const { state, content, rng, scratch } = ctx;
    const offers = currentOffers(state, content);
    const revenue = new Map<string, { cause: Cause; formulaId: string; cents: number }>();

    const sell = (offer: Offer, cause: Cause) => {
      const lot = takeBottle(state, offer.formula.id);
      if (!lot) return undefined;
      const price = offer.formula.priceCents;
      state.stats.bottlesSold += 1;
      state.stats.revenueCents += price;
      const key = `${cause}|${offer.formula.id}`;
      const r = revenue.get(key) ?? { cause, formulaId: offer.formula.id, cents: 0 };
      r.cents += price;
      revenue.set(key, r);
      const sale = state.month.sales.find((s) => s.formulaId === offer.formula.id);
      if (sale) {
        sale.bottles += 1;
        sale.revenueCents += price;
      } else {
        state.month.sales.push({ formulaId: offer.formula.id, bottles: 1, revenueCents: price });
      }
      const bought: Bought = {
        formulaId: offer.formula.id,
        lotId: lot.id,
        components: lot.components,
        processId: lot.processId,
        potency: lot.potency,
        badBatch: lot.badBatch,
      };
      return bought;
    };

    // Regulars, in a fixed order for determinism. A regular's attachment to the shop is the
    // stronger of habit and plain satisfaction: a delighted customer buys a second bottle for the
    // cupboard just as a dependent one does, though only the dependent pays beyond their means.
    // Every regular who comes in will buy one bottle they think worth its price; the budget
    // limits the extra ones. A habit pays what it is asked until the money runs out: spending
    // beyond their means runs up a debt, and a regular who is ruined buys one bottle at most.
    const market = content.config.market;
    for (const c of state.customers) {
      const bought: ReturnType<typeof sell>[] = [];
      const dep = c.body.dependency;
      const pBuy = Math.min(
        0.98,
        Math.max(0.05, c.profile.buyChance + 0.3 * c.satisfaction + 0.5 * dep),
      );
      let spent = 0;
      if (offers.length > 0 && rng.chance(pBuy)) {
        const attachment = Math.max(dep, c.satisfaction);
        const desired = isRuined(c, content)
          ? 1
          : 1 + Math.floor(attachment * market.bottlesPerAttachment);
        let budget =
          c.profile.baseWillingnessCents *
          (market.budgetBase + market.budgetPerAttachment * attachment) *
          (1 + dep);
        for (let k = 0; k < desired; k++) {
          const offer = chooseOffer(
            state,
            offers,
            customerAsBuyer(c),
            content,
            k === 0 ? Number.POSITIVE_INFINITY : budget,
          );
          if (!offer) {
            if (k === 0 && turnedAwayByPrice(state, offers, customerAsBuyer(c), content)) {
              state.month.turnedAwayByPrice += 1;
            }
            break;
          }
          const b = sell(offer, CAUSES.salesShop);
          if (!b) break;
          bought.push(b);
          budget -= offer.formula.priceCents;
          spent += offer.formula.priceCents;
        }
      }
      c.debtCents = Math.max(0, c.debtCents + spent - meansCents(c, content));
      const valid = bought.filter((b) => b !== undefined);
      scratch.purchases.set(c.id, valid);
      c.bottlesLastMonth = valid.length;
      if (valid.length > 0) {
        c.purchases += valid.length;
        c.lastPurchaseTurn = state.meta.turn;
        c.lastFormulaId = valid[valid.length - 1]?.formulaId ?? c.lastFormulaId;
      }
    }

    // Walk-ins.
    walkIns(ctx, offers, sell);

    for (const r of revenue.values()) adjust(state, 'cash', r.cents, r.cause, r.formulaId);
  },
};

function walkIns(
  { state, content, rng, scratch }: TurnContext,
  offers: Offer[],
  sell: (offer: Offer, cause: Cause) => Bought | undefined,
) {
  if (offers.length === 0) return;
  const { base, perReputation, conversionChance } = content.config.walkIns;
  const caps = state.player.capabilities;
  const visitors =
    rng.int(0, base + Math.floor(state.player.reputation * perReputation)) +
    scratch.adVisitors +
    Math.max(0, caps['walk_in_bonus'] ?? 0);
  // A rival's sale: a share of the callers whose bottle costs more than his go to him instead.
  const rivalShare = Math.min(100, caps['rival_share_pct'] ?? 0) / 100;
  const rivalPrice = caps['rival_price_cents'] ?? 0;
  const capacity = customerCapacity(state, content);
  for (let i = 0; i < visitors; i++) {
    const segment = pickSegment(content, rng);
    const ailmentId = pickAilment(segment, rng);
    const buyer: Buyer = {
      profile: segment.profile,
      ailment: ailmentId,
      tolerance: zeroTolerance(),
      dependency: 0,
    };
    const offer = chooseOffer(state, offers, buyer, content);
    if (!offer) {
      if (turnedAwayByPrice(state, offers, buyer, content)) state.month.turnedAwayByPrice += 1;
      continue;
    }
    if (rivalShare > 0 && offer.formula.priceCents > rivalPrice && rng.chance(rivalShare)) {
      state.month.turnedAwayByPrice += 1;
      continue;
    }
    const claims = claimAppeal(offer.formula.claims, ailmentId, content);
    // A stranger hesitates over a bottle priced near the most they would pay.
    const wtp = willingnessToPay(
      buyer,
      appeal(offer.analysis, buyer.tolerance, buyer.profile, 0, claims),
      content,
    );
    const hesitation = priceHesitation(offer.formula.priceCents, wtp, content);
    const pBuy = Math.min(0.95, segment.profile.buyChance * (1 + 0.1 * claims)) * hesitation;
    if (!rng.chance(pBuy)) continue;
    const bought = sell(offer, CAUSES.salesWalkIn);
    if (!bought) continue;

    // A walk-in who felt relief may come back as a regular.
    const ailment = content.byId.ailments.get(ailmentId);
    const relief =
      reliefScore(offer.analysis, buyer.tolerance, buyer.profile) - (ailment?.severity ?? 0);
    const reputationFactor = 0.5 + state.player.reputation / 50;
    const pConvert = conversionChance * (1 + 0.2 * claims) * reputationFactor;
    if (relief > 0 && state.customers.length < capacity && rng.chance(Math.min(0.9, pConvert))) {
      const customer = generateCustomer(state, content, rng, segment, ailmentId);
      state.customers.push(customer);
      state.stats.customersJoined += 1;
      scratch.purchases.set(customer.id, [bought]);
      customer.purchases = 1;
      customer.bottlesLastMonth = 1;
      customer.lastPurchaseTurn = state.meta.turn;
      customer.lastFormulaId = bought.formulaId;
      state.month.news.push({ kind: 'customer_joined', customerId: customer.id });
    } else {
      scratch.walkIns.push({ segmentId: segment.id, ailmentId, bought });
    }
  }
}

/** Turn phase: rent, and any retainers the shop has agreed to pay. */
export const expensesPhase: Phase = {
  id: 'expenses',
  run({ state, content }) {
    const rent = content.config.rentCents + (state.player.capabilities['rent_extra_cents'] ?? 0);
    adjust(state, 'cash', -rent, CAUSES.rent);
    const retainer = state.player.capabilities['retainer_cents'] ?? 0;
    if (retainer > 0) adjust(state, 'cash', -retainer, CAUSES.retainer);
  },
};
