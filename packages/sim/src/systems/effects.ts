import type { Effect } from '@panacea/content';
import type { Cause } from '../core/causes.ts';
import { adjust } from '../core/ledger.ts';
import type { GameState } from '../core/state.ts';

/** Applies content-defined effects, attributing every numeric change to `cause`. */
export function applyEffects(
  state: GameState,
  effects: readonly Effect[],
  cause: Cause,
  detail?: string,
): void {
  for (const e of effects) {
    switch (e.type) {
      case 'cash':
        adjust(state, 'cash', e.deltaCents, cause, detail);
        break;
      case 'reputation':
        adjust(state, 'reputation', e.delta, cause, detail);
        break;
      case 'notoriety':
        adjust(state, 'notoriety', e.delta, cause, detail);
        break;
      case 'setFlag':
        state.player.flags[e.id] = true;
        break;
      case 'clearFlag':
        delete state.player.flags[e.id];
        break;
      case 'capability':
        state.player.capabilities[e.id] = (state.player.capabilities[e.id] ?? 0) + e.delta;
        break;
    }
  }
}
