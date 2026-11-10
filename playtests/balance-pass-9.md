# Panacea · balance pass 9 · the careful households

Content 0.11.0 → **0.12.0**. Both sims changed together (TypeScript reference and C# port). Parity fixtures
regenerated (32 runs); `dotnet test` 129/129, TS 152/152, typecheck, lint, prettier, `content:lint` (0/0),
`godot` `dotnet build` and `check_cuts.py` pass. Nothing under `godot/`, in `events.json` or in `en.json` was touched.

Method as in [balance-pass-8.md](balance-pass-8.md): a fresh bot per game, seeds `top-0…39`, mean final cash
against adaptive's mean on the same seeds (median of paired ratios beside it), optimizer candidates regenerated
from `optimize:<search seed>`. "Before" is HEAD (`a6644f1`, content 0.11.0, identical to pass 8's "after":
adaptive $20,875 on both). Scripts are in `…/scratchpad/bal9/` (below).

New this pass: **scripted hand players** (`bal9/hand.mts`, 40 seeds, snapshots at months 12 and 24):

- `opt`: the sim optimizer's Kehoe Cordial: the starter mixture with ½ oz laudanum, ½ oz valerian, $1.00, four
  bold claims, a quarter column, the coroner paid every time, the almanac and the shop next door. `opt75`: the
  same at 75c; `opt15`: 1½ oz (his jump-ahead shop); `optsil`: silent at inquests instead of paying.
- `mb`: his drug-free Mulberry Bitters at 75c with the same claims and ad.
- `cozy`: the cozy player's Mulberry Comfort Tonic: 2 oz spirit, valerian, gentian, 45c, SOOTHES THE NERVES, a
  quarter column, testify, the shop next door at 25 regulars. `cozy50`: at 50c.

## What changed (both sims, identically)

| Rule                                                                                                                                                                                                                                                                                       | Config                                                                                                                    | Code (TS / C#)                                |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------- |
| **A mild bottle never makes a craving.** A month whose bottles all have habit under `habitFloor` (the assay's "none") can raise dependency only to `mildDependencyCap`, below the craving line. A habit already made is not wiped by a weaker bottle; it fades at the usual rate.          | `body.habitFloor` 1, `body.mildDependencyCap` 0.25                                                                        | `body.ts` monthForCustomer / `Body.cs`        |
| **The careful households.** Each kind of household has a `wary` share (mothers 0.4, well-to-do 0.5, clerks 0.35, seamstresses and elderly 0.25, labourers 0.15, dockworkers 0.1; a quarter of the ward). A wary buyer won't take a bottle whose opiates and coca make habit ≥ `waryHabit`. | `segments[].wary`, `regulars[].wary` (Halloran, Finch, Vanderhoef), `market.waryHabit` 2 (⅓ oz laudanum)                  | `refusesNarcotic`, `RefusesNarcotic`          |
| A bottle with no narcotic at all and a modest label (boldness ≤ `waryBoldness`) is its own recommendation to them: `cleanAppeal` more, as a claim would be (it raises what they'd pay, their chance to buy and to come back).                                                              | `market.cleanAppeal` 2, `market.waryBoldness` 1.5                                                                         | `trustedByWary`, `labelAppeal`                |
| Pleased wary regulars tell the neighbours: `carefulPerWary` callers each a month, all wary, counted as `callers.careful`, while the shelf holds a bottle they trust.                                                                                                                       | `walkIns.carefulPerWary` 0.5                                                                                              | `carefulCallers`, `CarefulCallers`            |
| Wary callers who won't take a narcotic bottle are counted apart from the price.                                                                                                                                                                                                            | –                                                                                                                         | `MonthInProgress/LedgerEntry.refusedNarcotic` |
| **The coroner's envelope.** A bought verdict no longer lowers the heat (it was −2; the death already raised it). The fee rises half the base with every envelope (was a tenth). Even the first can come out.                                                                               | `scrutiny.bribedVerdictHeat` 0, `bribeRisePerBribe` 0.5, `bribeExposureBase` 0.1, `bribeExposurePerBribe` 0.04 (was 0.03) | `resolveInquest`, `bribeExposureChance`       |
| **The shop next door** costs $3 a month (was $5).                                                                                                                                                                                                                                          | `investments.json` `shop_next_door.upkeepCents` 300                                                                       | –                                             |
| **Names**: every stock of surnames widened (Irish 10 → 30, German 9 → 29, Italian 9 → 29, Jewish 9 → 29, Yankee 11 → 35, the others 7 → 18–19), with more given names; no surname in two stocks; no letter character's surname (Feeney, Kehoe, Doyle…).                                    | `names.json`                                                                                                              | –                                             |

`Customer.wary` and `refusedNarcotic` are new state fields (pre-release: no save migration). The wary draw is
one RNG draw per caller, so every seed's stream moved.

## 1. The addictive strategy at the counter (sim optimizer, finding 1)

Hand players, 40 seeds, means (cash median in brackets). "Named" is games with a verdict against the bottle by
then; "exposed" is envelopes found out.

| Line    | Month | Cash before | Cash after  | Heat before → after | Product deaths | Inquests  | Named (games) | Exposed     | Bribes paid |
| ------- | ----- | ----------- | ----------- | ------------------- | -------------- | --------- | ------------- | ----------- | ----------- |
| `opt`   | 12    | $349 ($354) | $293 ($287) | 33 → 33             | 1.0 → 0.8      | 0.7 → 0.6 | 23% → 23%     | 0 → 0       | 0.1 → 0.1   |
| `opt`   | 24    | $847 ($826) | $691 ($693) | 57 → 56             | 3.7 → 2.5      | 2.9 → 2.1 | 53% → 57%     | 0.07 → 0.15 | 1.8 → 0.9   |
| `opt75` | 24    | $859 ($863) | $698 ($711) | 67 → 68             | 6.1 → 5.1      | 5.2 → 4.5 | 70% → 93%     | 0.30 → 0.60 | 3.3 → 3.0   |
| `opt15` | 24    | $111        | $119        | 90 → 88             | 25 → 24        | 14 → 13   | – → 97%       | – → 1.3     | 10 → 7      |
| `mb`    | 24    | $593 ($608) | $627 ($631) | 43 → 43             | 0 → 0          | 0.8 → 1.0 | 0% → 3%       | –           | –           |
| `cozy`  | 24    | $180 ($180) | $232 ($235) | 2 → 2               | 0 → 0          | 0.1 → 0.3 | 0%            | –           | –           |

Full game: `opt` $16,250 → **$11,728** (5% ended early); the same line silent at inquests, `optsil`, $16,811.
The bribe is now the worse reply: a paid verdict buys no quiet, and over a game the envelopes cost more and come out.

At the counter, `opt` at $1.00 in months 13–24, per month:

| `opt`, months 13–24 / month          | Before | After  |
| ------------------------------------ | ------ | ------ |
| bottles sold                         | 73.7   | 58.5   |
| callers who asked the price and left | 14.8   | 10.1   |
| wary callers who smelt the poppy     | –      | 8.6    |
| books (cash + stock)                 | $41.59 | $33.25 |
| regulars at m24                      | 35.5   | 28.3   |
| regulars ruined by debt at m24       | 4.3    | 4.3    |

- **The temptation stays.** `opt` still leads an honest bold-claims shop at m24 ($691 against `mb`'s $627) and
  the narcotic still holds its price (pass 8's `dearHabitHold` unchanged).
- **The cost now arrives in a short game.** By month 24, 57% of `opt` games have had a jury name the bottle,
  heat is 56 ("alarming"), a quarter of the ward won't buy, and every envelope raises the fee and the odds. At
  heat 40 the fees run $20, $30, $40, $55; the chance it comes out 10%, 14%, 18%, 22% (+ notoriety).
- **"Died of the Vapours" no longer lowers suspicion** (heat change 0 on a bought verdict).
- **The $9 surcharge** belongs to the 1½ oz line (`premiumHabitFloor` 4.5; ½ oz + 2 oz spirit is 3.8). That line
  ends early in every game, and 35% are closed by month 24.

What I tried and didn't keep (40 seeds): **a craver's hold lost to debt** (`holdDebtMonths`: a regular owing
three months' means asks the price like anyone). It did nothing by month 24 (`opt` $679 against $682) but in
the long game it cascaded (balked cravers go into withdrawal and die): greedy ended early in 100% of games, `b8x`
8 went 0.91 → 0.40×. **A habit that eats more of the means** (`meansLostToHabit` 0.6) cost adaptive 10%. Removed.

## 2. A drug-free remedy's own market (sim optimizer finding 2, cozy)

Two remedies on the shelf at the same price (`bal9/shelf.mts`): the clean Mulberry Bitters and the ½ oz Kehoe
Cordial added in month 7, months 7–24, 40 seeds. Bottles a month; regulars at month 24 by the bottle they last bought.

| Same price, same label       | Before: clean / narcotic | After: clean / narcotic | Wary regulars at m24 (after): clean / narcotic | Others: clean / narcotic |
| ---------------------------- | ------------------------ | ----------------------- | ---------------------------------------------- | ------------------------ |
| 75c, four bold claims        | 18.4 / 32.6 (1 : 1.8)    | 20.3 / 30.7 (1 : 1.5)   | **6.1 / 0**                                    | 4.8 / 18.2               |
| 50c, four bold claims        | 22.9 / 35.4 (1 : 1.5)    | 23.6 / 31.4 (1 : 1.3)   | **5.8 / 0**                                    | 5.8 / 17.6               |
| 75c, SOOTHES THE NERVES only | 9.4 / 2.9                | 12.8 / 5.0              | **7.2 / 0**                                    | 1.8 / 6.1                |

Among the customers who care, the clean bottle now takes all the custom. The rest still prefer the laudanum, as
they should. (My reconstruction doesn't reproduce the optimizer's 29 : 3: his Bitters regulars had a year of
spirits dependency pulling them to the stronger bottle. That pull is gone now, §4.)

Period basis: the temperance and chapel households, the respectable trade, and mothers who had read of soothing
syrups (the 1880s–1900s warnings about Mrs. Winslow's and its kind). Their wariness is of the poppy and of the
quack's label ("cures everything" is not trusted whatever is in the bottle), not of spirits.

## 3. The honest shop in year two (cozy, finding 3; satire reader, finding 7)

**Why it stalled.** The cozy line's margin is about 30c a bottle at 45c. In year two it sold 31 bottles a month
to 15 regulars against $6–11 of fixed costs (rent $3, the ad $3, the shop next door $5, a society's dues). Two
things kept it small:

- callers mostly wouldn't buy (valerian and gentian are bitter for mothers and seamstresses);
- strangers came only from the ward and the Gazette, so growth was about one regular a month.

The shop next door at $5 roughly broke even for two years.

Cozy line, cash per month by cause (40 seeds):

| Per month                              | Months 13–24 before | after      | Months 25–48 before | after       |
| -------------------------------------- | ------------------- | ---------- | ------------------- | ----------- |
| takings (shop + walk-ins)              | $14.12              | $20.02     | $22.81              | $37.22      |
| ingredients and bottling               | $4.77               | $6.77      | $6.75               | $10.99      |
| rent, ad, upkeep, letters              | $6.79               | $7.69      | $10.13              | $10.09      |
| **net**                                | **+$2.56**          | **+$5.56** | **+$5.93**          | **+$16.14** |
| regulars                               | 15.5                | 22.6       | 26.8                | 42.9        |
| callers sent by the careful households | –                   | 4.2        | –                   | 10.5        |

Cash at month 24: $180 → $232; whole game $4,167 → $5,414. Honest growth now pays: modestly in year two, and
better as the careful households fill the shop next door.

**The Larger Copper.** Its value is real when the stills are the limit. Bought at the right time (the bots'
rule: last month's sales at 85% of what the stills make), it is worth about $4.3k to adaptive (−20% without it),
$2.2k to shopkeeper and $1.6k to cautious. Bought before the stills are full, as the satire reader did (58 sold,
96 stillable), it is worth nothing until sales grow. The cozy line never qualifies in 24 months (`cozyc` buys
nothing). So it needs a warning, not a price cut: see the desk notes.

## 4. Spirits and habit (cozy, finding 1)

**Decision: no.** A recipe the assay calls mild can't make anyone crave. A bottle under habit 1 (2 oz of grain
alcohol is 0.8) builds at most dependency 0.25, a taste for it, below the craving line (0.3). A bottle of habit 1
or more (2½ oz of spirit, or any laudanum worth the name) can still make a craving, as Hostetter's did.

- **Cozy line at month 24:** craving 7.2 of 18 regulars → **0** of 29.
- **Cautious bot:** product deaths 6.5 → 0 and peak heat 41 → 15. Its spirits-withdrawal "hastened" deaths are
  gone, and the bot gains 55%.

**Threshold for the UI (FormulaBench "Habit"):** read `Config.Body.HabitFloor` (1.0), don't hard-code it.

- **habit = 0:** "none".
- **0 < habit < HabitFloor:** "a little; never a craving" (or "slight"). "none" is now also true of the effect,
  but "slight" tells the cozy player why their regulars like it.
- **habit ≥ HabitFloor:** the existing "they may come back" / "most will come back" / "hard to give up" (thresholds
  3 and 6 unchanged).

THE WORKS' "craving" stays `dependency > WithdrawalThreshold` (0.3), which a mild bottle now never reaches.

## 5. The 1.15× line (`b8` 79) and the searched lines

40 seeds, `top-0…39`, × adaptive mean (median of paired ratios, ended early). Adaptive: $20,875 before,
**$21,394** after. Before = pass 8's "after" column.

| Line                                                                         | Before               | After                  |
| ---------------------------------------------------------------------------- | -------------------- | ---------------------- |
| `b8` 79: 0.12 oz morphine, 83¢, three mild claims, every venture, laboratory | **1.15×** (1.15, 0%) | **1.005×** (1.003, 0%) |
| `ev1` 72: 1.1 oz laudanum escalating, 155¢, four bold claims                 | 0.31× (0.27, 10%)    | 0.21× (0.20, 18%)      |
| `ev1` 14: no narcotic, 2.5 oz spirit, 117¢, four bold claims                 | 0.36× (0.36, 0%)     | 0.37× (0.39, 0%)       |
| `ev2` 32: no narcotic, 3 oz spirit, 118¢, four bold claims                   | 0.69× (0.70, 0%)     | 0.69× (0.70, 3%)       |
| `ev2` 32 at 80¢                                                              | 1.08×                | 1.08× (1.08, 0%)       |
| `ev2` 32 at 100¢                                                             | 1.03×                | 1.03× (1.03, 0%)       |
| `ev2` 37: no narcotic, 2.7 oz spirit, 112¢                                   | 0.60× (0.60, 3%)     | 0.61× (0.63, 3%)       |
| `b8` 84: 0.13 oz morphine, 152¢, $30 ads, bold                               | 0.72× (0.75, 5%)     | 0.58× (0.60, 8%)       |
| `b8` 71: 0.63 oz laudanum, 103¢, bold, bribe                                 | 1.02× (1.02, 0%)     | 0.88× (0.89, 0%)       |
| `ev2` 33: 0.64 oz laudanum, 153¢, bold, bribe                                | 0.02× (0.02, 25%)    | 0.01× (0.00, 65%)      |
| `ev2` 95: 0.35 oz laudanum, 106¢, mild claims                                | 1.06× (1.07, 3%)     | 0.95× (0.96, 0%)       |
| `b8x` 8: 0.1 oz morphine, 3 oz spirit, 109¢                                  | 1.05× (1.08, 3%)     | 0.85× (0.90, 13%)      |

**Why `b8` 79 fell.** Its 7.2 narcotic habit is refused by the quarter of the ward that is wary, among them most
of the mothers and well-to-do it sold to. It stayed solvent (no early endings).

**Fresh searches** (`sim:optimize`'s search, 120 candidates × 4 seeds, the four search seeds of pass 8). Each
search's top three were re-scored on 40 seeds:

- Before (pass 8): `b8` 79 **1.15×**, 71 1.02×, 14 0.30×; `ev2` 95 1.06×, 56 0.97×, 62 0.94×; `b8x` 8 1.05×,
  46 0.97×, 31 0.89×; `ev1` 10 0.83×, 40 0.71×.
- After: `b8` **79 1.005×**, 57 0.93×, 71 0.88×; `ev2` 95 0.95×, 62 0.90× (23% early), 56 0.84×; `b8x` 46
  0.98×, 31 0.85×, 8 0.85×; `ev1` 10 0.68×, 97 0.67×, 40 0.54×.

**No line found is above 1.01×** (target ≤ 1.10×). The highest a repriced line reaches is the 3 oz spirit tonic
at 80¢, 1.08×.

## 6. Every bot, before → after

`pnpm sim:batch --bots shopkeeper,cautious,greedy,briber,adaptive,sloppy,heavy --runs 80 --seed b8 --fast`:

| Bot        | Median final         | Mean              | p10 / p90 after   | Early endings | Cash m12 / m24 / m48 after | Product deaths | Peak heat |
| ---------- | -------------------- | ----------------- | ----------------- | ------------- | -------------------------- | -------------- | --------- |
| shopkeeper | $15,751 → $16,222    | $15,689 → $16,186 | $15,723 / $16,678 | 0% → 0%       | $241 / $346 / $251         | 2 → 3          | 30 → 32   |
| cautious   | $6,867 → **$10,621** | $6,790 → $10,641  | $10,166 / $11,146 | 0% → 0%       | $208 / $290 / $518         | 6.5 → **0**    | 41 → 15   |
| greedy     | $11,077 → $6,503     | $11,492 → $6,679  | −$12 / $12,884    | 85% → **89%** | $103 / $299 / $454         | 9 → 7          | 100 → 100 |
| briber     | $15,356 → **$7,814** | $14,582 → $7,044  | −$16 / $13,427    | 5% → 10%      | $198 / $341 / $462         | 31 → 28        | 100 → 100 |
| adaptive   | $20,995 → $21,587    | $20,926 → $21,423 | $20,555 / $21,977 | 0% → 0%       | $211 / $254 / $705         | 2 → 1.5        | 22 → 22   |
| sloppy     | $16,028 → $15,653    | $15,440 → $15,559 | $10,847 / $19,379 | 1% → 0%       | $212 / $251 / $443         | 8 → 9          | 59 → 70   |
| heavy      | $4,512 → $14,150     | $8,411 → $10,398  | $1,250 / $16,344  | 61% → 43%     | $307 / $574 / $635         | 68 → 67.5      | 100 → 100 |

- **Honest bots within 5% or better:** adaptive +2.4%, shopkeeper +3%, sloppy −2%.
- **Cautious, the drug-free bot, gains 55%.** That is the market finding 2 asked for: the careful households
  trust its single mild claim, and it no longer makes spirit cravers who sicken in withdrawal. It is still half
  of adaptive.
- **Greedy is still a gamble and still ends early often** (89%; 83% on the 40 top seeds).
- **Briber halves.** Buying verdicts is now a losing habit: dearer each time, no quiet bought, found out more
  often. `optsil` against `opt` shows the same: silence beats the envelope.
- **Heavy ends early less often (61 → 43%)** but is no richer on the 40 top seeds (0.43 → 0.50×). It sells to
  fewer people (the wary won't touch 1 oz), so fewer strangers die.

## Notes for the desk (`godot/`, not touched)

- **Assay habit label:** see §4 (`Config.Body.HabitFloor`).
- **`callers.careful`** is a new caller source. Add to `godot/data/paper.en.json` `callerSources`, e.g.
  `"careful": "sent by the careful households"`. Until then THE TRADE prints the raw id.
- **`LedgerEntry.RefusedNarcotic`**: wary callers who would not take a narcotic bottle. Suggested Trade line:
  "Callers who smelt the poppy in it and went elsewhere: {n}." It's also good copy for a regular who leaves:
  "Mrs. Halloran says she can smell the soothing syrup in it now, and has gone back to camomile."
- **The Larger Copper prospectus** should say when the stills aren't full: if last month's bottles sold <
  `Stills × StillBottles` × 0.85, then "Your stills are not yet full: last month the house sold N of the M bottles
  they can make." (All three numbers are in `Observation`.)
- **A bought verdict** no longer lowers the heat. The ledger popup will show no "fell 2 · Inquest verdicts" line.
- **The coroner's letter** already shows the fee and the exposure odds (`BribeExposureChance`). Both now rise
  visibly with each envelope.

## Left open

- `opt` (½ oz at $1.00) still leads an honest shop at month 24 by about 10% in cash, with about 4 times the heat.
  That is the temptation the brief asked to keep. A stronger short-game brake would have to hit dear narcotic
  pricing directly (the debt hold above), which broke the long game.
- Police-court defendants' mismatched names ("Fritz Esposito") come from event and paper text, not `names.json`.

## Files

- **Content:** `config.json` (0.12.0; `body.habitFloor`, `mildDependencyCap`; `market.waryHabit`,
  `cleanAppeal`, `waryBoldness`; `walkIns.carefulPerWary`; `scrutiny.bribeExposureBase`, `bribedVerdictHeat`,
  `bribeRisePerBribe`, `bribeExposurePerBribe`), `segments.json` (`wary`), `regulars.json` (`wary`),
  `investments.json`, `names.json`, `schemas.ts`.
- **TS sim:** `systems/market.ts`, `body.ts`, `scrutiny.ts`, `customers.ts`, `core/state.ts`, `core/ledger.ts`,
  `game.ts`.
- **C# sim:** `Content/ContentTypes.cs`, `Systems/Market.cs`, `Body.cs`, `Scrutiny.cs`, `Pharmacology.cs`,
  `Core/State.cs`, `Core/Ledger.cs`.
- **Tests:** new `packages/sim/test/round9.test.ts` (12) and `sim-cs/Panacea.Sim.Tests/Round9Tests.cs` (12),
  mirrored; `m1.test.ts`, `capacity.test.ts`, `GameTests.cs` updated (version, upkeep, exposure base).
- **Fixtures:** `sim-cs/fixtures` regenerated.
- **Scratch** (`/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/bal9/`):
  - `hand.mts` + `lines.mts`: hand players;
  - `shelf.mts`: two remedies;
  - `causes.mts`: cash by cause;
  - `cand.mts`, `hunt.mts`/`hunt.sh`, `suite.sh`, `par.sh`: pass 8's method;
  - `mkbase.sh`: a HEAD copy for "before" runs.
