import { describe, expect, it } from 'vitest';
import type { ContentPack } from '@panacea/content';
import {
  analyzeFormula,
  checkInvariants,
  newScratch,
  Rng,
  type Bought,
  type Customer,
  type FormulaComponent,
  type GameState,
} from '../src/index.ts';
import { bodyPhase } from '../src/systems/body.ts';
import {
  carefulCallers,
  chooseOffer,
  labelAppeal,
  narcoticHabit,
  refusesNarcotic,
  salesPhase,
  trustedByWary,
  type Buyer,
  type Offer,
} from '../src/systems/market.ts';
import { bribeCostCents, bribeExposureChance, resolveInquest } from '../src/systems/scrutiny.ts';
import { zeroTolerance } from '../src/systems/pharmacology.ts';
import { act, content, roomFor, start } from './helpers.ts';

/** Round-9 balance: mild bottles, the careful households, and the coroner's envelope. */

const recipe = (alcohol: number, laudanum = 0): FormulaComponent[] => [
  { ingredientId: 'grain_alcohol', amount: alcohol },
  ...(laudanum > 0 ? [{ ingredientId: 'laudanum', amount: laudanum }] : []),
  { ingredientId: 'valerian', amount: 1 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 6 - alcohol - laudanum },
];
/** The cozy player's mild tonic: 2 oz of spirit (habit 0.8, the assay's "none"). */
const MILD = recipe(2);
/** A stronger spirit tonic: 2.5 oz (habit 1, "they may come back"). */
const SPIRITED = recipe(2.5);
const QUARTER_OZ = recipe(2, 0.25);
const HALF_OZ = recipe(2, 0.5);

function stocked(
  components: FormulaComponent[],
  priceCents: number,
  bottles: number,
  seed: string,
  claims: string[] = [],
) {
  let s = start(seed);
  s.player.cashCents = 1_000_000;
  s.month.startCashCents = s.player.cashCents;
  s = act(s, {
    type: 'CreateFormula',
    name: 'T',
    components,
    processId: 'simple_mix',
    priceCents,
    claims,
  });
  for (const c of components) {
    s = act(s, {
      type: 'BuyIngredient',
      ingredientId: c.ingredientId,
      units: Math.ceil(c.amount * bottles),
    });
  }
  roomFor(s, bottles);
  s = act(s, { type: 'BrewBatch', formulaId: s.lab.formulas[0]!.id, bottles });
  s.lab.stock.push(
    ...s.lab.batches.map((b) => ({
      id: `lot_${b.id}`,
      formulaId: b.formulaId,
      components: b.components,
      processId: b.processId,
      bottles: b.bottles,
      potency: 1,
      badBatch: false,
    })),
  );
  s.lab.batches = [];
  return s;
}

const offerOf = (components: FormulaComponent[], priceCents = 60, claims: string[] = []): Offer => {
  const formula = stocked(components, priceCents, 1, 'offer', claims).lab.formulas[0]!;
  return { formula, analysis: analyzeFormula(components, 'simple_mix', content) };
};

const buyer = (wary: boolean, segment = 'mother'): Buyer => {
  const seg = content.byId.segments.get(segment)!;
  return {
    profile: seg.profile,
    ailment: seg.ailments[0]!.id,
    tolerance: zeroTolerance(),
    dependency: 0,
    wary,
  };
};

