# Panacea · balance pass 8 · what the ward can pay

Content 0.10.0 → **0.11.0**. Both sims changed together (the TypeScript reference and the C# port). The parity
fixtures were regenerated (32 runs) and `dotnet test` is green (117). TS 140/140, typecheck, lint, prettier and
`content:lint` (0 errors, 0 warnings) pass. Nothing under `godot/`, in `events.json` or in the letter text of
`en.json` was touched.

This pass answers two things:

- the laudanum line's measurements disagreed (pass 2 said 1.12× adaptive, the letters fix said 1.64× and then
  1.51×);
- the honest bold-claims lines sat at 1.24× and 1.43× adaptive, and the round-8 tycoon found that price didn't
  push back ([round08/tycoon.md](round08/tycoon.md), finding 6: "going from 60c to 95c cost 2–4 walkaways a month
  while sales doubled").

## 1. The measurements, reconciled

**The right number for HEAD is 1.05×** (mean final cash, 40 seeds `top-0…39`, against adaptive on the same seeds;
the median of the paired ratios is 1.17×). Pass 2's 1.12× was right for its own commit. The letters fix's 1.64× and
1.51× came from a measuring script that reused one bot for all 40 games.

**What went wrong.** `policyBot` keeps the dose it has reached (`narcoticOz`) and its last recipe in a closure.
`pnpm sim:batch` and `pnpm sim:optimize` build a fresh bot for every game (`runBatch` takes factories for exactly
this reason), and so did pass 2's script (`ev2/topcheck.ts`). The letters fix's `lfix/cand.ts` called `policyBot(p)`
once and passed the same object to all 40 `runGame` calls. Game _k_ then started at the dose game _k − 1_ ended
with:

- adaptive started later games with whatever it had escalated to or cut to, which cost it about $1,500 a game;
- candidate 72 started at up to 1.2 oz.

Both numerator and denominator were wrong.

Re-scored with git worktrees, the same candidate (regenerated from `optimize:ev1`) and the same seeds:

| Commit                         | Fresh bot per game: adaptive mean, #72 | One bot for all 40 games: adaptive mean, #72 | Reported           |
| ------------------------------ | -------------------------------------- | -------------------------------------------- | ------------------ |
| `2081b78` (events pass 1)      | $21,017, **1.250×**                    | $19,382, 1.655×                              | pass 1: 1.25×      |
| `80e045d` (events pass 2)      | $20,769, **1.122×**                    | $19,287, 1.642×                              | pass 2: 1.12×      |
| `523a04d` / HEAD (letters fix) | $20,904, **1.050×**                    | $19,080, 1.513×                              | letters fix: 1.51× |

- **Fresh bots reproduce both events passes to the dollar**: $26,266 for #72 at `2081b78` and $23,293 at `80e045d`.
- **Stale bots reproduce the letters fix to the dollar**: $31,673 for #72 before its changes and $28,876 after.
- **Candidate 14 confirms the cause.** It has no narcotic, so it has no dose to carry over. It scores $25,780 at
  `80e045d` with either script. Only its ratio moved (1.24× against 1.34×), because the baseline under it did.
- **There was also a real change.** Between pass 2 and HEAD the letters fix moved #72 from 1.12× to 1.05×.

**The method used from here on** (`bal8/cand.mts`):

- a fresh `policyBot` for every game;
- seeds `top-0…39`;
- the mean final cash against adaptive's mean on the same seeds, with the median of the per-seed ratios beside it;
- optimizer candidates regenerated from `optimize:<search seed>`.

## 2. What was actually too strong

At HEAD with that method the laudanum line is not the problem (1.05×). The honest bold-claims lines are:

- `ev2` 32 at **1.43×**: no narcotic, 3 oz spirit, valerian, 118¢, every bold claim, every venture;
- `ev1` 14 at **1.24×**: 2.5 oz spirit, 117¢, every bold claim, three ventures.

The fresh searches found nothing higher (§5).

**Why price didn't push back.** Every competent shop fills its regulars to capacity (70 with the branch and the
shop next door), and a regular never leaves for the price:

- **Bottles per regular don't move.** A regular buys one bottle a month if it costs no more than they would pay,
  and spends their budget on more. Their spirits make them about 0.75 dependent within a year, even with no
  narcotic, which raises both what they would pay and their budget by three-quarters. So 32's regulars buy two
  bottles a month at 118¢, as adaptive's do at 70¢.
