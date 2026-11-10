# Paper fix after round 10: once in a game, the calendar, and a paper that checks its claims

Files changed: `godot/scripts/Paper/EditionWriter.cs`, `godot/scripts/Paper/EditionWriter.Routine.cs`,
`godot/scripts/Paper/PaperFacts.cs`, `godot/data/paper.en.json`, `godot/scripts/UI/EditionView.cs` (the dateline only), and
the paper-fact building in `godot/scripts/Game/SimGame.cs` (three facts and three flags; nothing in the desk model).

## How it was measured

The round-9 harness (`playtests/paper-fix-r9.md`), copied for this worktree. It plays four scripted games from January 1885 to
December 1906 (264 editions each), and writes each edition twice as the game does:

- `alpha`: laudanum ¼ oz, ¼-column ad;
- `beta`: an honest remedy;
- `gamma`: laudanum ½ oz, ½-column ad;
- `delta`: laudanum ¼ oz, the front page every month.

Before and after are the same four games, letter for letter. The harness now also dumps the masthead, the lead cut's caption and
a line of facts per edition. Two scripts count the results:

- `verbatim2.py` counts every printed sentence and every whole paragraph. It reports how often each is printed again within 24
  editions, within 120 (ten years), and at all in the same game.
- `checks.py` counts the contradictions a reader could catch: the clean lead against the deaths and the craving regulars, a
  "dull" lead in a month that sold more, the police court's disposition against its remark, the markets' move word against its
  figures, the weather box against the markets, and the issue number against the date.

Tools and outputs are in the scratch folder `r10fix/paper/` (`run.sh`, `tools/`, `copy/`, `before-out/`, `after-out/`). The
copy is built by `copy/merge.py` from the round-9 file plus this round's modules.

## 1. Repeats across decades (historian #1, commuter #8, min-maxer #12)

> "The Giants have signed a pitcher from Troy…: Apr 25 1885 and Apr 30 1904" … "the cut 'A CUSTOMER, UNDECIDED.' 7 times in 12
> editions" (historian.md #1)

### What changed

**A ten-year rest, once in a game first.** A new rule, `EditionWriter.LongGap = 120`, covers copy that has a plain alternative:

- items of standing matter (CITY ITEMS, BRIEFLY);
- the routine items' closing remarks;
- the Gazette's own wants;
- the advertisers' closing jokes;
- the captions under the stock cuts.

Copy never printed in this game always comes first. Copy printed within ten years is never printed again. The leads, the decks
and the PERSONAL notices keep the round-9 two-year rule (see Open).

**Paced, so the jokes last the game.**

- **Standing matter:** each unprinted item gets its share of the editions it has left to run. That share runs to its last year,
  or to December 1906, counting only its months. The paper prints as many items as those shares add up to, soonest out of date
  first. A shop's first year has three times the share and its second year twice, for the player who plays a year or two.
- **Remarks, wants, jokes and captions** (`PickSpared`): one prints every time while its list is full. As the list empties, it
  prints less often, and the plain item, the plain want, the ad without its joke or the plain caption takes its place.

**More generated news, so the page still fills.** Three new routine generators make fresh items each edition from the year's
streets, vehicles and figures:

- street accidents: horse-cars to 1899, the Broadway cable 1894–1901, trolleys from 1898, scorchers 1893–1901, automobiles from
  1900;
- the Board of Health: milk, placards, vaccinators, cellars;
- the Building Department: double-deckers to 1901, new-law tenements from 1902.

All the routine items now run every month, in a different order each edition. The weather and the theatres gained forms that
print the day's figures (the glass at noon, the house turned away), so a routine item without its remark is still a different
paragraph.

**Era tags and new copy:**

- 125 new items of standing matter. 73 are tagged to their years: the Boodle Aldermen, Comstock, the wires coming down, the
  Kinetoscope and the Vitascope, the Klondike, the Bend coming down, the Poison Squad, the subway digging, the Flatiron's wind,
  Mrs. Nation, the insurance inquiry. 52 are for any year, some of them for a season.
