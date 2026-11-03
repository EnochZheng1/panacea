import type { ContentPack, Regular, Segment } from '@panacea/content';
import type { Rng } from '../core/rng.ts';
import type { Customer, GameState } from '../core/state.ts';
import { newBody } from './pharmacology.ts';

/** Builds the shop's starting regulars from content. */
export function initialCustomers(content: ContentPack): Customer[] {
  return content.regulars.map((r: Regular) => {
    const seg = content.byId.segments.get(r.segment);
    const ailment = content.byId.ailments.get(r.ailment);
    return {
      id: r.id,
      name: r.displayName,
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
): Customer {
  const female = segment.gender === 'female' || (segment.gender === 'any' && rng.chance(0.5));
  const given = rng.pick(female ? content.names.female : content.names.male);
  let titles = segment.titles;
  if (segment.gender === 'any') {
    const gendered = titles.filter((t) => (female ? t !== 'Mr.' : t === 'Mr.'));
    if (gendered.length > 0) titles = gendered;
  }
  const name = `${rng.pick(titles)} ${given} ${rng.pick(content.names.surnames)}`;
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
  };
}
