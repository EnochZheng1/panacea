import { describe, expect, it } from 'vitest';
import {
  applyAction,
  bribeCostCents,
  endTurn,
  formulaUnitCost,
  legalActions,
  observe,
  type GameState,
} from '../src/index.ts';
import { act, content, setUpTonic, start, TONIC } from './helpers.ts';

const COPPER = content.config.stillBottles;

/** A shop with cash to spare and a tonic in the formula book, with stock for `bottles`. */
function shop(seed: string, bottles: number) {
  const s = start(seed);
  s.player.cashCents = 500_000;
  s.month.startCashCents = s.player.cashCents;
  const created = applyAction(
    s,
    {
      type: 'CreateFormula',
      name: 'T',
      components: TONIC,
      processId: 'simple_mix',
      priceCents: 60,
    },
    content,
  );
  let state = created.state;
  for (const c of TONIC) {
    state = act(state, {
      type: 'BuyIngredient',
      ingredientId: c.ingredientId,
      units: Math.ceil(c.amount * bottles),
    });
  }
  return { state, formulaId: state.lab.formulas[0]!.id };
}

describe('the stills (balance pass 6)', () => {
  it('a still makes at most a copper’s worth a batch, and a larger copper makes every still bigger', () => {
    const { state, formulaId } = shop('copper', 200);
    expect(observe(state, content)).toMatchObject({ stills: 1, stillBottles: COPPER });
    const brew = legalActions(state, content).find((a) => a.type === 'BrewBatch');
    expect(brew).toMatchObject({ maxBottles: COPPER });
    const over = applyAction(state, { type: 'BrewBatch', formulaId, bottles: COPPER + 1 }, content);
    expect(over.result).toEqual({
      ok: false,
      error: `a still makes at most ${COPPER} bottles a batch`,
    });
    act(state, { type: 'BrewBatch', formulaId, bottles: COPPER });

    const s = structuredClone(state);
    s.unlocks.unlocked['second_still'] = 0;
    s.player.capabilities['stills'] = 2;
    const copper = content.byId.investments.get('larger_copper')!;
    const after = act(s, { type: 'Invest', investmentId: 'larger_copper' });
    const bigger = COPPER + (copper.effects[0] as { delta: number }).delta;
    expect(observe(after, content)).toMatchObject({ stills: 2, stillBottles: bigger });
    const both = act(
      after,
      { type: 'BrewBatch', formulaId, bottles: bigger },
      { type: 'BrewBatch', formulaId, bottles: bigger - 1 },
    );
    expect(both.lab.batches.map((b) => b.bottles)).toEqual([bigger, bigger - 1]);
  });

  it('the still card can say what the next still waits for', () => {
    const s = start('next');
    s.stats.bottlesSold = 150;
    const steps = observe(s, content).nextCapacity;
    expect(steps.map((c) => [c.id, c.source, c.stills, c.stillBottles])).toEqual([
      ['second_still', 'unlock', 1, 0],
      ['third_still', 'unlock', 1, 0],
      ['larger_copper', 'investment', 0, 24],
      ['fourth_still', 'investment', 1, 0],
    ]);
    expect(steps[0]!.progress).toEqual([
      {
        kind: 'bottlesSold',
        id: null,
        sense: 'atLeast',
        have: 150,
        need: 200,
        met: false,
        via: null,
      },
    ]);
    // the third still waits on the second, whose own conditions follow it
    expect(steps[1]!.progress.map((c) => [c.kind, c.via])).toEqual([
      ['unlocked', null],
      ['bottlesSold', 'second_still'],
      ['bottlesSold', null],
    ]);
    // a venture ends with its price against the cash in hand
    expect(steps[2]!.progress.at(-1)).toMatchObject({ kind: 'cost', need: 20_000 });

    // once the second still is in, the third still is first, and gone once it is had
    s.unlocks.unlocked['second_still'] = 0;
    s.stats.bottlesSold = 450;
    const third = observe(s, content).nextCapacity[0]!;
    expect(third.id).toBe('third_still');
    expect(third.progress).toEqual([
      {
        kind: 'unlocked',
        id: 'second_still',
        sense: 'atLeast',
        have: 1,
        need: 1,
        met: true,
        via: null,
      },
      {
        kind: 'bottlesSold',
        id: null,
        sense: 'atLeast',
        have: 450,
        need: 600,
        met: false,
        via: null,
      },
    ]);
    s.unlocks.unlocked['third_still'] = 0;
    expect(observe(s, content).nextCapacity.map((c) => c.id)).toEqual([
      'larger_copper',
      'fourth_still',
    ]);
  });

  it('a formula’s unit cost is what an order charges today, after the market moves', () => {
    const s = start('prices');
    s.player.cashCents = 100_000;
    s.month.startCashCents = s.player.cashCents;
    const created = act(s, {
      type: 'CreateFormula',
      name: 'T',
      components: TONIC,
      processId: 'simple_mix',
      priceCents: 60,
    });
    const before = observe(created, content).formulas[0]!.unitCostCents;
    const dearer = structuredClone(created);
    dearer.month.startCashCents = dearer.player.cashCents;
    dearer.player.capabilities['surcharge_pct_grain_alcohol'] = 100;
    dearer.player.capabilities['ingredient_discount_pct'] = 15;
    const now = observe(dearer, content).formulas[0]!.unitCostCents;
    // what ordering a hundred bottles' worth of every ingredient, and bottling them, costs
    let charged = 100 * content.config.bottleCostCents;
    for (const c of TONIC) {
      const order = act(dearer, {
        type: 'BuyIngredient',
        ingredientId: c.ingredientId,
        units: c.amount * 100,
      });
      charged += dearer.player.cashCents - order.player.cashCents;
    }
    expect(now).not.toBeCloseTo(before);
    expect(now * 100).toBeCloseTo(charged, -1);
    expect(now).toBeCloseTo(formulaUnitCost(dearer, TONIC, 'simple_mix', content));
  });

  it('the books record what was left on the shelf, and observe counts the bare months', () => {
    const { state, formulaId } = setUpTonic(start('bare'), 10);
    const sold = endTurn(act(state, { type: 'BrewBatch', formulaId, bottles: 10 }), content);
    expect(sold.report.shelfEndBottles).toBe(
      sold.state.lab.stock.reduce((n, l) => n + l.bottles, 0),
    );
    let s = sold.state;
    for (let i = 0; i < 3; i++) s = endTurn(s, content).state;
    expect(s.ledger.slice(-3).every((e) => e.shelfEndBottles === 0)).toBe(true);
    expect(observe(s, content).shelfBareMonths).toBe(3);
  });
});

