import type { ContentPack, GameDate, Properties } from '@panacea/content';
import type { DeathRecord, EndReason, GameState, LedgerEntry } from '../core/state.ts';
import { customerCapacity } from '../systems/customers.ts';
import {
  analyzeFormula,
  isIngredientAvailable,
  isProcessAvailable,
  validateClaims,
} from '../systems/formula.ts';
import {
  ingredientUnitCost,
  maxBrewable,
  stillsFree,
  stockOf,
  stockValue,
} from '../systems/lab.ts';
import { availableChoices, nextUnlocks, type UnlockProgress } from '../systems/progression.ts';
import { isRuined } from '../systems/market.ts';
import { bribeCostCents, bribeExposureChance, INQUEST_EVENT_ID } from '../systems/scrutiny.ts';

/**
 * The machine API: what scripted bots and AI playtesters see and can do. `observe` returns only
 * what a player could know: customers' appearance and mood rather than their exact health, and
 * inquest verdicts rather than the true causes of death. `legalActions` lists the valid moves.
 */

export type Looks = 'hale' | 'peaky' | 'poorly' | 'gravely_ill';
export type Mood = 'delighted' | 'content' | 'grumbling' | 'unhappy';

export const looksFor = (health: number): Looks =>
  health >= 70 ? 'hale' : health >= 50 ? 'peaky' : health >= 30 ? 'poorly' : 'gravely_ill';

export const moodFor = (s: number): Mood =>
  s >= 0.4 ? 'delighted' : s >= 0 ? 'content' : s >= -0.4 ? 'grumbling' : 'unhappy';

export interface ObservedCustomer {
  id: string;
  name: string;
  segment: string;
  ailment: string;
  consumer: 'self' | 'infant';
  monthsAsCustomer: number;
  bottlesLastMonth: number;
  looks: Looks;
  mood: Mood;
  /** Asks for the medicine by name and won't take substitutes. */
  craving: boolean;
  /** Shaking, sweating, irritable: missed their dose. */
  withdrawal: boolean;
  /** Spent beyond their means on the bottle and can no longer pay: one bottle at most. */
  ruined: boolean;
}

export interface ObservedDeath {
  id: string;
  turn: number;
  date: GameDate;
  name: string;
  segment: string;
  consumer: 'self' | 'infant';
  anonymous: boolean;
  formulaId: string | null;
  inquest: DeathRecord['inquest'];
  verdict: string | null;
}

export interface Observation {
  turn: number;
  date: GameDate;
  endDate: GameDate;
  status: 'running' | 'ended';
  endReason: EndReason | null;
  cashCents: number;
  reputation: number;
  notoriety: number;
  heat: number;
  capabilities: Record<string, number>;
  stillsFree: number;
  inventory: Record<string, number>;
  /**
   * The stock at today's cost: bottles brewing or on the shelf, and ingredients on hand. Buying
   * stock moves money from cash into here; the month's books are cash and stock together.
   */
  stockValue: { shelfCents: number; ingredientsCents: number; totalCents: number };
  ingredients: { id: string; costPerUnitCents: number }[];
  processes: string[];
  claims: string[];
  canAdvertise: boolean;
  adSpendCents: number;
  formulas: {
    id: string;
    name: string;
    priceCents: number;
    claims: string[];
    stock: number;
    unitCostCents: number;
    properties: Properties;
  }[];
  batchesInProgress: { formulaId: string; bottles: number }[];
  customers: ObservedCustomer[];
  customerCapacity: number;
  deaths: ObservedDeath[];
  pendingEvents: {
    instanceId: string;
    eventId: string;
    choiceIds: string[];
    deathId?: string;
    subjectName?: string;
    bribeCostCents?: number;
    /** Inquests: the chance a bribe paid now would come out. */
    bribeExposureChance?: number;
  }[];
  unlocked: string[];
  nextUnlocks: UnlockProgress[];
  lastMonth: LedgerEntry | null;
}

