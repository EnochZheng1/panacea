import type { ContentPack, GameDate, Properties } from '@panacea/content';
import type { EndReason, GameState, LedgerEntry } from '../core/state.ts';
import { analyzeFormula, isIngredientAvailable, isProcessAvailable } from '../systems/formula.ts';
import { maxBrewable, stillsFree } from '../systems/lab.ts';
import { nextUnlocks, type UnlockProgress } from '../systems/progression.ts';

/**
 * The machine API: what scripted bots and AI playtesters see and can do. `observe` returns only
 * what a human player could see on screen; `legalActions` describes the valid moves.
 */

export interface Observation {
  turn: number;
  date: GameDate;
  endDate: GameDate;
  status: 'running' | 'ended';
  endReason: EndReason | null;
  cashCents: number;
  reputation: number;
  notoriety: number;
  capabilities: Record<string, number>;
  stillsFree: number;
  inventory: Record<string, number>;
  ingredients: { id: string; costPerUnitCents: number }[];
  processes: string[];
  formulas: {
    id: string;
    name: string;
    priceCents: number;
    stock: number;
    unitCostCents: number;
    properties: Properties;
  }[];
  batchesInProgress: { formulaId: string; bottles: number }[];
  pendingEvents: { instanceId: string; eventId: string; choiceIds: string[] }[];
  unlocked: string[];
  nextUnlocks: UnlockProgress[];
  lastMonth: LedgerEntry | null;
}

export function observe(state: GameState, content: ContentPack): Observation {
  return {
    turn: state.meta.turn,
    date: { ...state.meta.date },
    endDate: { ...content.config.endDate },
    status: state.meta.status,
    endReason: state.meta.endReason,
    cashCents: state.player.cashCents,
    reputation: state.player.reputation,
    notoriety: state.player.notoriety,
    capabilities: { ...state.player.capabilities },
    stillsFree: stillsFree(state),
    inventory: { ...state.lab.inventory },
    ingredients: content.ingredients
      .filter((i) => isIngredientAvailable(i.id, state, content))
      .map((i) => ({ id: i.id, costPerUnitCents: i.costPerUnitCents })),
    processes: content.processes
      .filter((p) => isProcessAvailable(p.id, state, content))
      .map((p) => p.id),
    formulas: state.lab.formulas.map((f) => {
      const a = analyzeFormula(f.components, f.processId, content);
      return {
        id: f.id,
        name: f.name,
        priceCents: f.priceCents,
        stock: state.lab.stock[f.id] ?? 0,
        unitCostCents: a.unitCostCents,
        properties: a.properties,
      };
    }),
    batchesInProgress: state.lab.batches.map((b) => ({
      formulaId: b.formulaId,
      bottles: b.bottles,
    })),
    pendingEvents: state.events.pending.map((p) => ({
      instanceId: p.instanceId,
      eventId: p.eventId,
      choiceIds: content.byId.events.get(p.eventId)?.choices.map((c) => c.id) ?? [],
    })),
    unlocked: Object.keys(state.unlocks.unlocked),
    nextUnlocks: nextUnlocks(state, content),
    lastMonth: state.ledger.at(-1) ?? null,
  };
}

export type ActionSpec =
  | { type: 'BuyIngredient'; ingredientId: string; maxUnits: number }
  | {
      type: 'CreateFormula';
      ingredientIds: string[];
      processIds: string[];
      maxVolume: number;
    }
  | { type: 'SetPrice'; formulaId: string }
  | { type: 'BrewBatch'; formulaId: string; maxBottles: number }
  | { type: 'ResolveEvent'; instanceId: string; choiceIds: string[] };

/** Every kind of move currently available, with parameter bounds. */
export function legalActions(state: GameState, content: ContentPack): ActionSpec[] {
  if (state.meta.status !== 'running') return [];
  const specs: ActionSpec[] = [];
  const available = content.ingredients.filter((i) => isIngredientAvailable(i.id, state, content));

  for (const ing of available) {
    const maxUnits =
      ing.costPerUnitCents === 0
        ? 10_000
        : Math.floor(state.player.cashCents / ing.costPerUnitCents);
    if (maxUnits >= 1) specs.push({ type: 'BuyIngredient', ingredientId: ing.id, maxUnits });
  }
  if (state.lab.formulas.length < content.config.maxFormulas) {
    specs.push({
      type: 'CreateFormula',
      ingredientIds: available.map((i) => i.id),
      processIds: content.processes
        .filter((p) => isProcessAvailable(p.id, state, content))
        .map((p) => p.id),
      maxVolume: content.config.bottleCapacity,
    });
  }
  for (const f of state.lab.formulas) {
    specs.push({ type: 'SetPrice', formulaId: f.id });
    const maxBottles = maxBrewable(state, f.id, content);
    if (maxBottles >= 1) specs.push({ type: 'BrewBatch', formulaId: f.id, maxBottles });
  }
  for (const p of state.events.pending) {
    const ev = content.byId.events.get(p.eventId);
    if (ev) {
      specs.push({
        type: 'ResolveEvent',
        instanceId: p.instanceId,
        choiceIds: ev.choices.map((c) => c.id),
      });
    }
  }
  return specs;
}
