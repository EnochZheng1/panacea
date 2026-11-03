/**
 * Parity fixtures for the C# port of the simulation (sim-cs/).
 *
 *   pnpm exec tsx packages/playtest/src/cli/parity-fixtures.ts          # writes sim-cs/fixtures/
 *   pnpm exec tsx packages/playtest/src/cli/parity-fixtures.ts --out dir --max-turns 120
 *
 * Plays several bots on several seeds and records, for every run: the new-game options, the
 * replay log, and the state hash after `newGame` and after every `endTurn` (plus a hash of each
 * top-level state section, so a divergence can be localised to e.g. `customers`). Full
 * `stableStringify` snapshots are kept at a few checkpoints. The C# tests replay each log and
 * compare. Also writes `vectors.json`: RNG sequences, dmath samples, and JSON formatting cases
 * (number-to-string and string escaping) that the C# serialiser must reproduce exactly.
 *
 * Fixture files are gzipped JSON to keep the repository small.
 */
import { mkdirSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { parseArgs } from 'node:util';
import { gzipSync } from 'node:zlib';
import { loadContent } from '@panacea/content';
import {
  applyActionInPlace,
  checkInvariants,
  dmath,
  endTurnInPlace,
  gaussian,
  hashValue,
  legalActions,
  newGame,
  observe,
  Rng,
  seedRng,
  stableStringify,
  type GameState,
  type NewGameOptions,
} from '@panacea/sim';
import { BOTS } from '../bots/index.ts';
import type { BotTurn } from '../bots/types.ts';

const { values } = parseArgs({
  options: {
    out: { type: 'string', default: resolve(import.meta.dirname, '../../../../sim-cs/fixtures') },
    bots: { type: 'string', default: 'greedy,briber,adaptive,mercurial,shopkeeper,random' },
    seeds: { type: 'string', default: 'parity-1,parity-2,parity-3' },
    'max-turns': { type: 'string', default: '10000' },
  },
});

const content = loadContent();
const outDir = values.out;
const maxTurns = Number.parseInt(values['max-turns'], 10);
const botIds = values.bots.split(',').filter(Boolean);
const seeds = values.seeds.split(',').filter(Boolean);
for (const id of botIds) {
  if (!(id in BOTS)) throw new Error(`unknown bot "${id}"`);
}

/** Top-level sections hashed separately every turn (the log and ledger only grow). */
const SECTIONS = [
  'meta',
  'player',
  'lab',
  'marketing',
  'customers',
  'deaths',
  'scrutiny',
  'stats',
  'unlocks',
  'events',
  'timeline',
  'month',
] as const;

const sectionHashes = (state: GameState): Record<string, string> => {
  const out: Record<string, string> = {};
  for (const k of SECTIONS) out[k] = hashValue(state[k]);
  out['ledgerLast'] = hashValue(state.ledger.at(-1) ?? null);
  out['ledgerLength'] = String(state.ledger.length);
  return out;
};

const write = (name: string, value: unknown) => {
  const path = join(outDir, name);
  writeFileSync(path, gzipSync(Buffer.from(JSON.stringify(value)), { level: 9 }));
  return path;
};

mkdirSync(join(outDir, 'runs'), { recursive: true });
for (const f of readdirSync(join(outDir, 'runs'))) rmSync(join(outDir, 'runs', f));

const manifest: {
  contentVersion: string;
  runs: {
    file: string;
    bot: string;
    options: NewGameOptions;
    turns: number;
    endReason: string | null;
    finalHash: string;
  }[];
} = { contentVersion: content.config.contentVersion, runs: [] };

seeds.forEach((seed, seedIndex) => {
  for (const botId of botIds) {
    const bot = (BOTS[botId] as () => ReturnType<(typeof BOTS)[string]>)();
    // Every third seed plays the other background, so both starts are covered.
    const options: NewGameOptions =
      seedIndex % 3 === 2 ? { seed, backgroundId: 'showman' } : { seed };
    const state = newGame(content, options);
    const botRng = Rng.fromSeed(`${seed}:bot:${bot.id}`);
    const hashes = [hashValue(state)];
    const sections = [sectionHashes(state)];
    const snapshots: Record<string, string> = { '0': stableStringify(state) };
    const verify = (where: string) => {
      const v = checkInvariants(state, content);
      if (v.length > 0) throw new Error(`${botId}/${seed} ${where}: ${v.join('; ')}`);
    };
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
        act: (action) => applyActionInPlace(state, action, content),
      };
      bot.playTurn(turn);
      endTurnInPlace(state, content);
      turns++;
      verify(`after endTurn ${turns}`);
      hashes.push(hashValue(state));
      sections.push(sectionHashes(state));
      if (turns === 1 || turns === 12) snapshots[String(turns)] = stableStringify(state);
    }
    snapshots[String(turns)] = stableStringify(state);

    const file = `runs/${botId}-${seed}.json.gz`;
    write(file, {
      bot: botId,
      options,
      contentVersion: content.config.contentVersion,
      endReason: state.meta.endReason,
      turns,
      log: state.log,
      hashes,
      sections,
      checkpoints: snapshots,
    });
    manifest.runs.push({
      file,
      bot: botId,
      options,
      turns,
      endReason: state.meta.endReason,
      finalHash: hashes.at(-1) as string,
    });
    console.log(
      `${botId.padEnd(11)} ${seed} ${(options.backgroundId ?? 'druggist').padEnd(8)} ` +
        `${String(turns).padStart(3)} turns  ${String(state.meta.endReason).padEnd(12)} ` +
        `deaths ${state.deaths.length}, inquests ${state.scrutiny.inquestsHeld}, ` +
        `bribes ${state.scrutiny.bribesPaid}, actions ${state.log.length - turns}`,
    );
  }
});

