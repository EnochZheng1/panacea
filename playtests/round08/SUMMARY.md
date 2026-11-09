# Panacea · review round 8 · summary

Round 8 was a first-time panel again. The five testers had never seen the game: a story-game fan, a Gilded Age
historian, a tycoon fan, a keyboard-only player and a short-session casual player. They played the frozen build
`80e045d`, which has every round-7 fix, the front door (a title page with a content note, settings, a pause menu,
saving and CONTINUE) and both letters passes (172 letters). For the first time every tester started from the title
page, as a bought copy does, with a save file of their own (`--player --profile`). Each played a fresh game by hand
from the first edition: 11 to 26 months. The story-game fan and the historian then jumped ahead to the 1890s and
1900s for coverage, and their reports say so.

## Scores

| First-time tester    | **R8**  | Played by hand                                | Headline                                                                                                           |
| -------------------- | ------- | --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| Historian            | **8**   | 1885–86, then jumps to the 1890s and 1900s    | "The best-researched satire of the patent-medicine trade I have seen"; the paper's filler is stuck in the 1880s    |
| Story-game fan       | **7**   | 20 months, 21 letters; jumps to 1893 and 1903 | The letters are the best writing in the game, but no correspondent came back, and the paper's filler repeats       |
| Tycoon fan           | **7**   | 26 months; $150 → $716, 12 → 68 regulars      | The books add up to the cent; the larger copper didn't change the batch, and deaths and suspicion can't be steered |
| Keyboard-only player | **7**   | 11 months, no mouse at all                    | Everything can be played from the keyboard; focus jumps where it wasn't asked                                      |
| Short-session casual | **7**   | 12 months over six launches, ended four ways  | Every planned stop came back exactly; a hard stop mid-month silently loses the month's work                        |
| **First-time mean**  | **7.2** |                                               | Round 5: 6.8; round 6: 6.9; round 7: 7.2                                                                           |

The mean held at 7.2. The new work held up: saving and CONTINUE brought back the exact shop for every tester, on
every planned stop, including unsent work on the proof and unanswered letters. The letters were praised by everyone
who wrote about them. What holds the game at 7 has moved again: it is now **the paper's filler**, **letters that
don't know where they are in time**, and a few **bugs in the new front door**.

## What the round agreed on

1. **The paper's filler repeats, and doesn't age (story-game fan, historian, casual).**
   - "on the recommendation of a neighbor, who has not been seen about lately" ran in 10 of 20 editions for one
     tester. "THE PROPRIETOR ORDERS A NEW HAT" ran three times for another.
   - About 60 of the roughly 70 paragraphs in the 1905–06 papers had already run in 1885. The Jan 1885 classifieds
     still run in 1903.
   - 1880s items keep running into the 1900s: the Metropolitans, Harry Hill's, the Signal Service forecast, the
     steamer _Wyoming_, the pedestrian match at the Garden, New Year's calls.
2. **Letters don't know what time it is (all five).**
   - The shop's age: "Your customer these four years" one or two years after the shop opened (historian, tycoon).
   - The season: "On the Slate" (a winter letter) in May; the roof that "has let in the rain all winter" in October.
   - The order of a chain: Feeney's handbill is signed "two doors down" two months before "Feeney has opened two
     doors down".
   - The date in the letter: Election Day in January after "come November"; a medicine show leaving "the first of
     May" offered in August.
3. **The writing assumes an opiate that isn't in the bottle (story-game fan).** A remedy assaying Habit _none_
   drew a "I have to have it" letter, and a death story calling the remedy "indispensable to him" beside a card that
   says "Died of old age".
4. **No correspondent came back (story-game fan).** With 172 letters, chains are spread thin: in 20 months no
   correspondent wrote twice, and hooks like the detective's "Somebody keeps a book of who paid" never paid off. The
   tycoon fan, in 26 months, did see two chains return.
5. **Escape on a front card throws (keyboard, story-game fan, tycoon).** Every Escape that closes the pause menu
   or settings logs a `NullReferenceException` at `FrontDoor.cs:154`: the card is freed in `OnEscape`, then
   `SetInputAsHandled` runs on a node no longer in the tree. Nothing visible, but it fired up to 14 times a session.
