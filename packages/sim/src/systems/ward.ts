import type { ContentPack } from '@panacea/content';
import type {
  Customer,
  DeathRecord,
  GameState,
  NewsItem,
  Phase,
  ReportedCause,
} from '../core/index.ts';

/**
 * The ward's memory of harm. The heat is the authorities' suspicion: it flares with every death
 * and burns itself out within a season. The memory is the neighbours': the widow who saw what
 * the bottle did, the street that heard the jury name it. It rises with the harm the remedy did
 * and was seen to do, fades by a share each month (a bad month weighs for most of a year), and
 * costs the shop at the counter: strangers who have heard the talk buy less often and seldom come
 * back as regulars, and regulars who are not hooked go elsewhere.
 */

/** The cause of a death as the ward gives it (see `ReportedCause`). */
export function reportedCauseOf(
  cause: DeathRecord['cause'],
  segment: string,
  ailment: string | null,
  content: ContentPack,
): ReportedCause {
  if (cause === 'overdose') return 'overdose';
  if (cause === 'poisoning') return 'wasting';
  const a = ailment ? content.byId.ailments.get(ailment) : undefined;
  // a complaint that kills (the consumption, a cough gone to the chest) is what the ward names
  if (cause === 'disease' && a) return 'ailment';
  if (a && a.mortalityMultiplier > 1) return 'ailment';
  if (segment === 'elderly') return 'old_age';
  return 'natural';
}

/** Whether the ward lays a death at the bottle's door: it was reported as an overdose or a wasting. */
export const blamedOnTheBottle = (d: Pick<DeathRecord, 'reportedCause'>): boolean =>
  d.reportedCause === 'overdose' || d.reportedCause === 'wasting';

/** How much one death adds to the ward's memory. */
export function deathWeight(d: DeathRecord, content: ContentPack): number {
  const cfg = content.config.ward;
  // the family keeps the label that said it cured what killed them
  const promise = d.promisedCure ? cfg.perBrokenPromise : 0;
  if (!d.productImplicated) return promise;
  let w = promise;
  if (!blamedOnTheBottle(d)) w += cfg.perHastenedDeath;
  else if (d.anonymous) w += cfg.perStrangerDeath;
  else if (d.consumer === 'infant') w += cfg.perInfantDeath;
  else w += cfg.perDeath;
  if (d.badBatch) w += cfg.perBadBatchDeath;
  return w;
}

/** How much one item of the month's news adds to the ward's memory. */
function newsWeight(state: GameState, n: NewsItem, content: ContentPack): number {
  const cfg = content.config.ward;
  switch (n.kind) {
    case 'death': {
      const d = state.deaths.find((x) => x.id === n.deathId);
      return d ? deathWeight(d, content) : 0;
    }
    case 'inquest':
      return n.outcome === 'implicated' ? cfg.perVerdict : 0;
    case 'bribe_exposed':
      return cfg.perExposedBribe;
    case 'stock_seized':
      return n.bottles > 0 ? cfg.perSeizure : 0;
    default:
      return 0;
  }
}

/** The share (0..1) by which the memory puts something off: `loss` at a memory of 100. */
export const memoryLoss = (state: GameState, loss: number): number =>
  1 - loss * Math.min(1, Math.max(0, state.ward.memory / 100));

/** Whether a regular who is not hooked hears the talk and goes elsewhere this month. */
export function frightenedAway(
  state: GameState,
  c: Customer,
  content: ContentPack,
  chance: (p: number) => boolean,
): boolean {
  const p = content.config.ward.leaveChance * (state.ward.memory / 100);
  return p > 0 && c.body.dependency < content.config.body.withdrawalThreshold && chance(p);
}

/**
 * Turn phase, after the body: last month's memory fades, and this month's harms are added to it
 * (the deaths, the verdicts the coroner's juries returned, a bribe that came out, a seizure).
 */
export const wardPhase: Phase = {
  id: 'ward',
  run({ state, content }) {
    const cfg = content.config.ward;
    let m = Math.max(0, state.ward.memory * (1 - cfg.memoryDecay) - cfg.memoryDecayFlat);
    for (const n of state.month.news) m += newsWeight(state, n, content);
    state.ward.memory = Math.min(100, m);
    state.ward.peakMemory = Math.max(state.ward.peakMemory, state.ward.memory);
  },
};

/** What the ward remembers, as the paper could print it (see `Observation.wardRemembers`). */
export interface WardRemembers {
  /** The months counted: this one and the ones before it, `rememberMonths` in all. */
  months: number;
  /** Deaths in those months the ward lays at the bottle's door (an overdose or a wasting). */
  deaths: number;
  /** Of those, infants. */
  infantDeaths: number;
  /** Of those, strangers found with the bottle. */
  strangerDeaths: number;
  /** Inquests in those months whose jury named the remedy (a bribe found out included). */
  verdicts: number;
  /** Times the Board of Health carried the bottles away. */
  seizures: number;
}

export function wardRemembers(state: GameState, content: ContentPack): WardRemembers {
  const months = content.config.ward.rememberMonths;
  const out: WardRemembers = {
    months,
    deaths: 0,
    infantDeaths: 0,
    strangerDeaths: 0,
    verdicts: 0,
    seizures: 0,
  };
  const since = state.meta.turn - months + 1;
  const news = [
    ...state.ledger.filter((e) => e.turn >= since).flatMap((e) => e.news),
    ...(state.meta.status === 'running' ? state.month.news : []),
  ];
  for (const n of news) {
    if (n.kind === 'death') {
      const d = state.deaths.find((x) => x.id === n.deathId);
      if (!d || !blamedOnTheBottle(d)) continue;
      out.deaths += 1;
      if (d.anonymous) out.strangerDeaths += 1;
      else if (d.consumer === 'infant') out.infantDeaths += 1;
    } else if (n.kind === 'inquest' && n.outcome === 'implicated') out.verdicts += 1;
    else if (n.kind === 'stock_seized' && n.bottles > 0) out.seizures += 1;
  }
  return out;
}
