import type { ContentPack } from '@panacea/content';
import type { Rng } from './rng.ts';
import type { FormulaComponent, GameState } from './state.ts';

/** One bottle bought this month, with the true strength of the batch it came from. */
export interface Bought {
  formulaId: string;
  lotId: string;
  components: FormulaComponent[];
  processId: string;
  potency: number;
  badBatch: boolean;
}

/** Working data passed between phases within a single month-end. Never saved. */
export interface TurnScratch {
  /** Customer id → bottles bought this month. */
  purchases: Map<string, Bought[]>;
  /** One-off walk-in purchases (anonymous drinkers). */
  walkIns: { segmentId: string; ailmentId: string; bought: Bought }[];
  adVisitors: number;
}

/** What every turn phase receives. `state` is a private draft the phase may mutate. */
export interface TurnContext {
  state: GameState;
  content: ContentPack;
  rng: Rng;
  scratch: TurnScratch;
}

export interface Phase {
  id: string;
  run(ctx: TurnContext): void;
}

export const newScratch = (): TurnScratch => ({ purchases: new Map(), walkIns: [], adVisitors: 0 });
