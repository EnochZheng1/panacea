import fc from 'fast-check';
import { describe, expect, it } from 'vitest';
import { Rng, seedRng } from '../src/index.ts';

describe('Rng', () => {
  it('is deterministic for a given seed', () => {
    const a = Rng.fromSeed('panacea');
    const b = Rng.fromSeed('panacea');
    const xs = Array.from({ length: 100 }, () => a.nextU32());
    const ys = Array.from({ length: 100 }, () => b.nextU32());
    expect(xs).toEqual(ys);
  });

  it('differs between seeds', () => {
    expect(Rng.fromSeed('a').nextU32()).not.toBe(Rng.fromSeed('b').nextU32());
  });

  it('resumes exactly from a saved state', () => {
    const a = Rng.fromSeed(7);
    for (let i = 0; i < 50; i++) a.nextU32();
    const resumed = new Rng(JSON.parse(JSON.stringify(a.state())));
    expect(Array.from({ length: 20 }, () => resumed.float())).toEqual(
      Array.from({ length: 20 }, () => a.float()),
    );
  });

  it('keeps every value in range', () => {
    fc.assert(
      fc.property(
        fc.string(),
        fc.integer({ min: -1000, max: 1000 }),
        fc.nat(1000),
        (seed, min, span) => {
          const r = Rng.fromSeed(seed);
          for (let i = 0; i < 20; i++) {
            const f = r.float();
            expect(f).toBeGreaterThanOrEqual(0);
            expect(f).toBeLessThan(1);
            const n = r.int(min, min + span);
            expect(n).toBeGreaterThanOrEqual(min);
            expect(n).toBeLessThanOrEqual(min + span);
          }
        },
      ),
    );
  });

  it('is roughly uniform', () => {
    const r = Rng.fromSeed('uniform');
    const buckets = new Array(10).fill(0);
    for (let i = 0; i < 100_000; i++) buckets[Math.floor(r.float() * 10)]++;
    for (const b of buckets) expect(Math.abs(b - 10_000)).toBeLessThan(500);
  });

  it('produces 32-bit unsigned state', () => {
    const s = seedRng('state');
    for (const v of Object.values(s)) {
      expect(Number.isInteger(v) && v >= 0 && v < 2 ** 32).toBe(true);
    }
  });

  it('weighted picks respect weights and ignore zero weights', () => {
    const r = Rng.fromSeed('w');
    const counts = { a: 0, b: 0, never: 0 };
    for (let i = 0; i < 10_000; i++) {
      const k = r.weighted([
        { item: 'a' as const, weight: 3 },
        { item: 'b' as const, weight: 1 },
        { item: 'never' as const, weight: 0 },
      ]);
      if (k) counts[k]++;
    }
    expect(counts.never).toBe(0);
    expect(counts.a / counts.b).toBeGreaterThan(2.5);
    expect(counts.a / counts.b).toBeLessThan(3.5);
  });
});
