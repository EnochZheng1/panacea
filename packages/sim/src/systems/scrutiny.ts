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
  const { bribeBaseCents, bribeRisePerBribe, bribeHeatDoubling } = content.config.scrutiny;
  const raw =
    bribeBaseCents *
    (1 + bribeRisePerBribe * state.scrutiny.bribesPaid) *
    (1 + state.scrutiny.heat / bribeHeatDoubling);
  return Math.max(500, Math.round(raw / 500) * 500);
}

/**
 * Chance that a bribe offered now comes out: the coroner's clerk talks. Every fee already paid is
 * one more that someone knows about, and a notorious name is worth more to a talker.
 */
export function bribeExposureChance(state: GameState, content: ContentPack): number {
  const cfg = content.config.scrutiny;
  return Math.min(
    cfg.bribeExposureMax,
    state.scrutiny.bribesPaid * cfg.bribeExposurePerBribe +
      state.player.notoriety * cfg.bribeExposurePerNotoriety,
  );
}

/**
 * The detectability at which the coroner's chemist neither helps nor hurts a proprietor who
 * testifies. Below it a bottle is mild (testimony suits it); above it, strong (silence does).
 */
export const CHEMIST_NEUTRAL_DETECTABILITY = 5;
/** The reply that suits the bottle is always at least this much likelier to clear you. */
export const INQUEST_REPLY_GAP = 0.1;

/**
 * Probability the jury blames your medicine.
 *
 * Testifying puts the formula on the record: a proprietor who speaks for a mild bottle is
 * believed, but the coroner's chemist then knows what to look for, so testimony for a loaded one
 * damns it — and he finds a mis-measured batch. Silence keeps the formula out of court. The reply
 * that suits the bottle's strength is always at least `INQUEST_REPLY_GAP` the better, and it alone
 * is capped two gaps below the usual cap, so the choice matters even when the talk is loudest.
 */
export function implicationChance(
  state: GameState,
  record: DeathRecord,
  approach: 'testify' | 'silent',
): number {
  const heat = state.scrutiny.heat;
  const gap = INQUEST_REPLY_GAP;
  const testify = approach === 'testify';
  const testimonySuits = record.detectability <= CHEMIST_NEUTRAL_DETECTABILITY;
  let raw: number;
  let lo: number;
  let hi: number;
  let chemist: number;
  if (!record.productImplicated) {
    raw = 0.05 + heat / 300 + state.scrutiny.timesImplicated * 0.01;
    lo = 0;
    hi = 0.3;
    chemist = 0;
  } else {
    raw =
      0.25 +
      record.detectability / 25 +
      heat / 200 -
      state.player.reputation / 400 +
      record.boldness * 0.02 +
      // the coroner remembers the label: every verdict against it makes the next one likelier
      state.scrutiny.timesImplicated * 0.03;
    lo = 0.05;
    hi = 0.95;
    chemist = record.detectability * 0.05 - 0.25;
  }
  const suits = testify === testimonySuits;
  let p = raw;
  if (testify) p += testimonySuits ? Math.min(chemist, -gap) : Math.max(chemist, gap);
  // only testimony shows the chemist a mis-measured batch
  if (testify && record.productImplicated && record.badBatch) p += 0.2;
  return suits ? Math.min(hi - 2 * gap, Math.max(lo, p)) : Math.min(hi, Math.max(lo + gap, p));
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

  const implicate = (heat: number, reputation: number, notoriety: number) => {
    adjust(state, 'reputation', -reputation, CAUSES.inquest, record.id);
    adjust(state, 'heat', heat, CAUSES.inquest, record.id);
    adjust(state, 'notoriety', notoriety, CAUSES.inquest, record.id);
    state.scrutiny.timesImplicated += 1;
    state.player.flags['implicated_at_inquest'] = true;
    for (const c of state.customers) c.satisfaction = Math.max(-1, c.satisfaction - 0.3);
  };

  if (approach === 'bribe') {
    const cost = bribeCostCents(state, content);
    if (cost > state.player.cashCents) return 'not enough cash for the coroner';
    const exposure = bribeExposureChance(state, content);
    adjust(state, 'cash', -cost, CAUSES.bribe, record.id);
    state.scrutiny.bribesPaid += 1;
    if (rng.chance(exposure)) {
      // the money is gone, and the jury hears where it went: worse than any verdict
      implicate(30, 15, 10);
      state.month.news.push({ kind: 'bribe_exposed', deathId: record.id });
      outcome = 'implicated';
    } else {
      adjust(state, 'notoriety', 2, CAUSES.inquest, record.id);
      adjust(state, 'heat', -2, CAUSES.inquest, record.id);
      outcome = 'bribed';
    }
  } else if (rng.chance(implicationChance(state, record, approach))) {
    implicate(24, 12, 8);
    outcome = 'implicated';
  } else {
    adjust(state, 'heat', -3, CAUSES.inquest, record.id);
    if (approach === 'testify') adjust(state, 'reputation', 1, CAUSES.inquest, record.id);
    outcome = 'cleared';
  }

  // The chemist's analysis of the formula a proprietor testified to reveals a mis-measured batch.
  if (record.badBatch && approach === 'testify') state.player.flags['suffered_bad_batch'] = true;

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
    const { heatDecayFlat, heatDecayRate, heatDecayHigh } = content.config.scrutiny;
    const heat = state.scrutiny.heat;
    // a scandal burns itself out: the hotter the talk, the faster it moves on to something new
    const decay = Math.min(
      heat,
      heatDecayFlat + heat * heatDecayRate + heatDecayHigh * heat * (heat / 100),
    );
    if (decay > 0) adjust(state, 'heat', -decay, CAUSES.heatDecay);
  },
};
