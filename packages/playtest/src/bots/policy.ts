import type { FormulaComponent, Observation } from '@panacea/sim';
import type { Bot, BotTurn } from './types.ts';

/**
 * A configurable strategy. Every named bot is a preset of this policy, and the optimizer
 * searches over its parameters to find the strongest strategy (the exploit hunter).
 */
export interface Policy {
  id: string;
  description: string;
  /** Active ingredient (if any) and its dose per bottle; the rest is fixed by the other fields. */
  narcotic: { ingredientId: string; oz: number } | null;
  alcoholOz: number;
  /** Extra non-drug ingredients (bitters, purgatives, metals…). */
  extras: FormulaComponent[];
  priceCents: number;
  /** Keep this many months of last month's sales on the shelf (minimum `minStock`). */
  stockCover: number;
  minStock: number;
  claims: string[];
  adSpendCents: number;
  inquest: 'silent' | 'testify' | 'bribe';
  /** How to answer random events: take the default, or the first (usually bolder) choice. */
  events: 'default' | 'first';
  /** Raise the narcotic dose when many customers are unhappy. */
  escalate?: { stepOz: number; maxOz: number };
  /** Lower it when the heat is up. */
  deescalate?: { heatAbove: number; stepOz: number; minOz: number };
  /**
   * Investments to buy, in order of preference, whenever one is offered and the cash left after
   * it would still cover half a year of rent, advertising and its own upkeep.
   */
  invest?: string[];
}

const FLAVOUR: FormulaComponent[] = [
  { ingredientId: 'peppermint_oil', amount: 0.1 },
  { ingredientId: 'sugar_syrup', amount: 1 },
];

const round2 = (x: number) => Math.round(x * 100) / 100;

export function recipeFor(
  policy: Policy,
  narcoticOz: number,
  capacity: number,
): FormulaComponent[] {
  const parts: FormulaComponent[] = [];
  if (policy.alcoholOz > 0) parts.push({ ingredientId: 'grain_alcohol', amount: policy.alcoholOz });
  if (policy.narcotic && narcoticOz > 0) {
    parts.push({ ingredientId: policy.narcotic.ingredientId, amount: round2(narcoticOz) });
  }
  for (const e of [...policy.extras, ...FLAVOUR]) {
    const existing = parts.find((p) => p.ingredientId === e.ingredientId);
    if (existing) existing.amount = round2(existing.amount + e.amount);
    else parts.push({ ...e });
  }
  const used = parts.reduce((s, p) => s + p.amount, 0);
  const water = round2(capacity - used);
  if (water > 0.01) parts.push({ ingredientId: 'water', amount: water });
  else {
    // Over capacity: scale everything down to fit.
    const k = capacity / used;
    for (const p of parts) p.amount = Math.floor(p.amount * k * 100) / 100;
  }
  return parts.filter((p) => p.amount > 0);
}

const unhappyShare = (obs: Observation) =>
  obs.customers.length === 0
    ? 0
    : obs.customers.filter((c) => c.mood === 'grumbling' || c.mood === 'unhappy').length /
      obs.customers.length;

