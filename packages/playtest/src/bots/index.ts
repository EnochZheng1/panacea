import type { FormulaComponent } from '@panacea/sim';
import type { Bot, BotTurn } from './types.ts';

export type { Bot, BotTurn } from './types.ts';

/** Does nothing; events resolve to their defaults. Establishes the economy's floor. */
export const idleBot: Bot = {
  id: 'idle',
  description: 'Takes no actions.',
  playTurn() {},
};

/** Legal random moves with random parameters. A fuzzer for the rules and invariants. */
export const randomBot: Bot = {
  id: 'random',
  description: 'Random legal actions with random parameters.',
  playTurn(t) {
    const moves = t.rng.int(0, 4);
    for (let i = 0; i < moves; i++) {
      const specs = t.legal();
      if (specs.length === 0) return;
      const spec = t.rng.pick(specs);
      switch (spec.type) {
        case 'BuyIngredient':
          t.act({ ...spec, units: t.rng.int(1, Math.min(spec.maxUnits, 40)) });
          break;
        case 'CreateFormula': {
          const count = t.rng.int(1, Math.min(4, spec.ingredientIds.length));
          const picked = new Set<string>();
          while (picked.size < count) picked.add(t.rng.pick(spec.ingredientIds));
          const share = spec.maxVolume / count;
          const components: FormulaComponent[] = [...picked].map((ingredientId) => ({
            ingredientId,
            amount: Math.round(share * (0.2 + 0.8 * t.rng.float()) * 100) / 100 || 0.01,
          }));
          t.act({
            type: 'CreateFormula',
            name: `Random Tonic ${t.state.meta.turn}`,
            components,
            processId: t.rng.pick(spec.processIds),
            priceCents: t.rng.int(10, 300),
          });
          break;
        }
        case 'SetPrice':
          t.act({ type: 'SetPrice', formulaId: spec.formulaId, priceCents: t.rng.int(10, 300) });
          break;
        case 'BrewBatch':
          t.act({
            type: 'BrewBatch',
            formulaId: spec.formulaId,
            bottles: t.rng.int(1, spec.maxBottles),
          });
          break;
        case 'ResolveEvent':
          t.act({
            type: 'ResolveEvent',
            instanceId: spec.instanceId,
            choiceId: t.rng.pick(spec.choiceIds),
          });
          break;
      }
    }
  },
};

/**
 * A sensible small-shop strategy: one laudanum tonic, restocked every month to a target level,
 * keeping a cash reserve for rent. A stand-in for "a competent player" until M1's bot roster.
 */
export function shopkeeperBot(options: { targetStock?: number; priceCents?: number } = {}): Bot {
  const targetStock = options.targetStock ?? 30;
  const priceCents = options.priceCents ?? 60;
  const recipe: FormulaComponent[] = [
    { ingredientId: 'grain_alcohol', amount: 2 },
    { ingredientId: 'laudanum', amount: 0.25 },
    { ingredientId: 'peppermint_oil', amount: 0.1 },
    { ingredientId: 'sugar_syrup', amount: 1 },
    { ingredientId: 'water', amount: 4.65 },
  ];

  return {
    id: 'shopkeeper',
    description: `Restocks one laudanum tonic to ${targetStock} bottles at ${priceCents}¢.`,
    playTurn(t: BotTurn) {
      // Leaves events to their default (cautious) choices.
      const obs = t.observe();
      let formula = obs.formulas[0];
      if (!formula) {
        const r = t.act({
          type: 'CreateFormula',
          name: "Dr. Placeholder's Soothing Elixir",
          components: recipe,
          processId: 'simple_mix',
          priceCents,
        });
        if (!r.ok) return;
        formula = t.observe().formulas[0];
        if (!formula) return;
      }

      const reserveCents = t.content.config.rentCents * 2;
      const wanted = targetStock - formula.stock;
      if (wanted <= 0) return;
      const affordable = Math.floor(
        Math.max(0, t.observe().cashCents - reserveCents) / Math.max(1, formula.unitCostCents),
      );
      const bottles = Math.min(wanted, affordable);
      if (bottles < 1) return;

      for (const c of recipe) {
        const have = t.observe().inventory[c.ingredientId] ?? 0;
        const need = Math.ceil(c.amount * bottles - have);
        if (need > 0) t.act({ type: 'BuyIngredient', ingredientId: c.ingredientId, units: need });
      }
      t.act({ type: 'BrewBatch', formulaId: formula.id, bottles });
    },
  };
}

export const BOTS: Record<string, () => Bot> = {
  idle: () => idleBot,
  random: () => randomBot,
  shopkeeper: () => shopkeeperBot(),
};
