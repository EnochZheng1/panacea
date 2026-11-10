# Panacea · balance pass 10 · promises, reasons and weaning

Content 0.12.0 → **0.13.0**. Both sims changed together (TypeScript reference and C# port). Parity fixtures regenerated
(32 runs). `dotnet test` 149/149, TS 174/174, typecheck, lint, prettier, `content:lint` (0/0), `godot` `dotnet build`
and `check_cuts.py` pass. The only Godot file touched is `godot/scripts/UI/LettersDesk.cs`.

Method as in [balance-pass-9.md](balance-pass-9.md): a fresh bot per game, seeds `top-0…39`, mean final cash against
adaptive's mean on the same seeds (median of paired ratios beside it). "Before" is HEAD (`d09fd50`, content 0.12.0),
run from an exported copy of the packages. Scripts are in
`/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/bal10/` (listed at the end).

This pass answers [round10/SUMMARY.md](round10/SUMMARY.md) items 3, 4, 5 and 7 and the min-maxer's, the gentle
player's and the commuter's reports.

## What changed (both sims, identically)

| Rule                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | Config                                                                                                                                                                                            | Code (TS / C#)                                                    |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| **A label's promise.** A claim that names a buyer's complaint promises relief: `promiseShare` × its appeal for an ordinary claim, made good if the bottle gives that much relief beyond the complaint (the shortfall is at most the promise). A **claim to cure** (`Claim.cures`: BANISHES RHEUMATISM, CURES CONSUMPTION, CURES EVERYTHING) promises `cureShare` × its appeal and is never made good. A regular is given `curePatience` months (a course of bottles) before it has wholly disappointed. | `market.promiseShare` 0.5, `cureShare` 0.8, `curePatience` 4, `disappointment` 1; `claims.json` `cures`                                                                                           | `formula.ts promiseShortfall` / `Formulas.PromiseShortfall`       |
| The shortfall counts against a regular's satisfaction, scaled by 1 − dependency (a regular who craves the bottle no longer reads the label). Disappointed regulars buy fewer bottles, leave ("dissatisfied") and lower the word of mouth. A stranger's chance of coming back is judged the same way (with no months of patience: a stranger believes the first bottle).                                                                                                                                 | –                                                                                                                                                                                                 | `body.ts` §5, `market.ts walkIns` / `Body.cs`, `Market.cs`        |
| Regulars the label disappointed are counted: `MonthInProgress/LedgerEntry.disappointed`.                                                                                                                                                                                                                                                                                                                                                                                                                | –                                                                                                                                                                                                 | `state.ts`, `ledger.ts` / `State.cs`, `Ledger.cs`                 |
| **The Board's and the press's eye.** Each claim on a bottle on sale to cure a _deadly_ disease (a `cures` claim naming a complaint with mortality over 1, or every complaint) adds heat each month.                                                                                                                                                                                                                                                                                                     | `advertising.curesHeat` 0.5                                                                                                                                                                       | `advertisingPhase`, cause `advertising.cure_claims`               |
| **A broken promise.** A customer who dies with a deadly complaint the label claimed to cure: heat (cause `scrutiny.broken_promise`) and the ward's memory. `DeathRecord.promisedCure` records it (the observation shows it: the IN MEMORIAM clipping can say so).                                                                                                                                                                                                                                       | `scrutiny.heatPerBrokenPromise` 4, `ward.perBrokenPromise` 3                                                                                                                                      | `recordDeath`, `deathWeight`                                      |
| **A death the bottle had no part in no longer moves the suspicion** (it was half a death's heat).                                                                                                                                                                                                                                                                                                                                                                                                       | `scrutiny.unimplicatedDeathShare` 0                                                                                                                                                               | `recordDeath`                                                     |
| **Inquest numbers in config**, unchanged in value, so the coroner's letter can print them exactly.                                                                                                                                                                                                                                                                                                                                                                                                      | `scrutiny.namedHeat` 24, `namedReputation` 12, `namedNotoriety` 8, `clearedHeat` 3, `testifiedReputation` 1, `exposedHeat` 30, `exposedReputation` 15, `exposedNotoriety` 10, `bribedNotoriety` 2 | `resolveInquest`                                                  |
| **Weaning.** A regular still dependent above `mildDependencyCap` whose bottles this month were all mild (habit under `habitFloor`) loses `weaningDecay` of the dependency a month (was 0.018 a month while any spirit was in the bottle). `Customer.weaning` marks them; the observation's regular has `weaning`.                                                                                                                                                                                       | `body.weaningDecay` 0.4                                                                                                                                                                           | `body.ts` §2 / `Body.cs`                                          |
| **Being better known brings strangers.** The ward's callers are drawn up to base + reputation × 0.2 + min(notoriety, 20) × 0.15. The cap keeps a name the papers print for poisoning from bringing ever more of the curious (without it, the heavy bot's notoriety fed strangers to its 1 oz laudanum).                                                                                                                                                                                                 | `walkIns.perNotoriety` 0.15, `notorietyCap` 20                                                                                                                                                    | `walkIns` / `Market.cs`                                           |
| New requirement **`regularDied`** `{ withinMonths, owing? }`: a named regular (taking the bottle themselves) died within that many months, and with `owing`, owed for spending beyond their means on it. Letter-only (linted). `DeathRecord.owedCents` records the debt.                                                                                                                                                                                                                                | –                                                                                                                                                                                                 | `requirements.ts` / `Progression.cs`, `ContentTypes.cs` converter |

New state fields (pre-release, no save migration): `Customer.weaning`, `DeathRecord.promisedCure`, `DeathRecord.owedCents`,
`MonthInProgress.disappointed`, `LedgerEntry.disappointed`. New causes: `advertising.cure_claims`, `scrutiny.broken_promise`.
The notoriety term widens the ward's `rng.int` range, so every seed's stream moved.

## 1. Bold claims on an honest bottle (min-maxer #2, SUMMARY item 5)

> "Bold claims plus the remedy hint at the cure for its buyer's complaint ('BANISHES RHEUMATISM !!', 'CURES CONSUMPTION
> !!!') ran six months for +2 suspicion with no visible downside, and sales rose. That makes them the clear best choice
> for an honest shop." (min-maxer, #2)

Hand lines (`hand.mts`, 40 seeds, cash means; m6/m12/m24 are the month's proof):

- `hon`: the min-maxer's honest remedy (2 oz spirit, 1 oz valerian, ½ oz capsicum), 75c, SOOTHES THE NERVES, a quarter
  column, the shop next door.
- `honbold`: the same with his four claims: PURIFIES THE BLOOD, BANISHES RHEUMATISM, CURES CONSUMPTION, SOOTHES THE NERVES.
- `honall`: CURES EVERYTHING, CURES CONSUMPTION, BANISHES RHEUMATISM, SOOTHES THE NERVES.
- `cozy` / `cozybold`: pass 9's cozy line (45c), plain and with `honbold`'s claims.

| Line           | m6 before → after | m12 before → after | m24 before → after | Whole game before → after | Heat m24 before → after |
| -------------- | ----------------- | ------------------ | ------------------ | ------------------------- | ----------------------- |
| `hon` (plain)  | $178 → $178       | $236 → $235        | $444 → $443        | $14,138 → $14,138         | 1.8 → 1.6               |
| `honbold`      | $189 → $186       | $266 → $260        | $523 → $487        | $13,771 → $11,958         | 18 → 22                 |
| `honall`       | $201 → $193       | $330 → $269        | $810 → $477        | $15,148 → $7,333          | 30 → 40                 |
| `cozy` (plain) | $152 → $152       | $166 → $167        | $237 → $243        | $5,456 → $5,451           | 1.9 → 2.1               |
| `cozybold`     | $157 → $156       | $183 → $173        | $294 → $250        | $5,350 → $4,401           | 18 → 23                 |

Bold against plain, same bottle:

| Bold ÷ plain                              | m6 before → after | m12 before → after                 | m24 before → after | Whole game before → after |
| ----------------------------------------- | ----------------- | ---------------------------------- | ------------------ | ------------------------- |
| `honbold` ÷ `hon`                         | +6% → +4%         | +13% → +11%                        | **+18% → +10%**    | −3% → **−15%**            |
| `honall` ÷ `hon`                          | +13% → +8%        | +40% → +14%                        | **+82% → +8%**     | +7% → **−48%**            |
| `cozybold` ÷ `cozy`                       | +3% → +3%         | +10% → +4%                         | **+24% → +3%**     | −2% → **−19%**            |
| bottles a month, m1–12, `honbold` ÷ `hon` |                   | 25.6 vs 21.4 → 24.8 vs 21.3 (+16%) |                    |                           |

- **Bold claims still sell more in the short run**: +11–27% more bottles in year one, and 3–14% more cash by month 12. By month 24 the edge is within 10%, and over a whole game an honest shop that lies on its label ends 15–48% poorer.
- **Where the cost shows** (`honbold`, months 13–24): 10.7 regulars a month disappointed by the label (none on `hon`),
  heat 22 at m24 (labels that cure consumption: +0.5 a month; a consumptive who dies on CURES CONSUMPTION: +4). `honall`:
  22 disappointed a month, heat 40.
- **Why not harsher at m24.** With `cureShare` 1 and no months of patience, `honall` fell to $1,487 over a game and gave
  no short-run gain at all; a claim should tempt. With `cureShare` 0.6 and 6 months, `honall` led by 24% at m24. 0.8 and
  4 months is the middle.
- **Left open:** `honall`'s whole game (−48%) is a trap. It shows in the books only if the desk prints the disappointed
  count (see the notes for the desk).

The dishonest lines (pass 9's `opt`: ½ oz laudanum, $1.00, cure-all claims, the coroner paid):

| Line                         | m24 before → after | Whole game before → after         | Heat m24 before → after |
| ---------------------------- | ------------------ | --------------------------------- | ----------------------- |
| `opt` (½ oz, $1.00)          | $670 → $535        | $11,024 → $10,268 (5% early)      | 60 → 62                 |
| `optsil` (silent, no bribes) | $702 → $557        | $16,142 → $16,172                 | 64 → 67                 |
| `opt75` (75c)                | $692 → $608        | $8,386 → $6,225 (10% → 18% early) | 64 → 71                 |
| `mb` (no drug, same claims)  | $631 → $451        | $9,654 → $3,460                   | 44 → 56                 |

`opt` at m24 now leads the honest plain shop by 21% (was 51%), at about 40 times the suspicion.

## 2. Suspicion with a reason (gentle #2, min-maxer #1, SUMMARY item 4)

> "March 1886. 'DEATH OF MR. LEFFERTS'. The deaths column said: '… the bottle is not blamed. No inquest was called.'
> Suspicion still went from 0 to 3, with no line saying why." (gentle)
>
> "Choosing 'Keep your head down' took suspicion from 38 to 75 the moment I clicked." (min-maxer, #1)

- **Decision: a death the bottle had no part in does not move suspicion.** Mr. Lefferts's fever now leaves it at 0.
  A death the remedy caused or hastened still raises it (6, an infant's 12, a stranger's 4). The exception is a broken
  promise: a consumptive who dies with CURES CONSUMPTION on the bottle (+4, "A customer died of what the label claimed
  to cure").
- **Inquests move it by printed numbers.** The amounts were already fixed constants in code. They are config now and
  unchanged: named +24 suspicion, −12 good name, +8 better known; cleared −3 suspicion (+1 good name for testifying);
  an envelope found out +30, −15, +10; a bought verdict +2 better known and no change in suspicion. The coroner's
  letter now prints every one of them under each reply (§4). After the reply, the strip says what moved: "named your
  bottle: suspicion +24, now 62 of 100; good name −12". The min-maxer's 38 → 75 was two named verdicts (+48) and two
  cleared (−6), plus the month's deaths, all in one month. Each line now says what it costs before the click.

**Every heat cause and its string** (for the proof's SUSPICION column; `cause.<id>` in `en.json`):

| Cause id                  | String                                            | When                                                                     |
| ------------------------- | ------------------------------------------------- | ------------------------------------------------------------------------ |
| `advertising.claims`      | Bold claims on the label and in the paper         | monthly: label boldness × 0.2, the ad × 0.1, the almanac (detail its id) |
| `advertising.cure_claims` | Labels that claim to cure a deadly disease        | monthly: 0.5 per such claim on a bottle on sale (new)                    |
| `scrutiny.death`          | Deaths among customers                            | a death the remedy caused or hastened (no longer any other death)        |
| `scrutiny.broken_promise` | A customer died of what the label claimed to cure | new, +4                                                                  |
| `scrutiny.inquest`        | Inquest verdicts                                  | +24 named, −3 cleared, +30 envelope found out                            |
| `scrutiny.decay`          | Talk dies down                                    | monthly fall                                                             |
| `body.complaints`         | Complaints from regulars and their families       | 0.12 a complaint (was "Complaints")                                      |
| `event`                   | Letters answered                                  | a reply's heat effect (detail: the event id)                             |
| `investment.mishap`       | Accidents on the premises                         | the laboratory's fire (+10)                                              |

`timeline` ("Current events") and `unlock` ("New capabilities") exist as causes but no content gives them heat today.
A TS and a C# test play a year of laudanum and bold claims and check that every heat delta has one of these causes and a
string.

## 3. Craving outlasts its cause (commuter #2 and #9, SUMMARY item 7)

> "My first recipe had ½ oz laudanum (January–April 1885). I took it out in May. From July 1885 to March 1886 the shop
> showed '9 craving' (later 8)… The note 'Death waits beside Widow Ida Schwartz' ran for eight months straight." (commuter)

**Why.** Dependency fell 6% a month, and only 1.8% while anything habit-forming was taken. 2 oz of spirit counts, so
0.5 took about four years to fall under the craving line (0.3). Worse, a bottle of habit 0.8 against a dependency of 0.49
reads as "missed their dose", so they shook.

**Now** a regular weaning off an old recipe loses 40% of the dependency a month while their bottles are mild. Opium
withdrawal is days to weeks and the craving lingers a season, so this is weeks to a few months, not years. 0.8 is under
0.3 in the third month and settles at the "taste for it" (0.25) the mild bottle allows.

The commuter's line (`wean.mts`): ½ oz laudanum Jan–Apr 1885 at 60c, then none; 40 seeds, regulars at each proof:

| Proof    | Craving before → after | Shaking before → after | Weaning (after) | Highest dependency before → after |
| -------- | ---------------------- | ---------------------- | --------------- | --------------------------------- |
| May 1885 | 7.1 → 6.6              | 0.0 → 0.1              | 0               | 0.48 → 0.47                       |
| Jun 1885 | 7.1 → 1.9              | 0.3 → 0.2              | 6.9             | 0.49 → 0.39                       |
| Jul 1885 | 6.3 → 0.5              | 0.4 → 0.0              | 3.4             | 0.49 → 0.28                       |
| Aug 1885 | 6.2 → 0.0              | 0.8 → 0.0              | 0.6             | 0.49 → 0.25                       |
| Oct 1885 | 5.5 → 0.0              | 1.0 → 0.0              | 0               | 0.49 → 0.25                       |
| Jan 1886 | 5.2 → 0.0              | 1.0 → 0.0              | 0               | 0.49 → 0.25                       |
| Apr 1886 | 4.8 → 0.0              | 1.4 → 0.0              | 0               | 0.48 → 0.25                       |

The death watch goes with it: the withdrawal harm (3 health a month) that kept Widow Schwartz "poorly… in want of the
bottle" lasts one or two months, not eight. **Surfacing it is the UI's**: `ObservedCustomer.weaning` (C# `Weaning`) is
true in a month the regular was weaned. See the notes for the desk.

## 4. "Better known" and "good name" (gentle #1 and #5, SUMMARY item 3)

> "My kindest replies carried an ominous rider: 'Better known +1: if you ever buy a verdict from the coroner, it is
> likelier to come out.' … For months it read like a threat attached to being nice." (gentle, #1)

- **Being better known now does something good.** A few more strangers come in off the street, having heard the name:
  notoriety up to 20, at 0.15 a point, raises the top of the ward's callers by up to 3.
- **`LettersDesk.cs`.** All 86 riders now read just "Better known +N." The meaning is said once a morning, under the
  first letter whose reply raises it, in the margin hand the desk already uses: "better known, more strangers come in off
  the street, having heard the name." The bad side is printed only where it matters, under "Pay the coroner's fee":
  "The better known the shop, the likelier an envelope comes out: better known 14 now."
- **"Good name" for "standing"** in every reply line ("Good name +1", "good name −2"), in the margin note for a suspected
  shop ("the ward credits at most half of the good name a reply says"), and in the strip after a reply ("your good name
  rises 1").
- `en.json` has no rider text (it was all in the desk), so nothing there needed changing.

## 5. Honest replies that were penalised (gentle #6)

I listed every reply whose only effects are a loss of good name or a rise in suspicion (`pen.py`, about 110), and read
the ones that are the honest or kind answer:

| Reply                                             | Before                                      | After                                                                                          |
| ------------------------------------------------- | ------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| The League's sheet: "Fill in the line truthfully" | good name +1, **suspicion +2**              | **good name +2**: "the League prints your line among the honest ones, for the mothers to read" |
| Feeney opens: "Ignore him"                        | **good name −3**: "the ward takes his side" | **good name −1**: "a few of your callers try the new man, out of curiosity"                    |
| Mrs. Kehoe: the cold spoon and the dispensary     | good name −1                                | costs nothing: "She hoped you had something, but she will take the advice"                     |
| The sanitary inspector: "Let him look"            | good name −2                                | good name −1: "He finds nothing worth writing down, but the ward sees him at it"               |

Left as they are, because the fiction carries them and the line says why:

- refusing the doctor's kickback (+2: "he speaks against your tonic at the bedside");
- the police collection;
- the Alderman's vote;
- the census returned truthfully (+1: "somebody reads confidential returns");
- telling the settlement nurse what is in it (+1).

The satire there is on the authorities.

## 6. Letters grounded in what happened (min-maxer #9)

> "'My husband died in the spring, owing you four dollars and sixty cents on the tonic' (Jun 1886). No man had died
> since December, and I had only put one family on the slate." (min-maxer, #9)

- **An Account Outstanding** now needs a regular who died in the last six months owing for spending beyond their means
  on the bottle (`regularDied`, `owing`). It was any three deaths, strangers included. It no longer asserts a husband,
  a spring or a sum: "I write for the family of one of your regulars, who died this year with your tonic by the bed,
  and owing you on it: your book will have the figure, which I have not the heart to look at…" The desk signs it
  "Respectfully, for the family".
- **Audit.** I read every letter body that asserts a death, a debt or a customer (`died|death|widow|owing|slate|your
customer|regular`). The others are grounded or ask rather than assert:
  - the pawn ticket waits on the promise of a stronger preparation;
  - "Something Stronger" needs a habit-forming remedy;
  - the Kehoe chain follows your replies;
  - Feeney's inquest is his customer;
  - "On the Slate" and the child's errand are requests;
  - "Not the Same Bottle" says "if you have changed the receipt".
- The clerk's Mr. Dunphy ("has had credit since before I came") is the clerk's word on an invented account. I left it.

## 7. Content text against its numbers (historian, min-maxer #8 and #13)

| Text                                 | Before                                                            | After                                                                                                                                 |
| ------------------------------------ | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| THE SHOP NEXT DOOR                   | "for five dollars more a month" ($3 charged)                      | "for three dollars more a month … The old shop holds thirty, so it pays only once they are all coming." (the min-maxer's "Mark well") |
| A FOURTH STILL                       | "a boy to mind the fire at two dollars a month" ($3.00)           | "at three dollars a month"                                                                                                            |
| THE PROPRIETOR'S ALMANAC             | "a year of callers" (post orders unstated)                        | "For a year: ten callers a month who never read a newspaper, a dozen bottles a month sent for by post, and every claim in print."     |
| Druggist's order, wholesale contract | "at a wholesale price"                                            | "at a little over half your price (never more than fifty cents a bottle)" (55%, capped at 50c)                                        |
| Dilute Sulphuric Acid                | ½c an ounce, which the drug shelf rounds to "free, from the pump" | 1c an ounce; "Oil of vitriol, a few drops to the pint of water, bought by the carboy from the chemical works…"                        |

The other ventures (Larger Copper 72 a batch, Branch 20 regulars and a dozen strangers, Laboratory a quarter of the
price) already agree. A test in each sim checks that any "N dollars a month" in a venture's text equals its upkeep, and
that only the pump water is free.

## 8. Words a newcomer guesses at (gentle #10)

- **Ingredients (`ingredient.*.description`, read by the jar tooltips)** now open with a plain gloss:
  - calomel: "A mercury salt, the physician's standby purge…";
  - Fowler's Solution: "Arsenic dissolved in water, sold as a respectable tonic…";
  - laudanum: "opium dissolved in spirit";
  - morphine: "The drug in opium, purified to a white powder";
  - coca: "the leaf cocaine is made from";
  - gentian: "A bitter root";
  - aloes: "which empties the bowels";
  - grain alcohol: "drinking alcohol, nearly pure";
  - acetanilide and heroin also.
- **Processes**:
  - steeping: "soak in the spirit for a week, like tea left in the pot";
  - percolation: "Spirit dripped slowly through the ground drugs, as through a coffee pot, draws out more of them";
  - the percolator unlock says the same.
- **Letters**:
  - court-plaster → "sticking-plaster for the cuts";
  - "lint and carron oil" → "lint and oil for the burns";
  - "as a petit juror" → "to sit on a jury";
  - the phrenologist "read[s] the bumps on the heads of thirty persons a week … and tell[s] each one his character by them".
- "assay" and "the careful households" are Godot strings: see the notes for the desk.

## 9. Every bot, before → after (pending decisions from pass 9)

`batch.ts --bots … --runs 80 --seed b8 --fast` (before = HEAD):

| Bot        | Median final      | Mean              | p10 / p90 after   | Early endings  | Cash m12 / m24 / m48 after | Product deaths | Peak heat |
| ---------- | ----------------- | ----------------- | ----------------- | -------------- | -------------------------- | -------------- | --------- |
| shopkeeper | $16,092 → $16,234 | $16,000 → $16,234 | $15,656 / $16,663 | 0% → 0%        | $236 / $338 / $332         | 3 → 3          | 31 → 28   |
| cautious   | $10,721 → $10,796 | $10,683 → $10,755 | $10,047 / $11,271 | 0% → 0%        | $205 / $281 / $552         | 0 → 0          | 16 → 10   |
| greedy     | $7,763 → $5,579   | $7,724 → $5,299   | −$12 / $8,520     | 85% → **100%** | $95 / $295 / $572          | 8 → 7          | 100 → 100 |
| briber     | $6,288 → $9,348   | $5,313 → $8,174   | −$20 / $13,807    | 30% → 16%      | $200 / $363 / $505         | 27.5 → 27      | 100 → 100 |
| adaptive   | $21,538 → $21,653 | $21,433 → $21,607 | $21,183 / $21,936 | 0% → 0%        | $220 / $259 / $732         | 1 → 1          | 22 → 16   |
| sloppy     | $16,039 → $15,637 | $15,649 → $15,203 | $9,358 / $19,379  | 0% → 0%        | $207 / $252 / $438         | 10 → 10        | 64 → 54   |
| heavy      | $13,809 → $14,278 | $10,169 → $11,268 | $1,705 / $16,750  | 45% → 39%      | $316 / $578 / $675         | 70 → 77        | 100 → 100 |

`cand.mts`, 40 top seeds, × adaptive mean (adaptive $21,505 both); mean ×, median of paired ratios, ended early:

| Line                                              | Before              | After                  |
| ------------------------------------------------- | ------------------- | ---------------------- |
| `b8` 79: 0.12 oz morphine, 83¢, three mild claims | 0.998× (0.997, 0%)  | **1.009×** (1.002, 0%) |
| `ev2` 32 at 80¢: 3 oz spirit, four bold claims    | 1.083× (1.079, 0%)  | 0.888× (0.896, 0%)     |
| `ev2` 32 at 100¢                                  | 1.020× (1.034, 3%)  | 0.838× (0.835, 0%)     |
| `ev2` 32 at 118¢                                  | 0.662× (0.684, 3%)  | 0.466× (0.486, 5%)     |
| `ev2` 37: 2.7 oz spirit, 112¢                     | 0.591× (0.620, 5%)  | 0.166× (0.182, 10%)    |
| `ev1` 14: no narcotic, 2.5 oz spirit, 117¢, bold  | 0.340× (0.358, 0%)  | 0.092× (0.098, 15%)    |
| `ev1` 72: 1.1 oz laudanum escalating, 155¢, bold  | 0.203× (0.192, 20%) | 0.124× (0.130, 15%)    |
| `b8` 84: 0.13 oz morphine, 152¢, $30 ads, bold    | 0.555× (0.575, 8%)  | 0.537× (0.560, 5%)     |
| `b8` 71: 0.63 oz laudanum, 103¢, bold, bribe      | 0.909× (0.945, 5%)  | 0.829× (0.852, 0%)     |
| `ev2` 33: 0.64 oz laudanum, 153¢, bold, bribe     | 0.005× (0.000, 63%) | 0.009× (0.000, 68%)    |
| `ev2` 95: 0.35 oz laudanum, 106¢, mild claims     | 0.917× (0.945, 0%)  | 0.914× (0.966, 5%)     |
| `b8x` 8: 0.1 oz morphine, 3 oz spirit, 109¢       | 0.896× (0.912, 5%)  | 0.916× (0.911, 0%)     |
| shopkeeper                                        | 0.747×              | 0.756×                 |
| cautious                                          | 0.501×              | 0.503×                 |
| briber                                            | 0.316× (15% early)  | 0.382× (5% early)      |
| greedy                                            | 0.334× (90% early)  | 0.261× (100% early)    |
| heavy                                             | 0.514× (40% early)  | 0.588× (20% early)     |

- **No line above 1.01×**, and the bold spirit tonics that came closest (1.08×) fell to 0.89×. Lines with modest claims
  (`b8` 79, `ev2` 95, `b8x` 8) hardly moved.
- **Honest bots within 2%**: adaptive +0.5%, shopkeeper +1%, cautious +1% (sloppy −3%).
- **Pass 9's decisions.**
  - **Cautious** stays where pass 9 put it: 0.50× adaptive, its +55% kept. It never builds a craving, and its single mild
    claim is made good.
  - **Briber** recovers part of pass 9's halving: 0.32× → 0.38×, and early endings 30% → 16% (batch). Deaths it had no part
    in no longer feed the heat, and its claims are mild. The envelope rules are unchanged, and it is still the worst
    shop that ends the game (`optsil` against `opt`: silence beats the envelope by about $6,000).
- **Greedy** (morphine and coca, CURES EVERYTHING and CURES CONSUMPTION, $20 of ads, bribes) now always ends early:
  100%, was 85–90%. The cure claims add a point of heat a month, and its consumptives' deaths are broken promises. It is
  the cartoon villain's line, but if the lead wants it to remain a gamble that sometimes pays, `curesHeat` 0.3 is the
  knob. With no cure heat and no broken-promise heat it ends early 68% of the time, at 0.46×.

## Notes for the desk (`godot/`, not touched except LettersDesk)

- **The proof's inquest warning** (`ProofView.cs` `InquestHeat` / `InquestGoodName`). The values are unchanged: named
  +24 suspicion, −12 good name, +8 better known; cleared −3 (+1 good name if testifying); found-out envelope +30 / −15 /
  +10; bought verdict 0 suspicion, +2 better known. They are now `Config.Scrutiny.NamedHeat`, `NamedReputation`,
  `NamedNotoriety`, `ClearedHeat`, `TestifiedReputation`, `ExposedHeat`, `ExposedReputation`, `ExposedNotoriety` and
  `BribedNotoriety`, so the proof can read config instead of mirroring constants.
- **The SUSPICION column**: the causes and strings are in §2. `scrutiny.death` is now only deaths the remedy caused or
  hastened.
- **Disappointed regulars**: `LedgerEntry.Disappointed`. A Trade line, e.g. "Regulars who had been promised a cure and
  were not cured: {n}." Without it the bold-claims cost is invisible except as regulars leaving "dissatisfied".
- **Weaning**: `ObservedCustomer.Weaning` / `weaning`.
  - The card can read "weaning off the old recipe" in place of "craving".
  - The death-watch note can say "she is weaning off the old recipe; it will pass in a month or two".
  - FormulaBench's "never a craving" tooltip could add "a craving made by an old recipe fades within a season".
- **IN MEMORIAM**: `ObservedDeath.PromisedCure`. "The label on her bottle said CURES CONSUMPTION."
- **THE WORKS' "better known" tooltip** (it says only "a bought verdict is likelier to come out", as `IGame.cs`'s
  comment does). Suggest: "How well known the shop is. More strangers come in off the street (up to 20 counts); a fee
  paid to the coroner is likelier to come out."
- **"assay"** (FormulaBench) is not in content. Suggest a first-use gloss: "the assay (what a chemist would find in it)".
- **"the careful households"** is in `paper.en.json`. Suggest "the careful households (those who will not have a
  narcotic in the house)" the first time the Trade prints it.
- **SUPPLIES** offered +8 / +32 for water and acid. Acid now costs 1c an ounce, so only water is free; the order buttons
  for water should hide.

## Tests

- New `packages/sim/test/round10.test.ts` (15) and `sim-cs/Panacea.Sim.Tests/Round10Tests.cs` (15), mirrored. They check:
  - promise shortfall (ordinary and cure, patience, ailment match);
  - a consumptive on CURES CONSUMPTION is less pleased and counted disappointed;
  - a craving regular reads the label less;
  - cure claims draw heat, and BANISHES RHEUMATISM does not;
  - an unimplicated death leaves suspicion at 0;
  - a broken promise (heat, memory, `promisedCure`);
  - every heat delta in a year of laudanum and bold claims has a cause and a string;
  - inquest numbers equal config and what the letter prints;
  - weaning: under the craving line in three months, the flag set, laudanum still craved;
  - better known brings callers, and the cap holds;
  - `regularDied` (window, owing, strangers);
  - venture texts agree with their terms;
  - only the water is free.
- `round9.test.ts` / `Round9Tests.cs`: a habit already made now fades to the mild cap. `GameTests.EmbeddedContentLoads`:
  version 0.13.0, 955 strings. Death-record literals in `capacity`, `m1` and `round9` tests gained the two new fields.

## Files

- **Content:** `config.json` (0.13.0 and the knobs above), `claims.json` (`cures`), `events.json`:
  - `widow_account` condition;
  - `stowe_tract.answer_truly`;
  - `rival_shop_opens.ignore`;
  - `kehoe_thanks.send_to_dispensary`;
  - `sanitary_inspector.let_him_look`.

  Also `ingredients.json` (acid 1c), `en.json` (§§4–8, two new cause strings, `cause.body.complaints`),
  `src/schemas.ts`, `src/lint.ts`.

- **TS sim:** `systems/formula.ts`, `body.ts`, `market.ts`, `scrutiny.ts`, `ward.ts`, `requirements.ts`, `customers.ts`,
  `core/state.ts`, `core/ledger.ts`, `core/causes.ts`, `game.ts`, `api/machine.ts`.
- **C# sim:** `Content/ContentTypes.cs`, `Core/State.cs`, `Core/Ledger.cs`, `Core/Actions.cs`, `Machine.cs`,
  `Systems/Formula.cs`, `Body.cs`, `Market.cs`, `Scrutiny.cs`, `Ward.cs`, `Progression.cs`, `Pharmacology.cs`.
- **Godot:** `godot/scripts/UI/LettersDesk.cs`.
- **Fixtures:** `sim-cs/fixtures` regenerated.
- **Scratch** (`/tmp/claude-0/-home-user-panacea/5950a741-0a66-5ec4-aee9-4515d76a27d7/scratchpad/bal10/`):
  - `hand.mts`, `lines.mts`, `run.sh`: hand lines;
  - `wean.mts`: the commuter's curve;
  - `cand.mts`, `suite.sh`: × adaptive;
  - `batch.sh`, `tools/btab.py`: bots;
  - `tools/pen.py`: penalised replies;
  - `tools/desk.py`: the LettersDesk rewrite;
  - `base/`: HEAD's packages.
