import type { BuyerProfile, ContentPack, DrugClass, Orders } from '@panacea/content';
import { CAUSES, type Cause } from '../core/causes.ts';
import { exp, log } from '../core/dmath.ts';
import { adjust } from '../core/ledger.ts';
import { DRUG_CLASSES } from '@panacea/content';
import type {
  Bought,
  Customer,
  Formula,
  GameState,
  Phase,
  SaleRecord,
  SaleSource,
  TurnContext,
  WholesaleMonth,
} from '../core/index.ts';
import { pickAilment, pickSegment, customerCapacity, generateCustomer } from './customers.ts';
import {
  analyzeFormula,
  claimAppeal,
  claimBoldness,
  cureClaims,
  promiseShortfall,
  type FormulaAnalysis,
} from './formula.ts';
import {
  heldInvestments,
  investmentWalkIns,
  wholesaleOrder,
  wholesalePrice,
} from './investments.ts';
import { stockOf, takeBottle, totalStock } from './lab.ts';
import { acuteDanger, appeal, overdoseChance, reliefScore, zeroTolerance } from './pharmacology.ts';
import { recordDeath } from './scrutiny.ts';
import { memoryLoss } from './ward.ts';

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
  /** Wary of narcotics (see `refusesNarcotic`). */
  wary: boolean;
}

/**
 * Whether a buyer wary of narcotics will not take this bottle (round-9 balance): its opiates and
 * cocaine make a habit of `waryHabit` or more. The mother who has read of soothing syrups knows
 * the poppy when she smells it; a trace in a tonic of spirits and herbs passes.
 */
export const refusesNarcotic = (buyer: Buyer, a: FormulaAnalysis, content: ContentPack): boolean =>
  buyer.wary && narcoticHabit(a) >= content.config.market.waryHabit;

/**
 * Whether the careful households trust this bottle: no opiate or coca in it at all, and a modest
 * label (its claims' boldness at most `waryBoldness`). A label that cures consumption and
 * everything else is the quack's, whatever is in the bottle.
 */
export const trustedByWary = (o: Offer, content: ContentPack): boolean =>
  narcoticHabit(o.analysis) <= 0 &&
  claimBoldness(o.formula.claims, content) <= content.config.market.waryBoldness;

/**
 * What a bottle's label is worth to this buyer: its claims that speak to their complaint, and, to
 * a buyer wary of narcotics, `cleanAppeal` more for a bottle they trust (see `trustedByWary`): it
 * is its own recommendation.
 */
export function labelAppeal(o: Offer, buyer: Buyer, content: ContentPack): number {
  const claims = claimAppeal(o.formula.claims, buyer.ailment, content);
  return buyer.wary && trustedByWary(o, content)
    ? claims + content.config.market.cleanAppeal
    : claims;
}

/**
 * Callers sent by the careful households (`callers.careful`): while the shelf holds a bottle they
 * trust, every wary regular who is pleased with it tells the neighbours, `carefulPerWary` callers
 * a month each (rounded down), all of them wary.
 */
export function carefulCallers(
  state: GameState,
  offers: readonly Offer[],
  content: ContentPack,
): number {
  const per = content.config.walkIns.carefulPerWary;
  if (per <= 0 || !offers.some((o) => trustedByWary(o, content))) return 0;
  let pleased = 0;
  for (const c of state.customers) if (c.wary && c.satisfaction > 0) pleased += 1;
  return Math.floor(per * pleased);
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
    if (refusesNarcotic(buyer, o.analysis, content)) continue;
    const a = appeal(
      o.analysis,
      buyer.tolerance,
      buyer.profile,
      buyer.dependency,
      labelAppeal(o, buyer, content),
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
    if (stockOf(state, o.formula.id) <= 0 || refusesNarcotic(buyer, o.analysis, content)) continue;
    const a = appeal(
      o.analysis,
      buyer.tolerance,
      buyer.profile,
      buyer.dependency,
      labelAppeal(o, buyer, content),
    );
    if (a > 0) return true;
  }
  return false;
}

