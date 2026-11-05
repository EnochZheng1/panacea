import type { ContentPack, Requirement } from '@panacea/content';
import { dateIndex } from '../core/date.ts';
import type { GameState } from '../core/state.ts';

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
    default: {
      const have = countFor(req, state) as number;
      const need = req.type === 'cash' ? req.atLeastCents : req.atLeast;
      const met = need <= 0 || have >= need;
      return [{ kind: req.type, id: null, sense: 'atLeast', have, need, met, via }];
    }
  }
}
