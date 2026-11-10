import type {
  ContentPack,
  Effect,
  EventChoice,
  GameEvent,
  LedgerTag,
  Requirement,
} from '@panacea/content';
import { CAUSES, type Cause } from '../core/causes.ts';
import { compareDates } from '../core/date.ts';
import type { GameState, PendingEvent, Phase, Rng } from '../core/index.ts';
import { applyEffects } from './effects.ts';
import { investmentProgress, isHeld } from './investments.ts';
import { letterTrust } from './market.ts';
import {
  evaluateRequirement,
  requirementProgress,
  type ConditionProgress,
} from './requirements.ts';
import { bribeCostCents, resolveInquest } from './scrutiny.ts';

/** Turn phase: historical events fire on their dates, regardless of the player. */
export const timelinePhase: Phase = {
  id: 'timeline',
  run({ state, content }) {
    for (const entry of content.timeline) {
      if (entry.id in state.timeline.fired) continue;
      if (compareDates(entry.date, state.meta.date) > 0) continue;
      state.timeline.fired[entry.id] = state.meta.turn;
      applyEffects(state, entry.effects, CAUSES.timeline, entry.id);
      state.month.news.push({ kind: 'timeline', id: entry.id });
    }
  },
};

/**
 * Turn phase, last: capability changes that were only for a while run out at the end of their
 * last month, so a change made in month t for n months holds through months t … t+n−1.
 */
export const timersPhase: Phase = {
  id: 'timers',
  run({ state }) {
    const live = [];
    for (const t of state.player.timedCapabilities) {
      if (t.expiresTurn > state.meta.turn + 1) {
        live.push(t);
        continue;
      }
      state.player.capabilities[t.id] = (state.player.capabilities[t.id] ?? 0) - t.delta;
    }
    state.player.timedCapabilities = live;
  },
};

/** Turn phase: grant any unlock whose requirement is now met. */
export const unlocksPhase: Phase = {
  id: 'unlocks',
  run({ state, content }) {
    // Loop so that unlocks which depend on other unlocks can chain within one month.
    let changed = true;
    while (changed) {
      changed = false;
      for (const unlock of content.unlocks) {
        if (unlock.id in state.unlocks.unlocked) continue;
        if (!evaluateRequirement(unlock.requirement, state, content).met) continue;
        state.unlocks.unlocked[unlock.id] = state.meta.turn;
        applyEffects(state, unlock.effects, CAUSES.unlock, unlock.id);
        state.month.news.push({ kind: 'unlock', id: unlock.id });
        changed = true;
      }
    }
  },
};

export interface UnlockProgress {
  id: string;
  progress: number;
}

/** The closest not-yet-gained unlocks, for the "next unlocks" progress bars. */
export function nextUnlocks(state: GameState, content: ContentPack, limit = 3): UnlockProgress[] {
  return content.unlocks
    .filter((u) => !(u.id in state.unlocks.unlocked))
    .map((u) => ({
      id: u.id,
      progress: evaluateRequirement(u.requirement, state, content).progress,
    }))
    .sort((a, b) => b.progress - a.progress || compareOrdinal(a.id, b.id))
    .slice(0, limit);
}

/** A step up in what the stills can make: another still, or a larger copper for every still. */
export interface CapacityStep {
  /** The unlock or venture that brings it. */
  id: string;
  source: 'unlock' | 'investment';
  /** Stills it adds. */
  stills: number;
  /** Bottles it adds to every still's batch. */
  stillBottles: number;
  /**
   * What it waits for, as for a venture's prospectus: every condition of its requirement (an
   * unlock it waits on followed by that unlock's own, marked `via`), and for a venture, its price
   * against the cash in hand (`kind: 'cost'`).
   */
  progress: ConditionProgress[];
}

const capacityOf = (effects: readonly Effect[]) => {
  let stills = 0;
  let stillBottles = 0;
  for (const e of effects) {
    if (e.type !== 'capability' || e.months !== undefined) continue;
    if (e.id === 'stills') stills += e.delta;
    if (e.id === 'still_bottles') stillBottles += e.delta;
  }
  return { stills, stillBottles };
};

