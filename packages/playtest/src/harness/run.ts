import type { ContentPack } from '@panacea/content';
import {
  applyAction,
  checkInvariants,
  endTurn,
  hashValue,
  legalActions,
  newGame,
  observe,
  Rng,
  type EndReason,
  type GameState,
  type NewGameOptions,
} from '@panacea/sim';
import type { Bot, BotTurn } from '../bots/types.ts';

export interface RunOptions {
  content: ContentPack;
  bot: Bot;
  seed: string;
  backgroundId?: string;
  /** Check invariants after every action and month (slower; on by default). */
  checkEveryStep?: boolean;
  /** Safety cap on months simulated. */
  maxTurns?: number;
}

export interface RunResult {
  seed: string;
  botId: string;
  backgroundId: string;
  endReason: EndReason | 'turn_cap';
  months: number;
  finalCashCents: number;
  bottlesSold: number;
  revenueCents: number;
  unlocks: Record<string, number>;
  actionsTaken: number;
  actionsRejected: number;
  eventsDrawn: number;
  violations: string[];
  stateHash: string;
  /** Wall-clock run time (harness only; never read by the sim). */
  ms: number;
  finalState: GameState;
}

/** Plays one full game with a bot and summarises it. */
export function runGame(options: RunOptions): RunResult {
  const { content, bot, seed } = options;
  const check = options.checkEveryStep ?? true;
  const maxTurns = options.maxTurns ?? 10_000;
  const started = performance.now();

  const gameOptions: NewGameOptions = { seed };
  if (options.backgroundId) gameOptions.backgroundId = options.backgroundId;
  let state = newGame(content, gameOptions);
  const botRng = Rng.fromSeed(`${seed}:bot:${bot.id}`);
  const violations = new Set<string>();
  let actionsTaken = 0;
  let actionsRejected = 0;
  const verify = (where: string) => {
    if (!check) return;
    for (const v of checkInvariants(state, content)) violations.add(`${where}: ${v}`);
  };

  verify('start');
  let turns = 0;
  while (state.meta.status === 'running' && turns < maxTurns) {
    const turn: BotTurn = {
      content,
      rng: botRng,
      get state() {
        return state;
      },
      observe: () => observe(state, content),
      legal: () => legalActions(state, content),
      act: (action) => {
        const out = applyAction(state, action, content);
        state = out.state;
        if (out.result.ok) actionsTaken++;
        else actionsRejected++;
        verify(`turn ${state.meta.turn} after ${action.type}`);
        return out.result;
      },
    };
    bot.playTurn(turn);
    state = endTurn(state, content).state;
    verify(`turn ${state.meta.turn} after endTurn`);
    turns++;
  }
  if (!check) for (const v of checkInvariants(state, content)) violations.add(`final: ${v}`);

  return {
    seed,
    botId: bot.id,
    backgroundId: state.meta.backgroundId,
    endReason: state.meta.endReason ?? 'turn_cap',
    months: state.ledger.length,
    finalCashCents: state.player.cashCents,
    bottlesSold: state.stats.bottlesSold,
    revenueCents: state.stats.revenueCents,
    unlocks: { ...state.unlocks.unlocked },
    actionsTaken,
    actionsRejected,
    eventsDrawn: Object.values(state.events.timesDrawn).reduce((a, b) => a + b, 0),
    violations: [...violations],
    stateHash: hashValue(state),
    ms: performance.now() - started,
    finalState: state,
  };
}