- **The price chooses the customers.** A dear price doesn't shrink the custom; it changes who the regulars are. At
  118¢ the cap fills with clerks and the well-to-do. At month 264 the line had 51 clerks and 14 well-to-do of its
  70 regulars, against 21 and 6 for adaptive. The ward is four clerks in 25 households and one well-to-do.
- **The claims are the door.** They lift what a stranger would pay over 118¢ and multiply the chance a buyer comes
  back (×1.9–2.5). Without its claims the same line goes bankrupt in every game. Once the cap is full, the claims'
  only cost is heat, which an honest shop with few deaths barely feels.

| `ev2` 32 at HEAD (40 seeds)    | × adaptive              |
| ------------------------------ | ----------------------- |
| at 70¢                         | 0.98×                   |
| at 90¢                         | 1.24×                   |
| at 100¢                        | 1.29×                   |
| at 118¢ (as found)             | 1.43×                   |
| at 150¢                        | 0.51× (60% ended early) |
| at 118¢, no claims             | bankrupt in every game  |
| adaptive with 32's four claims | 0.86×                   |

At an even price the bold claims buy nothing net. The whole edge is price, and price only works because a dear
shop can still fill itself with the ward's richest customers.

## 3. What changed

Two rules in the market, both sims, identically.

### What the ward can pay (`market.dearness`)

A regular who comes in for their monthly bottle asks the price. Up to what they can comfortably spend on one,
**`dearAbove` (1.15) × their usual price**, they always buy. Dearer than that, they make the last bottle do, and buy
that month with chance **(comfortable ÷ price) ^ `dearElasticity` (1.5)**. Past the comfortable price, each cent
asked loses more than a cent of custom.

- **Narcotics hold.** Spirits are sold in every saloon; a narcotic is not. A bottle whose opiates and cocaine make
  a habit of `dearHabitHold` (3, about half an ounce of laudanum) or more is found the money for at up to twice the
  comfortable price, pro rata below that. A quarter ounce counts half. A tonic of spirits and herbs gets no hold.
  This keeps "poison tempts" (balance pass 7) intact: the hooked still pay what they are asked.
- **The balks are counted.** A regular who goes without is counted with the callers who asked the price and went
  away (`turnedAwayByPrice`), so THE TRADE's line shows the pushback.
- **Code:** TS `dearness`, `narcoticHabit`; C# `Market.Dearness`, `Market.NarcoticHabit`. The power uses the
  portable `dmath` `exp`/`log`.

### How many of each kind the ward has (`market.wardHasRoomFor`)

The ward is so many households of each kind, in the proportions of the segments' weights. A stranger becomes a
regular only while the shop has fewer than **`wardShareSlack` (3) × its capacity × that kind's share** of that
kind. At a capacity of 70 that is:

| Kind       | Most regulars of that kind |
| ---------- | -------------------------- |
| clerks     | 33                         |
| labourers  | 50                         |
| mothers    | 33                         |
| well-to-do | 8                          |

A bottle priced for the carriage trade can't fill a shop with it. The honest bots end well under these limits
(adaptive ends with 21 clerks and 6 well-to-do).

Code: TS `wardHasRoomFor(state, content, segmentId, capacity)`, C# `Market.WardHasRoomFor`. It is checked before
the conversion roll, so the RNG is not drawn when there is no room.

### Config, tests, version

**Config:** `market.dearAbove` 1.15, `market.dearElasticity` 1.5, `market.dearHabitHold` 3,
`walkIns.wardShareSlack` 3 (0 turns either rule off). The fields are in `schemas.ts` and `ContentTypes.cs`.

**Tests:** TS `packages/sim/test/price.test.ts` and C# `PriceTests.cs` mirror each other.

- A regular always buys at the comfortable price and less often above it, and revenue falls as the price rises
  past it.
- A spirits taste doesn't pay a dear price; a laudanum habit does.
- Regulars who would pay go without a dear bottle and are counted as turned away by price. In TS this is checked
  against the same month with no elasticity.
- A shop wins at most slack × capacity × share of any kind, a full shop of clerks still has room for a labourer,
  and slack 0 is no limit.

`GameTests` checks the version and the two new values.

### What I tried and didn't keep

Each was measured on the same 40 seeds:

- **The comfortable price scaled by dependency, as willingness to pay is.** It changed nothing, because spirits
  alone make every long-term regular about 0.75 dependent.