- 81 more weather-box lines (11 to 14 a month now), including coal and ice lines that follow THE MARKETS.
- 46 advertisers' jokes, many for the 1890s and 1900s.
- 27 Gazette wants.
- 30 bodies for the two leads the long game runs most: a bare shelf and a brisk month.
- 29 theatre bills.
- 14 police remarks.
- Captions for every stock cut: 5–7 jokes for each kind of lead, then a plain caption of the cut and the month ("AT THE COUNTER,
  AUGUST, 1885."). The caption is never the same twice.

### Results

Whole paragraphs printed again, over the four games:

| Column                                 | Before: printed / in 24 / in 120 / in game | After: printed / in 24 / in 120 / in game |
| -------------------------------------- | ------------------------------------------ | ----------------------------------------- |
| City items and Briefly (prose)         | 6,648 / 0 / 5,318 / 5,318                  | 2,028 / 0 / **0** / 229                   |
| Routine news (ships, police, fires, …) | 5,888 / 0 / 32 / 41                        | 9,836 / 2 / 74 / 139                      |
| Wants & Sundries                       | 5,749 / 0 / 2,448 / 2,449                  | 5,749 / 0 / **4** / 386                   |
| Holiday items (new)                    | —                                          | 672 / 0 / 0 / **0**                       |
| Lead bodies                            | 1,060 / 5 / 443 / 481                      | 1,060 / 4 / 336 / 409                     |

Sentences printed again:

| Column                   | Before: printed / in 24 / in 120 / in game | After: printed / in 24 / in 120 / in game |
| ------------------------ | ------------------------------------------ | ----------------------------------------- |
| City items and Briefly   | 12,491 / 2 / 9,991 / 9,991                 | 3,881 / 0 / **16** / 451                  |
| Routine news             | 13,131 / 102 / 6,054 / 6,091               | 12,588 / 9 / 576 / 1,296                  |
| Advertisers' jokes       | 1,956 / 0 / 1,656 / 1,656                  | 866 / 0 / **0** / 382                     |
| Captions under the cuts  | 1,038 / 537 / 727 / 731                    | 1,038 / **10** / 180 / 224                |
| Weather box              | 1,060 / 0 / 767 / 768                      | 1,060 / 0 / **0** / 463                   |
| Wants (sentences)        | 10,241 / 4 / 5,978 / 6,226                 | 10,191 / 1,139 / 3,619 / 4,390            |
| Lead headlines and decks | unchanged (two-year rule)                  | unchanged                                 |

Notes on the table:

- The 16 prose sentences "in 120" are sentences two different items share, such as a closing line.
- The 229 prose reprints are items printed again after ten years or more. Every one falls after edition 216 (December 1902), when
  the pool of unprinted items is spent: 57 in a game of about 507.
- The 10 captions "in 24" are a customer's name caption ("MR. ABERNATHY.—From a stock likeness.") for the same person.
- Wants, sentence by sentence: a plain want of the day now takes the Gazette's want's place more often. Its opening clause can
  repeat ("SITUATION WANTED.—By a steady young man, as porter"), with a different box or house number. As whole paragraphs,
  wants within ten years fell from 2,448 to 4.
- Routine news, whole paragraph, in 120: a remark-less weather or theatre item can recur (74 of 9,836).

The round-10 examples, as printed per game (the four games):

| Item                                                 | Before     | After      |
| ---------------------------------------------------- | ---------- | ---------- |
| "The Giants have signed a pitcher from Troy"         | 6, 6, 7, 6 | 2, 2, 2, 2 |
| THE SCHOOLS, the Mott street school's soothing syrup | 7 each     | 1 each     |
| A BIRTHDAY, the hundredth                            | 7, 7, 8, 7 | 1, 1, 2, 1 |
| THE ELEVATED, asleep on the Second Avenue line       | 7 each     | 1, 1, 2, 1 |
| The customs trunk of complexion wafers (a remark)    | 9, 9, 8, 8 | 2, 1, 1, 2 |
| The ventriloquist's bitters; the dog Dandy           | 7 each     | 1 each     |

Where an item still prints twice in a game, the two printings are at least ten years apart: the pitcher runs in 1885 and again
after 1895.

**How much prose a page has.** The writer now hands the make-up 3.0 prose items an edition in the first two years, then about
1.9, against 6.2 before. The rest of the twelve paragraphs are routine news, which has its remark while its list lasts. A page
typically sets 6–9 of the twelve (the December 1885 edition above set nine), so a reader still sees two to four prose items, as
before. They are now new ones.

## 2. The calendar (historian #2)

> "The Jul 25 edition has no Fourth… Nov 28 1885, two days after Thanksgiving, has none. Dec 26 1885 has no Christmas item."

`HolidaysSince` finds the holidays that fell between the last edition and this one:

- New Year's Day;
- St. Patrick's Day;
- Decoration Day: May 30, in the May paper when that paper is dated the 30th itself (1885, 1891, 1896, 1903), and in the June
  paper otherwise;
- the Fourth;
- Labor Day: the first Monday of September, a New York legal holiday from 1887;
- Election Day: the Tuesday after the first Monday of November;
- Thanksgiving: the last Thursday of November, in the December paper in the years the November paper comes out first;
- Christmas: told as "to-day" on December 25, 1886 and 1897, when the paper is dated Christmas Day.

Each holiday's item opens the standing matter. It is chosen in this order:

1. A paragraph written for the day, never printed in this game, the soonest out of date first. There are 8–15 per holiday, 82
   in all, tagged by era: New Year's calls to 1888, "the custom… dying out" 1887–96, Times Square from 1905, Manhattan Field
   1889–96, the Salvation Army's kettles from 1898.
2. When those are spent, one made up for the year from the holiday's forms and slots: the day of the week, the posts in line,
   the burns dressed, the weather. Its remark is one not made in ten years, if there is one.

Election Day has a paragraph for each year with a result worth the news:

- Hill in 1885;
- Hewitt over George and Roosevelt in 1886;
- the first secret ballot and Grant in 1890;
- Flower in 1891;
- Cleveland in 1892;
- McKinley over Bryan in 1896;
- Roosevelt for Governor in 1898;
- McKinley and Roosevelt in 1900;
- Odell in 1902;
- Roosevelt over Parker in 1904;
- Hughes over Hearst in 1906.

The years whose THE ELECTION item of the day already runs (1888, 1894, 1897, 1901, 1903, 1905) print no second item. The nine
holiday paragraphs that were standing matter (one CHRISTMAS, THE FOURTH and the rest) now run only through the calendar, in the
right edition.

Result: 169 holiday items in each game, one after every holiday in its edition, against 42 or 43 month-gated paragraphs before,
some of them reprinted. Whole paragraphs printed twice in a game: **0**.

Seen on screen: a game sent to press by the driver from January 1885 printed "CHRISTMAS.—Holly and greens from the Jersey woods…"
at the head of BRIEFLY on December 26, 1885 (screenshot `dec1885_low.png`).

## 3. The lead contradicts the books (historian #3, min-maxer #12)

> "THE PROPRIETOR'S REMEDY HAS HURT NOBODY IN A TWELVEMONTH" (Aug 1885) for an 8-month-old laudanum shop with a death.

I audited every trade lead for a claim about the shop's record.

**THE CLEAN LEAD** now runs only when all of these hold:

- the shop is six months old or more;
- the writer's memory shows no death in the year behind the edition in which the bottle had a part (the register blamed it, or
  "a strong bottle may have hastened it"), and no inquest;