/**
 * Whether a wary buyer who left empty-handed did so for the narcotic in the bottle: some bottle on
 * the shelf they would otherwise have looked at, they would not take. (Call it only when
 * `chooseOffer` with no price cap found nothing and `turnedAwayByPrice` is false.)
 */
export function refusedForNarcotic(
  state: GameState,
  offers: readonly Offer[],
  buyer: Buyer,
  content: ContentPack,
): boolean {
  if (!buyer.wary) return false;
  for (const o of offers) {
    if (stockOf(state, o.formula.id) <= 0 || !refusesNarcotic(buyer, o.analysis, content)) continue;
    const a = appeal(
      o.analysis,
      buyer.tolerance,
      buyer.profile,
      buyer.dependency,
      labelAppeal(o, buyer, content),
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
 * Whether the ward has households of this kind left to win (balance pass 8). The ward is so many
 * households of each kind, in the proportions of the segments' weights: of every twenty-five, four
 * are clerks' and one is well-to-do. A shop can be the regular shop of at most
 * `wardShareSlack` × its capacity × that kind's share of them: a ward has only so many clerks, and
 * fewer carriage folk, so a bottle priced for them alone cannot fill a shop with them.
 */
export function wardHasRoomFor(
  state: GameState,
  content: ContentPack,
  segmentId: string,
  capacity: number,
): boolean {
  const slack = content.config.walkIns.wardShareSlack;
  if (slack <= 0) return true;
  let total = 0;
  for (const seg of content.segments) total += seg.weight;
  const share = (content.byId.segments.get(segmentId)?.weight ?? 0) / total;
  let have = 0;
  for (const c of state.customers) if (c.segment === segmentId) have += 1;
  return have < slack * capacity * share;
}

/** The habit a bottle's narcotics make: its opiates and its cocaine, not its spirits. */
export const narcoticHabit = (a: FormulaAnalysis): number =>
  a.byClass.opiate.habit + a.byClass.cocaine.habit;

/**
 * The chance a regular buys this month at the shelf price rather than make the last bottle do
 * (balance pass 8). Up to what they can comfortably spend on a bottle, `dearAbove` × their usual
 * price, they always do; above it the chance falls as (comfortable / price) ^ `dearElasticity`, so
 * past a little over the ward's usual price every cent asked costs more than a cent of custom.
 * Spirits are sold in every saloon; a narcotic is not: a bottle whose opiates or cocaine make a
 * habit of `dearHabitHold` or more is found the money for at up to twice the comfortable price.
 */
export function dearness(c: Customer, offer: Offer, content: ContentPack): number {
  const { dearAbove, dearElasticity, dearHabitHold } = content.config.market;
  const hold = Math.min(1, narcoticHabit(offer.analysis) / dearHabitHold);
  const comfortable = c.profile.baseWillingnessCents * dearAbove * (1 + hold);
  const price = offer.formula.priceCents;
  if (price <= comfortable || dearElasticity <= 0) return 1;
  return exp(dearElasticity * log(comfortable / price));
}

/**
 * What a regular can afford to spend in a month, in cents. A habit eats into it: a craving
 * regular loses work and wages to the bottle, so the more dependent they are, the less they have.
 */
export const meansCents = (c: Customer, content: ContentPack): number =>
  c.profile.baseWillingnessCents *
  content.config.market.meansPerMonth *
  (1 - content.config.market.meansLostToHabit * c.body.dependency);

/**
 * A regular who owes more than `ruinMonths` months' means: one bottle at most, and a family that
 * complains. A habit can run up a long slate before it comes to that.
 */
export const isRuined = (c: Customer, content: ContentPack): boolean =>
  c.debtCents > content.config.market.ruinMonths * meansCents(c, content);

/**
 * The bottles a regular wants this month: one, and as many more as their attachment to the shop
 * calls for. Satisfaction alone buys a bottle for the cupboard; a craving buys more, and more again
 * as the dose stops working (tolerance), up to `habitBottlesMax` extra.
 */
export function bottlesWanted(c: Customer, content: ContentPack): number {
  if (isRuined(c, content)) return 1;
  const m = content.config.market;
  const dep = c.body.dependency;
  const tolerance = DRUG_CLASSES.reduce((sum, cls) => sum + c.body.tolerance[cls], 0);
  const craving =
    dep *
    (m.bottlesPerAttachment + m.bottlesPerTolerance * Math.max(0, tolerance - m.toleranceFloor));
  const liking = Math.max(dep, c.satisfaction) * m.bottlesPerAttachment;
  return 1 + Math.floor(Math.min(m.habitBottlesMax, Math.max(liking, craving)));
}

/**
 * What a regular who craves the bottle pays over its shelf price: a habit pays what it is asked,
 * and something over to have a bottle kept back. Only for a bottle strong enough to make a habit
 * (habit past `premiumHabitFloor`, the whole premium at `premiumHabitSpan` beyond it), and only
 * while the regular still has the money (they owe less than `premiumDebtMonths` months' means). Up to
 * `cravingPremium` × dependency of the price.
 */
export function cravingPremiumCents(c: Customer, offer: Offer, content: ContentPack): number {
  const m = content.config.market;
  if (c.body.dependency <= content.config.body.withdrawalThreshold) return 0;
  if (c.debtCents > m.premiumDebtMonths * meansCents(c, content)) return 0;
  const habit = offer.analysis.properties.habit;
  const strength = Math.min(1, Math.max(0, (habit - m.premiumHabitFloor) / m.premiumHabitSpan));
  return Math.round(offer.formula.priceCents * m.cravingPremium * c.body.dependency * strength);
}

/** Adds one bottle sold to the month's sales of its formula, under where it went. */
export function recordSale(
  state: GameState,
  formulaId: string,
  priceCents: number,
  source: SaleSource,
  premiumCents = 0,
): void {
  priceCents += premiumCents;
  state.stats.bottlesSold += 1;
  state.stats.revenueCents += priceCents;
  let sale = state.month.sales.find((s) => s.formulaId === formulaId);
  if (!sale) {
    sale = {
      formulaId,
      bottles: 0,
      revenueCents: 0,
      shopBottles: 0,
      shopRevenueCents: 0,
      walkInBottles: 0,
      walkInRevenueCents: 0,
      wholesaleBottles: 0,
      wholesaleRevenueCents: 0,
      postBottles: 0,
      postRevenueCents: 0,
      cravingPremiumCents: 0,
    };
    state.month.sales.push(sale);
  }
  sale.bottles += 1;
  sale.revenueCents += priceCents;
  const bottles = `${source}Bottles` as const;
  const revenue = `${source}RevenueCents` as const;
  (sale as SaleRecord)[bottles] += 1;
  (sale as SaleRecord)[revenue] += priceCents;
  sale.cravingPremiumCents += premiumCents;
}

/**
 * Callers drawn by word of a strong bottle (`callers.word`): the most habit-forming bottle on the
 * shelf is talked about, and strangers come asking for the stuff that works.
 */
export function wordOfStrength(offers: readonly Offer[], content: ContentPack): number {
  const { wordPerHabit, wordHabitFloor, wordHabitCap } = content.config.walkIns;
  const habit = Math.max(0, ...offers.map((o) => o.analysis.properties.habit));
  return Math.floor(wordPerHabit * Math.min(wordHabitCap, Math.max(0, habit - wordHabitFloor)));
}

const customerAsBuyer = (c: Customer): Buyer => ({
  profile: c.profile,
  ailment: c.ailment,
  tolerance: c.body.tolerance,
  dependency: c.body.dependency,
  wary: c.wary,
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
    // A label that cures consumption is read at the Board of Health and in Park Row (round 10).
    const { curesHeat } = content.config.advertising;
    const cures = Math.max(0, ...onSale.map((f) => cureClaims(f.claims, content)));
    if (cures > 0 && curesHeat > 0) adjust(state, 'heat', cures * curesHeat, CAUSES.cureClaims);
    // An almanac prints every claim in the formula book, for a year, on every kitchen table.
    const printedBoldness = Math.max(
      0,
      ...state.lab.formulas.map((f) => claimBoldness(f.claims, content)),
    );
    for (const inv of heldInvestments(state, content)) {
      if (inv.heatPerBoldness > 0 && printedBoldness > 0) {
        adjust(state, 'heat', printedBoldness * inv.heatPerBoldness, CAUSES.claims, inv.id);
      }
    }

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

    const book = (cause: Cause, formulaId: string, cents: number) => {
      const key = `${cause}|${formulaId}`;
      const r = revenue.get(key) ?? { cause, formulaId, cents: 0 };
      r.cents += cents;
      revenue.set(key, r);
    };
    const sell = (offer: Offer, cause: Cause, premium = 0) => {
      const lot = takeBottle(state, offer.formula.id);
      if (!lot) return undefined;
      const price = offer.formula.priceCents;
      const source = cause === CAUSES.salesShop ? 'shop' : 'walkIn';
      recordSale(state, offer.formula.id, price, source, premium);
      book(cause, offer.formula.id, price);
      if (premium > 0) book(CAUSES.salesCraving, offer.formula.id, premium);
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
        const desired = bottlesWanted(c, content);
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
            if (k === 0) countTurnedAway(state, offers, customerAsBuyer(c), content);
            break;
          }
          // A dear bottle: the regular asks the price and makes the last one do this month (they
          // are counted with the callers who asked the price and went away).
          if (k === 0) {
            const keep = dearness(c, offer, content);
            if (keep < 1 && !rng.chance(keep)) {
              state.month.turnedAwayByPrice += 1;
              break;
            }
          }
          const premium = cravingPremiumCents(c, offer, content);
          const b = sell(offer, CAUSES.salesShop, premium);
          if (!b) break;
          bought.push(b);
          budget -= offer.formula.priceCents + premium;
          spent += offer.formula.priceCents + premium;
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

    // Orders from away are filled next, from what the regulars left, up to a share of it: strangers
    // at the counter get the rest, so a contract held on a thin shelf costs the shop new custom.
    ordersFromAway(ctx);

    // Walk-ins.
    walkIns(ctx, offers, sell);

    for (const r of revenue.values()) adjust(state, 'cash', r.cents, r.cause, r.formulaId);
  },
};

const CALLERS_PREFIX = 'callers_';

/**
 * Callers a letter's arrangement sends to the counter: a capability `callers_<source>` (the
 * wheelmen's club, the parish bazaar) brings that many strangers a month, counted under that
 * source, in the ordinal order of the capability ids. They come on someone's word: the rector's,
 * the club captain's, the doctor's. A ward that remembers harm and a shop the Board suspects make
 * that word harder to give, so the callers are scaled by both (`letterTrust`) and rounded.
 */
export function letterCallers(state: GameState, content: ContentPack): [string, number][] {
  const caps = state.player.capabilities;
  const trust = letterTrust(state, content);
  const out: [string, number][] = [];
  for (const id of Object.keys(caps).sort()) {
    if (!id.startsWith(CALLERS_PREFIX)) continue;
    const n = caps[id] ?? 0;
    const sent = Math.floor(n * trust + 0.5);
    if (sent > 0) out.push([id.slice(CALLERS_PREFIX.length), sent]);
  }
  return out;
}

/**
 * How far the ward takes the shop at a letter's word, 0..1: how readily a correspondent sends
 * people to it, and how much standing a kindness earns. Lessened by the ward's memory of harm
 * (`ward.letterCallerLoss` at a memory of 100) and by suspicion (`scrutiny.letterCallerLoss` at
 * the heat that shuts the shop; at 1.5, nobody vouches for a shop two-thirds of the way there).
 */
export function letterTrust(state: GameState, content: ContentPack): number {
  const { shutdownHeat, letterCallerLoss } = content.config.scrutiny;
  const suspicion = Math.min(1, Math.max(0, state.scrutiny.heat / shutdownHeat));
  return (
    memoryLoss(state, content.config.ward.letterCallerLoss) *
    Math.max(0, 1 - letterCallerLoss * suspicion)
  );
}

/** Counts a caller who found nothing to buy: put off by the price, or (wary) by the narcotic. */
function countTurnedAway(
  state: GameState,
  offers: readonly Offer[],
  buyer: Buyer,
  content: ContentPack,
): void {
  if (turnedAwayByPrice(state, offers, buyer, content)) state.month.turnedAwayByPrice += 1;
  else if (refusedForNarcotic(state, offers, buyer, content)) state.month.refusedNarcotic += 1;
}

function walkIns(
  { state, content, rng, scratch }: TurnContext,
  offers: Offer[],
  sell: (offer: Offer, cause: Cause) => Bought | undefined,
) {
  if (offers.length === 0) return;
  const { base, perReputation, perNotoriety, notorietyCap, conversionChance } =
    content.config.walkIns;
  const ward = content.config.ward;
  const caps = state.player.capabilities;
  const callers = state.month.callers;
  // the ward's callers: the good name brings them, and a name the papers print brings the curious
  callers['ward'] = rng.int(
    0,
    base +
      Math.floor(
        state.player.reputation * perReputation +
          Math.min(state.player.notoriety, notorietyCap) * perNotoriety,
      ),
  );
  callers['newspaper'] = scratch.adVisitors;
  callers['subscriptions'] = Math.max(0, caps['walk_in_bonus'] ?? 0);
  callers['word'] = wordOfStrength(offers, content);
  for (const [id, n] of investmentWalkIns(state, content)) callers[id] = n;
  for (const [id, n] of letterCallers(state, content)) callers[id] = n;
  // the careful households' callers come last, and are all wary
  const careful = carefulCallers(state, offers, content);
  if (careful > 0) callers['careful'] = careful;
  const visitors = Object.values(callers).reduce((a, b) => a + b, 0);
  const waryHouseholds = content.segments.map((s) => ({ item: s, weight: s.weight * s.wary }));
  // A rival's sale: a share of the callers whose bottle costs more than his go to him instead.
  const rivalShare = Math.min(100, caps['rival_share_pct'] ?? 0) / 100;
  const rivalPrice = caps['rival_price_cents'] ?? 0;
  const capacity = customerCapacity(state, content);
  for (let i = 0; i < visitors; i++) {
    const sentByCareful = i >= visitors - careful;
    const segment = sentByCareful
      ? (rng.weighted(waryHouseholds) ?? pickSegment(content, rng))
      : pickSegment(content, rng);
    const ailmentId = pickAilment(segment, rng);
    const buyer: Buyer = {
      profile: segment.profile,
      ailment: ailmentId,
      tolerance: zeroTolerance(),
      dependency: 0,
      wary: sentByCareful || (segment.wary > 0 && rng.chance(segment.wary)),
    };
    const offer = chooseOffer(state, offers, buyer, content);
    if (!offer) {
      countTurnedAway(state, offers, buyer, content);
      continue;
    }
    if (rivalShare > 0 && offer.formula.priceCents > rivalPrice && rng.chance(rivalShare)) {
      state.month.turnedAwayByPrice += 1;
      continue;
    }
    const claims = labelAppeal(offer, buyer, content);
    // A stranger hesitates over a bottle priced near the most they would pay.
    const wtp = willingnessToPay(
      buyer,
      appeal(offer.analysis, buyer.tolerance, buyer.profile, 0, claims),
      content,
    );
    const hesitation = priceHesitation(offer.formula.priceCents, wtp, content);
    // A stranger who has heard what the ward says of the bottle thinks twice.
    const pBuy =
      Math.min(0.95, segment.profile.buyChance * (1 + 0.1 * claims)) *
      hesitation *
      memoryLoss(state, ward.walkInLoss);
    if (!rng.chance(pBuy)) continue;
    const bought = sell(offer, CAUSES.salesWalkIn);
    if (!bought) continue;

    // A walk-in who felt relief may come back as a regular, unless the label promised a cure the
    // bottle did not make good (round 10).
    const ailment = content.byId.ailments.get(ailmentId);
    const felt =
      reliefScore(offer.analysis, buyer.tolerance, buyer.profile) - (ailment?.severity ?? 0);
    const relief =
      felt -
      content.config.market.disappointment *
        promiseShortfall(offer.formula.claims, ailmentId, felt, 0, content);
    const reputationFactor = 0.5 + state.player.reputation / 50;
    const pConvert =
      conversionChance *
      (1 + 0.2 * claims) *
      reputationFactor *
      memoryLoss(state, ward.conversionLoss);
    if (
      relief > 0 &&
      state.customers.length < capacity &&
      wardHasRoomFor(state, content, segment.id, capacity) &&
      rng.chance(Math.min(0.9, pConvert))
    ) {
      const customer = generateCustomer(state, content, rng, segment, ailmentId, buyer.wary);
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

/** The bottle orders from away are filled with: the one the shop has most of on the shelf. */
export function orderFormula(state: GameState): Formula | undefined {
  let formula: Formula | undefined;
  let most = 0;
  for (const f of state.lab.formulas) {
    const n = stockOf(state, f.id);
    if (n > most) {
      formula = f;
      most = n;
    }
  }
  return formula;
}

/**
 * Orders from away, filled after the regulars have had theirs and before the walk-ins, from at
 * most `ordersShelfShare` of what the regulars left: druggists out of town under contract, at the
 * wholesale price, and readers who send for a bottle by post, at the shelf price. All take the
 * bottle the shop has most of, and order fewer the dearer it is. They sell to (or are) strangers
 * with no tolerance: a strong enough bottle kills some of them, and the talk comes home.
 */
function ordersFromAway(ctx: TurnContext): void {
  const { state, content } = ctx;
  const allowance = { left: Math.ceil(totalStock(state) * content.config.market.ordersShelfShare) };
  for (const inv of heldInvestments(state, content)) {
    if (inv.wholesale) {
      deliver(ctx, inv.id, inv.wholesale, state.month.wholesale, CAUSES.salesWholesale, allowance);
    }
    if (inv.postOrders) {
      deliver(ctx, inv.id, inv.postOrders, state.month.post, CAUSES.salesPost, allowance);
    }
  }
}

function deliver(
  { state, content, rng }: TurnContext,
  investmentId: string,
  spec: Orders,
  month: WholesaleMonth,
  cause: Cause,
  allowance: { left: number },
): void {
  const formula = orderFormula(state);
  const ordered = wholesaleOrder(state, spec, formula?.priceCents ?? null);
  month.ordered += ordered;
  if (!formula || ordered <= 0) return;
  const price = wholesalePrice(spec, formula.priceCents);
  const risk = new Map<string, { p: number; detectability: number }>();
  let revenue = 0;
  for (let i = 0; i < ordered; i++) {
    if (allowance.left <= 0) break;
    const lot = takeBottle(state, formula.id);
    if (!lot) break;
    allowance.left -= 1;
    month.delivered += 1;
    revenue += price;
    recordSale(state, formula.id, price, cause === CAUSES.salesPost ? 'post' : 'wholesale');
    // A stranger of ordinary health, with no tolerance.
    let r = risk.get(lot.id);
    if (!r) {
      const a = analyzeFormula(lot.components, lot.processId, content);
      const dose = { potency: lot.potency, doseScale: 1, vulnerability: 1 };
      const danger = acuteDanger(a, zeroTolerance(), dose, content);
      r = {
        p: overdoseChance(danger, spec.strangerHealth),
        detectability: a.properties.detectability,
      };
      risk.set(lot.id, r);
    }
    if (r.p > 0 && rng.chance(r.p)) {
      const segment = pickSegment(content, rng);
      recordDeath(state, content, rng, {
        customerId: null,
        name: '',
        segment: segment.id,
        consumer: 'self',
        anonymous: true,
        ailment: null,
        cause: 'overdose',
        productImplicated: true,
        formulaId: null,
        badBatch: lot.badBatch,
        detectability: r.detectability,
      });
    }
  }
  month.revenueCents += revenue;
  if (revenue > 0) adjust(state, 'cash', revenue, cause, investmentId);
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