export function policyBot(policy: Policy): Bot {
  let narcoticOz = policy.narcotic?.oz ?? 0;
  let lastRecipeKey = '';

  return {
    id: policy.id,
    description: policy.description,
    playTurn(t: BotTurn) {
      let obs = t.observe();
      const capacity = t.content.config.bottleCapacity;

      // The Post.
      for (const ev of obs.pendingEvents) {
        let choiceId: string | undefined;
        if (ev.eventId === 'inquest') {
          const wanted =
            policy.inquest === 'bribe'
              ? 'bribe_coroner'
              : policy.inquest === 'testify'
                ? 'testify'
                : 'stay_silent';
          choiceId = ev.choiceIds.includes(wanted) ? wanted : 'stay_silent';
        } else if (policy.events === 'first') {
          choiceId = ev.choiceIds[0];
        }
        if (choiceId) t.act({ type: 'ResolveEvent', instanceId: ev.instanceId, choiceId });
      }

      // Investments.
      for (const id of policy.invest ?? []) {
        const inv = obs.investments.find((i) => i.id === id);
        if (!inv?.canBuy) continue;
        const reserve =
          6 * (t.content.config.rentCents + policy.adSpendCents + inv.upkeepCents) + 3000;
        if (obs.cashCents - inv.costCents < reserve) continue;
        if (t.act({ type: 'Invest', investmentId: id }).ok) obs = t.observe();
      }

      // Adjust the dose.
      if (policy.narcotic) {
        if (policy.escalate && unhappyShare(obs) > 0.4) {
          narcoticOz = Math.min(policy.escalate.maxOz, narcoticOz + policy.escalate.stepOz);
        }
        if (policy.deescalate && obs.heat > policy.deescalate.heatAbove) {
          narcoticOz = Math.max(policy.deescalate.minOz, narcoticOz - policy.deescalate.stepOz);
        }
      }
      const recipe = recipeFor(policy, narcoticOz, capacity);
      const recipeKey = JSON.stringify(recipe);

      // The formula: create it once, reformulate when the dose changes, keep claims current.
      let formula = obs.formulas[0];
      const claims = policy.claims
        .filter((c) => obs.claims.includes(c))
        .slice(0, t.content.config.maxClaimsPerFormula);
      if (!formula) {
        const r = t.act({
          type: 'CreateFormula',
          name: `${policy.id} tonic`,
          components: recipe,
          processId: 'simple_mix',
          priceCents: policy.priceCents,
          claims,
        });
        if (!r.ok) return;
        lastRecipeKey = recipeKey;
        obs = t.observe();
        formula = obs.formulas[0];
        if (!formula) return;
      } else if (recipeKey !== lastRecipeKey) {
        if (
          t.act({
            type: 'Reformulate',
            formulaId: formula.id,
            components: recipe,
            processId: 'simple_mix',
          }).ok
        ) {
          lastRecipeKey = recipeKey;
        }
      }
      if (JSON.stringify(formula.claims) !== JSON.stringify(claims)) {
        t.act({ type: 'SetClaims', formulaId: formula.id, claims });
      }

      // Advertising.
      if (obs.canAdvertise && obs.adSpendCents !== policy.adSpendCents) {
        t.act({ type: 'SetAdSpend', cents: policy.adSpendCents });
      }

      // Restock: cover recent demand, keeping a reserve for rent and advertising.
      obs = t.observe();
      const soldLastMonth =
        obs.lastMonth?.sales.find((s) => s.formulaId === formula.id)?.bottles ?? 0;
      const target = Math.max(policy.minStock, Math.ceil(soldLastMonth * policy.stockCover));
      const wanted = target - formula.stock;
      if (wanted <= 0 || obs.stillsFree === 0) return;
      const reserve = t.content.config.rentCents * 2 + policy.adSpendCents;
      const unit = Math.max(1, obs.formulas[0]?.unitCostCents ?? 1);
      const bottles = Math.min(wanted, Math.floor(Math.max(0, obs.cashCents - reserve) / unit));
      if (bottles < 1) return;
      for (const c of recipe) {
        const have = t.observe().inventory[c.ingredientId] ?? 0;
        const need = Math.ceil(c.amount * bottles - have - 1e-9);
        if (need > 0) t.act({ type: 'BuyIngredient', ingredientId: c.ingredientId, units: need });
      }
      t.act({ type: 'BrewBatch', formulaId: formula.id, bottles });
    },
  };
}

