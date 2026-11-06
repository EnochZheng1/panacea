import { describe, expect, it } from 'vitest';
import {
  analyzeFormula,
  checkInvariants,
  newScratch,
  observe,
  Rng,
  type FormulaComponent,
  type GameState,
  type TurnContext,
} from '../src/index.ts';
import { productionPhase } from '../src/systems/lab.ts';
import { cravingPremiumCents, salesPhase, wordOfStrength } from '../src/systems/market.ts';
import { recordDeath, resolveInquest, type DeathInput } from '../src/systems/scrutiny.ts';
import { reportedCauseOf, wardPhase } from '../src/systems/ward.ts';
import { act, content, roomFor, start } from './helpers.ts';

const MILD: FormulaComponent[] = [
  { ingredientId: 'grain_alcohol', amount: 2 },
  { ingredientId: 'laudanum', amount: 0.25 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 4.75 },
];
const STRONG: FormulaComponent[] = [
  { ingredientId: 'grain_alcohol', amount: 2 },
  { ingredientId: 'laudanum', amount: 1.25 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 3.75 },
];

/** A shop with `bottles` of a tonic on the shelf, at 70c. */
function stocked(components: FormulaComponent[], bottles: number, seed: string) {
  let s = start(seed);
  s.player.cashCents = 1_000_000;
  s.month.startCashCents = s.player.cashCents;
  s = act(s, {
    type: 'CreateFormula',
    name: 'T',
    components,
    processId: 'simple_mix',
    priceCents: 70,
  });
  for (const c of components) {
    s = act(s, {
      type: 'BuyIngredient',
      ingredientId: c.ingredientId,
      units: Math.ceil(c.amount * bottles),
    });
  }
  const formulaId = s.lab.formulas[0]!.id;
  roomFor(s, bottles);
  s = act(s, { type: 'BrewBatch', formulaId, bottles });
  productionPhase.run({ state: s, content, rng: Rng.fromSeed(seed), scratch: newScratch() });
  return { state: s, formulaId };
}

const ctxFor = (state: GameState, seed: string): TurnContext => ({
  state,
  content,
  rng: Rng.fromSeed(seed),
  scratch: newScratch(),
});

const death = (over: Partial<DeathInput> = {}): DeathInput => ({
  customerId: null,
  name: 'Mr. Test',
  segment: 'labourer',
  consumer: 'self',
  anonymous: false,
  ailment: 'rheumatism',
  cause: 'overdose',
  productImplicated: true,
  formulaId: null,
  badBatch: false,
  ...over,
});

describe('cause of death, as the ward gives it (balance pass 7)', () => {
  it('names an overdose and a wasting, and otherwise what a death looked like', () => {
    expect(reportedCauseOf('overdose', 'labourer', 'rheumatism', content)).toBe('overdose');
    expect(reportedCauseOf('poisoning', 'labourer', 'rheumatism', content)).toBe('wasting');
    expect(reportedCauseOf('disease', 'seamstress', 'consumption', content)).toBe('ailment');
    expect(reportedCauseOf('natural', 'seamstress', 'consumption', content)).toBe('ailment');
    expect(reportedCauseOf('natural', 'mother', 'teething_infant', content)).toBe('ailment');
    expect(reportedCauseOf('natural', 'elderly', 'rheumatism', content)).toBe('old_age');
    expect(reportedCauseOf('natural', 'clerk', 'dyspepsia', content)).toBe('natural');
  });

  it('a natural death reads the same whether or not the remedy hastened it', () => {
    const s = start('hastened');
    const a = recordDeath(
      s,
      content,
      Rng.fromSeed('a'),
      death({ cause: 'natural', productImplicated: false }),
    );
    const b = recordDeath(
      s,
      content,
      Rng.fromSeed('b'),
      death({ cause: 'natural', productImplicated: true }),
    );
    const seen = observe(s, content).deaths;
    expect(seen.find((d) => d.id === a.id)!.reportedCause).toBe('natural');
    expect(seen.find((d) => d.id === b.id)!.reportedCause).toBe('natural');
    expect(seen[0]!.ailment).toBe('rheumatism');
    expect(Object.keys(seen[0]!)).not.toContain('productImplicated');
  });
});

