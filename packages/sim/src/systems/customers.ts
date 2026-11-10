import type { ContentPack, Regular, Segment } from '@panacea/content';
import type { Rng } from '../core/rng.ts';
import type { Customer, GameState } from '../core/state.ts';
import { newBody } from './pharmacology.ts';

/** Tries before a name drawn for a new customer is allowed to repeat one (a pool all but used up). */
const NAME_TRIES = 40;

/**
 * Draws a stock of names, then a given name and a surname of that stock, that nobody in this game has had yet, living, dead or gone,
 * and records it as used. Returns "Given Surname".
 */
function drawName(state: GameState, content: ContentPack, rng: Rng, female: boolean): string {
  const used = state.meta.usedNames;
  let name = '';
  for (let i = 0; i < NAME_TRIES; i++) {
    const origin = rng.weighted(content.names.origins.map((o) => ({ item: o, weight: o.weight })));
    if (!origin) break;
    const given = rng.pick(female ? origin.female : origin.male);
    name = `${given} ${rng.pick(origin.surnames)}`;
    if (!used.includes(name)) break;
  }
  used.push(name);
  return name;
}

/**
 * Builds the shop's starting regulars from content. Each keeps the form of address the content
 * gives them ("Mrs.", "Widow"), but the name is drawn for this game, so every seed opens with a
 * different cast at the counter.
 */
export function initialCustomers(state: GameState, content: ContentPack, rng: Rng): Customer[] {
  return content.regulars.map((r: Regular) => {
    const seg = content.byId.segments.get(r.segment);
    const ailment = content.byId.ailments.get(r.ailment);
    const title = r.displayName.split(' ')[0] ?? '';
    const name = `${title} ${drawName(state, content, rng, title !== 'Mr.')}`;
    return {
      id: r.id,
      name,
      segment: r.segment,
      ailment: r.ailment,
      consumer: ailment?.consumer ?? 'self',
      profile: structuredClone(r.profile),
      joinedTurn: 0,
      // Starting regulars' self-limiting complaints clear at the late end of their range.
      ailmentEndsTurn: ailment?.resolvesAfterMonths ? ailment.resolvesAfterMonths[1] : null,
      body: newBody(seg?.baseHealth ?? 75),
      satisfaction: 0,
      purchases: 0,
      lastPurchaseTurn: null,
      lastFormulaId: null,
      bottlesLastMonth: 0,
      inWithdrawal: false,
      weaning: false,
      debtCents: 0,
      wary: r.wary,
    };
  });
}

export function pickSegment(content: ContentPack, rng: Rng): Segment {
  const seg = rng.weighted(content.segments.map((s) => ({ item: s, weight: s.weight })));
  if (!seg) throw new Error('no segments with positive weight');
  return seg;
}

export function pickAilment(segment: Segment, rng: Rng): string {
  return rng.weighted(segment.ailments.map((a) => ({ item: a.id, weight: a.weight }))) as string;
}

export function customerCapacity(state: GameState, content: ContentPack): number {
  return content.config.maxRegulars + (state.player.capabilities['shop_capacity'] ?? 0);
}

/** Creates a new regular from a walk-in of the given segment and ailment. */
export function generateCustomer(
  state: GameState,
  content: ContentPack,
  rng: Rng,
  segment: Segment,
  ailmentId: string,
  wary: boolean,
): Customer {
  const female = segment.gender === 'female' || (segment.gender === 'any' && rng.chance(0.5));
  let titles = segment.titles;
  if (segment.gender === 'any') {
    const gendered = titles.filter((t) => (female ? t !== 'Mr.' : t === 'Mr.'));
    if (gendered.length > 0) titles = gendered;
  }
  const title = rng.pick(titles);
  const name = `${title} ${drawName(state, content, rng, female)}`;
  const ailment = content.byId.ailments.get(ailmentId);
  const resolves = ailment?.resolvesAfterMonths;
  return {
    id: `cust_${state.meta.nextId++}`,
    name,
    segment: segment.id,
    ailment: ailmentId,
    consumer: ailment?.consumer ?? 'self',
    profile: structuredClone(segment.profile),
    joinedTurn: state.meta.turn,
    ailmentEndsTurn: resolves ? state.meta.turn + rng.int(resolves[0], resolves[1]) : null,
    body: newBody(segment.baseHealth),
    satisfaction: 0.2,
    purchases: 0,
    lastPurchaseTurn: null,
    lastFormulaId: null,
    bottlesLastMonth: 0,
    inWithdrawal: false,
    weaning: false,
    debtCents: 0,
    wary,
  };
}
