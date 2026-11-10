import type { ContentPack } from '@panacea/content';
import { describe, expect, it } from 'vitest';
import {
  acuteDanger,
  analyzeFormula,
  applyAction,
  bribeCostCents,
  bribeExposureChance,
  checkInvariants,
  implicationChance,
  endTurn,
  newScratch,
  observe,
  overdoseChance,
  reliefScore,
  Rng,
  seedRng,
  stockOf,
  stockValue,
  type Bought,
  type Customer,
  type FormulaComponent,
  type GameState,
  type TurnContext,
} from '../src/index.ts';
import { bodyPhase } from '../src/systems/body.ts';
import { productionPhase } from '../src/systems/lab.ts';
import {
  advertisingPhase,
  meansCents,
  priceHesitation,
  salesPhase,
} from '../src/systems/market.ts';
import { zeroTolerance } from '../src/systems/pharmacology.ts';
import { resolveInquest } from '../src/systems/scrutiny.ts';
import { act, content, months, roomFor, start } from './helpers.ts';

const LAUDANUM_TONIC: FormulaComponent[] = [
  { ingredientId: 'grain_alcohol', amount: 2 },
  { ingredientId: 'laudanum', amount: 0.5 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 4.5 },
];
const MERCURY_PILL: FormulaComponent[] = [
  { ingredientId: 'calomel', amount: 1 },
  { ingredientId: 'water', amount: 5 },
];
const DEATH_IN_A_BOTTLE: FormulaComponent[] = [
  { ingredientId: 'morphine', amount: 5 },
  { ingredientId: 'water', amount: 3 },
];

const bottle = (components: FormulaComponent[], potency = 1, formulaId = 'f'): Bought => ({
  formulaId,
  lotId: `lot_${JSON.stringify(components)}`,
  components,
  processId: 'simple_mix',
  potency,
  badBatch: false,
});

/** A game with a single regular, for isolating the body model. */
function oneCustomer(seed = 'body'): { state: GameState; customer: Customer } {
  const state = start(seed);
  const customer = state.customers.find((c) => c.segment === 'labourer')!;
  state.customers = [customer];
  return { state, customer };
}

function runBody(state: GameState, purchases: Record<string, Bought[]>, seed = 'b', c = content) {
  const ctx: TurnContext = { state, content: c, rng: Rng.fromSeed(seed), scratch: newScratch() };
  for (const [id, bought] of Object.entries(purchases)) ctx.scratch.purchases.set(id, bought);
  bodyPhase.run(ctx);
  return ctx;
}

