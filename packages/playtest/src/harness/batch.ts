import type { ContentPack } from '@panacea/content';
import type { Bot } from '../bots/types.ts';
import { runGame, type RunResult } from './run.ts';

export interface BatchOptions {
  content: ContentPack;
  /**
   * Bots to run. Pass a factory to get a fresh bot for every run: bots may keep state between
   * turns (an escalating dose), and a shared instance would carry it from one game into the next.
   */
  bots: (Bot | (() => Bot))[];
  runs: number;
  seedPrefix: string;
  backgroundIds?: string[];
  checkEveryStep?: boolean;
}

export interface BotSummary {
  botId: string;
  runs: number;
  endReasons: Record<string, number>;
  bankruptcyRate: number;
  shutDownRate: number;
  medianEndMonth: number | null;
  medianMonths: number;
  medianFinalCashCents: number;
  meanFinalCashCents: number;
  /** 10th and 90th percentiles of final cash: the spread of outcomes. */
  p10FinalCashCents: number;
  p90FinalCashCents: number;
  /** Median cash at the end of months 6 and 12 (a game that ended sooner counts its final cash). */
  medianCashMonth6Cents: number;
  medianCashMonth12Cents: number;
  medianBottlesSold: number;
  medianPeakCustomers: number;
  medianDeaths: number;
  medianProductDeaths: number;
  meanInfantDeaths: number;
  medianInquests: number;
  meanImplicated: number;
  medianBribes: number;
  medianPeakHeat: number;
  /** Unlock id → share of runs that reached it. */
  unlockReach: Record<string, number>;
  /** Unlock id → median month it was reached (among runs that reached it). */
  unlockMedianMonth: Record<string, number>;
  runsWithViolations: number;
  avgMsPerRun: number;
}

export interface BatchReport {
  contentVersion: string;
  seedPrefix: string;
  runsPerBot: number;
  summaries: BotSummary[];
  violations: { botId: string; seed: string; violations: string[] }[];
}

export const median = (xs: number[]): number => {
  if (xs.length === 0) return 0;
  const s = [...xs].sort((a, b) => a - b);
  const mid = Math.floor(s.length / 2);
  return s.length % 2 ? (s[mid] as number) : ((s[mid - 1] as number) + (s[mid] as number)) / 2;
};
/** Nearest-rank percentile (p in 0..100). */
export const percentile = (xs: number[], p: number): number => {
  if (xs.length === 0) return 0;
  const s = [...xs].sort((a, b) => a - b);
  const rank = Math.min(s.length, Math.max(1, Math.ceil((p / 100) * s.length)));
  return s[rank - 1] as number;
};
/** Cash at the end of the given month, or the final cash if the game ended sooner. */
export const cashAtMonth = (r: RunResult, month: number): number =>
  r.finalState.ledger[month - 1]?.cashEndCents ?? r.finalCashCents;
const mean = (xs: number[]) => (xs.length === 0 ? 0 : xs.reduce((a, b) => a + b, 0) / xs.length);

export function runBatch(options: BatchOptions): BatchReport {
  const backgrounds = options.backgroundIds ?? [options.content.config.defaultBackground];
  const summaries: BotSummary[] = [];
  const violations: BatchReport['violations'] = [];

  for (const entry of options.bots) {
    const fresh = typeof entry === 'function' ? entry : () => entry;
    const bot = fresh();
    const results: RunResult[] = [];
    for (let i = 0; i < options.runs; i++) {
      const backgroundId = backgrounds[i % backgrounds.length] as string;
      const r = runGame({
        content: options.content,
        bot: i === 0 ? bot : fresh(),
        seed: `${options.seedPrefix}-${i}`,
        backgroundId,
        ...(options.checkEveryStep === undefined ? {} : { checkEveryStep: options.checkEveryStep }),
      });
      results.push(r);
      if (r.violations.length > 0) {
        violations.push({ botId: bot.id, seed: r.seed, violations: r.violations });
      }
    }
    summaries.push(summarise(bot.id, results, options.content));
  }

  return {
    contentVersion: options.content.config.contentVersion,
    seedPrefix: options.seedPrefix,
    runsPerBot: options.runs,
    summaries,
    violations,
  };
}

