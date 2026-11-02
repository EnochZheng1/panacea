/**
 * Batch balance runner.
 *
 *   pnpm sim:batch --bots idle,random,shopkeeper --runs 100 --seed nightly --out report.md
 *
 * Exits non-zero if any run violated an invariant (always) or, with --fail-on-bankrupt-bot,
 * if a named bot went bankrupt.
 */
import { writeFileSync } from 'node:fs';
import { parseArgs } from 'node:util';
import { loadContent } from '@panacea/content';
import { BOTS } from '../bots/index.ts';
import { formatReport, runBatch } from '../harness/batch.ts';

const { values } = parseArgs({
  options: {
    bots: { type: 'string', default: Object.keys(BOTS).join(',') },
    runs: { type: 'string', default: '50' },
    seed: { type: 'string', default: 'batch' },
    backgrounds: { type: 'string' },
    out: { type: 'string' },
    json: { type: 'boolean', default: false },
    fast: { type: 'boolean', default: false },
    'fail-on-bankrupt-bot': { type: 'string' },
  },
});

const content = loadContent();
const botIds = values.bots
  .split(',')
  .map((s) => s.trim())
  .filter(Boolean);
const unknown = botIds.filter((id) => !(id in BOTS));
if (unknown.length > 0) {
  console.error(`Unknown bot(s): ${unknown.join(', ')}. Known: ${Object.keys(BOTS).join(', ')}`);
  process.exit(2);
}
const runs = Number.parseInt(values.runs, 10);
if (!Number.isInteger(runs) || runs < 1) {
  console.error('--runs must be a positive integer');
  process.exit(2);
}

const report = runBatch({
  content,
  bots: botIds.map((id) => (BOTS[id] as () => ReturnType<(typeof BOTS)[string]>)()),
  runs,
  seedPrefix: values.seed,
  ...(values.backgrounds ? { backgroundIds: values.backgrounds.split(',') } : {}),
  checkEveryStep: !values.fast,
});

const text = values.json ? JSON.stringify(report, null, 2) : formatReport(report);
console.log(text);
if (values.out) writeFileSync(values.out, `${text}\n`);

let failed = false;
if (report.violations.length > 0) {
  console.error(`\n${report.violations.length} run(s) violated invariants.`);
  failed = true;
}
const mustSurvive = values['fail-on-bankrupt-bot'];
if (mustSurvive) {
  for (const id of mustSurvive.split(',')) {
    const s = report.summaries.find((x) => x.botId === id);
    if (s && s.bankruptcyRate > 0) {
      console.error(`Bot "${id}" went bankrupt in ${Math.round(s.bankruptcyRate * 100)}% of runs.`);
      failed = true;
    }
  }
}
process.exit(failed ? 1 : 0);
