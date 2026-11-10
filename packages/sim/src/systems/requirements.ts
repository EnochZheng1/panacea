import type { ContentPack, DrugClass, Requirement } from '@panacea/content';
import { dateIndex } from '../core/date.ts';
import type { Formula, GameState } from '../core/state.ts';
import { analyzeFormula } from './formula.ts';

export interface RequirementStatus {
  met: boolean;
  /**
   * 0..1, for progress bars: the share of a numeric threshold reached. "All" is as far along as
   * its least-met part, "any" as its most-met part, so a bar never reads nearer than it is.
   */
  progress: number;
}

const ratio = (value: number, target: number): RequirementStatus =>
  target <= 0 || value >= target
    ? { met: true, progress: 1 }
    : { met: false, progress: Math.max(0, Math.min(1, value / target)) };

export function evaluateRequirement(
  req: Requirement,
  state: GameState,
  content: ContentPack,
): RequirementStatus {
  switch (req.type) {
    case 'cash':
      return ratio(state.player.cashCents, req.atLeastCents);
    case 'reputation':
      return ratio(state.player.reputation, req.atLeast);
    case 'notoriety':
      return ratio(state.player.notoriety, req.atLeast);
    case 'bottlesSold':
      return ratio(state.stats.bottlesSold, req.atLeast);
    case 'batchesBrewed':
      return ratio(state.stats.batchesBrewed, req.atLeast);
    case 'heat':
      return ratio(state.scrutiny.heat, req.atLeast);
    case 'deaths':
      return ratio(state.deaths.length, req.atLeast);
    case 'inquests':
      return ratio(state.scrutiny.inquestsHeld, req.atLeast);
    case 'regulars':
      return ratio(state.customers.length, req.atLeast);
    case 'date': {
      const start = dateIndex(content.config.startDate);
      const now = dateIndex(state.meta.date);
      const target = dateIndex(req.from);
      if (now >= target) return { met: true, progress: 1 };
      return { met: false, progress: Math.max(0, (now - start) / Math.max(1, target - start)) };
    }
    case 'season':
      return binary(req.months.includes(state.meta.date.month));
    case 'months':
      return ratio(state.meta.turn, req.atLeast);
    case 'since': {
      const months = monthsSince(state, req.event);
      return months === null ? binary(false) : ratio(months, req.atLeast);
    }
    case 'remedyHabit':
    case 'remedyDrug':
    case 'remedyClaim':
    case 'remedyPrice':
      return binary(remedyHolds(req, state, content));
    case 'remedies':
      return ratio(remediesSold(state), req.atLeast);
    case 'flag':
      return binary(state.player.flags[req.id] === true);
    case 'unlocked':
      return binary(req.id in state.unlocks.unlocked);
    case 'all': {
      const parts = req.of.map((r) => evaluateRequirement(r, state, content));
      return {
        met: parts.every((p) => p.met),
        progress: Math.min(...parts.map((p) => p.progress)),
      };
    }
    case 'any': {
      const parts = req.of.map((r) => evaluateRequirement(r, state, content));
      return { met: parts.some((p) => p.met), progress: Math.max(...parts.map((p) => p.progress)) };
    }
    case 'not':
      return binary(!evaluateRequirement(req.requirement, state, content).met);
  }
}

const binary = (met: boolean): RequirementStatus => ({ met, progress: met ? 1 : 0 });

/** Months since the letter last came, or null if it never has. */
function monthsSince(state: GameState, eventId: string): number | null {
  const last = state.events.lastDrawn[eventId];
  return last === undefined ? null : state.meta.turn - last;
}

/**
 * The shop's remedy, as its correspondents know it: the bottle it sold most of last month (the
 * first in the books, on a tie), else the newest in the formula book; null before there is one.
 */
export function shopRemedy(state: GameState): Formula | null {
  const last = state.ledger[state.ledger.length - 1];
  let best: string | null = null;
  let most = 0;
  for (const s of last?.sales ?? []) {
    if (s.bottles > most) {
      most = s.bottles;
      best = s.formulaId;
    }
  }
  const formulas = state.lab.formulas;
  return formulas.find((f) => f.id === best) ?? formulas[formulas.length - 1] ?? null;
}

/** How many different remedies the shop sold last month (0 before its first month is out). */
export function remediesSold(state: GameState): number {
  const last = state.ledger[state.ledger.length - 1];
  return (last?.sales ?? []).filter((s) => s.bottles > 0).length;
}

/** Fluid ounces per bottle of the formula's ingredients of one drug class. */
export function drugOunces(formula: Formula, drugClass: DrugClass, content: ContentPack): number {
  let oz = 0;
  for (const c of formula.components) {
    if (content.byId.ingredients.get(c.ingredientId)?.drugClass === drugClass) oz += c.amount;
  }
  return oz;
}

function remedyHolds(
  req: Extract<Requirement, { type: 'remedyHabit' | 'remedyDrug' | 'remedyClaim' | 'remedyPrice' }>,
  state: GameState,
  content: ContentPack,
): boolean {
  const remedy = shopRemedy(state);
  if (!remedy) return false;
  switch (req.type) {
    case 'remedyPrice':
      return remedy.priceCents >= req.atLeastCents;
    case 'remedyHabit':
      return (
        analyzeFormula(remedy.components, remedy.processId, content).properties.habit >= req.atLeast
      );
    case 'remedyDrug':
      return drugOunces(remedy, req.drugClass, content) >= req.atLeastOz;
    case 'remedyClaim':
      return remedy.claims.includes(req.id);
  }
}