/**
 * What would add to the stills or their size and is not yet had: unlocks in content order, then
 * ventures not held. The still card prints the first that adds a still ("the third still: 640 of
 * 1,000 bottles sold").
 */
export function capacitySteps(state: GameState, content: ContentPack): CapacityStep[] {
  const steps: CapacityStep[] = [];
  for (const u of content.unlocks) {
    if (u.id in state.unlocks.unlocked) continue;
    const c = capacityOf(u.effects);
    if (c.stills <= 0 && c.stillBottles <= 0) continue;
    steps.push({
      id: u.id,
      source: 'unlock',
      ...c,
      progress: requirementProgress(u.requirement, state, content),
    });
  }
  for (const inv of content.investments) {
    if (isHeld(state, inv.id)) continue;
    const c = capacityOf(inv.effects);
    if (c.stills <= 0 && c.stillBottles <= 0) continue;
    steps.push({
      id: inv.id,
      source: 'investment',
      ...c,
      progress: investmentProgress(state, inv, content),
    });
  }
  return steps;
}

/** Locale-independent string order (UTF-16 code units), identical in every engine. */
const compareOrdinal = (a: string, b: string): number => (a < b ? -1 : a > b ? 1 : 0);

const isRandomPending = (p: PendingEvent, content: ContentPack) =>
  content.byId.events.get(p.eventId)?.trigger !== 'system';

/**
 * Whether a condition waits on another letter (a `since`, not under a `not`): a follow-up. An
 * `any` waits only if every way of meeting it does, so "never signed, or signed a year ago" does
 * not make a fresh letter a follow-up.
 */
export function waitsOnLetter(req: Requirement): boolean {
  switch (req.type) {
    case 'since':
      return true;
    case 'all':
      return req.of.some(waitsOnLetter);
    case 'any':
      return req.of.every(waitsOnLetter);
    default:
      return false;
  }
}

/**
 * Draws this month's random events into The Post. System events (inquests) don't count.
 *
 * Every random event that could come this month (its condition holds, it is not already waiting,
 * not spent if once-only, not cooling down) is put in a seeded shuffle, and then each in turn is
 * drawn with its own chance until the month's post is full. So no event crowds out another by
 * its place in the content file. Follow-ups (`waitsOnLetter`) are tried before fresh letters.
 */
export function drawEvents(state: GameState, content: ContentPack, rng: Rng): void {
  let room =
    content.config.maxEventsPerMonth -
    state.events.pending.filter((p) => isRandomPending(p, content)).length;
  if (room <= 0) return;
  const eligible = content.events.filter((ev) => {
    if (ev.trigger !== 'random' || !ev.condition) return false;
    if (state.events.pending.some((p) => p.eventId === ev.id)) return false;
    if (ev.once && (state.events.timesDrawn[ev.id] ?? 0) > 0) return false;
    const last = state.events.lastDrawn[ev.id];
    if (last !== undefined && state.meta.turn - last < ev.cooldownMonths) return false;
    return evaluateRequirement(ev.condition, state, content).met;
  });
  // Fisher–Yates, from the end.
  for (let i = eligible.length - 1; i > 0; i--) {
    const j = rng.int(0, i);
    const t = eligible[i]!;
    eligible[i] = eligible[j]!;
    eligible[j] = t;
  }
  // A follow-up (a letter that waits on another) is drawn before fresh letters, in the shuffled
  // order of each, so a correspondent's next letter is not crowded out of a full post.
  const ordered = [
    ...eligible.filter((ev) => waitsOnLetter(ev.condition!)),
    ...eligible.filter((ev) => !waitsOnLetter(ev.condition!)),
  ];
  for (const ev of ordered) {
    if (room <= 0) break;
    if (!rng.chance(ev.chance)) continue;
    state.events.pending.push({
      instanceId: `event_${state.meta.nextId++}`,
      eventId: ev.id,
      drawnTurn: state.meta.turn,
    });
    state.events.lastDrawn[ev.id] = state.meta.turn;
    state.events.timesDrawn[ev.id] = (state.events.timesDrawn[ev.id] ?? 0) + 1;
    room--;
  }
}