describe('body model', () => {
  it('tolerance builds with use and blunts the felt effect', () => {
    const { state, customer } = oneCustomer();
    const a = analyzeFormula(LAUDANUM_TONIC, 'simple_mix', content);
    const fresh = reliefScore(a, zeroTolerance(), customer.profile);
    for (let i = 0; i < 12; i++)
      runBody(state, { [customer.id]: [bottle(LAUDANUM_TONIC)] }, `t${i}`);
    expect(customer.body.tolerance.opiate).toBeGreaterThan(0.2);
    expect(reliefScore(a, customer.body.tolerance, customer.profile)).toBeLessThan(fresh * 0.8);
  });

  it('dependency grows, and a missed month brings withdrawal', () => {
    const { state, customer } = oneCustomer();
    for (let i = 0; i < 24; i++)
      runBody(state, { [customer.id]: [bottle(LAUDANUM_TONIC), bottle(LAUDANUM_TONIC)] }, `d${i}`);
    expect(customer.body.dependency).toBeGreaterThan(content.config.body.withdrawalThreshold);
    const healthBefore = customer.body.health;
    runBody(state, { [customer.id]: [] }, 'dry');
    expect(customer.inWithdrawal).toBe(true);
    expect(customer.body.health).toBeLessThan(healthBefore);
    expect(state.month.complaints).toBeGreaterThan(0);
  });

  it('a lethal bottle kills, and the death is recorded with its true cause', () => {
    const { state, customer } = oneCustomer();
    runBody(state, { [customer.id]: [bottle(DEATH_IN_A_BOTTLE)] });
    expect(state.customers).toHaveLength(0);
    expect(state.deaths).toHaveLength(1);
    expect(state.deaths[0]).toMatchObject({
      name: customer.name,
      cause: 'overdose',
      productImplicated: true,
      anonymous: false,
    });
    expect(state.scrutiny.heat).toBeGreaterThan(0);
    expect(checkInvariants(state, content)).toEqual([]);
  });

  it('tolerance protects against a drug class, and losing it makes the same dose dangerous', () => {
    const a = analyzeFormula([{ ingredientId: 'morphine', amount: 2 }], 'simple_mix', content);
    const dose = { potency: 1, doseScale: 1, vulnerability: 1 };
    const naive = acuteDanger(a, zeroTolerance(), dose, content);
    const tolerant = acuteDanger(a, { ...zeroTolerance(), opiate: 2 }, dose, content);
    expect(tolerant).toBeLessThan(naive / 2);
    expect(overdoseChance(naive, 75)).toBeGreaterThan(overdoseChance(tolerant, 75));
  });

  it('heavy metals accumulate and wear health down slowly', () => {
    const { state, customer } = oneCustomer();
    const start = customer.body.health;
    for (let i = 0; i < 6; i++) runBody(state, { [customer.id]: [bottle(MERCURY_PILL)] }, `m${i}`);
    expect(customer.body.load).toBeGreaterThan(content.config.body.loadSafe);
    expect(customer.body.health).toBeLessThan(start - 5);
  });

  it('infants take smaller doses but are far more vulnerable', () => {
    const a = analyzeFormula(LAUDANUM_TONIC, 'simple_mix', content);
    const { infantDoseScale, infantVulnerability } = content.config.body;
    const adult = acuteDanger(
      a,
      zeroTolerance(),
      { potency: 1, doseScale: 1, vulnerability: 1 },
      content,
    );
    const infant = acuteDanger(
      a,
      zeroTolerance(),
      { potency: 1, doseScale: infantDoseScale, vulnerability: infantVulnerability },
      content,
    );
    expect(infant).toBeGreaterThan(adult);
  });
});

function stocked(components: FormulaComponent[], bottles: number, seed = 'm') {
  let s = start(seed);
  s.player.cashCents = 1_000_000;
  s.month.startCashCents = s.player.cashCents;
  s = act(s, {
    type: 'CreateFormula',
    name: 'T',
    components,
    processId: 'simple_mix',
    priceCents: 50,
  });
  for (const c of components) {
    s = act(s, {
      type: 'BuyIngredient',
      ingredientId: c.ingredientId,
      units: Math.ceil(c.amount * bottles),
    });
  }
  const formulaId = s.lab.formulas[0]!.id;
  roomFor(s, bottles);
  s = act(s, { type: 'BrewBatch', formulaId, bottles });
  productionPhase.run({ state: s, content, rng: Rng.fromSeed(seed), scratch: newScratch() });
  return { state: s, formulaId };
}

