import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { dmath, Rng } from '../src/index.ts';

const rel = (a: number, b: number) => (b === 0 ? Math.abs(a) : Math.abs(a - b) / Math.abs(b));

describe('dmath (portable transcendental functions)', () => {
  it('log matches Math.log closely', () => {
    const r = Rng.fromSeed('dmath-log');
    for (let i = 0; i < 20_000; i++) {
      const u = Math.max(r.float(), 1e-12);
      expect(rel(dmath.log(u), Math.log(u))).toBeLessThan(1e-14);
    }
    for (const x of [5e-324, 1e-300, 1e-12, 0.5, 1, 2, Math.E, 10, 1e300]) {
      expect(rel(dmath.log(x), Math.log(x))).toBeLessThan(1e-14);
    }
    expect(dmath.log(1)).toBe(0);
    expect(dmath.log(0)).toBe(Number.NEGATIVE_INFINITY);
    expect(dmath.log(-1)).toBeNaN();
  });

  it('cos matches Math.cos closely over [0, 2π)', () => {
    const r = Rng.fromSeed('dmath-cos');
    for (let i = 0; i < 20_000; i++) {
      const x = 2 * Math.PI * r.float();
      expect(Math.abs(dmath.cos(x) - Math.cos(x))).toBeLessThan(1e-15);
    }
    expect(dmath.cos(0)).toBe(1);
    expect(dmath.cos(-1)).toBe(dmath.cos(1));
  });

  it('tanh matches Math.tanh closely', () => {
    const r = Rng.fromSeed('dmath-tanh');
    for (let i = 0; i < 20_000; i++) {
      const x = (r.float() - 0.5) * 40;
      expect(rel(dmath.tanh(x), Math.tanh(x))).toBeLessThan(1e-14);
      const tiny = (r.float() - 0.5) * 1e-3;
      expect(rel(dmath.tanh(tiny), Math.tanh(tiny))).toBeLessThan(1e-14);
    }
    expect(dmath.tanh(0)).toBe(0);
    expect(dmath.tanh(100)).toBe(1);
    expect(dmath.tanh(-100)).toBe(-1);
  });

  it('the simulation uses only portable math', () => {
    // Engine-dependent functions would break bit-for-bit parity with the C# port.
    const forbidden =
      /Math\.(log|log2|log10|log1p|exp|expm1|pow|sin|cos|tan|asin|acos|atan|atan2|sinh|cosh|tanh|asinh|acosh|atanh|cbrt|hypot|random)\b|localeCompare|toLocale|Intl\./;
    const root = join(import.meta.dirname, '..', 'src');
    const files: string[] = [];
    const walk = (dir: string) => {
      for (const name of readdirSync(dir)) {
        const path = join(dir, name);
        if (statSync(path).isDirectory()) walk(path);
        else if (name.endsWith('.ts')) files.push(path);
      }
    };
    walk(root);
    const offenders = files.flatMap((f) =>
      readFileSync(f, 'utf8')
        .split('\n')
        .map((line, i) => ({ line: line.replace(/\/\/.*$|^\s*\*.*$/, ''), at: `${f}:${i + 1}` }))
        .filter((l) => forbidden.test(l.line))
        .map((l) => l.at),
    );
    expect(offenders).toEqual([]);
  });
});
