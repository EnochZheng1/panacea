import type { FormulaComponent } from './state.ts';

/**
 * Player commands. The UI, scripted bots and AI playtesters all play through exactly these, so
 * every way of playing exercises the same rules.
 */
export type Action =
  | { type: 'BuyIngredient'; ingredientId: string; units: number }
  | {
      type: 'CreateFormula';
      name: string;
      components: FormulaComponent[];
      processId: string;
      priceCents: number;
      claims?: string[];
    }
  | { type: 'SetPrice'; formulaId: string; priceCents: number }
  | {
      /** Quietly change what goes in the bottle. Stock already brewed is unaffected. */
      type: 'Reformulate';
      formulaId: string;
      components: FormulaComponent[];
      processId: string;
    }
  | { type: 'SetClaims'; formulaId: string; claims: string[] }
  | { type: 'SetAdSpend'; cents: number }
  | { type: 'BrewBatch'; formulaId: string; bottles: number }
  | { type: 'ResolveEvent'; instanceId: string; choiceId: string };

export type ActionType = Action['type'];

export type ActionResult = { ok: true; createdId?: string } | { ok: false; error: string };

export const MAX_PRICE_CENTS = 100_000;
export const MAX_FORMULA_NAME_LENGTH = 60;
export const MAX_AD_SPEND_CENTS = 100_000;