describe('market', () => {
  it('dependent regulars buy more bottles than new ones', () => {
    let dependent = 0;
    let fresh = 0;
    for (let i = 0; i < 20; i++) {
      const { state } = stocked(LAUDANUM_TONIC, 200, `dep${i}`);
      const [a, b] = state.customers;
      a!.body.dependency = 0.9;
      b!.body.dependency = 0;
      // neither is wary of the laudanum in it (round-9 balance)
      a!.wary = false;
      b!.wary = false;
      const ctx: TurnContext = {
        state,
        content,
        rng: Rng.fromSeed(`s${i}`),
        scratch: newScratch(),
      };
      salesPhase.run(ctx);
      dependent += ctx.scratch.purchases.get(a!.id)?.length ?? 0;
      fresh += ctx.scratch.purchases.get(b!.id)?.length ?? 0;
    }
    expect(dependent).toBeGreaterThan(fresh * 1.5);
  });

  it('satisfied walk-ins become regulars, up to the shop’s capacity', () => {
    let joined = 0;
    for (let i = 0; i < 10; i++) {
      const { state } = stocked(LAUDANUM_TONIC, 300, `walk${i}`);
      const before = state.customers.length;
      salesPhase.run({ state, content, rng: Rng.fromSeed(`w${i}`), scratch: newScratch() });
      joined += state.customers.length - before;
    }
    expect(joined).toBeGreaterThan(0);

    const { state } = stocked(LAUDANUM_TONIC, 300, 'full');
    while (state.customers.length < content.config.maxRegulars)
      state.customers.push({ ...state.customers[0]!, id: `x${state.customers.length}` });
    salesPhase.run({ state, content, rng: Rng.fromSeed('full'), scratch: newScratch() });
    expect(state.customers.length).toBe(content.config.maxRegulars);
  });

  it('bold label claims draw heat even without advertising', () => {
    const { state, formulaId } = stocked(LAUDANUM_TONIC, 10);
    state.lab.formulas.find((f) => f.id === formulaId)!.claims = ['cures_everything'];
    advertisingPhase.run({ state, content, rng: Rng.fromSeed('ad'), scratch: newScratch() });
    expect(state.scrutiny.heat).toBeGreaterThan(0);
  });
});

describe('lab', () => {
  it('reformulating changes future batches, not bottles already on the shelf', () => {
    const { state, formulaId } = stocked(LAUDANUM_TONIC, 10);
    const s = act(state, {
      type: 'Reformulate',
      formulaId,
      components: MERCURY_PILL,
      processId: 'simple_mix',
    });
    expect(s.lab.stock[0]!.components).toEqual(LAUDANUM_TONIC);
    expect(s.lab.formulas[0]!.components).toEqual(MERCURY_PILL);
  });

  it('quality control catches bad batches; without it they reach the shelf', () => {
    const alwaysBad: ContentPack = {
      ...content,
      config: { ...content.config, batches: { ...content.config.batches, badBatchChance: 1 } },
    };
    const run = (qc: boolean) => {
      let s = start('qc');
      if (qc) s.player.capabilities['quality_control'] = 1;
      s = act(s, {
        type: 'CreateFormula',
        name: 'T',
        components: LAUDANUM_TONIC,
        processId: 'simple_mix',
        priceCents: 50,
      });
      for (const c of LAUDANUM_TONIC)
        s = act(s, { type: 'BuyIngredient', ingredientId: c.ingredientId, units: 10 });
      s = act(s, { type: 'BrewBatch', formulaId: s.lab.formulas[0]!.id, bottles: 2 });
      productionPhase.run({
        state: s,
        content: alwaysBad,
        rng: Rng.fromSeed('qc'),
        scratch: newScratch(),
      });
      return s;
    };
    const without = run(false);
    expect(without.lab.stock[0]).toMatchObject({ badBatch: true });
    expect(without.lab.stock[0]!.potency).toBeGreaterThanOrEqual(2);
    const withQc = run(true);
    expect(withQc.lab.stock).toHaveLength(0);
    expect(withQc.month.news).toContainEqual({
      kind: 'bad_batch_caught',
      formulaId: withQc.lab.formulas[0]!.id,
      bottles: 2,
    });
  });

  it('a wholesale supplier discounts ingredients', () => {
    const s = start();
    s.player.capabilities['ingredient_discount_pct'] = 15;
    const after = act(s, { type: 'BuyIngredient', ingredientId: 'laudanum', units: 100 });
    expect(s.player.cashCents - after.player.cashCents).toBe(Math.round(100 * 15 * 0.85));
  });

  it('advertising requires the unlock', () => {
    const r = applyAction(start(), { type: 'SetAdSpend', cents: 500 }, content);
    expect(r.result).toEqual({ ok: false, error: 'newspaper advertising is not unlocked' });
  });
});

