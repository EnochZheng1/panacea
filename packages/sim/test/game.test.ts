import { stringKeys } from '@panacea/content';
import { describe, expect, it } from 'vitest';
import {
  analyzeFormula,
  applyAction,
  CAUSES,
  deserializeGame,
  endTurn,
  legalActions,
  observe,
  serializeGame,
  STATE_VERSION,
  sumDeltas,
} from '../src/index.ts';
import { act, content, deepFreeze, months, setUpTonic, start, TONIC } from './helpers.ts';

describe('new game', () => {
  it('starts on the configured date with the background’s cash', () => {
    const s = start();
    expect(s.meta.date).toEqual(content.config.startDate);
    expect(s.meta.turn).toBe(0);
    expect(s.player.cashCents).toBe(content.byId.backgrounds.get('druggist')!.startingCashCents);
    expect(s.player.capabilities['stills']).toBe(1);
    expect(s.meta.stateVersion).toBe(STATE_VERSION);
  });

  it('rejects unknown backgrounds', () => {
    expect(() => start('x', 'astronaut')).toThrow(/unknown background/);
  });
});

describe('actions', () => {
  it('never mutate the input state', () => {
    const s = deepFreeze(start());
    const { state: after } = setUpTonic(s, 10);
    expect(after).not.toBe(s);
    expect(s.lab.formulas).toHaveLength(0);
  });

  it('leave the state untouched and explain why when rejected', () => {
    const s = start();
    const r = applyAction(
      s,
      { type: 'BuyIngredient', ingredientId: 'morphine', units: 100_000 },
      content,
    );
    expect(r.result).toEqual({ ok: false, error: 'not enough cash' });
    expect(r.state).toBe(s);
  });

  it('buying charges the ingredient cost to the ledger', () => {
    const s = act(start(), { type: 'BuyIngredient', ingredientId: 'laudanum', units: 10 });
    expect(s.lab.inventory['laudanum']).toBe(10);
    expect(s.month.deltas).toEqual([
      { metric: 'cash', amount: -150, cause: CAUSES.ingredients, detail: 'laudanum' },
    ]);
  });

  it('refuses ingredients before their historical date', () => {
    const r = applyAction(
      start(),
      { type: 'BuyIngredient', ingredientId: 'heroin', units: 1 },
      content,
    );
    expect(r.result).toMatchObject({ ok: false, error: '"heroin" is not yet available' });
  });

  it('validates formulas', () => {
    const s = start();
    const bad = (components: typeof TONIC, processId = 'simple_mix') =>
      applyAction(
        s,
        { type: 'CreateFormula', name: 'X', components, processId, priceCents: 10 },
        content,
      ).result;
    expect(bad([])).toMatchObject({ ok: false, error: 'formula has no ingredients' });
    expect(bad([{ ingredientId: 'water', amount: 9 }])).toMatchObject({ ok: false });
    expect(bad([{ ingredientId: 'water', amount: -1 }])).toMatchObject({ ok: false });
    expect(bad([{ ingredientId: 'unobtainium', amount: 1 }])).toMatchObject({ ok: false });
    expect(bad(TONIC, 'percolate')).toMatchObject({
      ok: false,
      error: 'process "percolate" is not unlocked',
    });
  });

  it('brewing consumes ingredients, charges bottling, and uses the still', () => {
    const { state, formulaId } = setUpTonic(start(), 10);
    const s = act(state, { type: 'BrewBatch', formulaId, bottles: 10 });
    expect(s.lab.inventory['laudanum']).toBeCloseTo(3 - 2.5);
    expect(s.lab.batches).toHaveLength(1);
    expect(sumDeltas(s.month, 'cash')).toBe(s.player.cashCents - s.month.startCashCents);
    const again = applyAction(s, { type: 'BrewBatch', formulaId, bottles: 1 }, content);
    expect(again.result).toEqual({ ok: false, error: 'no free still this month' });
  });

  it('formula analysis sums ingredient properties per ounce', () => {
    const a = analyzeFormula([{ ingredientId: 'laudanum', amount: 0.5 }], 'simple_mix', content);
    expect(a.volume).toBe(0.5);
    expect(a.properties.calm).toBe(4);
    expect(a.unitCostCents).toBeCloseTo(content.config.bottleCostCents + 7.5);
  });
});

