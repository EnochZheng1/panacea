# Panacea · review round 11 · summary

Round 11 was a first-time panel. Four of the five testers were the same kinds of player as in rounds 9 and 10 (a
low-vision player, a min-maxer, a gentle shop-game player and a busy commuter), so the round shows whether the round-10
fix pass landed. The fifth was a keyboard-only player. They played the frozen build `64dc5d8`, which has the whole
round-10 fix pass: large print that reaches the proof's danger print, exact order prices, CONTINUE onto the proof, the
paper's ten-year rest and its holidays, bold claims that cost something, weaning, and letters that no longer threaten.
Every tester started from the title page with a save of their own and played a fresh game by hand from the first
edition, for 10 to 25 months. The min-maxer jumped ahead once for coverage, and says so.

## Scores

| First-time tester   | **R11** | R10 | R9  | Played by hand                        | Headline                                                                                               |
| ------------------- | ------- | --- | --- | ------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| Gentle player       | **8**   | 7   | 7   | 25 months, opium-free; $150 → $327.50 | "It never punished me without telling me why"; one death in 25 months, no inquests, suspicion ≤ 1      |
| Low-vision player   | **7**   | 6   | 6   | 10 months, all at LARGER              | Contrast 11–16:1, states in words; but the controls don't grow, and LARGER drops lines                 |
| Min-maxer           | **7**   | 7   | 7   | 14 months; one jump-ahead             | "The books are a min-maxer's dream", exact to the cent; mild laudanum plus bribes now dominates        |
| Busy commuter       | **7**   | 7   | 7   | 16 months over 8 launches, two shops  | Every relaunch exact; craving cleared in 2–3 months; the craving tooltip is 2,691 px wide              |
| Keyboard-only       | **7**   | —   | —   | 11 months, no mouse                   | Every screen works by keyboard, 10–12 presses a month; lists can't be scrolled and tooltips never show |
| **First-time mean** | **7.2** | 7.0 | 7.0 |                                       | Round 7: 7.2; round 8: 7.2                                                                             |

The mean rose to 7.2. The two kinds of player the last two passes aimed at moved up a point each: the low-vision
player (6 → 7) and the gentle player (7 → 8). The large print, the explanations and the softened letters landed:

