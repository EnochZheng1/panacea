# Panacea

_A Victorian patent-medicine tycoon._ New York, 1885: no FDA, no labeling law, and no limit on what you may bottle and call a cure. Build a medicine empire from one apothecary still before the Pure Food and Drug Act arrives in 1906.

Working title. Pre-production: milestone M1 (body model, scrutiny, bots, debug client) is complete. Latest balance findings: [docs/reports/m1-findings.md](docs/reports/m1-findings.md).

## Documents

- [Game Design](docs/GAME_DESIGN.md): premise, systems, progression, risks
- [Technical Plan](docs/TECHNICAL_PLAN.md): TypeScript stack, architecture, milestones
- [Playtest & Release Pipeline](docs/PLAYTEST_PIPELINE.md): simulated player/critic cycles and release gates

## Development

Requires Node 22.12+ and pnpm 10.

```bash
pnpm install
```

```bash
pnpm check
```

`pnpm check` runs format, lint, typecheck, content lint and tests. Other commands:

| Command                                                      | What it does                                                    |
| ------------------------------------------------------------ | --------------------------------------------------------------- |
| `pnpm test`                                                  | Unit, property and determinism tests                            |
| `pnpm content:lint`                                          | Validate game data and cross-references                         |
| `pnpm sim:batch --runs 100`                                  | Run every bot over many seeded games and print a balance report |
| `pnpm sim:batch --bots shopkeeper --runs 20 --out report.md` | Same, for chosen bots, saved to a file                          |
| `pnpm sim:optimize --candidates 60 --seeds 4`                | Random search for the most profitable strategy (exploit hunter) |
| `pnpm game:dev`                                              | The game: the 3D shop in the browser                            |
| `pnpm client:dev`                                            | Debug client in the browser: play the sim by hand               |

### Exporting the Godot game

`godot/export_presets.cfg` has Linux, Windows and macOS presets. Builds go to `build/`, which is
ignored by git. The presets ship `data/*.json` and the hyphenation patterns (`data/hyph/*.dic`),
which Godot doesn't import on its own. Install the Godot 4.7.2 .NET export templates first, then:

```bash
cd godot && godot --headless --export-release Linux ../build/linux/Panacea.x86_64
```

The macOS preset is unsigned and untested.

### Layout

| Package             | Role                                                                                       |
| ------------------- | ------------------------------------------------------------------------------------------ |
| `packages/content`  | Game data (JSON), Zod schemas, string tables, content linter                               |
| `packages/sim`      | Pure, deterministic simulation: state, turn pipeline, actions, machine API for bots/agents |
| `packages/playtest` | Strategy bots, batch harness, optimizer, balance reports                                   |
| `packages/game`     | The game client: 3D shop (React Three Fiber), ink-engraving renderer, paper UI             |
| `packages/client`   | Debug client (Vite + React)                                                                |

## Stack

TypeScript · React · Three.js (React Three Fiber) · Vite · Electron + Steamworks · Vitest · Playwright
