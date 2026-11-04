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
