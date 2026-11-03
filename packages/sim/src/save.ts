import { STATE_VERSION, type GameState } from './core/state.ts';

/**
 * Save files: `{ saveVersion, state }` as JSON. Every change to the state shape bumps
 * STATE_VERSION and adds a migration from the previous version, so saves from any earlier
 * build keep loading. Tests round-trip a save fixture from every past version.
 *
 * Pre-release policy: until the first external build ships, the state shape may change freely
 * without a version bump (no saves exist in the wild). From the first external build on, every
 * change bumps STATE_VERSION and adds a migration plus a fixture.
 */

type Migration = (state: Record<string, unknown>) => Record<string, unknown>;

/** MIGRATIONS[n] upgrades a version-n state to version n+1. */
const MIGRATIONS: Record<number, Migration> = {};

export interface SaveFile {
  saveVersion: number;
  state: GameState;
}

export function serializeGame(state: GameState): string {
  const file: SaveFile = { saveVersion: STATE_VERSION, state };
  return JSON.stringify(file);
}

export function deserializeGame(text: string): GameState {
  const file = JSON.parse(text) as { saveVersion?: unknown; state?: unknown };
  if (
    typeof file.saveVersion !== 'number' ||
    typeof file.state !== 'object' ||
    file.state === null
  ) {
    throw new Error('not a Panacea save file');
  }
  if (file.saveVersion > STATE_VERSION) {
    throw new Error(`save version ${file.saveVersion} is newer than this build (${STATE_VERSION})`);
  }
  let state = file.state as Record<string, unknown>;
  for (let v = file.saveVersion; v < STATE_VERSION; v++) {
    const migrate = MIGRATIONS[v];
    if (!migrate) throw new Error(`no migration from save version ${v}`);
    state = migrate(state);
  }
  return state as unknown as GameState;
}
