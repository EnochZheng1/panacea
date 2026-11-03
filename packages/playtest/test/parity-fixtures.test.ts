import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { gunzipSync } from 'node:zlib';
import { loadContent } from '@panacea/content';
import {
  applyActionInPlace,
  hashValue,
  legalActions,
  newGame,
  replay,
  type LogEntry,
  type NewGameOptions,
} from '@panacea/sim';
import { describe, expect, it } from 'vitest';

const content = loadContent();
const fixtures = join(import.meta.dirname, '..', '..', '..', 'sim-cs', 'fixtures');

interface Manifest {
  runs: { file: string; options: NewGameOptions; finalHash: string; turns: number }[];
}

describe('C# parity fixtures (sim-cs/fixtures)', () => {
  it('still replay to the recorded hashes in the TypeScript sim', () => {
    // If this fails, the sim or its content changed: regenerate the fixtures with
    //   pnpm exec tsx packages/playtest/src/cli/parity-fixtures.ts
    // and port the change to sim-cs/ so the C# parity tests pass again.
    const manifest = JSON.parse(readFileSync(join(fixtures, 'manifest.json'), 'utf8')) as Manifest;
    expect(manifest.runs.length).toBeGreaterThanOrEqual(12);
    for (const run of manifest.runs) {
      const fixture = JSON.parse(gunzipSync(readFileSync(join(fixtures, run.file))).toString()) as {
        log: LogEntry[];
        hashes: string[];
      };
      const state = replay(content, run.options, fixture.log);
      expect(hashValue(state), run.file).toBe(run.finalHash);
      expect(fixture.hashes).toHaveLength(run.turns + 1);
    }
  });

  it('the replay log records only the fields the rules read', () => {
    const state = newGame(content, { seed: 'canonical' });
    const spec = legalActions(state, content).find((s) => s.type === 'BuyIngredient');
    if (spec?.type !== 'BuyIngredient') throw new Error('no ingredient to buy');
    // Spreading a spec into an action carries its `maxUnits` along; the log must not.
    expect(applyActionInPlace(state, { ...spec, units: 1 }, content).ok).toBe(true);
    expect(state.log.at(-1)).toEqual({
      turn: 0,
      action: { type: 'BuyIngredient', ingredientId: spec.ingredientId, units: 1 },
    });
  });
});
