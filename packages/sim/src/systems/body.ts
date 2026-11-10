import { DRUG_CLASSES, type ContentPack, type DrugClass } from '@panacea/content';
import { CAUSES } from '../core/causes.ts';
import { tanh } from '../core/dmath.ts';
import { adjust } from '../core/ledger.ts';
import type { Rng } from '../core/rng.ts';
import type { Bought, Customer, DeathCause, GameState, LeaveReason, Phase } from '../core/index.ts';
import { analyzeFormula, type FormulaAnalysis } from './formula.ts';
import { isRuined } from './market.ts';
import {
  acuteDanger,
  dangerThreshold,
  doseContextFor,
  overdoseChance,
  reliefScore,
  zeroTolerance,
} from './pharmacology.ts';
import { recordDeath } from './scrutiny.ts';
import { frightenedAway } from './ward.ts';

/**
 * The body model. Each month, every regular takes the bottles they bought:
 *
 * - Tolerance (per drug class) blunts the felt effect, so the same dose works less over time,
 *   and partly protects against that class's toxicity.
 * - Dependency grows with habit-forming drugs; missing doses cause withdrawal.
 * - Acute danger is weighed against health: a strong enough bottle can kill outright. A drinker
 *   whose tolerance has faded (because you weakened the formula) is suddenly vulnerable again.
 * - Accumulating toxins (heavy metals) build up and wear health down slowly.
 *
 * Satisfaction compares felt relief with what the ailment demands. Unhappy, undependent
 * regulars drift away; dependent ones stay and complain.
 */

export const bodyPhase: Phase = {
  id: 'body',
  run({ state, content, rng, scratch }) {
    const analyses = new Map<string, FormulaAnalysis>();
    const analysisOf = (b: Bought) => {
      let a = analyses.get(b.lotId);
      if (!a) {
        a = analyzeFormula(b.components, b.processId, content);
        analyses.set(b.lotId, a);
      }
      return a;
    };

    let wordOfMouth = 0;
    let complaints = 0;
    const departed = new Set<string>();

    for (const c of state.customers) {
      const outcome = monthForCustomer(
        state,
        content,
        rng,
        c,
        scratch.purchases.get(c.id) ?? [],
        analysisOf,
      );
      if (outcome.complaint) complaints += 1;
      if (outcome.died || outcome.left) departed.add(c.id);
      if (!outcome.died) wordOfMouth += c.satisfaction;
    }

    // Anonymous walk-ins: one bottle, no tolerance. A strong enough bottle still kills.
    for (const w of scratch.walkIns) {
      const a = analysisOf(w.bought);
      const seg = content.byId.segments.get(w.segmentId);
      if (!a || !seg) continue;
      const consumer = content.byId.ailments.get(w.ailmentId)?.consumer ?? 'self';
      const dose = doseContextFor({ consumer, segment: seg.id }, w.bought.potency, content);
      const danger = acuteDanger(a, zeroTolerance(), dose, content);
      if (rng.chance(overdoseChance(danger, seg.baseHealth))) {
        recordDeath(state, content, rng, {
          customerId: null,
          name: '',
          segment: seg.id,
          consumer,
          anonymous: true,
          ailment: w.ailmentId,
          cause: 'overdose',
          productImplicated: true,
          formulaId: w.bought.formulaId,
          badBatch: w.bought.badBatch,
          detectability: a.properties.detectability,
        });
      }
    }

    state.customers = state.customers.filter((c) => !departed.has(c.id));
    state.stats.complaints += complaints;
    state.month.complaints += complaints;
    if (complaints > 0) {
      adjust(
        state,
        'heat',
        complaints * content.config.scrutiny.heatPerComplaint,
        CAUSES.complaints,
      );
      adjust(state, 'reputation', -0.15 * complaints, CAUSES.complaints);
    }
    const wom = Math.max(-2, Math.min(2, 0.08 * wordOfMouth));
    if (wom !== 0) adjust(state, 'reputation', wom, CAUSES.wordOfMouth);
    const { reputationBaseline, reputationDrift } = content.config;
    const drift = (reputationBaseline - state.player.reputation) * reputationDrift;
    if (Math.abs(drift) > 1e-9) adjust(state, 'reputation', drift, CAUSES.reputationDrift);
  },
};

