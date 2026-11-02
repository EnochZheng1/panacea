# PANACEA — Technical Plan

Companion to [GAME_DESIGN.md](GAME_DESIGN.md). The playtest and release process is in [PLAYTEST_PIPELINE.md](PLAYTEST_PIPELINE.md).

---

## 1. Principles

1. **The simulation is a pure library.** All game rules live in a headless, deterministic TypeScript package with no DOM, no rendering, no wall-clock time. The client only *displays* state and *submits* actions.
2. **Deterministic and replayable.** Seeded PRNG; a run is fully described by `(seed, background, difficulty, action log)`. Bug reports, balance tests and AI playtests all replay exactly.
3. **Data-driven content.** Ingredients, unlocks, events, regions, NPCs and endings are data validated against schemas, not code. Designers (and AI tools) can add content without touching systems.
4. **Machine-playable by design.** The sim exposes a structured observation/action API so scripted bots and AI playtesters can play the real game rules at high speed.
5. **Ship-quality from day one.** Saves are versioned with migrations, strings are externalised for localisation, CI runs on every push.

---

## 2. Stack

| Concern | Choice | Notes |
|---|---|---|
| Language | TypeScript (strict) | Shared across sim, client, tools |
| Package manager / monorepo | pnpm workspaces | |
| Build / dev server | Vite | Fast HMR for UI work |
| UI | React | Ledgers, cards, map overlays, label designer: HTML/CSS strengths |
| UI state | Zustand | Thin; subscribes to sim state snapshots |
| 2D rendering (lab, map effects) | PixiJS v8 | Liquids, gauges, steam, particles |
| Animation | Motion (Framer Motion) for UI; GSAP or Pixi tweens for scenes | |
| Audio | Howler.js | |
| Schemas / validation | Zod | Content and save validation |
| i18n | ICU MessageFormat (FormatJS) | All strings externalised |
| Tests | Vitest, fast-check (property tests) | |
| UI automation | Playwright (pre-installed in cloud sessions) | E2E and AI UI playtests |
| Desktop packaging | Electron + steamworks.js | Most proven route to Steam (achievements, cloud saves, overlay). Tauri is a smaller-binary alternative but its Steamworks support is less mature |
| CI | GitHub Actions | Lint, typecheck, test, balance regression, builds |

All dependencies pinned to exact versions; upgrades are deliberate.

---

## 3. Repository layout

```
panacea/
├─ docs/                     design, technical plan, playtest pipeline
├─ packages/
│  ├─ sim/                   pure deterministic simulation (no DOM)
│  │  ├─ src/core/           state, turn pipeline, RNG, action reducer
│  │  ├─ src/systems/        lab, body, market, channels, ads, scrutiny,
│  │  │                      relationships, rivals, events, unlocks, timeline, endings
│  │  └─ src/api/            observation + legal actions + apply (bot/agent API)
│  ├─ content/               data files + Zod schemas + content linter
│  │  ├─ ingredients/  processes/  regions/  segments/  unlocks/
│  │  ├─ events/       npcs/       rivals/   endings/   backgrounds/
│  │  └─ strings/en/         localisation source strings
│  ├─ client/                React + PixiJS game client
│  │  ├─ src/screens/        Lab, Ledger & Map, Post, Press Room, Parlour, Unlocks
│  │  ├─ src/pixi/           lab scene, liquids, gauges, map effects
│  │  └─ src/platform/       save storage, settings, Steam bridge (abstracted)
│  ├─ desktop/               Electron shell, steamworks.js integration, builds
│  └─ playtest/              bots, balance harness, AI playtester + critic tooling
│     ├─ bots/               scripted strategy bots
│     ├─ harness/            batch runner, metrics, reports
│     └─ agents/             persona prompts, review rubric, aggregation
└─ .github/workflows/        CI
```

---

## 4. Simulation architecture

### 4.1 State

A single serialisable `GameState` (plain JSON-compatible objects; no classes with hidden state):

- `meta`: seed, RNG state, date (year, month), background, difficulty, version
- `player`: cash, debts, reputation, persona, notoriety
- `lab`: equipment, formulas, batch queue, inventory, supply contracts
- `market`: regions → segments → cohort distributions; brand state per region
- `customers`: named regulars (individual body models)
- `channels`, `campaigns`, `contracts`
- `npcs`: relationships (price, loyalty, leverage)
- `rivals`: AI proprietor state
- `scrutiny`: heat per region, active investigations, incidents
- `events`: active cards, chain flags, cooldowns
- `unlocks`: unlocked, progress, lost
- `ledger`: monthly history, the human ledger
- `log`: action log (for replay)

### 4.2 Turn pipeline

Ending a month runs fixed, ordered phases, each a pure function `(state, rng) → state`, plus a list of **causal deltas** for the Ledger:

1. Production (batches complete; variance; bad-batch rolls)
2. Supply & prices (shocks, shortages)
3. Advertising (awareness, trust)
4. Distribution (availability per region/channel)
5. Demand & sales (per segment; named customers individually)
6. Body model (tolerance, dependency, toxic load, health, deaths)
7. Rivals (AI decisions)
8. Scrutiny (incidents from deaths/complaints; investigations progress; heat decay)
9. Relationships (loyalty drift, demands, betrayals)
10. Timeline (historical events by date)
11. Unlock evaluation (progress, gains, losses)
12. Event generation (draw The Post for next month)
13. Ledger (snapshot, causal breakdown)