/** A state with one recorded death awaiting an inquest. */
function withInquest(productImplicated: boolean, detectability = 4, seed = 'inq'): GameState {
  const s = start(seed);
  s.deaths.push({
    id: 'death_x',
    turn: 0,
    date: { ...s.meta.date },
    customerId: null,
    name: 'Mr. Test',
    segment: 'labourer',
    consumer: 'self',
    anonymous: false,
    ailment: 'rheumatism',
    cause: productImplicated ? 'overdose' : 'natural',
    reportedCause: productImplicated ? 'overdose' : 'natural',
    productImplicated,
    formulaId: null,
    badBatch: false,
    detectability,
    boldness: 0,
    promisedCure: false,
    owedCents: 0,
    inquest: 'pending',
    verdict: null,
  });
  s.events.pending.push({
    instanceId: 'inq_1',
    eventId: 'inquest',
    drawnTurn: 0,
    deathId: 'death_x',
  });
  return s;
}

describe('scrutiny', () => {
  it('bribing the coroner needs the unlock and the cash', () => {
    const s = withInquest(true);
    const choices = observe(s, content).pendingEvents[0]!.choiceIds;
    expect(choices).not.toContain('bribe_coroner');
    expect(
      applyAction(
        s,
        { type: 'ResolveEvent', instanceId: 'inq_1', choiceId: 'bribe_coroner' },
        content,
      ).result.ok,
    ).toBe(false);

    s.unlocks.unlocked['coroner_fees'] = 0;
    expect(observe(s, content).pendingEvents[0]!.choiceIds).toContain('bribe_coroner');
    const cost = bribeCostCents(s, content);
    const after = act(s, { type: 'ResolveEvent', instanceId: 'inq_1', choiceId: 'bribe_coroner' });
    expect(after.player.cashCents).toBe(s.player.cashCents - cost);
    expect(after.deaths[0]).toMatchObject({ inquest: 'bribed' });
    expect(content.config.scrutiny.benignVerdicts).toContain(after.deaths[0]!.verdict);
    // he comes back for more: each fee paid raises the next (named to the nearest five dollars)
    expect(bribeCostCents(after, content)).toBeGreaterThanOrEqual(cost);
    const regular = structuredClone(after);
    regular.scrutiny.bribesPaid += 4;
    expect(bribeCostCents(regular, content)).toBeGreaterThan(cost);
  });

  it('juries usually blame a detectable poison, and rarely an innocent death', () => {
    let guilty = 0;
    let innocent = 0;
    for (let i = 0; i < 100; i++) {
      const a = withInquest(true, 20, `g${i}`);
      resolveInquest(a, content, Rng.fromSeed(`g${i}`), 'death_x', 'silent');
      if (a.deaths[0]!.inquest === 'implicated') guilty++;
      const b = withInquest(false, 20, `n${i}`);
      resolveInquest(b, content, Rng.fromSeed(`n${i}`), 'death_x', 'silent');
      if (b.deaths[0]!.inquest === 'implicated') innocent++;
    }
    expect(guilty).toBeGreaterThan(70);
    expect(innocent).toBeLessThan(20);
  });

  it('being implicated hurts reputation and raises heat; surviving any inquest unlocks the coroner', () => {
    let s = withInquest(true, 50, 'inq2');
    s.scrutiny.heat = 50;
    s.month.startHeat = 50;
    s = act(s, { type: 'ResolveEvent', instanceId: 'inq_1', choiceId: 'stay_silent' });
    expect(s.deaths[0]!.inquest).toBe('implicated');
    expect(s.player.reputation).toBeLessThan(content.config.startingReputation);
    expect(s.scrutiny.heat).toBeGreaterThan(50);
    expect(s.player.flags['survived_inquest']).toBe(true);
    s = months(s, 1);
    expect('coroner_fees' in s.unlocks.unlocked).toBe(true);
  });

  it('the authorities close the shop after heat stays at the limit through the grace period', () => {
    // Heat that never fades, so it stays pinned at the limit.
    // No letters either: a letter's reply (the Board's fine, the League's donation) lowers it.
    const noDecay: ContentPack = {
      ...content,
      events: content.events.filter((e) => e.trigger === 'system'),
      config: {
        ...content.config,
        scrutiny: {
          ...content.config.scrutiny,
          heatDecayFlat: 0,
          heatDecayRate: 0,
          heatDecayHigh: 0,
        },
      },
    };
    const s = start('heat');
    s.events.pending = [];
    s.scrutiny.heat = 100;
    s.month.startHeat = 100;
    s.player.cashCents = 1_000_000;
    s.month.startCashCents = s.player.cashCents;
    const first = endTurn(s, noDecay);
    expect(first.state.meta.status).toBe('running');
    expect(first.report.news).toContainEqual({ kind: 'shutdown_warning', monthsLeft: 1 });
    const second = endTurn(first.state, noDecay);
    expect(second.state.meta.endReason).toBe('shut_down');
  });

  it('heat dies down when nothing happens', () => {
    let s = start();
    s.scrutiny.heat = 40;
    s.month.startHeat = 40;
    s.player.cashCents = 1_000_000;
    s.month.startCashCents = s.player.cashCents;
    s = months(s, 6);
    expect(s.scrutiny.heat).toBeLessThan(30);
  });
});

