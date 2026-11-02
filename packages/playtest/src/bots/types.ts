import type { ContentPack } from '@panacea/content';
import type { Action, ActionResult, ActionSpec, GameState, Observation, Rng } from '@panacea/sim';

/** What a bot can see and do during its turn. The harness owns the real state. */
export interface BotTurn {
  readonly content: ContentPack;
  /** The bot's own RNG, separate from the game's, so bots never perturb the simulation. */
  readonly rng: Rng;
  observe(): Observation;
  legal(): ActionSpec[];
  act(action: Action): ActionResult;
  /** Full state, for bots that need internals (e.g. analysis tooling). Prefer `observe`. */
  readonly state: GameState;
}

export interface Bot {
  id: string;
  description: string;
  /** Called once per month, before the month is ended. */
  playTurn(turn: BotTurn): void;
}