- **Large print.** At LARGER the low-vision player found contrast of 11–16:1, states set as words ("POORLY", "ASKS BY
  NAME", "WEANING OFF IT"), and the letters and BOOKS at 9–10 px x-height. "The story of consequences survives large
  print."
- **The honest shop.** "Cost previews on every letter reply. This is the single biggest reason I trusted the game."
  The SUSPICION column's reasons ("Rose 2: replies to the post") and THE MONTH SO FAR meant the gentle player "always
  knew what I had already done".
- **The books.** The min-maxer found every figure (ledger, BOOKS, THE WORKS, THE MONTH SO FAR, the still and SUPPLIES
  buttons, the paper) agreed to the cent, "down to half-cent recipe costs".
- **Saving and CONTINUE.** Every relaunch exact; CONTINUE returns to the screen you saved on.
- **Weaning.** Craving cleared in 2–3 months after the laudanum came out, against 10 months in round 10.
- **Keyboard.** Every screen and drawer can be played without a mouse, and focus lands where the month needs it.

## What the round agreed on

1. **The honest shop is a break-even, and mild laudanum plus bribes dominates (min-maxer, gentle).**
   - Mild laudanum at about 55c cleared 26c a bottle plus the craving surcharge, with no deaths or inquests in 9 months;
     the honest remedy cleared 10–15c and lost callers above 35c, against $3–6 a month of fixed costs.
   - The gentle player's honest year one made +36c, +42c and −$4.42 months, $48 for the year.
   - Buying your way out is always the best expected value: the coroner's $25 for "suspicion does not move" at 2 in 10
     risk, against 8–9 in 10 of +24 for the honest replies; the Captain's $5 for −3 suspicion.
   - In round 10 the min-maxer found the honest shop the better line. Pass 10's cost on bold claims took away the
     honest shop's main lever, and mild laudanum stayed under the inquest threshold.
2. **TEXT SIZE still misses the controls, and LARGER drops lines (low-vision, keyboard, gentle, commuter).**
   - At LARGER the drawer tabs, claim slugs, ALL THE REGULARS, +8/+32, −5c/+5c and letter reply buttons stay at 6–10 px.
   - LARGER drops THE WORKS' last-month money line, the ad preview, the rival ads and two counter cards; NOTES shows
     about three lines with the risk note below the fold.
   - Tooltips run off the screen: the "7 CRAVING" tooltip is one line 2,691 px wide at REGULAR; the suspicion tooltip
     is cut mid-word at LARGER.
3. **Deaths still come with no warning on the card (low-vision, gentle).** Regulars shown "peaky", which the legend
   defines as "not yet in danger", died: after a laudanum recipe change, and of consumption in month 3 of an opium-free
   shop.
4. **What exists only on hover or behind a scroll is lost to keyboard players (keyboard).** ALL THE REGULARS, the BOOKS
   table and the overflow of NOTES and the deaths/suspicion column can't be scrolled by keyboard; suspicion's meaning,
   who is frail, the looks key and jar prices are tooltip-only; the formula book prints only mouse instructions.
5. **VENTURES quotes the wrong month's sales (gentle, min-maxer, low-vision).** "Last month the shop sold fifty-nine
   bottles" when the paper and BOOKS say 89: `Prospectus.cs` reads `Facts.SoldLastMonth`, which is the month before.
6. **The narcotic arc doesn't explain itself (commuter).** After the laudanum came out, craving rose from 2 to 7 and four
   regulars left, because the old strong bottles on the shelf sold first; nothing said so.

## Other findings

| Finding                                                                                                                                                                 | Who                 | Severity |
| ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- | -------- |
| The claim list jumps back to the top after each click; a second click set COMFORT FOR TEETHING BABES by accident                                                        | min-maxer           | Minor    |
| BOOKS lists "$2.00 what the letters cost" as money taken in                                                                                                             | gentle              | Minor    |
| The paper's TRADE lines leave out the craving surcharge, so they don't add up to Takings; SUSPICION's reasons don't add up to its movement                              | min-maxer           | Minor    |
| A wholesale offer at 17c a bottle below the remedy's 20c cost, with no comparison                                                                                       | min-maxer           | Minor    |
| The inquest letter names "an Unknown Young Mother" and the verdict an infant                                                                                            | low-vision          | Minor    |
| One edition names two Nora Sullivans (a new customer and a defendant)                                                                                                   | commuter            | Minor    |
| THE STILL's batch resets to 24 every month                                                                                                                              | commuter            | Minor    |
| "The claims on … reset." when a claim was set                                                                                                                           | commuter, min-maxer | Low      |
| The drawer tabs' "something new" fill is stronger than the focus mark; Enter on the counter does nothing; arrows in the jar grid act like Tab; price is 18 presses away | keyboard            | Low      |
| Disabled "−" buttons on the order blank at 1.70:1; a hovered reply button grows and shifts                                                                              | low-vision          | Low      |
| A new remedy's "Last month, nobody went away over the price."; "best month" lead for a remedy that sold none                                                            | min-maxer           | Low      |
| Ingredients poured but not entered vanish on a stop                                                                                                                     | commuter            | Low      |
| Some items still repeat (the careless-smoker fire three times in ten months; the proprietor's cut; the same display ads)                                                | commuter, gentle    | Low      |
| RID and ObjectDB leak warnings at exit                                                                                                                                  | commuter, min-maxer | Low      |

## Still open from earlier rounds

- Words a newcomer guesses at: Calomel, Fowler's Solution, steeping and simple mixture (gentle).
- "Good name" and "better known" are still read as two meters of one thing (gentle).