- **Claims believed less as suspicion rises** (× (1 − k × heat/100)). At k = 1, 32 only fell from 1.43× to 1.36×,
  while the laudanum lines went bankrupt (the strangers stop believing the label at their heat).
- **Elasticity alone.** The line just reprices: the 3 oz spirit recipe peaks at 1.42× at 100¢, with the cap full of
  clerks.
- **The per-kind limit alone.** It still peaks at 1.26× at 100¢.
- **A tighter or soft per-kind limit** (slack 2–2.5). Cautious lost 10% and greedy collapsed.

## 4. Bold claims, now

At an even price an honest bottle with every bold claim earns what adaptive does:

- the 3 oz spirit recipe at 80¢ is 1.08×, against adaptive repriced to 80¢ at 1.06×;
- the 2.5 oz recipe (`ev1` 14) at 80¢ is 0.80×, and at 95¢ 0.96×.

The claims still draw strangers and still cost heat, wholesale contracts and letter trust. They no longer let a
shop charge the carriage-trade price to a full house. I did not change the claims themselves.

## 5. The lines, before → after (40 seeds `top-0…39`, mean × adaptive, median of paired ratios, ended early)

Adaptive's mean is $20,904 before and $20,875 after.

| Line (candidate)                                                                               | Before                | After              |
| ---------------------------------------------------------------------------------------------- | --------------------- | ------------------ |
| 1.1 oz laudanum escalating, 155¢, four bold claims, silent, every venture, first (`ev1` 72)    | 1.05× (1.17×, 10%)    | 0.31× (0.27×, 10%) |
| No narcotic, 2.5 oz spirit, 117¢, four bold claims, testify, three ventures (`ev1` 14)         | **1.24×** (1.24×, 0%) | 0.36× (0.36×, 0%)  |
| No narcotic, 3 oz spirit, 118¢, $8 ads, four bold claims, every venture (`ev2` 32)             | **1.43×** (1.42×, 0%) | 0.69× (0.70×, 0%)  |
| No narcotic, 2.7 oz spirit, 112¢, four bold claims (`ev2` 37)                                  | 1.18× (1.19×, 0%)     | 0.60× (0.60×, 3%)  |
| 0.64 oz laudanum, 153¢, four bold claims, bribe (`ev2` 33)                                     | 0.94× (1.17×, 23%)    | 0.02× (0.02×, 25%) |
| 0.13 oz morphine, 152¢, $30 ads, four bold claims (`b8` 84)                                    | 1.13× (1.20×, 8%)     | 0.72× (0.75×, 5%)  |
| 0.12 oz morphine, 83¢, three mild claims, testify, every venture with the laboratory (`b8` 79) | 1.15× (1.15×, 0%)     | 1.15× (1.15×, 0%)  |
| 0.15 oz morphine, 3 oz spirit, 178¢, $30 ads, bold claims (`b8x` 53)                           | 1.04× (1.15×, 45%)    | 0.10× (0.10×, 3%)  |
| 0.63 oz laudanum, 103¢, bold claims, bribe (`b8` 71)                                           | 1.04× (1.06×, 0%)     | 1.02× (1.02×, 0%)  |
| 0.35 oz laudanum, 106¢, mild claims, silent (`ev2` 95)                                         | 1.09× (1.09×, 0%)     | 1.06× (1.07×, 3%)  |
| 0.1 oz morphine, 3 oz spirit, 109¢, no claims, every venture (`b8x` 8)                         | 1.05× (1.08×, 3%)     | 1.05× (1.08×, 3%)  |

**The fixed lines that collapse are mispriced now, not dead.** Repriced, after:

| Line, repriced                     | × adaptive (40 seeds) |
| ---------------------------------- | --------------------- |
| `ev2` 32 at 80¢                    | 1.08×                 |
| `ev2` 32 at 90¢                    | 1.01×                 |
| `ev2` 32 at 100¢                   | 1.03×                 |
| `ev2` 32 at 110¢                   | 0.86×                 |
| `ev2` 32 at 130¢                   | 0.24×                 |
| `ev2` 32 at 100¢, before this pass | 1.29×                 |
| `ev1` 14 at 80¢                    | 0.80×                 |
| `ev1` 14 at 95¢                    | 0.96×                 |
| `ev2` 37 at 90¢                    | 0.86×                 |
| `ev1` 72 at 120¢                   | 1.06× (median 1.13×)  |
| adaptive at 80¢                    | 1.06×                 |
| adaptive at 90¢                    | 1.08×                 |
| adaptive at 100¢                   | 0.96×                 |