/** Named strategies for balance reports. */
export const PRESETS: Record<string, Policy> = {
  shopkeeper: {
    id: 'shopkeeper',
    description:
      'A modest laudanum tonic at a fair price; no advertising; keeps quiet at inquests.',
    narcotic: { ingredientId: 'laudanum', oz: 0.25 },
    alcoholOz: 2,
    extras: [],
    priceCents: 60,
    stockCover: 1.3,
    minStock: 15,
    claims: [],
    adSpendCents: 0,
    inquest: 'silent',
    events: 'default',
    invest: ['branch_shop', 'wholesale_contract', 'druggists_order', 'almanac'],
  },
  cautious: {
    id: 'cautious',
    description: 'No narcotics: spirits, valerian, bitters and pepper. Honest at inquests.',
    narcotic: null,
    alcoholOz: 1.5,
    extras: [
      { ingredientId: 'gentian', amount: 0.2 },
      { ingredientId: 'capsicum', amount: 0.3 },
      { ingredientId: 'valerian', amount: 1 },
    ],
    priceCents: 50,
    stockCover: 1.3,
    minStock: 15,
    claims: ['purifies_the_blood'],
    adSpendCents: 0,
    inquest: 'testify',
    events: 'default',
    invest: ['branch_shop', 'wholesale_contract', 'druggists_order', 'almanac'],
  },
  greedy: {
    id: 'greedy',
    description:
      'Morphine and cocaine at a price for the hooked, escalating doses, wild claims, heavy advertising, bribes; its own laboratory.',
    // Doses in content 0.3.0 morphine (4× the strength per ounce of 0.2.0's): the same
    // 0.4 → 1.5 old ounces, escalating by 0.1.
    narcotic: { ingredientId: 'morphine', oz: 0.1 },
    alcoholOz: 2,
    extras: [{ ingredientId: 'coca_extract', amount: 0.3 }],
    priceCents: 150,
    stockCover: 1.5,
    minStock: 20,
    claims: ['cures_everything', 'cures_consumption', 'teething_comfort', 'endorsed_by_physicians'],
    adSpendCents: 2000,
    inquest: 'bribe',
    events: 'first',
    escalate: { stepOz: 0.025, maxOz: 0.375 },
    invest: ['laboratory', 'branch_shop'],
  },
  briber: {
    id: 'briber',
    description: 'A strong laudanum tonic; buys off every inquest it can.',
    narcotic: { ingredientId: 'laudanum', oz: 0.6 },
    alcoholOz: 2,
    extras: [],
    priceCents: 80,
    stockCover: 1.3,
    minStock: 20,
    claims: ['soothes_nerves', 'banishes_rheumatism'],
    adSpendCents: 1000,
    inquest: 'bribe',
    events: 'default',
    invest: ['branch_shop', 'wholesale_contract', 'druggists_order', 'almanac'],
  },
  adaptive: {
    id: 'adaptive',
    description: 'Raises the laudanum when customers grumble, cuts it when the heat rises.',
    narcotic: { ingredientId: 'laudanum', oz: 0.25 },
    alcoholOz: 2,
    extras: [],
    priceCents: 70,
    stockCover: 1.3,
    minStock: 15,
    claims: ['soothes_nerves', 'womens_friend'],
    adSpendCents: 800,
    inquest: 'testify',
    events: 'default',
    escalate: { stepOz: 0.05, maxOz: 0.8 },
    deescalate: { heatAbove: 35, stepOz: 0.1, minOz: 0.15 },
    invest: ['branch_shop', 'wholesale_contract', 'druggists_order', 'almanac'],
  },
  mercurial: {
    id: 'mercurial',
    description: 'A cheap calomel-and-aloes purgative: no narcotics, but mercury accumulates.',
    narcotic: null,
    alcoholOz: 1,
    extras: [
      { ingredientId: 'calomel', amount: 0.5 },
      { ingredientId: 'aloe', amount: 0.5 },
    ],
    priceCents: 50,
    stockCover: 1.3,
    minStock: 15,
    claims: ['purifies_the_blood'],
    adSpendCents: 300,
    inquest: 'silent',
    events: 'default',
    invest: ['branch_shop', 'wholesale_contract', 'druggists_order', 'almanac'],
  },
};
