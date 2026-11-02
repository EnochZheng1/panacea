import type { ContentPack } from '@panacea/content';
import type { Rng } from './rng.ts';
import type { GameState } from './state.ts';

/** What every turn phase receives. `state` is a private draft the phase may mutate. */
export interface TurnContext {
  state: GameState;
  content: ContentPack;
  rng: Rng;
}

export interface Phase {
  id: string;
  run(ctx: TurnContext): void;
}