**Fresh exploit-hunter searches.** I ran `sim:optimize`'s search (120 candidates × 4 seeds `opt-<seed>-0…3`, the
same candidates) on four search seeds: `ev1`, `ev2`, a new `b8` and `b8x`. Each search's top lines were re-scored on
40 seeds.

- **Before (HEAD):**
  - `ev2` 32 1.43×; `ev1` 14 1.24×; `ev2` 37 1.18×;
  - `b8` 79 1.15×; `b8` 84 1.13×; `ev2` 56 1.10×; `b8` 117 1.08×;
  - `b8x` 53 1.04×; `b8x` 8 1.05×.
- **After:**
  - `b8` 79 **1.15×**, then 71 1.02× and 14 0.30×;
  - `ev2` 95 1.06×, then 56 0.97× and 62 0.94×;
  - `b8x` 8 1.05×, then 46 0.97× and 31 0.89×;
  - `ev1` 10 0.83× and 40 0.71×.

**No line found is above 1.15×.** The best, `b8` 79, is 0.12 oz of morphine a bottle at 83¢ with its own laboratory. This pass didn't touch it (1.15× before and after). Wealth and product deaths correlate at 0.18 / −0.01
/ 0.29 / 0.17 across the four searches, against 0.11 / −0.05 / 0.21 / 0.10 before. The richest lines lean a little
more on narcotics now, because the narcotics hold their price.

## 6. Every bot, before → after

`pnpm sim:batch --bots shopkeeper,cautious,greedy,briber,adaptive,sloppy,heavy --runs 80 --seed b8 --fast`, before
(HEAD, 0.10.0) and after:

| Bot        | Median final, before → after | Mean              | p10 / p90 after   | Early endings | Cash m12 / m24 / m48 after | Product deaths | Peak heat |
| ---------- | ---------------------------- | ----------------- | ----------------- | ------------- | -------------------------- | -------------- | --------- |
| shopkeeper | $15,713 → **$15,751**        | $15,685 → $15,689 | $15,122 / $16,224 | 0% → 0%       | $236 / $319 / $353         | 2 → 2          | 30 → 30   |
| cautious   | $6,877 → **$6,867**          | $6,808 → $6,790   | $6,096 / $7,359   | 0% → 0%       | $189 / $220 / $283         | 6.5 → 6.5      | 41 → 41   |
| greedy     | $25,877 → **$11,077**        | $20,768 → $11,492 | $3,906 / $19,043  | 50% → 85%     | $254 / $495 / $1,360       | 5 → 9          | 100 → 100 |
| briber     | $15,395 → **$15,356**        | $14,618 → $14,582 | $11,715 / $17,584 | 5% → 5%       | $219 / $435 / $680         | 31 → 31        | 100 → 100 |
| adaptive   | $21,023 → **$20,995**        | $20,940 → $20,926 | $20,297 / $21,371 | 0% → 0%       | $215 / $271 / $598         | 2 → 2          | 22 → 22   |
| sloppy     | $15,936 → **$16,028**        | $15,320 → $15,440 | $11,358 / $18,816 | 1% → 1%       | $214 / $299 / $432         | 8.5 → 8        | 61 → 59   |
| heavy      | $4,512 → **$4,512**          | $8,408 → $8,411   | $476 / $18,501    | 61% → 61%     | $331 / $631 / $809         | 68 → 68        | 100 → 100 |

The dark and noisy bots at 240 runs (`--seed b8h`), before → after:

| Bot    | Ended early   | Median            | Mean              | p10 / p90                             |
| ------ | ------------- | ----------------- | ----------------- | ------------------------------------- |
| heavy  | 54% → **54%** | $9,819 → $9,655   | $9,757 → $9,728   | $620 / $18,432 → $625 / $18,443       |
| greedy | 54% → **87%** | $24,402 → $10,911 | $20,008 → $11,632 | $4,899 / $29,554 → $4,646 / $19,087   |
| briber | 3% → **3%**   | $15,743 → $15,699 | $15,180 → $15,162 | $12,473 / $18,165 → $12,328 / $18,115 |
| sloppy | 0% → **0%**   | $14,965 → $15,068 | $14,825 → $14,887 | $10,083 / $18,552 → $10,012 / $18,680 |

