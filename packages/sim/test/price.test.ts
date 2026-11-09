import { describe, expect, it } from 'vitest';
import type { ContentPack } from '@panacea/content';
import {
  analyzeFormula,
  checkInvariants,
  newScratch,
  Rng,
  type FormulaComponent,
  type GameState,
} from '../src/index.ts';
import { productionPhase } from '../src/systems/lab.ts';
import {
  dearness,
  narcoticHabit,
  salesPhase,
  wardHasRoomFor,
  type Offer,
} from '../src/systems/market.ts';
import { act, content, roomFor, start } from './helpers.ts';

/** Balance pass 8: what the ward can pay, and how many of each kind it has. */

const SPIRITS: FormulaComponent[] = [
  { ingredientId: 'grain_alcohol', amount: 3 },
  { ingredientId: 'valerian', amount: 1 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 3 },
];
const LAUDANUM: FormulaComponent[] = [
  { ingredientId: 'grain_alcohol', amount: 2 },
  { ingredientId: 'laudanum', amount: 1 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 4 },
];

function stocked(
  components: FormulaComponent[],
  priceCents: number,
  bottles: number,
  seed: string,
) {
  let s = start(seed);
  s.player.cashCents = 1_000_000;
  s.month.startCashCents = s.player.cashCents;
  s = act(s, { type: 'CreateFormula', name: 'T', components, processId: 'simple_mix', priceCents });
  for (const c of components) {
    s = act(s, {
      type: 'BuyIngredient',
      ingredientId: c.ingredientId,
      units: Math.ceil(c.amount * bottles),
    });
  }
  roomFor(s, bottles);
  s = act(s, { type: 'BrewBatch', formulaId: s.lab.formulas[0]!.id, bottles });
  productionPhase.run({ state: s, content, rng: Rng.fromSeed(seed), scratch: newScratch() });
  return s;
}

const offerAt = (components: FormulaComponent[], priceCents: number): Offer => {
  const formula = stocked(components, priceCents, 1, 'offer').lab.formulas[0]!;
  return { formula, analysis: analyzeFormula(components, 'simple_mix', content) };
};

const withMarket = (over: Partial<ContentPack['config']['market']>): ContentPack => ({
  ...content,
  config: { ...content.config, market: { ...content.config.market, ...over } },
});

describe('what the ward can pay (balance pass 8)', () => {
  const regular = () =>
    structuredClone(start('dear').customers.find((c) => c.segment === 'labourer')!);
  const { dearAbove, dearElasticity, dearHabitHold } = content.config.market;

  it('a regular buys every month up to a little over their usual price, and less often above it', () => {
    const c = regular();
    const comfortable = c.profile.baseWillingnessCents * dearAbove;
    expect(dearness(c, offerAt(SPIRITS, Math.floor(comfortable)), content)).toBe(1);
    const dear = dearness(c, offerAt(SPIRITS, Math.round(comfortable * 1.5)), content);
    expect(dear).toBeCloseTo(
      Math.pow(comfortable / Math.round(comfortable * 1.5), dearElasticity),
      9,
    );
    expect(dear).toBeLessThan(1);
    // and still less at twice the price: past the comfortable price, revenue falls as the price rises
    const dearer = dearness(c, offerAt(SPIRITS, Math.round(comfortable * 2)), content);
    expect(dearer).toBeLessThan(dear);
    expect(dearer * Math.round(comfortable * 2)).toBeLessThan(dear * Math.round(comfortable * 1.5));
  });

  it('a taste for spirits does not pay a dear price; a narcotic habit does', () => {
    const c = regular();
    c.body.dependency = 1;
    const comfortable = c.profile.baseWillingnessCents * dearAbove;
    const price = Math.round(comfortable * 1.6);
    expect(narcoticHabit(offerAt(SPIRITS, price).analysis)).toBe(0);
    expect(dearness(c, offerAt(SPIRITS, price), content)).toBeLessThan(1);
    expect(narcoticHabit(offerAt(LAUDANUM, price).analysis)).toBeGreaterThanOrEqual(dearHabitHold);
    expect(dearness(c, offerAt(LAUDANUM, price), content)).toBe(1);
  });

  it('regulars buy fewer dear bottles, and are counted among those who asked the price and went away', () => {
    const sold = (market: ContentPack) => {
      // the regulars alone: no strangers at the counter
      const pack = {
        ...market,
        config: { ...market.config, walkIns: { ...market.config.walkIns, base: 0 } },
      };
      const s = stocked(SPIRITS, 140, 400, 'shelf');
      s.player.reputation = 0;
      s.month.startReputation = 0;
      // they like it and would pay it: only the price holds them back
      for (const c of s.customers) {
        c.body.dependency = 1;
        c.satisfaction = 1;
      }
      salesPhase.run({
        state: s,
        content: pack,
        rng: Rng.fromSeed('shelf'),
        scratch: newScratch(),
      });
      expect(checkInvariants(s, pack)).toEqual([]);
      return { bottles: s.month.sales[0]!.shopBottles, away: s.month.turnedAwayByPrice };
    };
    const dear = sold(content);
    const flat = sold(withMarket({ dearElasticity: 0 }));
    expect(dear.bottles).toBeLessThan(flat.bottles);
    expect(dear.away).toBeGreaterThan(flat.away);
  });
});

describe('the ward has so many of each kind (balance pass 8)', () => {
  const filled = (kind: string, n: number): GameState => {
    const s = start('kinds');
    const c0 = s.customers[0]!;
    s.customers = Array.from({ length: n }, (_, i) => ({
      ...structuredClone(c0),
      id: `c${i}`,
      segment: kind,
    }));
    return s;
  };
  const share = (kind: string) =>
    content.byId.segments.get(kind)!.weight / content.segments.reduce((a, x) => a + x.weight, 0);

  it('a shop wins at most its slack × capacity × the kind’s share of any one kind', () => {
    const capacity = 70;
    const limit = content.config.walkIns.wardShareSlack * capacity * share('clerk');
    expect(limit).toBeGreaterThan(20);
    expect(limit).toBeLessThan(capacity / 2);
    expect(wardHasRoomFor(filled('clerk', Math.ceil(limit) - 1), content, 'clerk', capacity)).toBe(
      true,
    );
    expect(wardHasRoomFor(filled('clerk', Math.ceil(limit)), content, 'clerk', capacity)).toBe(
      false,
    );
    // a shop full of clerks still has room for a labourer, and with no slack there is no limit
    expect(wardHasRoomFor(filled('clerk', 60), content, 'labourer', capacity)).toBe(true);
    const unlimited = {
      ...content,
      config: { ...content.config, walkIns: { ...content.config.walkIns, wardShareSlack: 0 } },
    };
    expect(wardHasRoomFor(filled('clerk', 60), unlimited, 'clerk', capacity)).toBe(true);
  });

  it('a well-to-do clientele is the smallest', () => {
    const capacity = 70;
    const { wardShareSlack } = content.config.walkIns;
    expect(wardShareSlack * capacity * share('well_to_do')).toBeLessThan(10);
  });
});