// ---------------------------------------------------------------------------
// Vectors for the C# unit tests
// ---------------------------------------------------------------------------

const bitsOf = (x: number) => {
  const view = new DataView(new ArrayBuffer(8));
  view.setFloat64(0, x);
  return view.getBigUint64(0).toString(16).padStart(16, '0');
};

const rngVectors = ['panacea', '', 'a', 'b', '7', 'parity-1', 'Ünïcödé ✓ 🎲', 'x'.repeat(100)].map(
  (seed) => {
    const r = Rng.fromSeed(seed);
    const u32 = Array.from({ length: 16 }, () => r.nextU32());
    const floats = Array.from({ length: 8 }, () => bitsOf(r.float()));
    const ints = Array.from({ length: 8 }, (_, i) => r.int(-i * 7, i * 13));
    const gaussians = Array.from({ length: 32 }, () => bitsOf(gaussian(r)));
    return { seed, state: seedRng(seed), u32, floats, ints, gaussians, end: r.state() };
  },
);

const sampler = Rng.fromSeed('vectors');
const dmathVectors: { fn: string; x: string; y: string }[] = [];
const sample = (fn: 'log' | 'cos' | 'tanh' | 'exp', x: number) =>
  dmathVectors.push({ fn, x: bitsOf(x), y: bitsOf(dmath[fn](x)) });
for (let i = 0; i < 500; i++) {
  sample('log', Math.max(sampler.float(), 1e-12));
  sample('cos', 2 * Math.PI * sampler.float());
  sample('tanh', (sampler.float() - 0.5) * 12);
  sample('tanh', (sampler.float() - 0.5) * 1e-3);
  sample('exp', (sampler.float() - 0.5) * 100);
}
for (const x of [1e-12, 5e-324, 1e-300, 0.5, 1, 2, 10, 1e300]) sample('log', x);
for (const x of [0, 1, -1, Math.PI, Math.PI / 2, 6.283185307179586, 100, -1e5]) sample('cos', x);
for (const x of [0, 1e-300, 0.17, 0.18, -3, 21.9, 22.5, -100]) sample('tanh', x);

// Number formatting: special and random doubles, with JavaScript's String(x).
const numbers: number[] = [
  0,
  -0,
  1,
  -1,
  0.1,
  0.2,
  0.30000000000000004,
  1 / 3,
  2 / 3,
  100,
  1e20,
  1e21,
  1.5e21,
  123456789012345680000,
  1e-6,
  1e-7,
  1.5e-7,
  0.000001234,
  5e-324,
  1.7976931348623157e308,
  2 ** 53,
  2 ** 53 + 2,
  4294967295,
  4294967296,
  0.5,
  12.375,
  1e100,
  1.2345678901234567e-100,
  -1e-7,
  -123.456,
];
for (let i = 0; i < 2000; i++) {
  const scale = 10 ** sampler.int(-30, 30);
  numbers.push((sampler.float() - 0.5) * scale);
  numbers.push(sampler.int(-1_000_000, 1_000_000) / 100);
}
const view = new DataView(new ArrayBuffer(8));
for (let i = 0; i < 2000; i++) {
  view.setUint32(0, sampler.nextU32());
  view.setUint32(4, sampler.nextU32());
  const x = view.getFloat64(0);
  if (Number.isFinite(x)) numbers.push(x);
}
const numberVectors = numbers.map((x) => ({ bits: bitsOf(x), js: JSON.stringify(x) }));

const strings = [
  '',
  'plain',
  'quote " backslash \\ slash /',
  'controls \u0000\u0001\u0007\b\t\n\u000b\f\r\u001f\u007f',
  'Mrs. Bridget Halloran’s “Soothing” Syrup — 1885 ✓',
  'emoji 🎲 and astral 𝔄',
  'lone \ud800 high, lone \udc00 low, pair 🎲',
  '   line separators',
  '<script>&amp;</script>',
];
const stringVectors = strings.map((s) => ({
  units: Array.from({ length: s.length }, (_, i) => s.charCodeAt(i)),
  json: JSON.stringify(s),
  hash: hashValue(s),
}));

write('vectors.json.gz', {
  rng: rngVectors,
  dmath: dmathVectors,
  numbers: numberVectors,
  strings: stringVectors,
  stableStringify: [
    { value: { b: 1, a: [true, null, 'x'], c: { z: -0, y: 1e21 } }, json: '' },
    { value: [{}, [], 0.1, 'é'], json: '' },
  ].map((v) => ({ ...v, json: stableStringify(v.value), hash: hashValue(v.value) })),
});
writeFileSync(join(outDir, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
console.log(`\nWrote ${manifest.runs.length} runs to ${outDir}`);
