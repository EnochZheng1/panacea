import { describe, expect, it } from 'vitest';
import {
  CAUSES,
  endTurn,
  newScratch,
  Rng,
  type Bought,
  type Customer,
  type DeathRecord,
  type FormulaComponent,
  type GameState,
} from '../src/index.ts';
import { bodyPhase } from '../src/systems/body.ts';
import { cureClaims, promiseShortfall, promisesCure } from '../src/systems/formula.ts';
import { advertisingPhase, salesPhase } from '../src/systems/market.ts';
import { evaluateRequirement } from '../src/systems/requirements.ts';
import { recordDeath, resolveInquest } from '../src/systems/scrutiny.ts';
import { deathWeight } from '../src/systems/ward.ts';
import { act, content, roomFor, start } from './helpers.ts';

/**
 * Round-10 balance: a label's promise, suspicion that always has a reason, inquests that move it by
 * what their letter prints, weaning off an old recipe, and the good side of being better known.
 */

const recipe = (alcohol: number, laudanum = 0): FormulaComponent[] => [
  { ingredientId: 'grain_alcohol', amount: alcohol },
  ...(laudanum > 0 ? [{ ingredientId: 'laudanum', amount: laudanum }] : []),
  { ingredientId: 'valerian', amount: 1 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 6 - alcohol - laudanum },
];
const MILD = recipe(2);
const HALF_OZ = recipe(2, 0.5);
const BOLD = ['purifies_the_blood', 'banishes_rheumatism', 'cures_consumption', 'soothes_nerves'];

/** A shop with one remedy of this recipe and label on the shelf. */
function shop(components: FormulaComponent[], claims: string[], seed: string): GameState {
  let s = start(seed);
  s.player.cashCents = 1_000_000;
  s.month.startCashCents = s.player.cashCents;
  s = act(s, {
    type: 'CreateFormula',
    name: 'T',
    components,
    processId: 'simple_mix',
    priceCents: 60,
    claims,
  });
  roomFor(s, 48);
  s.lab.stock.push({
    id: 'lot_t',
    formulaId: s.lab.formulas[0]!.id,
    components,
    processId: 'simple_mix',
    bottles: 200,
    potency: 1,
    badBatch: false,
  });
  return s;
}

const heatBy = (s: GameState, cause: string) =>
  s.month.deltas
    .filter((d) => d.metric === 'heat' && d.cause === cause)
    .reduce((a, d) => a + d.amount, 0);