6. **The front door's backdrop is stale (casual, keyboard, historian).** Behind the title card on relaunch, the
   paper and footer show a fresh January 1885 game while the CONTINUE line shows the save.

## Other findings

| Finding                                                                                                                                        | Who                   | Severity |
| ---------------------------------------------------------------------------------------------------------------------------------------------- | --------------------- | -------- |
| A LARGER COPPER ($200) doesn't change the still's batch: it still opens at 48, and ALL THREE STILLS still lights 48 each                       | tycoon                | Major    |
| Deaths and suspicion can't be steered: 19 clean months on one recipe, then four deaths in a month and suspicion 33 → 55 with no readable cause | tycoon                | Major    |
| In SETTINGS, toggling SOUND moves focus to THE WINDOW, so the next Enter goes full screen                                                      | keyboard              | Major    |
| A hard stop mid-month loses the month's replies and changes, with no word that anything was lost                                               | casual                | Minor    |
| "AT THE LABORATORY.—72 bottles … came out wrong" for a shop with no laboratory; the loss isn't in the books and nothing warned of it           | tycoon                | Minor    |
| The brew button's price leaves out bottles, corks and labels ($16.80 shown, $19.68 spent)                                                      | tycoon                | Minor    |
| Price doesn't push back: 60c → 95c doubled sales for 2–4 walkaways a month                                                                     | tycoon                | Minor    |
| The counter strip has no focus ring and Down does nothing; 17 Tabs to SEND TO PRESS                                                            | keyboard              | Minor    |
| −5c / +5c sends focus back to the drug shelf                                                                                                   | keyboard              | Minor    |
| The headline says "THE BOOKS WELL UP" when the brief says "the books down $2.05"                                                               | keyboard              | Minor    |
| A paid reply never shows: GOLD MEDAL, CHICAGO 1893 never appeared as a claim or in the paper                                                   | story-game fan        | Minor    |
| TRADE TALK says the shop is "some way yet" from twenty-five regulars at 29                                                                     | story-game fan        | Minor    |
| Letters about things that aren't so: a label claim the shop never set; thanks from a family that was never a customer                          | story-game fan        | Minor    |
| A regular's letter gives one street, then the paper reports her missing from another                                                           | story-game fan        | Minor    |
| The percolator and the second and third stills arrive unannounced; VENTURES' "So far" line implies the second still costs $200                 | casual, story, tycoon | Low      |
| Money is about a tenth of period prices (rent $3 a month, a boy $2 a month)                                                                    | historian             | Low      |
| "Coroner Messemer" is a real coroner of the period, and the game gives him fictional verdicts                                                  | historian (praised)   | Low      |
| ☰ MENU is never in the Tab order; BACK on settings looks focused when it isn't; arrow jumps on the proof are odd                              | keyboard              | Low      |
| The two sound toggles have different names (THE SHOP HEARD / ON)                                                                               | casual                | Low      |
| Shared surnames read as families that never connect (the Bernsteins, the Krauses)                                                              | story-game fan        | Low      |
| No history view of the shop's growth                                                                                                           | tycoon                | Low      |

## What worked

- **Saving.** Five testers, at least nine round trips, and every planned stop came back exactly: the date, cash,
  regulars, unanswered letters, and work set on the proof but not sent. A NEW SHOP asks before discarding a save.
  The settings persist.
- **The letters.** "The best writing in it: period-exact, and every reply is a real choice" (story-game fan). Every
  reply prints what it does before you choose (tycoon). The historian named the pedestal fund, the red clause, the
  mug book and the 1905–06 run as exactly right.
- **The history.** The historian's period-accuracy table has 19 items, most of them small, and their score was 8.
- **Keyboard play.** Every part of the game can be reached without a mouse; the still brews in five keys.
- **The ledger.** It added up to the cent for 26 months.
- **No crashes.** The only exception is the harmless one on Escape.

## Still open from earlier rounds

- The bottle engravings carry their own labels.
- The still picture doesn't show more stills; the counter doesn't show growth.
- Words a non-gamer couldn't decode (peaky, assay, Kick, Lingers).
- A stronger exploit line that predates the letters: no narcotic, 3 oz spirit, $1.18, every bold claim and every
  venture, at 1.41× adaptive (`events-pass-2.md`).