export function observe(state: GameState, content: ContentPack): Observation {
  const threshold = content.config.body.withdrawalThreshold;
  return {
    turn: state.meta.turn,
    date: { ...state.meta.date },
    endDate: { ...content.config.endDate },
    status: state.meta.status,
    endReason: state.meta.endReason,
    cashCents: state.player.cashCents,
    reputation: state.player.reputation,
    notoriety: state.player.notoriety,
    heat: state.scrutiny.heat,
    capabilities: { ...state.player.capabilities },
    stillsFree: stillsFree(state),
    inventory: { ...state.lab.inventory },
    stockValue: stockValue(state, content),
    ingredients: content.ingredients
      .filter((i) => isIngredientAvailable(i.id, state, content))
      .map((i) => ({ id: i.id, costPerUnitCents: ingredientUnitCost(state, i.id, content) })),
    processes: content.processes
      .filter((p) => isProcessAvailable(p.id, state, content))
      .map((p) => p.id),
    claims: content.claims
      .filter((c) => validateClaims([c.id], state, content) === null)
      .map((c) => c.id),
    canAdvertise: 'newspaper_ads' in state.unlocks.unlocked,
    adSpendCents: state.marketing.adSpendCents,
    formulas: state.lab.formulas.map((f) => {
      const a = analyzeFormula(f.components, f.processId, content);
      return {
        id: f.id,
        name: f.name,
        priceCents: f.priceCents,
        claims: [...f.claims],
        stock: stockOf(state, f.id),
        unitCostCents: a.unitCostCents,
        properties: a.properties,
      };
    }),
    batchesInProgress: state.lab.batches.map((b) => ({
      formulaId: b.formulaId,
      bottles: b.bottles,
    })),
    customers: state.customers.map((c) => ({
      id: c.id,
      name: c.name,
      segment: c.segment,
      ailment: c.ailment,
      consumer: c.consumer,
      monthsAsCustomer: state.meta.turn - c.joinedTurn,
      bottlesLastMonth: c.bottlesLastMonth,
      looks: looksFor(c.body.health),
      mood: moodFor(c.satisfaction),
      craving: c.body.dependency > threshold,
      withdrawal: c.inWithdrawal,
      ruined: isRuined(c, content),
    })),
    customerCapacity: customerCapacity(state, content),
    deaths: state.deaths.map((d) => ({
      id: d.id,
      turn: d.turn,
      date: { ...d.date },
      name: d.name,
      segment: d.segment,
      consumer: d.consumer,
      anonymous: d.anonymous,
      formulaId: d.formulaId,
      inquest: d.inquest,
      verdict: d.verdict,
    })),
    pendingEvents: state.events.pending.map((p) => {
      const death = p.deathId ? state.deaths.find((d) => d.id === p.deathId) : undefined;
      return {
        instanceId: p.instanceId,
        eventId: p.eventId,
        choiceIds: availableChoices(state, content, p).map((c) => c.id),
        ...(p.deathId ? { deathId: p.deathId } : {}),
        ...(death ? { subjectName: death.name } : {}),
        ...(p.eventId === INQUEST_EVENT_ID
          ? {
              bribeCostCents: bribeCostCents(state, content),
              bribeExposureChance: bribeExposureChance(state, content),
            }
          : {}),
      };
    }),
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
      claimIds: string[];
      maxVolume: number;
    }
  | { type: 'SetPrice'; formulaId: string }
  | {
      type: 'Reformulate';
      formulaId: string;
      ingredientIds: string[];
      processIds: string[];
      maxVolume: number;
    }
  | { type: 'SetClaims'; formulaId: string; claimIds: string[]; maxClaims: number }
  | { type: 'SetAdSpend' }
  | { type: 'BrewBatch'; formulaId: string; maxBottles: number }
  | { type: 'ResolveEvent'; instanceId: string; choiceIds: string[] };

/** Every kind of move currently available, with parameter bounds. */
export function legalActions(state: GameState, content: ContentPack): ActionSpec[] {
  if (state.meta.status !== 'running') return [];
  const specs: ActionSpec[] = [];
  const available = content.ingredients.filter((i) => isIngredientAvailable(i.id, state, content));
  const claimIds = content.claims
    .filter((c) => validateClaims([c.id], state, content) === null)
    .map((c) => c.id);

  for (const ing of available) {
    const unit = ingredientUnitCost(state, ing.id, content);
    const maxUnits = unit === 0 ? 10_000 : Math.floor(state.player.cashCents / unit);
    if (maxUnits >= 1) specs.push({ type: 'BuyIngredient', ingredientId: ing.id, maxUnits });
  }
  if (state.lab.formulas.length < content.config.maxFormulas) {
    specs.push({
      type: 'CreateFormula',
      ingredientIds: available.map((i) => i.id),
      processIds: content.processes
        .filter((p) => isProcessAvailable(p.id, state, content))
        .map((p) => p.id),
      claimIds,
      maxVolume: content.config.bottleCapacity,
    });
  }
  const processIds = content.processes
    .filter((p) => isProcessAvailable(p.id, state, content))
    .map((p) => p.id);
  for (const f of state.lab.formulas) {
    specs.push({ type: 'SetPrice', formulaId: f.id });
    specs.push({
      type: 'Reformulate',
      formulaId: f.id,
      ingredientIds: available.map((i) => i.id),
      processIds,
      maxVolume: content.config.bottleCapacity,
    });
    specs.push({
      type: 'SetClaims',
      formulaId: f.id,
      claimIds,
      maxClaims: content.config.maxClaimsPerFormula,
    });
    const maxBottles = maxBrewable(state, f.id, content);
    if (maxBottles >= 1) specs.push({ type: 'BrewBatch', formulaId: f.id, maxBottles });
  }
  if ('newspaper_ads' in state.unlocks.unlocked) specs.push({ type: 'SetAdSpend' });
  for (const p of state.events.pending) {
    const choiceIds = availableChoices(state, content, p).map((c) => c.id);
    if (choiceIds.length > 0)
      specs.push({ type: 'ResolveEvent', instanceId: p.instanceId, choiceIds });
  }
  return specs;
}