interface MonthOutcome {
  died: boolean;
  left: boolean;
  complaint: boolean;
}

function monthForCustomer(
  state: GameState,
  content: ContentPack,
  rng: Rng,
  c: Customer,
  bought: readonly Bought[],
  analysisOf: (b: Bought) => FormulaAnalysis,
): MonthOutcome {
  const cfg = content.config.body;
  const body = c.body;
  const seg = content.byId.segments.get(c.segment);
  const ailment = content.byId.ailments.get(c.ailment);
  const habitIntake: Record<DrugClass, number> = zeroTolerance();
  let loadIntake = 0;
  let acuteHarm = 0;
  let reliefSum = 0;
  let lastFormula: string | null = null;
  let lastBadBatch = false;
  let lastDetectability: number | undefined;
  let strongestHabit = 0;

  const die = (cause: DeathCause, productImplicated: boolean): MonthOutcome => {
    recordDeath(state, content, rng, {
      customerId: c.id,
      name: c.name,
      segment: c.segment,
      consumer: c.consumer,
      anonymous: false,
      ailment: c.ailment,
      cause,
      productImplicated,
      formulaId: lastFormula ?? recentFormula(state, c),
      badBatch: lastBadBatch,
      ...(lastDetectability === undefined ? {} : { detectability: lastDetectability }),
    });
    if (c.consumer === 'infant') {
      state.stats.customersLost += 1;
      state.month.news.push({
        kind: 'customer_left',
        customerId: c.id,
        name: c.name,
        reason: 'bereaved',
      });
    }
    return { died: c.consumer === 'self', left: c.consumer === 'infant', complaint: false };
  };

  // 1. Each bottle taken this month.
  for (const b of bought) {
    const a = analysisOf(b);
    lastFormula = b.formulaId;
    lastDetectability = a.properties.detectability;
    lastBadBatch = lastBadBatch || b.badBatch;
    strongestHabit = Math.max(strongestHabit, a.properties.habit);
    const dose = doseContextFor(c, b.potency, content);
    reliefSum += reliefScore(a, body.tolerance, c.profile);
    const danger = acuteDanger(a, body.tolerance, dose, content);
    if (rng.chance(overdoseChance(danger, body.health))) return die('overdose', true);
    const harm = Math.max(0, danger - 0.3 * dangerThreshold(body.health)) * cfg.acuteHarm;
    body.health -= harm;
    acuteHarm += harm;
    for (const cls of DRUG_CLASSES)
      habitIntake[cls] += a.byClass[cls].habit * b.potency * dose.doseScale;
    loadIntake += a.properties.accumulation * b.potency * dose.doseScale * dose.vulnerability;
  }

  // 2. Tolerance and dependency.
  for (const cls of DRUG_CLASSES) {
    body.tolerance[cls] =
      body.tolerance[cls] * (1 - cfg.toleranceDecay) + cfg.toleranceGain * habitIntake[cls];
  }
  const habitTotal = DRUG_CLASSES.reduce((s, cls) => s + habitIntake[cls], 0);
  const dependencyDecay = cfg.dependencyDecay * (habitTotal > 0 ? 0.3 : 1);
  const before = body.dependency;
  body.dependency = clamp01(
    before + cfg.dependencyGain * habitTotal * (1 - before) - dependencyDecay * before,
  );
  // A mild bottle (habit under `habitFloor`, as the assay reads it: "none") gives a taste for it,
  // never a craving: it can build dependency up to `mildDependencyCap` and no further.
  if (strongestHabit < cfg.habitFloor && body.dependency > before) {
    body.dependency = Math.max(before, Math.min(body.dependency, cfg.mildDependencyCap));
  }
  const withdrawal = body.dependency > cfg.withdrawalThreshold && habitTotal < body.dependency * 2;
  c.inWithdrawal = withdrawal;
  if (withdrawal) body.health -= cfg.withdrawalHarm;

  // 3. Accumulated toxins, the illness itself, and recovery.
  body.load = body.load * (1 - cfg.loadDecay) + loadIntake;
  const chronicHarm = Math.max(0, body.load - cfg.loadSafe) * cfg.loadHarm;
  body.health -= chronicHarm;
  const drift = ailment?.healthDriftPerMonth ?? 0;
  body.health += drift;
  if (acuteHarm === 0 && chronicHarm === 0 && !withdrawal && body.health < body.baseHealth) {
    body.health += (body.baseHealth - body.health) * cfg.recovery;
  }
  body.health = Math.min(100, body.health);
  const productHarm = acuteHarm + chronicHarm + (withdrawal ? cfg.withdrawalHarm : 0);

  // 4. Death from wasting, or from ordinary causes made likelier by poor health.
  if (body.health <= 0) {
    const cause: DeathCause =
      chronicHarm > 0 && chronicHarm >= -drift
        ? 'poisoning'
        : acuteHarm > 0
          ? 'overdose'
          : 'disease';
    return die(cause, productHarm > 0);
  }
  const weak = body.health < 40 ? 1 + (40 - body.health) / 10 : 1;
  const hazard =
    (seg?.naturalHazard ?? 0.002) *
    (ailment?.mortalityMultiplier ?? 1) *
    (c.consumer === 'infant' ? 2 : 1) *
    weak;
  if (rng.chance(hazard)) return die('natural', body.load > cfg.loadSafe || productHarm > 0);

  // 5. Satisfaction.
  if (bought.length > 0) {
    const felt = (reliefSum / bought.length) * (1 + 0.5 * (bought.length - 1));
    const relief = felt - (ailment?.severity ?? 3);
    c.satisfaction = 0.6 * c.satisfaction + 0.4 * tanh(relief / 3);
  } else {
    c.satisfaction *= 0.8;
  }
  if (withdrawal) c.satisfaction -= 0.4;
  c.satisfaction = Math.max(-1, Math.min(1, c.satisfaction));
  // A regular ruined by the habit has a family, and the family complains.
  const complaint =
    withdrawal || (bought.length > 0 && c.satisfaction < -0.3) || isRuined(c, content);

  // 6. Does the customer keep coming?
  let reason: LeaveReason | null = null;
  if (c.ailmentEndsTurn !== null && state.meta.turn >= c.ailmentEndsTurn && body.dependency < 0.5) {
    reason = 'recovered';
  } else if (
    body.dependency < cfg.withdrawalThreshold &&
    c.satisfaction < -0.4 &&
    rng.chance(cfg.lapseChance)
  ) {
    reason = 'dissatisfied';
  } else if (
    body.dependency < cfg.withdrawalThreshold &&
    (c.lastPurchaseTurn === null
      ? state.meta.turn - c.joinedTurn
      : state.meta.turn - c.lastPurchaseTurn) >= 3
  ) {
    reason = 'drifted';
  } else if (frightenedAway(state, c, content, (p) => rng.chance(p))) {
    // a regular who is not hooked hears what the ward says of the bottle, and goes elsewhere
    reason = 'afraid';
  }
  if (reason) {
    state.stats.customersLost += 1;
    state.month.news.push({ kind: 'customer_left', customerId: c.id, name: c.name, reason });
    return { died: false, left: true, complaint };
  }
  return { died: false, left: false, complaint };
}

/** The formula a customer took recently, to attribute a death in a month without purchases. */
function recentFormula(state: GameState, c: Customer): string | null {
  if (c.lastPurchaseTurn === null || state.meta.turn - c.lastPurchaseTurn > 6) return null;
  return c.lastFormulaId;
}

const clamp01 = (x: number) => Math.max(0, Math.min(1, x));
