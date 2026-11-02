import type { ContentPack } from '@panacea/content';
import { compareDates, dateIndex } from './core/date.ts';
import { REPUTATION_MAX, REPUTATION_MIN, sumDeltas } from './core/ledger.ts';
import type { GameState } from './core/state.ts';

const EPS = 1e-6;

/**
 * Checks rules that must hold in every reachable state. Returns human-readable violations
 * (empty when healthy). Run by tests after every step and by the batch harness on every run.
 */
export function checkInvariants(state: GameState, content: ContentPack): string[] {
  const v: string[] = [];
  const { player, lab, meta } = state;

  if (!Number.isSafeInteger(player.cashCents))
    v.push(`cash is not an integer: ${player.cashCents}`);
  if (!(player.reputation >= REPUTATION_MIN - EPS && player.reputation <= REPUTATION_MAX + EPS)) {
    v.push(`reputation out of range: ${player.reputation}`);
  }
  if (!(player.notoriety >= -EPS)) v.push(`notoriety negative: ${player.notoriety}`);

  for (const [id, qty] of Object.entries(lab.inventory)) {
    if (!Number.isFinite(qty) || qty < -EPS) v.push(`inventory "${id}" invalid: ${qty}`);
    if (!content.byId.ingredients.has(id)) v.push(`inventory holds unknown ingredient "${id}"`);
  }
  for (const [id, qty] of Object.entries(lab.stock)) {
    if (!Number.isInteger(qty) || qty < 0) v.push(`stock "${id}" invalid: ${qty}`);
    if (!lab.formulas.some((f) => f.id === id)) v.push(`stock for unknown formula "${id}"`);
  }
  if (lab.formulas.length > content.config.maxFormulas) v.push('too many formulas');

  if (meta.date.month < 1 || meta.date.month > 12) v.push(`invalid month ${meta.date.month}`);
  if (dateIndex(meta.date) - dateIndex(content.config.startDate) !== meta.turn) {
    v.push(`turn ${meta.turn} does not match date`);
  }
  if (compareDates(meta.date, content.config.endDate) > 0) v.push('date is past the end date');
  if (meta.status === 'ended' && meta.endReason === null) v.push('ended without a reason');
  if (meta.status === 'running' && meta.endReason !== null) v.push('running with an end reason');

  // The Ledger must explain every change: deltas sum to the actual change, months chain up.
  const entries =
    meta.status === 'running' ? [...state.ledger, openMonthAsEntry(state)] : [...state.ledger];
  entries.forEach((e, i) => {
    if (Math.abs(sumDeltas(e, 'cash') - (e.cashEndCents - e.cashStartCents)) > EPS) {
      v.push(`ledger turn ${e.turn}: cash deltas do not explain the change`);
    }
    if (Math.abs(sumDeltas(e, 'reputation') - (e.reputationEnd - e.reputationStart)) > EPS) {
      v.push(`ledger turn ${e.turn}: reputation deltas do not explain the change`);
    }
    if (Math.abs(sumDeltas(e, 'notoriety') - (e.notorietyEnd - e.notorietyStart)) > EPS) {
      v.push(`ledger turn ${e.turn}: notoriety deltas do not explain the change`);
    }
    const prev = entries[i - 1];
    if (prev && prev.cashEndCents !== e.cashStartCents) {
      v.push(`ledger turn ${e.turn}: cash does not continue from the previous month`);
    }
  });

  for (const p of state.events.pending) {
    if (!content.byId.events.has(p.eventId)) v.push(`pending unknown event "${p.eventId}"`);
  }
  if (state.events.pending.length > content.config.maxEventsPerMonth)
    v.push('too many pending events');
  return v;
}

/** The open month expressed like a ledger entry. */
function openMonthAsEntry(state: GameState) {
  return {
    turn: state.meta.turn,
    cashStartCents: state.month.startCashCents,
    cashEndCents: state.player.cashCents,
    reputationStart: state.month.startReputation,
    reputationEnd: state.player.reputation,
    notorietyStart: state.month.startNotoriety,
    notorietyEnd: state.player.notoriety,
    deltas: state.month.deltas,
  };
}
