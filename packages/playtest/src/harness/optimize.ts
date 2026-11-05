import type { ContentPack } from '@panacea/content';
import { Rng } from '@panacea/sim';
import { policyBot, type Policy } from '../bots/policy.ts';
import { median } from './batch.ts';
import { runGame, type RunResult } from './run.ts';

/**
 * The exploit hunter: random search over the policy space, scoring each candidate by money
 * alone across several seeds. If the richest strategy is also the deadliest, the game has a
 * dominant "max narcotics" strategy that the balance needs to address.
 */

const NARCOTICS = [null, 'laudanum', 'morphine', 'coca_extract'] as const;
const CLAIM_SETS: string[][] = [
  [],
  ['soothes_nerves'],
  ['soothes_nerves', 'banishes_rheumatism', 'womens_friend'],
  ['cures_everything', 'cures_consumption', 'teething_comfort', 'endorsed_by_physicians'],
];
const EXTRAS: Policy['extras'][] = [
  [],
  [{ ingredientId: 'valerian', amount: 1 }],
  [
    { ingredientId: 'capsicum', amount: 0.3 },
    { ingredientId: 'gentian', amount: 0.2 },
  ],
  [
    { ingredientId: 'calomel', amount: 0.3 },
    { ingredientId: 'aloe', amount: 0.4 },
  ],
];

const CAPACITY = ['shop_next_door', 'larger_copper', 'fourth_still'];
const INVEST_SETS: string[][] = [
  [],
  ['branch_shop', 'wholesale_contract', 'druggists_order', 'almanac', ...CAPACITY],
  ['laboratory', 'branch_shop', 'almanac', 'wholesale_contract', 'druggists_order', ...CAPACITY],
  ['branch_shop', 'almanac', 'shop_next_door'],
  ['almanac', ...CAPACITY],
];

export function randomPolicy(rng: Rng, id: string): Policy {
  const narcotic = rng.pick(NARCOTICS);
  const maxOz = narcotic === 'laudanum' ? 1.2 : narcotic === 'morphine' ? 0.15 : 0.8;
  const oz = Math.round(rng.float() * maxOz * 100) / 100;
  const escalate = narcotic && rng.chance(0.4);
  const deescalate = narcotic && rng.chance(0.4);
  return {
    id,
    description: 'optimizer candidate',
    narcotic: narcotic ? { ingredientId: narcotic, oz } : null,
    alcoholOz: Math.round(rng.float() * 3 * 10) / 10,
    extras: rng.pick(EXTRAS),
    priceCents: rng.int(30, 200),
    stockCover: 1.3,
    minStock: 15,
    claims: rng.pick(CLAIM_SETS),
    adSpendCents: rng.pick([0, 0, 300, 800, 1500, 3000]),
    inquest: rng.pick(['silent', 'testify', 'bribe'] as const),
    events: rng.pick(['default', 'first'] as const),
    invest: rng.pick(INVEST_SETS),
    ...(escalate ? { escalate: { stepOz: 0.05, maxOz: Math.max(oz, maxOz) } } : {}),
    ...(deescalate
      ? {
          deescalate: {
            heatAbove: rng.int(20, 60),
            stepOz: 0.05,
            minOz: Math.round(oz * 50) / 100,
          },
        }
      : {}),
  };
}

export interface CandidateResult {
  policy: Policy;
  meanCashCents: number;
  medianCashCents: number;
  meanDeaths: number;
  meanProductDeaths: number;
  earlyEndRate: number;
  runs: RunResult[];
}

export interface OptimizeOptions {
  content: ContentPack;
  candidates: number;
  seeds: number;
  searchSeed: string;
}