describe('the shop next door (balance pass 6)', () => {
  it('is knocked through only when taken up, and then the rent rises and the branch is offered', () => {
    const s = start('nextdoor');
    s.player.cashCents = 100_000;
    s.month.startCashCents = s.player.cashCents;
    // a full shop is not enough on its own: nothing happens until the proprietor says so
    while (s.customers.length < 30)
      s.customers.push({ ...s.customers[0]!, id: `c${s.customers.length}` });
    const left = endTurn(s, content);
    expect(left.state.unlocks.unlocked['larger_shop']).toBeUndefined();
    expect(left.report.deltas.filter((d) => d.cause === 'expense.rent')).toEqual([
      { metric: 'cash', amount: -content.config.rentCents, cause: 'expense.rent' },
    ]);
    const offer = observe(left.state, content).investments.find((i) => i.id === 'shop_next_door')!;
    expect(offer).toMatchObject({ canBuy: true, upkeepCents: 500 });
    const taken = act(left.state, { type: 'Invest', investmentId: 'shop_next_door' });
    const { state, report } = endTurn(taken, content);
    expect(state.unlocks.unlocked['larger_shop']).toBe(taken.meta.turn);
    expect(report.deltas).toContainEqual({
      metric: 'cash',
      amount: -500,
      cause: 'expense.upkeep',
      detail: 'shop_next_door',
    });
    expect(observe(state, content).customerCapacity).toBe(
      observe(left.state, content).customerCapacity + 20,
    );
    expect(
      observe(state, content).investments.find((i) => i.id === 'branch_shop')?.refusal,
    ).not.toBe('not yet offered');
  });
});

describe('the coroner’s fee (balance pass 6)', () => {
  it('rises with every envelope, even when the talk has died down since the last', () => {
    const s: GameState = start('fee');
    s.player.cashCents = 100_000;
    s.month.startCashCents = s.player.cashCents;
    s.unlocks.unlocked['coroner_fees'] = 0;
    s.scrutiny.heat = 100;
    s.month.startHeat = 100;
    const fees: number[] = [];
    let state = s;
    for (let i = 0; i < 3; i++) {
      state = structuredClone(state);
      state.deaths.push({
        id: `death_${i}`,
        turn: 0,
        date: { ...state.meta.date },
        customerId: null,
        name: 'Mr. Test',
        segment: 'labourer',
        consumer: 'self',
        anonymous: false,
        cause: 'overdose',
        productImplicated: true,
        formulaId: null,
        badBatch: false,
        detectability: 4,
        boldness: 0,
        inquest: 'pending',
        verdict: null,
      });
      state.events.pending.push({
        instanceId: `inq_${i}`,
        eventId: 'inquest',
        drawnTurn: 0,
        deathId: `death_${i}`,
      });
      const cost = bribeCostCents(state, content);
      expect(observe(state, content).pendingEvents.at(-1)!.bribeCostCents).toBe(cost);
      fees.push(cost);
      state = act(state, {
        type: 'ResolveEvent',
        instanceId: `inq_${i}`,
        choiceId: 'bribe_coroner',
      });
      expect(state.scrutiny.lastBribeCents).toBe(cost);
      // the talk dies down between envelopes
      const cooled = Math.max(0, state.scrutiny.heat - 30);
      state.month.startHeat -= state.scrutiny.heat - cooled;
      state.scrutiny.heat = cooled;
    }
    const rise = content.config.scrutiny.bribeMinRiseCents;
    expect(fees[1]!).toBeGreaterThanOrEqual(fees[0]! + rise);
    expect(fees[2]!).toBeGreaterThanOrEqual(fees[1]! + rise);
  });
});