describe('month end', () => {
  it('advances the date, charges rent, and records a ledger entry', () => {
    const { state, report } = endTurn(start(), content);
    expect(state.meta.date).toEqual({ year: 1885, month: 2 });
    expect(state.meta.turn).toBe(1);
    expect(report.deltas).toContainEqual({
      metric: 'cash',
      amount: -content.config.rentCents,
      cause: CAUSES.rent,
    });
    expect(report.cashEndCents - report.cashStartCents).toBe(sumDeltas(report, 'cash'));
  });

  it('finished batches are sold to customers', () => {
    const { state, formulaId } = setUpTonic(start('sales'), 30);
    const s = months(act(state, { type: 'BrewBatch', formulaId, bottles: 30 }), 1);
    const sold = s.ledger[0]!.sales.find((x) => x.formulaId === formulaId);
    expect(sold?.bottles).toBeGreaterThan(0);
    expect(s.lab.stock[formulaId]).toBe(30 - sold!.bottles);
    expect(s.stats.bottlesSold).toBe(sold!.bottles);
  });

  it('ends at the timeline end date', () => {
    let s = start();
    s.player.cashCents = 10_000_000; // enough to never go bankrupt
    s.month.startCashCents = s.player.cashCents;
    s = months(s, 10_000);
    expect(s.meta.status).toBe('ended');
    expect(s.meta.endReason).toBe('timeline_end');
    expect(s.meta.date).toEqual(content.config.endDate);
    expect(s.ledger).toHaveLength(264);
    expect(() => endTurn(s, content)).toThrow();
  });

  it('bankrupts after the grace period of insolvency', () => {
    let s = start('broke');
    s.player.cashCents = 0;
    s.month.startCashCents = 0;
    s = months(s, 10);
    expect(s.meta.endReason).toBe('bankrupt');
    expect(s.ledger).toHaveLength(content.config.bankruptcyGraceMonths);
    expect(
      applyAction(s, { type: 'SetPrice', formulaId: 'x', priceCents: 1 }, content).result.ok,
    ).toBe(false);
  });

  it('fires historical events on their dates', () => {
    let s = start();
    s.player.cashCents = 10_000_000;
    s.month.startCashCents = s.player.cashCents;
    s = months(s, 13); // through January 1886
    expect('antifebrin' in s.timeline.fired).toBe(true);
    expect(s.player.flags['era_acetanilide']).toBe(true);
    expect('poison_squad' in s.timeline.fired).toBe(false);
  });

  it('grants unlocks when their requirement is met', () => {
    let s = start();
    s.stats.batchesBrewed = 10;
    s = months(s, 1);
    expect('percolator' in s.unlocks.unlocked).toBe(true);
    expect(s.ledger[0]!.news).toContainEqual({ kind: 'unlock', id: 'percolator' });
    expect(observe(s, content).processes).toContain('percolate');
  });

  it('auto-resolves undecided events with their default choice', () => {
    let s = start();
    s.stats.bottlesSold = 10;
    s.events.pending.push({ instanceId: 'manual', eventId: 'grateful_letter', drawnTurn: 0 });
    s = months(s, 1);
    expect(s.player.flags['testimonial_received']).toBe(true);
    expect(s.ledger[0]!.news).toContainEqual({
      kind: 'event_resolved',
      eventId: 'grateful_letter',
      choiceId: 'print_testimonial',
      auto: true,
    });
  });

  it('applies a chosen event outcome', () => {
    const s0 = start();
    s0.events.pending.push({ instanceId: 'm', eventId: 'diploma_mill', drawnTurn: 0 });
    const s = act(s0, { type: 'ResolveEvent', instanceId: 'm', choiceId: 'buy_diploma' });
    expect(s.player.flags['bought_diploma']).toBe(true);
    expect(s.player.notoriety).toBe(5);
    expect(s.events.pending).toHaveLength(0);
  });
});

describe('machine API', () => {
  it('lists legal actions that actually succeed', () => {
    const { state, formulaId } = setUpTonic(start(), 5);
    const specs = legalActions(state, content);
    const brew = specs.find((x) => x.type === 'BrewBatch');
    expect(brew).toMatchObject({ formulaId, maxBottles: 5 });
    if (brew?.type === 'BrewBatch') {
      expect(
        applyAction(state, { type: 'BrewBatch', formulaId, bottles: brew.maxBottles }, content)
          .result.ok,
      ).toBe(true);
    }
  });

  it('offers nothing once the game has ended', () => {
    const s = start();
    s.meta.status = 'ended';
    s.meta.endReason = 'bankrupt';
    expect(legalActions(s, content)).toEqual([]);
  });
});

describe('saves', () => {
  it('round-trip exactly', () => {
    const { state } = setUpTonic(start('save'), 4);
    const s = months(state, 3);
    expect(deserializeGame(serializeGame(s))).toEqual(s);
  });

  it('refuse saves from a newer build', () => {
    expect(() =>
      deserializeGame(JSON.stringify({ saveVersion: STATE_VERSION + 1, state: {} })),
    ).toThrow(/newer than this build/);
  });

  it('refuse files that are not saves', () => {
    expect(() => deserializeGame('{"hello":1}')).toThrow(/not a Panacea save/);
  });
});

describe('strings', () => {
  it('every ledger cause has a display string', () => {
    for (const cause of Object.values(CAUSES)) {
      for (const key of stringKeys.cause(cause)) expect(content.strings[key], key).toBeTruthy();
    }
  });
});
