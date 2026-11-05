# Panacea · review round 6 · summary

Round 6 was a first-time panel only. Five testers had never seen the game: a strategy gamer, a cozy-game player, a
narrative-game reader, a streamer and a keyboard-only player with low vision. None of them could read the earlier
reports, the design documents or the source. They played the frozen build `3e627bf`, which has every round-5 fix
and balance pass 5. Each played a fresh game from the first edition, by hand, for 11–17 months (27–35 minutes), and
was asked to reach a venture. Four of the five then jumped ahead in a second game for coverage, and their reports
say so.

## Scores

| First-time tester         | **R6**  | Ventures by hand                           | Headline                                                                                    |
| ------------------------- | ------- | ------------------------------------------ | ------------------------------------------------------------------------------------------- |
| Streamer                  | **7.5** | Almanac, month 6                           | "The darkest, most clippable consequence loop I've streamed"; the first proof says nothing  |
| Strategy gamer            | **7**   | Almanac, month 7 (paid for itself by post) | Once two stills sell out, the best play is obvious; standing is a hidden stat               |
| Cozy-game player          | **7**   | Almanac, month 9; standing order, month 12 | "A warm, funny shop sim" played honestly; the middle months stall on the still cap          |
| Narrative-game reader     | **7**   | Almanac, month 7; standing order, month 11 | The paper tells the story of the shop, but the small items and leads repeat within the year |
| Keyboard-only, low vision | **6**   | Almanac, month 10                          | A month needs the mouse once (the batch size); focus is often lost or too faint             |
| **First-time mean**       | **6.9** |                                            | Round 5's first-time panel: 6.8                                                             |

The first-time mean barely moved (6.8 to 6.9). Without the keyboard tester, who is grading a different axis, it is
7.1. The ventures fix worked: every tester reached one by hand, between months 6 and 10, against round 5, where only
the almanac was reached and only from month 9. Two took a second venture by month 12. What holds the score at 7
is three things: the shop runs out of capacity in the middle months, the paper repeats itself, and there are a
handful of interaction slips.

## What the round agreed on

1. **The shop hits a ceiling in the middle months (strategy, cozy).** Each still makes at most 48 bottles. With two
   stills, everything sells out from about month 7 or 8, and the leads turn to "THE SHELF BARE". After that, the
   ad, the price and the shelf-dependent ventures (the standing order sold to a bare shelf) stop being choices.
   Nothing says when the next still comes.
2. **The paper still repeats (story, streamer).**
   - **Small items:** 60 of the 125 distinct items in 11 editions ran more than once. Oysters ran three editions in
     a row. The rotation steps six items an edition, but a page prints 12–19, so each edition reprints much of the
     last.
   - **Leads:** the same lead runs word for word up to four times in a year, both "THE SHELF BARE" and "BRISK TRADE".
   - **Personal and verdict lines:** the stock phrases come back four or five times.
3. **The lead contradicts the books (cozy, streamer, keyboard).**
   - "A RECORD MONTH… THE BOOKS WELL UP" runs over "the books down $84.45". The round-5 fix excused venture outlays
     from the slump test, but the record and brisk decks still say the books rose.
   - "TRADE DULL" ran in a best month when a lease ($60) was paid, which is an event payment, not a venture.
4. **Controls move or leak (all five).**
   - **The still:** LIGHT THE FIRE moves after ORDER ENOUGH, so a click missed and a month brewed nothing.
   - **The ventures confirm:** it re-scrolls about 60 px, so the second press misses.
   - **Counter tooltips:** they show over open drawers (four testers).
   - **Escape:** it doesn't close a drawer after a click inside it.
   - **The formula book's price buttons:** they changed the old remedy while a new one was being built.
   - **"ORDER AND LIGHT BOTH":** with two remedies, it read as both remedies.
5. **Nothing teaches the first minutes (streamer, keyboard, strategy).**
   - The first proof doesn't point to the formula book, and Enter, Enter sends an empty month.
   - New stills and ventures arrive with no signal.
   - Death at the counter is unexplained.
   - Standing changes with every letter but is never shown.
6. **The keyboard (keyboard tester).**
   - The still's batch can't be changed without the mouse.
   - Tab never reaches "TAKE IT UP", because the focusable notice cards swallow it.
   - Focus drops to none after LIGHT THE FIRE, −5c and the last reply.
   - Focus on the drug jars and the name field is too faint to see.
   - After ORDER ENOUGH, focus lands on the dearer "both" action.

## Findings by area

**Balance**

| Finding                                                                   | Who            |
| ------------------------------------------------------------------------- | -------------- |
| Two stills sell out from month 7–8; the ad and price become obvious       | strategy, cozy |
| The standing order is offered to, and sold to, a shop whose shelf is bare | cozy           |
| The shop next door raises the rent from $3 to $8 without being chosen     | strategy       |
| The coroner's "fee rises with every envelope" doesn't (both $30)          | story          |

