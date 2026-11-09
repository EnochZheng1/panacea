import type { ContentPack, GameEvent } from '@panacea/content';
import { describe, expect, it } from 'vitest';
import { newScratch, Rng, type GameState } from '../src/index.ts';
import { applyEffects } from '../src/systems/effects.ts';
import { productionPhase } from '../src/systems/lab.ts';
import { letterCallers, letterTrust, salesPhase } from '../src/systems/market.ts';
import { creditedEffects, drawEvents } from '../src/systems/progression.ts';
import { evaluateRequirement } from '../src/systems/requirements.ts';
import { act, content, roomFor, start, TONIC } from './helpers.ts';

/** `n` random letters that could all come this month, each certain to be drawn if reached. */
function letters(n: number, chance = 1): GameEvent[] {
  return Array.from({ length: n }, (_, i) => ({
    id: `letter_${i}`,
    trigger: 'random' as const,
    condition: { type: 'date' as const, from: { year: 1885, month: 1 } },
    chance,
    once: false,
    cooldownMonths: 0,
    choices: [{ id: 'ok', effects: [], default: true }],
  }));
}

const withEvents = (events: GameEvent[]): ContentPack => ({
  ...content,
  events,
  byId: { ...content.byId, events: new Map(events.map((e) => [e.id, e])) },
});

const fresh = (seed: string): GameState => {
  const s = start(seed);
  s.events = { pending: [], lastDrawn: {}, timesDrawn: {} };
  return s;
};

describe('the post (drawEvents)', () => {
  it('draws no more than the month allows, and every letter has a fair turn', () => {
    const pack = withEvents(letters(20));
    const counts = new Map<string, number>();
    for (let i = 0; i < 400; i++) {
      const s = fresh(`fair-${i}`);
      drawEvents(s, pack, Rng.fromSeed(`fair-${i}`));
      expect(s.events.pending).toHaveLength(pack.config.maxEventsPerMonth);
      for (const p of s.events.pending) counts.set(p.eventId, (counts.get(p.eventId) ?? 0) + 1);
    }
    // 800 letters among 20: about 40 each. In file order the first two would have taken all 800.
    expect(counts.size).toBe(20);
    for (const n of counts.values()) {
      expect(n).toBeGreaterThan(20);
      expect(n).toBeLessThan(65);
    }
  });

  it('leaves out a letter already waiting, spent, cooling down or not yet due', () => {
    const [a, b, c, d, e] = letters(5);
    const pack = withEvents([
      a!,
      { ...b!, once: true },
      { ...c!, cooldownMonths: 12 },
      { ...d!, condition: { type: 'date', from: { year: 1900, month: 1 } } },
      e!,
    ]);
    const s = fresh('rules');
    s.events.pending.push({ instanceId: 'x', eventId: 'letter_0', drawnTurn: 0 });
    s.events.timesDrawn['letter_1'] = 1;
    s.events.lastDrawn['letter_2'] = s.meta.turn - 3;
    drawEvents(s, pack, Rng.fromSeed('rules'));
    expect(s.events.pending.map((p) => p.eventId)).toEqual(['letter_0', 'letter_4']);
    expect(s.events.lastDrawn['letter_4']).toBe(s.meta.turn);
    expect(s.events.timesDrawn['letter_4']).toBe(1);
  });

  it('draws each letter with its own chance', () => {
    const pack = withEvents(letters(1, 0.25));
    let drawn = 0;
    for (let i = 0; i < 2000; i++) {
      const s = fresh(`chance-${i}`);
      drawEvents(s, pack, Rng.fromSeed(`chance-${i}`));
      drawn += s.events.pending.length;
    }
    expect(drawn / 2000).toBeGreaterThan(0.22);
    expect(drawn / 2000).toBeLessThan(0.28);
  });

  it('spreads a whole run of letters over many correspondents', () => {
    const s = start('spread');
    const seen = new Set<string>();
    const rng = Rng.fromSeed('spread');
    for (let turn = 0; turn < 240; turn++) {
      s.events.pending = [];
      s.meta.turn = turn;
      s.meta.date = { year: 1885 + Math.floor(turn / 12), month: (turn % 12) + 1 };
      s.stats.bottlesSold = 50 * turn;
      s.stats.batchesBrewed = turn;
      drawEvents(s, content, rng);
      for (const p of s.events.pending) seen.add(p.eventId);
    }
    expect(seen.size).toBeGreaterThan(40);
  });

  it('brings a seasonal letter only in its months', () => {
    const [a] = letters(1);
    const pack = withEvents([{ ...a!, condition: { type: 'season', months: [12, 1] } }]);
    const s = fresh('season');
    const months: number[] = [];
    for (let m = 1; m <= 12; m++) {
      s.events.pending = [];
      s.meta.date = { year: 1890, month: m };
      expect(evaluateRequirement({ type: 'season', months: [12, 1] }, s, pack).met).toBe(
        m === 12 || m === 1,
      );
      drawEvents(s, pack, Rng.fromSeed(`season-${m}`));
      if (s.events.pending.length > 0) months.push(m);
    }
    expect(months).toEqual([1, 12]);
  });
});

