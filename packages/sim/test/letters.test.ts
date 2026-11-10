import type { ContentPack, GameEvent } from '@panacea/content';
import { describe, expect, it } from 'vitest';
import {
  newScratch,
  Rng,
  type GameState,
  type LedgerEntry,
  type SaleRecord,
} from '../src/index.ts';
import { applyEffects } from '../src/systems/effects.ts';
import { productionPhase } from '../src/systems/lab.ts';
import { letterCallers, letterTrust, salesPhase } from '../src/systems/market.ts';
import {
  creditedEffects,
  drawEvents,
  LETTER_LEDGER,
  resolveEvent,
  waitsOnLetter,
} from '../src/systems/progression.ts';
import { evaluateRequirement, remediesSold, shopRemedy } from '../src/systems/requirements.ts';
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

  it('waits for the shop to be old enough, and for the letter a follow-up answers', () => {
    const s = fresh('months');
    s.meta.turn = 11;
    expect(evaluateRequirement({ type: 'months', atLeast: 12 }, s, content)).toEqual({
      met: false,
      progress: 11 / 12,
    });
    s.meta.turn = 12;
    expect(evaluateRequirement({ type: 'months', atLeast: 12 }, s, content).met).toBe(true);

    const since = { type: 'since', event: 'burglary', atLeast: 2 } as const;
    expect(evaluateRequirement(since, s, content).met).toBe(false); // never came
    s.events.lastDrawn['burglary'] = 11;
    expect(evaluateRequirement(since, s, content).met).toBe(false); // a month ago
    s.events.lastDrawn['burglary'] = 10;
    expect(evaluateRequirement(since, s, content).met).toBe(true);
    expect(waitsOnLetter({ type: 'all', of: [{ type: 'flag', id: 'x' }, since] })).toBe(true);
    expect(waitsOnLetter({ type: 'not', requirement: since })).toBe(false);
    expect(waitsOnLetter({ type: 'flag', id: 'x' })).toBe(false);
  });

  it('draws a follow-up before fresh letters when the post is full', () => {
    const many = letters(12);
    const follow = {
      ...letters(1)[0]!,
      id: 'follow_up',
      condition: { type: 'since' as const, event: 'letter_0', atLeast: 0 },
    };
    const pack = withEvents([...many, follow]);
    for (let i = 0; i < 50; i++) {
      const s = fresh(`follow-${i}`);
      s.events.lastDrawn['letter_0'] = -5;
      s.events.timesDrawn['letter_0'] = 1;
      drawEvents(s, pack, Rng.fromSeed(`follow-${i}`));
      expect(s.events.pending.map((p) => p.eventId)).toContain('follow_up');
    }
  });

  it("knows the shop's remedy: what is in it, how habit-forming it is, and its label", () => {
    let s = fresh('remedy');
    const habit = { type: 'remedyHabit', atLeast: 1 } as const;
    const spirit = { type: 'remedyDrug', drugClass: 'alcohol', atLeastOz: 2 } as const;
    const opiate = { type: 'remedyDrug', drugClass: 'opiate', atLeastOz: 0.01 } as const;
    const label = { type: 'remedyClaim', id: 'soothes_nerves' } as const;
    expect(shopRemedy(s)).toBeNull();
    for (const r of [habit, spirit, opiate, label]) {
      expect(evaluateRequirement(r, s, content).met).toBe(false);
    }
    const plain = [
      { ingredientId: 'grain_alcohol', amount: 1 },
      { ingredientId: 'valerian', amount: 1 },
      { ingredientId: 'water', amount: 6 },
    ];
    s = act(s, {
      type: 'CreateFormula',
      name: 'Plain',
      components: plain,
      processId: 'simple_mix',
      priceCents: 50,
      claims: ['soothes_nerves'],
    });
    s = act(s, {
      type: 'CreateFormula',
      name: 'Strong',
      components: TONIC,
      processId: 'simple_mix',
      priceCents: 50,
      claims: [],
    });
    const [plainId, strongId] = s.lab.formulas.map((f) => f.id);
    // nothing sold yet: the newest in the formula book
    expect(shopRemedy(s)?.id).toBe(strongId);
    expect(evaluateRequirement(habit, s, content).met).toBe(true);
    expect(evaluateRequirement(spirit, s, content).met).toBe(true);
    expect(evaluateRequirement(opiate, s, content).met).toBe(true);
    expect(evaluateRequirement(label, s, content).met).toBe(false);
    // the bottle the shop sold most of last month
    const sold = (formulaId: string, bottles: number) => ({ formulaId, bottles }) as SaleRecord;
    s.ledger.push({ sales: [sold(strongId!, 3), sold(plainId!, 9)] } as LedgerEntry);
    expect(shopRemedy(s)?.id).toBe(plainId);
    expect(evaluateRequirement(habit, s, content).met).toBe(false);
    expect(evaluateRequirement(spirit, s, content).met).toBe(false);
    expect(evaluateRequirement(opiate, s, content).met).toBe(false);
    expect(evaluateRequirement(label, s, content).met).toBe(true);
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

describe('correspondents who remember (round 9)', () => {
  /** A shop in its second summer whose `remedies` bottles, each at `priceCents`, all sold last month. */
  function shop(seed: string, priceCents: number, remedies = 1): GameState {
    let s = fresh(seed);
    for (let i = 0; i < remedies; i++) {
      s = act(s, {
        type: 'CreateFormula',
        name: `Remedy ${i}`,
        components: TONIC,
        processId: 'simple_mix',
        priceCents,
        claims: [],
      });
    }
    const sold = s.lab.formulas.map((f, i) => ({ formulaId: f.id, bottles: 9 - i }) as SaleRecord);
    s.ledger.push({ sales: sold } as LedgerEntry);
    s.stats.bottlesSold = 700;
    s.stats.batchesBrewed = 30;
    s.meta.turn = 17;
    s.meta.date = { year: 1886, month: 6 };
    s.player.cashCents = 100_000;
    s.scrutiny.heat = 0;
    s.ward.memory = 0;
    return s;
  }
  /** Whether the letter could come this month. */
  const could = (s: GameState, id: string) =>
    evaluateRequirement(content.byId.events.get(id)!.condition!, s, content).met;
  /** Answers the letter as if it had come `ago` months back (so a follow-up can wait on it). */
  const reply = (s: GameState, eventId: string, choiceId: string, ago = 2) => {
    s.events.pending.push({ instanceId: `r_${eventId}`, eventId, drawnTurn: s.meta.turn });
    expect(resolveEvent(s, content, Rng.fromSeed(eventId), `r_${eventId}`, choiceId, false)).toBe(
      null,
    );
    s.events.lastDrawn[eventId] = s.meta.turn - ago;
    s.events.timesDrawn[eventId] = 1;
  };
  const text = (key: string) => content.strings[`event.${key}`] ?? '';

  it('Feeney remembers how his opening was taken, and only undersells a dearer bottle', () => {
    const slandered = shop('feeney-slander', 75);
    reply(slandered, 'rival_shop_opens', 'spread_rumour');
    expect(could(slandered, 'rival_price_war_slander')).toBe(true);
    expect(could(slandered, 'rival_price_war')).toBe(false);
    expect(could(slandered, 'feeney_boy_slander')).toBe(true);
    expect(could(slandered, 'feeney_boy')).toBe(false);
    expect(text('rival_price_war_slander.body')).toContain('what you told the ward of his stock');
    expect(text('feeney_boy_slander.body')).toContain('whose stock is adulterated');
    expect(text('rival_price_war_slander.choice.cry_him_down')).toContain('again');

    const ignored = shop('feeney-ignored', 75);
    reply(ignored, 'rival_shop_opens', 'ignore');
    expect(could(ignored, 'rival_price_war')).toBe(true);
    expect(could(ignored, 'rival_price_war_slander')).toBe(false);
    expect(could(ignored, 'feeney_boy')).toBe(true);
    expect(could(ignored, 'feeney_boy_slander')).toBe(false);
    expect(text('rival_price_war.body')).toContain('paying him no mind');

    // "Why pay more?" only to a shop that does charge more than his 65c
    for (const s of [slandered, ignored]) s.lab.formulas[0]!.priceCents = 65;
    expect(could(slandered, 'rival_price_war_slander')).toBe(false);
    expect(could(ignored, 'rival_price_war')).toBe(false);
  });

  it("Feeney's copy follows whichever errand-boy letter came, and a boy who left is gone", () => {
    const s = shop('feeney-boy', 75, 2);
    reply(s, 'rival_shop_opens', 'spread_rumour', 4);
    expect(could(s, 'wrong_bottle')).toBe(true);
    expect(could(s, 'boy_measles')).toBe(true);
    reply(s, 'feeney_boy_slander', 'let_him_go');
    expect(s.player.flags['feeney_has_receipt']).toBe(true);
    expect(could(s, 'feeney_copy')).toBe(true);
    expect(waitsOnLetter(content.byId.events.get('feeney_copy')!.condition!)).toBe(true);
    // Tommy is sweeping for Feeney now: no letters from him, or about him
    expect(could(s, 'wrong_bottle')).toBe(false);
    expect(could(s, 'boy_measles')).toBe(false);
    s.meta.date = { year: 1896, month: 5 };
    expect(could(s, 'bicycle_boy')).toBe(false);
    delete s.player.flags['boy_left'];
    expect(could(s, 'bicycle_boy')).toBe(true);
  });

  it('the wrong bottle needs two remedies to mix up', () => {
    const one = shop('one-remedy', 75, 1);
    expect(remediesSold(one)).toBe(1);
    expect(could(one, 'wrong_bottle')).toBe(false);
    const two = shop('two-remedies', 75, 2);
    expect(remediesSold(two)).toBe(2);
    expect(could(two, 'wrong_bottle')).toBe(true);
    expect(evaluateRequirement({ type: 'remedies', atLeast: 2 }, two, content)).toEqual({
      met: true,
      progress: 1,
    });
    two.ledger[two.ledger.length - 1]!.sales[1]!.bottles = 0;
    expect(evaluateRequirement({ type: 'remedies', atLeast: 2 }, two, content)).toEqual({
      met: false,
      progress: 0.5,
    });
    expect(text('wrong_bottle.body')).not.toContain('liniment');
    // the shop's own price, read off the bottle it sells most of
    expect(evaluateRequirement({ type: 'remedyPrice', atLeastCents: 75 }, two, content).met).toBe(
      true,
    );
    expect(evaluateRequirement({ type: 'remedyPrice', atLeastCents: 76 }, two, content).met).toBe(
      false,
    );
    expect(
      evaluateRequirement({ type: 'remedyPrice', atLeastCents: 1 }, fresh('none'), content).met,
    ).toBe(false);
  });

  it('the reporter remembers whether he was paid or shown the door', () => {
    const s = shop('pryor', 75);
    s.meta.date = { year: 1892, month: 3 };
    reply(s, 'reporter_calls', 'pay_him');
    expect(could(s, 'reporter_proofs_paid')).toBe(true);
    expect(could(s, 'reporter_proofs')).toBe(false);
    reply(s, 'reporter_proofs_paid', 'buy_the_head', 12);
    expect(s.player.flags['reporter_kept']).toBe(true);
    s.meta.date = { year: 1904, month: 1 };
    expect(could(s, 'reporter_returns')).toBe(true);

    const t = shop('pryor-door', 75);
    t.meta.date = { year: 1892, month: 3 };
    reply(t, 'reporter_calls', 'show_the_door');
    expect(could(t, 'reporter_proofs')).toBe(true);
    expect(could(t, 'reporter_proofs_paid')).toBe(false);
    expect(text('reporter_proofs.body')).toContain('You showed me the door');
    expect(text('reporter_proofs_paid.body')).toContain('Your five dollars');
  });

  it('Mrs. Stowe remembers what you wrote on her sheet', () => {
    for (const [answer, letter, other, words] of [
      ['purely_vegetable', 'stowe_washington_vegetable', 'stowe_washington', 'PURELY VEGETABLE'],
      ['answer_truly', 'stowe_washington', 'stowe_washington_vegetable', 'the one honest line'],
    ] as const) {
      const s = shop(`stowe-${answer}`, 75);
      reply(s, 'stowe_tract', answer, 18 * 12);
      s.meta.date = { year: 1905, month: 3 };
      expect(could(s, letter)).toBe(true);
      expect(could(s, other)).toBe(false);
      expect(text(`${letter}.body`)).toContain(words);
    }
  });

  it('waits on an "any" only when every way of meeting it waits on a letter', () => {
    const a = { type: 'since', event: 'a', atLeast: 1 } as const;
    const b = { type: 'since', event: 'b', atLeast: 1 } as const;
    expect(waitsOnLetter({ type: 'any', of: [a, b] })).toBe(true);
    expect(waitsOnLetter({ type: 'any', of: [{ type: 'flag', id: 'x' }, a] })).toBe(false);
    // the rival house is not a follow-up of the exclusive terms, though it waits a year after them
    expect(waitsOnLetter(content.byId.events.get('jobber_rival')!.condition!)).toBe(false);
    const s = shop('jobbers', 75);
    expect(could(s, 'jobber_rival')).toBe(true);
    reply(s, 'jobber_exclusive', 'sign', 3);
    expect(could(s, 'jobber_rival')).toBe(false);
    s.events.lastDrawn['jobber_exclusive'] = s.meta.turn - 12;
    expect(could(s, 'jobber_rival')).toBe(true);
  });

  it("books a reply's money where it belongs, and everything else to the letter", () => {
    const booked = (eventId: string, choiceId: string) => {
      const s = shop(`ledger-${eventId}`, 75);
      const before = s.month.deltas.length;
      reply(s, eventId, choiceId);
      return s.month.deltas.slice(before).map((d) => [d.metric, d.cause]);
    };
    expect(booked('spirits_dearer', 'outside_distiller')).toEqual([
      ['cash', 'expense.ingredients'],
    ]);
    expect(booked('sign_painter', 'gilt_sign')).toEqual([['cash', 'expense.investment']]);
    expect(booked('photographer', 'sit_for_it')).toEqual([
      ['cash', 'expense.investment'],
      ['reputation', 'event'],
      ['notoriety', 'event'],
    ]);
    expect(booked('sanitary_inspector', 'pay_fee')).toEqual([
      ['cash', 'expense.bribe'],
      ['notoriety', 'event'],
    ]);
    expect(booked('pedestal_fund', 'box_and_dollar')).toEqual([
      ['cash', 'expense.retainer'],
      ['reputation', 'event'],
    ]);
    expect(booked('jury_duty', 'serve')).toEqual([
      ['cash', LETTER_LEDGER.trade],
      ['reputation', 'event'],
    ]);
    expect(booked('kehoe_thanks', 'send_syrup')[0]).toEqual(['cash', 'event']);
    expect(booked('medicine_show_returns', 'take_money')).toEqual([['cash', 'event']]);
  });
});
