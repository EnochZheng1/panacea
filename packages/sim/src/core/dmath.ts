/**
 * Portable transcendental functions.
 *
 * `Math.log`, `Math.cos`, `Math.tanh` (and friends) are not required to be correctly rounded,
 * and engines differ in the last bit: V8 and .NET can disagree. The simulation must produce
 * bit-identical results everywhere it runs (the C# port in `sim-cs/` replays the same games), so
 * it uses these instead. They are built only from `+ − × ÷` and `Math.sqrt`/`Math.floor`, which
 * IEEE 754 defines exactly, and the C# port (`DMath.cs`) performs the very same operations in
 * the very same order. Keep the two files in lockstep: any change here must be mirrored there,
 * and the parity fixtures regenerated.
 *
 * Accuracy is a few ulps (relative error well below 1e-14) over the ranges the sim uses.
 */

const LN2 = 0.6931471805599453;
/** ln 2 split so that `k * LN2_HI` is exact for |k| < 2^20 (fdlibm's split). */
const LN2_HI = 6.9314718036912381649e-1;
const LN2_LO = 1.9082149292705877e-10;
const INV_LN2 = 1.4426950408889634;
const SQRT_HALF = 0.7071067811865476;
const SQRT_TWO = 1.4142135623730951;
/** π/2 split so that `k * PIO2_HI` is exact for |k| < 2^20 (fdlibm's pio2_1 / pio2_1t). */
const PIO2_HI = 1.5707963267341256;
const PIO2_LO = 6.077100506506192e-11;
const INV_PIO2 = 0.6366197723675814;
const TWO_64 = 18446744073709551616;
const TWO_M64 = 5.421010862427522e-20;

/** Natural logarithm. */
export function log(x: number): number {
  if (Number.isNaN(x) || x < 0) return Number.NaN;
  if (x === 0) return Number.NEGATIVE_INFINITY;
  if (x === Number.POSITIVE_INFINITY) return x;
  // x = m · 2^e with m in [√½, √2). Scaling by powers of two is exact.
  let m = x;
  let e = 0;
  while (m < TWO_M64) {
    m = m * TWO_64;
    e = e - 64;
  }
  while (m >= TWO_64) {
    m = m * TWO_M64;
    e = e + 64;
  }
  while (m < SQRT_HALF) {
    m = m * 2;
    e = e - 1;
  }
  while (m >= SQRT_TWO) {
    m = m / 2;
    e = e + 1;
  }
  // ln m = 2·atanh(s), s = (m − 1)/(m + 1), |s| ≤ 0.1716.
  const s = (m - 1) / (m + 1);
  const s2 = s * s;
  let term = s;
  let sum = s;
  for (let k = 3; k <= 31; k += 2) {
    term = term * s2;
    sum = sum + term / k;
  }
  return e * LN2 + 2 * sum;
}

/** e^x − 1 for |x| ≤ ln2/2 (Taylor series; accurate even for tiny x). */
function expm1Small(x: number): number {
  let term = x;
  let sum = x;
  for (let i = 2; i <= 18; i++) {
    term = (term * x) / i;
    sum = sum + term;
  }
  return sum;
}

/** e^x. */
export function exp(x: number): number {
  if (Number.isNaN(x)) return x;
  if (x > 709.782712893384) return Number.POSITIVE_INFINITY;
  if (x < -745.1332191019412) return 0;
  // x = k·ln2 + r, |r| ≤ ln2/2.
  const k = Math.floor(x * INV_LN2 + 0.5);
  const r = x - k * LN2_HI - k * LN2_LO;
  let y = 1 + expm1Small(r);
  // Multiply by 2^k one step at a time (exact until the result under- or overflows). Split the
  // scaling so intermediate values stay normal where possible.
  let n = k;
  while (n > 0) {
    y = y * 2;
    n = n - 1;
  }
  while (n < 0) {
    y = y / 2;
    n = n + 1;
  }
  return y;
}

/** Hyperbolic tangent. */
export function tanh(x: number): number {
  if (Number.isNaN(x)) return x;
  const ax = x < 0 ? -x : x;
  if (ax > 22) return x < 0 ? -1 : 1;
  const y = 2 * ax;
  const em1 = y <= 0.34657359027997264 ? expm1Small(y) : exp(y) - 1;
  const t = em1 / (em1 + 2);
  return x < 0 ? -t : t;
}

/** Sine on the reduced range |r| ≤ ~π/4. */
function sinReduced(r: number): number {
  const r2 = r * r;
  let term = r;
  let sum = r;
  for (let i = 1; i <= 10; i++) {
    term = (term * -r2) / (2 * i * (2 * i + 1));
    sum = sum + term;
  }
  return sum;
}

/** Cosine on the reduced range |r| ≤ ~π/4. */
function cosReduced(r: number): number {
  const r2 = r * r;
  let term = 1;
  let sum = 1;
  for (let i = 1; i <= 10; i++) {
    term = (term * -r2) / ((2 * i - 1) * (2 * i));
    sum = sum + term;
  }
  return sum;
}

/** Cosine. Accurate for |x| < 2^20·π/2; the sim only needs [0, 2π). */
export function cos(x: number): number {
  if (!Number.isFinite(x)) return Number.NaN;
  const ax = x < 0 ? -x : x;
  // ax = k·π/2 + r, |r| ≤ ~π/4.
  const k = Math.floor(ax * INV_PIO2 + 0.5);
  const r = ax - k * PIO2_HI - k * PIO2_LO;
  const quadrant = k % 4;
  if (quadrant === 0) return cosReduced(r);
  if (quadrant === 1) return -sinReduced(r);
  if (quadrant === 2) return -cosReduced(r);
  return sinReduced(r);
}