describe('letter effects', () => {
  it("sends fewer of a letter's callers when the ward remembers harm or the Board suspects", () => {
    const s = start('trust');
    s.scrutiny.heat = 0;
    s.ward.memory = 0;
    applyEffects(s, [{ type: 'capability', id: 'callers_wheelmen', delta: 6 }], 'event');
    applyEffects(s, [{ type: 'capability', id: 'callers_bazaar', delta: 4 }], 'event');
    applyEffects(s, [{ type: 'capability', id: 'callers_portrait', delta: 1 }], 'event');
    expect(letterTrust(s, content)).toBe(1);
    // the ward remembers: half the word is given
    s.ward.memory = 50;
    expect(letterTrust(s, content)).toBeCloseTo(0.5);
    expect(letterCallers(s, content)).toEqual([
      ['bazaar', 2],
      ['portrait', 1],
      ['wheelmen', 3],
    ]);
    // and the Board suspects it: at a fifth of the way to shutting it, 1 − 1.5 × 0.2 of that
    expect(content.config.scrutiny.letterCallerLoss).toBe(1.5);
    s.scrutiny.heat = 20;
    expect(letterTrust(s, content)).toBeCloseTo(0.35);
    expect(letterCallers(s, content)).toEqual([
      ['bazaar', 1],
      ['wheelmen', 2],
    ]);
    // a ward that remembers everything sends nobody on a letter's word
    s.scrutiny.heat = 0;
    s.ward.memory = 100;
    expect(letterCallers(s, content)).toEqual([]);
    // nor for a shop two-thirds of the way to being shut
    s.ward.memory = 0;
    s.scrutiny.heat = 70;
    expect(letterTrust(s, content)).toBe(0);
    expect(letterCallers(s, content)).toEqual([]);
  });

  it('credits the standing a reply earns as far as the ward trusts the shop, and takes it in full', () => {
    const s = start('credit');
    const letter = content.byId.events.get('parish_bazaar')!;
    const inquest = content.byId.events.get('inquest')!;
    const effects = [
      { type: 'reputation' as const, delta: 2 },
      { type: 'reputation' as const, delta: -1 },
      { type: 'cash' as const, deltaCents: -300 },
    ];
    s.scrutiny.heat = 0;
    s.ward.memory = 0;
    expect(creditedEffects(s, content, letter, effects)).toEqual(effects);
    s.scrutiny.heat = 20;
    s.ward.memory = 50;
    expect(letterTrust(s, content)).toBeCloseTo(0.35);
    expect(creditedEffects(s, content, letter, effects)).toEqual([
      { type: 'reputation', delta: 2 * letterTrust(s, content) },
      { type: 'reputation', delta: -1 },
      { type: 'cash', deltaCents: -300 },
    ]);
    // the coroner's notice is not a letter of the ward's
    expect(creditedEffects(s, content, inquest, effects)).toEqual(effects);
  });

  it('nudges the ward memory within 0–100, and raises its peak', () => {
    const s = start('memory');
    applyEffects(s, [{ type: 'wardMemory', delta: 30 }], 'event');
    expect(s.ward.memory).toBe(30);
    expect(s.ward.peakMemory).toBe(30);
    applyEffects(s, [{ type: 'wardMemory', delta: -50 }], 'event');
    expect(s.ward.memory).toBe(0);
    expect(s.ward.peakMemory).toBe(30);
    applyEffects(s, [{ type: 'wardMemory', delta: 500 }], 'event');
    expect(s.ward.memory).toBe(100);
    expect(s.ward.peakMemory).toBe(100);
  });

  it("sends a letter's callers to the counter, under their own name", () => {
    let s = start('callers');
    s.player.cashCents = 100_000;
    s.month.startCashCents = s.player.cashCents;
    applyEffects(
      s,
      [{ type: 'capability', id: 'callers_wheelmen', delta: 6, months: 12 }],
      'event',
    );
    applyEffects(s, [{ type: 'capability', id: 'callers_bazaar', delta: 4 }], 'event');
    applyEffects(s, [{ type: 'capability', id: 'callers_fountain', delta: 0 }], 'event');
    s.scrutiny.heat = 0;
    expect(letterCallers(s, content)).toEqual([
      ['bazaar', 4],
      ['wheelmen', 6],
    ]);
    s = act(s, {
      type: 'CreateFormula',
      name: 'T',
      components: TONIC,
      processId: 'simple_mix',
      priceCents: 60,
    });
    for (const c of TONIC) {
      s = act(s, { type: 'BuyIngredient', ingredientId: c.ingredientId, units: c.amount * 40 });
    }
    roomFor(s, 40);
    s = act(s, { type: 'BrewBatch', formulaId: s.lab.formulas[0]!.id, bottles: 40 });
    productionPhase.run({ state: s, content, rng: Rng.fromSeed('callers'), scratch: newScratch() });
    expect(s.lab.stock.length).toBeGreaterThan(0);
    salesPhase.run({ state: s, content, rng: Rng.fromSeed('callers'), scratch: newScratch() });
    expect(s.month.callers['wheelmen']).toBe(6);
    expect(s.month.callers['bazaar']).toBe(4);
    expect(s.month.callers['fountain']).toBeUndefined();
  });
});
