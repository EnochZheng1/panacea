# Panacea · balance pass 7 · the ward remembers, and poison pays (for a while)

Content 0.6.0 → **0.7.0**. Both sims changed together (the TypeScript reference and the C# port). The parity
fixtures were regenerated (32 runs, now including the new `heavy` bot and a per-turn hash of the new `ward`
section), and `dotnet test` is green.

This pass answers the round-7 balance findings ([SUMMARY.md](SUMMARY.md) items 3, 6 and 7;
[cold-satire-fan.md](cold-satire-fan.md) findings 1–2; [cold-board-gamer.md](cold-board-gamer.md) F2;
[cold-tycoon-fan.md](cold-tycoon-fan.md) finding 7; [cold-non-gamer.md](cold-non-gamer.md) finding 2):

- the ward forgets: three months after fourteen deaths, new customers buy the bottle three juries named;
- poison doesn't pay: nine laudanum months took the satire tester's cash from $125 to $113;
- the paper guesses which remedy the post and the druggists took;
- customers die with nothing to say whether the remedy did it.

All tables are 80 runs per bot, seeds `r7b-0..79`, unless marked otherwise. "Before" is commit `ed67ec8`
(content 0.6.0), run on the same seeds with the same bots. The new `heavy` bot (below) was run against the old
sim as well.

## First, why the dark path never paid

I traced the satire tester's line (1 oz of laudanum rising to 1.5, 70c, the teething and rheumatism claims) as a
bot, `heavy`, and compared it with an honest twin that orders, advertises and invests the same way. Three things
kept the strong bottle from paying:

1. **The stills are the limit early, not demand.** By month 8 both shops sell every bottle two stills make. A
   strong bottle brings more custom, but extra custom on a full shelf only changes _who_ buys. So the only way
   the strong bottle can pay more is per bottle.
2. **Per bottle it paid less.** An ounce and a half of laudanum costs 22c, against 4c for the twin's quarter
   ounce. At the same 70c the strong bottle's margin was about 46c against the twin's 55c.
3. **A habit could never spend more than its means.** A hooked regular wanted two bottles and could afford
   about 2.4, so the habit brought in roughly what a delighted honest regular does.

Months 7–18 of a shop that turns dark at month 7 (old sim): revenue +$14/month over the twin, ingredients +$8,
so **+$5.6/month operating**, all of it spent on ventures. Its wealth at month 24 was _lower_ than the twin's
($639 against $766). The satire tester was right: the poison cost nothing to give up, because it never paid.

## What changed

### 1. The ward remembers (`state.ward`, both sims)

A new memory of harm, **0–100**, kept apart from the heat. The heat is the authorities' suspicion: it flares and
"burns itself out" within a season (it decays faster the hotter it is). The memory is the neighbours'.

**What adds to it** (`config.ward`, at month-end, from the month's news):

| Harm                                                     | Memory |
| -------------------------------------------------------- | ------ |
| A regular's death the remedy caused and that _looked_ it | +6     |
| An infant's                                              | +12    |
| A stranger found with the bottle (walk-in or post)       | +3     |
| A death the remedy hastened but that looked natural      | +2     |
| …and if it came of a mis-measured (bad) batch            | +4     |
| **An inquest whose jury named the remedy**               | +14    |
| …and if the verdict came of a bribe found out            | +8     |
| The Board of Health carried the bottles away             | +6     |
| A natural death the remedy had nothing to do with        | 0      |

"Looked it" means the reported cause (§4) was an overdose or a wasting. So a verdict weighs more than two
unexplained deaths, as asked.

**How it fades:** each month-end it falls to 88% of itself, less 0.2. From 100 it is at 45 after six months, 20
after a year and 8 after eighteen months, and it is gone after about two and a half years. Its half-life is six
months.

**What it costs** (scaling with the memory, the full amount at 100):

- strangers at the counter buy **60% less often**;
- strangers who do buy come back as regulars **80% less often**;
- each regular who is _not_ hooked (dependency under the withdrawal threshold) leaves with a chance of **10% a
  month**. Their `customer_left` reason is **`afraid`** ("heard the talk and took their custom elsewhere"). The
  hooked stay: they have to.

It does not touch the heat, the inquests or the reputation. The paper can say THE WARD REMEMBERS without the
shop being any nearer the magistrate.

**What a bad month costs** (paired runs, `sloppy` at month 25, 60 games, the same game with and without the
memory). A 14-death month with three verdicts puts the memory at 100:

| Months after | Memory | Walk-in bottles lost (of) | New regulars lost | Regulars frightened off | Regulars fewer | Sales lost |
| ------------ | ------ | ------------------------- | ----------------- | ----------------------- | -------------- | ---------- |
| +1           | 88     | 7.0 (of 13)               | 1.2               | 0.8                     | 2.0            | $4.62      |
| +3           | 70     | 6.5 (of 15)               | 1.1               | 0.5                     | 5.3            | $5.55      |
| +6           | 49     | 4.5 (of 15)               | 0.6               | 0.2                     | 6.9            | $10.62     |
| +9           | 34     | 2.1 (of 15)               | —                 | 0.2                     | 7.6            | $4.20      |
| +12          | 25     | 2.1 (of 17)               | —                 | 0                       | 5.8            | $8.38      |
| +18          | 13     | 0.8 (of 16)               | —                 | 0.1                     | 2.8            | $7.00      |

- **The first quarter:** half the strangers stop buying, and a regular a month is lost or not gained.
- **The first year:** the shop runs six or seven regulars short, and its takings are down about **$80** (a
  tenth of a sloppy shop's year).
- **After a year and a half:** the shop is still three regulars down.

A milder bad month (two deaths and a verdict, memory 30) costs about two regulars for most of a year, and $17 of
takings.

**Where it sits in play** (40 runs; median peak, and the median at months 12 and 24):

| Bot        | Peak memory | At m12 | At m24 | Regulars frightened off (game) |
| ---------- | ----------- | ------ | ------ | ------------------------------ |
| cautious   | 11          | 0      | 0      | 4                              |
| shopkeeper | 14          | 0      | 0      | 0                              |
| adaptive   | 6           | 0      | 0      | 0                              |
| sloppy     | 24          | 0      | 0      | 2                              |
| briber     | 73          | 15     | 21     | 7                              |
| greedy     | 34          | 0      | 3      | 1                              |
| **heavy**  | **100**     | **65** | **50** | 7.5                            |

The honest shops are barely touched: a stray death a decade weighs 6 and is forgotten by the next one. A
laudanum shop at 1 oz is remembered all the time.

One side effect is worth knowing about. **The memory lowers the dark shop's death rate**, because the regulars
it frightens away and the strangers it puts off are exactly the untolerant ones who overdose. Over months 1–36
the `heavy` bot's regular deaths fell from 12.1 to 7.6 a game, and its infant deaths from 4.9 to 3.6. So it is
shut down less often (73% → 55%), and it ends smaller and poorer (below).

**For the paper** (`Observation.wardMemory`, and `wardRemembers` over the last 12 months, this one included):

```
wardRemembers: { months: 12, deaths, infantDeaths, strangerDeaths, verdicts, seizures }
```

`deaths` counts only the deaths the ward lays at the bottle's door (reported as an overdose or a wasting), so the
counts never leak the hidden truth. `LedgerEntry.wardMemoryStart` and `wardMemoryEnd` give the month's change.

### 2. Poison tempts (`config.market`, `config.walkIns`)

Three rules, all aimed at the strong bottle rather than the mild one:

1. **The craving pays over the price** (`cravingPremiumCents`). A regular who craves a strong bottle
   (dependency over the withdrawal threshold) pays over its shelf price, up to `cravingPremium` (100%) ×
   dependency of it, "to have a bottle kept back".
   - **Strong bottles only:** it scales from nothing at a bottle's habit of 4.5 to the whole amount at 7.5. An
     ounce of laudanum and 2 oz of spirits is 6.8, three-quarters of the way. A mild tonic (a quarter ounce:
     habit 2.3) never earns a cent of it, and none of the honest bots did.
   - **Only while the money lasts:** a regular who owes more than three months' means stops paying it. So it is
     the windfall of a new habit, not an income.
   - **Its own ledger line:** the cash delta is cause `sales.craving` ("Paid over the price by regulars who crave
     it"), detail the formula id, and `SaleRecord.cravingPremiumCents` holds it. The counter still took
     `shopBottles × price`, so the board gamer's check still holds, and the premium is plainly the money the
     dark path made.
2. **A habit wants more as the dose stops working** (`bottlesWanted`). The craving's extra bottles grow with a
   regular's tolerance past 2 ("two spoons and then four"): dependency × (2.25 + 0.25 × (tolerance − 2)), at
   most three extra. A mild tonic's regulars never pass a tolerance of 1.7, even after twenty years; a strong
   bottle's reach 3–5 within a year.
3. **Word of a strong bottle** (`wordOfStrength`) brings callers of its own: `callers.word` = ⌊3 × clamp(habit −
   3.5, 0, 8)⌋ a month, by the most habit-forming bottle on the shelf. About 9 for an ounce of laudanum and 18
   for an ounce and a half; none for a mild tonic. They are strangers like any walk-in: the memory puts them
   off, and a strong enough bottle kills some of them.

The ruin comes later: a regular is ruined (one bottle at most, a family that complains) once they owe **five**
months' means (`ruinMonths`), instead of one. The habit runs up a long slate before it comes to that.

**The temptation, measured.** Median cash at the end of months 6, 12, 18 and 24 (and wealth: cash + ventures
bought + stock at cost, which is fairer to shops that spend on ventures).

- `heavy` is the satire tester's line from the first month.
- `heavy from m7` plays the honest twin for six months and turns at month 7, as the tester did.
- `honest twin` is the same bot with a quarter ounce and two mild claims.

| Line                     | m6 before → after | m12 before → after | m18 before → after | m24 before → after | Wealth m18 before → after | Final median before → after | Ended early |
| ------------------------ | ----------------- | ------------------ | ------------------ | ------------------ | ------------------------- | --------------------------- | ----------- |
| **heavy**                | $148 → **$182**   | $252 → **$326**    | $362 → **$461**    | $510 → **$624**    | $452 → **$606**           | $676 → $12,326              | 73% → 55%   |
| **heavy from m7**        | $153 → $154       | $218 → **$237**    | $315 → **$382**    | $433 → **$583**    | $455 → **$617**           | $525 → $11,129              | 84% → 56%   |
| honest twin              | $153 → $154       | $230 → $228        | $305 → $304        | $252 → $242        | $487 → $486               | $21,338 → $21,252           | 0%          |
| sloppy                   | $154 → $155       | $220 → $218        | $207 → $201        | $309 → $320        | $377 → $374               | $15,678 → $15,602           | 0%          |
| adaptive                 | $132 → $132       | $219 → $219        | $240 → $241        | $253 → $247        | $421 → $421               | $20,835 → $20,886           | 0%          |
| briber (0.6 oz, bribes)  | $153 → $162       | $217 → $225        | $338 → $346        | $362 → $345        | $539 → $535               | $15,086 → $16,118           | 8% → 3%     |
| greedy (morphine, $1.50) | $147 → $171       | $123 → $266        | $191 → $449        | $355 → $558        | $205 → $496               | $25,541 → $28,126           | 33% → 39%   |

- **The heavy shop now leads from month 6.** Against sloppy and adaptive it holds 1.2× and 1.4× their cash at
  m6, 1.5× at m12, and **2.3× and 1.9× at m18**. At its peak in the first two years it held a median **$675**, against $356 for sloppy and $414 for
  adaptive. That is the "$300–500 in the bank before the inquests" the satire tester asked for.
- **The turn at month 7 now pays.** Before, it left the shop $130 _poorer_ at month 24 than staying honest. Now
  it is **+$130 in wealth at m18** and +$65 at m24. In cash it is 2.4× the twin at m24 ($583 against $242),
  because the dark shop cannot get the druggists' contract and has nowhere to spend its money.
- **Then it catches up:**
  - the dark shops end early in more than half their games (55% and 56%);
  - their median final cash is $11–12k, against $21k for the twin;
  - their best games (p90 $18.5k) don't reach the honest twin's worst (p10 $20.7k).
- **Reform now costs something.** The premium stops the month the laudanum comes out, and the extra bottles
  within a month or two as the tolerance fades. The shaking regulars stay.
- The craving premium is **14% of the heavy shop's takings** in its first two years ($169 a game), and 17% of
  greedy's ($323).

### 3. Sales by remedy and by where they went (`SaleRecord`, both sims)

Every formula's sales record now carries its split. The four sources sum to `bottles` and `revenueCents`, and an
invariant checks it in both sims:

```
SaleRecord {
  formulaId, bottles, revenueCents,
  shopBottles, shopRevenueCents,             // regulars at the counter (premium included)
  walkInBottles, walkInRevenueCents,         // strangers at the counter
  wholesaleBottles, wholesaleRevenueCents,   // the druggists under contract, at the wholesale price
  postBottles, postRevenueCents,             // orders by post (the almanac), at the shelf price
  cravingPremiumCents,                       // of shopRevenueCents, paid over the price
}
```

It is in `month.sales`, in every `LedgerEntry.sales`, and so in the report `endTurn` returns and in
`Observation.lastMonth`. The paper's per-remedy TRADE counts can come straight from it. In the board gamer's
November that is "Panacea: 57 at the counter; Valerian Bitters: 2 at the counter, 9 by post". `month.wholesale`
and `month.post` keep their totals (`ordered`, `delivered`, `revenueCents`).

### 4. Cause of death, as the paper could know it (`DeathRecord.reportedCause`, both sims)

Every death now records the customer's **`ailment`** (null for a stranger by post) and a **`reportedCause`**. The
coroner's register and the street would give one of these:

| `reportedCause` | When                                                                                                                             | Suggested copy                                           |
| --------------- | -------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| `overdose`      | every overdose (truth `overdose`)                                                                                                | "of an overdose"; "found insensible after a dose"        |
| `wasting`       | a slow poisoning by a load of metals (truth `poisoning`)                                                                         | "wasted away"; "of a wasting"                            |
| `ailment`       | the complaint ran its course (truth `disease`), or a natural death of a deadly complaint (the consumption, an infant's teething) | "of the consumption she came in with"; "of the teething" |
| `old_age`       | a natural death of an elderly customer                                                                                           | "of old age"; "peacefully, in the ordinary way"          |
| `natural`       | any other natural death                                                                                                          | "suddenly, of a fever going about"                       |

It never reveals `productImplicated`. A natural death the remedy hastened (withdrawal, a weakened body, a
metal load under the wasting point) reads exactly like one it didn't, so the deliberate ambiguity stays:

- **A harmless bottle:** the non-gamer's case reads plainly natural ("of old age"). Nothing on a harmless bottle
  can be reported as an overdose or a wasting.
- **A strong bottle:** an overdose says so, and is suspicious, as it should be.
- **A hastened death:** if the remedy hastened a death that looked natural, only the inquest, the heat and the
  ward's small +2 know.

`Observation.deaths[]` carries `ailment` and `reportedCause`. Suggested memorial line: "Died of old age. The doctor did not
mention your bottle." Add the verdict when there was an inquest.

### 5. The still ladder

Untouched: `stillBottles` 48, the copper +24, the third still at 600 bottles, the fourth at 2,000.

## Before and after (80 runs, `--seed r7b`)

| Bot        | Median final, before → after | Mean              | p10 / p90 after   | Early endings | Cash m6 / m12 / m24 after | Product deaths | Bottles sold    |
| ---------- | ---------------------------- | ----------------- | ----------------- | ------------- | ------------------------- | -------------- | --------------- |
| cautious   | $7,041 → **$7,057**          | $6,994 → $7,054   | $6,621 / $7,430   | 0% → 0%       | $132 / $194 / $229        | 7 → 7          | 41,569 → 41,641 |
| shopkeeper | $15,905 → **$15,814**        | $15,855 → $15,813 | $15,259 / $16,276 | 0% → 0%       | $151 / $243 / $342        | 3 → 3          | 55,749 → 55,514 |
| adaptive   | $20,835 → **$20,886**        | $20,872 → $20,864 | $20,410 / $21,286 | 0% → 0%       | $132 / $219 / $247        | 2 → 2          | 61,200 → 61,266 |
| briber     | $15,086 → **$16,118**        | $13,960 → $15,379 | $11,872 / $18,193 | 8% → 3%       | $162 / $225 / $345        | 30.5 → 29.5    | 45,609 → 45,856 |
| greedy     | $25,541 → **$28,126**        | $21,272 → $23,203 | $5,671 / $31,066  | 33% → 39%     | $171 / $266 / $558        | 6 → 6          | 28,668 → 29,757 |
| sloppy     | $15,678 → **$15,602**        | $15,782 → $15,675 | $10,823 / $19,440 | 0% → 0%       | $155 / $218 / $320        | 12.5 → 11      | 54,172 → 53,336 |
| heavy      | $676 → **$12,326**           | $5,652 → $9,964   | $730 / $18,540    | 73% → 55%     | $182 / $326 / $624        | 41.5 → 73      | 5,037 → 34,858  |

- **The honest lines are unchanged** to within 1%. Adaptive is at **$20.9k** (band $15–21k), sloppy at $15.6k
  and shopkeeper at $15.8k. None of them sells a bottle strong enough to earn the premium or the word. Their
  regulars never build the tolerance for the extra bottles, and the ward barely knows them.
- **Greedy stays the gamble with a jackpot.** 39% of its games end early (33% before), its p10 is $5.7k and its
  p90 **$31.1k** (1.49× adaptive's median). The premium makes its early years much richer (m12 $123 → $266) and
  its survivors richer still. Its mean ($23.2k) is 1.11× adaptive's.
- **Briber** gains a little (median +$1k), from the later ruin and two callers a month by word. Its 0.6 oz
  bottle (habit 4.4) is just under the premium's threshold.
- **Heavy** survives more often (see §1's side effect) and lives longer, so it kills more people in all (41.5 →
  73 product deaths) and ends with a median of $12.3k: well short of every honest line but cautious.

## The exploit hunter

`pnpm sim:optimize --candidates 120 --seeds 4 --seed r7b`:

- **Best line:** 0.41 oz of laudanum (escalating), 2.1 oz spirits, capsicum and gentian, at **$1.02**, a $8 ad, no
  claims, silent at inquests, every venture including the laboratory. It averages **$22,348**, which is **1.07×
  adaptive's mean** ($20,864). The correlation between wealth and product deaths is **0.02**: the richest
  strategies don't kill more.
- **Targeted, 40 runs** (adaptive's mean is $20,864):

  | Variant                                         | Mean    | × adaptive |
  | ----------------------------------------------- | ------- | ---------- |
  | adaptive at 80c                                 | $22,239 | 1.07×      |
  | adaptive at 90c                                 | $22,978 | **1.10×**  |
  | adaptive at 100c                                | $21,325 | 1.02×      |
  | adaptive at 110c                                | $17,974 | 0.86×      |
  | adaptive with 0.6 oz, $1.00, bribes, no cutting | $23,087 | **1.11×**  |
  | heavy at $1.10                                  | $16,470 | 0.79×      |

  The 0.6 oz line was 1.07× before this pass. With the premium starting at a habit of 2.5 it reached 1.24×, which
  is why the premium starts at 4.5 and the word at 3.5. No line passes 1.15×.

## Plumbing

**TypeScript** (`packages/sim`):

- **New system** `systems/ward.ts`:
  - `wardPhase`, a month-end phase after the body;
  - `reportedCauseOf`, `blamedOnTheBottle` and `deathWeight`;
  - `memoryLoss` and `frightenedAway`;
  - `wardRemembers` and the `WardRemembers` type.
- **State:**
  - `GameState.ward { memory, peakMemory }`;
  - `MonthInProgress.startWardMemory`, and `LedgerEntry.wardMemoryStart` and `wardMemoryEnd`;
  - `DeathRecord.ailment` and `reportedCause`, with the type `ReportedCause`;
  - `LeaveReason` gains `'afraid'`;
  - the `SaleRecord` splits (§3), with the type `SaleSource`.
- **Market** (`systems/market.ts`): `bottlesWanted`, `cravingPremiumCents`, `recordSale` and `wordOfStrength`.
  `isRuined` now uses `ruinMonths`. Cause `sales.craving` is new, and caller source `word`.
- **Observe:**
  - `Observation.wardMemory` and `wardRemembers`;
  - `ObservedDeath.ailment` and `reportedCause`.
- **Invariants:** the sales splits add up, the craving premium is possible, and the memory is in 0–100 and no
  higher than its peak.
- **Config:**
  - `walkIns.wordPerHabit` (3), `wordHabitFloor` (3.5) and `wordHabitCap` (8);
  - `market.bottlesPerTolerance` (0.25), `toleranceFloor` (2) and `habitBottlesMax` (3);
  - `market.cravingPremium` (1), `premiumDebtMonths` (3), `premiumHabitFloor` (4.5) and `premiumHabitSpan` (3);
  - `market.ruinMonths` (5);
  - a new `ward` section (the table in §1, `memoryDecay` 0.12, `memoryDecayFlat` 0.2, `rememberMonths` 12).
- **Strings:** `cause.sales.craving`.
- **Tests:** a new `test/ward.test.ts` covers the reported causes, the memory's weights and decay, the walk-ins it
  puts off, the sales splits, the premium and its ledger line, and word of strength.

**Playtest** (`packages/playtest`):

- the new `heavy` preset;
- the parity fixtures add `heavy` to their bots and hash the `ward` section every turn.

**C#** (`Panacea.Sim`), the same names in PascalCase:

```csharp
// Core/State.cs
public sealed class WardState { double Memory; double PeakMemory; }   // GameState.Ward
LeaveReasons.Afraid = "afraid"
public static class ReportedCauses { Overdose="overdose", Wasting="wasting", Ailment="ailment", OldAge="old_age", Natural="natural" }
DeathRecord.Ailment (string?), DeathRecord.ReportedCause (string, required)
SaleRecord.ShopBottles, ShopRevenueCents, WalkInBottles, WalkInRevenueCents,
          WholesaleBottles, WholesaleRevenueCents, PostBottles, PostRevenueCents, CravingPremiumCents
public enum SaleSource { Shop, WalkIn, Wholesale, Post }
MonthInProgress.StartWardMemory; LedgerEntry.WardMemoryStart, LedgerEntry.WardMemoryEnd

// Systems/Ward.cs
public sealed record WardRemembers(int Months, int Deaths, int InfantDeaths, int StrangerDeaths, int Verdicts, int Seizures);
public static class WardSystem { ReportedCauseOf, BlamedOnTheBottle, DeathWeight, MemoryLoss, FrightenedAway, WardPhase, Remembers }

// Machine.cs
Observation.WardMemory (double, 0..100), Observation.WardRemembers (WardRemembers)
ObservedDeath(..., string? Ailment, string ReportedCause)   // appended after Verdict

// Systems/Market.cs
Market.BottlesWanted, Market.CravingPremiumCents, Market.RecordSale, Market.WordOfStrength
Causes.SalesCraving = "sales.craving"; Callers["word"]
// Content: Config.Ward (WardConfig); WalkInsConfig.WordPerHabit/WordHabitFloor/WordHabitCap;
// MarketConfig.BottlesPerTolerance/ToleranceFloor/HabitBottlesMax/CravingPremium/PremiumDebtMonths/
//   PremiumHabitFloor/PremiumHabitSpan/RuinMonths
```

## Notes for the desk (`godot/`, not touched)

- **THE WARD REMEMBERS.**
  - Lead or PERSONAL item when `Observation.WardMemory` is high (say ≥ 40) or `WardRemembers.Verdicts > 0`.
  - The counts are for the copy: "nine customers of this shop dead in a twelvemonth, three juries naming the
    bottle".
  - `LedgerEntry.WardMemoryEnd − WardMemoryStart` says whether it rose this month.
  - A new `customer_left` reason, **`afraid`**: `EditionWriter`'s switch sends unknown reasons to
    "leftDrifted". Suggested: "has taken her custom elsewhere, having heard the talk".
  - "Not been seen about lately" (satire finding 9) can key off `WardRemembers.Deaths > 0`.
- **The premium.**
  - The ledger popup gets a new income line, cause `sales.craving`. The string is in `en.json`: "Paid over the
    price by regulars who crave it".
  - The paper could run it as a temptation: "The craving pays: $4.20 over the price this month."
- **The Trade, by remedy.** Replace the guess at `EditionWriter.cs:243` with the `SaleRecord` splits. The counter
  is `ShopBottles + WalkInBottles`; then `PostBottles` "by post" and `WholesaleBottles` "to the druggists".
  The counter's takings are `ShopRevenueCents + WalkInRevenueCents`, of which `CravingPremiumCents` is over the
  price.
- **The memorial card.** `ObservedDeath.ReportedCause` with `Ailment` (the `ailment.<id>.name` string) gives the
  line the non-gamer asked for. Add "your bottle was not mentioned" when there was no inquest, or the verdict
  when there was one.
- **Callers.** The caller sources gain `word` ("callers who had heard what it does").