export function optimize(options: OptimizeOptions): CandidateResult[] {
  const rng = Rng.fromSeed(`optimize:${options.searchSeed}`);
  const results: CandidateResult[] = [];
  for (let i = 0; i < options.candidates; i++) {
    const policy = randomPolicy(rng, `candidate_${i}`);
    const runs = Array.from({ length: options.seeds }, (_, s) =>
      runGame({
        content: options.content,
        bot: policyBot(policy),
        seed: `opt-${options.searchSeed}-${s}`,
        checkEveryStep: false,
      }),
    );
    const mean = (f: (r: RunResult) => number) => runs.reduce((a, r) => a + f(r), 0) / runs.length;
    results.push({
      policy,
      meanCashCents: mean((r) => r.finalCashCents),
      medianCashCents: median(runs.map((r) => r.finalCashCents)),
      meanDeaths: mean((r) => r.deaths),
      meanProductDeaths: mean((r) => r.productDeaths),
      earlyEndRate: mean((r) => (r.endReason === 'timeline_end' ? 0 : 1)),
      runs,
    });
  }
  return results.sort((a, b) => b.meanCashCents - a.meanCashCents);
}

/** Pearson correlation, for "do the richest strategies kill the most?" */
export function correlation(xs: number[], ys: number[]): number {
  const n = xs.length;
  if (n < 2) return 0;
  const mx = xs.reduce((a, b) => a + b, 0) / n;
  const my = ys.reduce((a, b) => a + b, 0) / n;
  let sxy = 0,
    sxx = 0,
    syy = 0;
  for (let i = 0; i < n; i++) {
    const dx = (xs[i] as number) - mx;
    const dy = (ys[i] as number) - my;
    sxy += dx * dy;
    sxx += dx * dx;
    syy += dy * dy;
  }
  return sxx === 0 || syy === 0 ? 0 : sxy / Math.sqrt(sxx * syy);
}

const usd = (c: number) => `$${Math.round(c / 100).toLocaleString('en-US')}`;

export function describePolicy(p: Policy): string {
  const narcotic = p.narcotic ? `${p.narcotic.ingredientId} ${p.narcotic.oz}oz` : 'no narcotic';
  const extras = p.extras.map((e) => e.ingredientId).join('+') || 'none';
  const adapt = [
    p.escalate ? 'escalates' : '',
    p.deescalate ? `cuts at heat>${p.deescalate.heatAbove}` : '',
  ]
    .filter(Boolean)
    .join(', ');
  return (
    `${narcotic}, alcohol ${p.alcoholOz}oz, extras ${extras}, ${p.priceCents}¢, ` +
    `ads ${usd(p.adSpendCents)}/mo, claims [${p.claims.join(', ')}], inquests: ${p.inquest}` +
    (adapt ? `, ${adapt}` : '') +
    (p.invest?.length ? `, invests [${p.invest.join(', ')}]` : '')
  );
}

export function formatOptimizeReport(results: CandidateResult[], options: OptimizeOptions): string {
  const lines: string[] = [];
  lines.push(`# Optimizer report — content ${options.content.config.contentVersion}`);
  lines.push('');
  lines.push(
    `${options.candidates} random strategies × ${options.seeds} seeds, scored by mean final cash ` +
      `(search seed \`${options.searchSeed}\`).`,
  );
  lines.push('');
  const r = correlation(
    results.map((x) => x.meanCashCents),
    results.map((x) => x.meanProductDeaths),
  );
  lines.push(`**Correlation between wealth and deaths caused by the product: ${r.toFixed(2)}**`);
  lines.push('');
  lines.push('| Rank | Mean cash | Product deaths | All deaths | Ended early | Strategy |');
  lines.push('|---|---|---|---|---|---|');
  const show = [...results.slice(0, 10), ...results.slice(-3)];
  show.forEach((x) => {
    const rank = results.indexOf(x) + 1;
    lines.push(
      `| ${rank} | ${usd(x.meanCashCents)} | ${x.meanProductDeaths.toFixed(1)} | ${x.meanDeaths.toFixed(1)} | ` +
        `${Math.round(x.earlyEndRate * 100)}% | ${describePolicy(x.policy)} |`,
    );
  });
  return lines.join('\n');
}
