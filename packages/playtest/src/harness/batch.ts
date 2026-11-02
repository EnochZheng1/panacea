import type { ContentPack } from '@panacea/content';
import type { Bot } from '../bots/types.ts';
import { runGame, type RunResult } from './run.ts';

export interface BatchOptions {
  content: ContentPack;
  bots: Bot[];
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
  medianBankruptcyMonth: number | null;
  medianMonths: number;
  medianFinalCashCents: number;
  medianBottlesSold: number;
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

const median = (xs: number[]): number => {
  if (xs.length === 0) return 0;
  const s = [...xs].sort((a, b) => a - b);
  const mid = Math.floor(s.length / 2);
  return s.length % 2 ? (s[mid] as number) : ((s[mid - 1] as number) + (s[mid] as number)) / 2;
};

export function runBatch(options: BatchOptions): BatchReport {
  const backgrounds = options.backgroundIds ?? [options.content.config.defaultBackground];
  const summaries: BotSummary[] = [];
  const violations: BatchReport['violations'] = [];

  for (const bot of options.bots) {
    const results: RunResult[] = [];
    for (let i = 0; i < options.runs; i++) {
      const backgroundId = backgrounds[i % backgrounds.length] as string;
      const runOptions = {
        content: options.content,
        bot,
        seed: `${options.seedPrefix}-${i}`,
        backgroundId,
        ...(options.checkEveryStep === undefined ? {} : { checkEveryStep: options.checkEveryStep }),
      };
      const r = runGame(runOptions);
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

function summarise(botId: string, results: RunResult[], content: ContentPack): BotSummary {
  const endReasons: Record<string, number> = {};
  for (const r of results) endReasons[r.endReason] = (endReasons[r.endReason] ?? 0) + 1;
  const bankrupt = results.filter((r) => r.endReason === 'bankrupt');
  const unlockReach: Record<string, number> = {};
  const unlockMedianMonth: Record<string, number> = {};
  for (const u of content.unlocks) {
    const reached = results.filter((r) => u.id in r.unlocks);
    unlockReach[u.id] = reached.length / results.length;
    if (reached.length > 0)
      unlockMedianMonth[u.id] = median(reached.map((r) => r.unlocks[u.id] as number));
  }
  return {
    botId,
    runs: results.length,
    endReasons,
    bankruptcyRate: bankrupt.length / results.length,
    medianBankruptcyMonth: bankrupt.length > 0 ? median(bankrupt.map((r) => r.months)) : null,
    medianMonths: median(results.map((r) => r.months)),
    medianFinalCashCents: median(results.map((r) => r.finalCashCents)),
    medianBottlesSold: median(results.map((r) => r.bottlesSold)),
    unlockReach,
    unlockMedianMonth,
    runsWithViolations: results.filter((r) => r.violations.length > 0).length,
    avgMsPerRun: results.reduce((s, r) => s + r.ms, 0) / results.length,
  };
}

const dollars = (cents: number) => `$${(cents / 100).toFixed(2)}`;
const pct = (x: number) => `${Math.round(x * 100)}%`;

/** Renders a batch report as Markdown, for terminals, CI logs and committed balance reports. */
export function formatReport(report: BatchReport): string {
  const lines: string[] = [];
  lines.push(`# Balance report — content ${report.contentVersion}`);
  lines.push('');
  lines.push(
    `Seeds \`${report.seedPrefix}-0..${report.runsPerBot - 1}\`, ${report.runsPerBot} runs per bot.`,
  );
  lines.push('');
  lines.push(
    '| Bot | Bankrupt | Median bankruptcy month | Median months | Median final cash | Median bottles sold | Runs with violations | ms/run |',
  );
  lines.push('|---|---|---|---|---|---|---|---|');
  for (const s of report.summaries) {
    lines.push(
      `| ${s.botId} | ${pct(s.bankruptcyRate)} | ${s.medianBankruptcyMonth ?? '—'} | ${s.medianMonths} | ` +
        `${dollars(s.medianFinalCashCents)} | ${s.medianBottlesSold} | ${s.runsWithViolations} | ${s.avgMsPerRun.toFixed(1)} |`,
    );
  }
  lines.push('');
  lines.push('## Unlock reach (share of runs · median month)');
  lines.push('');
  const unlockIds = Object.keys(report.summaries[0]?.unlockReach ?? {});
  lines.push(`| Unlock | ${report.summaries.map((s) => s.botId).join(' | ')} |`);
  lines.push(`|---|${report.summaries.map(() => '---').join('|')}|`);
  for (const id of unlockIds) {
    const cells = report.summaries.map((s) => {
      const reach = s.unlockReach[id] ?? 0;
      const month = s.unlockMedianMonth[id];
      return reach === 0 ? '—' : `${pct(reach)} · m${month}`;
    });
    lines.push(`| ${id} | ${cells.join(' | ')} |`);
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
