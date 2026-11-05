import type { FormulaComponent } from '@panacea/sim';
import { recipeFor, type Policy } from './policy.ts';
import type { Bot, BotTurn } from './types.ts';

/**
 * A proprietor who plays the way the round-5 testers did by hand: an honest-ish bottle with a
 * little laudanum, a middling price picked once and left alone, stock ordered by eye (too much one
 * month, the shelf bare the next, the odd month forgotten), plain answers to the post, a quarter
 * column in the Gazette, and a venture taken up whenever it can be paid for with something left
 * in the till. Everything it gets wrong is drawn from its own RNG, so the game stays reproducible.
 */
export interface SloppyOptions {
  id: string;
  /** Investments it will take up, cheapest first, whenever offered and affordable. */
  invest: string[];
}

/** Everything but the laboratory, which a reader of "opium, morphine and coca" passes by. */
export const SLOPPY_INVESTMENTS = [
  'almanac',
  'shop_next_door',
  'larger_copper',
  'fourth_still',
  'druggists_order',
  'wholesale_contract',
  'branch_shop',
];

/**
 * Ventures that only make the stills bigger or more: taken up only once the proprietor has seen
 * the wall, a month with every still lit full and the shelf bare all the same.
 */
const CAPACITY = ['larger_copper', 'fourth_still'];

/** Batch sizes a hand player picks from the still's dial: dozens, up to what a still holds. */
const BATCH_STEP = 12;

export function sloppyBot(options: SloppyOptions): Bot {
  // Chosen once per game, on the first turn, from the bot's own RNG.
  let priceCents = 0;
  let laudanumOz = 0;
  let adCents = 0;
  /** The shelf the proprietor means to keep for the counter, adjusted by eye each month. */
  let shelf = 24;
  let created = false;
  /** Whether last month every still was lit with a full batch, and when that last sold out. */
  let litEveryStill = false;
  let walledTurn = Number.NEGATIVE_INFINITY;

  const policyFor = (): Policy => ({
    id: options.id,
    description: '',
    narcotic: { ingredientId: 'laudanum', oz: laudanumOz },
    alcoholOz: 2,
    extras: [{ ingredientId: 'valerian', amount: 0.5 }],
    priceCents,
    stockCover: 1,
    minStock: 0,
    claims: [],
    adSpendCents: 0,
    inquest: 'silent',
    events: 'default',
  });

  return {
    id: options.id,
    description:
      'Plays like a person: a mild laudanum tonic at 60–75c, stock by eye, plain letters, a little advertising, ventures when the till allows.',
    playTurn(t: BotTurn) {
      if (!created) {
        priceCents = t.rng.pick([60, 65, 70, 75]);
        laudanumOz = t.rng.pick([0.15, 0.2, 0.25, 0.3, 0.35]);
        adCents = t.rng.pick([300, 500]);
      }
      let obs = t.observe();
      const capacity = t.content.config.bottleCapacity;
      const rent = t.content.config.rentCents;
      if (litEveryStill && obs.formulas[0]?.stock === 0) walledTurn = obs.turn;
      litEveryStill = false;

      // The post: inquests answered one way or the other; most letters get the plain answer
      // (the default), and now and then the proprietor takes up whatever the letter offers.
      for (const ev of obs.pendingEvents) {
        let choiceId: string | undefined;
        if (ev.eventId === 'inquest') {
          choiceId = t.rng.chance(0.5) ? 'testify' : 'stay_silent';
        } else if (t.rng.chance(0.3)) {
          choiceId = t.rng.pick(ev.choiceIds);
        }
        if (choiceId && ev.choiceIds.includes(choiceId)) {
          t.act({ type: 'ResolveEvent', instanceId: ev.instanceId, choiceId });
        }
      }
      obs = t.observe();

      // Ventures: the cheapest one on offer, if the till keeps something back after it.
      const offered = obs.investments
        .filter((i) => i.canBuy && options.invest.includes(i.id))
        .sort((a, b) => a.costCents - b.costCents);
      for (const inv of offered) {
        if (CAPACITY.includes(inv.id) && obs.turn - walledTurn > 3) continue;
        const reserve = 6000 + 6 * (rent + inv.upkeepCents);
        if (obs.cashCents - inv.costCents < reserve) continue;
        if (t.act({ type: 'Invest', investmentId: inv.id }).ok) obs = t.observe();
      }

      // The formula, made once and never reformulated; claims added as they are allowed.
      const recipe: FormulaComponent[] = recipeFor(policyFor(), laudanumOz, capacity);
      let formula = obs.formulas[0];
      const claims = ['soothes_nerves', 'banishes_rheumatism'].filter((c) =>
        obs.claims.includes(c),
      );
      if (!formula) {
        const r = t.act({
          type: 'CreateFormula',
          name: `${options.id} tonic`,
          components: recipe,
          processId: 'simple_mix',
          priceCents,
          claims,
        });
        if (!r.ok) return;
        created = true;
        obs = t.observe();
        formula = obs.formulas[0];
        if (!formula) return;
      } else if (formula.claims.length < claims.length) {
        t.act({ type: 'SetClaims', formulaId: formula.id, claims });
      }

      // A quarter or a half column when the Gazette allows it and the till can stand it.
      if (obs.canAdvertise) {
        const want = obs.cashCents > 6000 ? adCents : 0;
        if (obs.adSpendCents !== want) t.act({ type: 'SetAdSpend', cents: want });
      }

      // Stock by eye: a shelf the proprietor means to keep, raised after a month the shelf went
      // bare and lowered after one with plenty left over, never quite right; the orders from away
      // the paper printed, on top; in dozens; and now and then the still is simply forgotten.
      obs = t.observe();
      const last = obs.lastMonth;
      if (last) {
        const k = t.rng.float();
        const short =
          last.wholesale.delivered < last.wholesale.ordered ||
          last.post.delivered < last.post.ordered;
        if (formula.stock === 0 || short) {
          // The shelf went bare, or the paper said the orders went short: noticed now and then,
          // and put right by a little.
          if (t.rng.chance(0.4)) shelf *= 1.1 + 0.15 * k;
        } else if (formula.stock > shelf * 0.4) shelf *= 0.8 + 0.15 * k;
        else shelf *= 0.95 + 0.1 * k;
        shelf = Math.max(24, shelf);
      }
      if (t.rng.chance(0.08)) return;
      const away = last ? last.wholesale.ordered + last.post.ordered : 0;
      const guess = shelf + away * (0.8 + 0.4 * t.rng.float());
      let wanted = Math.round((guess - formula.stock) / BATCH_STEP) * BATCH_STEP;
      const unit = Math.max(1, formula.unitCostCents);
      wanted = Math.min(wanted, Math.floor(Math.max(0, obs.cashCents - 2 * rent) / unit));
      const most = Math.floor(obs.stillBottles / BATCH_STEP) * BATCH_STEP;
      let full = 0;
      while (wanted >= BATCH_STEP && t.observe().stillsFree > 0) {
        const bottles = Math.min(most, Math.floor(wanted / BATCH_STEP) * BATCH_STEP);
        for (const c of recipe) {
          const have = t.observe().inventory[c.ingredientId] ?? 0;
          const need = Math.ceil(c.amount * bottles - have - 1e-9);
          if (need > 0) t.act({ type: 'BuyIngredient', ingredientId: c.ingredientId, units: need });
        }
        if (!t.act({ type: 'BrewBatch', formulaId: formula.id, bottles }).ok) break;
        if (bottles === most) full += 1;
        wanted -= bottles;
      }
      litEveryStill = full > 0 && full === obs.stills;
    },
  };
}
