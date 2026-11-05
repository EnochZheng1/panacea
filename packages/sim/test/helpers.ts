import { loadContent } from '@panacea/content';
import { expect } from 'vitest';
import {
  applyAction,
  checkInvariants,
  endTurn,
  newGame,
  type Action,
  type FormulaComponent,
  type GameState,
} from '../src/index.ts';

export const content = loadContent();

export const TONIC: FormulaComponent[] = [
  { ingredientId: 'grain_alcohol', amount: 2 },
  { ingredientId: 'laudanum', amount: 0.25 },
  { ingredientId: 'sugar_syrup', amount: 1 },
  { ingredientId: 'water', amount: 4.75 },
];

export function start(seed = 'test', backgroundId?: string): GameState {
  return newGame(content, backgroundId ? { seed, backgroundId } : { seed });
}

/** Applies actions, asserting each succeeds and that invariants hold afterwards. */
export function act(state: GameState, ...actions: Action[]): GameState {
  for (const action of actions) {
    const { state: next, result } = applyAction(state, action, content);
    expect(result, `${action.type} should succeed`).toMatchObject({ ok: true });
    state = next;
    expect(checkInvariants(state, content)).toEqual([]);
  }
  return state;
}

export function months(state: GameState, n: number): GameState {
  for (let i = 0; i < n && state.meta.status === 'running'; i++) {
    state = endTurn(state, content).state;
    expect(checkInvariants(state, content)).toEqual([]);
  }
  return state;
}

/** Puts a copper big enough for a batch of `bottles` on every still (a test's shortcut). */
export function roomFor(state: GameState, bottles: number): void {
  const extra = bottles - content.config.stillBottles;
  if (extra > (state.player.capabilities['still_bottles'] ?? 0)) {
    state.player.capabilities['still_bottles'] = extra;
  }
}

/** Creates the test tonic and buys ingredients for `bottles` bottles. Returns the formula id. */
export function setUpTonic(
  state: GameState,
  bottles: number,
): { state: GameState; formulaId: string } {
  const created = applyAction(
    state,
    {
      type: 'CreateFormula',
      name: 'Test Tonic',
      components: TONIC,
      processId: 'simple_mix',
      priceCents: 50,
    },
    content,
  );
  expect(created.result.ok).toBe(true);
  const formulaId = (created.result as { createdId: string }).createdId;
  let s = created.state;
  roomFor(s, bottles);
  for (const c of TONIC) {
    s = act(s, {
      type: 'BuyIngredient',
      ingredientId: c.ingredientId,
      units: Math.ceil(c.amount * bottles),
    });
  }
  return { state: s, formulaId };
}

/** Deep-freezes a value so any mutation of it throws (in strict mode). */
export function deepFreeze<T>(value: T): T {
  if (value && typeof value === 'object' && !Object.isFrozen(value)) {
    Object.freeze(value);
    for (const v of Object.values(value)) deepFreeze(v);
  }
  return value;
}
