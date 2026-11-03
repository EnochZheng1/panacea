/**
 * Seeded, serialisable pseudo-random number generator (sfc32).
 *
 * The generator's whole state is four 32-bit integers, stored inside `GameState`, so a run can
 * be saved, resumed and replayed bit-for-bit. Never use `Math.random` in the simulation.
 */

export interface RngState {
  a: number;
  b: number;
  c: number;
  d: number;
}

/** Derives an initial generator state from any string or number seed. */
export function seedRng(seed: string | number): RngState {
  const str = String(seed);
  // cyrb128-style string hash → four 32-bit seeds.
  let h1 = 1779033703,
    h2 = 3144134277,
    h3 = 1013904242,
    h4 = 2773480762;
  for (let i = 0; i < str.length; i++) {
    const k = str.charCodeAt(i);
    h1 = h2 ^ Math.imul(h1 ^ k, 597399067);
    h2 = h3 ^ Math.imul(h2 ^ k, 2869860233);
    h3 = h4 ^ Math.imul(h3 ^ k, 951274213);
    h4 = h1 ^ Math.imul(h4 ^ k, 2716044179);
  }
  h1 = Math.imul(h3 ^ (h1 >>> 18), 597399067);
  h2 = Math.imul(h4 ^ (h2 >>> 22), 2869860233);
  h3 = Math.imul(h1 ^ (h3 >>> 17), 951274213);
  h4 = Math.imul(h2 ^ (h4 >>> 19), 2716044179);
  const rng = new Rng({
    a: (h1 ^ h2 ^ h3 ^ h4) >>> 0,
    b: (h2 ^ h1) >>> 0,
    c: (h3 ^ h1) >>> 0,
    d: (h4 ^ h1) >>> 0,
  });
  for (let i = 0; i < 12; i++) rng.nextU32(); // warm up
  return rng.state();
}

export class Rng {
  private a: number;
  private b: number;
  private c: number;
  private d: number;

  constructor(state: RngState) {
    this.a = state.a >>> 0;
    this.b = state.b >>> 0;
    this.c = state.c >>> 0;
    this.d = state.d >>> 0;
  }

  static fromSeed(seed: string | number): Rng {
    return new Rng(seedRng(seed));
  }

  state(): RngState {
    return { a: this.a, b: this.b, c: this.c, d: this.d };
  }

  /** Uniform 32-bit unsigned integer. */
  nextU32(): number {
    const t0 = (this.a + this.b) | 0;
    this.a = this.b ^ (this.b >>> 9);
    this.b = (this.c + (this.c << 3)) | 0;
    this.c = (this.c << 21) | (this.c >>> 11);
    this.d = (this.d + 1) | 0;
    const t = (t0 + this.d) | 0;
    this.c = (this.c + t) | 0;
    this.a >>>= 0;
    this.b >>>= 0;
    this.c >>>= 0;
    this.d >>>= 0;
    return t >>> 0;
  }

  /** Uniform float in [0, 1). */
  float(): number {
    return this.nextU32() / 4294967296;
  }

  /** Uniform integer in [min, max] (inclusive). */
  int(min: number, max: number): number {
    if (!Number.isInteger(min) || !Number.isInteger(max) || max < min) {
      throw new RangeError(`Rng.int: invalid range [${min}, ${max}]`);
    }
    return min + Math.floor(this.float() * (max - min + 1));
  }

  /** True with probability p. */
  chance(p: number): boolean {
    return this.float() < p;
  }

  pick<T>(items: readonly T[]): T {
    if (items.length === 0) throw new RangeError('Rng.pick: empty array');
    return items[this.int(0, items.length - 1)] as T;
  }

  /** Picks an item with probability proportional to its weight; undefined if all weights are 0. */
  weighted<T>(items: readonly { item: T; weight: number }[]): T | undefined {
    const total = items.reduce((sum, x) => sum + Math.max(0, x.weight), 0);
    if (total <= 0) return undefined;
    let roll = this.float() * total;
    for (const x of items) {
      roll -= Math.max(0, x.weight);
      if (roll < 0) return x.item;
    }
    return items.at(-1)?.item;
  }
}

/** Standard normal sample (Box–Muller). */
export function gaussian(rng: Rng): number {
  const u = Math.max(rng.float(), 1e-12);
  const v = rng.float();
  return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
}