- no regular is craving the remedy (`PaperFacts.CravingRegulars`, new);
- nothing in any bottle on the shelf forms a habit or does harm by the assay.

Within the clean lead:

- "Twelvemonth" and "a year" need a year of trade (`if: year`). A younger shop gets "{age} without harm" ("eight months").
- "A YEAR WITHOUT A FUNERAL" and "THE UNDERTAKER IS NOT CALLED" need no customer's death at all (`nodeaths`).

**Other leads:**

- **"Dull" is said of the counter.** A month that sold more than the last is never "TRADE DULL", whatever was spent. Before, a
  month in which money was laid out could lead "dull" in the shop's best month (historian, Nov 1885). Such a month now leads
  "brisk", with the decks that explain the books.
- **The ward's tally** ("THE TALK OF SEVEN DEATHS AND TWO CORONER'S VERDICTS…") is not led again within three editions unless
  it has grown. When it runs again unchanged, its decks and bodies do not restate the count.
- **"A Second Still Is Spoken Of"** and "HAS HEARD OF A SECOND STILL" need a shop with one still. "SPEAK OF A SECOND SHOP" needs a
  shop with no branch.
- **The almanac** is "printed again" only the second time the shop takes it up (gentle #9).
- **The PERSONAL notice** names the remedy the shop last sold the customer (`PersonFact.Product`, now set for customers from the
  sim's `LastFormulaId`). The teething mother's notice names the syrup, not the bitters (historian #3).

| Check (four games)                                   | Before   | After   |
| ---------------------------------------------------- | -------- | ------- |
| Clean leads that contradict the books                | 13 of 42 | 0 of 16 |
| "Dull" leads in a month that sold more than the last | 1        | 0       |

## 4. The police court (historian #4)

> "held in three hundred dollars bail… the matter went no further than the fine"

Each case is now built as a piece:

- **The charge** carries the dispositions it can earn (bail or the Island for a pistol, a theft or a flat-iron; a fine, the Island
  or a warning for drink) and the trades that bring it (a truck is driven by a truck driver, fruit sold by a fruit vender).
- **The disposition** (fined, the Island, discharged, held in bail) picks its own words.
- **The remark** asks for the case it fits: "paid the fine in pennies" for a fine, "could not find the bail" for bail,
  "complainant did not appear" only for a case with a complainant and a discharge.

New charges are tied to their months or years: the election-night bonfire, the cannon cracker under a horse in July, scorching
1894–1901, the motor car in the Park from 1902.

I audited the other generators for the same kind of mix:

- **The theatres** each have their kind of bill: variety, legit, opera, melodrama, foreign-language, museum, spectacle. There is
  no strong man at Daly's (historian, Sep 1885). The remarks follow the house: the villainous druggist only in melodrama.
- **The fire's** "nightclothes… remedy for the cold" needs a cold month. "The cause was a cause not known" is gone.
- **The shipping's** icebergs are April to July.
- **The heat remark** at the ball game is June to August.

| Check (four games)                                 | Before | After |
| -------------------------------------------------- | ------ | ----- |
| Police items mixing the disposition and the remark | 56     | 0\*   |
| Police items whose trade does not fit the charge   | 91     | 0     |

\* The checker flags 2. Both are a deliberate "asked for the Island rather than the fine" in a drink case, which was the choice
the court offered.

## 5. The markets and the weather (historian #8)

> Coal at $5.85 "advancing", then "$5.80, unchanged"; the weather says "Coal up" while the markets give coal "declining".

- **The move word** now compares the figure with the one this paper printed in its last edition (`_quotesAt`, replayed with the
  rest of the memory). Higher is "advancing" or "firmer", lower is "easier", and the same is "unchanged". There is no word when
  the last edition did not print the commodity. Before, the word came from a recomputed price for "last month", which for the
  second January edition was December's, and from a ±2% band.
- **The weather box agrees with the markets.** THE MARKETS is set first, and the weather lines that speak of coal or ice ask for
  its move (`coal_up`, `!coal_down`, `ice_up`). "Grey and bitter. Coal up, spirits down." prints only when THE MARKETS has coal
  up.

| Check (four games)                     | Before    | After    |
| -------------------------------------- | --------- | -------- |
| Coal moves that contradict the figures | 95 of 440 | 0 of 440 |
| "Coal up" weather against the markets  | 13 of 28  | 0 of 23  |

## 6. The gilt sign (commuter #3)

> "I chose the touch-up. The next edition's TRADE column said '… one who saw the gilt sign.'"

Both answers to the sign-painter's letter give the shop the same caller source, `sign`. The paper now has the flags
(`gilt_sign`, `sign_touched`, `shopfront_painted`, passed in `PaperFacts.Flags`) and credits the sign the shop has:

- the gilt sign: "drawn by the gilt sign";
- the touch-up: "drawn by the old sign, touched up", "who saw the sign fresh-painted";
- the painted front: "drawn by the new paint on the front";
- otherwise: "drawn by the sign".

In the four games, all 380 credits to the gilt sign fell in shops that had bought it, and the touched-up shop's 24 credits named
the old sign. I checked the other sources against the events that grant them: each is granted only by the event its phrase names.
"Neighbour" is now "neighbor".

## 7. The masthead (historian #9, commuter #7, gentle #8)

- **The issue number** counts Saturdays: `600 + weeks since January 3, 1885`. May 30, 1885 is No. 621, December 26, 1885 is No.
  651, and February 27, 1886 is No. 660. All 1,060 mastheads in the four games are right, against 20 before.
- **The dash.** The string always had an em dash. IM Fell's dash is short, and the paper's grain broke it in two, so it read as
  "--" (my own screenshot of the old build showed it). The dateline now rules the dash: a 2-pixel rule nine-tenths of an em
  long, which follows the text size. At LARGER the date also stays on one line (gentle #8). Screenshots: `dateline_both.png`
  (LARGER over REGULAR), `ed14_larger.png`.

## 8. Small slips (historian's table and #7)

| Slip                                                                           | Now                                                                                                                                 |
| ------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| The Statue "waits in its crates" (March 1885)                                  | "which the French are taking to pieces in Paris to be crated for the voyage"                                                        |
| Icebergs off the Banks in October                                              | April to July only                                                                                                                  |
| "Coroner Hanlon… found that an unknown elderly woman 'Death by Misadventure.'" | Every inquest template returns "a verdict of '…'". The verdict lead's deck is "THE VERDICT UPON …"                                  |
| "The almanac… is printed again" on its first edition                           | Only when taken up before                                                                                                           |
| "No. 249 Doyers street"; "Roosevelt street, room 479"                          | House numbers a street has (Doyers to 21; `streetNumbers`). Rooms only in office buildings; tenement addresses read "up one flight" |
| "waiting for a tram"                                                           | "a car on a line that has been discontinued"                                                                                        |
| Daly's with a strong man                                                       | Venues have kinds (see 4)                                                                                                           |
| "Oysters are in" in April                                                      | September and October                                                                                                               |
| _Maasdam_ in 1906                                                              | To 1902                                                                                                                             |
| "$14.71 owing in hand"                                                         | A till in debt has its own words: "The books close the month in debt: $14.71 owing."                                                |
| "must have a wheel" before the safety bicycle                                  | From 1891                                                                                                                           |
| Plans filed and houses sold at the Exchange "on Sunday"                        | Weekdays only                                                                                                                       |

## The round-9 guarantees

- **The paper agrees with the books.** Every figure still comes from the facts. The new facts (`CravingRegulars`, `Stills`, the
  sign flags, a customer's last remedy) are built in `SimGame.BuildFacts` from the observation and the sim state the month went
  to press with.
- **A loaded save prints the same paper.** All the new memory is kept in the writer and rebuilt by replaying the saved editions:
  printed quotes and moves, the year's dead, the ward's tally, the ventures taken up, the holidays told. The load test showed 0 of
  24 editions differing after a load at month 60 (seeds `seed-a`, `paper-s1`, `gamma`) and at month 170 (`beta`).

## Checks

| Check                               | Result                                                                                                                                   |
| ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `cd godot && dotnet build`          | 0 errors (the one CS8603 warning in `Cuts.cs`, which I did not change)                                                                   |
| `cd sim-cs && dotnet test`          | 134 passed                                                                                                                               |
| `pnpm -s test`                      | 159 passed (15 files)                                                                                                                    |
| `pnpm -s typecheck`, `pnpm -s lint` | pass                                                                                                                                     |
| `pnpm content:lint`                 | 0 errors, 0 warnings                                                                                                                     |
| `npx prettier --check`              | pass, for the files I changed                                                                                                            |
| `python3 godot/tools/check_cuts.py` | up to date                                                                                                                               |
| On screen                           | `--play 12`, `--play 14` at REGULAR and LARGER, and a driver game sent to press from January to December 1885. No exceptions in any log. |

No TypeScript and no content the sim reads changed, so the parity fixtures are untouched.

## Open

**Files I don't own:**

- **"Nothing in the post this month but a circular for galvanic belts."** This line is in `godot/scripts/UI/ProofView.cs:810` and
  `godot/scripts/UI/DeskView.cs:161`, not in the paper. It needs a few variants chosen by the month (seed and turn), for
  example:
  - "Nothing in the post this month but a circular for galvanic belts."
  - "The postman brought nothing but a seed catalogue and a bill from the printer."
  - "No letters this month. A handbill for a rival's liniment was pushed under the door."
  - "Nothing in the post but the almanac of a rival, with your symptoms on every page."
  - "No letters. The postman says the ward is saving its stamps."
  - "Nothing in the post this month but a card from a phrenologist, offering to read your head at a reduction."
- **"Heroin 42c an ounce with morphine at $2.12"** (historian, Sep 1905): THE MARKETS prints the shop's own price for its drugs,
  which comes from `packages/content` (the ingredient's cost).
- **The phrenologist letter's "Neighbour/parlour"** is in the events' strings (`packages/content/strings/en.json`).
- **"A regular for 0 months"** is the counter card's (UI).

**In my files, by choice:**

- **Leads and PERSONAL notices** keep the two-year rule. Lead bodies printed again within ten years fell from 443 to 336, from
  the 30 new bodies, but a 22-year game runs the "bare shelf" and "brisk" leads about 60 times each. Once in a game would need
  some 200 more bodies, or a lead built from a plain opening and a rationed joke, as the routine news now is.
- **The weekly's other Saturdays.** The number counts every Saturday although the game prints one edition a month. That is
  right for a weekly, and the reader sees only the last Saturday's paper.
