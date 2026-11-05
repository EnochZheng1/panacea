# Panacea · balance pass 6 · the stills grow with the shop

Content 0.5.0 → **0.6.0**. Both sims changed together (the TypeScript reference and the C# port). The parity
fixtures were regenerated (28 runs), and `dotnet test` is green (93 tests).

This pass answers the round-6 balance findings ([SUMMARY.md](SUMMARY.md),
[cold-strategy-gamer.md](cold-strategy-gamer.md) findings 1, 4 and 9, [cold-cozy-player.md](cold-cozy-player.md)).
They are:

- the two-still ceiling from month 7–8;
- the standing order sold to a bare shelf;
- the shop next door knocked through unasked;
- the coroner's fee that didn't rise;
- the still's stale prices;
- no word of when the next still comes.

All tables are 80 runs per bot, seeds `r6b-0..79`, unless marked otherwise. "Before" is commit `e6a556e` (content
0.5.0, pass 5's bots), run on the same seeds.

## First, what the old numbers hid

**The 48-bottle still was a rule of the desk, not of the sim.** The Godot still card draws a 48-bottle shelf (it
grows, up to 96, only when the stock on hand already covers more), and `sloppy` brewed in dozens up to 48. The sim itself had no limit, though. Every policy bot (`adaptive`,
`shopkeeper`, `briber`, `greedy`, `cautious`) brewed the whole month in **one batch of any size, in one still**. So
pass 5's balance numbers never met the ceiling the testers hit.

I replayed the old game with the policy bots under the desk's rule: 48 a still, using every still. In that game the
wall is permanent. The third still at 1,000 bottles is the last one, so sales stop at **144 a month for good**:
adaptive from month 36, and sloppy from month 60.

| Old game, 80 runs           | cautious | shopkeeper | adaptive    | briber  | greedy  | sloppy (already 48 a still) |
| --------------------------- | -------- | ---------- | ----------- | ------- | ------- | --------------------------- |
| As the bots played it       | $6,670   | $14,836    | **$19,563** | $10,788 | $24,460 | **$10,145**                 |
| As a player could, 48/still | $6,465   | $10,817    | **$12,547** | $11,698 | $25,951 | (same)                      |

The second thing the single batch hid was **batch-count unlocks and bad batches**. Players brew two to four batches
a month, so quality control (40 batches) reaches them at month 21–28, not month 39. A bad batch then spoils one
still's worth, not a whole month's stock. I checked it by running the new game with the copper set to 100,000
bottles, so every bot brews one batch a month again (40 runs):

| One batch vs split, early endings / median | briber                 | greedy                  | adaptive       |
| ------------------------------------------ | ---------------------- | ----------------------- | -------------- |
| One batch a month (pass 5's bots)          | 18/40 · $8,970, QC m39 | 19/40 · $25,742, QC m39 | 0/40 · $19,525 |
| Split across stills (this pass)            | 3/40 · $16,018, QC m21 | 9/40 · $26,734, QC m28  | 0/40 · $21,013 |

So the policy bots now brew the way a player does: as many stills as it takes, each up to a still's batch. Their
safety rises for that reason, not because of any change to the narcotics or the scrutiny rules.

## What changed

### 1. The stills: a ladder of capacity, with a clear goal at every rung

| Rung                                          | Before                  | After                                                                                             |
| --------------------------------------------- | ----------------------- | ------------------------------------------------------------------------------------------------- |
| A still's batch                               | 48 (the desk only)      | **48, a sim rule** (`config.stillBottles`; `BrewBatch` refuses more; `maxBottles` is capped)      |
| `second_still` (unlock)                       | 200 bottles sold        | unchanged                                                                                         |
| `third_still` (unlock)                        | 1,000 bottles sold      | **600 bottles sold**                                                                              |
| **`larger_copper`** (new venture)             | —                       | **$200**, offered once the second still is in: **every still makes 72** (`still_bottles` +24)     |
| **`fourth_still`** (new venture)              | —                       | **$300 + $3 a month** (a boy to mind the fire), offered at the third still and 2,000 bottles sold |
| Most a month: 1 / 2 / 3 / 3+copper / 4+copper | 48 / 96 / 144 (the end) | 48 / 96 / 144 / 216 / 288                                                                         |

Why this ladder:

- **The third still at 600 bottles** is a goal a person can see and reach. A tester-like shop that fills both
  stills sells about 90 a month by month 12. It gets its third still at month 12, against month 16 before (the strategy
  gamer's came at month 16 too). Every bot gets it four to seven months sooner: adaptive m18 → m14,
  sloppy m22 → m16.
- **The larger copper is the choice.** At months 8–11 the shop has two stills, both full, and about $200. It can
  wait for the third still (the still card can now say "480 of 600 bottles sold"). It can spend the $200 on the
  copper, or on the standing order and the almanac's renewal instead. Every still made bigger also makes the third
  and fourth stills bigger. So the copper is the step that carries a shop from about 140 a month to about 210, which
  is where an ambitious shop's demand sits through years 2–4.
- **The fourth still** is the long game's rung. Adaptive's and shopkeeper's demand tops out at about 250–265 a month
  with the branch and the contracts. Without it, three coppered stills (216) would be the new permanent wall. It is a
  venture with upkeep, so it is bought when the shop needs it, not by default.
- **Bare shelf is still possible.** Nothing brews itself. A shop that orders short still sells out (sloppy below),
  and a shop that outgrows its stills still hits the wall until it buys the next rung.

How much the rungs are worth (paired seeds, 40 runs):

- **Adaptive:** without the copper and the fourth still, median **$12,619** against **$21,012** with them (paired
  gain **+$8.4k**). The fourth still alone adds +$2.0k.
- **Shopkeeper:** +$5.2k for both, +$0.6k for the fourth still alone.

Neither venture is free money. Both pay only for a shop that has filled its stills (the bots buy them only after
selling ≥ 85% of what the stills can make).

**The ceiling, measured.** I counted months 6–24 that **end sold out** (nothing left on the shelf at the month's
close, after selling something). "At the wall" counts the months that sold out with every still lit full.

| Months 6–24                                        | Sold out: before → after | At the wall: before → after | Standing order filled to m24 | Sales m24 / m36 / m60 / m120      |
| -------------------------------------------------- | ------------------------ | --------------------------- | ---------------------------- | --------------------------------- |
| **sloppy**                                         | **56% → 56%**            | 6% → 4%                     | 46% → 45%                    | 91/133/144/144 → 87/138/206/234   |
| **adaptive** (old game as the bot played it)       | 14% → **17%**            | 1% → 5%                     | 84% → 80%                    | 140/195/246/249 → 129/175/248/263 |
| **adaptive** (old game at 48 a still, as a player) | 19% → **17%**            | 7% → 5%                     | 78% → 80%                    | 133/144/144/144 → 129/175/248/263 |
| Tester-like: every still full, ½ col (scratch bot) | 8% → **2%**              | 6% → 1%                     | **67% → 98%**                | 144/144/144/144 → 143/198/212/207 |

What the table shows:

- **Sloppy's bare shelves are its own ordering.** It notices a bare shelf only 40% of the time, and then raises
  its stock by only 10–25%. That share doesn't move, and it shouldn't. Only 4% of its months 6–24 are at the wall.
  The long-game wall is gone, though: it sold 144 a month from year 5 to the end, and now it sells 206–234.
- **Adaptive was at the wall from month 36 to the end** in the game a player could play. Now the wall comes at
  around month 24 (144) and month 36. Adaptive grows past it with the copper at m26.5 and the fourth still at m41.
  Production stays a constraint (5% of months 6–24 at the wall), not a ceiling.
- **The tester-like shop** fills every still with a half column running, as the strategy and cozy testers did. It
  hit the wall at 96 and then at 144. Now it climbs past both: the standing order m10, the third still m12, the
  copper m17, 2% of months bare. Its standing order is filled 98% of the time.

**The ad and the price are choices again.** Bottles sold in months 13–24 by the tester-like shop, by ad size (40
runs):

| Ad a month (desk tiers)                  | none  | ¼ col $3 | ½ col $6 | full col $11 | front page $20 |
| ---------------------------------------- | ----- | -------- | -------- | ------------ | -------------- |
| Before (two stills, 3rd at 1,000)        | 1,115 | 1,415    | 1,416    | 1,452        | 1,467          |
| After                                    | 1,029 | 1,359    | 1,451    | **1,675**    | 1,616          |
| Share of months 6–24 at the wall, before | 0%    | 1%       | 7%       | 14%          | 12%            |

Before, going from a half column to a full one bought 36 more bottles in a year (+2.5%). The strategy gamer's
"cutting $17 of advertising cost 22 Gazette callers and no sales". Now the same step buys 223 more (+15%).

**The bots.**

- **Policy bots** brew across every free still, each up to `stillBottles`. They buy `shop_next_door`,
  `larger_copper` and `fourth_still` (the capacity ventures only after a month that sold ≥ 85% of capacity). Greedy
  takes the shop next door, for its branch.
- **`sloppy`** reads the batch from `stillBottles` instead of a hard-coded 48. It takes up a capacity venture only
  within three months of having lit every still full and still sold out: a person sees the wall before buying a
  bigger copper. Its other rules are unchanged.
- **The exploit hunter's** invest sets include the new ventures.
- **The web prototype's** still dial (`packages/game`) stops at `stillBottles`.

### 2. The standing order and the bare shelf: moot by the fix, plus a warning in the API

I didn't add a capacity requirement. Tying the standing order to the third still (600 bottles) would push sloppy's
second venture from m13 to m16, against the brief.

Instead, the ladder fixes the case the cozy tester met. A shop that fills its stills now has spare capacity soon
after the order is offered: the order at 400 bottles, the third still at 600 and the copper any time. Its order is
filled 98% of the time to month 24 (67% before).

What's left is a shop whose shelf is bare from under-ordering (sloppy fills 45%). That shop now gets numbers for a
warning:

- every ledger entry records `shelfEndBottles`, the bottles left when the month closed;
- the observation counts `shelfBareMonths` (of the last three months, how many ended bare).

The prospectus card could say "he takes what is left on the shelf; yours was bare at the end of 3 of the last 3
months".

### 3. The shop next door: a venture the player takes up (`shop_next_door`)

- **No event, a venture.** It is in the prospectus from the first month, with its progress line ("18 of 25
  regulars"). It costs **$0 and $5 a month upkeep**, the same rise as before, now shown on the card and in the books
  as upkeep with detail `shop_next_door`. It gives **+20 regulars of room**.
- **The unlock `larger_shop` stays**, as the marker that the wall is down. Its requirement is now the flag
  `knocked_through`, set by the venture, and its effects moved to the venture. So the branch's requirement and the
  paper's knock-through item still work. The item now fires at the month-end after the player took up the venture.
- **Nothing happens unless the player takes it up**, and the rent stays at $3.
- **Bot timing:** sloppy takes it at m18 (the old automatic unlock fired at m19). Branch timing moves by 0–6 months
  (sloppy m41 → m43.5; adaptive m33 → m39). The bots now also spend on the copper, which leaves less saved toward
  the branch's $600.

### 4. The coroner's fee rises with every envelope (both sims)

The fee was `round5($15 × (1 + 0.1 × bribes paid) × (1 + heat/100))`. The heat falls after a bought verdict, so two
envelopes a month apart could both be $30, which is what the story reader saw. It is now **never less than the last
fee paid plus `bribeMinRiseCents` ($5)**. The state keeps `scrutiny.lastBribeCents`. A test pays three envelopes
while the heat falls by 30 points between them: $30 → $35 → $40. The floor binds only when the talk has died down
since the last envelope. Briber pays a median of 36 envelopes (39 before).

### 5. The still's money lines use today's prices

`formulas[].unitCostCents` in observe used the base prices from content. It is now
**`formulaUnitCost(state, components, process)`**, which uses each ingredient's present price (bulk discount,
supplier surcharges, the laboratory) plus bottling. That is exactly what an order charges; the stock valuation uses
the same function. The still card's "takes $X of stock" (`n × UnitCostCents − bottles`) follows it. The bots'
cash-limit check uses it too.

### 6. What the next still waits for (machine API, both sims)

`Observation.nextCapacity` lists every rung not yet had, in content order (the unlocks, then the ventures). Each
rung has the same `ConditionProgress` lines as the prospectus, and a venture's list ends with its `cost`. The first
rung with `stills > 0` is the next still.

Example: a game with the second still in and 450 bottles sold has `nextCapacity[0]` = `third_still`, with
`[unlocked second_still ✓, bottlesSold 450/600 ✗]`. Then come `larger_copper`
`[unlocked second_still ✓, cost have/20000]` and `fourth_still`
`[unlocked third_still ✗, unlocked second_still via third_still ✓, bottlesSold 450/600 via third_still ✗,
bottlesSold 450/2000 ✗, cost …]`.

Also new: `Observation.stills` (stills owned) and `Observation.stillBottles` (the most a still makes in a batch,
every still alike).

## Before and after (80 runs, `--seed r6b`)

| Bot        | Median final, before → after | Mean              | p10 / p90 after   | Early endings (bankrupt + shut) | Cash m12    | Cash m24    | Cash m48      | Product deaths | Bottles sold    |
| ---------- | ---------------------------- | ----------------- | ----------------- | ------------------------------- | ----------- | ----------- | ------------- | -------------- | --------------- |
| cautious   | $6,670 → **$7,040**          | $6,631 → $6,943   | $6,414 / $7,439   | 0% → 0%                         | $190 → $192 | $225 → $221 | $311 → $328   | 6 → 7          | 37,208 → 41,356 |
| shopkeeper | $14,836 → **$15,938**        | $14,140 → $15,894 | $15,395 / $16,398 | 4% → 0%                         | $243 → $245 | $345 → $347 | $387 → $268   | 10 → 3         | 50,116 → 55,861 |
| adaptive   | $19,563 → **$20,958**        | $18,624 → $20,942 | $20,524 / $21,367 | 0% → 0%                         | $226 → $223 | $234 → $231 | $1,260 → $643 | 6 → 1          | 55,074 → 61,410 |
| briber     | $10,788 → **$15,588**        | $9,876 → $14,194  | $12,225 / $17,040 | 49% → 8%                        | $235 → $221 | $509 → $363 | $1,193 → $631 | 41 → 30        | 39,634 → 45,154 |
| greedy     | $24,460 → **$26,008**        | $19,226 → $22,063 | $6,930 / $28,483  | 50% → 30%                       | $115 → $115 | $315 → $331 | $999 → $1,046 | 7.5 → 5        | 27,310 → 29,048 |
| sloppy     | $10,145 → **$15,982**        | $9,570 → $15,793  | $11,938 / $19,668 | 4% → 0%                         | $225 → $225 | $333 → $319 | $540 → $466   | 21 → 8.5       | 31,713 → 52,356 |

- **Adaptive: $21.0k**, at the top of the $15–21k band. The game a player could actually play gains much more:
  $12.5k → $21.0k.
- **Cautious and shopkeeper** stay viable and gain 5–7%. Shopkeeper's cash at m48 dips because it buys the copper
  at m36 and saves for the branch.
- **Greedy:** a median of $26.0k, p10 $6.9k, p90 **$28.5k (1.36× adaptive's median)**, and 30% early endings. It
  is still the high-variance line with a jackpot for its survivors. It ends early less often (50% → 30%) for the
  batch reason above.
- **Briber's** early endings fall from 49% to 8%, for the same reason. The one-batch experiment puts it back at
  18/40. If the heavy lines should be riskier again, the lever is quality control's batch count (40), not the
  stills. I left it alone, because sloppy, the bot that already brewed like a person, gets QC at m27 both before
  and after.
- **Sloppy: $10.1k → $16.0k.** The rise is the long game. Before, its sales stopped at 144 a month from year 5. The
  empty shelf also cost it its dependent regulars (product deaths 21 → 8.5, peak heat 97 → 55). Its first two years
  match the hand band as before: cash m12 $225, m24 $319.

## When each bot takes up its ventures (share of runs · median month first bought)

| Venture            | cautious  | shopkeeper | adaptive  | briber                | greedy          | sloppy                  |
| ------------------ | --------- | ---------- | --------- | --------------------- | --------------- | ----------------------- |
| almanac            | m3        | m3         | m3        | m3                    | —               | 85% · m3                |
| druggists_order    | m14       | m13        | m12       | 96% · m11             | —               | m13                     |
| shop_next_door     | — → m23   | — → m20    | — → m13   | — → m11               | — → 96% · m19   | — → 92% · m18           |
| larger_copper      | — → m57   | — → m36    | — → m26.5 | — → 99% · m22         | —               | — → 94% · m42           |
| wholesale_contract | m34       | m24        | m22       | 64% · m20 → 55% · m22 | —               | m26                     |
| branch_shop        | m65 → m69 | m42 → m45  | m33 → m39 | 96% · m30 → m36       | 96% · m39 → m38 | 91% · m41 → 92% · m43.5 |
| fourth_still       | — → m92   | — → m53    | — → m41   | — → 92% · m46         | —               | — → 94% · m61           |
| laboratory         | —         | —          | —         | —                     | 96% · m30       | —                       |

`sloppy`'s **n-th venture**, before → after:

- First: m3 → m3.
- Second: m13 → m13 (the standing order, as in pass 5).
- Third: m26 → **m18** (the shop next door).
- Fourth: m41 → **m26** (the contract).
- Fifth: none → m38.

The standing order (m13) and the contract (m26) come exactly when they did. The branch comes 2.5 months later,
because sloppy now buys its copper first, once its stills are full.

Unlocks, before → after:

| Unlock          | cautious  | shopkeeper | adaptive  | briber     | greedy    | sloppy    |
| --------------- | --------- | ---------- | --------- | ---------- | --------- | --------- |
| second_still    | m8        | m7         | m7        | m6         | m11       | m7        |
| third_still     | m23 → m17 | m21 → m15  | m18 → m14 | m16 → m12  | m27 → m20 | m22 → m16 |
| larger_shop     | m23 → m23 | m19 → m20  | m11 → m13 | m9.5 → m11 | m19       | m19 → m18 |
| quality_control | m39 → m26 | m39 → m24  | m39 → m22 | m39 → m21  | m39 → m28 | m27       |
| bulk_supplier   | m24 → m19 | m24 → m17  | m24 → m16 | m24 → m15  | m24 → m20 | m20       |

## The exploit hunter

`pnpm sim:optimize --candidates 120 --seeds 4 --seed r6b`:

- **Best line:** a trace of coca (0.04 oz), 2.7 oz alcohol and valerian, at **86c**, with a $3 ad, the four bold
  claims, testimony at inquests, escalating, and the almanac, the shop next door and the branch. It averages
  **$18,304**, which is **0.87× adaptive's mean** ($20,942). The same search before found $19,023, 1.02× of
  $18,624. The correlation between wealth and product deaths is 0.03 (0.18 before).
- **Targeted, 80 runs** (adaptive's mean is $20,942):

  | Adaptive variant | Mean    | × adaptive |
  | ---------------- | ------- | ---------- |
  | at 80c           | $22,228 | 1.06×      |
  | at 90c           | $22,665 | **1.08×**  |
  | at 100c          | $21,317 | 1.02×      |
  | at 110c          | $18,074 | 0.86×      |
  | with a $15 ad    | $19,486 | 0.93×      |

  The round-5 tycoon's line (90c) was 1.08× before this pass too ($20,025 against $18,624). No line passes 1.15×.

## Plumbing

**TypeScript** (`packages/sim`):

- `stillBottles(state, content)` and `formulaUnitCost(state, components, processId, content)` in `systems/lab.ts`.
- `investmentProgress(state, inv, content)` in `systems/investments.ts`, shared by the prospectus and the ladder.
- `capacitySteps(state, content)` and the `CapacityStep` type in `systems/progression.ts`.
- `Observation.stills`, `stillBottles`, `nextCapacity` and `shelfBareMonths`.
- `LedgerEntry.shelfEndBottles` and `scrutiny.lastBribeCents`.
- Config `stillBottles` (48) and `scrutiny.bribeMinRiseCents` (500).

**C#** (`Panacea.Sim`), the same names:

```csharp
// Systems/Progression.cs
public sealed record CapacityStep(
    string Id,          // the unlock or venture: "third_still", "larger_copper", "fourth_still", …
    string Source,      // "unlock" or "investment"
    int Stills,         // stills it adds
    int StillBottles,   // bottles it adds to every still's batch
    IReadOnlyList<ConditionProgress> Progress); // as a venture's prospectus; a venture's ends with "cost"
Progression.CapacitySteps(GameState, ContentPack) → List<CapacityStep>

// Machine.cs, Observation (new members)
int Stills                                  // stills owned
int StillBottles                            // the most a still makes in a batch
IReadOnlyList<CapacityStep> NextCapacity    // rungs not yet had; first with Stills > 0 is the next still
int ShelfBareMonths                         // of the last three closed months, how many ended bare
// ObservedFormula.UnitCostCents is now at today's prices

// Elsewhere
LedgerEntry.ShelfEndBottles                 // bottles left on the shelf at the month's close
ScrutinyState.LastBribeCents
Config.StillBottles; ScrutinyConfig.BribeMinRiseCents
Lab.StillBottles(state, content); Lab.FormulaUnitCost(state, components, processId, content)
Investments.Progress(state, inv, content)   // a venture's progress lines (was inline in Machine)
// BrewBatch over the copper fails with "a still makes at most {n} bottles a batch".
```

## Notes for the desk (`godot/`, not touched)

- **The still card.**
  - `StillBench` clamps the batch to 96 and draws a 48–96 shelf. It should use `Observation.StillBottles` (48, or
    72 with the copper). `MaxBrewable` is already capped by the sim.
  - "The third still: 450 of 600 bottles sold" is `NextCapacity.First(c => c.Stills > 0)`. The copper (`Kind
"cost"` last) can read "a larger copper: $180 of the $200".
- **The formula book's preview.** `SimGame.Preview` takes `UnitCostCents` from `Formulas.Analyze`, which is still
  at base prices. Use `Lab.FormulaUnitCost(_state, comps, processId, _content)` so the preview agrees with the
  formula row and the still.
- **The prospectus.**
  - The three new ventures need their cards. Their strings are in `en.json`:
    - `investment.shop_next_door.name` / `.description`: "The Shop Next Door". Upkeep 500 is the rent rise.
    - `investment.larger_copper.name` / `.description`: "A Larger Copper".
    - `investment.fourth_still.name` / `.description`: "A Fourth Still".
  - The branch's progress is now `[unlocked larger_shop, flag knocked_through (via larger_shop), cost]`. The flag
    line needs a phrase ("waits on the shop next door").
  - The changed strings are `unlock.second_still.description`, `unlock.third_still.description` (it names 600
    bottles) and `unlock.larger_shop.description`.
- **The paper.**
  - The `larger_shop` unlock item ("…knocked through… rent up five dollars a month") still fires, now at the
    month-end after the venture is taken up. It reads as news of the player's own choice. The venture's own
    `investment … bought` item fires the same month, so print one or the other.
  - There is **no new event**, so no letterhead is needed.
- **The coroner's letter.** "His fee rises with every envelope" is now true: each fee is at least $5 more than the
  last.
- **The standing order's warning.** `ShelfBareMonths` gives the count. Suggested copy: "He takes what is left on
  the shelf after your regulars; yours was bare at the end of 3 of the last 3 months."
