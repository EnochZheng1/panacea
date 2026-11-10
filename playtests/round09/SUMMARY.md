# Panacea · review round 9 · summary

Round 9 was another first-time panel. None of the five testers had seen the game: a sim optimizer, a satire reader, a
cozy shop-game player, a low-vision reader and a lapsed player who plays in short bursts. They played the frozen build
`49d5c40`, which has every round-8 fix: the paper that ages and rarely repeats, letters that know the season and the
shop's age, autosave mid-month, deaths you can read, and balance pass 8 (price pushes back, the ward runs out of
clerks). Every tester started from the title page with a save file of their own (`--player --profile`) and played a
fresh game by hand from the first edition, for 11 to 24 months. The sim optimizer and the low-vision reader then
jumped ahead 18 months, and the satire reader jumped to 1895 and 1903, for coverage. Their reports say so.

## Scores

| First-time tester   | **R9**  | Played by hand                                | Headline                                                                                                         |
| ------------------- | ------- | --------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Satire reader       | **8**   | 15 months; jumps to 1895 and 1903             | "Sharp, historically exact, almost always aimed upward"; punchlines reprint word for word after about a year     |
| Sim optimizer       | **7**   | 12 months; jump-ahead to 1886                 | "Unusually transparent": the books match the paper to the cent, but an opiate at a high price wins outright      |
| Cozy player         | **7**   | 24 months, an honest shop; THE SHOP NEXT DOOR | An honest shop is "a real and lovely way to play", but "Habit: none" regulars start craving and the money stalls |
| Lapsed player       | **7**   | 15 months over 8 launches, quit 6 ways        | Every relaunch came back exactly; mid-month, nothing shows what you already did                                  |
| Low-vision reader   | **6**   | 11 months; jump-ahead to 1886                 | Headlines and letters read well; much of what you act on is 5–6 px italic, and there is no text-size setting     |
| **First-time mean** | **7.0** |                                               | Round 6: 6.9; round 7: 7.2; round 8: 7.2                                                                         |

The mean fell from 7.2 to 7.0. Without the low-vision reader, whose concerns are new to the panel, it would be 7.25.
The round-8 fixes held up: no tester saw the front door's errors, every save, hard stop and CONTINUE came back
exactly (the lapsed player alone made 7 relaunches, including killing the game inside a drawer and during the press
run), and no exception appeared in any driver log. The paper's month-to-month news was praised as fresh. What holds
the game at 7 now is **repeats over a year or more**, **the numbers not explaining habit, death and money**, and
**small print**.

## What the round agreed on