describe('what the player can see', () => {
  it('shows appearances and verdicts, never exact health or the true cause of death', () => {
    const s = withInquest(true);
    const o = observe(s, content);
    const customer = o.customers[0] as unknown as Record<string, unknown>;
    expect(customer['looks']).toBeDefined();
    expect(customer['body']).toBeUndefined();
    expect(customer['health']).toBeUndefined();
    const death = o.deaths[0] as unknown as Record<string, unknown>;
    expect(death['cause']).toBeUndefined();
    expect(death['productImplicated']).toBeUndefined();
  });

  it('stock counts are summed across lots', () => {
    const { state, formulaId } = stocked(LAUDANUM_TONIC, 12);
    expect(stockOf(state, formulaId)).toBe(12);
    expect(observe(state, content).formulas[0]!.stock).toBe(12);
  });
});

describe('the habit, the coroner and the post (balance pass 2)', () => {
  const sellTo = (state: GameState, seed: string) => {
    const ctx: TurnContext = { state, content, rng: Rng.fromSeed(seed), scratch: newScratch() };
    salesPhase.run(ctx);
    return ctx;
  };

  it('a hooked regular who spends beyond their means runs up a debt, and once ruined buys one bottle', () => {
    let ruinedBottles = 0;
    let solventBottles = 0;
    for (let i = 0; i < 20; i++) {
      const { state, formulaId } = stocked(LAUDANUM_TONIC, 300, `ruin${i}`);
      state.lab.formulas.find((f) => f.id === formulaId)!.priceCents = 150;
      const [a, b] = state.customers;
      for (const c of [a!, b!]) {
        c.body.dependency = 0.95;
        c.satisfaction = 0.5;
      }
      const means = meansCents(a!, content);
      a!.debtCents = content.config.market.ruinMonths * means + 1;
      expect(observe(state, content).customers.find((c) => c.id === a!.id)!.ruined).toBe(true);
      const ctx = sellTo(state, `r${i}`);
      const boughtA = ctx.scratch.purchases.get(a!.id) ?? [];
      const boughtB = ctx.scratch.purchases.get(b!.id) ?? [];
      expect(boughtA.length).toBeLessThanOrEqual(1);
      ruinedBottles += boughtA.length;
      solventBottles += boughtB.length;
      // the ruined regular pays nothing over the price; the solvent one pays the craving's premium
      const premium = state.month.sales.reduce((n, x) => n + x.cravingPremiumCents, 0);
      const spentB = boughtB.length * 150 + premium;
      const meansB = meansCents(b!, content);
      expect(b!.debtCents).toBe(Math.max(0, spentB - meansB));
    }
    expect(solventBottles).toBeGreaterThan(ruinedBottles * 1.5);
  });

  it('a ruined regular’s family complains', () => {
    const { state, customer } = oneCustomer('family');
    customer.debtCents = 1_000_000;
    runBody(state, { [customer.id]: [] });
    expect(state.month.complaints).toBe(1);
  });

  it('counts the callers who asked the price and left', () => {
    const { state, formulaId } = stocked(LAUDANUM_TONIC, 100, 'dear');
    state.lab.formulas.find((f) => f.id === formulaId)!.priceCents = 5000;
    const ctx = sellTo(state, 'dear');
    expect(state.month.sales).toEqual([]);
    expect(state.month.turnedAwayByPrice).toBeGreaterThan(0);
    const report = endTurn(ctx.state, content).report;
    expect(report.turnedAwayByPrice).toBeGreaterThanOrEqual(state.month.turnedAwayByPrice);
  });

  it('a bribe can come out: the fee is gone and the verdict goes against you', () => {
    const base = withInquest(true);
    base.unlocks.unlocked['coroner_fees'] = 0;
    // even the first envelope can come out (round-9 balance)
    const { bribeExposureBase, bribeExposurePerBribe, bribeExposurePerNotoriety } =
      content.config.scrutiny;
    expect(bribeExposureChance(base, content)).toBeCloseTo(bribeExposureBase);
    expect(bribeExposureBase).toBeGreaterThan(0);
    base.scrutiny.bribesPaid = 5;
    base.player.notoriety = 40;
    base.month.startNotoriety = 40;
    const chance = bribeExposureChance(base, content);
    expect(chance).toBeCloseTo(
      bribeExposureBase + 5 * bribeExposurePerBribe + 40 * bribeExposurePerNotoriety,
    );
    expect(chance).toBeCloseTo(0.4);
    expect(observe(base, content).pendingEvents[0]!.bribeExposureChance).toBeCloseTo(chance);
    let exposed = 0;
    for (let i = 0; i < 60; i++) {
      const s = structuredClone(base);
      s.meta.rng = seedRng(`expose${i}`);
      const cost = bribeCostCents(s, content);
      const after = act(s, {
        type: 'ResolveEvent',
        instanceId: 'inq_1',
        choiceId: 'bribe_coroner',
      });
      expect(after.player.cashCents).toBeLessThanOrEqual(s.player.cashCents - cost);
      if (after.month.news.some((n) => n.kind === 'bribe_exposed')) {
        exposed += 1;
        expect(after.deaths[0]).toMatchObject({ inquest: 'implicated' });
        expect(after.scrutiny.heat).toBeGreaterThan(s.scrutiny.heat + 20);
      } else {
        expect(after.deaths[0]).toMatchObject({ inquest: 'bribed' });
      }
    }
    expect(exposed).toBeGreaterThan(10);
    expect(exposed).toBeLessThan(40);
  });

  it('a rival’s sale lasts four months, and only takes trade dearer than his', () => {
    let s = start('rival');
    s.player.cashCents = 1_000_000;
    s.month.startCashCents = s.player.cashCents;
    s.events.pending = [{ instanceId: 'r1', eventId: 'rival_price_war', drawnTurn: 0 }];
    s = act(s, { type: 'ResolveEvent', instanceId: 'r1', choiceId: 'let_him_try' });
    expect(s.player.capabilities['rival_share_pct']).toBe(40);
    expect(s.player.capabilities['rival_price_cents']).toBe(65);
    s = months(s, 3);
    expect(s.player.capabilities['rival_share_pct']).toBe(40);
    s = months(s, 1);
    expect(s.player.capabilities['rival_share_pct']).toBe(0);
    expect(s.player.capabilities['rival_price_cents']).toBe(0);
    expect(s.player.timedCapabilities).toEqual([]);

    // with the rival at full strength, a bottle dearer than his loses walk-ins; a cheaper one does not
    const sold = (price: number) => {
      let n = 0;
      for (let i = 0; i < 10; i++) {
        const { state, formulaId } = stocked(LAUDANUM_TONIC, 300, `rv${i}`);
        state.customers = [];
        state.lab.formulas.find((f) => f.id === formulaId)!.priceCents = price;
        state.player.capabilities['rival_share_pct'] = 100;
        state.player.capabilities['rival_price_cents'] = 65;
        sellTo(state, `rs${i}`);
        n += state.month.sales.reduce((t, x) => t + x.bottles, 0);
      }
      return n;
    };
    expect(sold(70)).toBe(0);
    expect(sold(60)).toBeGreaterThan(0);
  });

  it('a supplier’s rise makes one ingredient dearer until it runs out', () => {
    let s = start('spirits');
    const before = observe(s, content).ingredients.find((i) => i.id === 'grain_alcohol')!;
    s.events.pending = [{ instanceId: 'sp', eventId: 'spirits_dearer', drawnTurn: 0 }];
    s = act(s, { type: 'ResolveEvent', instanceId: 'sp', choiceId: 'pay_the_new_price' });
    const during = observe(s, content).ingredients.find((i) => i.id === 'grain_alcohol')!;
    expect(during.costPerUnitCents).toBe(before.costPerUnitCents * 2);
    s = months(s, 6);
    const after = observe(s, content).ingredients.find((i) => i.id === 'grain_alcohol')!;
    expect(after.costPerUnitCents).toBe(before.costPerUnitCents);
  });

  it('a subscription is booked as its own expense for as long as it runs', () => {
    let s = start('society');
    s.player.cashCents = 1_000_000;
    s.month.startCashCents = s.player.cashCents;
    s.events.pending = [{ instanceId: 'bs', eventId: 'benefit_society', drawnTurn: 0 }];
    s = act(s, { type: 'ResolveEvent', instanceId: 'bs', choiceId: 'subscribe' });
    s = months(s, 12);
    const paid = s.ledger.filter((e) => e.deltas.some((d) => d.cause === 'expense.retainer'));
    expect(paid).toHaveLength(12);
    expect(s.player.capabilities['walk_in_bonus']).toBe(0);
  });
});