export function summarise(botId: string, results: RunResult[], content: ContentPack): BotSummary {
  const endReasons: Record<string, number> = {};
  for (const r of results) endReasons[r.endReason] = (endReasons[r.endReason] ?? 0) + 1;
  const endedEarly = results.filter(
    (r) => r.endReason === 'bankrupt' || r.endReason === 'shut_down',
  );
  const unlockReach: Record<string, number> = {};
  const unlockMedianMonth: Record<string, number> = {};
  for (const u of content.unlocks) {
    const reached = results.filter((r) => u.id in r.unlocks);
    unlockReach[u.id] = reached.length / results.length;
    if (reached.length > 0) {
      unlockMedianMonth[u.id] = median(reached.map((r) => r.unlocks[u.id] as number));
    }
  }
  const pick = (f: (r: RunResult) => number) => results.map(f);
  return {
    botId,
    runs: results.length,
    endReasons,
    bankruptcyRate: (endReasons['bankrupt'] ?? 0) / results.length,
    shutDownRate: (endReasons['shut_down'] ?? 0) / results.length,
    medianEndMonth: endedEarly.length > 0 ? median(endedEarly.map((r) => r.months)) : null,
    medianMonths: median(pick((r) => r.months)),
    medianFinalCashCents: median(pick((r) => r.finalCashCents)),
    meanFinalCashCents: mean(pick((r) => r.finalCashCents)),
    p10FinalCashCents: percentile(
      pick((r) => r.finalCashCents),
      10,
    ),
    p90FinalCashCents: percentile(
      pick((r) => r.finalCashCents),
      90,
    ),
    medianCashMonth6Cents: median(pick((r) => cashAtMonth(r, 6))),
    medianCashMonth12Cents: median(pick((r) => cashAtMonth(r, 12))),
    medianBottlesSold: median(pick((r) => r.bottlesSold)),
    medianPeakCustomers: median(pick((r) => r.peakCustomers)),
    medianDeaths: median(pick((r) => r.deaths)),
    medianProductDeaths: median(pick((r) => r.productDeaths)),
    meanInfantDeaths: mean(pick((r) => r.infantDeaths)),
    medianInquests: median(pick((r) => r.inquests)),
    meanImplicated: mean(pick((r) => r.implicated)),
    medianBribes: median(pick((r) => r.bribes)),
    medianPeakHeat: median(pick((r) => r.peakHeat)),
    unlockReach,
    unlockMedianMonth,
    runsWithViolations: results.filter((r) => r.violations.length > 0).length,
    avgMsPerRun: mean(pick((r) => r.ms)),
  };
}

export const dollars = (cents: number) =>
  `${cents < 0 ? '-' : ''}$${Math.abs(cents / 100).toLocaleString('en-US', { maximumFractionDigits: 0 })}`;
const pct = (x: number) => `${Math.round(x * 100)}%`;
const n1 = (x: number) => (Number.isInteger(x) ? String(x) : x.toFixed(1));

/** Renders a batch report as Markdown, for terminals, CI logs and committed balance reports. */
export function formatReport(report: BatchReport): string {
  const lines: string[] = [];
  const row = (cells: (string | number)[]) => lines.push(`| ${cells.join(' | ')} |`);
  const header = (cells: string[]) => {
    row(cells);
    lines.push(`|${cells.map(() => '---').join('|')}|`);
  };

  lines.push(`# Balance report — content ${report.contentVersion}`);
  lines.push('');
  lines.push(
    `Seeds \`${report.seedPrefix}-0..${report.runsPerBot - 1}\`, ${report.runsPerBot} runs per bot.`,
  );
  lines.push('');
  lines.push('## Business');
  lines.push('');
  header([
    'Bot',
    'Bankrupt',
    'Shut down',
    'Median end month (early endings)',
    'Median final cash',
    'Mean final cash',
    'p10 final cash',
    'p90 final cash',
    'Cash m6 (median)',
    'Cash m12 (median)',
    'Median bottles sold',
    'Median peak customers',
    'ms/run',
  ]);
  for (const s of report.summaries) {
    row([
      s.botId,
      pct(s.bankruptcyRate),
      pct(s.shutDownRate),
      s.medianEndMonth ?? '—',
      dollars(s.medianFinalCashCents),
      dollars(s.meanFinalCashCents),
      dollars(s.p10FinalCashCents),
      dollars(s.p90FinalCashCents),
      dollars(s.medianCashMonth6Cents),
      dollars(s.medianCashMonth12Cents),
      s.medianBottlesSold,
      s.medianPeakCustomers,
      s.avgMsPerRun.toFixed(0),
    ]);
  }
  lines.push('');
  lines.push('## Human cost and scrutiny');
  lines.push('');
  header([
    'Bot',
    'Median deaths',
    'Caused by product',
    'Infant deaths (mean)',
    'Inquests',
    'Implicated (mean)',
    'Bribes',
    'Peak heat',
  ]);
  for (const s of report.summaries) {
    row([
      s.botId,
      n1(s.medianDeaths),
      n1(s.medianProductDeaths),
      s.meanInfantDeaths.toFixed(1),
      n1(s.medianInquests),
      s.meanImplicated.toFixed(1),
      n1(s.medianBribes),
      s.medianPeakHeat.toFixed(0),
    ]);
  }
  lines.push('');
  lines.push('## Unlock reach (share of runs · median month)');
  lines.push('');
  header(['Unlock', ...report.summaries.map((s) => s.botId)]);
  for (const id of Object.keys(report.summaries[0]?.unlockReach ?? {})) {
    row([
      id,
      ...report.summaries.map((s) => {
        const reach = s.unlockReach[id] ?? 0;
        return reach === 0 ? '—' : `${pct(reach)} · m${s.unlockMedianMonth[id]}`;
      }),
    ]);
  }
  if (report.violations.length > 0) {
    lines.push('');
    lines.push('## Invariant violations');
    for (const v of report.violations.slice(0, 20)) {
      lines.push(`- **${v.botId}** seed \`${v.seed}\`: ${v.violations.slice(0, 3).join('; ')}`);
    }
  }
  return lines.join('\n');
}
