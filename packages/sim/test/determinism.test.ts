import fc from 'fast-check';
import { describe, expect, it } from 'vitest';
import {
  applyAction,
  checkInvariants,
  endTurn,
  hashValue,
  legalActions,
  replay,
  type Action,
  type ActionSpec,
  type GameState,
} from '../src/index.ts';
import { content, start } from './helpers.ts';

/** Turns a legal action spec plus random numbers into a concrete action. */
function concretise(spec: ActionSpec, r: number[]): Action {
  const pick = <T>(xs: readonly T[], x: number) => xs[Math.floor(x * xs.length) % xs.length] as T;
  const [a = 0, b = 0, c = 0] = r;
  switch (spec.type) {
    case 'BuyIngredient':
      return { ...spec, units: 1 + Math.floor(a * Math.min(spec.maxUnits, 50)) };
    case 'CreateFormula':
      return {
        type: 'CreateFormula',
        name: 'Fuzz',
        components: [
          { ingredientId: pick(spec.ingredientIds, a), amount: 0.1 + b * 3 },
          { ingredientId: 'water', amount: 1 + c * 3 },
        ].filter((x, i, all) => all.findIndex((y) => y.ingredientId === x.ingredientId) === i),
        processId: pick(spec.processIds, b),
        priceCents: Math.floor(c * 200),
        claims: a > 0.5 ? spec.claimIds.slice(0, 2) : [],
      };
    case 'Reformulate':
      return {
        type: 'Reformulate',
        formulaId: spec.formulaId,
        components: [
          { ingredientId: pick(spec.ingredientIds, b), amount: 0.1 + a * 2 },
          { ingredientId: 'water', amount: 2 },
        ].filter((x, i, all) => all.findIndex((y) => y.ingredientId === x.ingredientId) === i),
        processId: pick(spec.processIds, c),
      };
    case 'SetClaims':
      return {
        type: 'SetClaims',
        formulaId: spec.formulaId,
        claims: spec.claimIds
          .filter((_, i) => (i + Math.floor(a * 10)) % 3 === 0)
          .slice(0, spec.maxClaims),
      };
    case 'SetAdSpend':
      return { type: 'SetAdSpend', cents: Math.floor(a * 3000) };
    case 'SetPrice':
      return { type: 'SetPrice', formulaId: spec.formulaId, priceCents: Math.floor(a * 300) };
    case 'BrewBatch':
      return {
        type: 'BrewBatch',
        formulaId: spec.formulaId,
        bottles: 1 + Math.floor(a * spec.maxBottles),
      };
    case 'ResolveEvent':
      return {
        type: 'ResolveEvent',
        instanceId: spec.instanceId,
        choiceId: pick(spec.choiceIds, a),
      };
  }
}

/** A random playthrough script: per month, a few (spec index, params) moves. */
const scriptArb = fc.array(
  fc.array(
    fc.tuple(
      fc.double({ min: 0, max: 1, noNaN: true }),
      fc.array(fc.double({ min: 0, max: 0.999, noNaN: true }), { minLength: 3, maxLength: 3 }),
    ),
    {
      maxLength: 5,
    },
  ),
  { minLength: 1, maxLength: 40 },
);

function play(seed: string, script: [number, number[]][][]): GameState {
  let s = start(seed);
  for (const month of script) {
    if (s.meta.status !== 'running') break;
    for (const [which, params] of month) {
      const specs = legalActions(s, content);
      if (specs.length === 0) break;
      const spec = specs[Math.floor(which * specs.length) % specs.length]!;
      s = applyAction(s, concretise(spec, params), content).state;
      expect(checkInvariants(s, content)).toEqual([]);
    }
    s = endTurn(s, content).state;
    expect(checkInvariants(s, content)).toEqual([]);
  }
  return s;
}

describe('determinism', () => {
  it('random playthroughs never violate invariants', () => {
    fc.assert(
      fc.property(fc.string({ maxLength: 8 }), scriptArb, (seed, script) => {
        play(seed, script);
      }),
      { numRuns: 60 },
    );
  });

  it('the same seed and actions produce the same game, and the log replays it exactly', () => {
    fc.assert(
      fc.property(fc.string({ maxLength: 8 }), scriptArb, (seed, script) => {
        const a = play(seed, script);
        const b = play(seed, script);
        expect(hashValue(a)).toBe(hashValue(b));
        const replayed = replay(content, { seed }, a.log);
        expect(hashValue(replayed)).toBe(hashValue(a));
      }),
      { numRuns: 30 },
    );
  });

  it('different seeds diverge', () => {
    const run = (seed: string) => {
      let s = start(seed);
      s.player.cashCents = 1_000_000;
      s.month.startCashCents = s.player.cashCents;
      s.stats.bottlesSold = 100;
      for (let i = 0; i < 24; i++) s = endTurn(s, content).state;
      return hashValue(s);
    };
    expect(run('one')).not.toBe(run('two'));
  });
});