describe("a label's promise (round-10 balance)", () => {
  const { promiseShare, cureShare, curePatience } = content.config.market;

  it('an ordinary claim the bottle makes good costs nothing; one it does not, at most its promise', () => {
    const promise = promiseShare * content.byId.claims.get('soothes_nerves')!.appeal;
    expect(promiseShortfall(['soothes_nerves'], 'nerves', promise + 1, 12, content)).toBe(0);
    expect(promiseShortfall(['soothes_nerves'], 'nerves', promise / 2, 12, content)).toBeCloseTo(
      promise / 2,
    );
    expect(promiseShortfall(['soothes_nerves'], 'nerves', -5, 12, content)).toBeCloseTo(promise);
    // a claim that does not name the complaint promises that buyer nothing
    expect(promiseShortfall(['soothes_nerves'], 'rheumatism', -5, 12, content)).toBe(0);
  });

  it('a claim to cure is never made good, and is given a few months', () => {
    const appeal = content.byId.claims.get('cures_consumption')!.appeal;
    const full = cureShare * appeal;
    expect(content.byId.claims.get('cures_consumption')!.cures).toBe(true);
    expect(content.byId.claims.get('soothes_nerves')!.cures).toBe(false);
    expect(promiseShortfall(['cures_consumption'], 'consumption', 99, 0, content)).toBe(0);
    expect(promiseShortfall(['cures_consumption'], 'consumption', 99, curePatience, content)).toBe(
      full,
    );
    expect(
      promiseShortfall(['cures_consumption'], 'consumption', 99, curePatience / 2, content),
    ).toBeCloseTo(full / 2);
    expect(promiseShortfall(['cures_consumption'], 'dyspepsia', 99, 24, content)).toBe(0);
  });

  /** One regular of this complaint takes two bottles a month of this label for `n` months. */
  const regularOn = (claims: string[], ailment: string, n: number, hooked = 0) => {
    const s = shop(MILD, claims, 'promise');
    const c = s.customers.find((x) => x.consumer === 'self')!;
    c.ailment = ailment;
    c.ailmentEndsTurn = null;
    s.customers = [c];
    const bought: Bought = {
      formulaId: s.lab.formulas[0]!.id,
      lotId: 'lot_t',
      components: MILD,
      processId: 'simple_mix',
      potency: 1,
      badBatch: false,
    };
    let disappointed = 0;
    for (let m = 0; m < n; m++) {
      s.meta.turn += 1;
      s.month.disappointed = 0;
      // a regular hooked on an old recipe (the dependency is set again each month)
      if (hooked > 0) c.body.dependency = hooked / (1 - content.config.body.weaningDecay);
      const scratch = newScratch();
      scratch.purchases.set(c.id, [bought, bought]);
      bodyPhase.run({ state: s, content, rng: Rng.fromSeed(`p${m}`), scratch });
      c.body.health = 100;
      disappointed += s.month.disappointed;
      if (!s.customers.includes(c)) s.customers = [c];
    }
    return { c, disappointed };
  };

  it('a consumptive sold a cure is less pleased with the same bottle, and is counted disappointed', () => {
    const plain = regularOn(['soothes_nerves'], 'consumption', 8);
    const cure = regularOn(['cures_consumption'], 'consumption', 8);
    expect(cure.c.satisfaction).toBeLessThan(plain.c.satisfaction - 0.1);
    expect(plain.disappointed).toBe(0);
    expect(cure.disappointed).toBeGreaterThan(5);
  });

  it('a regular who craves the bottle reads the label less', () => {
    const gap = (hooked: number) =>
      regularOn(['soothes_nerves'], 'consumption', 8, hooked).c.satisfaction -
      regularOn(['cures_consumption'], 'consumption', 8, hooked).c.satisfaction;
    expect(gap(0.6)).toBeGreaterThan(0);
    expect(gap(0.6)).toBeLessThan(gap(0) * 0.7);
  });

  it('claims to cure a deadly disease draw heat of their own; a cure for rheumatism does not', () => {
    const { curesHeat } = content.config.advertising;
    expect(cureClaims(BOLD, content)).toBe(1);
    expect(cureClaims(['cures_everything', 'cures_consumption'], content)).toBe(2);
    expect(cureClaims(['banishes_rheumatism'], content)).toBe(0);
    const s = shop(MILD, BOLD, 'cures');
    advertisingPhase.run({ state: s, content, rng: Rng.fromSeed('c'), scratch: newScratch() });
    expect(heatBy(s, CAUSES.cureClaims)).toBeCloseTo(curesHeat);
    const r = shop(MILD, ['banishes_rheumatism'], 'rheum');
    advertisingPhase.run({ state: r, content, rng: Rng.fromSeed('c'), scratch: newScratch() });
    expect(heatBy(r, CAUSES.cureClaims)).toBe(0);
  });
});