describe('a mild bottle makes a taste for it, never a craving (round-9 balance)', () => {
  const { habitFloor, mildDependencyCap, withdrawalThreshold } = content.config.body;

  /** One healthy regular takes three bottles of this recipe a month for two years. */
  const twoYears = (components: FormulaComponent[], dependency = 0): Customer => {
    const s = start('mild');
    const c = s.customers.find((x) => x.consumer === 'self')!;
    s.customers = [c];
    c.body.dependency = dependency;
    c.body.health = 100;
    const bought: Bought = {
      formulaId: 'f',
      lotId: 'lot',
      components,
      processId: 'simple_mix',
      potency: 1,
      badBatch: false,
    };
    for (let m = 0; m < 24; m++) {
      const scratch = newScratch();
      scratch.purchases.set(c.id, [bought, bought, bought]);
      bodyPhase.run({ state: s, content, rng: Rng.fromSeed(`mild${m}`), scratch });
      c.body.health = 100;
      if (!s.customers.includes(c)) s.customers = [c];
    }
    return c;
  };

  it('the assay reads 2 oz of spirit as under the floor, and 2.5 oz at it', () => {
    expect(analyzeFormula(MILD, 'simple_mix', content).properties.habit).toBeLessThan(habitFloor);
    expect(analyzeFormula(SPIRITED, 'simple_mix', content).properties.habit).toBeGreaterThanOrEqual(
      habitFloor,
    );
    expect(mildDependencyCap).toBeLessThan(withdrawalThreshold);
  });

  it('a regular on a mild bottle never comes to crave it', () => {
    const c = twoYears(MILD);
    expect(c.body.dependency).toBeGreaterThan(0);
    expect(c.body.dependency).toBeLessThanOrEqual(mildDependencyCap + 1e-12);
    expect(c.body.dependency).toBeLessThan(withdrawalThreshold);
  });

  it('a stronger spirit tonic, or any laudanum, can make a craving', () => {
    expect(twoYears(SPIRITED).body.dependency).toBeGreaterThan(withdrawalThreshold);
    expect(twoYears(QUARTER_OZ).body.dependency).toBeGreaterThan(withdrawalThreshold);
  });

  it('a habit already made does not vanish when the bottle is made mild: it fades as it would', () => {
    const c = twoYears(MILD, 0.8);
    expect(c.body.dependency).toBeLessThan(0.8);
    expect(c.body.dependency).toBeGreaterThan(mildDependencyCap);
  });
});

describe('the careful households (round-9 balance)', () => {
  it('the ward has wary households, and three of the starting regulars are wary', () => {
    expect(content.byId.segments.get('mother')!.wary).toBe(0.4);
    expect(content.regulars.filter((r) => r.wary)).toHaveLength(3);
    expect(start('w').customers.filter((c) => c.wary)).toHaveLength(3);
    expect(content.config.body.habitFloor).toBe(1);
    expect(content.config.market.waryHabit).toBe(2);
  });

  const { waryHabit, cleanAppeal, waryBoldness } = content.config.market;

  it('a wary buyer will not take a bottle that smells of the poppy; a trace passes', () => {
    expect(narcoticHabit(offerOf(HALF_OZ).analysis)).toBeGreaterThanOrEqual(waryHabit);
    expect(narcoticHabit(offerOf(QUARTER_OZ).analysis)).toBeLessThan(waryHabit);
    expect(refusesNarcotic(buyer(true), offerOf(HALF_OZ).analysis, content)).toBe(true);
    expect(refusesNarcotic(buyer(true), offerOf(QUARTER_OZ).analysis, content)).toBe(false);
    expect(refusesNarcotic(buyer(false), offerOf(HALF_OZ).analysis, content)).toBe(false);
  });

  it('a clean bottle with a modest label is its own recommendation to them, and only to them', () => {
    const clean = offerOf(MILD, 60, ['soothes_nerves']);
    const bold = offerOf(MILD, 60, ['cures_everything']);
    const traced = offerOf(QUARTER_OZ, 60, ['soothes_nerves']);
    expect(trustedByWary(clean, content)).toBe(true);
    expect(trustedByWary(bold, content)).toBe(false);
    expect(trustedByWary(traced, content)).toBe(false);
    expect(content.byId.claims.get('cures_everything')!.boldness).toBeGreaterThan(waryBoldness);
    const plain = labelAppeal(clean, buyer(false), content);
    expect(labelAppeal(clean, buyer(true), content)).toBeCloseTo(plain + cleanAppeal);
    expect(labelAppeal(bold, buyer(true), content)).toBeCloseTo(
      labelAppeal(bold, buyer(false), content),
    );
  });

  it('at the same price the wary take the clean bottle; the rest may take the laudanum', () => {
    const s = stocked(MILD, 60, 10, 'two');
    const clean: Offer = {
      formula: s.lab.formulas[0]!,
      analysis: analyzeFormula(MILD, 'simple_mix', content),
    };
    const narcotic: Offer = { ...offerOf(HALF_OZ, 60), formula: { ...clean.formula, id: 'n' } };
    s.lab.formulas.push(narcotic.formula);
    s.lab.stock.push({ ...s.lab.stock[0]!, id: 'lot_n', formulaId: 'n', components: HALF_OZ });
    const labourer = buyer(false, 'labourer');
    expect(chooseOffer(s, [clean, narcotic], labourer, content)?.formula.id).toBe('n');
    expect(
      chooseOffer(s, [clean, narcotic], { ...labourer, wary: true }, content)?.formula.id,
    ).toBe(clean.formula.id);
  });

  it('wary regulars who will not take the bottle are counted, apart from the price', () => {
    const s = stocked(HALF_OZ, 40, 200, 'refuse');
    for (const c of s.customers) {
      c.wary = true;
      c.satisfaction = 1;
    }
    const pack: ContentPack = {
      ...content,
      config: { ...content.config, walkIns: { ...content.config.walkIns, base: 0 } },
    };
    s.player.reputation = 0;
    s.month.startReputation = 0;
    salesPhase.run({ state: s, content: pack, rng: Rng.fromSeed('refuse'), scratch: newScratch() });
    expect(s.month.sales.reduce((n, x) => n + x.shopBottles, 0)).toBe(0);
    expect(s.month.refusedNarcotic).toBeGreaterThan(3);
    expect(checkInvariants(s, pack)).toEqual([]);
  });

  it('pleased wary regulars send callers while the shelf holds a bottle they trust', () => {
    const offers = (components: FormulaComponent[], claims: string[]) => [
      offerOf(components, 60, claims),
    ];
    const s: GameState = start('careful');
    const wary = s.customers.filter((c) => c.wary);
    expect(wary.length).toBeGreaterThan(0);
    for (const c of s.customers) c.satisfaction = 0.5;
    const per = content.config.walkIns.carefulPerWary;
    const expected = Math.floor(per * wary.length);
    expect(carefulCallers(s, offers(MILD, ['soothes_nerves']), content)).toBe(expected);
    expect(carefulCallers(s, offers(MILD, ['cures_everything']), content)).toBe(0);
    expect(carefulCallers(s, offers(HALF_OZ, []), content)).toBe(0);
    for (const c of wary) c.satisfaction = -0.5;
    expect(carefulCallers(s, offers(MILD, ['soothes_nerves']), content)).toBe(0);
  });
});