/**
 * One condition of a requirement, as numbers a progress line can print: "1,120 of 1,500 bottles
 * sold", "suspicion 12, offered below 30", "the cobbler's shop: not yet".
 */
export interface ConditionProgress {
  /**
   * What is counted: a requirement type (`bottlesSold`, `regulars`, `cash`, `heat`, `unlocked`,
   * `flag`, `date`, …), or `cost` for an investment's price against the cash in hand.
   */
  kind: string;
  /** The unlock or flag waited on (`unlocked`, `flag`), or null. */
  id: string | null;
  /**
   * `atLeast`: met once `have` reaches `need`. `below`: met while `have` stays under `need` (a
   * ceiling, such as "while the talk in the ward is low").
   */
  sense: 'atLeast' | 'below';
  /** The present count. Cash and cost in cents; unlocked and flag 1 or 0; date a month index. */
  have: number;
  need: number;
  met: boolean;
  /** Set on the conditions of an unlock that is itself waited on: that unlock's id. */
  via: string | null;
}

/** The count a numeric requirement compares, or null for the others. */
function countFor(req: Requirement, state: GameState): number | null {
  switch (req.type) {
    case 'cash':
      return state.player.cashCents;
    case 'reputation':
      return state.player.reputation;
    case 'notoriety':
      return state.player.notoriety;
    case 'bottlesSold':
      return state.stats.bottlesSold;
    case 'batchesBrewed':
      return state.stats.batchesBrewed;
    case 'heat':
      return state.scrutiny.heat;
    case 'deaths':
      return state.deaths.length;
    case 'inquests':
      return state.scrutiny.inquestsHeld;
    case 'regulars':
      return state.customers.length;
    case 'months':
      return state.meta.turn;
    case 'remedies':
      return remediesSold(state);
    default:
      return null;
  }
}

/**
 * The leaves of a requirement as progress lines, in content order. "All" lists every part; "any"
 * lists the parts of its most-met branch; "not" turns a threshold into a ceiling. An `unlocked`
 * condition not yet met is followed by its unlock's own conditions, marked `via` that unlock.
 */
export function requirementProgress(
  req: Requirement,
  state: GameState,
  content: ContentPack,
  via: string | null = null,
): ConditionProgress[] {
  switch (req.type) {
    case 'all':
      return req.of.flatMap((r) => requirementProgress(r, state, content, via));
    case 'any': {
      let best = req.of[0] as Requirement;
      let bestProgress = Number.NEGATIVE_INFINITY;
      for (const r of req.of) {
        const p = evaluateRequirement(r, state, content).progress;
        if (p > bestProgress) {
          best = r;
          bestProgress = p;
        }
      }
      return requirementProgress(best, state, content, via);
    }
    case 'not': {
      const inner = requirementProgress(req.requirement, state, content, via);
      // A ceiling on one number reads as that number held below its threshold.
      if (inner.length === 1 && inner[0]!.sense === 'atLeast' && inner[0]!.via === via) {
        const c = inner[0]!;
        return [{ ...c, sense: 'below', met: !c.met }];
      }
      const met = !evaluateRequirement(req.requirement, state, content).met;
      return [{ kind: 'not', id: null, sense: 'below', have: met ? 0 : 1, need: 1, met, via }];
    }
    case 'unlocked': {
      const met = req.id in state.unlocks.unlocked;
      const line: ConditionProgress = {
        kind: 'unlocked',
        id: req.id,
        sense: 'atLeast',
        have: met ? 1 : 0,
        need: 1,
        met,
        via,
      };
      const unlock = content.byId.unlocks.get(req.id);
      if (met || !unlock) return [line];
      return [line, ...requirementProgress(unlock.requirement, state, content, req.id)];
    }
    case 'flag': {
      const met = state.player.flags[req.id] === true;
      return [{ kind: 'flag', id: req.id, sense: 'atLeast', have: met ? 1 : 0, need: 1, met, via }];
    }
    case 'date': {
      const have = dateIndex(state.meta.date);
      const need = dateIndex(req.from);
      return [{ kind: 'date', id: null, sense: 'atLeast', have, need, met: have >= need, via }];
    }
    case 'season': {
      const met = req.months.includes(state.meta.date.month);
      return [{ kind: 'season', id: null, sense: 'atLeast', have: met ? 1 : 0, need: 1, met, via }];
    }
    case 'since': {
      const months = monthsSince(state, req.event);
      const met = months !== null && months >= req.atLeast;
      const have = months ?? 0;
      return [
        { kind: 'since', id: req.event, sense: 'atLeast', have, need: req.atLeast, met, via },
      ];
    }
    case 'remedyHabit':
    case 'remedyDrug':
    case 'remedyClaim':
    case 'remedyPrice': {
      const met = remedyHolds(req, state, content);
      const id =
        req.type === 'remedyClaim' ? req.id : req.type === 'remedyDrug' ? req.drugClass : null;
      return [{ kind: req.type, id, sense: 'atLeast', have: met ? 1 : 0, need: 1, met, via }];
    }
    default: {
      const have = countFor(req, state) as number;
      const need = req.type === 'cash' ? req.atLeastCents : (req as { atLeast: number }).atLeast;
      const met = need <= 0 || have >= need;
      return [{ kind: req.type, id: null, sense: 'atLeast', have, need, met, via }];
    }
  }
}