describe('suspicion always has a reason (round-10 balance)', () => {
  const death = (s: GameState, ailment: string, productImplicated: boolean): DeathRecord =>
    recordDeath(s, content, Rng.fromSeed('d'), {
      customerId: 'cust_x',
      name: 'Mr. Lefferts',
      segment: 'clerk',
      consumer: 'self',
      anonymous: false,
      ailment,
      cause: 'natural',
      productImplicated,
      formulaId: s.lab.formulas[0]!.id,
      badBatch: false,
    });

  it('a death the bottle had no part in does not move the suspicion', () => {
    const s = shop(MILD, ['soothes_nerves'], 'fever');
    const d = death(s, 'nerves', false);
    expect(s.scrutiny.heat).toBe(0);
    expect(d.promisedCure).toBe(false);
    expect(deathWeight(d, content)).toBe(0);
  });

  it('a consumptive who dies under a label that cured consumption is a broken promise', () => {
    const s = shop(MILD, BOLD, 'promise');
    const d = death(s, 'consumption', false);
    expect(d.promisedCure).toBe(true);
    expect(heatBy(s, CAUSES.brokenPromise)).toBe(content.config.scrutiny.heatPerBrokenPromise);
    expect(heatBy(s, CAUSES.death)).toBe(0);
    expect(deathWeight(d, content)).toBe(content.config.ward.perBrokenPromise);
    // rheumatism does not kill: "banishes rheumatism" is not a broken promise at a funeral
    expect(promisesCure(BOLD, 'rheumatism', content)).toBe(false);
    expect(promisesCure(['cures_everything'], 'teething_infant', content)).toBe(true);
  });

  it('every change to the suspicion over a year of bold claims and laudanum has a plain cause', () => {
    let s = shop(HALF_OZ, BOLD, 'causes');
    s.unlocks.unlocked['newspaper_ads'] = 0;
    s = act(s, { type: 'SetAdSpend', cents: 600 });
    for (let m = 0; m < 12 && s.meta.status === 'running'; m++) {
      for (const p of [...s.events.pending]) {
        const choice = p.eventId === 'inquest' ? 'stay_silent' : undefined;
        if (choice)
          s = act(s, { type: 'ResolveEvent', instanceId: p.instanceId, choiceId: choice });
      }
      s = endTurn(s, content).state;
    }
    const causes = new Set<string>(Object.values(CAUSES));
    const heat = s.ledger.flatMap((e) => e.deltas).filter((d) => d.metric === 'heat');
    expect(heat.length).toBeGreaterThan(0);
    for (const d of heat) {
      expect(causes.has(d.cause), d.cause).toBe(true);
      expect(content.strings[`cause.${d.cause}`], d.cause).toBeTruthy();
    }
  });

  it("an inquest's verdict moves the suspicion by exactly what the coroner's letter prints", () => {
    const cfg = content.config.scrutiny;
    // the numbers LettersDesk prints under the replies
    expect([cfg.namedHeat, cfg.namedReputation, cfg.namedNotoriety]).toEqual([24, 12, 8]);
    expect([cfg.clearedHeat, cfg.testifiedReputation]).toEqual([3, 1]);
    expect([cfg.exposedHeat, cfg.exposedReputation, cfg.exposedNotoriety]).toEqual([30, 15, 10]);
    expect(cfg.bribedNotoriety).toBe(2);
    let named = 0;
    let cleared = 0;
    for (let i = 0; i < 40; i++) {
      const s = shop(HALF_OZ, [], `inq${i}`);
      s.scrutiny.heat = 40;
      s.month.startHeat = 40;
      s.player.reputation = 30;
      s.month.startReputation = 30;
      const d = death(s, 'nerves', true);
      d.inquest = 'pending';
      const heat = s.scrutiny.heat;
      const rep = s.player.reputation;
      expect(resolveInquest(s, content, Rng.fromSeed(`i${i}`), d.id, 'testify')).toBeNull();
      if ((d.inquest as DeathRecord['inquest']) === 'implicated') {
        named += 1;
        expect(s.scrutiny.heat - heat).toBeCloseTo(cfg.namedHeat);
        expect(rep - s.player.reputation).toBeCloseTo(cfg.namedReputation);
      } else {
        cleared += 1;
        expect(heat - s.scrutiny.heat).toBeCloseTo(cfg.clearedHeat);
        expect(s.player.reputation - rep).toBeCloseTo(cfg.testifiedReputation);
      }
    }
    expect(named).toBeGreaterThan(0);
    expect(cleared).toBeGreaterThan(0);
  });
});

describe('weaning off an old recipe (round-10 balance)', () => {
  const { weaningDecay, withdrawalThreshold, mildDependencyCap } = content.config.body;

  /** A craving regular takes three bottles a month of this recipe; dependency after each month. */
  const months = (components: FormulaComponent[], n: number): [number[], Customer] => {
    const s = start('wean');
    const c = s.customers.find((x) => x.consumer === 'self')!;
    s.customers = [c];
    c.body.dependency = 0.8;
    const bought: Bought = {
      formulaId: 'f',
      lotId: 'lot',
      components,
      processId: 'simple_mix',
      potency: 1,
      badBatch: false,
    };
    const deps: number[] = [];
    for (let m = 0; m < n; m++) {
      const scratch = newScratch();
      scratch.purchases.set(c.id, [bought, bought, bought]);
      bodyPhase.run({ state: s, content, rng: Rng.fromSeed(`w${m}`), scratch });
      c.body.health = 100;
      if (!s.customers.includes(c)) s.customers = [c];
      deps.push(c.body.dependency);
    }
    return [deps, c];
  };

  it('once the narcotic is out of the bottle the craving fades within a season', () => {
    const [deps] = months(MILD, 4);
    expect(deps[0]!).toBeGreaterThan(0.8 * (1 - weaningDecay));
    expect(deps[0]!).toBeLessThan(0.8 * (1 - weaningDecay) + 0.05);
    expect(deps[2]!).toBeLessThan(withdrawalThreshold);
    expect(deps[3]!).toBeLessThanOrEqual(mildDependencyCap);
  });

  it('a weaning regular is marked so; one still on laudanum is not, and still craves', () => {
    const [, weaned] = months(MILD, 1);
    expect(weaned.weaning).toBe(true);
    const [deps, fed] = months(HALF_OZ, 4);
    expect(fed.weaning).toBe(false);
    expect(deps[3]!).toBeGreaterThan(withdrawalThreshold);
  });
});

