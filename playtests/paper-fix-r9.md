# Paper fix after round 9: two years between jokes, a paper that moves, and the dead explained

Files changed: `godot/scripts/Paper/EditionWriter.cs`, `godot/scripts/Paper/EditionWriter.Routine.cs`,
`godot/scripts/Paper/PaperFacts.cs`, `godot/data/paper.en.json`, and the paper fact-building in `godot/scripts/Game/SimGame.cs`
(the grateful letter's writer, the dead's pending inquest, last month's takings, the cash lines by cause, the outlay filter).
`godot/scripts/Paper/EditionWriter.Routine.cs.uid` is new: Godot generated it on import, and round 8 never committed it.
`EditionView.cs` did not need changing.

## How it was measured

The round-8 harness (`playtests/paper-fix-r8.md`): four scripted 264-month games, January 1885 to December 1906, each edition
written twice as the game does. The games are `alpha` (laudanum ¼ oz, ¼-col ad), `beta` (an honest remedy), `gamma` (laudanum
½ oz, ½-col ad) and `delta` (laudanum ¼ oz, the front page every month). Two changes to the harness:

- The scripted proprietor answered letters with `string.GetHashCode()`, which .NET randomises per process, so the "four games"
  were different games on every run. It now uses a fixed hash, and before and after are the same four games, letter for letter.
- It now also dumps the weather box, THE MARKETS and every display ad.

I counted two ways:

- **Word for word**, as a reader meets it: every printed sentence (headlines, decks and the weather box whole), and how often the
  same sentence is printed again fewer than 12, 24 or 36 editions after its last printing.
- **By template** (the round-8 script), placeholders as wildcards.

### Word for word: sentences printed again within N editions (four games)

| Column                                   | Before: printed / in 12 / **in 24** | After: printed / in 12 / **in 24** |
| ---------------------------------------- | ----------------------------------- | ---------------------------------- |
| Lead headlines                           | 1,060 / 320 / **759**               | 1,060 / 0 / **1**                  |
| Lead decks                               | 2,664 / 1,369 / **1,763**           | 2,386 / 9 / **27**                 |
| Lead bodies                              | 2,190 / 854 / **1,416**             | 2,026 / 11 / **42**                |
| City items and Briefly (standing matter) | 26,312 / 7 / **5,091**              | 25,954 / 2 / **105**               |
| Wants & Sundries                         | 10,959 / 1,570 / **6,653**          | 11,051 / 1 / **2**                 |
| Personal                                 | 1,125 / 13 / **45**                 | 1,131 / 4 / **6**                  |
| Weather box                              | 1,060 / 0 / 4 (964 within 36)       | 1,060 / 0 / **0** (0 within 36)    |
| Display ads, the closing joke            | 2,120 / 1,938 / **2,060**           | 1,956 / 0 / **0**                  |
| THE MARKETS, the whole strip             | 70 strips printed more than once    | **0** of 1,060 strips repeat       |
| The trade column                         | 7,582 / 2,710 / 2,933               | 7,340 / 1,503 / 1,811              |

What is left within 24:

- **Headline.** The one repeat is "DEATH OF MR. ROMANO" for two different Romanos. The sim's surname pool is small.
- **Decks.** "A RESIDENT OF CHERRY STREET OWES ALL TO…" (the reading notice's street deck), and the death decks in gamma's run of
  deaths.
- **Lead bodies.** Reading-notice tails such as "Sold on Mulberry street, 60c.—Adv.", which end every bought front page.
- **Standing.** Plain sentences of the police court, such as "He was fined two dollars." These are fact, not jokes.
- **Trade column.** Ledger figures in a scripted game that sells the same each month. For example, the almanac's "twelve bottles,
  $7.20" is fixed by the sim (see "Open").
- **Display ads.** The advertiser's name and slogan repeat, as a real advertiser's did. Only the joke line must rest two years.

### By template (the round-8 table's pools)

| Pool                          | Before: uses / in 12 / **in 24** / median gap | After: uses / in 12 / **in 24** / median gap |
| ----------------------------- | --------------------------------------------- | -------------------------------------------- |
| Routine news, closing remarks | 5,713 / 0 / **4,373** / 17                    | 5,618 / 0 / **0** / 30                       |
| The Gazette's own wants       | 3,392 / 0 / **2,799** / 20                    | 2,873 / 0 / **0** / 24                       |
| Standing prose                | 6,814 / 0 / **0** / 38                        | 6,812 / 0 / **0** / 38                       |
| Lead bodies                   | 682 / 137 / **434** / 14                      | 682 / 0 / **0** / 30                         |
| Lead decks                    | 1,679 / 688 / **1,150** / 12                  | 1,663 / 1 / **5** / 33                       |
| Lead headlines                | 682 / 208 / **513** / 12                      | 729 / 29 / 54\* / 45                         |
| Deaths                        | 362 / 8 / **66** / 37                         | 410 / 2 / **27** / 54                        |
| Personal                      | 806 / 50 / **148** / 56                       | 763 / 25 / **27** / 63                       |

\* The template matcher counts a short headline found inside a longer one ("MORE NEW FACES" inside "STILL MORE NEW FACES"). Word
for word, 1 headline repeats.

## 1. Long-run repeats (satire reader, lapsed, cozy, low-vision)

> "Ten repeats in my own 15 months… 'Nobody has asked the bottles.' (Jan 31 1885, Jan 30 1886, Jun 1895, Jun 1903)"
> (satire-reader.md #1)
>
> "THE BEST MONTH YET FOR DR. ——'S CELEBRATED PANACEA (Mar 28 1885 and Dec 26 1885)" (lapsed.md #3)

The writer's memory now enforces the gap (`EditionWriter.Gap = 24`):

- `FreshRested`/`PickRested` return only copy unprinted for 24 editions, or nothing, and every caller has an honest fallback:
  - A routine item (ships, the forecast, the police court, the deaths, a fire, the theatres, the ball game) prints without its
    closing remark.
  - A Gazette want gives way to a plain want of the day.
  - A deck is left out.
  - A customer's notice whose joke has run is named with the others in one plain line ("Also among the month's new customers: …",
    with a cutting that lists them).
  - A trade lead whose every headline has run lately leads with another true thing about the month ("brisk" instead of
    "shortage" when both hold).
  - The reading notice quotes a different satisfied customer each month and falls back to the proprietor's own puff.
- The memory still lives in the writer and is rebuilt by replaying the saved editions. Load test: **0 of 24 editions differ after
  a load**, on three seeds.

New copy where the pools were too small:

- Closing remarks: 108 → 198, every kind now with 21–39.
- Trade-lead headlines 55 → 153, with about 130 more decks and 40 more bodies.
- The reading notice: 16 frames, 15 decks, 14 bodies.
- 8 death leads and 23 lines on the bottle's part in a death.
- Verdict and exposure headlines.
- Plain wants given slots, so "A girl for general housework…" no longer reads the same each time.

**Seasonal items.** January's weather box no longer returns the next January: 6–7 lines a month, and none came back within 36
editions in four games. Seasonal closing remarks rest two years like the rest.

## 2. Things that never change (low-vision #13, lapsed #9 and #12, cozy #9)

**THE MARKETS** now prints, each month:

- The house's remedy, in one of five phrasings.
- Two of the shop's dear drugs in turn, at the price the shop really pays, marked "(dearer)" or "(cheaper)" when a supplier's
  rise or a discount moved them.
- Two commodities "on the Exchange" that drift by the month, with era prices:
  - opium, quinine (95c. an ounce in 1885, 26c. by 1900), camphor (up with the Japanese monopoly), cod-liver oil, glycerine,
    spirits and gentian; cocaine from 1886;
  - coal in the cold months (×2.6 in the 1902 strike) and ice in the hot (×1.8 in the 1900 Ice Trust summer).
- A pine coffin priced by the city's weekly deaths (the same figure THE MORTALITY prints) and the shop's own dead,
  "advancing" when it rose.
- The lilies, by the shop's dead and Easter.

The weather box says "Pine coffins advance" only when THE MARKETS has them advancing: 106 of 106 in the four games.

**Display ads.**

- Seventeen advertisers, four of them new: Texas Jack Wilder's Prairie Bitters, Dr. Hartwell's Catarrh Snuff (1886+),
  Dr. Ashby's Crimson Pellets (1892+) and Professor Marvin's Magic Liniment (to 1898). They rotate, the longest out of the paper
  first.
- Each has 3–7 closing jokes that rest two years, and some change their body copy with the years.
- Mrs. Whittle's joke is now "notwithstanding her funeral in 'eighty-three" to 1892, "ten years ago" 1893–97, and "a million
  letters, all but the first few from beyond the grave" from 1898. There is no more March funeral in a February paper.

**Trade lines.**

- The callers' line, the post orders, wholesale, stock and "turned away by the price" each have 3–8 frames.
- Each caller source has two to four phrasings ("ten by the almanac", "ten with the almanac in hand", "ten who had read the
  almanac").
- The shelf-bare sentence is no longer repeated under a SOLD OUT lead.

## 3. Wrong for the date (lapsed #5 and #7, sim optimizer #3)

- **Dates by month.** `from`/`to` may now be a month ("1885-05") anywhere in the copy, not only a year.
- **New Year's.** "Gentlemen made their calls on New Year's Day… and the druggists opened early the next morning for the
  headaches."
- **Darkest Africa.** "Darkest Africa" from 1890; "the Dark Continent" before.
- **Ships**, by maiden voyage:

  | Ship                | First edition it can arrive in |
  | ------------------- | ------------------------------ |
  | _Etruria_           | May 1885                       |
  | _Campania_          | May 1893                       |
  | _Lucania_           | October 1893                   |
  | _Majestic_          | May 1890                       |
  | _City of Paris_     | May 1889                       |
  | _St. Louis_         | July 1895                      |
  | _St. Paul_          | November 1895                  |
  | _Fürst Bismarck_    | June 1891                      |
  | _Statendam_         | September 1898                 |
  | _Potsdam_           | June 1900                      |
  | _Cedric_            | March 1903                     |
  | _Kaiser Wilhelm II_ | May 1903                       |
  | _Baltic_            | July 1904                      |

- **Other dated items:**
  - the Hippodrome from May 1905;
  - the ball game ends with the September paper ("baseball on October 31", cozy #9);
  - Christmas is told in the past tense (the December paper is dated the 25th–31st);
  - the Chinese New Year in February only;
  - Decoration Day in the June paper;
  - Thanksgiving and the newsboys' Thursday dinner only when the paper's date is after the last Thursday of November. In 1893,
    1899 and 1905 it is not.

## 4. The testimonial under the wrong name (satire reader #2)

The grateful letter's writer is now chosen in the fact-building the month the letter arrives, and remembered as she was. That
happens before the reader sees the letter, and again on every replay. The testimonial prints her name whether or not she still
buys at the shop. Before, a writer who had left fell back on "any delighted Mrs.".

Harness check over 40 seeds, each printing the first grateful letter:

- before: 39 of 40 correct ("signed Mrs. Concetta Russo, printed Mrs. Louisa Schmidt");
- after: **40 of 40**.

## 5. Gender (satire reader #3)

The police court passes `{spouse}`, `{spouseObject}` and `{man}`: "The prisoner's husband said she had been a quiet woman until
the bottle; the prisoner said the same of him." I checked every `{pronoun}` template for hard-coded gender. The only other
problem was the dead infant's line on the bottle, which was given the mother's pronoun; it now says "the child".

## 6. Punching down (satire reader #4)

Rewritten so the last clause lands on the trade, the court or the press:

- **The interpreter.** He arrives after the fine is paid, "and the court, which had understood nothing, charged the county for
  his time."
- **The lost child.** The mother claims the child before supper, and a proprietor calling with soothing syrup "for the child's
  nerves" is shown the door.
- **The baby left behind.** Now a proprietor's crate of Elixir left on the Foundling Asylum's steps: "The Sisters have the babies
  in hand."
- **The basket on the church steps.** Now two dozen bottles left by a proprietor for the Sexton's endorsement, which the Sexton
  gives to the Board of Health.

Found in the audit and rewritten the same way:

- the infant lead in which the mother "informs the neighbors that the child slept through every night… and this last one also"
  (now "the label had promised that it would sleep. The label kept its promise.");
- the tenement-roof child dosed by its mother (now a druggist sent down the stairs);
- the widow's pension spent on nerve tonic;
- "resting" mocking the prisoner;
- "Melancholy general among the unemployed" in the weather box.

## 7. Small clashes (lapsed #8 and #9, sim optimizer #7, satire reader #9)

- **"Walked from Mulberry street."** That joined line now needs the customer to live elsewhere (`if: away`). The bad batch's
  gutter is Mulberry street's.
- **The shop next door.** It is "the empty shop next door, the cobbler's that was" in TRADE TALK and the notice, which now agree
  with the prospectus's "stands empty".
- **Plurals.** The compositor's last pass makes one of anything singular ("1 bottles", "one callers" and the rest). "had no,
  paying 0c." is now "had none, the shelf being bare".
- **Defendants' names.** Each is drawn from one family of names (Irish, Italian, German, Jewish, Polish from 1888, Scandinavian,
  native), weighted to the lower wards.

## 8. Deaths explained (cozy #2)

The paper now gives the bottle's part in every death of a customer, using the proof's reading (`ProofView.BottleNote`), so the
paper and the proof agree:

- the register blames the bottle (overdose or wasting);
- the remedy is harmless by the assay and the dead was not craving it, so the bottle is not blamed;
- a strong bottle may have hastened it, and nobody can say;
- nothing had been bought at the shop in six months.

The cutting prints that line. The lead story ends with the physician's word in the paper's voice, unless its own body already
gave it.

For Mr. Bruno's case: "A bottle from the shop on Mulberry street stood by the bed, half finished… He died of what brought him to
the shop, the physician says, and not of the remedy, which neither helped nor hindered."

"No inquest has been called. Yet." is gone. A new fact, `PersonFact.InquestPending`, says whether the inquest the death called
waits in the letters, and the cutting reads "An inquest sits: see your letters." or "No inquest was called."

## 9. Why the till is flat (cozy #3)

**WHERE THE MONEY WENT.** When the takings rose on last month but the till closed within an eighth of the takings of where it
opened, THE TRADE says why, at most once in three editions. The fact-building now passes last month's takings and the cash
lines by cause. Example:

> "The shop took $17.40 this month, $1.20 more than last, and yet the till closed only $1.60 fuller than it opened. A bottle of
> Mulberry Street Balm sells at 60c. and costs 13c. to make, so 47c. of each sale is the shop's. What came in went out again:
> drugs for the next batches $8.41, bottles, corks and labels $2.64 and the letters answered $2.00, with $2.75 in smaller sums.
> Of that, $7.46 is on the shelf and in the jars, not lost."

It printed 9 times in the honest game.

## Requests from the lead, folded in

- **Death notice.** The dead regular's notice no longer ends "Yet.", and says whether the bottle played a part, as in 8 above.
- **THE MONTH IN BRIEF** names the money laid out on the business whenever there was an outlay: "the books up $9.60, after $1.50
  laid out on the business". Before, it did so only when the books were down.
- **Outlays.** `InvestedCents` no longer counts the "event" cause, so trade given up to answer a letter (jury duty) is not "laid
  out on the business".

## The round-8 guarantees

- **The books.** Every figure in the trade column, the brief and the ledger cutting still comes from the books. The new money
  line lists the month's own cash lines.
- **Loaded saves.** 0 of 24 editions differ after a load (seeds `seed-a`, `paper-s1`, `gamma`).

## Checks

| Check                               | Result                                                                                                                                                                                                                                                                                    |
| ----------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `cd godot && dotnet build`          | 0 errors (one CS8603 warning, in `Cuts.cs`, which I did not change)                                                                                                                                                                                                                       |
| `cd sim-cs && dotnet test`          | 117 passed                                                                                                                                                                                                                                                                                |
| `pnpm -s typecheck`, `pnpm -s lint` | pass                                                                                                                                                                                                                                                                                      |
| `npx prettier --check .`            | pass                                                                                                                                                                                                                                                                                      |
| `pnpm content:lint`                 | 0 errors                                                                                                                                                                                                                                                                                  |
| `python3 godot/tools/check_cuts.py` | up to date                                                                                                                                                                                                                                                                                |
| `pnpm -s test`                      | 138 of 140 with `--testTimeout=120000`. The two failures are timing: "a full run stays within the performance budget" (2,084 ms against 1,000) and a replay test's own 30 s timeout, with the machine's load average at 15. This pass changed no TypeScript and no content the sim reads. |

On screen, `--play 14` (February 1886) renders cleanly. The first edition's MARKETS strip, read with the driver, sets on two lines.
No exceptions appeared in either log.

## Open (files I don't own)

- **The almanac's post orders** are a fixed number every month in the sim ("twelve bottles, $7.20"). The paper varies the words
  but cannot vary the figure without disagreeing with the books. A little month-to-month spread in `Post.Delivered` (sim) would
  finish the lapsed player's #12.
- **"Taken up: The Shop Next Door, for 0c."** is printed by the VENTURES drawer (UI), not by the paper.
- **The regulars' surname pool** is the sim's (two Romanos die within two years; three Krauses). Widening it is for
  `packages/content`.
- **`--play N`** still skips the writer for the skipped months, so a jump-ahead's first paper is "fresh". Real games and loaded
  saves are unaffected.
