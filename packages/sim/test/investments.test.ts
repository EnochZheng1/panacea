import { describe, expect, it } from 'vitest';
import {
  applyAction,
  endTurn,
  evaluateRequirement,
  implicationChance,
  legalActions,
  nextUnlocks,
  observe,
  type GameState,
} from '../src/index.ts';
import { ingredientUnitCost } from '../src/systems/lab.ts';
import { act, content, months, setUpTonic, start } from './helpers.ts';

/** A shop with money, the unlocks every investment asks for, and the talk quiet. */
function richShop(seed = 'inv'): GameState {
  const s = start(seed);
  s.player.cashCents = 500_000;
  s.month.startCashCents = s.player.cashCents;
  for (const id of ['newspaper_ads', 'larger_shop', 'percolator']) s.unlocks.unlocked[id] = 0;
  s.stats.bottlesSold = 5000;
  return s;
}

const refusal = (s: GameState, investmentId: string) => {
  const r = applyAction(s, { type: 'Invest', investmentId }, content).result;
  return r.ok ? null : r.error;
};

describe('investments (balance pass 4)', () => {
  it('is offered only when its requirement holds, and only once while held', () => {
    const fresh = start('inv0');
    expect(refusal(fresh, 'branch_shop')).toBe('not yet offered');
    expect(refusal(fresh, 'no_such_thing')).toMatch(/unknown investment/);
    const s = richShop();
    expect(legalActions(s, content).find((a) => a.type === 'Invest')).toMatchObject({
      investmentIds: ['branch_shop', 'almanac', 'wholesale_contract', 'laboratory'],
    });
    const bought = act(s, { type: 'Invest', investmentId: 'branch_shop' });
    expect(refusal(bought, 'branch_shop')).toBe('already held');
    const poor = richShop();
    poor.player.cashCents = 100;
    expect(refusal(poor, 'branch_shop')).toBe('not enough cash');
  });

  it('a branch costs its price, adds room for regulars and callers, and charges its rent', () => {
    let s = richShop();
    const branch = content.byId.investments.get('branch_shop')!;
    const before = s.player.cashCents;
    const capacity = observe(s, content).customerCapacity;
    s = act(s, { type: 'Invest', investmentId: 'branch_shop' });
    expect(before - s.player.cashCents).toBe(branch.costCents);
    expect(observe(s, content).customerCapacity).toBe(capacity + 20);
    expect(s.month.news).toContainEqual({
      kind: 'investment',
      id: 'branch_shop',
      change: 'bought',
    });
    const { state, report } = endTurn(s, content);
    expect(report.deltas).toContainEqual({
      metric: 'cash',
      amount: -branch.upkeepCents,
      cause: 'expense.upkeep',
      detail: 'branch_shop',
    });
    expect(observe(state, content).investments.find((i) => i.id === 'branch_shop')).toMatchObject({
      held: true,
      monthsLeft: null,
      canBuy: false,
      refusal: 'already held',
    });
  });

  it('an almanac runs twelve months, brings callers, prints the claims, then lapses', () => {
    const setUp = setUpTonic(richShop('alm'), 400);
    const formulaId = setUp.formulaId;
    let s = setUp.state;
    s = act(
      s,
      { type: 'SetClaims', formulaId, claims: ['cures_everything'] },
      { type: 'BrewBatch', formulaId, bottles: 400 },
      { type: 'Invest', investmentId: 'almanac' },
    );
    const first = endTurn(s, content);
    expect(first.report.callers['almanac']).toBe(10);
    expect(first.report.deltas.some((d) => d.metric === 'heat' && d.detail === 'almanac')).toBe(
      true,
    );
    s = months(first.state, 10);
    expect(observe(s, content).investments.find((i) => i.id === 'almanac')?.monthsLeft).toBe(1);
    const last = endTurn(s, content);
    expect(last.report.news).toContainEqual({
      kind: 'investment',
      id: 'almanac',
      change: 'lapsed',
    });
    expect('almanac' in last.state.player.investments).toBe(false);
    expect(refusal(last.state, 'almanac')).toBeNull();
  });

  it('druggists out of town take their order at the wholesale price, and cancel when the talk turns', () => {
    const setUp = setUpTonic(richShop('whl'), 300);
    const formulaId = setUp.formulaId;
    let s = setUp.state;
    s = act(
      s,
      { type: 'SetPrice', formulaId, priceCents: 100 },
      { type: 'BrewBatch', formulaId, bottles: 300 },
      { type: 'Invest', investmentId: 'wholesale_contract' },
    );
    const inv = content.byId.investments.get('wholesale_contract')!;
    const w = inv.wholesale!;
    const order = Math.floor(w.bottlesPerMonth * (1 + s.player.reputation * w.perReputation));
    const price = Math.min(w.maxPriceCents, Math.round((100 * w.pricePct) / 100));
    const { state, report } = endTurn(s, content);
    expect(report.wholesale).toEqual({
      ordered: order,
      delivered: order,
      revenueCents: order * price,
    });
    expect(report.deltas).toContainEqual({
      metric: 'cash',
      amount: order * price,
      cause: 'sales.wholesale',
      detail: 'wholesale_contract',
    });
    const hot = structuredClone(state);
    hot.scrutiny.heat = 80;
    const cancelled = endTurn(hot, content);
    expect(cancelled.report.news).toContainEqual({
      kind: 'investment',
      id: 'wholesale_contract',
      change: 'lost',
    });
    expect(refusal(cancelled.state, 'wholesale_contract')).toBe('not yet offered');
  });

  it('a laboratory cheapens the narcotics, and nothing else', () => {
    const s = richShop('lab');
    const morphine = ingredientUnitCost(s, 'morphine', content);
    const alcohol = ingredientUnitCost(s, 'grain_alcohol', content);
    const after = act(s, { type: 'Invest', investmentId: 'laboratory' });
    const pct = after.player.capabilities['narcotic_discount_pct']!;
    expect(pct).toBeGreaterThan(0);
    expect(ingredientUnitCost(after, 'morphine', content)).toBeCloseTo(morphine * (1 - pct / 100));
    expect(ingredientUnitCost(after, 'grain_alcohol', content)).toBe(alcohol);
  });

  it('every month the books say where the callers came from', () => {
    const setUp = setUpTonic(start('callers'), 30);
    const formulaId = setUp.formulaId;
    let s = setUp.state;
    s = act(s, { type: 'BrewBatch', formulaId, bottles: 30 });
    const { report } = endTurn(s, content);
    expect(Object.keys(report.callers)).toEqual(['ward', 'newspaper', 'subscriptions']);
    expect(report.callers['newspaper']).toBe(0);
  });
});