/** Whether a choice can be taken right now (its requirement, and anything it would cost). */
export function isChoiceAvailable(
  state: GameState,
  content: ContentPack,
  choice: EventChoice,
): boolean {
  if (choice.requires && !evaluateRequirement(choice.requires, state, content).met) return false;
  for (const e of choice.effects) {
    if (e.type === 'cash' && e.deltaCents < 0 && state.player.cashCents < -e.deltaCents)
      return false;
    if (
      e.type === 'inquest' &&
      e.approach === 'bribe' &&
      state.player.cashCents < bribeCostCents(state, content)
    ) {
      return false;
    }
  }
  return true;
}

export function availableChoices(state: GameState, content: ContentPack, pending: PendingEvent) {
  const ev = content.byId.events.get(pending.eventId);
  return ev ? ev.choices.filter((c) => isChoiceAvailable(state, content, c)) : [];
}

/** Resolves a pending event with the given choice. Returns an error message on failure. */
export function resolveEvent(
  state: GameState,
  content: ContentPack,
  rng: Rng,
  instanceId: string,
  choiceId: string,
  auto: boolean,
): string | null {
  const index = state.events.pending.findIndex((p) => p.instanceId === instanceId);
  const pending = state.events.pending[index];
  if (!pending) return `no pending event "${instanceId}"`;
  const ev = content.byId.events.get(pending.eventId);
  if (!ev) return `unknown event "${pending.eventId}"`;
  const choice = ev.choices.find((c) => c.id === choiceId);
  if (!choice) return `event "${ev.id}" has no choice "${choiceId}"`;
  if (!isChoiceAvailable(state, content, choice)) return `choice "${choiceId}" is not available`;
  state.events.pending.splice(index, 1);
  for (const e of choice.effects) {
    if (e.type !== 'inquest') continue;
    const error = resolveInquest(state, content, rng, pending.deathId, e.approach);
    if (error) {
      state.events.pending.splice(index, 0, pending);
      return error;
    }
  }
  const cashCause = choice.ledger ? LETTER_LEDGER[choice.ledger] : CAUSES.event;
  for (const e of creditedEffects(state, content, ev, choice.effects)) {
    applyEffects(state, [e], e.type === 'cash' ? cashCause : CAUSES.event, ev.id);
  }
  state.month.news.push({ kind: 'event_resolved', eventId: ev.id, choiceId: choice.id, auto });
  return null;
}

/**
 * The ledger line a reply's money is booked under, by its `ledger` tag: a gilt sign is laid out
 * on the business, the Jersey distiller's spirit is an ingredient, the inspector's three dollars
 * a consulting fee, the parish's dozen a subscription. Untagged money (a pound of tea, a gallon
 * sold for a mare) is "letters answered", and so, for now, is trade given up (see the round-9
 * letters write-up: it wants a cause of its own).
 */
export const LETTER_LEDGER: Record<LedgerTag, Cause> = {
  outlay: CAUSES.investment,
  supplies: CAUSES.ingredients,
  fees: CAUSES.bribe,
  subscriptions: CAUSES.retainer,
  trade: CAUSES.event,
};

/**
 * The effects of a reply to a letter as the ward credits them: standing earned by a kindness is
 * scaled by how far the ward takes the shop at its word (`letterTrust`), as a letter's callers
 * are. Standing lost is lost in full.
 */
export function creditedEffects(
  state: GameState,
  content: ContentPack,
  ev: GameEvent,
  effects: readonly Effect[],
): Effect[] {
  if (ev.trigger !== 'random') return [...effects];
  const trust = letterTrust(state, content);
  return effects.map((e) =>
    e.type === 'reputation' && e.delta > 0 ? { ...e, delta: e.delta * trust } : e,
  );
}

/** Resolves every pending event with its default (or first available) choice. */
export function autoResolveEvents(state: GameState, content: ContentPack, rng: Rng): void {
  for (const pending of [...state.events.pending]) {
    const choices = availableChoices(state, content, pending);
    const choice = choices.find((c) => c.default) ?? choices[0];
    const error = choice
      ? resolveEvent(state, content, rng, pending.instanceId, choice.id, true)
      : 'no available choice';
    if (error) state.events.pending = state.events.pending.filter((p) => p !== pending);
  }
}