describe('being better known (round-10 balance)', () => {
  it('brings more strangers in off the street', () => {
    const { base, perNotoriety, notorietyCap } = content.config.walkIns;
    const callers = (notoriety: number) => {
      let most = 0;
      for (let i = 0; i < 40; i++) {
        const s = shop(MILD, [], `known${i}`);
        s.player.reputation = 0;
        s.player.notoriety = notoriety;
        salesPhase.run({ state: s, content, rng: Rng.fromSeed(`k${i}`), scratch: newScratch() });
        most = Math.max(most, s.month.callers['ward'] ?? 0);
      }
      return most;
    };
    expect(perNotoriety).toBeGreaterThan(0);
    expect(callers(0)).toBeLessThanOrEqual(base);
    expect(callers(notorietyCap)).toBeGreaterThan(base);
    // a name the papers print for poisoning brings no more of the curious than a name well known
    expect(callers(10 * notorietyCap)).toBe(callers(notorietyCap));
  });
});

describe('letters grounded in what happened (round 10)', () => {
  const died = (s: GameState, monthsAgo: number, owedCents: number, anonymous = false) =>
    s.deaths.push({
      id: `death_${s.deaths.length}`,
      turn: s.meta.turn - monthsAgo,
      date: { ...s.meta.date },
      customerId: anonymous ? null : 'cust_x',
      name: anonymous ? '' : 'Mr. Dunne',
      segment: 'clerk',
      consumer: 'self',
      anonymous,
      ailment: 'nerves',
      cause: 'natural',
      reportedCause: 'natural',
      productImplicated: false,
      formulaId: null,
      badBatch: false,
      detectability: 0,
      boldness: 0,
      promisedCure: false,
      owedCents,
      inquest: 'none',
      verdict: null,
    });
  const req = { type: 'regularDied', withinMonths: 6, owing: true } as const;

  it('the widow writes only after a regular died owing on the bottle, this half-year', () => {
    const widow = content.byId.events.get('widow_account')!;
    expect(JSON.stringify(widow.condition)).toContain('"regularDied"');
    const s = start('widow');
    s.meta.turn = 10;
    expect(evaluateRequirement(req, s, content).met).toBe(false);
    died(s, 2, 0);
    expect(evaluateRequirement(req, s, content).met).toBe(false);
    expect(evaluateRequirement({ ...req, owing: false }, s, content).met).toBe(true);
    died(s, 2, 500, true);
    expect(evaluateRequirement(req, s, content).met).toBe(false);
    died(s, 8, 500);
    expect(evaluateRequirement(req, s, content).met).toBe(false);
    died(s, 3, 460);
    expect(evaluateRequirement(req, s, content).met).toBe(true);
  });

  it('every venture says the monthly sums its terms charge', () => {
    const words: Record<number, string> = {
      100: 'a dollar',
      200: 'two dollars',
      300: 'three dollars',
      400: 'four dollars',
      500: 'five dollars',
    };
    for (const inv of content.investments) {
      const text = content.strings[`investment.${inv.id}.description`] ?? '';
      for (const [cents, w] of Object.entries(words)) {
        if (text.includes(`${w} more a month`) || text.includes(`${w} a month`)) {
          expect(inv.upkeepCents, inv.id).toBe(Number(cents));
        }
      }
    }
    expect(content.strings['investment.shop_next_door.description']).toContain('three dollars');
    expect(content.strings['investment.fourth_still.description']).toContain('three dollars');
  });

  it('only the pump water is free', () => {
    for (const ing of content.ingredients) {
      if (ing.id !== 'water') expect(Math.round(ing.costPerUnitCents), ing.id).toBeGreaterThan(0);
    }
  });
});
