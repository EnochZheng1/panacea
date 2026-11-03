import { DRUG_CLASSES, type ContentPack } from '@panacea/content';
import { compareDates, dateIndex } from './core/date.ts';
import { HEAT_MAX, REPUTATION_MAX, REPUTATION_MIN, sumDeltas } from './core/ledger.ts';
import type { GameState } from './core/state.ts';

const EPS = 1e-6;

/**
 * Checks rules that must hold in every reachable state. Returns human-readable violations
 * (empty when healthy). Run by tests after every step and by the batch harness on every run.
 */
export function checkInvariants(state: GameState, content: ContentPack): string[] {
  const v: string[] = [];
  const { player, lab, meta, scrutiny } = state;

  if (!Number.isSafeInteger(player.cashCents)) {
    v.push(`cash is not an integer: ${player.cashCents}`);
  }
  if (!(player.reputation >= REPUTATION_MIN - EPS && player.reputation <= REPUTATION_MAX + EPS)) {
    v.push(`reputation out of range: ${player.reputation}`);
  }
  if (!(player.notoriety >= -EPS)) v.push(`notoriety negative: ${player.notoriety}`);
  if (!(scrutiny.heat >= -EPS && scrutiny.heat <= HEAT_MAX + EPS)) {
    v.push(`heat out of range: ${scrutiny.heat}`);
  }

  // Lab
  for (const [id, qty] of Object.entries(lab.inventory)) {
    if (!Number.isFinite(qty) || qty < -EPS) v.push(`inventory "${id}" invalid: ${qty}`);
    if (!content.byId.ingredients.has(id)) v.push(`inventory holds unknown ingredient "${id}"`);
  }
  for (const lot of lab.stock) {
    if (!Number.isInteger(lot.bottles) || lot.bottles <= 0) {
      v.push(`lot "${lot.id}" has invalid bottles: ${lot.bottles}`);
    }
    if (!(lot.potency > 0) || !Number.isFinite(lot.potency)) {
      v.push(`lot "${lot.id}" has invalid potency: ${lot.potency}`);
    }
    if (!lab.formulas.some((f) => f.id === lot.formulaId)) {
      v.push(`lot "${lot.id}" is of unknown formula "${lot.formulaId}"`);
    }
  }
  if (lab.formulas.length > content.config.maxFormulas) v.push('too many formulas');
  for (const f of lab.formulas) {
    if (f.claims.length > content.config.maxClaimsPerFormula)
      v.push(`formula "${f.id}" has too many claims`);
  }

  // Customers
  const ids = new Set<string>();
  for (const c of state.customers) {
    if (ids.has(c.id)) v.push(`duplicate customer "${c.id}"`);
    ids.add(c.id);
    const b = c.body;
    if (!(b.health > 0 && b.health <= 100 + EPS))
      v.push(`customer "${c.id}" health invalid: ${b.health}`);
    if (!(b.dependency >= 0 && b.dependency <= 1)) v.push(`customer "${c.id}" dependency invalid`);
    if (!(b.load >= -EPS) || !Number.isFinite(b.load)) v.push(`customer "${c.id}" load invalid`);
    for (const cls of DRUG_CLASSES) {
      if (!(b.tolerance[cls] >= -EPS) || !Number.isFinite(b.tolerance[cls])) {
        v.push(`customer "${c.id}" tolerance "${cls}" invalid`);
      }
    }
    if (!(c.satisfaction >= -1 - EPS && c.satisfaction <= 1 + EPS)) {
      v.push(`customer "${c.id}" satisfaction out of range`);
    }
    if (!content.byId.segments.has(c.segment)) v.push(`customer "${c.id}" has unknown segment`);
    if (!content.byId.ailments.has(c.ailment)) v.push(`customer "${c.id}" has unknown ailment`);
  }
  if (
    state.deaths.some(
      (d) => d.customerId !== null && ids.has(d.customerId) && d.consumer === 'self',
    )
  ) {
    v.push('a dead customer is still a customer');
  }

  // Time
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
    const checks: [string, number, number][] = [
      ['cash', e.cashStartCents, e.cashEndCents],
      ['reputation', e.reputationStart, e.reputationEnd],
      ['notoriety', e.notorietyStart, e.notorietyEnd],
      ['heat', e.heatStart, e.heatEnd],
    ];
    for (const [metric, start, end] of checks) {
      const sum = sumDeltas(e, metric as 'cash' | 'reputation' | 'notoriety' | 'heat');
      if (Math.abs(sum - (end - start)) > EPS) {
        v.push(`ledger turn ${e.turn}: ${metric} deltas do not explain the change`);
      }
    }
    const prev = entries[i - 1];
    if (prev && prev.cashEndCents !== e.cashStartCents) {
      v.push(`ledger turn ${e.turn}: cash does not continue from the previous month`);
    }
  });

  // Events
  let randomPending = 0;
  for (const p of state.events.pending) {
    const ev = content.byId.events.get(p.eventId);
    if (!ev) {
      v.push(`pending unknown event "${p.eventId}"`);
      continue;
    }
    if (ev.trigger === 'random') randomPending++;
    if (ev.trigger === 'system') {
      const death = state.deaths.find((d) => d.id === p.deathId);
      if (!death || death.inquest !== 'pending')
        v.push(`inquest "${p.instanceId}" has no pending death`);
    }
  }
  if (randomPending > content.config.maxEventsPerMonth) v.push('too many pending events');
  for (const d of state.deaths) {
    if (d.inquest === 'pending' && !state.events.pending.some((p) => p.deathId === d.id)) {
      v.push(`death "${d.id}" awaits an inquest that is not scheduled`);
    }
  }
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
    heatStart: state.month.startHeat,
    heatEnd: state.scrutiny.heat,
    deltas: state.month.deltas,
  };
}
