import type { ContentPack, GameDate, Properties } from '@panacea/content';
import type {
  DeathRecord,
  EndReason,
  GameState,
  LedgerEntry,
  ReportedCause,
} from '../core/state.ts';
import { customerCapacity } from '../systems/customers.ts';
import {
  analyzeFormula,
  isIngredientAvailable,
  isProcessAvailable,
  validateClaims,
} from '../systems/formula.ts';
import {
  formulaUnitCost,
  ingredientUnitCost,
  maxBrewable,
  stillBottles,
  stillsFree,
  stockOf,
  stockValue,
} from '../systems/lab.ts';
import {
  investmentProgress,
  investRefusal,
  wholesaleOrder,
  wholesalePrice,
} from '../systems/investments.ts';
import type { ConditionProgress } from '../systems/requirements.ts';
import {
  availableChoices,
  capacitySteps,
  nextUnlocks,
  type CapacityStep,
  type UnlockProgress,
} from '../systems/progression.ts';
import { isRuined } from '../systems/market.ts';
import { bribeCostCents, bribeExposureChance, INQUEST_EVENT_ID } from '../systems/scrutiny.ts';
import { wardRemembers, type WardRemembers } from '../systems/ward.ts';

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
  /**
   * Weaning off an old recipe: still craving one that had a narcotic in it, on a bottle that has
   * none (see `Customer.weaning`); the craving fades within a season.
   */
  weaning: boolean;
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
  /** The complaint they took the remedy for, if known (a stranger by post: null). */
  ailment: string | null;
  /**
   * The cause as the ward and the coroner's register give it: `overdose`, `wasting` (as from a
   * slow poison), `ailment` (of the complaint they came in with), `old_age` or `natural`. Whether
   * the remedy hastened a death that looked natural is not shown.
   */
  reportedCause: ReportedCause;
  formulaId: string | null;
  /** The label on their bottle claimed to cure the complaint they died with. */
  promisedCure: boolean;
  inquest: DeathRecord['inquest'];
  verdict: string | null;
}

/** An investment as the proprietor sees it: what it costs, what it does, and whether it is held. */
export interface ObservedInvestment {
  id: string;
  costCents: number;
  /** Paid every month-end while held. */
  upkeepCents: number;
  /** How long it runs once bought, or null for good. */
  months: number | null;
  held: boolean;
  boughtTurn: number | null;
  /** Held, with a term: month-ends left, this one included. */
  monthsLeft: number | null;
  canBuy: boolean;
  /** Why it cannot be bought: "already held", "not yet offered" or "not enough cash". */
  refusal: string | null;
  walkInsPerMonth: number;
  /** Heat a month per point of label boldness while held. */
  heatPerBoldness: number;
  /**
   * Wholesale: bottles the druggists would order this month, at the shop's present name and the
   * shelf price of the bottle the shop has most of (fewer the dearer it is).
   */
  wholesaleBottles: number | null;
  /** Wholesale: what they would pay a bottle for the bottle the shop has most of. */
  wholesalePriceCents: number | null;
  /** Orders by post (an almanac's readers): bottles a month at the present shelf price. */
  postBottles: number | null;
  /** Orders by post: what each brings. */
  postPriceCents: number | null;
  /** Chance of an accident each month while held. */
  mishapChance: number | null;
  /**
   * What it waits for, as numbers: every condition of its requirement (met or not), then its
   * price against the cash in hand (`kind: 'cost'`). Empty while it is held.
   */
  progress: ConditionProgress[];
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
  /**
   * What the ward remembers against the shop, 0..100: the neighbours' memory of the harm the
   * remedy did (not the authorities' suspicion, which is `heat`). It fades over most of a year,
   * and while it lasts strangers buy less, few come back as regulars, and some regulars leave.
   */
  wardMemory: number;
  /** What it remembers, counted over the last `months` months, for the paper. */
  wardRemembers: WardRemembers;
  capabilities: Record<string, number>;
  /** Stills in the back room, lit or cold. */
  stills: number;
  stillsFree: number;
  /** The most bottles one still makes in a batch (every still alike). */
  stillBottles: number;
  /**
   * What would add to the stills or their size, not yet had: unlocks (in content order), then
   * ventures, each with what it waits for. The first with `stills > 0` is the next still.
   */
  nextCapacity: CapacityStep[];
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
    /** A bottle at today's prices: ingredients (with any discount or surcharge) and bottling. */
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
  investments: ObservedInvestment[];
  lastMonth: LedgerEntry | null;
  /**
   * Of the last three closed months, how many ended with the shelf bare: the months an order
   * from away, which takes only what is left on the shelf, would have gone short.
   */
  shelfBareMonths: number;
}

/** The formula orders from away would take: the one with most bottles on the shelf. */
function orderFormula(state: GameState) {
  let best = state.lab.formulas[0];
  let most = 0;
  for (const f of state.lab.formulas) {
    const n = stockOf(state, f.id);
    if (n > most) {
      best = f;
      most = n;
    }
  }
  return best;
}

export function observeInvestments(state: GameState, content: ContentPack): ObservedInvestment[] {
  const formula = orderFormula(state);
  const price = formula?.priceCents ?? null;
  return content.investments.map((inv) => {
    const holding = state.player.investments[inv.id];
    const refusal = investRefusal(state, inv, content);
    const progress = holding ? [] : investmentProgress(state, inv, content);
    return {
      id: inv.id,
      costCents: inv.costCents,
      upkeepCents: inv.upkeepCents,
      months: inv.months ?? null,
      held: holding !== undefined,
      boughtTurn: holding?.boughtTurn ?? null,
      monthsLeft: holding && holding.endsTurn !== null ? holding.endsTurn - state.meta.turn : null,
      canBuy: refusal === null,
      refusal,
      walkInsPerMonth: inv.walkInsPerMonth,
      heatPerBoldness: inv.heatPerBoldness,
      wholesaleBottles: inv.wholesale ? wholesaleOrder(state, inv.wholesale, price) : null,
      wholesalePriceCents:
        inv.wholesale && price !== null ? wholesalePrice(inv.wholesale, price) : null,
      postBottles: inv.postOrders ? wholesaleOrder(state, inv.postOrders, price) : null,
      postPriceCents:
        inv.postOrders && price !== null ? wholesalePrice(inv.postOrders, price) : null,
      mishapChance: inv.mishap?.chancePerMonth ?? null,
      progress,
    };
  });
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
    wardMemory: state.ward.memory,
    wardRemembers: wardRemembers(state, content),
    capabilities: { ...state.player.capabilities },
    stills: Math.max(0, state.player.capabilities['stills'] ?? 0),
    stillsFree: stillsFree(state),
    stillBottles: stillBottles(state, content),
    nextCapacity: capacitySteps(state, content),
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
        unitCostCents: formulaUnitCost(state, f.components, f.processId, content),
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
      weaning: c.weaning,
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
      ailment: d.ailment,
      reportedCause: d.reportedCause,
      formulaId: d.formulaId,
      promisedCure: d.promisedCure,
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
    investments: observeInvestments(state, content),
    lastMonth: state.ledger.at(-1) ?? null,
    shelfBareMonths: state.ledger.slice(-3).filter((e) => e.shelfEndBottles === 0).length,
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
  | { type: 'ResolveEvent'; instanceId: string; choiceIds: string[] }
  | { type: 'Invest'; investmentIds: string[] };

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
  const investmentIds = content.investments
    .filter((inv) => investRefusal(state, inv, content) === null)
    .map((inv) => inv.id);
  if (investmentIds.length > 0) specs.push({ type: 'Invest', investmentIds });
  return specs;
}
