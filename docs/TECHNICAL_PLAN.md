# PANACEA — Technical Plan

Companion to [GAME_DESIGN.md](GAME_DESIGN.md). The playtest and release process is in [PLAYTEST_PIPELINE.md](PLAYTEST_PIPELINE.md).

> **Engine change (pending a full rewrite of this plan).** The game client moves to **Godot 4 (.NET / C#)** (`godot/`), and the art is the Gazette woodcut set (`godot/art/README.md`). The web client rows of the stack table below (React, Three.js, Electron) are superseded; `packages/game` is a retired style experiment.
>
> The deterministic simulation stays the source of truth. How Godot runs it is the open decision: port it to C# with parity tests on identical seeds against the TypeScript version, or call the TypeScript library from the client. It will be settled when the first playable edition is built.

---

## 1. Principles

1. **The simulation is a pure library.** All game rules live in a headless, deterministic TypeScript package with no DOM, no rendering, no wall-clock time. The client only _displays_ state and _submits_ actions.
2. **Deterministic and replayable.** Seeded PRNG; a run is fully described by `(seed, background, difficulty, action log)`. Bug reports, balance tests and AI playtests all replay exactly.
3. **Data-driven content.** Ingredients, unlocks, events, regions, NPCs and endings are data validated against schemas, not code. Designers (and AI tools) can add content without touching systems.
4. **Machine-playable by design.** The sim exposes a structured observation/action API so scripted bots and AI playtesters can play the real game rules at high speed.
5. **Ship-quality from day one.** Saves are versioned with migrations, strings are externalised for localisation, CI runs on every push.

---

## 2. Stack

| Concern                    | Choice                                                        | Notes                                                                                                                                            |
| -------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Language                   | TypeScript (strict)                                           | Shared across sim, client, tools                                                                                                                 |
| Package manager / monorepo | pnpm workspaces                                               |                                                                                                                                                  |
| Build / dev server         | Vite                                                          | Fast HMR for UI work                                                                                                                             |
| UI                         | React                                                         | Ledgers, cards, map overlays, label designer: HTML/CSS strengths                                                                                 |
| UI state                   | React state (Zustand if it grows)                             | Thin; the sim state is the source of truth                                                                                                       |
| 3D rendering               | Three.js + React Three Fiber                                  | The shop and its stations; a custom ink-engraving post-process (`packages/game/src/render`)                                                      |
| Animation                  | Motion (Framer Motion) for UI; GSAP or Pixi tweens for scenes |                                                                                                                                                  |
| Audio                      | Howler.js                                                     |                                                                                                                                                  |
| Schemas / validation       | Zod                                                           | Content and save validation                                                                                                                      |
| i18n                       | ICU MessageFormat (FormatJS)                                  | All strings externalised                                                                                                                         |
| Tests                      | Vitest, fast-check (property tests)                           |                                                                                                                                                  |
| UI automation              | Playwright (pre-installed in cloud sessions)                  | E2E and AI UI playtests                                                                                                                          |
| Desktop packaging          | Electron + steamworks.js                                      | Most proven route to Steam (achievements, cloud saves, overlay). Tauri is a smaller-binary alternative but its Steamworks support is less mature |
| CI                         | GitHub Actions                                                | Lint, typecheck, test, balance regression, builds                                                                                                |

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
│  ├─ game/                  the game: React Three Fiber 3D shop + paper UI
│  ├─ client/                debug client (plain React) for playing the sim by hand
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
- The 3D shop (`packages/game`) is a React Three Fiber scene. Every object reflects sim state (bottle fill from inventory, display-case stock, customers' posture, letters on the desk); interactions emit sim actions.
- **Render pipeline:** colour + depth, view-space normals and a liquid mask are rendered offscreen; a composite shader hatches tone, inks depth/normal/luminance edges, adds paper grain, and lays the glowing liquids over the top. Meshes opt in with `userData.liquid` / `userData.glass`. The pass can be disabled for accessibility and low-end machines.
- Paper UI is HTML/CSS over the canvas: crisp text, localisation, and operable by Playwright-driven AI playtesters.
- Every interactive element has an accessible role and label, which serves accessibility _and_ lets Playwright-driven AI playtesters operate the real UI.
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

| Milestone                      | Scope                                                                                                                                                                                               | Exit criteria                                                                            |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| **M0 Foundations**             | Monorepo, tooling, CI, sim skeleton (state, RNG, turn pipeline, action reducer), content schemas, machine API, bot harness skeleton                                                                 | Bots play complete runs (to the 1906 end date or bankruptcy) deterministically in CI     |
| **M1 Sim prototype**           | Lab (properties + processes, no visuals), body model, the shop with named customers, 1 region, basic newspapers, first ~10 unlocks, inquest event; debug UI                                         | Bots reveal tolerance/death trade-offs; first balance report                             |
| **M2 Vertical slice**          | Manhattan + 1 rural region, wholesale, scrutiny & incidents, ~40 events, Ledger with causal breakdowns, Pixi lab with the still interaction, art-style test on 2 screens                            | First full AI playtest + critic cycle; a human friends-and-family test; Steam store page |
| **M3 Alpha**                   | All systems: all channels, factory, shows, mail order, letters, rivals, relationships, Association & lobbying, full timeline to 1906, endings, backgrounds; placeholder art where needed            | All systems playable end to end; no P0 bugs; balance bands met by bots                   |
| **M4 Beta (content complete)** | All content (~24 ingredients, ~60 unlocks, 300+ events, ~6 regions), final art and audio, localisation-ready, Electron + Steam (achievements, cloud saves), saves/migrations, accessibility options | Release gates trending green; closed human beta; Next Fest demo                          |
| **M5 Release candidate**       | Polish, performance, bug burn-down, launch build                                                                                                                                                    | All release gates in the pipeline doc pass on 3 consecutive builds                       |

---

## 8. Status

**M0 is complete:** monorepo, CI, deterministic simulation core, content schemas and linter, machine API, bot harness.

**M1 is complete.** What it added:

- **Body model** (`packages/sim/src/systems/body.ts`, `pharmacology.ts`): per-class tolerance (alcohol, opiate, cocaine) that blunts felt effect and partly protects against toxicity; dependency and withdrawal; acute overdose weighed against health; accumulating toxins (heavy metals); infants as vulnerable consumers; satisfaction against each ailment's severity.
- **Customer base** (`customers.ts`, `market.ts`): named regulars from content plus procedurally generated ones; walk-ins drawn from segments; satisfied walk-ins become regulars up to shop capacity; dependent regulars buy more and pay more.
- **Lab**: stock is tracked per batch (lot) with its own recipe snapshot and true potency; occasional mis-measured batches; quality control; `Reformulate`, label claims, newspaper ad spend, wholesale discount.
- **Scrutiny** (`scrutiny.ts`): heat from deaths, complaints and bold claims, with decay; coroner's inquests raised as system events; verdict odds from detectability, heat, reputation, claims and bad batches; escalating bribes; shutdown after two months at heat 100.
- **Human ledger**: every death is recorded with its public verdict and hidden truth.
- **Content**: 17 ingredients, 9 unlocks, 8 events (incl. inquests, the temperance ladies, the Board of Health), 7 customer segments, 11 ailments, 8 label claims.
- **Bots** (`packages/playtest/src/bots/policy.ts`): one parameterised policy with presets `shopkeeper`, `cautious`, `greedy`, `briber`, `adaptive`, `mercurial`, plus `idle`, `random` and `sloppy` (`bots/sloppy.ts`, calibrated to how human testers play: stock by eye, plain letters, ventures when the till allows); batch reports now include deaths, causes, inquests, bribes and heat.
- **Optimizer** (`pnpm sim:optimize`): random search for the most profitable strategy, reporting whether wealth correlates with deaths.
- **Debug client** (`packages/client`, `pnpm client:dev`): plays the real sim by hand, with a "show hidden truth" toggle.
- **Performance**: in-place actions for the harness keep a full run at ~80–100 ms.

Balance results and open issues: [reports/m1-findings.md](reports/m1-findings.md).

**3D style test (between M1 and M2): done.** `packages/game` (`pnpm game:dev`) is a playable 3D shop with the ink-engraving look, wired to the real simulation. See the art direction in the design document, section 10.

**M2 next steps (vertical slice)**

1. Regions and cohorts: Manhattan plus one rural region, as aggregate markets with the shop's regulars as the named sample.
2. Wholesalers and newspapers as channels, with the red clause.
3. Counterweights to wild claims and deadliness (see the findings), including the 1905 exposé.
4. ~40 events; Ledger causal breakdowns in the client.
5. Build on the 3D style test: hands-on still interaction (valve, pressure risk), label press, month-end Ledger reveal with sound.
6. First AI playtester and critic cycle (see the playtest pipeline).