**The paper**

| Finding                                                                              | Who                      |
| ------------------------------------------------------------------------------------ | ------------------------ |
| Small items repeat (60 of 125 in 11 editions); oysters three editions running        | story, streamer          |
| Leads word for word four times a year; personal and verdict phrases repeat           | story, streamer          |
| "THE BOOKS WELL UP" over "the books down"; "TRADE DULL" in a month with a lease paid | cozy, streamer, keyboard |
| "an unknown young mother" in Coroner's Court, "the infant child of" in the lead      | story                    |
| With two remedies, no death says which one the dead took                             | story                    |
| The notice's clipping shows the default bottle and price, not the player's           | story                    |
| A death on a harmless formula with no cause given                                    | strategy                 |

**The desk**

| Finding                                                                            | Who                             |
| ---------------------------------------------------------------------------------- | ------------------------------- |
| LIGHT THE FIRE moves after ORDER ENOUGH                                            | strategy                        |
| Counter tooltips over open drawers; a lingering tooltip eats an Escape             | cozy, story, strategy, streamer |
| The ventures confirm re-scrolls; the drawer jumps                                  | cozy, streamer, keyboard        |
| Escape doesn't close a drawer after a click inside it                              | strategy                        |
| The formula book's price buttons edit the old remedy                               | story                           |
| "BOTH" lights the current remedy twice, read as both remedies                      | streamer                        |
| "POUR IT INTO THE BOTTLE" reads as "bottle a batch"                                | cozy                            |
| The first proof doesn't say what to do; new stills and ventures arrive unsignalled | streamer                        |
| Standing is a hidden stat                                                          | strategy                        |
| Death at the counter unexplained                                                   | streamer                        |
| The type case's claims scroll under the cursor                                     | streamer, keyboard              |

**Keyboard and low vision**

| Finding                                                                                | Who      |
| -------------------------------------------------------------------------------------- | -------- |
| The still's batch can't be set by keyboard                                             | keyboard |
| Tab doesn't reach TAKE IT UP; the confirm moves focus off screen                       | keyboard |
| Focus lost after LIGHT THE FIRE, −5c, POUR, the last reply                             | keyboard |
| Focus on jars (one pixel), clippings (1.34:1) and the name field too faint             | keyboard |
| After ORDER ENOUGH, Enter lands on the dearer BOTH                                     | keyboard |
| Enter, Enter from the first edition sends an empty month                               | keyboard |
| After skipping the press, the footer keeps "press any key" and Space skips the edition | keyboard |
| Instructions are mouse-only ("Click … hold Shift … Right-click")                       | keyboard |
| Key consequences set in 13–14 px italics                                               | keyboard |

## What worked

- **The ventures arrive.** Every tester reached one by hand, at months 6–10. The almanac paid for itself by post.
- **The consequence loop is the hook.** The streamer's best clip was their own "COMFORT FOR TEETHING BABES!"
  notice printed as the lead beside a baby's obituary. The cozy player found it "warm and funny" played honestly.
- **The writing.** The narrative reader called it excellent: the paper tells the story of the shop.
- **No crashes.** No tester found an exception in five games and five jump-ahead runs.

## Next fixes, in order

1. **The paper's repetition.**
   - Step the rotation by the number of items actually printed, or keep a per-game "printed" set.
   - No lead body or deck word for word twice in a year.
   - More bodies for the common leads, and rotate the personal and verdict phrases.
2. **The lead and the books.**
   - Record and brisk decks that say "the books" only when the books rose.
   - Excuse event payments (a lease) as well as ventures.
3. **The middle-game ceiling.**
   - A larger still or a third one sooner, or the still's capacity as a venture.
   - Say when the next still comes.
   - Offer the standing order only when the shelf can serve it.
4. **Interaction.**
   - Keep LIGHT THE FIRE in place.
   - Tooltips off while a drawer is open.
   - The ventures confirm in place.
   - Escape always closes a drawer.
   - The formula book's price buttons label which remedy they change.
   - "BOTH STILLS" names the remedy.
5. **The first minutes.**
   - A pointer on the first proof ("Open the FORMULA book to make your first remedy").
   - A mark on THE STILL or VENTURES when something new arrives.
   - Standing on the Works slip.
   - A line explaining Death.
6. **Keyboard.**
   - The batch by arrows.
   - TAKE IT UP in the Tab order.
   - Focus kept after every action.
   - Stronger focus on jars, clippings and the name field.
   - Keyboard hints in the formula book.
   - The first proof's focus not on SEND TO PRESS.
