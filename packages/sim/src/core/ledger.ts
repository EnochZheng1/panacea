import type { Cause } from './causes.ts';
import type { GameState, LedgerEntry, Metric, MonthInProgress } from './state.ts';

export const REPUTATION_MIN = 0;
export const REPUTATION_MAX = 100;

/**
 * The single way to change a player-visible number. Applies the change (with clamping), records
 * a causal delta for the Ledger, and returns the amount actually applied.
 */
export function adjust(
  state: GameState,
  metric: Metric,
  amount: number,
  cause: Cause,
  detail?: string,
): number {
  let applied: number;
  switch (metric) {
    case 'cash': {
      applied = Math.round(amount);
      state.player.cashCents += applied;
      break;
    }
    case 'reputation': {
      const before = state.player.reputation;
      const after = Math.min(REPUTATION_MAX, Math.max(REPUTATION_MIN, before + amount));
      state.player.reputation = after;
      applied = after - before;
      break;
    }
    case 'notoriety': {
      const before = state.player.notoriety;
      const after = Math.max(0, before + amount);
      state.player.notoriety = after;
      applied = after - before;
      break;
    }
  }
  if (applied !== 0) {
    state.month.deltas.push(
      detail === undefined
        ? { metric, amount: applied, cause }
        : { metric, amount: applied, cause, detail },
    );
  }
  return applied;
}

export function openMonth(state: GameState): MonthInProgress {
  return {
    startCashCents: state.player.cashCents,
    startReputation: state.player.reputation,
    startNotoriety: state.player.notoriety,
    deltas: [],
    news: [],
    sales: [],
  };
}

export function closeMonth(state: GameState): LedgerEntry {
  const m = state.month;
  return {
    turn: state.meta.turn,
    date: { ...state.meta.date },
    cashStartCents: m.startCashCents,
    cashEndCents: state.player.cashCents,
    reputationStart: m.startReputation,
    reputationEnd: state.player.reputation,
    notorietyStart: m.startNotoriety,
    notorietyEnd: state.player.notoriety,
    deltas: m.deltas,
    news: m.news,
    sales: m.sales,
  };
}

/** Sums a ledger entry's deltas for one metric, optionally grouped by cause. */
export function sumDeltas(entry: Pick<LedgerEntry, 'deltas'>, metric: Metric): number {
  return entry.deltas.filter((d) => d.metric === metric).reduce((s, d) => s + d.amount, 0);
}

export function deltasByCause(
  entry: Pick<LedgerEntry, 'deltas'>,
  metric: Metric,
): Partial<Record<Cause, number>> {
  const out: Partial<Record<Cause, number>> = {};
  for (const d of entry.deltas) {
    if (d.metric === metric) out[d.cause] = (out[d.cause] ?? 0) + d.amount;
  }
  return out;
}