describe('the ward remembers (balance pass 7)', () => {
  it('a death the remedy caused, and a verdict that named it, weigh on the ward and fade', () => {
    const s = start('memory');
    const cfg = content.config.ward;
    const d = recordDeath(s, content, Rng.fromSeed('d'), death({ formulaId: null }));
    // a natural death the remedy had nothing to do with is not held against the shop
    recordDeath(
      s,
      content,
      Rng.fromSeed('n'),
      death({ cause: 'natural', productImplicated: false }),
    );
    wardPhase.run(ctxFor(s, 'w'));
    expect(s.ward.memory).toBeCloseTo(cfg.perDeath, 9);

    d.inquest = 'pending';
    s.events.pending.push({ instanceId: 'i', eventId: 'inquest', drawnTurn: 0, deathId: d.id });
    s.month.news = [];
    // the first seed on which the jury names the remedy
    for (let seed = 0; seed < 200; seed++) {
      const copy = structuredClone(s);
      resolveInquest(copy, content, Rng.fromSeed(`v${seed}`), d.id, 'testify');
      if (copy.deaths.find((x) => x.id === d.id)!.inquest === 'implicated') {
        Object.assign(s, copy);
        break;
      }
    }
    expect(s.month.news.some((n) => n.kind === 'inquest' && n.outcome === 'implicated')).toBe(true);
    s.events.pending = [];
    const before = s.ward.memory;
    wardPhase.run(ctxFor(s, 'w'));
    expect(s.ward.memory).toBeCloseTo(
      before * (1 - cfg.memoryDecay) - cfg.memoryDecayFlat + cfg.perVerdict,
      9,
    );
    const seen = observe(s, content);
    expect(seen.wardMemory).toBe(s.ward.memory);
    expect(seen.wardRemembers).toMatchObject({ months: cfg.rememberMonths, verdicts: 1 });

    // a quiet year: most of it is forgotten, not all
    const peak = s.ward.memory;
    for (let i = 0; i < 12; i++) {
      s.month.news = [];
      wardPhase.run(ctxFor(s, 'w'));
    }
    expect(s.ward.memory).toBeGreaterThan(0);
    expect(s.ward.memory).toBeLessThan(peak * 0.35);
    expect(s.ward.peakMemory).toBe(peak);
  });

  it('puts off strangers at the counter and keeps them from coming back', () => {
    let forgotten = 0;
    let remembered = 0;
    for (let i = 0; i < 20; i++) {
      const a = stocked(MILD, 300, `w${i}`).state;
      const b = structuredClone(a);
      b.ward.memory = 100;
      const ctxA = ctxFor(a, `s${i}`);
      const ctxB = ctxFor(b, `s${i}`);
      salesPhase.run(ctxA);
      salesPhase.run(ctxB);
      forgotten += a.month.sales[0]!.walkInBottles;
      remembered += b.month.sales[0]!.walkInBottles;
    }
    expect(remembered).toBeLessThan(forgotten * (1 - content.config.ward.walkInLoss / 2));
  });
});

describe('sales by remedy and by where they went (balance pass 7)', () => {
  it('every bottle is under one source, and the post takes the remedy with most on the shelf', () => {
    const { state, formulaId } = stocked(MILD, 200, 'split');
    state.player.investments['almanac'] = { boughtTurn: 0, endsTurn: null };
    salesPhase.run(ctxFor(state, 'split'));
    const sale = state.month.sales.find((x) => x.formulaId === formulaId)!;
    expect(sale.postBottles).toBeGreaterThan(0);
    expect(sale.shopBottles + sale.walkInBottles + sale.wholesaleBottles + sale.postBottles).toBe(
      sale.bottles,
    );
    expect(sale.postRevenueCents).toBe(state.month.post.revenueCents);
    expect(sale.walkInRevenueCents).toBe(sale.walkInBottles * 70);
    expect(checkInvariants(state, content)).toEqual([]);
  });
});

describe('poison tempts (balance pass 7)', () => {
  it('a regular who craves a strong bottle pays over its price; a mild one does not', () => {
    const strong = stocked(STRONG, 10, 'p');
    const mild = stocked(MILD, 10, 'q');
    const offerOf = (s: GameState) => {
      const f = s.lab.formulas[0]!;
      return { formula: f, analysis: analyzeFormula(f.components, f.processId, content) };
    };
    const hooked = structuredClone(strong.state.customers[0]!);
    hooked.body.dependency = 0.9;
    expect(cravingPremiumCents(hooked, offerOf(strong.state), content)).toBeGreaterThan(0);
    expect(cravingPremiumCents(hooked, offerOf(mild.state), content)).toBe(0);
    // a habit that has run up its slate pays the price and no more
    hooked.debtCents = 1_000_000;
    expect(cravingPremiumCents(hooked, offerOf(strong.state), content)).toBe(0);
  });

  it('the premium is booked as its own line, and the books still balance', () => {
    const { state } = stocked(STRONG, 300, 'books');
    for (const c of state.customers) c.body.dependency = 0.9;
    salesPhase.run(ctxFor(state, 'books'));
    const premium = state.month.sales[0]!.cravingPremiumCents;
    expect(premium).toBeGreaterThan(0);
    const booked = state.month.deltas
      .filter((d) => d.cause === 'sales.craving')
      .reduce((n, d) => n + d.amount, 0);
    expect(booked).toBe(premium);
    expect(checkInvariants(state, content)).toEqual([]);
  });

  it('word of a strong bottle brings callers; a mild tonic is not talked about', () => {
    const offer = (components: FormulaComponent[]) => [
      {
        formula: stocked(components, 1, 'o').state.lab.formulas[0]!,
        analysis: analyzeFormula(components, 'simple_mix', content),
      },
    ];
    expect(wordOfStrength(offer(MILD), content)).toBe(0);
    expect(wordOfStrength(offer(STRONG), content)).toBeGreaterThan(5);
  });
});