Every numeric change that the player sees must carry a `cause` tag, which is how the Ledger explains "Sales −18%: …".

### 4.3 Actions

Player actions are serialisable commands (`BrewBatch`, `EditFormula`, `PlaceAd`, `SignContract`, `ResolveEvent`, `Bribe`, …) applied through a single reducer that validates legality. The client, bots and AI agents all use the same commands.

### 4.4 Machine API (for bots and AI playtesters)

```ts
observe(state, view: 'full' | 'player'): Observation   // what a player could see
legalActions(state): ActionSpec[]                      // with parameter ranges
apply(state, action): { state, result }
endTurn(state): { state, report: TurnReport }          // report includes causal deltas
```

The `player` view hides internal truths (e.g. real death counts before newspapers report them) so agents see what a human would.

### 4.5 Body & market at scale

- Named customers: individually simulated (~12–40).
- Cohorts: each segment's population is simulated as a small set of distribution buckets (e.g. tolerance × dependency × load bins) rather than individuals. This keeps a full national run fast enough to simulate **thousands of runs per minute** headless.
- Performance budget: full 260-month run ≤ 200 ms headless on a CI machine.

### 4.6 Content & events

- Events are data: `conditions` (a small typed predicate DSL over state), `choices`, `effects` (typed effect DSL), `weight`, `cooldown`, `chain` flags.
- A **content linter** in CI checks schemas, unreachable events, missing strings, broken chain references, unlock cycles and unreachable unlocks.

### 4.7 Saves

- Save = `{ version, state }` serialised JSON, compressed; autosave each month plus manual slots; Ironman single slot.
- Versioned **migrations** for every schema change; CI round-trips saves from every past version.
- Steam Cloud through the platform layer.

---

## 5. Client architecture

- React renders screens from immutable state snapshots; Zustand holds UI-only state (selected region, open panels).
- The PixiJS lab scene is embedded in the Lab screen; liquid colour, level, foam and gauge readings are derived from sim state; distillation interaction emits actions back to the sim.
- Every interactive element has an accessible role and label, which serves accessibility *and* lets Playwright-driven AI playtesters operate the real UI.
- Platform layer abstracts storage, Steam, window management, so the same client runs in a browser (for demos and testing) and in Electron.

---

## 6. Quality engineering

- **Unit and property tests** for every system (e.g. tolerance never negative; money conserved across transactions; deterministic replay).
- **Golden replays:** recorded runs whose end states are snapshot-tested; unintended diffs fail CI.
- **Balance regression:** nightly batch of bot runs; metrics compared to bands (see the playtest pipeline doc).
- **E2E:** Playwright smoke tests of each screen and a scripted month loop.
- **Performance budgets:** 60 fps on min spec in the lab; turn resolution < 100 ms in-client; cold start < 5 s.
- **Crash reporting:** opt-in, privacy-respecting (local log bundle + optional upload).

---

## 7. Milestones

Each milestone ends with a playtest cycle (see the pipeline doc). No date estimates until M1 tells us real velocity.

| Milestone | Scope | Exit criteria |
|---|---|---|
| **M0 Foundations** | Monorepo, tooling, CI, sim skeleton (state, RNG, turn pipeline, action reducer), content schemas, machine API, bot harness skeleton | A "do nothing" bot plays a 260-month run deterministically in CI |
| **M1 Sim prototype** | Lab (properties + processes, no visuals), body model, the shop with named customers, 1 region, basic newspapers, first ~10 unlocks, inquest event; debug UI | Bots reveal tolerance/death trade-offs; first balance report |
| **M2 Vertical slice** | Manhattan + 1 rural region, wholesale, scrutiny & incidents, ~40 events, Ledger with causal breakdowns, Pixi lab with the still interaction, art-style test on 2 screens | First full AI playtest + critic cycle; a human friends-and-family test; Steam store page |
| **M3 Alpha** | All systems: all channels, factory, shows, mail order, letters, rivals, relationships, Association & lobbying, full timeline to 1906, endings, backgrounds; placeholder art where needed | All systems playable end to end; no P0 bugs; balance bands met by bots |
| **M4 Beta (content complete)** | All content (~24 ingredients, ~60 unlocks, 300+ events, ~6 regions), final art and audio, localisation-ready, Electron + Steam (achievements, cloud saves), saves/migrations, accessibility options | Release gates trending green; closed human beta; Next Fest demo |
| **M5 Release candidate** | Polish, performance, bug burn-down, launch build | All release gates in the pipeline doc pass on 3 consecutive builds |

---

## 8. Immediate next steps

1. Scaffold the monorepo (pnpm, Vite, TypeScript strict, Vitest, ESLint/Prettier, CI).
2. Implement `packages/sim` core: state types, seeded RNG, turn pipeline skeleton, action reducer, machine API.
3. Define Zod schemas for ingredients, processes, unlocks and events; seed a first data set.
4. A first bot ("do nothing" and "greedy narcotic") running full runs in CI.
