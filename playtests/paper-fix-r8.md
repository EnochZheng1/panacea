# Paper fix after round 8: the filler, its age, and copy that contradicts the facts

Files changed: `godot/scripts/Paper/EditionWriter.cs`, `godot/scripts/Paper/EditionWriter.Routine.cs` (new), `godot/scripts/Paper/PaperFacts.cs`,
`godot/data/paper.en.json`, and the fact-building parts of `godot/scripts/Game/SimGame.cs` (prospects, remedy assay, the dead's
remedy and craving, flags, the grateful letter's writer). No layout code was changed.

## How it was measured

A console harness (scratch, not committed) compiles the Paper scripts, `SimGame` and the C# sim, plays four 264-month games with a
scripted proprietor, writes every edition **twice** (as the game does), lets the "page" set 12 standing items (7 in the first edition),
and dumps every printed paragraph. A script then matches each paragraph against every template in `paper.en.json` (placeholders as
wildcards) and counts how often a template is printed again within N editions of its last printing.

Games: `alpha` (laudanum ¼ oz, ¼-col ad), `beta` (an honest remedy: alcohol, syrup, peppermint, valerian; Habit _none_, Danger
_harmless_), `gamma` (laudanum ½ oz, ½-col ad), `delta` (laudanum ¼ oz, the front page every month). All four run January 1885 to
December 1906. Before = the build as round 8 played it; after = this pass.

**One fact about the page decides the arithmetic:** `EditionView.Balance` sets standing matter until the columns balance, capped at
12 paragraphs, and in every edition measured it used all 12. At 12 a month, a three-year window needs about 430 different items per
era. This pass gets there in two ways: half the slots now carry **routine news made fresh each edition** (ships, the forecast, the
police court, the ball game, the week's deaths, a fire, the theatres), so they never repeat as text; and the prose pool went from 81
to 335 items, tagged by era.

## Repeats, before and after (summed over the four games)

"In 6" = printings whose previous printing of the same template was fewer than 6 editions before; likewise 12, 24, 36.

| Pool                                                | Before: uses / in 6 / in 12 / in 36 / median gap                                          | After: uses / in 6 / in 12 / in 36 / median gap                                               |
| --------------------------------------------------- | ----------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Standing items (City Items, Briefly)                | 12,700 / 4,324 / 11,217 / 12,312 / **6**                                                  | 6,814 / **0** / **0** / 820 / **38** (none within 24)                                         |
| Routine news (new; text never repeats)              | –                                                                                         | 4,075 items; its frames recur every ~6, its closing remarks (108 of them): 0 in 12, median 17 |
| Classifieds, the Gazette's own                      | 5,599 / 3,485 / 5,385 / 5,479 / **5**                                                     | 3,341 / **0** / **0** / 2,912 / **18**                                                        |
| Classifieds, plain wants of the day (new)           | –                                                                                         | 2,523 made fresh; 25 frames, none within 6                                                    |
| Personal (joined, wary, left ×5)                    | 1,378 / 786 / 1,065 / 1,294 / **4**                                                       | 1,077 / **12** / 169 / 757 / **18**                                                           |
| Deaths (notices, leads, decks, out of town)         | 227 / 1 / 24 / 121 / 30                                                                   | 307 / **0** / 1 / 43 / 55                                                                     |
| Inquests                                            | 62 / 0 / 1 / 10 / 72                                                                      | 45 / 0 / 4 / 7 / 77                                                                           |
| Lead headlines                                      | 899 / 188 / 319 / 764 / 12                                                                | 940 / **17** / 373 / 793 / 12                                                                 |
| Lead decks (e.g. "THE PROPRIETOR ORDERS A NEW HAT") | 2,258 / 1,600 / 1,934 / 2,098 / **3**                                                     | 2,278 / **19** / 788 / 1,826 / **12**                                                         |
| Lead bodies                                         | 899 / 0 / 346 / 672 / 12                                                                  | 940 / 0 / 109 / 702 / 16                                                                      |
| Reading notice (front-page advertiser)              | headline was the first claim every month: "SOOTHES THE NERVES!" 255 times in 255 editions | 9 frames × the claims in turn; decks 6–7 variants each, 8+8 bodies                            |
| Notices (bad batch, seizure, ventures…)             | 393 / 104 / 170 / 286 / 11                                                                | 280 / 39\* / 39 / 51 / 36                                                                     |
| Trade column (sales line, books line, TRADE TALK)   | 2,780 / 2,738 / 2,739 / 2,742 / **1**                                                     | 2,498 / **5** / 2,142 / 2,358 / **8**                                                         |

\* the 39 are the RADIO-ACTIVE line on the proprietor's own notice, which is meant to print every month once earned.

The phrases the testers named: "on the recommendation of a neighbor, who has not been seen about lately" ran in 10 of 20 editions for
the story fan; it is now one of 31 "joined" variants, read longest-unused first. "THE PROPRIETOR ORDERS A NEW HAT" is one of seven
record-month decks and prints only when the brief says the books are up.

**Save and load.** The paper's memory of what it printed used to come from the page (`Edition.Printed`), which a replayed save never
calls: in the harness, 24 of 24 editions after a load differed from the unbroken game (the standing matter forgot everything). The
writer now counts the 12 items it hands the page as printed when it writes them, and keeps all its memory (standing, `Fresh`, the
lead kinds, TRADE TALK cadence, flags) in itself: 0 of 24 differ after a load, on two seeds.

## Making the filler age

Every standing item, classified and routine list entry can carry `from`/`to` years (and `months`); the writer passes over those out of
their years. Retired or corrected (historian's table, items 1–15):

| Item                                                                | Now                                                                                                                                                                                       |
| ------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| The Metropolitans beat the Brooklyns (and "the Atlantics' pitcher") | 1885–87, one club named                                                                                                                                                                   |
| Harry Hill's sparring                                               | 1885–86                                                                                                                                                                                   |
| The Signal Service forecast                                         | to 1890; the routine forecast says "the Signal Service" to June 1891, "the Weather Bureau" from July 1891, "Old Probabilities" in the eighties                                            |
| The steamer _Wyoming_                                               | to 1890; ships now rotate from 63 by line and years (Guion and Inman in the eighties, the American Line from 1893, _Kaiser Wilhelm der Grosse_ from 1898, _Oceanic_ 1900, _Baltic_ 1904…) |
| The pedestrian match at the Garden                                  | to 1888; the six-day **bicycle** race from 1892                                                                                                                                           |
| New Year's calls                                                    | to 1889; Times square on New Year's Eve from 1905                                                                                                                                         |
| The trolley killing a dog on Third avenue                           | now Brooklyn's trolleys, 1893+; Broadway cable cars 1893–1901                                                                                                                             |
| Parkhurst                                                           | 1892–95; a nameless "clergyman of Fifth avenue" before                                                                                                                                    |
| Swill milk seized "in Brooklyn" by the Health Department            | the Brooklyn Board of Health, to 1897; the city's Department from 1898                                                                                                                    |
| Tammany's "sachem"                                                  | "the Leader of the Hall"                                                                                                                                                                  |
| Koster & Bial's vitascope with a train                              | 1896–97, the surf at Dover                                                                                                                                                                |
| The Flatiron                                                        | 1902–03, without "twenty-three skidoo"                                                                                                                                                    |
| Collier's "THE MUCKRAKERS" (Oct 1905)                               | "THE MAGAZINES"; "THE MUCK-RAKERS" in April 1906, after Roosevelt's speech                                                                                                                |
| Quinine dear "on the news from Panama"                              | the French canal years, to 1889                                                                                                                                                           |
| The White Star _Germanic_ cleared for Liverpool                     | to 1902                                                                                                                                                                                   |
| Bark from Smyrna                                                    | to 1898                                                                                                                                                                                   |
| "Cheaper than the World" at two cents (1892)                        | "As Cheap as the World"                                                                                                                                                                   |
| The closing edition's list of what the 1906 Act declares            | adds heroin, eucaine, chloral hydrate, acetanilide                                                                                                                                        |
| The abandoned-baby Personal (historian: leans on the victim)        | 1885–89 only                                                                                                                                                                              |
| "The poor will burn the furniture, as usual"                        | rewritten so the joke falls on the coal dealers                                                                                                                                           |
| Police court items                                                  | "the court" where they said "the Justice"; routine items say "the Justice" to 1894, "the Magistrate" from 1895                                                                            |

### New copy, by era

|                                    | Any year             | 1885–91     | 1892–98     | 1899–1906   | Spans part of two eras                                                                        |
| ---------------------------------- | -------------------- | ----------- | ----------- | ----------- | --------------------------------------------------------------------------------------------- |
| Standing prose (81 → 335; 254 new) | 145 new (198 in all) | 23 new (29) | 21 new (24) | 49 new (51) | 35 (e.g. Raines law 1896+, Ellis Island 1892–97, Barge Office 1898–1900, the bicycle 1893–98) |
| Items of the day (16 → 41)         | –                    | 13          | 14          | 14          | –                                                                                             |
| The Gazette's own wants (30 → 80)  | 30 new               | 2           | 2           | 8           | 8                                                                                             |

Effective prose pool per year (seasonal items counted by their share of the year): 165–190 in every year 1885–1906, against 6 prose
slots an edition. New era copy covers: Castle Garden, the boodle aldermen, the Broadway horse cars, the Obelisk, the wires cut down,
the Wild West at Erastina, the high wheel, the steam El's cinders; Ellis Island and its fire, the cable cars, the bicycle face, scorchers,
bloomers, Waring's White Wings, Sunday closing, the Raines law sandwich, the Lexow committee, Mulberry Bend coming down and its park,
the Yellow Kid, the Klondike, the war (Maine, the 71st, Santiago, Montauk, army beef), Greater New York, the Columbian parade, the
Chicago fair, Coxey's army, the Horton law; the subway diggings and the subway, the electric El, the automobile and the Automobile Show,
electric cabs, wireless, the tenement law of 1901, the Poison Squad, the Ladies' Home Journal, the Hippodrome, Dreamland's incubators,
the nickel shows, Times square, the Williamsburg bridge, the ice trust, the coal strike, McKinley, the Slocum, the Giants' 1905
championship, Hearst's run, The Jungle, San Francisco, the Pure Food law. The routine news ages with its lists: ships, ball clubs
(the Metropolitans 1885–87, the Highlanders at Hilltop Park 1903+), police courts, theatres (Niblo's to 1894, Koster & Bial's to 1900,
Keith's from 1894, Hammerstein's Victoria from 1899, the Hippodrome from 1905), the plain wants (the high wheel, the safety, the phonograph,
"four rooms and bath", the garage).

## Copy that contradicted the facts

1. **Habit and death.** The death copy now asks the facts before it insinuates. `PaperFacts` carries each remedy's assay (Kick, Calm,
   Habit, Danger, Lingers) and, for the dead, whose bottle it was and whether they were last seen craving it. "Lately become
   indispensable to him" and "could not get through a morning without it" need a regular **and** a remedy of Habit ≥ 1 or a customer
   the cutting itself shows "asking for it by name". "Slept soundly to the last", "excellent spirits", "remarkably cheerful", the quiet
   infant and the teething label need a sedative remedy that could do harm (Danger not _harmless_, or Lingers) or a register that says
   overdose or wasting. A harmless remedy beside "Died of old age" now gets copy like "It did nothing to prevent the end and, the
   physician says, nothing to hasten it, which its proprietor regards as a testimonial." A sickly month with no bottle to blame says
   "Nobody has thought to look at the bottles, and for once there would be nothing to find in them." The trade leads' "would not be
   without the remedy" and the reading notice's "I cannot do without it" need a remedy of Habit ≥ 1. In the honest-remedy game, every
   remaining "indispensable" stands beside a cutting that says the dead was asking for it by name.
2. **The books in the headline.** Headline, decks and bodies that say the books are up (the new hat, the till that will not shut, the
   best hat) now print only when the brief's own figure is up; a record month with the money spent prints "THE TAKINGS PUT STRAIGHT
   BACK INTO THE BUSINESS" or "THE MONEY HAS GONE INTO THE JARS". Before: 6 such contradictions in four games; after: 0 of 171 claims.
3. **TRADE TALK.** The band (some way / halfway / very near) is now checked against the count, which is printed: "(16 of 25 regulars
   so far)". An unlock that waits on a venture's flag (the cobbler's shop next door) is measured by the venture's own requirement; once
   the venture is on offer the line says the landlord has offered it and waits on the proprietor's word. TRADE TALK runs when its subject
   or band changes, or every fourth month, not every edition. Checked against every printed count: 0 mismatches.
4. **The bad batch** names the place: "IN THE BACK ROOM" (or "AT THE LABORATORY" only if the laboratory venture is held), says the taster
   caught it, and what went down the gutter ("about $10.80 of drugs and glass"); its cutting explains that the drugs were paid for when
   bought, so the loss is in the stock, not a line in the books. Before: 18 laboratory notices from shops without one; after: 0.
5. **Unlocks the player did not choose** (the percolator, the second and third stills, the importer's terms…) are reported as earned:
   "Ten batches run have taught the proprietor the use of the glass percolator, which the trade does not sell so much as hand on". Each
   cutting says plainly "Nothing was bought", what brought it, and what it does ("A new method, Percolation, is on the formula bench: kick
   and calm about a quarter stronger, habit a tenth stronger, 2c. more a bottle to make"), ending "Earned this month, not bought."
6. **Coroner Messemer** is gone; the coroners are Fogarty, Hesselbach, Gilhooley and Hanlon (Nugent went too, in case). I found no other
   real person used as a character in `paper.en.json`; `godot/data` has no other JSON. Real people appear only as public figures in news
   items (Grant, Cody, Barnum, Henry George, Parkhurst, Roosevelt, Waring, Bryan, Dewey, McKinley, Low, McClellan, Hearst, Marconi,
   Wiley, Mathewson, Mr. Straus, Riis unnamed). The doctor-title hint no longer names Cincinnati (historian item 19).

### Requests from the letters pass (folded in)

- `gold_medal_1893` and `radio_active` are exposed to `PaperFacts.Flags`; "GOLD MEDAL, CHICAGO 1893" and "RADIO-ACTIVE" print with the
  claims on the proprietor's own notice (and its cutting), and once in THE TRADE in the edition where the flag first appears. Seen in the
  harness for RADIO-ACTIVE; the medal letter did not come up in the four games, but it takes the same path.
- The testimonial now gives the writer's own street (`Home(f, tw)`), "top floor, rear".
- The grateful letter's writer (and the testimonial's fallback) prefers a Mrs. who buys for herself, not for a teething infant.
- The "indispensable" death story runs only for a remedy of Habit ≥ 1 or a customer seen craving it (above).
- `feeney_closed` is exposed too, but the paper names Feeney only in the callers' line the sim reports ("from Feeney's old shop"),
  so there was nothing to change.

## Checked on screen

Editions of March 1885, December 1894 and May 1905 (seed `paper-s1`, `--play 2/120/245`), scrolled through with the driver: the
columns now end close to level (the routine news is short), headings and dashes are as before, every item belonged to its year (the
Inman _City of Chester_ and Guion _Alaska_ with "the Signal Service" and "the Justice" in 1885; the Spree and Maasdam, the Weather
Bureau and Mulberry Bend in 1894; the subway, the electric El, radium and the Augusta Victoria with "the Magistrate" in 1905). No
exceptions in the logs. `dotnet build`, `python3 godot/tools/check_cuts.py` and `npx prettier --check .` pass.

## Limits

- Standing items: none repeats within two years; 12% of printings come back between 24 and 36 months (median gap 38). Reaching a full
  three years for every item would take roughly another 60 prose items, or fewer prose slots per page.
- The routine frames ("POLICE COURT.—{name}, {age}, {trade}, was fined…") recur about every six editions by design, as a real paper's
  did; the names, ships, scores, numbers and the closing remark differ each time.
- `--play N` (Main.cs) does not run the writer for the skipped months, so a jump-ahead dev game's first paper is "fresh"; a real game
  and a loaded save are not affected.

## Facts to check (I am fairly but not fully sure)

- Ship service years (conservative ranges chosen): Guion _Wyoming/Arizona/Alaska_ to 1890; Inman to 1892 and the American Line from
  Southampton from 1893; NDL _Eider_ to 1891, _Elbe_ to 1894, _Saale_ to 1899; HAPAG _Columbia_ to 1897, _Furst Bismarck_ to 1903;
  _La Bourgogne_ to 1897; Red Star _Westernland/Noordland_ to 1900; HAL _Rotterdam_ 1887–95.
- Clubs: the Brooklyns in the AA to 1889 and the NL from 1890; the Giants' "Polo Grounds" for every year but 1889; Hilltop Park 1903+.
- Weekly deaths in the routine item: about 650–750 a week for Manhattan before 1898 and 1,200–1,350 for the Greater City after, ×1.35 in
  summer, a third to nearly half under five, about an eighth of consumption. Plausible magnitudes, not looked up.
- Police justices replaced by city magistrates in 1895; the Essex Market police court kept to 1899 to be safe.
- The Signal Service station and its weather flags on the Equitable building (an 1885–90 remark).
- Theatre years: Niblo's Garden to 1894, Koster & Bial's to 1900, Keith's Union Square from 1894, Hammerstein's Olympia 1896–98, the
  London and the Windsor on the Bowery throughout.
- The Ladies' Home Journal refusing patent-medicine advertisements from 1892 (the item says "has long refused").
- Mr. Straus's pasteurized-milk depots from 1893; electric hansoms from 1898 (dated 1899+); the 1886 horse-car strike; the boodle aldermen
  fleeing to Canada 1886–87; the Obelisk's coating 1885; the Mayor's men cutting the Broadway poles in 1889.
- Times Square revels from New Year's Eve 1904–05 (historian's note), dated January 1905 on.
- The Slocum item (June 1904): "more than a thousand dead", rotten life preservers, iron in some to make up the weight.