describe("the coroner's envelope (round-9 balance)", () => {
  const withInquest = (seed: string): GameState => {
    const s = start(seed);
    s.player.cashCents = 1_000_000;
    s.month.startCashCents = s.player.cashCents;
    s.unlocks.unlocked['coroner_fees'] = 0;
    s.scrutiny.heat = 40;
    s.month.startHeat = 40;
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
      cause: 'overdose',
      reportedCause: 'overdose',
      productImplicated: true,
      formulaId: null,
      badBatch: false,
      detectability: 5,
      boldness: 0,
      inquest: 'pending',
      verdict: null,
    });
    return s;
  };

  it('a bought verdict does not quiet the talk, and the first envelope can already come out', () => {
    let bought = 0;
    let exposed = 0;
    for (let i = 0; i < 80; i++) {
      const s = withInquest(`env${i}`);
      const rng = Rng.fromSeed(`env${i}`);
      expect(resolveInquest(s, content, rng, 'death_x', 'bribe')).toBeNull();
      if (s.deaths[0]!.inquest === 'bribed') {
        bought += 1;
        expect(s.scrutiny.heat).toBeGreaterThanOrEqual(40);
      } else exposed += 1;
    }
    expect(bought).toBeGreaterThan(50);
    expect(exposed).toBeGreaterThan(2);
    expect(bribeExposureChance(withInquest('x'), content)).toBeGreaterThanOrEqual(
      content.config.scrutiny.bribeExposureBase,
    );
  });

  it('each envelope costs more than the last, and each makes the next likelier to come out', () => {
    const s = withInquest('fees');
    const fees: number[] = [];
    const risks: number[] = [];
    for (let k = 0; k < 4; k++) {
      fees.push(bribeCostCents(s, content));
      risks.push(bribeExposureChance(s, content));
      s.scrutiny.lastBribeCents = fees[k]!;
      s.scrutiny.bribesPaid += 1;
    }
    for (let k = 1; k < 4; k++) {
      expect(fees[k]! - fees[k - 1]!).toBeGreaterThanOrEqual(500);
      expect(risks[k]!).toBeGreaterThan(risks[k - 1]!);
    }
    // by the fourth envelope the fee has doubled
    expect(fees[3]!).toBeGreaterThanOrEqual(2 * fees[0]!);
  });
});