describe('testimony, the habit’s cost, dear bottles and the books (balance pass 3)', () => {
  const record = (s: GameState) => s.deaths[0]!;

  it('testimony helps a mild bottle and damns a loaded one; silence is the better bet for a strong one', () => {
    const mild = withInquest(true, 2);
    expect(implicationChance(mild, record(mild), 'testify')).toBeLessThan(
      implicationChance(mild, record(mild), 'silent'),
    );
    const loaded = withInquest(true, 9);
    expect(implicationChance(loaded, record(loaded), 'testify')).toBeGreaterThan(
      implicationChance(loaded, record(loaded), 'silent'),
    );
  });

  it('silence keeps a mis-measured batch out of the record; testimony has it found', () => {
    const base = withInquest(true, 4);
    base.deaths[0]!.badBatch = true;
    const clean = withInquest(true, 4);
    expect(implicationChance(base, record(base), 'silent')).toBeCloseTo(
      implicationChance(clean, record(clean), 'silent'),
    );
    expect(implicationChance(base, record(base), 'testify')).toBeCloseTo(
      implicationChance(clean, record(clean), 'testify') + 0.2,
    );
    const silent = structuredClone(base);
    resolveInquest(silent, content, Rng.fromSeed('bb'), 'death_x', 'silent');
    expect(silent.player.flags['suffered_bad_batch']).toBeUndefined();
    const testified = structuredClone(base);
    resolveInquest(testified, content, Rng.fromSeed('bb'), 'death_x', 'testify');
    expect(testified.player.flags['suffered_bad_batch']).toBe(true);
  });

  it('a verdict against you costs the same heat however you answered', () => {
    const heatAfter = (approach: 'testify' | 'silent') => {
      for (let i = 0; i < 50; i++) {
        const s = withInquest(true, 40, `v${i}`);
        resolveInquest(s, content, Rng.fromSeed(`v${i}`), 'death_x', approach);
        if (s.deaths[0]!.inquest === 'implicated') return s.scrutiny.heat;
      }
      throw new Error('never implicated');
    };
    expect(heatAfter('testify')).toBe(heatAfter('silent'));
  });

  it('the habit eats a regular’s means', () => {
    const { customer } = oneCustomer('means');
    customer.body.dependency = 0;
    const sober = meansCents(customer, content);
    expect(sober).toBe(customer.profile.baseWillingnessCents * content.config.market.meansPerMonth);
    customer.body.dependency = 1;
    expect(meansCents(customer, content)).toBeCloseTo(
      sober * (1 - content.config.market.meansLostToHabit),
    );
  });

  it('a stranger hesitates over a bottle priced near the most they would pay', () => {
    const { priceHesitation: h, hesitationMargin } = content.config.market;
    expect(priceHesitation(50, 100, content)).toBe(1);
    expect(priceHesitation(100 * (1 - hesitationMargin), 100, content)).toBeCloseTo(1);
    expect(priceHesitation(100, 100, content)).toBeCloseTo(1 - h);
    expect(priceHesitation(90, 100, content)).toBeGreaterThan(1 - h);
    expect(priceHesitation(90, 100, content)).toBeLessThan(1);
  });

  it('the coroner’s fee follows the configured rise per bribe and with heat', () => {
    const s = withInquest(true);
    const { bribeBaseCents, bribeRisePerBribe, bribeHeatDoubling } = content.config.scrutiny;
    s.scrutiny.bribesPaid = 4;
    s.scrutiny.heat = 40;
    const raw = bribeBaseCents * (1 + 4 * bribeRisePerBribe) * (1 + 40 / bribeHeatDoubling);
    expect(bribeCostCents(s, content)).toBe(Math.max(500, Math.round(raw / 500) * 500));
  });

  it('stock bought is not money lost: the books value ingredients and bottles at cost', () => {
    let s = start('books');
    s.player.cashCents = 100_000;
    s.month.startCashCents = s.player.cashCents;
    expect(observe(s, content).stockValue).toEqual({
      shelfCents: 0,
      ingredientsCents: 0,
      totalCents: 0,
    });
    s = act(s, { type: 'BuyIngredient', ingredientId: 'morphine', units: 2 });
    s = act(s, { type: 'BuyIngredient', ingredientId: 'grain_alcohol', units: 20 });
    const spent = 100_000 - s.player.cashCents;
    expect(observe(s, content).stockValue).toEqual({
      shelfCents: 0,
      ingredientsCents: spent,
      totalCents: spent,
    });
    s = act(s, {
      type: 'CreateFormula',
      name: 'Books',
      components: [
        { ingredientId: 'morphine', amount: 0.1 },
        { ingredientId: 'grain_alcohol', amount: 2 },
      ],
      processId: 'simple_mix',
      priceCents: 100,
    });
    s = act(s, { type: 'BrewBatch', formulaId: s.lab.formulas[0]!.id, bottles: 10 });
    const brewing = observe(s, content).stockValue;
    const perBottle = 0.1 * 250 + 2 * 2 + content.config.bottleCostCents;
    expect(brewing.shelfCents).toBe(Math.round(10 * perBottle));
    expect(brewing.totalCents).toBe(spent + 10 * content.config.bottleCostCents);
    const { state, report } = endTurn(s, content);
    expect(report.stockValueStartCents).toBe(0);
    expect(report.stockValueEndCents).toBe(stockValue(state, content).totalCents - 0);
    expect(state.month.startStockValueCents).toBe(report.stockValueEndCents);
    const next = endTurn(state, content).report;
    expect(next.stockValueStartCents).toBe(report.stockValueEndCents);
  });
});