- **Honest play is unchanged** to within 1%: adaptive $21.0k, shopkeeper $15.8k, cautious $6.9k, sloppy $16.0k,
  briber $15.4k. None of them prices above what its ward can comfortably pay, or fills itself with one kind.
- **Heavy is untouched**: at 70¢ its laudanum holds, so it still ends early in 54–61% of games.
- **Greedy is still a gamble, a worse one.** It ends early in 85–87% of games (50–54% before). Its median fell from
  $24–26k to $11k, and its best games (p90 $19k) no longer beat adaptive's median. It still leads early: m12 $254
  and m24 $495, against adaptive's $215 and $271.
  - **Why:** greedy's jackpot was a full house of hooked clerks and well-to-do at $1.50 (59 of its 67 regulars at
    the end). The ward now has 33 clerks and 8 well-to-do to give it. The poorer regulars it takes instead die and
    complain faster than they pay.
  - **The knob:** `walkIns.wardShareSlack` 4 restores greedy (0.89–0.92× adaptive's mean, 53–65% ended early).
    But it lets the 3 oz spirit recipe back to 1.22–1.24× at 95–100¢. I kept 3 because the target was the exploit
    line, and greedy still ends early often.

## 7. The tycoon's finding

Price now pushes back on a tonic of spirits and herbs. `ev2` 32 at 118¢, months 97 on (20 seeds):

| Per month, months 97 on              | Before | After |
| ------------------------------------ | ------ | ----- |
| regulars                             | 70     | 54    |
| counter bottles                      | 142    | 80    |
| takings                              | $181   | $110  |
| callers who asked the price and left | 28     | 40    |

A bottle with real laudanum in it is different. The tycoon's tonic had half an ounce, and its habit still pays what
it is asked, as balance pass 7 meant it to. So at 95¢ it will keep selling. That was the poison tempting, not price
failing.

## Notes for the desk (`godot/`, not touched)

- **`TurnedAwayByPrice` now includes the dear balks.** It counts regulars who came in, asked the price and made the
  last bottle do, as well as callers who couldn't pay. THE TRADE's line ("Callers who asked the price and went
  away") and the "dear" option in `EditionWriter` pick it up unchanged. If the desk wants the two apart, the sim can
  split the field. Suggested copy for the regulars: "Mrs. Doyle asked the price and said her last bottle would do
  another month."
- **When a shop has all the clerks the ward has**, `Market.WardHasRoomFor(state, content, "clerk", capacity)` is
  false. The paper could say so: "Every clerk in the ward who takes a tonic already takes yours."

## Plumbing

- **Content:** `config.json`:
  - `contentVersion` 0.11.0;
  - `market.dearAbove` 1.15, `dearElasticity` 1.5 and `dearHabitHold` 3;
  - `walkIns.wardShareSlack` 3.

  The fields are in `schemas.ts`.

- **TS sim** (`systems/market.ts`): `dearness`, `narcoticHabit` and `wardHasRoomFor`. The dear check sits in
  `salesPhase` (a regular's first bottle) and the per-kind check in `walkIns` (conversion). `core/state.ts` updates
  the `turnedAwayByPrice` doc.
- **C# sim:**
  - `Content/ContentTypes.cs` adds `MarketConfig.DearAbove`, `DearElasticity` and `DearHabitHold`, and
    `WalkInsConfig.WardShareSlack`;
  - `Systems/Market.cs` adds `Market.Dearness`, `Market.NarcoticHabit` and `Market.WardHasRoomFor`, in the same
    places as the TS;
  - `Core/State.cs` updates the doc.
- **Tests:** `packages/sim/test/price.test.ts` (5) and `sim-cs/Panacea.Sim.Tests/PriceTests.cs` (4); `GameTests`
  (version, new config).
- **Fixtures** regenerated (`pnpm exec tsx packages/playtest/src/cli/parity-fixtures.ts`).
- **Measuring scripts** (scratch, not committed), in `…/scratchpad/bal8/`:
  - `cand.mts`: re-scores lines with a fresh bot per game; `ROOT=` takes a worktree, `CFG=` overrides config;
  - `hunt.mts` / `hunt.sh`: the optimizer's search, sharded, with candidate indices;
  - `diag.mts`: where a line's money comes from;
  - `cand-stale.mts`: the stale-bot version, kept to reproduce the letters fix's figures.
