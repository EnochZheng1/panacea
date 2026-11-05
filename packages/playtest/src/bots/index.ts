import type { FormulaComponent } from '@panacea/sim';
import { policyBot, PRESETS } from './policy.ts';
import { SLOPPY_INVESTMENTS, sloppyBot } from './sloppy.ts';
import type { Bot, BotTurn } from './types.ts';

export type { Bot, BotTurn } from './types.ts';
export { policyBot, PRESETS, recipeFor, type Policy } from './policy.ts';
export { sloppyBot, SLOPPY_INVESTMENTS, type SloppyOptions } from './sloppy.ts';

/** Does nothing; events resolve to their defaults. Establishes the economy's floor. */
export const idleBot: Bot = {
  id: 'idle',
  description: 'Takes no actions.',
  playTurn() {},
};

function randomComponents(
  t: BotTurn,
  ingredientIds: string[],
  maxVolume: number,
): FormulaComponent[] {
  const count = t.rng.int(1, Math.min(4, ingredientIds.length));
  const picked = new Set<string>();
  while (picked.size < count) picked.add(t.rng.pick(ingredientIds));
  const share = maxVolume / count;
  return [...picked].map((ingredientId) => ({
    ingredientId,
    amount: Math.round(share * (0.2 + 0.8 * t.rng.float()) * 100) / 100 || 0.01,
  }));
}

function randomSubset<T>(t: BotTurn, items: readonly T[], max: number): T[] {
  return items.filter(() => t.rng.chance(0.3)).slice(0, max);
}

/** Legal random moves with random parameters. A fuzzer for the rules and invariants. */
export const randomBot: Bot = {
  id: 'random',
  description: 'Random legal actions with random parameters.',
  playTurn(t) {
    const moves = t.rng.int(0, 5);
    for (let i = 0; i < moves; i++) {
      const specs = t.legal();
      if (specs.length === 0) return;
      const spec = t.rng.pick(specs);
      switch (spec.type) {
        case 'BuyIngredient':
          t.act({ ...spec, units: t.rng.int(1, Math.min(spec.maxUnits, 40)) });
          break;
        case 'CreateFormula':
          t.act({
            type: 'CreateFormula',
            name: `Random Tonic ${t.state.meta.turn}`,
            components: randomComponents(t, spec.ingredientIds, spec.maxVolume),
            processId: t.rng.pick(spec.processIds),
            priceCents: t.rng.int(10, 300),
            claims: randomSubset(t, spec.claimIds, 2),
          });
          break;
        case 'Reformulate':
          t.act({
            type: 'Reformulate',
            formulaId: spec.formulaId,
            components: randomComponents(t, spec.ingredientIds, spec.maxVolume),
            processId: t.rng.pick(spec.processIds),
          });
          break;
        case 'SetPrice':
          t.act({ type: 'SetPrice', formulaId: spec.formulaId, priceCents: t.rng.int(10, 300) });
          break;
        case 'SetClaims':
          t.act({
            type: 'SetClaims',
            formulaId: spec.formulaId,
            claims: randomSubset(t, spec.claimIds, spec.maxClaims),
          });
          break;
        case 'SetAdSpend':
          t.act({ type: 'SetAdSpend', cents: t.rng.int(0, 3000) });
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
        case 'Invest':
          t.act({ type: 'Invest', investmentId: t.rng.pick(spec.investmentIds) });
          break;
      }
    }
  },
};

/** A sensible small-shop strategy; the baseline "competent player". */
export const shopkeeperBot = (): Bot => policyBot(PRESETS['shopkeeper']!);

export const BOTS: Record<string, () => Bot> = {
  idle: () => idleBot,
  random: () => randomBot,
  ...Object.fromEntries(Object.entries(PRESETS).map(([id, p]) => [id, () => policyBot(p)])),
  sloppy: () => sloppyBot({ id: 'sloppy', invest: SLOPPY_INVESTMENTS }),
};
