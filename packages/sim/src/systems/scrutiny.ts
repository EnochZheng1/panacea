import type { ContentPack } from '@panacea/content';
import { CAUSES } from '../core/causes.ts';
import { adjust } from '../core/ledger.ts';
import type { Rng } from '../core/rng.ts';
import type { DeathCause, DeathRecord, GameState, InquestOutcome, Phase } from '../core/index.ts';
import { claimBoldness, analyzeFormula } from './formula.ts';

/**
 * Scrutiny: heat, deaths and coroner's inquests.
 *
 * Heat is how much the neighbourhood, the press and the authorities are talking about you.
 * Deaths raise it (infants most of all); it fades slowly. When a death is investigated, a
 * coroner's jury may blame your medicine — more likely the more detectable the drug, the bolder
 * the label, the hotter the talk and the worse your name. Bribery works, but each bribe makes
 * the next one dearer.
 */

export const INQUEST_EVENT_ID = 'inquest';

export interface DeathInput {
  customerId: string | null;
  name: string;
  segment: string;
  consumer: 'self' | 'infant';
  anonymous: boolean;
  cause: DeathCause;
  productImplicated: boolean;
  formulaId: string | null;
  badBatch: boolean;
  /** Detectability of the bottle actually taken, if known. */
  detectability?: number;
}

/** Records a death in the human ledger, raises heat, and may convene an inquest. */
export function recordDeath(
  state: GameState,
  content: ContentPack,
  rng: Rng,
  input: DeathInput,
): DeathRecord {
  const cfg = content.config.scrutiny;
  const formula = input.formulaId
    ? state.lab.formulas.find((f) => f.id === input.formulaId)
    : undefined;
  const record: DeathRecord = {
    id: `death_${state.meta.nextId++}`,
    turn: state.meta.turn,
    date: { ...state.meta.date },
    ...input,
    detectability:
      input.detectability ??
      (formula
        ? analyzeFormula(formula.components, formula.processId, content).properties.detectability
        : 0),
    boldness: formula ? claimBoldness(formula.claims, content) : 0,
    inquest: 'none',
    verdict: null,
  };
  state.deaths.push(record);
  state.month.news.push({ kind: 'death', deathId: record.id });

  // How much talk a death causes depends on how it looked: a poisoning is visibly different
  // from an old woman dying in her sleep.
  const base = input.anonymous
    ? cfg.heatPerAnonymousDeath
    : input.consumer === 'infant'
      ? cfg.heatPerInfantDeath
      : cfg.heatPerDeath;
  adjust(state, 'heat', input.productImplicated ? base : base / 2, CAUSES.death, record.id);
  if (!input.anonymous) adjust(state, 'reputation', -1, CAUSES.death, record.id);

  // Was the medicine a regular part of this person's life? Then the coroner may take an interest.
  if (input.formulaId) {
    let p =
      cfg.inquestBaseChance +
      state.scrutiny.heat / 150 +
      (input.consumer === 'infant' ? 0.25 : 0) +
      (input.productImplicated ? 0.15 : 0);
    if (input.anonymous) p *= 0.6;
    if (rng.chance(Math.min(0.95, p))) {
      record.inquest = 'pending';
      state.events.pending.push({
        instanceId: `event_${state.meta.nextId++}`,
        eventId: INQUEST_EVENT_ID,
        drawnTurn: state.meta.turn,
        deathId: record.id,
      });
    }
  }
  return record;
}

/**
 * What the coroner currently charges. Rises with every bribe and with the heat, and is asked
 * in round sums: a coroner names his price to the nearest five dollars.
 */
export function bribeCostCents(state: GameState, content: ContentPack): number {
  const base = content.config.scrutiny.bribeBaseCents;
  const raw = base * (1 + 0.5 * state.scrutiny.bribesPaid) * (1 + state.scrutiny.heat / 50);
  return Math.max(500, Math.round(raw / 500) * 500);
}

/** Probability the jury blames your medicine. */
export function implicationChance(
  state: GameState,
  record: DeathRecord,
  approach: 'testify' | 'silent',
): number {
  const heat = state.scrutiny.heat;
  if (!record.productImplicated) {
    return Math.min(
      0.3,
      Math.max(
        0,
        0.05 +
          heat / 300 +
          state.scrutiny.timesImplicated * 0.01 -
          (approach === 'testify' ? 0.04 : 0),
      ),
    );
  }
  const p =
    0.25 +
    record.detectability / 25 +
    heat / 200 -
    state.player.reputation / 400 +
    record.boldness * 0.02 +
    (record.badBatch ? 0.2 : 0) +
    // the coroner remembers the label: every verdict against it makes the next one likelier
    state.scrutiny.timesImplicated * 0.03 -
    (approach === 'testify' ? 0.15 : 0);
  return Math.min(0.95, Math.max(0.05, p));
}

/** Resolves an inquest. Returns an error message if the approach is not possible. */
export function resolveInquest(
  state: GameState,
  content: ContentPack,
  rng: Rng,
  deathId: string | undefined,
  approach: 'testify' | 'silent' | 'bribe',
): string | null {
  const record = state.deaths.find((d) => d.id === deathId);
  if (!record || record.inquest !== 'pending') return 'no inquest is pending for that death';
  const cfg = content.config.scrutiny;
  let outcome: InquestOutcome;

  if (approach === 'bribe') {
    const cost = bribeCostCents(state, content);
    if (cost > state.player.cashCents) return 'not enough cash for the coroner';
    adjust(state, 'cash', -cost, CAUSES.bribe, record.id);
    adjust(state, 'notoriety', 2, CAUSES.inquest, record.id);
    adjust(state, 'heat', -2, CAUSES.inquest, record.id);
    state.scrutiny.bribesPaid += 1;
    outcome = 'bribed';
  } else if (rng.chance(implicationChance(state, record, approach))) {
    // a proprietor who would not come and speak for his bottle looks the guiltier for it
    adjust(state, 'reputation', -12, CAUSES.inquest, record.id);
    adjust(state, 'heat', approach === 'silent' ? 28 : 20, CAUSES.inquest, record.id);
    adjust(state, 'notoriety', 8, CAUSES.inquest, record.id);
    state.scrutiny.timesImplicated += 1;
    state.player.flags['implicated_at_inquest'] = true;
    for (const c of state.customers) c.satisfaction = Math.max(-1, c.satisfaction - 0.3);
    outcome = 'implicated';
  } else {
    adjust(state, 'heat', -3, CAUSES.inquest, record.id);
    if (approach === 'testify') adjust(state, 'reputation', 1, CAUSES.inquest, record.id);
    outcome = 'cleared';
  }

  // A chemist's analysis at an honest inquest reveals a mis-measured batch.
  if (record.badBatch && approach !== 'bribe') state.player.flags['suffered_bad_batch'] = true;

  record.inquest = outcome;
  record.verdict =
    outcome === 'implicated' ? 'proprietary_poisoning' : rng.pick(cfg.benignVerdicts);
  state.scrutiny.inquestsHeld += 1;
  state.player.flags['survived_inquest'] = true;
  state.month.news.push({ kind: 'inquest', deathId: record.id, outcome, verdict: record.verdict });
  return null;
}

/** Turn phase: talk dies down over time. */
export const heatDecayPhase: Phase = {
  id: 'heat_decay',
  run({ state, content }) {
    const { heatDecayFlat, heatDecayRate } = content.config.scrutiny;
    const decay = Math.min(
      state.scrutiny.heat,
      heatDecayFlat + state.scrutiny.heat * heatDecayRate,
    );
    if (decay > 0) adjust(state, 'heat', -decay, CAUSES.heatDecay);
  },
};