describe('inquest replies (balance pass 4)', () => {
  it('testimony and silence differ by at least a tenth in every state', () => {
    const s = start('odds');
    for (const productImplicated of [true, false]) {
      for (let detectability = 0; detectability <= 20; detectability += 0.5) {
        for (const heat of [0, 10, 35, 50, 75, 100]) {
          for (const reputation of [0, 15, 60]) {
            for (const timesImplicated of [0, 4, 20]) {
              s.scrutiny.heat = heat;
              s.player.reputation = reputation;
              s.scrutiny.timesImplicated = timesImplicated;
              const record = {
                ...({} as GameState['deaths'][number]),
                productImplicated,
                detectability,
                boldness: 5,
                badBatch: false,
              };
              const t = implicationChance(s, record, 'testify');
              const q = implicationChance(s, record, 'silent');
              expect(Math.abs(t - q)).toBeGreaterThanOrEqual(0.1 - 1e-9);
              // the reply that suits the bottle's strength is the better one
              if (detectability <= 5) expect(t).toBeLessThan(q);
              else expect(q).toBeLessThan(t);
            }
          }
        }
      }
    }
  });
});

describe('names and progress (balance pass 4)', () => {
  it('no two customers in a game share a name, living or dead', () => {
    const setUp = setUpTonic(richShop('names'), 2000);
    const formulaId = setUp.formulaId;
    let s = setUp.state;
    s = act(s, { type: 'BrewBatch', formulaId, bottles: 2000 });
    s = months(s, 60);
    const used = s.meta.usedNames;
    expect(new Set(used).size).toBe(used.length);
    const living = s.customers.map((c) => c.name.split(' ').slice(1).join(' '));
    const dead = s.deaths
      .filter((d) => !d.anonymous)
      .map((d) => d.name.split(' ').slice(1).join(' '));
    for (const n of [...living, ...dead]) expect(used).toContain(n);
    expect(new Set(living).size).toBe(living.length);
  });

  it('the opening cast varies by seed, keeping each regular’s form of address', () => {
    const a = start('cast-a').customers.map((c) => c.name);
    const b = start('cast-b').customers.map((c) => c.name);
    expect(a).not.toEqual(b);
    for (const [i, r] of content.regulars.entries()) {
      expect(a[i]!.split(' ')[0]).toBe(r.displayName.split(' ')[0]);
    }
  });

  it('an unlock that needs everything is only as near as its least-met part', () => {
    const s = start('progress');
    s.stats.bottlesSold = 547;
    s.unlocks.unlocked['second_still'] = 0;
    const third = content.byId.unlocks.get('third_still')!;
    expect(evaluateRequirement(third.requirement, s, content).progress).toBeCloseTo(0.547);
    s.stats.batchesBrewed = 1;
    s.player.cashCents = 50_000;
    const qc = content.byId.unlocks.get('quality_control')!;
    expect(evaluateRequirement(qc.requirement, s, content).progress).toBeCloseTo(1 / 40);
    expect(nextUnlocks(s, content).find((u) => u.id === 'third_still')?.progress).toBeCloseTo(
      0.547,
    );
  });
});