1. **The paper still repeats, now on a yearly cycle (satire reader, lapsed, cozy, low-vision).**
   - Punchlines reprint word for word after about 12 months: the satire reader counted ten in 15 months ("The
     druggists of the lower wards note that their trade is twice as good", Jan 3 and Nov 28, 1885).
   - Lead stories rerun word for word: "THE BEST MONTH YET FOR DR. ——'S CELEBRATED PANACEA" (Mar 28 and Dec 26, 1885).
   - January's seasonal items come back the next January.
   - THE MARKETS strip is the same every month, and so are the display ads and the almanac and Gazette trade lines.
2. **The numbers don't explain habit, death and money (cozy, sim optimizer, satire reader).**
   - The assay prints "Habit: none" for anything under 1, but spirits make a habit too: 11 of the cozy player's 36
     regulars ended up "craving" on a recipe they chose to be habit-free.
   - Deaths still come from nowhere. A "hale, content" regular died, the paper put the bottle by the bed, and the card
     ended "No inquest has been called. Yet." The satire reader saw "Death waits beside" one regular for three months
     while an unflagged one died.
   - The honest shop's money stalls in year two ($194–204 for nine months while regulars doubled), and nothing says
     why.
3. **An opiate at a high price still wins at the counter (sim optimizer).** At $1.00 no regular walked away from a
   ½ oz laudanum remedy, cravers paid $9 a month over the price, and profit per regular was about 7.5 times the honest
   shop's. At the same 75c, the laudanum remedy outsold the drug-free one 29 to 3. A $20 coroner's fee bought "Died of
   the Vapours" and lowered suspicion. Pass 8 deliberately lets a real habit pay up to twice the comfortable price, so
   this is the design working; the question is whether the pushback (deaths, inquests, exposure) arrives often enough
   in a hand-played game to make it a choice rather than a solved sum.
4. **Letter writers who return don't remember you, except some (lapsed, satire reader).** Feeney never mentions the
   slander or the ignored price war; Detective Lynch and Mrs. Kehoe do remember.
5. **Coming back mid-month shows nothing of what you did (lapsed).** Answered letters vanish ("NO LETTERS"), and money
   spent or recipe changes leave no trace on the proof. The footer and the edition then show two different cash
   figures.
6. **Small print (low-vision reader).** SETTINGS has no text size, zoom
   or contrast. Customer health, suspicion, jar prices and glass labels are 5–6 px x-height italic, and suspicion looks
   the same at "none to speak of" and "alarming (47 of 100)".

## Other findings

| Finding                                                                                                                                   | Who                    | Severity |
| ----------------------------------------------------------------------------------------------------------------------------------------- | ---------------------- | -------- |
| The testimonial was signed by Mrs. Abigail Finch but printed as "Mrs. Lena Wbeer"                                                         | satire reader          | Major    |
| A gender template gives Rosa Cassidy a "wife" who calls her "a quiet man"                                                                 | satire reader          | Minor    |
| Four lines land on the people sold to (mothers, an immigrant with an interpreter) rather than the sellers                                 | satire reader          | Minor    |
| Wrong for the date: New Year's calls "yesterday" on January 31; "Darkest Africa" in 1885; the _Etruria_ arriving before her maiden voyage | lapsed, sim optimizer  | Minor    |
| A letter assumes a boy and a liniment the shop doesn't have; three letters assume a predecessor in a shop the paper calls new             | sim optimizer, satire  | Minor    |
| No warning before a sell-out when the shelf is low but not empty                                                                          | cozy                   | Minor    |
| "Become (a little) better known" on about ten replies, with no meter and no explanation                                                   | cozy                   | Minor    |
| Jargon: peaky, dele, standing in the ward, Lingers, "in an assay"; STEEPING and PERCOLATION have no tooltip                               | cozy                   | Minor    |
| A NEW SHOP from a fresh title page shows the title page again, with CONTINUE, before the paper                                            | cozy, satire reader    | Minor    |
| THE WORKS' drawer tabs move by up to 30 px with the letters box; clicks land on blank space                                               | low-vision, sim optim. | Minor    |
| Old-style figures make "1" read as "I" and "0" as "o" in prices                                                                           | low-vision             | Minor    |
| An open drawer dims cash and suspicion to 2.4–3.2:1 while a letter asks for money                                                         | low-vision             | Minor    |
| Marked and unmarked bottles in THE STILL differ only by a pale tint (1.15:1)                                                              | low-vision             | Minor    |
| The Larger Copper wasn't worth it: sales never filled two 48-bottle stills                                                                | satire reader          | Minor    |
| The Board of Health circular "the same every June" is dated July                                                                          | low-vision, satire     | Low      |
| The weather box says "Pine coffins advance" while THE MARKETS has them steady                                                             | lapsed, satire reader  | Low      |
| The shop next door "stands empty", yet the wall was knocked through to "the cobbler's"; "Taken up … for 0c."                              | cozy, lapsed           | Low      |
| One still's price leaves out the bottles while the both-stills price includes them                                                        | lapsed                 | Low      |
| Defendants' first names and surnames mismatched ("Fritz Esposito"); a small pool of regulars' surnames                                    | satire reader          | Low      |
| "moved 1 bottles"; "three stills are free" in lower case; "{street}" can repeat the shop's own street                                     | sim optimizer, lapsed  | Low      |
| Ledger labels: jury duty and the Jersey distiller booked as "letters answered"                                                            | cozy                   | Low      |
| Clipped claims in the type case; the drug shelf runs past its frame; ±5c buttons stretch beside a long name                               | low-vision, sim optim. | Low      |
| Water is free but "0 oz on hand", so the still won't light until you order it                                                             | cozy                   | Low      |
| A stale tooltip floats over the ventures list after scrolling                                                                             | sim optimizer          | Low      |
| "A Thread object is being destroyed" warning on every quit                                                                                | lapsed                 | Low      |

## What worked

- **Saving.** Five testers and well over a dozen relaunches, including hard stops mid-month, inside a drawer and
  during the press run. Every one came back exactly.
- **The writing.** The satire reader: "the reason to own this: sharp, historically exact, almost always aimed upward,
  and the letters are better still." Grant, Jumbo, Roosevelt's Police Board and Luna Park each arrived in their month.
- **The honest shop.** The cozy player: "never mocks you for being kind." "Mothers in the ward report that Mulberry
  Comfort Tonic does little and harms less, which in this street is a recommendation."
- **The books.** The sim optimizer found the ledger and the paper agreed to the cent every month they checked.
- **Fresh monthly news.** Personals, weather and city items were new every month (low-vision reader).
- **The content note** prepared the cozy player well.
- **No exceptions** in any tester's log.

## Still open from earlier rounds

- The bottle engravings carry their own labels.
- The still picture doesn't show more stills; the counter doesn't show growth.
- Jargon (peaky, assay, Kick, Lingers), raised again by the cozy player.
- The one line at 1.15× adaptive in balance pass 8 (a 0.12 oz morphine tonic at 83¢).
