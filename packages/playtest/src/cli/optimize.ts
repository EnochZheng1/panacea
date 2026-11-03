/**
 * Exploit hunter: random search for the most profitable strategy.
 *
 *   pnpm sim:optimize --candidates 60 --seeds 4 --out optimize-report.md
 */
import { writeFileSync } from 'node:fs';
import { parseArgs } from 'node:util';
import { loadContent } from '@panacea/content';
import { formatOptimizeReport, optimize } from '../harness/optimize.ts';

const { values } = parseArgs({
  options: {
    candidates: { type: 'string', default: '40' },
    seeds: { type: 'string', default: '4' },
    seed: { type: 'string', default: 'search' },
    out: { type: 'string' },
  },
});

const options = {
  content: loadContent(),
  candidates: Number.parseInt(values.candidates, 10),
  seeds: Number.parseInt(values.seeds, 10),
  searchSeed: values.seed,
};
if (!(options.candidates > 0 && options.seeds > 0)) {
  console.error('--candidates and --seeds must be positive integers');
  process.exit(2);
}
const text = formatOptimizeReport(optimize(options), options);
console.log(text);
if (values.out) writeFileSync(values.out, `${text}\n`);
