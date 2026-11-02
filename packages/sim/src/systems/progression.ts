import type { ContentPack } from '@panacea/content';
import { CAUSES } from '../core/causes.ts';
import { compareDates } from '../core/date.ts';
import type { GameState, Phase, Rng } from '../core/index.ts';
import { applyEffects } from './effects.ts';
import { evaluateRequirement } from './requirements.ts';

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
    .sort((a, b) => b.progress - a.progress || a.id.localeCompare(b.id))
    .slice(0, limit);
}

/** Draws this month's events into The Post. */
export function drawEvents(state: GameState, content: ContentPack, rng: Rng): void {
  const max = content.config.maxEventsPerMonth;
  for (const ev of content.events) {
    if (state.events.pending.length >= max) break;
    if (state.events.pending.some((p) => p.eventId === ev.id)) continue;
    const times = state.events.timesDrawn[ev.id] ?? 0;
    if (ev.once && times > 0) continue;
    const last = state.events.lastDrawn[ev.id];
    if (last !== undefined && state.meta.turn - last < ev.cooldownMonths) continue;
    if (!evaluateRequirement(ev.condition, state, content).met) continue;
    if (!rng.chance(ev.chance)) continue;
    state.events.pending.push({
      instanceId: `event_${state.meta.nextId++}`,
      eventId: ev.id,
      drawnTurn: state.meta.turn,
    });
    state.events.lastDrawn[ev.id] = state.meta.turn;
    state.events.timesDrawn[ev.id] = times + 1;
  }
}

/** Resolves a pending event with the given choice. Returns an error message on failure. */
export function resolveEvent(
  state: GameState,
  content: ContentPack,
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
  state.events.pending.splice(index, 1);
  applyEffects(state, choice.effects, CAUSES.event, ev.id);
  state.month.news.push({ kind: 'event_resolved', eventId: ev.id, choiceId: choice.id, auto });
  return null;
}

/** Resolves every pending event with its default (or first) choice. */
export function autoResolveEvents(state: GameState, content: ContentPack): void {
  for (const pending of [...state.events.pending]) {
    const ev = content.byId.events.get(pending.eventId);
    const choice = ev?.choices.find((c) => c.default) ?? ev?.choices[0];
    if (choice) resolveEvent(state, content, pending.instanceId, choice.id, true);
    else state.events.pending = state.events.pending.filter((p) => p !== pending);
  }
}
